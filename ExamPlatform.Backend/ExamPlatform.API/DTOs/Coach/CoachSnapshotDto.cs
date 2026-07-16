namespace ExamPlatform.API.DTOs.Coach
{
    // Response for GET /api/coach/snapshot — the candidate's deterministic performance
    // summary. No AI call is involved, so the UI can render its welcome state, its no-data
    // state, and the "here's what I already know about you" panel without spending a token.
    public class CoachSnapshotDto
    {
        public string FirstName { get; set; } = string.Empty;

        // False when the candidate has no published results yet. The UI shows the "complete an
        // exam and wait for results" state and hides the ask box's performance framing.
        public bool HasData { get; set; }

        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
        public double PassRate { get; set; }
        public int CertificatesEarned { get; set; }

        // Every topic the backend computed, strongest first.
        public List<CoachTopicDto> Topics { get; set; } = new();

        public List<CoachTopicDto> StrongTopics { get; set; } = new();
        public List<CoachFocusAreaDto> FocusAreas { get; set; } = new();
    }
}
