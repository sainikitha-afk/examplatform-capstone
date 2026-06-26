using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
    {
        public void Configure(EntityTypeBuilder<ExamQuestion> builder)
        {
            builder.HasKey(eq => eq.ExamQuestionId);

            builder.Property(eq => eq.QuestionOrder).IsRequired();

            // ───────────── COMPOSITE UNIQUE INDEX ─────────────
            // From Phase 2: the same question cannot appear twice in the same
            // exam. We enforce this with a UNIQUE INDEX on the PAIR
            // (ExamId, QuestionId). PostgreSQL treats the combination as
            // unique — two rows with the same ExamId but different QuestionId
            // are fine; two rows with both values equal are rejected.
            builder.HasIndex(eq => new { eq.ExamId, eq.QuestionId }).IsUnique();

            // Exam → ExamQuestion: composition. Removing an exam removes its
            // question mappings (the questions themselves stay; only the
            // bridge rows disappear). Cascade is correct here.
            builder.HasOne(eq => eq.Exam)
                   .WithMany(e => e.ExamQuestions)
                   .HasForeignKey(eq => eq.ExamId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Question → ExamQuestion: Restrict. A question used in an exam
            // can't be deleted out from under the bridge. The system relies
            // on soft-delete via IsActive instead.
            builder.HasOne(eq => eq.Question)
                   .WithMany(q => q.ExamQuestions)
                   .HasForeignKey(eq => eq.QuestionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}