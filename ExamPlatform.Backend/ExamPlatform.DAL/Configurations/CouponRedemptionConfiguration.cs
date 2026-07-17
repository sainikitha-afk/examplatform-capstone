using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
    {
        public void Configure(EntityTypeBuilder<CouponRedemption> builder)
        {
            builder.HasKey(r => r.RedemptionId);

            // Coupon-abuse guard (Part 9): one redemption per (coupon, user),
            // DB-enforced so two concurrent redemption attempts can't both win.
            builder.HasIndex(r => new { r.CouponId, r.UserId }).IsUnique();

            builder.Property(r => r.RedeemedAt).IsRequired();

            builder.HasOne(r => r.Coupon)
                   .WithMany(c => c.Redemptions)
                   .HasForeignKey(r => r.CouponId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Payment)
                   .WithMany()
                   .HasForeignKey(r => r.PaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
