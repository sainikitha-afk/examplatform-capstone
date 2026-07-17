using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
    {
        public void Configure(EntityTypeBuilder<Coupon> builder)
        {
            builder.HasKey(c => c.CouponId);

            builder.Property(c => c.Code).IsRequired().HasMaxLength(30);
            builder.HasIndex(c => c.Code).IsUnique();

            builder.Property(c => c.DiscountPercent).IsRequired();
            builder.Property(c => c.MaxRedemptions);
            builder.Property(c => c.TimesRedeemed).IsRequired().HasDefaultValue(0);

            builder.Property(c => c.ValidFrom).IsRequired();
            builder.Property(c => c.ValidTo).IsRequired();
            builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);

            builder.Property(c => c.AppliesTo)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);
        }
    }
}
