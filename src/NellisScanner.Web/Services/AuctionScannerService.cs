using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Data;

namespace NellisScanner.Web.Services;

public class AuctionScannerService
{
    private readonly Core.NellisScanner _nellisScanner;
    private readonly ILogger<AuctionScannerService> _logger;
    private readonly NellisScannerDbContext _dbContext;

    public AuctionScannerService(
        Core.NellisScanner nellisScanner,
        ILogger<AuctionScannerService> logger,
        NellisScannerDbContext dbContext)
    {
        _nellisScanner = nellisScanner;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Scans electronics auctions and stores them in the database
    /// </summary>
    public async Task ScanElectronicsAsync(CancellationToken cancellationToken = default)
    {
        await ScanCategoryAsync(Category.Electronics, cancellationToken);
    }

    /// <summary>
    /// Scans auctions from a specific category and stores them in the database
    /// </summary>
    public async Task ScanCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting scan of {Category} auctions", category);
            
            // Get first page of auctions
            var response = await _nellisScanner.GetAuctionItemsAsync(
                category: category,
                pageNumber: 0,
                pageSize: 120,
                cancellationToken: cancellationToken);
            
            // Process products from first page
            await ProcessProductsAsync(response.Products, cancellationToken);
            
            // Check if there are multiple pages and process them
            if (response.Algolia?.NumberOfPages > 1)
            {
                int totalPages = Math.Min(response.Algolia.NumberOfPages, 5); // Limit to 5 pages to avoid too many requests
                for (int page = 1; page < totalPages; page++)
                {
                    response = await _nellisScanner.GetAuctionItemsAsync(
                        category: category,
                        pageNumber: page,
                        pageSize: 120,
                        cancellationToken: cancellationToken);
                    await ProcessProductsAsync(response.Products, cancellationToken);
                }
            }
            
            _logger.LogInformation("Completed scan of {Category} auctions", category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning {Category} auctions", category);
        }
    }

    /// <summary>
    /// Updates the status of closed auctions by checking all active auctions that are 
    /// past their closing time by at least 30 minutes
    /// </summary>
    public async Task UpdateClosedAuctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing expired auctions to mark as closed");
            
            // Get auctions that are still active but past their close time by at least 30 minutes
            var thirtyMinutesAgo = DateTimeOffset.UtcNow.AddMinutes(-30);
            var potentiallyClosedAuctions = await _dbContext.Auctions
                .Where(a => a.State == AuctionState.Active && 
                           a.CloseTime < thirtyMinutesAgo)
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Found {Count} expired auctions to update", potentiallyClosedAuctions.Count);
            
            foreach (var auction in potentiallyClosedAuctions)
            {
                try
                {
                    var priceInfo = await _nellisScanner.GetAuctionPriceInfoAsync(
                        auction.Id, auction.Title ?? "", cancellationToken);
                    
                    // Update the auction with the final price
                    auction.State = AuctionState.Closed;
                    auction.FinalPrice = priceInfo.Price;
                    auction.LastUpdated = DateTimeOffset.UtcNow;
                    
                    // Update the inventory last seen date
                    if (!string.IsNullOrEmpty(priceInfo.InventoryNumber))
                    {
                        var inventory = await EnsureInventoryExistsAsync(
                            _dbContext, priceInfo.InventoryNumber, auction.Title, cancellationToken);
                        
                        if (inventory != null)
                        {
                            inventory.LastSeen = DateTimeOffset.UtcNow;
                        }
                    }
                    
                    _logger.LogInformation("Updated closed auction {Id} with final price {Price}", 
                        auction.Id, auction.FinalPrice);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating closed auction {Id}", auction.Id);
                }
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed processing expired auctions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired auctions");
        }
    }
    
    /// <summary>
    /// Process a list of products and save/update them in the database
    /// </summary>
    private async Task ProcessProductsAsync(List<Product> products, CancellationToken cancellationToken)
    {
        foreach (var product in products)
        {
            await UpdateProductAsync(product, cancellationToken);
        }
    }
    
    /// <summary>
    /// Update or add a product in the database
    /// </summary>
    private async Task UpdateProductAsync(Product product, CancellationToken cancellationToken)
    {
        try
        {
            // Check if auction already exists in database
            var existingAuction = await _dbContext.Auctions
                .FirstOrDefaultAsync(a => a.Id == product.Id, cancellationToken);
            
            if (existingAuction != null)
            {
                // Auction exists, update it
                existingAuction.Title = product.Title;
                existingAuction.RetailPrice = product.RetailPrice;
                existingAuction.CurrentPrice = product.CurrentPrice;
                existingAuction.BidCount = product.BidCount;
                
                // Only update state if it's active (we don't want to revert closed auctions back to active)
                if (existingAuction.State != AuctionState.Closed)
                {
                    existingAuction.State = product.IsClosed ? AuctionState.Closed : AuctionState.Active;
                }
                
                existingAuction.CloseTime = product.CloseTime;
                existingAuction.LastUpdated = DateTimeOffset.UtcNow;
                
                // Set location if available
                if (product.Location != null)
                {
                    existingAuction.Location = $"{product.Location.City}, {product.Location.State}";
                }
                
                // Update the inventory if we have an inventory number
                if (!string.IsNullOrEmpty(product.InventoryNumber) && 
                    (existingAuction.InventoryNumber != product.InventoryNumber || existingAuction.Inventory == null))
                {
                    existingAuction.InventoryNumber = product.InventoryNumber;
                    await EnsureInventoryExistsAsync(_dbContext, product.InventoryNumber, product.Title, cancellationToken);
                }
            }
            else
            {
                // Auction doesn't exist, add it
                var newAuction = new AuctionItem
                {
                    Id = product.Id,
                    Title = product.Title,
                    InventoryNumber = product.InventoryNumber,
                    RetailPrice = product.RetailPrice,
                    CurrentPrice = product.CurrentPrice,
                    FinalPrice = 0, // Will be set when auction closes
                    BidCount = product.BidCount,
                    State = product.IsClosed ? AuctionState.Closed : AuctionState.Active,
                    OpenTime = product.OpenTime,
                    CloseTime = product.CloseTime,
                    LastUpdated = DateTimeOffset.UtcNow
                };
                
                // Set location if available
                if (product.Location != null)
                {
                    newAuction.Location = $"{product.Location.City}, {product.Location.State}";
                }
                
                _dbContext.Auctions.Add(newAuction);
                
                // Create inventory entry if we have an inventory number
                if (!string.IsNullOrEmpty(product.InventoryNumber))
                {
                    await EnsureInventoryExistsAsync(_dbContext, product.InventoryNumber, product.Title, cancellationToken);
                }
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating auction {ProductId}", product.Id);
        }
    }
    
    /// <summary>
    /// Ensures that an inventory item exists in the database
    /// </summary>
    private async Task<InventoryItem?> EnsureInventoryExistsAsync(
        NellisScannerDbContext dbContext, 
        string inventoryNumber, 
        string? productTitle, 
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.Inventory
            .FirstOrDefaultAsync(i => i.InventoryNumber == inventoryNumber, cancellationToken);
        
        if (inventory == null)
        {
            // Create new inventory item
            inventory = new InventoryItem
            {
                InventoryNumber = inventoryNumber,
                Description = productTitle,
                FirstSeen = DateTimeOffset.UtcNow,
                LastSeen = DateTimeOffset.UtcNow
            };
            
            dbContext.Inventory.Add(inventory);
        }
        else
        {
            // Update last seen date
            inventory.LastSeen = DateTimeOffset.UtcNow;
            
            // Update description if it's empty and we have a title
            if (string.IsNullOrEmpty(inventory.Description) && !string.IsNullOrEmpty(productTitle))
            {
                inventory.Description = productTitle;
            }
        }
        
        return inventory;
    }
}