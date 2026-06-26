using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class QuestionConfiguration : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> builder)
        {
            builder.HasKey(q => q.QuestionId);

            builder.Property(q => q.QuestionText)
                   .IsRequired()
                   .HasMaxLength(1000);  // generous; question text can be long

            // ───────────── DECIMAL PRECISION ─────────────
            // HasPrecision(p, s):
            //   p = total digits, s = digits after the decimal point.
            // HasPrecision(5, 2) means values like 999.99 max — perfect for marks.
            // Without this, PostgreSQL uses an unbounded "numeric" type, which
            // works but is less explicit about the expected range.
            builder.Property(q => q.Marks)
                   .IsRequired()
                   .HasPrecision(5, 2);

            builder.Property(q => q.Difficulty)
                   .IsRequired()
                   .HasMaxLength(10);   // "Easy"/"Medium"/"Hard"

            builder.Property(q => q.CreatedAt).IsRequired();

            builder.Property(q => q.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            // ───────────── RELATIONSHIP: Question → User (examiner) ─────────────
            // Read this fluently:
            //   "A Question HAS ONE Examiner; that Examiner has MANY Questions;
            //    the FK on the Question side is ExaminerId; on DELETE the
            //    parent, RESTRICT (don't allow deletion)."
            //
            // OnDelete(DeleteBehavior.Restrict):
            //   If someone tries to delete a User who has authored questions,
            //   PostgreSQL REJECTS the delete. We don't cascade — orphaning
            //   questions or accidentally wiping content is too dangerous.
            //   To delete an examiner, you'd first reassign their questions.
            //   We deactivate (IsActive=false) rather than delete anyway.
            builder.HasOne(q => q.Examiner)
                   .WithMany(u => u.Questions)
                   .HasForeignKey(q => q.ExaminerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ───────────── RELATIONSHIP: Question → ExamCategory ─────────────
            // Same pattern. Deleting a category that has questions is rejected.
            builder.HasOne(q => q.Category)
                   .WithMany(c => c.Questions)
                   .HasForeignKey(q => q.CatId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}