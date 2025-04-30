using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NellisScanner.Core;
using NellisScanner.Web.Components.Pages;
using NellisScanner.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NellisScanner.Web.Tests.Components
{
    public class AuctionsPageTests : TestContext
    {
        private readonly NellisScannerDbContext _dbContext;

        public AuctionsPageTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<NellisScannerDbContext>()
                .UseInMemoryDatabase(databaseName: "AuctionsPageTestDb_" + Guid.NewGuid())
                .Options;
            _dbContext = new NellisScannerDbContext(options);

            // Register the DbContext in the BUnit test context
            Services.AddSingleton(_dbContext);
        }

        [Fact]
        public void AuctionsPage_ShouldDisplayAuctions_WhenDataIsAvailable()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();

            // Assert
            // Wait for auctions to load
            cut.WaitForState(() => cut.FindAll("div.bg-white.rounded-lg").Count() > 0);
            
            // Should display 5 auction cards (default page size is 12, we have 5 items)
            var auctionCards = cut.FindAll("div.bg-white.rounded-lg.shadow-md");
            Assert.Equal(5, auctionCards.Count());
            
            // Check that auction titles are displayed
            var titles = auctionCards.Select(card => card.QuerySelector("h5.text-lg").TextContent).ToList();
            Assert.Contains("Laptop", titles);
            Assert.Contains("Smartphone", titles);
        }

        [Fact]
        public void AuctionsPage_ShouldFilterBySearchTerm()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();
            
            // Wait for initial load
            cut.WaitForState(() => cut.FindAll("div.bg-white.rounded-lg").Count() > 0);
            
            // Find the search input and enter a search term
            var searchInput = cut.Find("input[placeholder='Search by title...']");
            searchInput.Change("Laptop");
            
            // Click the search button
            var searchButton = cut.Find("button[type='button']");
            searchButton.Click();
            
            // Assert
            // Should now show only items with "Laptop" in the title
            var auctionCards = cut.FindAll("div.bg-white.rounded-lg.shadow-md");
            Assert.Single(auctionCards);
            Assert.Contains("Laptop", auctionCards[0].QuerySelector("h5").TextContent);
        }

        [Fact]
        public void AuctionsPage_ShouldSortAuctions()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();
            
            // Wait for initial load
            cut.WaitForState(() => cut.FindAll("div.bg-white.rounded-lg").Count() > 0);
            
            // Change the sort order to "Price: Low to High" (current_asc)
            var sortSelect = cut.Find("select");
            sortSelect.Change("current_asc");
            
            // Assert
            // After sorting, the first item should be the cheapest (Headphones at $49.99)
            var auctionCards = cut.FindAll("div.bg-white.rounded-lg.shadow-md");
            var firstCardPriceText = auctionCards[0].QuerySelector("div:contains('Current Price:') + span").TextContent;
            
            // The price should contain "$49.99"
            Assert.Contains("$49.99", firstCardPriceText);
            Assert.Contains("Headphones", auctionCards[0].QuerySelector("h5").TextContent);
        }

        [Fact]
        public void AuctionsPage_ShouldShowEmptyState_WhenNoResults()
        {
            // Arrange
            SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();
            
            // Wait for initial load
            cut.WaitForState(() => cut.FindAll("div.bg-white.rounded-lg").Count() > 0);
            
            // Search for something that doesn't exist
            var searchInput = cut.Find("input[placeholder='Search by title...']");
            searchInput.Change("NonExistentProduct");
            
            // Click the search button
            var searchButton = cut.Find("button[type='button']");
            searchButton.Click();
            
            // Assert
            // Should show the "No auctions found" message
            var noAuctionsMessage = cut.FindAll("div.bg-blue-50.text-blue-700");
            Assert.Contains(noAuctionsMessage, div => div.TextContent.Contains("No auctions found"));
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
                    Id = 101,
                    Title = "Laptop",
                    RetailPrice = 1200.00M,
                    CurrentPrice = 500.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-2),
                    CloseTime = now.AddDays(3),
                    LastUpdated = now,
                    BidCount = 12,
                    InventoryNumber = "INV-101"
                },
                new AuctionItem {
                    Id = 102,
                    Title = "Smartphone",
                    RetailPrice = 800.00M,
                    CurrentPrice = 350.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-1),
                    CloseTime = now.AddDays(2),
                    LastUpdated = now,
                    BidCount = 8,
                    InventoryNumber = "INV-102"
                },
                new AuctionItem {
                    Id = 103,
                    Title = "Tablet",
                    RetailPrice = 600.00M,
                    CurrentPrice = 200.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-3),
                    CloseTime = now.AddHours(5),
                    LastUpdated = now,
                    BidCount = 15,
                    InventoryNumber = "INV-103"
                },
                new AuctionItem {
                    Id = 104,
                    Title = "Headphones",
                    RetailPrice = 150.00M,
                    CurrentPrice = 49.99M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-4),
                    CloseTime = now.AddDays(1),
                    LastUpdated = now,
                    BidCount = 5,
                    InventoryNumber = "INV-104"
                },
                new AuctionItem {
                    Id = 105,
                    Title = "Gaming Console",
                    RetailPrice = 450.00M,
                    CurrentPrice = 200.00M,
                    State = AuctionState.Active,
                    OpenTime = now.AddDays(-5),
                    CloseTime = now.AddDays(4),
                    LastUpdated = now,
                    BidCount = 10,
                    InventoryNumber = "INV-105"
                }
            });
            _dbContext.SaveChanges();
        }
    }
}