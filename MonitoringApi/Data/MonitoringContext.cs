using Microsoft.EntityFrameworkCore;
using MonitoringApi.Models;
using System;

namespace MonitoringApi.Data
{
    public class MonitoringContext : DbContext
    {
        public MonitoringContext(DbContextOptions<MonitoringContext> options) : base(options) { }

        public DbSet<Litter> Litter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}