using Microsoft.EntityFrameworkCore;

namespace ClaimRequest.DAL.Data.Entities
{
    public class ClaimRequestDbContext : DbContext
    {
        // DbSet for ClaimChangeLog (you need to create this entity)
        public DbSet<ClaimChangeLog> ClaimChangeLogs { get; set; }

        public ClaimRequestDbContext(DbContextOptions<ClaimRequestDbContext> options)
            : base(options)
        {
        }

        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectStaff> ProjectStaffs { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimApprover> ClaimApprovers { get; set; }
        public DbSet<Otp> Otps { get; set; }
        public DbSet<RefreshTokens> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("ClaimRequest"); // Chi dinh Schema mac dinh cho DbContext
            base.OnModelCreating(modelBuilder);

            // Configure enum conversions to store their string representations.
            modelBuilder.Entity<Staff>()
                .Property(s => s.SystemRole)
                .HasConversion<string>();

            modelBuilder.Entity<Staff>()
                .Property(s => s.Department)
                .HasConversion<string>();

            modelBuilder.Entity<Project>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ProjectStaff>()
                .Property(ps => ps.ProjectRole)
                .HasConversion<string>();

            modelBuilder.Entity<Claim>()
                .Property(c => c.Status)
                .HasConversion<string>(); // e.g., "Draft" will be stored as "Draft"

            modelBuilder.Entity<Claim>()
                .Property(c => c.ClaimType)
                .HasConversion<string>(); // e.g., "Travel" will be stored as "Travel"

            // Configure the relationship for Claim.Claimer
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Claimer)
                .WithMany()
                .HasForeignKey(c => c.ClaimerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship for Claim.Finance
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Finance)
                .WithMany()
                .HasForeignKey(c => c.FinanceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure the relationship for Claim.Project
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Project)
                .WithMany(p => p.Claims)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure the relationship for ClaimApprover (explicit join entity)
            modelBuilder.Entity<ClaimApprover>()
                .HasKey(ca => new { ca.ClaimId, ca.ApproverId });

            // Configure ClaimChangeLog relationships
            modelBuilder.Entity<ClaimChangeLog>()
                .HasOne(cl => cl.Claim)
                .WithMany(c => c.ChangeHistory)
                .HasForeignKey(cl => cl.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            modelBuilder.Entity<Claim>()
                .Property(c => c.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Claim>()
                .Property(c => c.TotalWorkingHours)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Project>()
                .Property(p => p.Budget)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Staff>()
                .Property(s => s.Salary)
                .HasColumnType("decimal(18,2)");

            // Configure DateOnly conversions for Claim
            modelBuilder.Entity<Claim>()
                .Property(c => c.StartDate)
                .HasColumnType("date")
                .HasConversion(
                    dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    dateTime => DateOnly.FromDateTime(dateTime)
                );

            modelBuilder.Entity<Claim>()
                .Property(c => c.EndDate)
                .HasColumnType("date")
                .HasConversion(
                    dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    dateTime => DateOnly.FromDateTime(dateTime)
                );

            // Configure DateOnly conversions for Project
            modelBuilder.Entity<Project>()
                .Property(p => p.StartDate)
                .HasColumnType("date")
                .HasConversion(
                    dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    dateTime => DateOnly.FromDateTime(dateTime)
                );

            modelBuilder.Entity<Project>()
                .Property(p => p.EndDate)
                .HasColumnType("date")
                .HasConversion<DateTime?>(
                    dateOnly => dateOnly.HasValue ? dateOnly.Value.ToDateTime(TimeOnly.MinValue) : null,
                    dateTime => dateTime.HasValue ? DateOnly.FromDateTime(dateTime.Value) : null
                );

            // Configure Otp entity to use Redis schema
            modelBuilder.Entity<Otp>()
                .ToTable("Otps", "redis");

            modelBuilder.Entity<RefreshTokens>()
                .ToTable("RefreshTokens", "redis");

            modelBuilder.Entity<RefreshTokens>()
                .HasOne(rt => rt.Staff)
                .WithMany(s => s.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .HasPrincipalKey(s => s.Id);
        }

        public override int SaveChanges()
        {
            var auditEntries = new List<ClaimChangeLog>();
            // Replace this with an injected current user service if available
            var currentUser = "system";
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Claim>())
            {
                if (entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties)
                    {
                        // Check if the property value has actually changed
                        if (!object.Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntries.Add(new ClaimChangeLog
                            {
                                HistoryId = Guid.NewGuid(),
                                ClaimId = entry.Entity.Id,
                                FieldChanged = property.Metadata.Name,
                                OldValue = property.OriginalValue?.ToString(),
                                NewValue = property.CurrentValue?.ToString(),
                                ChangedAt = now,
                                ChangedBy = currentUser
                            });
                        }
                    }
                }
            }

            // Save the changes to your Claim (and other entities)
            var result = base.SaveChanges();

            // If there are any audit entries, save them as well
            if (auditEntries.Any())
            {
                ClaimChangeLogs.AddRange(auditEntries);
                base.SaveChanges();
            }

            return result;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditEntries = new List<ClaimChangeLog>();
            var currentUser = "system";
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Claim>())
            {
                if (entry.State == EntityState.Modified)
                {
                    foreach (var property in entry.Properties)
                    {
                        // Check if the property value has actually changed
                        if (!object.Equals(property.OriginalValue, property.CurrentValue))
                        {
                            auditEntries.Add(new ClaimChangeLog
                            {
                                HistoryId = Guid.NewGuid(),
                                ClaimId = entry.Entity.Id,
                                FieldChanged = property.Metadata.Name,
                                OldValue = property.OriginalValue?.ToString(),
                                NewValue = property.CurrentValue?.ToString(),
                                ChangedAt = now,
                                ChangedBy = currentUser
                            });
                        }
                    }
                }
            }

            // Save the changes to your Claim (and other entities)
            var result = await base.SaveChangesAsync(cancellationToken);

            // If there are any audit entries, save them as well
            if (auditEntries.Any())
            {
                ClaimChangeLogs.AddRange(auditEntries);
                await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }
    }
}
