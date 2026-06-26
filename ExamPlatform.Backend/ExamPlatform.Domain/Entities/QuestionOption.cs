namespace ExamPlatform.Domain.Entities
{
    // Maps to "question_options". A MASTER table. Each MCQ question has
    // multiple options, exactly one flagged correct (enforced in business
    // logic, since SQL can't easily enforce "exactly one true per group").
    public class QuestionOption
    {
        public int OptionId { get; set; }                    // PK

        public int QuestionId { get; set; }                  // FK → Question

        public string OptionText { get; set; } = null!;

        // The flag that marks the correct answer. Evaluation compares the
        // candidate's selected option's id against the option where IsCorrect
        // is true.
        public bool IsCorrect { get; set; }

        // ───────────── NAVIGATION ─────────────
        public Question Question { get; set; } = null!;      // parent (the "one")

        // Reverse: this option may be the selected answer in many attempts.
        public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();
    }
}