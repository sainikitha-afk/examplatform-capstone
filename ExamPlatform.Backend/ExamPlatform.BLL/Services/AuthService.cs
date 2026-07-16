// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using BCrypt.Net;

namespace ExamPlatform.BLL.Services
{
    public class AuthService : IAuthService
    {
        // ───────────── DEPENDENCIES ─────────────
        // The service depends on the User repository to read/write users.
        // It does NOT touch DbContext directly — that would skip the layering.
        private readonly IUserRepository _userRepository;

        // ILogger<T> is Microsoft's structured logging abstraction.
        // ASP.NET Core injects it automatically via the DI container;
        // we don't need to configure a logger factory ourselves.
        // T = the class using it → log entries are tagged with the class name
        // so you can filter them in the output.
        private readonly ILogger<AuthService> _logger;

        // Constructor injection. DI hands us the repository and logger at request time.
        public AuthService(IUserRepository userRepository, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //                        REGISTER
        // ════════════════════════════════════════════════════════════════
        public async Task<User> RegisterAsync(
            string fullName,
            string username,
            string email,
            string password)
        {
            // ─── STEP 1: GUARD CLAUSES (validation first) ────────────────
            // The "fail fast" pattern: validate inputs at the top.
            // Reject bad data BEFORE expensive operations (DB calls, hashing).

            // Trim before validating/hashing so the stored password never carries
            // accidental edge whitespace — LoginAsync trims its input the same way,
            // and the pair must agree or a space-padded signup could never log in.
            password = password?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ValidationException("Full name is required.");

            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                throw new ValidationException("Username must be at least 3 characters.");

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new ValidationException("A valid email is required.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ValidationException("Password must be at least 6 characters.");

            // ─── STEP 2: BUSINESS RULES (uniqueness checks) ──────────────
            // Email and username must be unique. We check BEFORE inserting
            // to give a clean error rather than letting the DB unique-constraint
            // violation bubble up as a generic exception.

            if (await _userRepository.IsEmailTakenAsync(email))
            {
                // WARNING: a duplicate-email attempt is a business-rule failure,
                // not a programming error — the client should be told to use a
                // different email. LogWarning is appropriate (not LogError).
                // We do NOT log the email value itself to avoid writing PII to logs.
                _logger.LogWarning("Registration attempt failed: email already in use.");
                throw new DuplicateEmailException();
            }

            if (await _userRepository.IsUsernameTakenAsync(username))
            {
                _logger.LogWarning(
                    "Registration attempt failed: username '{Username}' already taken.",
                    username); // username is not sensitive, so it's safe to log
                throw new DuplicateUsernameException();
            }

            // ─── STEP 3: HASH THE PASSWORD ───────────────────────────────
            // BCrypt.HashPassword generates a salt internally and produces
            // a self-contained hash string like:
            //   $2a$11$N9qo8uLOickgx2ZMRZoMye...
            // The salt is embedded; we don't store it separately.
            //
            // Work factor (the "11"): how many iterations. Higher = slower
            // = more secure but more CPU. 11 is a good 2026 baseline.
            // IMPORTANT: NEVER log the password or hash — even the hash is sensitive.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // ─── STEP 4: BUILD THE ENTITY ────────────────────────────────
            // New users default to Candidate role and active status.
            // Admin/Examiner accounts are created separately by an admin —
            // candidates can NEVER self-register as another role.
            var user = new User
            {
                FullName = fullName.Trim(),
                Username = username.Trim(),
                Email = email.Trim().ToLowerInvariant(),  // normalize for uniqueness
                PasswordHash = passwordHash,
                Role = UserRole.Candidate,
                DateOfJoining = DateTime.UtcNow,           // always UTC
                IsActive = true
            };

            // ─── STEP 5: PERSIST ─────────────────────────────────────────
            // Add to the change tracker, then SaveChanges to actually INSERT.
            // After SaveChanges, EF Core has populated user.UserId from
            // the DB-generated identity column.
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // SUCCESS — log at Information level.
            // Named placeholder {UserId} lets log aggregators query by user id.
            _logger.LogInformation(
                "Candidate registered successfully: userId={UserId}, username='{Username}'.",
                user.UserId, user.Username);

            return user;
        }

        // ════════════════════════════════════════════════════════════════
        //                  CREATE EXAMINER (Admin-only)
        // ════════════════════════════════════════════════════════════════
        //
        // Mirrors RegisterAsync but assigns the Examiner role. Called from
        // an admin-protected controller endpoint. We keep the validation +
        // hashing + uniqueness logic identical to register; only the Role differs.
        public async Task<User> CreateExaminerAsync(
            string fullName, string username, string email, string password)
        {
            // Same trim-then-validate as Register — see the note there.
            password = password?.Trim() ?? string.Empty;

            // Same guard clauses as Register.
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ValidationException("Full name is required.");
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                throw new ValidationException("Username must be at least 3 characters.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                throw new ValidationException("A valid email is required.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ValidationException("Password must be at least 6 characters.");

            // Normalize.
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedUsername = username.Trim();

            // Uniqueness.
            if (await _userRepository.IsEmailTakenAsync(normalizedEmail))
            {
                _logger.LogWarning(
                    "CreateExaminer failed: email already in use (username='{Username}').",
                    normalizedUsername);
                throw new DuplicateEmailException();
            }

            if (await _userRepository.IsUsernameTakenAsync(normalizedUsername))
            {
                _logger.LogWarning(
                    "CreateExaminer failed: username '{Username}' already taken.",
                    normalizedUsername);
                throw new DuplicateUsernameException();
            }

            // Hash + build with EXAMINER role.
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                FullName = fullName.Trim(),
                Username = normalizedUsername,
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                Role = UserRole.Examiner,          // ← the only meaningful difference
                DateOfJoining = DateTime.UtcNow,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Examiner account created: userId={UserId}, username='{Username}'.",
                user.UserId, user.Username);

            return user;
        }

        // ════════════════════════════════════════════════════════════════
        //                         LOGIN
        // ════════════════════════════════════════════════════════════════
        public async Task<User> LoginAsync(string emailOrUsername, string password)
        {
            // ─── STEP 0: NORMALISE COPY-PASTE WHITESPACE ─────────────────
            // Credentials pasted from an email (the examiner temp-password mail
            // reads "Password : Xy3kQ…\n") routinely arrive with a trailing
            // space or newline attached, and BCrypt.Verify then fails on an
            // otherwise-correct password. Registration and change-password trim
            // the same way, so a stored password never has edge whitespace and
            // trimming here can never lock a legitimate user out.
            emailOrUsername = emailOrUsername?.Trim() ?? string.Empty;
            password = password?.Trim() ?? string.Empty;

            // ─── STEP 1: BASIC INPUT GUARDS ──────────────────────────────
            // We throw InvalidCredentialsException (not ValidationException)
            // even for empty inputs at login. Reason: never give an attacker
            // ANY signal about why login failed. All login failures look the same.
            if (string.IsNullOrWhiteSpace(emailOrUsername) || string.IsNullOrWhiteSpace(password))
                throw new InvalidCredentialsException();

            // ─── STEP 2: FIND THE USER ──────────────────────────────────
            // Try email first, then username. Either is acceptable as the
            // identifier (FR1.2 — login with email or username).
            // Normalize email lookups to lowercase.
            var user = await _userRepository.GetByEmailAsync(emailOrUsername.ToLowerInvariant())
                       ?? await _userRepository.GetByUsernameAsync(emailOrUsername);

            // Same exception whether the user doesn't exist or password is wrong.
            // (Security: don't let attackers enumerate valid emails/usernames.)
            // WARNING log here: repeated warnings for the same input = brute-force signal.
            if (user == null)
            {
                _logger.LogWarning(
                    "Login attempt for unknown identifier (not found in DB).");
                // We do NOT log the identifier itself — it could be someone's email.
                throw new InvalidCredentialsException();
            }

            // ─── STEP 3: CHECK ACTIVATION ────────────────────────────────
            // Deactivated users cannot log in (business rule 25 / FR11.2).
            // Distinct exception from invalid credentials because here the
            // password might be correct — we just refuse to let them in.
            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Login blocked for deactivated user {UserId}.", user.UserId);
                throw new UserDeactivatedException();
            }

            // ─── STEP 4: VERIFY PASSWORD ─────────────────────────────────
            // BCrypt.Verify hashes the input password the same way (using the
            // salt embedded in the stored hash) and compares the result.
            // Constant-time comparison internally prevents timing attacks.
            var passwordIsValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!passwordIsValid)
            {
                // WARNING: failed password on a KNOWN user. Possible brute-force.
                _logger.LogWarning(
                    "Login failed: wrong password for user {UserId}.", user.UserId);
                throw new InvalidCredentialsException();
            }

            // ─── STEP 5: RETURN ──────────────────────────────────────────
            // The user is authenticated. Return the entity; the controller
            // will package it into a JWT and respond to the client.
            _logger.LogInformation(
                "User {UserId} (role={Role}) logged in successfully.", user.UserId, user.Role);

            return user;
        }

        // ════════════════════════════════════════════════════════════════
        //                     CHANGE PASSWORD
        // ════════════════════════════════════════════════════════════════
        // The missing half of the examiner-onboarding flow: approval mails a
        // temporary password and tells the new examiner to "change it after
        // first login" — this is the endpoint that finally makes that possible.
        // Works for every role (candidates change theirs the same way).
        public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            // Trim exactly as Register/Login do, so a temp password pasted from
            // the approval email (with its trailing newline) still verifies.
            currentPassword = currentPassword?.Trim() ?? string.Empty;
            newPassword = newPassword?.Trim() ?? string.Empty;

            // A wrong CURRENT password is deliberately a ValidationException (400),
            // not InvalidCredentialsException (401): the user *is* authenticated —
            // their JWT proved that — they just mistyped a form field. A 401 here
            // would make the frontend interceptor clear the session and bounce them
            // to the login screen mid-form, which is hostile for a typo.
            if (string.IsNullOrWhiteSpace(currentPassword))
                throw new ValidationException("Current password is required.");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new ValidationException("New password must be at least 6 characters.");

            if (newPassword == currentPassword)
                throw new ValidationException("New password must be different from the current password.");

            // userId comes from the JWT (controller), never from the request body —
            // a user can only ever change their OWN password through this path.
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new UserNotFoundException(userId);

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                // Failed current-password check on a logged-in account is worth a
                // warning — repeated hits could be someone at an unlocked session.
                _logger.LogWarning(
                    "Change-password failed: wrong current password for user {UserId}.", user.UserId);
                throw new ValidationException("Current password is incorrect.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User {UserId} changed their password.", user.UserId);
        }
    }
}
