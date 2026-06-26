namespace ExamPlatform.Domain.Entities
{
    // Maps to "exam_categories". A MASTER table — reference data that rarely
    // changes (e.g., "Aptitude", "C# Programming"). Questions and Exams are
    // classified by category, enabling the search/filter feature (FR5.2).
    public class ExamCategory
    {
        public int CatId { get; set; }                       // PK

        public string CatName { get; set; } = null!;         // unique (configured in DAL)

        // Nullable: description is optional. string? signals "may be null",
        // mapping to a nullable DB column.
        public string? Description { get; set; }

        // ───────────── NAVIGATION ─────────────
        // One category → many questions, and one category → many exams.
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}