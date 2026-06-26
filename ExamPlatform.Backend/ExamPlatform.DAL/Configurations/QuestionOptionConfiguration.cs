using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
    {
        public void Configure(EntityTypeBuilder<QuestionOption> builder)
        {
            builder.HasKey(o => o.OptionId);

            builder.Property(o => o.OptionText)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(o => o.IsCorrect)
                   .IsRequired();

            // Question → QuestionOption is composition (options belong to the
            // question). If a question is hard-deleted (rare; we soft-delete),
            // its options should go with it. Hence Cascade here, unlike the
            // protective Restrict elsewhere.
            builder.HasOne(o => o.Question)
                   .WithMany(q => q.Options)
                   .HasForeignKey(o => o.QuestionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}