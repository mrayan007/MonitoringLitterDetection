using Microsoft.EntityFrameworkCore;
using MonitoringApi.Models;
using System;

namespace MonitoringApi.Data
{
    public class MonitoringContext : DbContext
    {
        public MonitoringContext(DbContextOptions<MonitoringContext> options) : base(options) { }

        public DbSet<Litter> Litter { get; set; }
        public DbSet<EnrichedLitter> EnrichedLitter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuratie voor Litter
            modelBuilder.Entity<Litter>()
                .Property(l => l.Id)
                .ValueGeneratedOnAdd(); // Zorgt ervoor dat de GUID wordt gegenereerd bij toevoegen

            // Configuratie voor de 1-op-1 relatie tussen Litter en EnrichedLitter
            modelBuilder.Entity<EnrichedLitter>()
                // De primaire sleutel van EnrichedLitter is de gedeelde 'Id' property
                .HasKey(el => el.Id);

            modelBuilder.Entity<EnrichedLitter>()
                // EnrichedLitter heeft één Litter (via de navigatie property 'Litter')
                .HasOne(el => el.Litter)
                // Litter heeft één EnrichedLitter (standaard).
                // Als je een navigatie property van Litter naar EnrichedLitter zou toevoegen,
                // zou je hier .WithOne(l => l.EnrichedLitter) kunnen zetten.
                // Zonder zo'n property blijft WithOne() zonder argument prima.
                .WithOne()
                // De foreign key die de relatie definieert, is de 'Id' property van EnrichedLitter
                // die tevens de primaire sleutel is.
                .HasForeignKey<EnrichedLitter>(el => el.Id)
                .IsRequired(); // Relatie is verplicht
        }
    }
}