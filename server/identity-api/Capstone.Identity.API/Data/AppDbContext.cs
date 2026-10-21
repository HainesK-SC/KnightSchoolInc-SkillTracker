using Capstone.Identity.API.Models;
using Capstone.Identity.API.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Capstone.Identity.API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Discipline> Disciplines { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<ParticipantSkillProgress> ParticipantSkillProgresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserProfile>()
                .Property(p => p.DisplayName).HasMaxLength(100);

            builder.Entity<UserProfile>()
                .HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(p => p.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserProfile>()
                .Property(p => p.DisplayNameSalutation)
                .HasConversion<string>().HasMaxLength(50);

            builder.Entity<UserProfile>()
                .Property(p => p.DisplayNameModifier)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.NormalizedEmail)
                .HasDatabaseName("EmailIndex")
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL");

            // Discipline and Skill related configurations
            
            // Defined in the model - leaving for reference
            //builder.Entity<Discipline>()
            //    .HasIndex(d => d.Name)
            //    .IsUnique();

            builder.Entity<Skill>()
                .HasOne<Discipline>()
                .WithMany()
                .HasForeignKey(d => d.DisciplineId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ParticipantSkillProgress>()
                .HasOne<UserProfile>()
                .WithMany()
                .HasForeignKey(p => p.UserProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ParticipantSkillProgress>()
                .HasOne<Skill>()
                .WithMany()
                .HasForeignKey(k => k.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Defined in the model, leaving for reference
            //builder.Entity<ParticipantSkillProgress>()
            //    .HasIndex(p => new { p.UserProfileId, p.SkillId })
            //    .IsUnique();

            builder.Entity<ParticipantSkillProgress>()
                .Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<ParticipantSkillProgress>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_ParticipantSkillProgresses_Status_NotLocked",
                    "[Status] <> 'Locked'"));
        }
    }
}
