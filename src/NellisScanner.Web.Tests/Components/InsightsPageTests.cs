using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Components.Pages;
using NellisScanner.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NellisScanner.Web.Tests.Components
{
    public class InsightsPageTests : BunitContext
    {
        private readonly NellisScannerDbContext _dbContext;

        public InsightsPageTests()
        {
            var options = new DbContextOptionsBuilder<NellisScannerDbContext>()
                .UseInMemoryDatabase(databaseName: "InsightsPageTestDb_" + Guid.NewGuid())
                .Options;
            _dbContext = new NellisScannerDbContext(options);
            Services.AddSingleton(_dbContext);
        }

        [Fact]
        public void Insights_ShouldRenderAllSections()
        {
            SeedData();
            var cut = Render<Insights>();
            cut.WaitForState(() => !cut.Markup.Contains("animate-spin"), TimeSpan.FromSeconds(5));

            var markup = cut.Markup;
            Assert.Contains("Savings by Category", markup);
            Assert.Contains("Most Relisted Items", markup);
            Assert.Contains("Closed Without Bids", markup);
        }

        [Fact]
        public void Insights_ShouldShowCategoryStats()
        {
            SeedData();
            var cut = Render<Insights>();
            cut.WaitForState(() => cut.Markup.Contains("Savings by Category") && !cut.Markup.Contains("animate-spin"), TimeSpan.FromSeconds(5));

            var markup = cut.Markup;
            // Electronics category label should appear
            Assert.Contains("Electronics", markup);
        }

        [Fact]
        public void Insights_ShouldSurfaceRelistedItems()
        {
            SeedData();
            var cut = Render<Insights>();
            cut.WaitForState(() => cut.Markup.Contains("Most Relisted Items") && !cut.Markup.Contains("animate-spin"), TimeSpan.FromSeconds(5));

            // Inventory #500 has two auctions, so it should appear in the relisted table
            var markup = cut.Markup;
            Assert.Contains("Relisted Widget", markup);
        }

        [Fact]
        public void Insights_ShouldSurfaceZeroBidCloses()
        {
            SeedData();
            var cut = Render<Insights>();
            cut.WaitForState(() => cut.Markup.Contains("Closed Without Bids") && !cut.Markup.Contains("animate-spin"), TimeSpan.FromSeconds(5));

            var markup = cut.Markup;
            Assert.Contains("Unsold Monitor", markup);
        }

        private void SeedData()
        {
            _dbContext.Auctions.RemoveRange(_dbContext.Auctions);
            _dbContext.Inventory.RemoveRange(_dbContext.Inventory);
            _dbContext.SaveChanges();

            var now = DateTimeOffset.UtcNow;

            // Active electronics auction
            _dbContext.Auctions.Add(new AuctionItem
            {
                Id = 10,
                Title = "Active Laptop",
                RetailPrice = 1000m,
                CurrentPrice = 400m,
                State = AuctionState.Active,
                OpenTime = now.AddDays(-1),
                CloseTime = now.AddDays(2),
                LastUpdated = now,
                BidCount = 4,
                InventoryNumber = 100,
                CategoryId = (int)Category.Electronics,
                CategoryName = "Electronics"
            });

            // Inventory item 500 relisted across two auctions
            _dbContext.Inventory.Add(new InventoryItem
            {
                InventoryNumber = 500,
                Description = "Relisted Widget",
                FirstSeen = now.AddDays(-10),
                LastSeen = now,
                CategoryName = "Electronics"
            });
            _dbContext.Auctions.Add(new AuctionItem
            {
                Id = 11,
                Title = "Relisted Widget - first listing",
                RetailPrice = 200m,
                CurrentPrice = 50m,
                FinalPrice = 55m,
                State = AuctionState.Closed,
                OpenTime = now.AddDays(-10),
                CloseTime = now.AddDays(-8),
                LastUpdated = now.AddDays(-8),
                BidCount = 2,
                InventoryNumber = 500,
                CategoryId = (int)Category.Electronics,
                CategoryName = "Electronics"
            });
            _dbContext.Auctions.Add(new AuctionItem
            {
                Id = 12,
                Title = "Relisted Widget - came back",
                RetailPrice = 200m,
                CurrentPrice = 60m,
                State = AuctionState.Active,
                OpenTime = now.AddDays(-3),
                CloseTime = now.AddDays(1),
                LastUpdated = now,
                BidCount = 1,
                InventoryNumber = 500,
                CategoryId = (int)Category.Electronics,
                CategoryName = "Electronics"
            });

            // Zero-bid closed auction
            _dbContext.Auctions.Add(new AuctionItem
            {
                Id = 13,
                Title = "Unsold Monitor",
                RetailPrice = 300m,
                CurrentPrice = 300m,
                FinalPrice = 0m,
                State = AuctionState.Closed,
                OpenTime = now.AddDays(-4),
                CloseTime = now.AddDays(-1),
                LastUpdated = now.AddDays(-1),
                BidCount = 0,
                InventoryNumber = 600,
                CategoryId = (int)Category.Electronics,
                CategoryName = "Electronics"
            });

            _dbContext.SaveChanges();
        }
    }
}
