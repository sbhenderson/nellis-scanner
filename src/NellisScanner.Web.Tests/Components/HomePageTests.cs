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
            cut.WaitForState(() => cut.FindAll("div.bg-blue-600").Any());
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
            cut.WaitForState(() => cut.FindAll("div.bg-cyan-600").Any());
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
            // Check for the top value auctions table
            cut.WaitForState(() => cut.FindAll("table").Any());
            var tableRows = cut.FindAll("table:first-of-type tbody tr").ToArray();
            
            // Should have top 3 auctions by retail price
            Assert.Equal(3, tableRows.Length);
            
            // First auction should be "High Value Item" with highest retail price
            var firstRowColumns = tableRows[0].FindAll("td").ToArray();
            Assert.Contains("High Value Item", firstRowColumns[0].TextContent);
            Assert.Contains("$1,500.00", firstRowColumns[1].TextContent);
        }

        [Fact]
        public void Home_ShouldDisplayClosingSoonAuctions()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Home>();

            // Assert
            // Check for the closing soon auctions table
            cut.WaitForState(() => cut.FindAll("table").Count() >= 2);
            var closingSoonTable = cut.FindAll("table")[1];
            var tableRows = closingSoonTable.FindAll("tbody tr").ToArray();
            
            // Should have 1 auction closing soon
            Assert.Single(tableRows);
            
            // The auction should be "Closing Soon Item"
            var firstRowColumns = tableRows[0].FindAll("td").ToArray();
            Assert.Contains("Closing Soon Item", firstRowColumns[0].TextContent);
        }

        private void SeedDatabaseWithTestData()
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
                    BidCount = 5
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
                    BidCount = 3
                },
                new AuctionItem {
                    Id = 3,
                    Title = "Closing Soon Item",
                    RetailPrice = 500.00M,
                    CurrentPrice = 100.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-3),
                    CloseTime = now.AddMinutes(15), // Closing within 30 minutes
                    LastUpdated = now,
                    BidCount = 10
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
                    BidCount = 7
                }
            });
            _dbContext.SaveChanges();
        }
    }
}