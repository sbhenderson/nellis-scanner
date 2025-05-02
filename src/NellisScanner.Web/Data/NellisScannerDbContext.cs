using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Core.Models;

namespace NellisScanner.Web.Data;

public class NellisScannerDbContext : DbContext
{
    public NellisScannerDbContext(DbContextOptions<NellisScannerDbContext> options) : base(options)
    {
    }

    public DbSet<AuctionItem> Auctions { get; set; } = null!;
    public DbSet<InventoryItem> Inventory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuctionItem>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();  // Use the ID from the API
            entity.Property(p => p.Title).HasMaxLength(500);
            entity.Property(p => p.InventoryNumber);
            entity.Property(p => p.RetailPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.CurrentPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.FinalPrice).HasColumnType("decimal(18,2)");
            entity.Property(p => p.State).HasConversion<string>();
            
            // Create an index on inventory number
            entity.HasIndex(p => p.InventoryNumber);
            entity.HasIndex(p => p.CloseTime); // Add index on close time for efficient queries
            
            // Relationship with Inventory
            entity.HasOne(a => a.Inventory)
                .WithMany(i => i.Auctions)
                .HasForeignKey(a => a.InventoryNumber)
                .HasPrincipalKey(i => i.InventoryNumber);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(i => i.InventoryNumber);
            entity.Property(i => i.Description).HasMaxLength(500);
            entity.Property(i => i.CategoryName).HasMaxLength(100);
        });
    }
}

/// <summary>
/// Represents a single auction tracked from Nellis Auction
/// </summary>
public class AuctionItem
{
    // Primary key - uses Nellis Auction's own ID
    public int Id { get; set; }
    
    // Auction details
    public string? Title { get; set; }
    public long InventoryNumber { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal CurrentPrice { get; set; } // Current price during auction
    public decimal FinalPrice { get; set; } // Final price when auction is closed
    public AuctionState State { get; set; }
    public DateTimeOffset OpenTime { get; set; }
    public DateTimeOffset CloseTime { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string? Location { get; set; }
    public int? BidCount { get; set; } // Track bid count
    
    // Navigation property
    public InventoryItem? Inventory { get; set; }
}

/// <summary>
/// Represents a specific inventory item that may appear in multiple auctions
/// </summary>
public class InventoryItem
{
    // Auto-generated ID
    
    // The inventory number used by Nellis Auction
    public long InventoryNumber { get; set; }
    
    // Product details
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
    public DateTimeOffset LastSeen { get; set; }
    
    // Navigation property
    public ICollection<AuctionItem> Auctions { get; set; } = new List<AuctionItem>();
}