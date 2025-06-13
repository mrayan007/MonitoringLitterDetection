using Microsoft.EntityFrameworkCore;
using MonitoringApi.Models;

namespace MonitoringApi.Data
{
    public class MonitoringContext : DbContext
    {
        public MonitoringContext(DbContextOptions<MonitoringContext> options) : base(options) { }

        public DbSet<Litter> Litter { get; set; }
        public DbSet<EnrichedLitter> EnrichedLitter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
