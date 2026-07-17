// InstitutionVerificationService — Institute Verification badge (Part 3/7).
// Mirrors ExaminerRequestService's request/decision shape: an eligible
// examiner submits, an Admin approves or rejects, the examiner is notified.

#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class InstitutionVerificationService : IInstitutionVerificationService
    {
        private readonly IInstitutionRepository _institutionRepository;
        private readonly IExaminerSubscriptionService _subscriptionService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<InstitutionVerificationService> _logger;

        public InstitutionVerificationService(
            IInstitutionRepository institutionRepository,
            IExaminerSubscriptionService subscriptionService,
            INotificationRepository notificationRepository,
            ILogger<InstitutionVerificationService> logger)
        {
            _institutionRepository = institutionRepository;
            _subscriptionService = subscriptionService;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task<Institution> SubmitAsync(int examinerId, string name, string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ValidationException("Institution name is required.");

            // Institute Verification is a Professional-tier feature — reuses the
            // same tier gate as premium exam creation rather than duplicating
            // the concept of "which tier is this".
            var eligibility = await _subscriptionService.CanCreateExamAsync(examinerId);
            if (!eligibility.CanCreatePremiumExams)
                throw new ValidationException(
                    "Institute Verification is available to Professional examiners only. Upgrade your plan first.");

            var now = DateTime.UtcNow;
            var existing = await _institutionRepository.GetByOwnerUserIdAsync(examinerId);

            if (existing != null)
            {
                if (existing.VerificationStatus != InstitutionVerificationStatus.Rejected)
                    throw new ValidationException(
                        $"You already have a {existing.VerificationStatus} institute verification request.");

                // Re-submission after a rejection updates the same row (OwnerUserId
                // is unique) and resets it to Pending — mirrors ExaminerRequest
                // allowing reapplication after rejection.
                existing.Name = name.Trim();
                existing.LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim();
                existing.VerificationStatus = InstitutionVerificationStatus.Pending;
                existing.RequestedAt = now;
                existing.VerifiedAt = null;
                existing.VerifiedByAdminUserId = null;

                _institutionRepository.Update(existing);
                await _institutionRepository.SaveChangesAsync();

                _logger.LogInformation("Examiner {ExaminerId} re-submitted institute verification.", examinerId);
                return existing;
            }

            var institution = new Institution
            {
                OwnerUserId = examinerId,
                Name = name.Trim(),
                LogoUrl = string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl.Trim(),
                VerificationStatus = InstitutionVerificationStatus.Pending,
                RequestedAt = now
            };
            await _institutionRepository.AddAsync(institution);
            await _institutionRepository.SaveChangesAsync();

            _logger.LogInformation("Examiner {ExaminerId} submitted institute verification.", examinerId);
            return institution;
        }

        public async Task<Institution?> GetMineAsync(int examinerId)
        {
            return await _institutionRepository.GetByOwnerUserIdAsync(examinerId);
        }

        public async Task<IEnumerable<Institution>> GetPendingAsync()
        {
            return await _institutionRepository.GetByStatusAsync(InstitutionVerificationStatus.Pending);
        }

        public async Task<Institution> ApproveAsync(int institutionId, int adminUserId)
        {
            var institution = await _institutionRepository.GetByIdAsync(institutionId)
                ?? throw new InstitutionNotFoundException(institutionId);

            if (institution.VerificationStatus != InstitutionVerificationStatus.Pending)
                throw new ValidationException($"Institution {institutionId} has already been processed.");

            institution.VerificationStatus = InstitutionVerificationStatus.Verified;
            institution.VerifiedAt = DateTime.UtcNow;
            institution.VerifiedByAdminUserId = adminUserId;
            _institutionRepository.Update(institution);
            await _institutionRepository.SaveChangesAsync();

            await _notificationRepository.AddAsync(new Notification
            {
                UserId = institution.OwnerUserId,
                NotifType = NotificationType.InstitutionVerified,
                Message = $"Your institute '{institution.Name}' has been verified. " +
                          "The Institute Verified badge now appears on your premium exams.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _notificationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Institution {InstitutionId} verified by admin {AdminUserId}.", institutionId, adminUserId);

            return institution;
        }

        public async Task<Institution> RejectAsync(int institutionId, int adminUserId)
        {
            var institution = await _institutionRepository.GetByIdAsync(institutionId)
                ?? throw new InstitutionNotFoundException(institutionId);

            if (institution.VerificationStatus != InstitutionVerificationStatus.Pending)
                throw new ValidationException($"Institution {institutionId} has already been processed.");

            institution.VerificationStatus = InstitutionVerificationStatus.Rejected;
            institution.VerifiedAt = DateTime.UtcNow;
            institution.VerifiedByAdminUserId = adminUserId;
            _institutionRepository.Update(institution);
            await _institutionRepository.SaveChangesAsync();

            await _notificationRepository.AddAsync(new Notification
            {
                UserId = institution.OwnerUserId,
                NotifType = NotificationType.InstitutionRejected,
                Message = $"Your institute verification request for '{institution.Name}' was not approved. " +
                          "You may submit a new request with updated details.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _notificationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Institution {InstitutionId} rejected by admin {AdminUserId}.", institutionId, adminUserId);

            return institution;
        }
    }
}
