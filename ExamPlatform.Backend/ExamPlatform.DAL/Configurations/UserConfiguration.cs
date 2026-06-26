// Microsoft.EntityFrameworkCore.Metadata.Builders contains EntityTypeBuilder<T>,
// the object we use to configure one entity.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.DAL.Configurations
{
    // IEntityTypeConfiguration<User> is the EF Core contract:
    // "I configure the User entity."
    // When OnModelCreating calls ApplyConfigurationsFromAssembly, EF Core
    // discovers this class by interface and runs Configure() automatically.
    // This is the "open-closed" pattern in action: open for extension (add new
    // configs without modifying the context), closed for modification.
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // ───────────── PRIMARY KEY ─────────────
            // EF Core convention would already detect UserId as PK because of
            // the naming pattern (<EntityName>Id). We declare it EXPLICITLY
            // anyway for two reasons:
            //   1. Self-documenting — anyone reading this file sees the PK at top.
            //   2. Safety — if we ever rename or restructure, the explicit
            //      declaration survives the convention change.
            builder.HasKey(u => u.UserId);

            // ───────────── COLUMN CONSTRAINTS ─────────────
            // Username: required, max 50 chars, UNIQUE.
            // IsRequired() → NOT NULL in SQL.
            // HasMaxLength(50) → varchar(50) instead of unbounded text.
            //   Why limit lengths? Two reasons:
            //   (a) Database optimization — fixed/bounded sizes help indexing.
            //   (b) Validation guardrail — DB rejects pathological 10MB usernames.
            builder.Property(u => u.Username)
                   .IsRequired()
                   .HasMaxLength(50);

            // HasIndex().IsUnique() creates a UNIQUE INDEX. Two effects:
            //   1. Constraint: PostgreSQL rejects INSERT/UPDATE causing duplicate.
            //   2. Performance: lookups by username become O(log n) via the index.
            // This is how we enforce business rule 23 (unique email/username).
            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique();

            // Hash and salt are long strings; bounded but generous.
            // BCrypt hashes are ~60 chars; we give 255 for safety/future algorithms.
            builder.Property(u => u.PasswordHash)
                   .IsRequired()
                   .HasMaxLength(255);

            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            // ───────────── ENUM-AS-STRING ─────────────
            // This is the single most important Fluent API call for enums.
            // Without this, EF Core stores Role as integer (0/1/2). With it,
            // EF Core stores the enum's NAME ("Admin", "Examiner", "Candidate").
            //
            // HasConversion<string>() registers a value converter that EF Core
            // applies transparently:
            //   - On save: enum.ToString() → "Admin"
            //   - On read: Enum.Parse → UserRole.Admin
            //
            // We also bound the column length — even though "Candidate" is
            // only 9 chars, we allow 20 for future role additions.
            builder.Property(u => u.Role)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(u => u.DateOfJoining)
                   .IsRequired();

            // HasDefaultValue(true) means: when an INSERT doesn't specify
            // IsActive, PostgreSQL defaults it to true. Every new user is
            // active unless explicitly set otherwise.
            builder.Property(u => u.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            // ───────────── NOTE ON RELATIONSHIPS ─────────────
            // We do NOT configure relationships HERE. By convention, EF Core
            // already discovered all five collections on User (Questions, Exams,
            // Attempts, Certificates, Notifications) by matching navigation
            // properties on the OTHER side back to User. We'll handle each
            // relationship inside the CHILD's configuration (e.g., Question
            // configures its FK to User), which keeps each relationship
            // configured in exactly one place — Single Responsibility.
        }
    }
}