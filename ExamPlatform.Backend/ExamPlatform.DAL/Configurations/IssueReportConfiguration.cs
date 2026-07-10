using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class IssueReportConfiguration : IEntityTypeConfiguration<IssueReport>
    {
        public void Configure(EntityTypeBuilder<IssueReport> builder)
        {
            builder.HasKey(r => r.IssueReportId);

            builder.Property(r => r.Message)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(r => r.Status)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            builder.Property(r => r.CreatedAt).IsRequired();

            builder.Property(r => r.ResolutionNote)
                   .HasMaxLength(1000);

            // Speed up the examiner's "reports for my exam" query.
            builder.HasIndex(r => r.ExamId);

            // Candidate → IssueReport. Cascade: reports are meaningless without
            // the candidate. No inverse collection on User (keeps User untouched).
            builder.HasOne(r => r.Candidate)
                   .WithMany()
                   .HasForeignKey(r => r.CandidateId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Exam → IssueReport. Restrict so removing/archiving an exam in dev
            // doesn't silently wipe its audit trail.
            builder.HasOne(r => r.Exam)
                   .WithMany()
                   .HasForeignKey(r => r.ExamId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Question → IssueReport. Optional FK. Restrict — a flagged question
            // is exactly what an examiner needs to keep around to investigate.
            builder.HasOne(r => r.Question)
                   .WithMany()
                   .HasForeignKey(r => r.QuestionId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
