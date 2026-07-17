using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.HasKey(p => p.PaymentId);

            builder.Property(p => p.Purpose)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(30);

            builder.Property(p => p.Amount).HasPrecision(10, 2).IsRequired();
            builder.Property(p => p.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("INR");

            builder.Property(p => p.Method)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(p => p.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(p => p.FakeTransactionId).IsRequired().HasMaxLength(50);
            builder.HasIndex(p => p.FakeTransactionId).IsUnique();

            builder.Property(p => p.FakeGatewayReference).IsRequired().HasMaxLength(30);

            // The double-payment / payment-replay guard (Part 9): a retried
            // submission with the same key can never insert a second row.
            builder.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.IdempotencyKey).IsUnique();

            builder.Property(p => p.InitiatedAt).IsRequired();
            builder.Property(p => p.FailureReason).HasMaxLength(200);

            builder.HasOne(p => p.User)
                   .WithMany(u => u.Payments)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Subscription)
                   .WithMany(s => s.Payments)
                   .HasForeignKey(p => p.SubscriptionId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Coupon)
                   .WithMany(c => c.Payments)
                   .HasForeignKey(p => p.CouponId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
