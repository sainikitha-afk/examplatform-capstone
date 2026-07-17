using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
    {
        public void Configure(EntityTypeBuilder<Institution> builder)
        {
            builder.HasKey(i => i.InstitutionId);

            // One institution per examiner.
            builder.HasIndex(i => i.OwnerUserId).IsUnique();

            builder.Property(i => i.Name).IsRequired().HasMaxLength(150);
            builder.Property(i => i.LogoUrl).HasMaxLength(500);

            builder.Property(i => i.VerificationStatus)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(Domain.Enums.InstitutionVerificationStatus.Pending);

            builder.Property(i => i.RequestedAt).IsRequired();

            builder.HasOne(i => i.Owner)
                   .WithOne(u => u.Institution)
                   .HasForeignKey<Institution>(i => i.OwnerUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Admin who verified/rejected — no inverse collection on User,
            // kept as a shadow-only relationship (mirrors how ExaminerRequest
            // avoids adding every possible reverse nav to User).
            builder.HasOne(i => i.VerifiedByAdmin)
                   .WithMany()
                   .HasForeignKey(i => i.VerifiedByAdminUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
