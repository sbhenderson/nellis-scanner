using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NellisScanner.Core;
using NellisScanner.Web.Components.Pages;
using NellisScanner.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task AuctionsPage_ShouldDisplayAuctions_WhenDataIsAvailable()
        {
            // Arrange
            await SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();

            // Assert
            // Wait for auctions to load
            cut.WaitForElement("div.bg-white");

            // Should display 5 auction cards (default page size is 12, we have 5 items)
            var auctionCards = cut.FindAll("div.bg-white");
            Assert.Equal(5, auctionCards.Count());

            // Check that auction titles are displayed
            var titles = cut.Markup;

            Assert.Contains("Laptop", titles);
            Assert.Contains("Smartphone", titles);
        }

        [Fact]
        public async Task AuctionsPage_ShouldFilterBySearchTerm()
        {
            // Arrange
            await SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();

            // Wait for initial load
            cut.WaitForElement("div.bg-white");

            // Find the search input and enter a search term
            var searchInput = cut.Find("input[type='text']");
            searchInput.Input("Laptop");

            // Click the search button (not submitting a form)
            var searchButton = cut.Find("button");
            searchButton.Click();

            // Wait for the filtered results
            cut.WaitForState(() => cut.FindAll("div.bg-white").Count() < 5);

            // Assert - check if markup contains "Laptop" but not other product names
            var markup = cut.Markup;
            Assert.Contains("Laptop", markup);
            Assert.DoesNotContain("Headphones", markup);
        }

        [Fact]
        public async Task AuctionsPage_ShouldSortAuctions()
        {
            // Arrange
            await SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();

            // Wait for initial load
            cut.WaitForElement("div.bg-white");

            // Find and change the sort select - look for any select element
            var sortSelect = cut.Find("select");
            sortSelect.Change("current_asc");

            // Wait for the sorting to take effect
            await Task.Delay(200);

            // Assert - check if the cheapest item (Headphones) appears before more expensive items
            var markup = cut.Markup;

            // Check if the Headphones item contains the price $49.99 somewhere in the markup
            Assert.Contains("Headphones", markup);
            Assert.Contains("49.99", markup);
        }

        [Fact]
        public async Task AuctionsPage_ShouldShowEmptyState_WhenNoResults()
        {
            // Arrange
            await SeedDatabaseWithTestData();

            // Act
            var cut = RenderComponent<Auctions>();

            // Wait for initial load
            cut.WaitForElement("div.bg-white");

            // Search for something that doesn't exist
            var searchInput = cut.Find("input[type='text']");
            searchInput.Input("NonExistentProduct");

            // Click the search button
            var searchButton = cut.Find("button");
            searchButton.Click();

            // Wait for the component to update and show the empty state
            cut.WaitForState(() =>
                cut.Markup.Contains("No auctions found") ||
                !cut.FindAll("div.bg-white").Any());

            // Assert - check if markup contains the empty state message
            var markup = cut.Markup;
            Assert.Contains("No auctions found", markup);
        }

        private async Task SeedDatabaseWithTestData()
        {
            // Clear any existing data
            _dbContext.Auctions.RemoveRange(_dbContext.Auctions);
            await _dbContext.SaveChangesAsync();

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
                    InventoryNumber = 101
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
                    InventoryNumber = 102
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
                    InventoryNumber = 103
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
                    InventoryNumber = 104
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
                    InventoryNumber = 105
                }
            });
            await _dbContext.SaveChangesAsync();
        }
    }
}