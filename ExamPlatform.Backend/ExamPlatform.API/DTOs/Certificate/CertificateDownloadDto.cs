namespace ExamPlatform.API.DTOs.Certificate
{
    // Returned by GET /api/certificates/{id}/download.
    // Contains every field a frontend needs to render or print a certificate.
    // In a production system this would be a PDF byte stream; returning a rich
    // JSON DTO here lets the frontend render the certificate without a PDF library
    // dependency on the server, while a future migration to QuestPDF / iTextSharp
    // only requires changing this action's return type and content-type header.
    public class CertificateDownloadDto
    {
        public int CertificateId { get; set; }
        public string CertificateCode { get; set; } = null!;   // human-readable unique code

        // Candidate's display name — pulled from the User navigation on the cert.
        public string CandidateName { get; set; } = null!;

        // Exam information — pulled from the Exam navigation on the cert.
        public string ExamTitle { get; set; } = null!;
        public decimal TotalMarks { get; set; }

        public decimal Score { get; set; }            // snapshot at issuance
        public DateTime IssuedAt { get; set; }        // UTC
        public string CertStatus { get; set; } = null!;

        // Pre-formatted certificate body text — the frontend can render this
        // verbatim or use it as fallback when building a custom layout.
        // Example: "This certifies that Riya has successfully completed
        //            Software Engineering Assessment with a score of 70 / 100."
        public string CertificateText { get; set; } = null!;
    }
}
