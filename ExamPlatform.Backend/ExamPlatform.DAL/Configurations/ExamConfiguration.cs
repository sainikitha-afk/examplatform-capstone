using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class ExamConfiguration : IEntityTypeConfiguration<Exam>
    {
        public void Configure(EntityTypeBuilder<Exam> builder)
        {
            builder.HasKey(e => e.ExamId);

            builder.Property(e => e.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(e => e.Instructions)
                   .HasMaxLength(2000);

            builder.Property(e => e.DurationMinutes).IsRequired();

            // Same precision discipline as Question.Marks.
            builder.Property(e => e.TotalMarks).HasPrecision(6, 2);
            builder.Property(e => e.PassingMarks).HasPrecision(6, 2);

            builder.Property(e => e.StartTime).IsRequired();
            builder.Property(e => e.EndTime).IsRequired();

            // ExamStatus enum stored as string — same pattern as UserRole.
            builder.Property(e => e.ExamStatus)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(30);

            builder.Property(e => e.CreatedAt).IsRequired();

            // ───────────── SUBSCRIPTION & PAYMENT (additive) ─────────────
            builder.Property(e => e.IsPremium).IsRequired().HasDefaultValue(false);
            builder.Property(e => e.Price).HasPrecision(10, 2);
            builder.Property(e => e.IsFeatured).IsRequired().HasDefaultValue(false);

            // Relationships — both Restrict. Don't delete a user who authored
            // exams; don't delete a category that has exams.
            builder.HasOne(e => e.Examiner)
                   .WithMany(u => u.Exams)
                   .HasForeignKey(e => e.ExaminerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(e => e.Category)
                   .WithMany(c => c.Exams)
                   .HasForeignKey(e => e.CatId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}