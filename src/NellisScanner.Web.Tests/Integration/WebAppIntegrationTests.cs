using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NellisScanner.Core;
using NellisScanner.Core.Models;
using NellisScanner.Web.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NellisScanner.Web.Tests.Integration
{
    public class NellisScannerWebFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"IntegrationTestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<NellisScannerDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add DbContext using in-memory database for testing
                services.AddDbContext<NellisScannerDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName);
                });

                // Mock NellisScanner service
                var mockNellisScanner = new Mock<NellisScanner.Core.NellisScanner>(
                    Mock.Of<HttpClient>(),
                    Mock.Of<ILogger<NellisScanner.Core.NellisScanner>>());

                // Setup default mock behavior
                mockNellisScanner.Setup(s => s.GetAuctionItemsAsync(
                        It.IsAny<Category>(),
                        It.IsAny<int>(),
                        It.IsAny<int>(),
                        It.IsAny<NellisLocations>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new SearchResponse
                    {
                        Products = new List<Product>
                        {
                            new Product
                            {
                                Id = 9001,
                                Title = "Test Integration Product",
                                RetailPrice = 799.99M,
                                CurrentPrice = 299.99M,
                                IsClosed = false,
                                OpenTime = DateTimeOffset.UtcNow.AddDays(-1),
                                CloseTime = DateTimeOffset.UtcNow.AddDays(5),
                                BidCount = 7,
                                InventoryNumber = "INT-TEST-001"
                            }
                        },
                        Algolia = new SearchMetadata { NumberOfPages = 1 }
                    });

                // Replace NellisScanner service with our mocked version
                services.Remove(services.SingleOrDefault(
                    d => d.ServiceType == typeof(NellisScanner.Core.NellisScanner)));
                services.AddTransient<NellisScanner.Core.NellisScanner>(sp => mockNellisScanner.Object);

                // Ensure database is created and seeded
                using (var scope = services.BuildServiceProvider().CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }
    }

    public class WebAppIntegrationTests : IClassFixture<NellisScannerWebFactory>
    {
        private readonly NellisScannerWebFactory _factory;

        public WebAppIntegrationTests(NellisScannerWebFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HomePage_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode(); // Status code 200-299
            Assert.Equal("text/html; charset=utf-8", 
                response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task AuctionsPage_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/auctions");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8",
                response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task ScheduledJob_ShouldUpdateDatabase()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
            var scannerService = scope.ServiceProvider.GetRequiredService<Services.AuctionScannerService>();

            // Act
            await scannerService.ScanElectronicsAsync(CancellationToken.None);

            // Assert
            var auctions = await dbContext.Auctions.ToListAsync();
            Assert.Single(auctions);
            Assert.Equal("Test Integration Product", auctions[0].Title);
            Assert.Equal(299.99M, auctions[0].CurrentPrice);
            Assert.Equal("INT-TEST-001", auctions[0].InventoryNumber);
        }

        [Fact]
        public async Task ClosedAuctions_ShouldBeMarkedAsClosed()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
            
            // Add an auction that's past its close time by more than 30 minutes
            var expiredAuction = new AuctionItem
            {
                Id = 8001,
                Title = "Expired Integration Test Auction",
                RetailPrice = 499.99M,
                CurrentPrice = 199.99M,
                State = AuctionState.Active, // Still marked as active even though it's expired
                OpenTime = DateTimeOffset.UtcNow.AddDays(-2),
                CloseTime = DateTimeOffset.UtcNow.AddMinutes(-31), // Past close time by more than 30 minutes
                LastUpdated = DateTimeOffset.UtcNow.AddHours(-1),
                InventoryNumber = "INT-TEST-EXPIRED"
            };
            dbContext.Auctions.Add(expiredAuction);
            await dbContext.SaveChangesAsync();

            // Setup mock scanner for price info
            var scannerService = scope.ServiceProvider.GetRequiredService<Services.AuctionScannerService>();
            
            // Mock the NellisScanner service
            var mockNellisScanner = new Mock<NellisScanner.Core.NellisScanner>(
                Mock.Of<HttpClient>(),
                Mock.Of<ILogger<NellisScanner.Core.NellisScanner>>());
                
            mockNellisScanner.Setup(s => s.GetAuctionPriceInfoAsync(
                    It.Is<int>(id => id == 8001),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuctionPriceInfo
                {
                    ProductId = 8001,
                    State = AuctionState.Closed,
                    Price = 219.99M,
                    InventoryNumber = "INT-TEST-EXPIRED",
                    TimeRetrieved = DateTimeOffset.UtcNow
                });
                
            // Replace the NellisScanner in the service provider
            var field = typeof(Services.AuctionScannerService).GetField("_nellisScanner", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(scannerService, mockNellisScanner.Object);

            // Act
            await scannerService.UpdateClosedAuctionsAsync(CancellationToken.None);

            // Assert
            var updatedAuction = await dbContext.Auctions.FindAsync(8001);
            Assert.NotNull(updatedAuction);
            Assert.Equal(AuctionState.Closed, updatedAuction.State);
            Assert.Equal(219.99M, updatedAuction.FinalPrice);
        }
    }
}