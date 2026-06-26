namespace ExamPlatform.API.DTOs.Certificate
{
    public class CertificateDto
    {
        public int CertificateId { get; set; }
        public string CertificateCode { get; set; } = null!;
        public int UserId { get; set; }
        public string? UserFullName { get; set; }      // populated from Include
        public int ExamId { get; set; }
        public string? ExamTitle { get; set; }          // populated from Include
        public int AttemptId { get; set; }
        public decimal Score { get; set; }
        public DateTime IssuedAt { get; set; }
        public string CertStatus { get; set; } = null!;
        public DateTime? RevokedAt { get; set; }
    }
}