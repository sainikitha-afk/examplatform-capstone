using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    // Fluent API configuration for the ExaminerRequest entity.
    // Discovered automatically via ApplyConfigurationsFromAssembly in OnModelCreating.
    public class ExaminerRequestConfiguration : IEntityTypeConfiguration<ExaminerRequest>
    {
        public void Configure(EntityTypeBuilder<ExaminerRequest> builder)
        {
            // ───────────── PRIMARY KEY ─────────────
            builder.HasKey(r => r.ExaminerRequestId);

            // ───────────── COLUMN CONSTRAINTS ─────────────
            builder.Property(r => r.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(r => r.Email)
                   .IsRequired()
                   .HasMaxLength(150);

            // HasIndex without IsUnique — the same email CAN submit a new
            // request after a rejection; uniqueness is enforced at the service
            // layer (no Pending/Approved request for this email). The index
            // still makes the HasActiveRequestAsync query O(log n).
            builder.HasIndex(r => r.Email);

            builder.Property(r => r.Purpose)
                   .IsRequired()
                   .HasMaxLength(1000);

            // Store the enum as its string name ("Pending", "Approved", "Rejected")
            // for human-readable database rows.
            builder.Property(r => r.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(Domain.Enums.ExaminerRequestStatus.Pending);

            builder.Property(r => r.RequestedAt)
                   .IsRequired();

            // ProcessedAt is nullable — null while the request is Pending.
            builder.Property(r => r.ProcessedAt)
                   .IsRequired(false);

            // AdminNote is optional — only set on rejection or if admin wants
            // to attach a comment.
            builder.Property(r => r.AdminNote)
                   .HasMaxLength(500)
                   .IsRequired(false);
        }
    }
}
