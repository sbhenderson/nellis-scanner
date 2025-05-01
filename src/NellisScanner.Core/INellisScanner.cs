using NellisScanner.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NellisScanner.Core
{
    public interface INellisScanner
    {
        Task<SearchResponse> GetAuctionItemsAsync(
            Category category = Category.Electronics, 
            int pageNumber = 0, 
            int pageSize = 120, 
            NellisLocations location = NellisLocations.Houston, 
            string sortBy = "retail_price_desc", 
            CancellationToken cancellationToken = default);

        Task<SearchResponse> GetElectronicsHighToLowAsync(
            int page = 0, 
            NellisLocations location = NellisLocations.Houston,
            CancellationToken cancellationToken = default);

        Task<Product?> GetProductAsync(
            int productId, 
            CancellationToken cancellationToken = default);

        Task<AuctionPriceInfo> GetAuctionPriceInfoAsync(
            int productId, 
            string productName = "Always-Have-Something",
            CancellationToken cancellationToken = default);

        IAsyncEnumerable<ProductUpdate> MonitorProductUpdatesAsync(
            int productId,
            CancellationToken cancellationToken = default);
    }
}