namespace ExamPlatform.Domain.Entities
{
    // Maps to "exam_questions". The N-N BRIDGE between Exam and Question.
    // Each row says: "this exam includes this question at this position."
    // Made an explicit entity (not implicit skip-navigation) because we carry
    // the QuestionOrder column — extra data the bridge needs to hold.
    public class ExamQuestion
    {
        public int ExamQuestionId { get; set; }               // PK (synthetic)

        // The two FKs that form the bridge. From Phase 2, we'll add a UNIQUE
        // constraint on (ExamId, QuestionId) in DAL — same question can't be
        // added twice to the same exam.
        public int ExamId { get; set; }
        public int QuestionId { get; set; }

        // Position of this question within the exam (1, 2, 3...). Lets the
        // examiner control display order independent of insertion order.
        public int QuestionOrder { get; set; }

        // ───────────── NAVIGATION ─────────────
        // A bridge entity has exactly TWO navigation references — one to each
        // side of the N-N relationship. No collections (a bridge row links one
        // exam to one question; there's no "many" side from here).
        public Exam Exam { get; set; } = null!;
        public Question Question { get; set; } = null!;
    }
}