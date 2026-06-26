// The 'namespace' groups related types and mirrors the folder path.
// Convention: <ProjectName>.<FolderName>. This is how other layers will
// reference this type: "using ExamPlatform.Domain.Enums;"
namespace ExamPlatform.Domain.Enums
{
    // An 'enum' (enumeration) is a value type that defines a fixed,
    // named set of allowed constants. It exists to make INVALID STATES
    // UNREPRESENTABLE: a variable of type UserRole can ONLY ever hold
    // one of the three values below — the compiler guarantees it.
    //
    // WHY an enum instead of a plain string?
    //   - Type safety: user.Role = "Admni" (typo) won't compile as an enum.

    // HOW it's stored: by default EF Core would persist these as integers
    // (Admin=0, Examiner=1, Candidate=2). We will OVERRIDE that in the DAL
    // layer's Fluent API to store them as readable STRINGS ("Admin", etc.)
    // so the database table is human-readable and immune to reordering.
    public enum UserRole
    {
        // Each member is implicitly assigned an integer (0,1,2...) but we
        // never rely on those numbers because we store the NAME as a string.
        Admin,      // Platform governance: manages users, examiners, categories, revokes certificates.
        Examiner,   // Academic content: creates exams, questions, publishes exams & results.
        Candidate   // End user: attempts exams, views results, earns certificates.
    }
}