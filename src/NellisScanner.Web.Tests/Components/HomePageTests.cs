using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NellisScanner.Core;
using NellisScanner.Web.Components.Pages;
using NellisScanner.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NellisScanner.Web.Tests.Components
{
    public class HomePageTests : BunitContext
    {
        private readonly NellisScannerDbContext _dbContext;

        public HomePageTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<NellisScannerDbContext>()
                .UseInMemoryDatabase(databaseName: "HomePageTestDb_" + Guid.NewGuid())
                .Options;
            _dbContext = new NellisScannerDbContext(options);

            // Register the DbContext in the BUnit test context
            Services.AddSingleton(_dbContext);
        }

        [Fact]
        public void Home_ShouldDisplayActiveCounts_WhenDataIsAvailable()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = Render<Home>();

            // Assert
            // Check for statistics in the component
            cut.WaitForElement("h2");
            var h2s = cut.FindAll("h2");

            // There should be 3 active auctions in the seeded data
            Assert.Contains(h2s, h => h.TextContent.Trim() == "3");
            // There should be 1 auction closing soon
            Assert.Contains(h2s, h => h.TextContent.Trim() == "1");
        }

        [Fact]
        public void Home_ShouldDisplayAuctionValues_WhenDataIsAvailable()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = Render<Home>();

            // Assert
            // Check for value statistics in the component
            cut.WaitForElement("h2");
            var h2s = cut.FindAll("h2");

            // Total retail value should be $3,000
            Assert.Contains(h2s, h => h.TextContent.Contains("$3,000"));
            // Total current bids should be $550
            Assert.Contains(h2s, h => h.TextContent.Contains("$550"));
        }

        [Fact]
        public void Home_ShouldDisplayHighestValueAuctions()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = Render<Home>();

            // Assert
            // Wait for the component to render
            cut.WaitForElement("table");
            
            // Check for content related to high value items
            var pageContent = cut.Markup;
            Assert.Contains("High Value Item", pageContent);
            Assert.Contains("1,500.00", pageContent);  // Price formatting may vary
        }

        [Fact]
        public void Home_ShouldDisplayClosingSoonAuctions()
        {
            // Arrange
            SeedDatabaseWithTestData(includeClosingSoon: true);

            // Act
            var cut = Render<Home>();

            // Assert
            // Wait for the component to render fully
            cut.WaitForElement("table");
            
            // Check that the page contains a reference to our closing soon item
            var pageContent = cut.Markup;
            Assert.Contains("Closing Soon Item", pageContent);
            
            // Look for time-related content (likely to appear near closing soon items)
            Assert.Contains("minutes", pageContent.ToLower());
        }

        private void SeedDatabaseWithTestData(bool includeClosingSoon = true)
        {
            // Clear any existing data
            _dbContext.Auctions.RemoveRange(_dbContext.Auctions);
            _dbContext.SaveChanges();

            // Setup current time for testing
            var now = DateTimeOffset.UtcNow;

            // Add test auctions
            _dbContext.Auctions.AddRange(new List<AuctionItem>
            {
                new AuctionItem {
                    Id = 1,
                    Title = "High Value Item",
                    RetailPrice = 1500.00M,
                    CurrentPrice = 300.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-2),
                    CloseTime = now.AddDays(1),
                    LastUpdated = now,
                    BidCount = 5,
                    InventoryNumber = 1L
                },
                new AuctionItem {
                    Id = 2,
                    Title = "Medium Value Item",
                    RetailPrice = 1000.00M,
                    CurrentPrice = 150.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-1),
                    CloseTime = now.AddDays(2),
                    LastUpdated = now,
                    BidCount = 3,
                    InventoryNumber = 2L
                },
                new AuctionItem {
                    Id = 3,
                    Title = "Closing Soon Item",
                    RetailPrice = 500.00M,
                    CurrentPrice = 100.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-3),
                    // Ensure this item is always recognized as closing soon (within 15 minutes)
                    CloseTime = includeClosingSoon ? now.AddMinutes(15) : now.AddDays(1),
                    LastUpdated = now,
                    BidCount = 10,
                    InventoryNumber = 3L
                },
                new AuctionItem {
                    Id = 4,
                    Title = "Closed Item",
                    RetailPrice = 800.00M,
                    CurrentPrice = 200.00M,
                    FinalPrice = 200.00M,
                    State = AuctionState.Closed, // This item is already closed
                    OpenTime = now.AddDays(-5),
                    CloseTime = now.AddDays(-1),
                    LastUpdated = now,
                    BidCount = 7,
                    InventoryNumber = 4L
                }
            });
            _dbContext.SaveChanges();
        }
    }
}