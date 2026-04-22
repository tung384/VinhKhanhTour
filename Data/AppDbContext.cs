using Microsoft.EntityFrameworkCore;
using OneSBackend.Models;

namespace OneSBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<POI> POIs => Set<POI>();
    public DbSet<POITranslation> POITranslations => Set<POITranslation>();
    public DbSet<POIImage> POIImages => Set<POIImage>();
    public DbSet<ContentVersion> ContentVersions => Set<ContentVersion>();
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<POI>()
            .HasMany(p => p.Translations)
            .WithOne(t => t.POI!)
            .HasForeignKey(t => t.POIId);

        modelBuilder.Entity<POI>()
            .HasMany(p => p.Images)
            .WithOne(i => i.POI!)
            .HasForeignKey(i => i.POIId);

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.Username)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .HasIndex(a => a.PoiId)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .HasOne(a => a.Poi)
            .WithMany()
            .HasForeignKey(a => a.PoiId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
