using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class ExamCategoryConfiguration : IEntityTypeConfiguration<ExamCategory>
    {
        public void Configure(EntityTypeBuilder<ExamCategory> builder)
        {
            builder.HasKey(c => c.CatId);

            builder.Property(c => c.CatName)
                   .IsRequired()
                   .HasMaxLength(100);

            // Category names must be unique — no two "Aptitude" categories.
            builder.HasIndex(c => c.CatName).IsUnique();

            // Description is nullable string?, so we don't mark IsRequired.
            // We still bound the length to keep the column sane.
            builder.Property(c => c.Description)
                   .HasMaxLength(500);
        }
    }
}