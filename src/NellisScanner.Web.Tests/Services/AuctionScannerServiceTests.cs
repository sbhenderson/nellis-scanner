using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Data;
using NellisScanner.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NellisScanner.Web.Tests.Services
{
    public class AuctionScannerServiceTests
    {
        private readonly Mock<INellisScanner> _mockNellisScanner;
        private readonly Mock<ILogger<AuctionScannerService>> _mockLogger;
        private readonly NellisScannerDbContext _dbContext;
        private readonly AuctionScannerService _sut; // System Under Test

        public AuctionScannerServiceTests()
        {
            // Create mock for INellisScanner
            _mockNellisScanner = new Mock<INellisScanner>();
                
            _mockLogger = new Mock<ILogger<AuctionScannerService>>();

            // Create in-memory database context
            var options = new DbContextOptionsBuilder<NellisScannerDbContext>()
                .UseInMemoryDatabase(databaseName: "NellisScannerTestDb_" + Guid.NewGuid())
                .Options;
            _dbContext = new NellisScannerDbContext(options);

            // Create system under test
            _sut = new AuctionScannerService(
                _mockNellisScanner.Object,
                _mockLogger.Object,
                _dbContext);
        }

        [Fact]
        public async Task ScanElectronicsAsync_ShouldScanCategoryWithElectronics()
        {
            // Arrange
            var testProducts = new List<Product>
            {
                new Product
                {
                    Id = 1001,
                    Title = "Test Electronics Product",
                    RetailPrice = 999.99M,
                    CurrentPrice = 199.99M,
                    IsClosed = false,
                    OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                    CloseTime = DateTimeOffset.UtcNow.AddDays(1),
                    BidCount = 5,
                    InventoryNumber = "INV-001"
                }
            };

            var testResponse = new SearchResponse
            {
                Products = testProducts
            };
            
            // Add NumberOfPages property via reflection if property exists in AlgoliaInfo
            var algoliaInfo = new AlgoliaInfo();
            var property = typeof(AlgoliaInfo).GetProperty("NumberOfPages");
            if (property != null)
            {
                property.SetValue(algoliaInfo, 1);
            }
            
            // Set the Algolia property through reflection
            var algoliaProperty = typeof(SearchResponse).GetProperty("Algolia");
            if (algoliaProperty != null)
            {
                algoliaProperty.SetValue(testResponse, algoliaInfo);
            }

            _mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                    Category.Electronics,
                    It.IsAny<int>(),
                    120,
                    It.IsAny<NellisLocations>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponse);

            // Act
            await _sut.ScanEachCategoryAsync(CancellationToken.None);

            // Assert
            var auctions = await _dbContext.Auctions.ToListAsync();
            Assert.Single(auctions);
            Assert.Equal(1001, auctions[0].Id);
            Assert.Equal("Test Electronics Product", auctions[0].Title);
            Assert.Equal(AuctionState.Active, auctions[0].State);

            // Verify the scanner was called with Electronics category
            _mockNellisScanner.Verify(s => s.GetAuctionItemsAsync(
                Category.Electronics,
                0,
                120,
                It.IsAny<NellisLocations>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateClosedAuctionsAsync_ShouldUpdateExpiredAuctions()
        {
            // Arrange
            // Add an auction that's past its close time by more than 30 minutes
            var expiredAuction = new AuctionItem
            {
                Id = 5001,
                Title = "Expired Auction",
                RetailPrice = 599.99M,
                CurrentPrice = 349.99M,
                State = AuctionState.Active, // Still marked as active even though it's expired
                OpenTime = DateTimeOffset.UtcNow.AddDays(-2),
                CloseTime = DateTimeOffset.UtcNow.AddMinutes(-31), // Past close time by more than 30 minutes
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                InventoryNumber = "INV-5001"
            };
            _dbContext.Auctions.Add(expiredAuction);
            await _dbContext.SaveChangesAsync();

            // Setup mock scanner to return a closed auction
            var priceInfo = new AuctionPriceInfo
            {
                ProductId = 5001,
                State = AuctionState.Closed,
                Price = 359.99M, // Final price
                InventoryNumber = "INV-5001",
                TimeRetrieved = DateTimeOffset.UtcNow
            };

            _mockNellisScanner.Setup(s => s.GetAuctionPriceInfoAsync(
                    It.Is<int>(id => id == 5001),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(priceInfo);

            // Act
            await _sut.UpdateClosedAuctionsAsync(CancellationToken.None);

            // Assert
            var updatedAuction = await _dbContext.Auctions.FindAsync(5001);
            Assert.NotNull(updatedAuction);
            Assert.Equal(AuctionState.Closed, updatedAuction.State);
            Assert.Equal(359.99M, updatedAuction.FinalPrice);
        }

        [Fact]
        public async Task ProcessProducts_ShouldUpdateExistingAuctions()
        {
            // Arrange
            // Add an existing auction to the database
            var existingAuction = new AuctionItem
            {
                Id = 2001,
                Title = "Existing Auction",
                RetailPrice = 499.99M,
                CurrentPrice = 99.99M,
                State = AuctionState.Active,
                OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                CloseTime = DateTimeOffset.UtcNow.AddDays(1),
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1)
            };
            _dbContext.Auctions.Add(existingAuction);
            await _dbContext.SaveChangesAsync();

            // Create an updated version of the product
            var updatedProduct = new Product
            {
                Id = 2001,
                Title = "Updated Auction Title",
                RetailPrice = 499.99M,
                CurrentPrice = 149.99M, // Price has increased
                IsClosed = false,
                OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                CloseTime = DateTimeOffset.UtcNow.AddDays(1),
                BidCount = 8, // Bid count has increased
                InventoryNumber = "INV-2001" // Now has an inventory number
            };

            var testResponse = new SearchResponse
            {
                Products = new List<Product> { updatedProduct }
            };

            // Add NumberOfPages property via reflection if property exists
            var algoliaInfo = new AlgoliaInfo();
            var property = typeof(AlgoliaInfo).GetProperty("NumberOfPages");
            if (property != null)
            {
                property.SetValue(algoliaInfo, 1);
            }
            
            // Set the Algolia property through reflection
            var algoliaProperty = typeof(SearchResponse).GetProperty("Algolia");
            if (algoliaProperty != null)
            {
                algoliaProperty.SetValue(testResponse, algoliaInfo);
            }

            _mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                    It.IsAny<Category>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<NellisLocations>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponse);

            // Act
            await _sut.ScanCategoryAsync(Category.Electronics, CancellationToken.None);

            // Assert
            var updatedAuction = await _dbContext.Auctions.FindAsync(2001);
            Assert.NotNull(updatedAuction);
            Assert.Equal("Updated Auction Title", updatedAuction.Title);
            Assert.Equal(149.99M, updatedAuction.CurrentPrice);
            Assert.Equal(8, updatedAuction.BidCount);
            Assert.Equal("INV-2001", updatedAuction.InventoryNumber);
        }

        [Fact]
        public async Task InventoryTracking_ShouldCreateInventoryItem_WhenNewInventoryNumberIsDetected()
        {
            // Arrange
            var product = new Product
            {
                Id = 3001,
                Title = "New Product with Inventory",
                RetailPrice = 799.99M,
                CurrentPrice = 299.99M,
                IsClosed = false,
                OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                CloseTime = DateTimeOffset.UtcNow.AddDays(1),
                BidCount = 3,
                InventoryNumber = "INV-3001"
            };

            var testResponse = new SearchResponse
            {
                Products = new List<Product> { product }
            };

            // Add NumberOfPages property via reflection if property exists
            var algoliaInfo = new AlgoliaInfo();
            var property = typeof(AlgoliaInfo).GetProperty("NumberOfPages");
            if (property != null)
            {
                property.SetValue(algoliaInfo, 1);
            }
            
            // Set the Algolia property through reflection
            var algoliaProperty = typeof(SearchResponse).GetProperty("Algolia");
            if (algoliaProperty != null)
            {
                algoliaProperty.SetValue(testResponse, algoliaInfo);
            }

            _mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                    It.IsAny<Category>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<NellisLocations>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponse);

            // Act
            await _sut.ScanCategoryAsync(Category.Electronics, CancellationToken.None);

            // Assert
            // Verify auction was created
            var auction = await _dbContext.Auctions.FindAsync(3001);
            Assert.NotNull(auction);
            Assert.Equal("INV-3001", auction.InventoryNumber);

            // Verify inventory item was created
            var inventory = await _dbContext.Inventory
                .FirstOrDefaultAsync(i => i.InventoryNumber == "INV-3001");
            Assert.NotNull(inventory);
            Assert.Equal("New Product with Inventory", inventory.Description);
        }

        [Fact]
        public async Task InventoryTracking_ShouldLinkMultipleAuctions_ToSameInventoryItem()
        {
            // Arrange
            // First, add an auction with a specific inventory number
            var product1 = new Product
            {
                Id = 4001,
                Title = "First Auction with Shared Inventory",
                RetailPrice = 399.99M,
                CurrentPrice = 99.99M,
                IsClosed = false,
                OpenTime = DateTimeOffset.UtcNow.AddDays(-3),
                CloseTime = DateTimeOffset.UtcNow.AddDays(-2),
                InventoryNumber = "SHARED-INV-001"
            };

            var testResponse1 = new SearchResponse
            {
                Products = new List<Product> { product1 }
            };

            // Add NumberOfPages property via reflection if property exists
            var algoliaInfo1 = new AlgoliaInfo();
            var property = typeof(AlgoliaInfo).GetProperty("NumberOfPages");
            if (property != null)
            {
                property.SetValue(algoliaInfo1, 1);
            }
            
            // Set the Algolia property through reflection
            var algoliaProperty = typeof(SearchResponse).GetProperty("Algolia");
            if (algoliaProperty != null)
            {
                algoliaProperty.SetValue(testResponse1, algoliaInfo1);
            }

            _mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                    It.IsAny<Category>(),
                    0,
                    It.IsAny<int>(),
                    It.IsAny<NellisLocations>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponse1);

            // Act - First scan
            await _sut.ScanCategoryAsync(Category.Electronics, CancellationToken.None);

            // Setup a second auction with the same inventory number
            var product2 = new Product
            {
                Id = 4002,
                Title = "Second Auction with Shared Inventory",
                RetailPrice = 399.99M,
                CurrentPrice = 129.99M,
                IsClosed = false,
                OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                CloseTime = DateTimeOffset.UtcNow.AddDays(1),
                InventoryNumber = "SHARED-INV-001"
            };

            var testResponse2 = new SearchResponse
            {
                Products = new List<Product> { product2 }
            };

            // Add NumberOfPages property via reflection if property exists
            var algoliaInfo2 = new AlgoliaInfo();
            if (property != null)
            {
                property.SetValue(algoliaInfo2, 1);
            }
            
            // Set the Algolia property through reflection
            if (algoliaProperty != null)
            {
                algoliaProperty.SetValue(testResponse2, algoliaInfo2);
            }

            _mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                    It.IsAny<Category>(),
                    0,
                    It.IsAny<int>(),
                    It.IsAny<NellisLocations>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(testResponse2);

            // Act - Second scan
            await _sut.ScanCategoryAsync(Category.Electronics, CancellationToken.None);

            // Assert
            // Verify both auctions exist
            var auctions = await _dbContext.Auctions
                .Where(a => a.InventoryNumber == "SHARED-INV-001")
                .ToListAsync();
            Assert.Equal(2, auctions.Count);
            
            // Verify a single inventory item exists with that number
            var inventoryItems = await _dbContext.Inventory
                .Where(i => i.InventoryNumber == "SHARED-INV-001")
                .ToListAsync();
            Assert.Single(inventoryItems);
        }
    }
}