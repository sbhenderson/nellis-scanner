using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Data;

namespace NellisScanner.Web.Services;

public class AuctionScannerService
{
    private readonly Core.NellisScanner _nellisScanner;
    private readonly ILogger<AuctionScannerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuctionScannerService(
        Core.NellisScanner nellisScanner,
        ILogger<AuctionScannerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _nellisScanner = nellisScanner;
        _logger = logger;
        _scopeFactory = scopeFactory;
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
    /// Monitors auctions that are close to closing to track final prices
    /// </summary>
    public async Task MonitorClosingAuctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Monitoring auctions that are closing soon");
            
            // Create a scope to use the DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
            
            // Get auctions that are closing within the next 30 minutes
            var thirtyMinutesFromNow = DateTimeOffset.UtcNow.AddMinutes(30);
            var now = DateTimeOffset.UtcNow;
            
            var closingAuctions = await dbContext.Auctions
                .Where(a => a.State == AuctionState.Active && 
                           a.CloseTime > now && 
                           a.CloseTime <= thirtyMinutesFromNow)
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Found {Count} auctions closing within 30 minutes", closingAuctions.Count);
            
            // Update each auction's current price
            foreach (var auction in closingAuctions)
            {
                try
                {
                    var priceInfo = await _nellisScanner.GetAuctionPriceInfoAsync(
                        auction.Id, auction.Title ?? "", cancellationToken);
                    
                    auction.CurrentPrice = priceInfo.Price;
                    auction.LastUpdated = DateTimeOffset.UtcNow;
                    
                    if (!string.IsNullOrEmpty(priceInfo.InventoryNumber) && 
                        auction.InventoryNumber != priceInfo.InventoryNumber)
                    {
                        auction.InventoryNumber = priceInfo.InventoryNumber;
                        await EnsureInventoryExistsAsync(dbContext, priceInfo.InventoryNumber, auction.Title, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating auction {Id}", auction.Id);
                }
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            // Process recently closed auctions
            await ProcessClosedAuctionsAsync(cancellationToken);
            
            _logger.LogInformation("Completed monitoring of closing auctions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring closing auctions");
        }
    }

    /// <summary>
    /// Processes recently closed auctions to get final prices
    /// </summary>
    private async Task ProcessClosedAuctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing recently closed auctions");
            
            // Create a scope to use the DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
            
            // Get auctions that are still active but past their close time
            var dayAgo = DateTimeOffset.UtcNow.AddDays(-1);
            var potentiallyClosedAuctions = await dbContext.Auctions
                .Where(a => a.State == AuctionState.Active && 
                           a.CloseTime < DateTimeOffset.UtcNow && 
                           a.CloseTime > dayAgo)
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Found {Count} potentially closed auctions to update", potentiallyClosedAuctions.Count);
            
            foreach (var auction in potentiallyClosedAuctions)
            {
                // Get final price from product page
                try
                {
                    var priceInfo = await _nellisScanner.GetAuctionPriceInfoAsync(
                        auction.Id, auction.Title ?? "", cancellationToken);
                    
                    // Update the auction with the final price if it's closed
                    if (priceInfo.State == AuctionState.Closed)
                    {
                        auction.State = AuctionState.Closed;
                        auction.FinalPrice = priceInfo.Price;
                        auction.LastUpdated = DateTimeOffset.UtcNow;
                        
                        // Update the inventory last seen date
                        if (!string.IsNullOrEmpty(priceInfo.InventoryNumber))
                        {
                            var inventory = await EnsureInventoryExistsAsync(
                                dbContext, priceInfo.InventoryNumber, auction.Title, cancellationToken);
                            
                            if (inventory != null)
                            {
                                inventory.LastSeen = DateTimeOffset.UtcNow;
                            }
                        }
                        
                        _logger.LogInformation("Updated closed auction {Id} with final price {Price}", 
                            auction.Id, auction.FinalPrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating auction {Id}", auction.Id);
                }
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed processing closed auctions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing closed auctions");
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
        // Create a scope to use the DbContext
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
        
        try
        {
            // Check if auction already exists in database
            var existingAuction = await dbContext.Auctions
                .FirstOrDefaultAsync(a => a.Id == product.Id, cancellationToken);
            
            if (existingAuction != null)
            {
                // Auction exists, update it
                existingAuction.Title = product.Title;
                existingAuction.RetailPrice = product.RetailPrice;
                existingAuction.CurrentPrice = product.CurrentPrice;
                existingAuction.BidCount = product.BidCount;
                existingAuction.State = product.IsClosed ? AuctionState.Closed : AuctionState.Active;
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
                    await EnsureInventoryExistsAsync(dbContext, product.InventoryNumber, product.Title, cancellationToken);
                }
                
                // If the auction is closed, make sure we get the final price directly from the HTML
                if (product.IsClosed && existingAuction.State != AuctionState.Closed)
                {
                    try
                    {
                        var priceInfo = await _nellisScanner.GetAuctionPriceInfoAsync(
                            product.Id, product.Title ?? "", cancellationToken);
                            
                        existingAuction.State = AuctionState.Closed;
                        existingAuction.FinalPrice = priceInfo.Price;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting final price for closed auction {ProductId}", product.Id);
                        existingAuction.FinalPrice = product.CurrentPrice;
                    }
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
                    FinalPrice = product.IsClosed ? product.CurrentPrice : 0, // Initial value for final price
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
                
                dbContext.Auctions.Add(newAuction);
                
                // Create inventory entry if we have an inventory number
                if (!string.IsNullOrEmpty(product.InventoryNumber))
                {
                    await EnsureInventoryExistsAsync(dbContext, product.InventoryNumber, product.Title, cancellationToken);
                }
                
                // If the auction is closed, try to get the real final price from HTML
                if (product.IsClosed)
                {
                    try
                    {
                        var priceInfo = await _nellisScanner.GetAuctionPriceInfoAsync(
                            product.Id, product.Title ?? "", cancellationToken);
                            
                        newAuction.FinalPrice = priceInfo.Price;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting final price for new closed auction {ProductId}", product.Id);
                    }
                }
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
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