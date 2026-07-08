// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using System.Security.Cryptography;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class ExaminerRequestService : IExaminerRequestService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IExaminerRequestRepository _requestRepository;

        // Needed to:
        //   a) Fan-out admin notifications on new request (GetAllByRoleAsync).
        //   b) Create the examiner User account on approval.
        //   c) Verify username uniqueness during account creation.
        private readonly IUserRepository _userRepository;

        // Creates in-app Notification rows for admins (new request)
        // and for the new examiner (account approved).
        private readonly INotificationRepository _notifRepository;

        // Sends approval/rejection emails. If Smtp:Host is empty in config
        // the DI container injects NoOpNotificationEmailService — the workflow
        // still works, the email just gets logged instead of sent.
        private readonly INotificationEmailService _emailService;

        private readonly ILogger<ExaminerRequestService> _logger;

        public ExaminerRequestService(
            IExaminerRequestRepository requestRepository,
            IUserRepository userRepository,
            INotificationRepository notifRepository,
            INotificationEmailService emailService,
            ILogger<ExaminerRequestService> logger)
        {
            _requestRepository  = requestRepository;
            _userRepository     = userRepository;
            _notifRepository    = notifRepository;
            _emailService       = emailService;
            _logger             = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   SUBMIT  (anonymous — public endpoint)
        // ════════════════════════════════════════════════════════════════
        public async Task<ExaminerRequest> SubmitAsync(
            string fullName, string email, string purpose)
        {
            // ─── Input validation ────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ValidationException("Full name is required.");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new ValidationException("A valid email address is required.");

            if (string.IsNullOrWhiteSpace(purpose) || purpose.Length < 20)
                throw new ValidationException(
                    "Please describe your purpose in at least 20 characters.");

            // ─── Duplicate-request guard ─────────────────────────────────
            // We block a second submission while a Pending or Approved request
            // already exists. This prevents an impatient user from flooding
            // the admin inbox with the same request. A Rejected request is NOT
            // blocked — the user may legitimately re-apply.
            if (await _requestRepository.HasActiveRequestAsync(email))
            {
                _logger.LogWarning(
                    "Duplicate examiner request blocked for email {Email}.", email);
                throw new DuplicateEmailException(
                    "An active examiner request already exists for this email address. " +
                    "Please wait for admin review or contact support.");
            }

            // ─── Create the request ──────────────────────────────────────
            var request = new ExaminerRequest
            {
                FullName    = fullName.Trim(),
                Email       = email.Trim().ToLower(),
                Purpose     = purpose.Trim(),
                Status      = ExaminerRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };
            await _requestRepository.AddAsync(request);

            // ─── Notify all admins ───────────────────────────────────────
            // GetAllByRoleAsync returns all active Admin users. We create one
            // Notification row per admin so each admin sees it in their own
            // inbox (GET /api/notifications). All rows share the same
            // SaveChangesAsync call below — one round-trip for the batch.
            var admins = await _userRepository.GetAllByRoleAsync(UserRole.Admin);
            foreach (var admin in admins)
            {
                await _notifRepository.AddAsync(new Notification
                {
                    UserId    = admin.UserId,
                    NotifType = NotificationType.ExaminerRequestReceived,
                    Message   = $"New examiner request from {request.FullName} ({request.Email}): \"{request.Purpose.Substring(0, Math.Min(80, request.Purpose.Length))}…\"",
                    IsRead    = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Flush request INSERT + all notification INSERTs in one transaction.
            await _requestRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Examiner request submitted by {Email}; {AdminCount} admin(s) notified.",
                request.Email, admins.Count());

            return request;
        }

        // ════════════════════════════════════════════════════════════════
        //   READ
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<ExaminerRequest>> GetAllAsync()
            => await _requestRepository.GetAllAsync();

        public async Task<IEnumerable<ExaminerRequest>> GetPendingAsync()
            => await _requestRepository.GetPendingAsync();

        public async Task<ExaminerRequest> GetByIdAsync(int requestId)
        {
            var request = await _requestRepository.GetByIdAsync(requestId);
            if (request == null)
                throw new ExaminerRequestNotFoundException(requestId);
            return request;
        }

        // ════════════════════════════════════════════════════════════════
        //   APPROVE
        // ════════════════════════════════════════════════════════════════
        public async Task<ExaminerApprovalResult> ApproveAsync(int requestId)
        {
            var request = await GetByIdAsync(requestId);

            // Idempotency guard — throw if already processed.
            if (request.Status != ExaminerRequestStatus.Pending)
                throw new ExaminerRequestAlreadyProcessedException(
                    requestId, request.Status.ToString());

            // ─── Guard: email must not already belong to a user account ─────
            // If the applicant registered as a Candidate with the same email,
            // SaveChangesAsync would throw DbUpdateException (unique constraint)
            // → unhandled → 500. Throw DuplicateEmailException (409) instead.
            if (await _userRepository.IsEmailTakenAsync(request.Email))
                throw new DuplicateEmailException(
                    $"A user account already exists for {request.Email}. " +
                    "This request cannot be approved — the applicant already has " +
                    "an account and can log in with their existing credentials.");

            // ─── Generate credentials ────────────────────────────────────
            // Username: derived from the email prefix (everything before @).
            // e.g. "riya.sharma@gmail.com" → "riya_sharma".
            // Dots and plus signs are replaced with underscores to stay
            // within the allowed character set. If the derived name is already
            // taken, numeric suffixes are appended until it's unique.
            var username = await GenerateUniqueUsernameAsync(request.Email);
            var tempPassword = GenerateTemporaryPassword();

            // BCrypt.HashPassword produces a 60-char string containing the
            // salt embedded in the hash. Work factor 11 (default) = ~100ms
            // per hash on modern hardware — slow enough to resist brute force,
            // fast enough for a signup flow.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            // ─── Create the examiner User account ───────────────────────
            var examiner = new User
            {
                FullName      = request.FullName,
                Username      = username,
                Email         = request.Email,
                PasswordHash  = passwordHash,
                Role          = UserRole.Examiner,
                DateOfJoining = DateTime.UtcNow,
                IsActive      = true
            };
            await _userRepository.AddAsync(examiner);

            // EF Core must flush the User INSERT first so we get a real
            // UserId before we reference it in the Notification below.
            await _userRepository.SaveChangesAsync();

            // ─── Welcome notification for the new examiner ───────────────
            // The new user will see this the first time they open
            // GET /api/notifications after logging in.
            await _notifRepository.AddAsync(new Notification
            {
                UserId    = examiner.UserId,
                NotifType = NotificationType.ExaminerAccountApproved,
                Message   = $"Welcome, {examiner.FullName}! Your examiner account is ready. " +
                             "Please change your temporary password after your first login.",
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });

            // ─── Update the request ──────────────────────────────────────
            request.Status      = ExaminerRequestStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            _requestRepository.Update(request);

            // Flush notification INSERT + request UPDATE in one round-trip.
            await _requestRepository.SaveChangesAsync();

            // ─── Send approval email ─────────────────────────────────────
            // Fire-and-forget with try/catch — email failure must NOT roll
            // back the approval. The admin can see the temp password in the
            // HTTP response and relay it manually if needed.
            try
            {
                await _emailService.SendAsync(
                    toEmail: request.Email,
                    subject: "Your Examiner Account is Ready — Exam Platform",
                    body:
                        $"Dear {request.FullName},\n\n" +
                        $"Your examiner account request has been approved.\n\n" +
                        $"Login credentials:\n" +
                        $"  Username : {username}\n" +
                        $"  Password : {tempPassword}\n\n" +
                        "Please log in and change your password immediately.\n\n" +
                        "Best regards,\nExam Platform Team");
            }
            catch (Exception ex)
            {
                // Log the failure but do NOT rethrow — the account was created
                // successfully. The controller response includes the credentials.
                _logger.LogWarning(ex,
                    "Approval email could not be sent to {Email}; " +
                    "credentials are in the HTTP response.", request.Email);
            }

            _logger.LogInformation(
                "Examiner request {RequestId} approved; User {UserId} created (username={Username}).",
                requestId, examiner.UserId, username);

            return new ExaminerApprovalResult
            {
                Request           = request,
                CreatedUser       = examiner,
                TemporaryPassword = tempPassword
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   REJECT
        // ════════════════════════════════════════════════════════════════
        public async Task<ExaminerRequest> RejectAsync(int requestId, string? adminNote)
        {
            var request = await GetByIdAsync(requestId);

            if (request.Status != ExaminerRequestStatus.Pending)
                throw new ExaminerRequestAlreadyProcessedException(
                    requestId, request.Status.ToString());

            request.Status      = ExaminerRequestStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.AdminNote   = adminNote?.Trim();

            _requestRepository.Update(request);
            await _requestRepository.SaveChangesAsync();

            // Send rejection email — fire-and-forget, same pattern as approval.
            try
            {
                var noteSection = string.IsNullOrWhiteSpace(adminNote)
                    ? string.Empty
                    : $"\n\nReason: {adminNote}\n";

                await _emailService.SendAsync(
                    toEmail: request.Email,
                    subject: "Examiner Request Update — Exam Platform",
                    body:
                        $"Dear {request.FullName},\n\n" +
                        "We have reviewed your examiner account request and are unable " +
                        $"to approve it at this time.{noteSection}\n" +
                        "You may reapply in the future.\n\n" +
                        "Best regards,\nExam Platform Team");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Rejection email could not be sent to {Email}.", request.Email);
            }

            _logger.LogInformation(
                "Examiner request {RequestId} rejected (adminNote={HasNote}).",
                requestId, !string.IsNullOrEmpty(adminNote));

            return request;
        }

        // ───────────── PRIVATE HELPERS ─────────────

        // Derives a username from the email prefix, ensures uniqueness by
        // appending an incrementing suffix if the base name is taken.
        // "riya.sharma@gmail.com" → "riya_sharma" (or "riya_sharma1", etc.)
        private async Task<string> GenerateUniqueUsernameAsync(string email)
        {
            // Take everything before '@', replace non-alphanumeric chars with '_'.
            var prefix = email.Split('@')[0];
            var baseUsername = new string(
                prefix.Select(c => char.IsLetterOrDigit(c) ? char.ToLower(c) : '_').ToArray());

            var candidate = baseUsername;
            var suffix    = 1;

            // Loop until we find an unclaimed username. In practice almost
            // never runs more than once — the suffix loop is a safety net.
            while (await _userRepository.IsUsernameTakenAsync(candidate))
                candidate = $"{baseUsername}{suffix++}";

            return candidate;
        }

        // Generates a cryptographically random 12-character password.
        // Uses RandomNumberGenerator (CSPRNG) — NOT System.Random (not crypto-safe).
        // Ambiguous characters (0/O, 1/l/I) are excluded to avoid confusion
        // when the admin reads the password out to someone over the phone.
        private static string GenerateTemporaryPassword()
        {
            const string chars =
                "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";

            var bytes = new byte[12];
            RandomNumberGenerator.Fill(bytes);

            // Map each random byte to a char in the allowed set.
            // Modulo bias is negligible here (55 chars, byte range 0-255).
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}
