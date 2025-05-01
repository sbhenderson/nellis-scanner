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
    public class HomePageTests : TestContext
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
            var cut = RenderComponent<Home>();

            // Assert
            // Check for statistics in the component
            cut.WaitForElement("div.bg-blue-600");
            var activeAuctionsElement = cut.Find("div.bg-blue-600 h2");
            var closingSoonElement = cut.Find("div.bg-green-600 h2");

            // There should be 3 active auctions in the seeded data
            Assert.Equal("3", activeAuctionsElement.TextContent);
            // There should be 1 auction closing soon
            Assert.Equal("1", closingSoonElement.TextContent);
        }

        [Fact]
        public void Home_ShouldDisplayAuctionValues_WhenDataIsAvailable()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Home>();

            // Assert
            // Check for value statistics in the component
            cut.WaitForElement("div.bg-cyan-600");
            var retailValueElement = cut.Find("div.bg-cyan-600 h2");
            var currentBidsElement = cut.Find("div.bg-yellow-500 h2");

            // Total retail value should be $3,000
            Assert.Contains("$3,000", retailValueElement.TextContent);
            // Total current bids should be $550
            Assert.Contains("$550", currentBidsElement.TextContent);
        }

        [Fact]
        public void Home_ShouldDisplayHighestValueAuctions()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Home>();

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
            var cut = RenderComponent<Home>();

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
                    InventoryNumber = "INV-HV-001"
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
                    InventoryNumber = "INV-MV-002"
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
                    InventoryNumber = "INV-CS-003"
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
                    InventoryNumber = "INV-CL-004"
                }
            });
            _dbContext.SaveChanges();
        }
    }
}