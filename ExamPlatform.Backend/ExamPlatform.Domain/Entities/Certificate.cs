using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to "certificates". TRANSACTIONAL — issued when an exam's results
    // are published and a candidate's best attempt passed. Revoked (not
    // deleted) if the admin needs to invalidate it.
    public class Certificate
    {
        public int CertificateId { get; set; }                // PK (internal)

        // Human-readable certificate code, e.g., "CERT-2026-000042".
        // Generated in the service layer, exposed to the user. Unique
        // (configured in DAL via Fluent API).
        public string CertificateCode { get; set; } = null!;

        public int UserId { get; set; }                       // FK → User (recipient)
        public int ExamId { get; set; }                       // FK → Exam

        // The specific attempt that earned this certificate (the best-scoring
        // passing attempt). 1-to-1 with Attempt from the Certificate side.
        public int AttemptId { get; set; }

        // Snapshot of the score at issuance time. Snapshotted (not computed
        // on the fly) because the underlying attempt could conceivably be
        // touched later — the certificate must always show what was earned.
        public decimal Score { get; set; }

        public DateTime IssuedAt { get; set; }                // UTC

        public CertificateStatus CertStatus { get; set; }

        // Nullable: only filled when the admin revokes the certificate. Null
        // while the cert is Issued.
        public DateTime? RevokedAt { get; set; }

        // ───────────── PREMIUM BRANDING (additive, snapshotted) ─────────────
        // Snapshotted at issuance for the same reason Score is: the underlying
        // Institution could later be edited or have its verification revoked,
        // but an already-issued certificate must keep showing what was true
        // when it was earned. Default false/null — no change for any existing
        // or future non-premium certificate.
        public bool IsPremium { get; set; }
        public string? InstituteName { get; set; }
        public string? InstituteLogoUrl { get; set; }

        // ───────────── NAVIGATION ─────────────
        public User User { get; set; } = null!;
        public Exam Exam { get; set; } = null!;
        public Attempt Attempt { get; set; } = null!;
    }
}