namespace ExamPlatform.Domain.Enums
{
    // The lifecycle states of an Exam. From Phase 2 / business rules,
    // an exam moves one-way through these stages. Modeling them as an
    // enum lets the business layer write guard clauses like:
    //   if (exam.Status != ExamStatus.Published) throw ...
    // and the compiler ensures we never compare against a bogus state.
    public enum ExamStatus
    {
        Draft,            // Being built by the examiner. Questions can be added/edited freely.
        Published,        // Visible to candidates; question set is now locked.
        ResultsPublished, // Examiner has released results; certificates + notifications fired.
        Archived          // Retired from active listing; kept for historical/audit purposes.
    }
}