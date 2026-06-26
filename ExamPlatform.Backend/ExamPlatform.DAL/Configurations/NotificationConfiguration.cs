using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.NotificationId);

            builder.Property(n => n.NotifType)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(30);

            builder.Property(n => n.Message)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(n => n.IsRead)
                   .IsRequired()
                   .HasDefaultValue(false);

            builder.Property(n => n.CreatedAt).IsRequired();

            // User → Notification: composition. If a user is ever hard-deleted
            // (very unusual; we deactivate), notifications go with them.
            // Cascade is appropriate since notifications without a recipient
            // are meaningless.
            builder.HasOne(n => n.User)
                   .WithMany(u => u.Notifications)
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Exam → Notification: OPTIONAL relationship (RelatedExamId is
            // nullable). Restrict — don't break notifications when an exam
            // is archived/removed in dev.
            builder.HasOne(n => n.RelatedExam)
                   .WithMany(e => e.Notifications)
                   .HasForeignKey(n => n.RelatedExamId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}