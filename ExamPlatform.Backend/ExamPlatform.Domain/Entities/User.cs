// Pull in the enums we defined (UserRole lives here).
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.Domain.Entities
{
    // Maps to the "users" table from Phase 2.
    // This single entity represents ALL three actors (Admin/Examiner/Candidate);
    // the Role property distinguishes them. We collapsed three actors into one
    // table because they share the same core fields (login, name, status) and
    // differ only by role — a classic case of avoiding redundant tables.
    public class User
    {
        // PRIMARY KEY. By EF Core convention, a property named "Id" or
        // "<ClassName>Id" (UserId) is automatically treated as the PK.
        // int + identity means PostgreSQL auto-generates sequential values.
        public int UserId { get; set; }

        // "= null!;" is the null-forgiving operator. With nullable reference
        // types ON, the compiler would warn "this non-nullable string is never
        // initialized." We promise EF Core will always populate it on load,
        // so we suppress the warning. The DB column itself will be NOT NULL.
        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        // We store the HASH and SALT, never the raw password (security/NFR1).
        // Hashing is one-way: we can verify a password but never recover it.
        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        // Strongly-typed enum (not a string). Compile-time safety on roles.
        // Configured in DAL to persist as a readable string column.
        public UserRole Role { get; set; }

        // Non-nullable: a user always has a join date, set at registration.
        // Stored in UTC.
        public DateTime DateOfJoining { get; set; }

        // Soft-state flag. A deactivated user (false) cannot log in (business
        // rule 25). We never DELETE users — we deactivate, preserving history.
        public bool IsActive { get; set; }

        // ───────────── NAVIGATION PROPERTIES (not DB columns) ─────────────
        // These express relationships as object graphs. A User (as examiner)
        // authors many Questions and Exams; a User (as candidate) has many
        // Attempts, Certificates, Notifications. ICollection<T> is the standard
        // type for a "many" side. Initialized to empty lists so they're never
        // null when we add to them in code.

        public ICollection<Question> Questions { get; set; } = new List<Question>();      // authored (examiner)
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();                  // created (examiner)
        public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();         // taken (candidate)
        public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>(); // earned (candidate)
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>(); // received
    }
}