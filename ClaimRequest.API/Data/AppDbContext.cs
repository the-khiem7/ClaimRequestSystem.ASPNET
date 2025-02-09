using Microsoft.EntityFrameworkCore;
using ClaimRequest.API.Data.Entities;

namespace ClaimRequest.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public AppDbContext()
        {
        }

        public DbSet<Claim> ClaimRequests { get; set; }
        public DbSet<Staff> ClaimRequestItems { get; set; }
        public DbSet<Project> ClaimStatuses { get; set; }
    }
}
