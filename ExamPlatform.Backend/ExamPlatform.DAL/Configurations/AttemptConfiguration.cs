using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
    {
        public void Configure(EntityTypeBuilder<Attempt> builder)
        {
            builder.HasKey(a => a.AttemptId);

            builder.Property(a => a.StartedAt).IsRequired();

            // SubmittedAt left as not-required because it's DateTime? — null
            // while attempt is InProgress. EF Core infers IsRequired from
            // nullability annotations, so we don't need to call it here.

            builder.Property(a => a.AttemptStatus)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            // Partial unique index: at most one InProgress attempt per (user, exam).
            // Multiple attempts are allowed overall (candidates may retake), but only
            // one can be active at a time. A filtered index prevents the race condition
            // where two concurrent POST /api/attempts/start requests both see null in
            // GetInProgressAsync and both insert — the DB enforces the constraint even
            // if the service-layer check is bypassed by a concurrent request.
            // HasFilter uses the snake_case column name produced by OnModelCreating.
            builder.HasIndex(a => new { a.UserId, a.ExamId })
                   .IsUnique()
                   .HasFilter("attempt_status = 'InProgress'")
                   .HasDatabaseName("uix_attempts_user_exam_inprogress");

            // Nullable decimal — score filled on evaluation. Precision applies
            // either way.
            builder.Property(a => a.Score).HasPrecision(6, 2);

            // Integrity counter — defaults to 0 so existing rows backfill cleanly.
            builder.Property(a => a.TabSwitchCount)
                   .IsRequired()
                   .HasDefaultValue(0);

            // User → Attempt: Restrict. Don't allow deleting a candidate who
            // has attempts. Their history must persist (deactivate instead).
            builder.HasOne(a => a.User)
                   .WithMany(u => u.Attempts)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Exam → Attempt: also Restrict. Don't allow deleting an exam
            // that has attempts; archive it instead. Preserves historical
            // integrity for analytics and certificates.
            builder.HasOne(a => a.Exam)
                   .WithMany(e => e.Attempts)
                   .HasForeignKey(a => a.ExamId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}