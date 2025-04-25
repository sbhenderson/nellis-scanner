using Microsoft.EntityFrameworkCore;
using NellisScanner.Core.Models;

namespace NellisScanner.Web.Data;

public class NellisScannerDbContext : DbContext
{
    public NellisScannerDbContext(DbContextOptions<NellisScannerDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();  // Use the ID from the API
            entity.Property(p => p.Title).HasMaxLength(500);
            entity.Property(p => p.InventoryNumber).HasMaxLength(50);
            entity.Property(p => p.RetailPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CurrentPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Notes).HasMaxLength(2000);
            
            // Use JSON serialization for complex properties
            entity.Property(p => p.Grade).HasColumnType("jsonb");
            entity.Property(p => p.Photos).HasColumnType("jsonb");
            entity.Property(p => p.Location).HasColumnType("jsonb");
        });

        modelBuilder.Entity<PriceHistory>(entity =>
        {
            entity.HasKey(ph => ph.Id);
            entity.Property(ph => ph.Id).UseIdentityColumn();
            entity.Property(ph => ph.Price).HasColumnType("decimal(18,2)");
            
            // Relationship
            entity.HasOne(ph => ph.Product)
                .WithMany(p => p.PriceHistory)
                .HasForeignKey(ph => ph.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Create an index on product ID and timestamp
            entity.HasIndex(ph => new { ph.ProductId, ph.RecordedAt });
        });
    }
}