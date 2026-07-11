namespace ExamPlatform.API.DTOs.Analytics
{
    // Per-exam best score entry within CandidateDashboardDto.
    public class ExamBestScoreDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = null!;
        public decimal BestScore { get; set; }
        public bool IsPassed { get; set; }
        public string? TopicScoresUrl { get; set; }
    }
}
