using Capstone.Identity.API.Models;
using Capstone.Identity.API.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Capstone.Identity.API.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

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
        }
    }
}
