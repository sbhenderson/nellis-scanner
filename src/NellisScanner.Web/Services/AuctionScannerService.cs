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
        try
        {
            _logger.LogInformation("Starting scan of electronics auctions");
            
            // Get first page of auctions
            var response = await _nellisScanner.GetElectronicsHighToLowAsync(0, cancellationToken: cancellationToken);
            
            // Process products from first page
            await ProcessProductsAsync(response.Products, cancellationToken);
            
            // Check if there are multiple pages and process them
            if (response.Algolia?.NumberOfPages > 1)
            {
                int totalPages = Math.Min(response.Algolia.NumberOfPages, 5); // Limit to 5 pages to avoid too many requests
                for (int page = 1; page < totalPages; page++)
                {
                    response = await _nellisScanner.GetElectronicsHighToLowAsync(page, cancellationToken: cancellationToken);
                    await ProcessProductsAsync(response.Products, cancellationToken);
                }
            }
            
            _logger.LogInformation("Completed scan of electronics auctions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning electronics auctions");
        }
    }

    /// <summary>
    /// Monitors auctions closing soon and updates their prices more frequently
    /// </summary>
    public async Task MonitorClosingAuctionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Monitoring auctions closing soon");
            
            // Create a scope to use the DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
            
            // Get auctions closing within the next 30 minutes
            var closingTime = DateTimeOffset.UtcNow.AddMinutes(30);
            var closingProducts = await dbContext.Products
                .Where(p => p.CloseTime <= closingTime && !p.IsClosed)
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Found {Count} auctions closing within 30 minutes", closingProducts.Count);
            
            foreach (var product in closingProducts)
            {
                // Check specific product for updates
                var freshProduct = await _nellisScanner.GetProductAsync(product.Id, cancellationToken);
                if (freshProduct != null)
                {
                    await UpdateProductAsync(freshProduct, cancellationToken);
                }
            }
            
            _logger.LogInformation("Completed monitoring auctions closing soon");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring closing auctions");
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
            // Check if product already exists in database
            var existingProduct = await dbContext.Products
                .Include(p => p.PriceHistory.OrderByDescending(ph => ph.RecordedAt).Take(1))
                .FirstOrDefaultAsync(p => p.Id == product.Id, cancellationToken);
            
            if (existingProduct != null)
            {
                // Product exists, update it
                existingProduct.Title = product.Title;
                existingProduct.CurrentPrice = product.CurrentPrice;
                existingProduct.BidCount = product.BidCount;
                existingProduct.RetailPrice = product.RetailPrice;
                existingProduct.Notes = product.Notes;
                existingProduct.IsClosed = product.IsClosed;
                existingProduct.MarketStatus = product.MarketStatus;
                existingProduct.OpenTime = product.OpenTime;
                existingProduct.CloseTime = product.CloseTime;
                existingProduct.InitialCloseTime = product.InitialCloseTime;
                existingProduct.ExtensionInterval = product.ExtensionInterval;
                existingProduct.ProjectExtended = product.ProjectExtended;
                
                // Check if price or bid count changed to add a new price history entry
                var latestHistory = existingProduct.PriceHistory.FirstOrDefault();
                if (latestHistory == null || 
                    latestHistory.Price != product.CurrentPrice || 
                    latestHistory.BidCount != product.BidCount)
                {
                    // Add new price history entry
                    var priceHistory = new PriceHistory
                    {
                        ProductId = product.Id,
                        Price = product.CurrentPrice,
                        BidCount = product.BidCount,
                        RecordedAt = DateTimeOffset.UtcNow
                    };
                    dbContext.PriceHistory.Add(priceHistory);
                }
            }
            else
            {
                // Product doesn't exist, add it
                dbContext.Products.Add(product);
                
                // Add initial price history entry
                var priceHistory = new PriceHistory
                {
                    ProductId = product.Id,
                    Price = product.CurrentPrice,
                    BidCount = product.BidCount,
                    RecordedAt = DateTimeOffset.UtcNow
                };
                dbContext.PriceHistory.Add(priceHistory);
            }
            
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", product.Id);
        }
    }
}