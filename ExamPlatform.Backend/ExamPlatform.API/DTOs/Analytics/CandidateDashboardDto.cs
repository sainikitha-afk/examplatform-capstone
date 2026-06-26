namespace ExamPlatform.API.DTOs.Analytics
{
    // Response DTO for GET /api/analytics/me — candidate performance dashboard.
    public class CandidateDashboardDto
    {
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }          // percentage, e.g. 66.67
        public int CertificatesEarned { get; set; }
        public List<ExamBestScoreDto> BestScores { get; set; } = new();
    }
}
