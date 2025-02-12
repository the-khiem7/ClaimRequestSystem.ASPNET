using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ClaimRequest.DAL.Data.Entities
{
    public class ClaimRequestDbContext : DbContext
    {
        public ClaimRequestDbContext(DbContextOptions<ClaimRequestDbContext> options)
            : base(options)
        {
        }

        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectStaff> ProjectStaffs { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimApprover> ClaimApprovers { get; set; }  // New DbSet

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure enum conversions to store their string representations.
            modelBuilder.Entity<Staff>()
                .Property(s => s.Position)
                .HasConversion<string>();

            modelBuilder.Entity<Project>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ProjectStaff>()
                .Property(ps => ps.ProjectRole)
                .HasConversion<string>();

            modelBuilder.Entity<Claim>()
                .Property(c => c.ClaimType)
                .HasConversion<string>();

            modelBuilder.Entity<Claim>()
                .Property(c => c.Status)
                .HasConversion<string>();

            // Configure the relationship for Claim.Claimer
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Claimer)
                .WithMany() // No inverse navigation property on Staff for this relationship
                .HasForeignKey(c => c.ClaimerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship for Claim.Finance
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Finance)
                .WithMany() // No inverse navigation property on Staff for this relationship
                .HasForeignKey(c => c.FinanceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship for Claim.Project
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Claims) // Assuming you want a collection of Claims in Project
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // If you have an explicit join entity for claim approvers, configure it separately
            modelBuilder.Entity<ClaimApprover>()
                .HasKey(ca => new { ca.ClaimId, ca.ApproverId });
        }
    }
}
