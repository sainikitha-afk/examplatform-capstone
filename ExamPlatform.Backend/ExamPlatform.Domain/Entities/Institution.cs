using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "institutions". Backs the "Institute Verified" badge (Part 3/7):
    // a Professional examiner submits a name + logo, an Admin approves or
    // rejects it — mirroring ExaminerRequest's request/decision shape. One
    // institution per examiner (OwnerUserId is unique).
    public class Institution
    {
        public int InstitutionId { get; set; }          // PK

        public int OwnerUserId { get; set; }             // FK -> User (examiner), unique

        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }

        public InstitutionVerificationStatus VerificationStatus { get; set; }

        public DateTime RequestedAt { get; set; }
        public DateTime? VerifiedAt { get; set; }

        // Nullable — set only once an admin has decided.
        public int? VerifiedByAdminUserId { get; set; }  // FK -> User (admin)

        public User Owner { get; set; } = null!;
        public User? VerifiedByAdmin { get; set; }
    }
}
