using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.HasKey(p => p.PlanId);

            builder.Property(p => p.Name)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(p => p.Tier)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(p => p.PriceMonthly).HasPrecision(10, 2).IsRequired();
            builder.Property(p => p.PriceYearly).HasPrecision(10, 2).IsRequired();

            builder.Property(p => p.MaxExamsLifetime);
            builder.Property(p => p.ExamCooldownDays);

            builder.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);
        }
    }
}
