namespace ExamPlatform.API.DTOs.Analytics
{
    // Response DTO for GET /api/analytics/platform — Admin overview.
    public class PlatformStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalExaminers { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalExams { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalCertificatesIssued { get; set; }
        public int ExamsCompletedLast30Days { get; set; }
    }
}
