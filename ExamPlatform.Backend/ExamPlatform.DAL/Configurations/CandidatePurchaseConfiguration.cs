using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class CandidatePurchaseConfiguration : IEntityTypeConfiguration<CandidatePurchase>
    {
        public void Configure(EntityTypeBuilder<CandidatePurchase> builder)
        {
            builder.HasKey(cp => cp.PurchaseId);

            // Duplicate-purchase guard (Part 9): one purchase per (candidate, exam),
            // enforced at the DB level so a concurrent double-click can't create two.
            builder.HasIndex(cp => new { cp.UserId, cp.ExamId }).IsUnique();

            builder.Property(cp => cp.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(cp => cp.PurchasedAt).IsRequired();

            builder.HasOne(cp => cp.User)
                   .WithMany(u => u.CandidatePurchases)
                   .HasForeignKey(cp => cp.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cp => cp.Exam)
                   .WithMany(e => e.CandidatePurchases)
                   .HasForeignKey(cp => cp.ExamId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 1-to-1 with Payment — duplicate-purchase-via-payment guard.
            builder.HasOne(cp => cp.Payment)
                   .WithOne(p => p.CandidatePurchase)
                   .HasForeignKey<CandidatePurchase>(cp => cp.PaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
