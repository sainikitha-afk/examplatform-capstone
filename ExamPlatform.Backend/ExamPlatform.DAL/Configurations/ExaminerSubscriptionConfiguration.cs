using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class ExaminerSubscriptionConfiguration : IEntityTypeConfiguration<ExaminerSubscription>
    {
        public void Configure(EntityTypeBuilder<ExaminerSubscription> builder)
        {
            builder.HasKey(s => s.SubscriptionId);

            // One subscription per examiner — the DB-level guard behind
            // "trial abuse" prevention (Part 9): there is no path to a second row.
            builder.HasIndex(s => s.UserId).IsUnique();

            builder.Property(s => s.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(s => s.TrialExamsUsed).IsRequired().HasDefaultValue(0);
            builder.Property(s => s.UpgradeDeclineCount).IsRequired().HasDefaultValue(0);

            builder.Property(s => s.SubscriptionStartDate).IsRequired();
            builder.Property(s => s.AutoRenew).IsRequired().HasDefaultValue(false);

            builder.Property(s => s.DiscountAppliedPercent);
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.UpdatedAt).IsRequired();

            builder.HasOne(s => s.User)
                   .WithOne(u => u.ExaminerSubscription)
                   .HasForeignKey<ExaminerSubscription>(s => s.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Plan)
                   .WithMany(p => p.Subscriptions)
                   .HasForeignKey(s => s.PlanId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
