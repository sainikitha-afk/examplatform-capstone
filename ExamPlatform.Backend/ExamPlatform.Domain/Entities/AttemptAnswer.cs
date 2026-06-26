namespace ExamPlatform.Domain.Entities
{
    // Maps to "attempt_answers". TRANSACTIONAL — one row per question per
    // attempt. Stores what the candidate picked and (after evaluation) whether
    // it was correct and how many marks were awarded.
    public class AttemptAnswer
    {
        public int AnswerId { get; set; }                     // PK

        public int AttemptId { get; set; }                    // FK → Attempt
        public int QuestionId { get; set; }                   // FK → Question

        // NULLABLE FK: if the candidate skipped a question, no option was
        // selected. Null means "skipped" — distinct from "answered incorrectly."
        // This nullable FK is one of the most important nullability decisions
        // in the schema: it lets us tell "didn't answer" apart from "got wrong."
        public int? SelectedOptionId { get; set; }

        // Nullable: filled during evaluation on submit. Null means not yet
        // evaluated (in-progress attempts).
        public bool? IsCorrect { get; set; }
        public decimal? MarksAwarded { get; set; }

        // When this specific answer was saved. Updated on every change since
        // we auto-save answers as the user clicks (NFR2 / business rule 12).
        public DateTime AnsweredAt { get; set; }

        // ───────────── NAVIGATION ─────────────
        public Attempt Attempt { get; set; } = null!;
        public Question Question { get; set; } = null!;

        // Nullable navigation because SelectedOptionId is nullable. When
        // the candidate skips, both the FK and the navigation are null.
        public QuestionOption? SelectedOption { get; set; }
    }
}