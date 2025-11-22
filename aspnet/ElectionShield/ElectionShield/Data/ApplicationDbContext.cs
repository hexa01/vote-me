using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ElectionShield.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ElectionShield.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Report> Reports { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<AdminVerification> AdminVerifications { get; set; }
        public DbSet<AIAnalysis> AIAnalyses { get; set; }

        public DbSet<Manifesto> Manifestos { get; set; } = default;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Report configuration
            builder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(r => r.Description)
                    .IsRequired()
                    .HasMaxLength(4000);
                entity.Property(r => r.Location)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(r => r.ReportCode)
                    .IsRequired()
                    .HasMaxLength(10);
                entity.Property(r => r.Category)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(r => r.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                // Indexes
                entity.HasIndex(r => r.ReportCode).IsUnique();
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => r.Category);

                // Relationships
                entity.HasMany(r => r.MediaFiles)
                      .WithOne(m => m.Report)
                      .HasForeignKey(m => m.ReportId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Verification)
                      .WithOne(v => v.Report)
                      .HasForeignKey<AdminVerification>(v => v.ReportId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.AIAnalysis)
                      .WithOne(a => a.Report)
                      .HasForeignKey<AIAnalysis>(a => a.ReportId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(r => r.AiTag)
                      .HasMaxLength(200);
            });

            // MediaFile configuration
            builder.Entity<MediaFile>(entity =>
            {
                entity.HasKey(m => m.Id);
                entity.Property(m => m.FileName)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(m => m.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);
                entity.Property(m => m.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(m => m.Type)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasIndex(m => m.ReportId);
                entity.HasIndex(m => m.Type);
            });

            // AdminVerification configuration
            builder.Entity<AdminVerification>(entity =>
            {
                entity.HasKey(v => v.Id);
                entity.Property(v => v.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.Property(v => v.Comments)
                    .HasMaxLength(1000);

                entity.HasIndex(v => v.ReportId).IsUnique();
                entity.HasIndex(v => v.AdminUserId);
                entity.HasIndex(v => v.Status);
                entity.HasIndex(v => v.VerifiedAt);

                // Relationships
                entity.HasOne(v => v.AdminUser)
                      .WithMany(u => u.Verifications)
                      .HasForeignKey(v => v.AdminUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // AIAnalysis configuration
            builder.Entity<AIAnalysis>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.AnalysisType)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(a => a.ConfidenceScore)
                    .HasPrecision(5, 2);
                entity.Property(a => a.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);
                entity.Property(a => a.Flags)
                    .HasMaxLength(1000);

                entity.HasIndex(a => a.ReportId).IsUnique();
                entity.HasIndex(a => a.Status);
                entity.HasIndex(a => a.AnalyzedAt);
            });

            // ApplicationUser configuration
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FullName)
                    .HasMaxLength(100);
                entity.Property(u => u.PhoneNumber)
                    .HasMaxLength(15);

                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.PhoneNumber).IsUnique();
            });
        }
    }
}