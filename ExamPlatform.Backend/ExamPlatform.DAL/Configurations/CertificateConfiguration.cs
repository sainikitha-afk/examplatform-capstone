using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ExamPlatform.Domain.Entities;

namespace ExamPlatform.DAL.Configurations
{
    public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
    {
        public void Configure(EntityTypeBuilder<Certificate> builder)
        {
            builder.HasKey(c => c.CertificateId);

            // Human-readable code is unique. Generated in service layer
            // (e.g., "CERT-2026-000042"). Bounded length, indexed unique.
            builder.Property(c => c.CertificateCode)
                   .IsRequired()
                   .HasMaxLength(30);
            builder.HasIndex(c => c.CertificateCode).IsUnique();

            // One certificate per (User, Exam) — business rule 27.
            builder.HasIndex(c => new { c.UserId, c.ExamId }).IsUnique();

            builder.Property(c => c.Score).HasPrecision(6, 2).IsRequired();
            builder.Property(c => c.IssuedAt).IsRequired();

            builder.Property(c => c.CertStatus)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(20);

            // ───────────── PREMIUM BRANDING (additive, snapshotted) ─────────────
            builder.Property(c => c.IsPremium).IsRequired().HasDefaultValue(false);
            builder.Property(c => c.InstituteName).HasMaxLength(150);
            builder.Property(c => c.InstituteLogoUrl).HasMaxLength(500);

            builder.HasOne(c => c.User)
                   .WithMany(u => u.Certificates)
                   .HasForeignKey(c => c.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Exam)
                   .WithMany(e => e.Certificates)
                   .HasForeignKey(c => c.ExamId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ───────────── 1-to-1 RELATIONSHIP ─────────────
            // Certificate ↔ Attempt is one-to-one: each certificate is tied
            // to exactly one specific attempt (the best-scoring passing one),
            // and each attempt has at most one certificate.
            //
            // HasOne(c => c.Attempt).WithOne(a => a.Certificate) declares 1-1.
            // HasForeignKey<Certificate>(c => c.AttemptId) places the FK on
            // the Certificate side (the dependent), making it the side that
            // points to the principal (Attempt).
            //
            // Restrict — we never delete attempts in production; even if we
            // did in dev, deleting an attempt with a certificate should fail.
            builder.HasOne(c => c.Attempt)
                   .WithOne(a => a.Certificate)
                   .HasForeignKey<Certificate>(c => c.AttemptId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}