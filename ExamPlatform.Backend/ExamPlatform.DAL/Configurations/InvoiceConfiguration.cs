using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.InvoiceId);

            builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(30);
            builder.HasIndex(i => i.InvoiceNumber).IsUnique();

            builder.Property(i => i.FakeGstNumber).IsRequired().HasMaxLength(20);
            builder.Property(i => i.BillingName).IsRequired().HasMaxLength(100);
            builder.Property(i => i.BillingEmail).IsRequired().HasMaxLength(150);

            builder.Property(i => i.Subtotal).HasPrecision(10, 2).IsRequired();
            builder.Property(i => i.DiscountAmount).HasPrecision(10, 2).IsRequired();
            builder.Property(i => i.TaxAmount).HasPrecision(10, 2).IsRequired();
            builder.Property(i => i.Total).HasPrecision(10, 2).IsRequired();

            builder.Property(i => i.IssuedAt).IsRequired();

            // 1-to-1 with Payment — duplicate-invoice guard (Part 9).
            builder.HasOne(i => i.Payment)
                   .WithOne(p => p.Invoice)
                   .HasForeignKey<Invoice>(i => i.PaymentId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
