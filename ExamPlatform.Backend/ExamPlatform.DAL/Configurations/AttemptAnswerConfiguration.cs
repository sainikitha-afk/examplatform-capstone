using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
    {
        public void Configure(EntityTypeBuilder<AttemptAnswer> builder)
        {
            builder.HasKey(aa => aa.AnswerId);

            // One answer row per question per attempt. Composite uniqueness
            // prevents accidental duplicate saves (idempotency for auto-save).
            builder.HasIndex(aa => new { aa.AttemptId, aa.QuestionId }).IsUnique();

            builder.Property(aa => aa.MarksAwarded).HasPrecision(5, 2);
            builder.Property(aa => aa.AnsweredAt).IsRequired();

            // Attempt → AttemptAnswer: composition. Delete an attempt (rare;
            // would only happen in dev/test), its answers go too. Cascade.
            builder.HasOne(aa => aa.Attempt)
                   .WithMany(a => a.AttemptAnswers)
                   .HasForeignKey(aa => aa.AttemptId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Question → AttemptAnswer: Restrict. A question referenced by
            // any historical answer cannot be hard-deleted (preserves history).
            builder.HasOne(aa => aa.Question)
                   .WithMany(q => q.AttemptAnswers)
                   .HasForeignKey(aa => aa.QuestionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // QuestionOption → AttemptAnswer: this navigation is NULLABLE
            // (a candidate may have skipped the question; SelectedOptionId
            // is int?). Restrict here too — if a question is restructured,
            // its options aren't dropped from under historical answers.
            // EF Core requires IsRequired(false) be implicit from the int?
            // annotation; the relationship is automatically optional.
            builder.HasOne(aa => aa.SelectedOption)
                   .WithMany(o => o.AttemptAnswers)
                   .HasForeignKey(aa => aa.SelectedOptionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}