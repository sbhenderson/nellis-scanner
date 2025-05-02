using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Data;
using EFCore.BulkExtensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace NellisScanner.Web.Services;

public class AuctionScannerService
{
    private readonly INellisScanner _nellisScanner;
    private readonly ILogger<AuctionScannerService> _logger;
    private readonly NellisScannerDbContext _dbContext;

    public AuctionScannerService(
        INellisScanner nellisScanner,
        ILogger<AuctionScannerService> logger,
        NellisScannerDbContext dbContext)
    {
        _nellisScanner = nellisScanner;
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Scans each category's auctions and stores them in the database
    /// </summary>
    public async Task ScanEachCategoryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scan of all auction categories");
        var sw = Stopwatch.StartNew();
        foreach(var category in Enum.GetValues<Category>())
        {
            await ScanCategoryAsync(category, cancellationToken);
        }
        _logger.LogInformation("Completed scan of all auction categories in {Elapsed}", sw.Elapsed);
    }

    /// <summary>
    /// Scans auctions from a specific category and stores them in the database
    /// </summary>
    public async Task ScanCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting scan of {Category} auctions", category);
            
            // Collect all products from all pages before processing them
            var allProducts = new List<Product>(600);
            
            // Get first page of auctions
            var response = await _nellisScanner.GetAuctionItemsAsync(
                category: category,
                pageNumber: 0,
                pageSize: 120,
                cancellationToken: cancellationToken);
            
            // Add products from first page
            allProducts.AddRange(response.Products);
            
            // Check if there are multiple pages and fetch them
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
                    
                    allProducts.AddRange(response.Products);
                }
            }
            
            // Process all products in a single batch
            _logger.LogInformation("Fetched total of {Count} products for {Category}", allProducts.Count, category);
            await ProcessProductsAsync(allProducts, cancellationToken);
            
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
            var sw = Stopwatch.StartNew();
            
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
                    auction.State = priceInfo.State;
                    auction.FinalPrice = priceInfo.Price;
                    auction.LastUpdated = DateTimeOffset.UtcNow;
                    
                    // Update the inventory last seen date
                    if (!string.IsNullOrEmpty(priceInfo.InventoryNumber))
                    {
                        var inventory = await EnsureInventoryExistsAsync(
                            _dbContext, priceInfo.InventoryNumber, auction.Title, cancellationToken);
                        
                        if (inventory != null)
                        {
                            inventory.LastSeen = auction.LastUpdated;
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
            _logger.LogInformation("Completed processing expired auctions in {Elapsed}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired auctions");
        }
    }
    
    /// <summary>
    /// Process a list of products and save/update them in the database using bulk operations
    /// </summary>
    private async Task ProcessProductsAsync(List<Product> products, CancellationToken cancellationToken)
    {
        try
        {
            if (products.Count == 0)
            {
                _logger.LogInformation("No products to process");
                return;
            }

            _logger.LogInformation("Processing {Count} products with bulk operations", products.Count);
            
            var now = DateTimeOffset.UtcNow;
            
            // Prepare list of auction items to bulk upsert
            var auctionItems = new List<AuctionItem>();
            
            // Track inventory numbers for bulk inventory processing
            var inventoryNumbers = new HashSet<string>();
            
            foreach (var product in products)
            {
                var auctionItem = new AuctionItem
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
                    LastUpdated = now
                };
                
                // Set location if available
                if (product.Location != null)
                {
                    auctionItem.Location = $"{product.Location.City}, {product.Location.State}";
                }
                
                auctionItems.Add(auctionItem);
                
                // Collect inventory numbers
                if (!string.IsNullOrEmpty(product.InventoryNumber))
                {
                    inventoryNumbers.Add(product.InventoryNumber);
                }
            }
            
            // Get existing auctions to determine which ones should preserve the Closed state
            var existingAuctionIds = auctionItems.Select(a => a.Id).ToList();
            var existingClosedAuctions = await _dbContext.Auctions
                .Where(a => existingAuctionIds.Contains(a.Id) && a.State == AuctionState.Closed)
                .Select(a => a.Id)
                .ToListAsync(cancellationToken);
            
            // Preserve Closed state for auctions that are already closed in our database
            foreach (var auction in auctionItems)
            {
                if (existingClosedAuctions.Contains(auction.Id))
                {
                    auction.State = AuctionState.Closed;
                }
            }
            
            // Bulk insert/update auctions
            var bulkConfig = new BulkConfig 
            { 
                PreserveInsertOrder = false,
                SetOutputIdentity = false,
                UpdateByProperties = [ nameof(AuctionItem.Id) ],
                PropertiesToExcludeOnUpdate = [ 
                    nameof(AuctionItem.Id), 
                    nameof(AuctionItem.FinalPrice) // Don't update FinalPrice as it's set separately when auction closes
                ]
            };

            await EfCoreHelpers.BulkInsertOrUpdateEntitiesAsync(
                _dbContext,
                auctionItems,
                bulkConfig,
                new List<string> { nameof(AuctionItem.Id) },
                new List<string> { nameof(AuctionItem.Id), nameof(AuctionItem.FinalPrice) },
                cancellationToken);
            
            _logger.LogInformation("Completed bulk insert/update of {Count} auctions", auctionItems.Count);
            
            // Handle inventory items in bulk
            await ProcessInventoryItemsAsync(inventoryNumbers, products, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk processing of products");
        }
    }
    
    /// <summary>
    /// Process inventory items in bulk
    /// </summary>
    private async Task ProcessInventoryItemsAsync(
        HashSet<string> inventoryNumbers,
        List<Product> products,
        CancellationToken cancellationToken)
    {
        if (inventoryNumbers.Count == 0)
        {
            return;
        }
        
        _logger.LogInformation("Processing {Count} inventory items", inventoryNumbers.Count);
        
        var now = DateTimeOffset.UtcNow;
        
        // Get existing inventory items
        var existingInventory = await _dbContext.Inventory
            .Where(i => inventoryNumbers.Contains(i.InventoryNumber))
            .ToDictionaryAsync(i => i.InventoryNumber, cancellationToken);
        
        // Create collection for bulk insert/update
        var inventoryItemsToUpsert = new List<InventoryItem>(products.Count);
        
        foreach (var inventoryNumber in inventoryNumbers)
        {
            // Find corresponding product
            var matchingProduct = products.FirstOrDefault(p => p.InventoryNumber == inventoryNumber);
            string? description = matchingProduct?.Title;
            
            if (existingInventory.TryGetValue(inventoryNumber, out var inventory))
            {
                // Existing inventory, update
                inventory.LastSeen = now;
                
                // Update description if necessary
                if (string.IsNullOrEmpty(inventory.Description) && !string.IsNullOrEmpty(description))
                {
                    inventory.Description = description;
                }
                
                inventoryItemsToUpsert.Add(inventory);
            }
            else
            {
                // New inventory item
                var newInventory = new InventoryItem
                {
                    InventoryNumber = inventoryNumber,
                    Description = description,
                    FirstSeen = now,
                    LastSeen = now
                };
                
                inventoryItemsToUpsert.Add(newInventory);
            }
        }
        
        // Bulk insert/update inventory
        var bulkConfig = new BulkConfig 
        {
            PreserveInsertOrder = false,
            SetOutputIdentity = true,
            UpdateByProperties = [ nameof(InventoryItem.InventoryNumber) ]
        };

        await EfCoreHelpers.BulkInsertOrUpdateEntitiesAsync(
            _dbContext,
            inventoryItemsToUpsert,
            bulkConfig,
            new List<string> { nameof(InventoryItem.InventoryNumber) },
            null,
            cancellationToken);
        
        _logger.LogInformation("Completed bulk insert/update of {Count} inventory items", inventoryItemsToUpsert.Count);
    }
    
    /// <summary>
    /// Ensures that an inventory item exists in the database
    /// </summary>
    private async Task<InventoryItem?> EnsureInventoryExistsAsync(
        NellisScannerDbContext dbContext, 
        long inventoryNumber, 
        string? productTitle, 
        CancellationToken cancellationToken)
    {
        var inventory = await dbContext.Inventory
            .FindAsync(inventoryNumber, cancellationToken);
        
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