namespace ExamPlatform.Domain.Entities
{
    // Maps to "questions". A MASTER table — questions live in a reusable bank
    // and can be added to many exams (via ExamQuestion). MCQ-only for now.
    public class Question
    {
        public int QuestionId { get; set; }                  // PK

        // ───────────── FOREIGN KEYS (real columns) ─────────────
        // The examiner (a User) who authored this question.
        public int ExaminerId { get; set; }
        // The category this question belongs to.
        public int CatId { get; set; }

        public string QuestionText { get; set; } = null!;

        // decimal (NOT double) for exact marks. A question worth 2.5 marks
        // must stay exactly 2.5.
        public decimal Marks { get; set; }

        // Stored as string in code for simplicity (Easy/Medium/Hard). Could be
        // an enum too; kept as string here since it's only used for filtering
        // and display, not for control-flow guard clauses. (Defensible either
        // way — be ready to explain the trade-off.)
        public string Difficulty { get; set; } = null!;

        public DateTime CreatedAt { get; set; }              // UTC

        // Soft-delete flag (business rule 21): a question used in a published
        // exam is never hard-deleted — we set IsActive=false to preserve the
        // integrity of historical results that reference it.
        public bool IsActive { get; set; }

        // ───────────── NAVIGATION ─────────────
        // The parent objects this question points TO (the "one" sides):
        public User Examiner { get; set; } = null!;          // matches ExaminerId
        public ExamCategory Category { get; set; } = null!;  // matches CatId

        // The children this question OWNS (the "many" sides):
        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
        public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
    }
}