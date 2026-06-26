namespace ExamPlatform.API.DTOs.Certificate
{
    // Returned by GET /api/certificates/mine and GET /api/certificates/mine/{id}.
    // Deliberately leaner than CertificateDto: candidates do not need UserId or
    // AttemptId, and exposing internal IDs they cannot act on is unnecessary noise.
    // ExamTitle is included so the list is readable without a second API call.
    public class CandidateCertificateDto
    {
        public int CertificateId { get; set; }
        public string CertificateCode { get; set; } = null!;  // e.g. CERT-2026-000001
        public int ExamId { get; set; }
        public string? ExamTitle { get; set; }         // from Include(c => c.Exam) in repo
        public decimal Score { get; set; }
        public DateTime IssuedAt { get; set; }
        public string CertStatus { get; set; } = null!; // "Issued" or "Revoked"
        public DateTime? RevokedAt { get; set; }       // null until revoked by admin
    }
}
