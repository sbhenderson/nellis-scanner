using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NellisScanner.Core.Models;
using System.Text.RegularExpressions;

namespace NellisScanner.Core;

public class NellisScanner
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NellisScanner> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public NellisScanner(HttpClient httpClient, ILogger<NellisScanner> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = 
            {
                new JsonStringEnumConverter(),
                // Custom converter for the Date object in JSON
                new DateTimeOffsetConverter()
            }
        };
    }
    
    /// <summary>
    /// Fetches current auction results for Electronics sorted by retail price (high to low)
    /// </summary>
    /// <param name="page">Page number starting from 0</param>
    /// <param name="location">Location name, default is "Houston, TX"</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SearchResponse containing auction products</returns>
    public async Task<SearchResponse> GetElectronicsHighToLowAsync(
        int page = 0, 
        string location = "Houston, TX",
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"https://www.nellisauction.com/search?query=&Taxonomy+Level+1=Electronics&sortBy=retail_price_desc&page={page}&_data=routes%2Fsearch";
            _logger.LogInformation("Fetching electronics data from page {Page}", page);
            
            var response = await _httpClient.GetFromJsonAsync<SearchResponse>(url, _jsonOptions, cancellationToken);
            if (response == null)
            {
                _logger.LogWarning("Received null response from Nellis Auction API");
                return new SearchResponse();
            }
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching electronics data from page {Page}", page);
            throw;
        }
    }
    
    /// <summary>
    /// Fetches a specific product by ID
    /// </summary>
    /// <param name="productId">The ID of the product to fetch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details</returns>
    public async Task<Product?> GetProductAsync(
        int productId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"https://www.nellisauction.com/p/{productId}/_data";
            _logger.LogInformation("Fetching product data for ID {ProductId}", productId);
            
            var response = await _httpClient.GetFromJsonAsync<Product>(url, _jsonOptions, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product data for ID {ProductId}", productId);
            throw;
        }
    }
    
    /// <summary>
    /// Retrieves auction price information by parsing the HTML product page
    /// </summary>
    /// <param name="productId">The ID of the product</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AuctionPriceInfo containing current or final price and auction state</returns>
    public async Task<AuctionPriceInfo> GetAuctionPriceInfoAsync(
        int productId,
        string productName = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // URL-friendly product name is optional but helps create a valid URL
            string url;
            if (!string.IsNullOrWhiteSpace(productName))
            {
                // Create URL-friendly name by replacing spaces with dashes and removing special chars
                var urlFriendlyName = Regex.Replace(productName, @"[^a-zA-Z0-9\s-]", "")
                    .Replace(" ", "-");
                url = $"https://www.nellisauction.com/p/{urlFriendlyName}/{productId}";
            }
            else
            {
                url = $"https://www.nellisauction.com/p/{productId}";
            }
            
            _logger.LogInformation("Fetching HTML page for product ID {ProductId}", productId);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            
            // Parse HTML to extract price information
            var priceInfo = ParseHtmlForPriceInfo(html);
            priceInfo.ProductId = productId;
            
            return priceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching and parsing HTML for product ID {ProductId}", productId);
            throw;
        }
    }
    
    /// <summary>
    /// Parses the HTML content to extract price and auction state
    /// </summary>
    /// <param name="html">The HTML content of the product page</param>
    /// <returns>AuctionPriceInfo with extracted information</returns>
    private AuctionPriceInfo ParseHtmlForPriceInfo(string html)
    {
        var result = new AuctionPriceInfo
        {
            TimeRetrieved = DateTimeOffset.UtcNow
        };
        
        // Check if auction is closed ("Won For" or "Ended")
        var endedMatch = Regex.Match(html, @"<strong class="""">(Ended|Won For)<\/strong>");
        if (endedMatch.Success)
        {
            result.State = AuctionState.Closed;
            // Look for final price
            var priceMatch = Regex.Match(html, @"<p class=""text-gray-900 font-semibold line-clamp-1[^>]+>(\$[0-9,]+)<\/p>");
            if (priceMatch.Success && priceMatch.Groups.Count > 1)
            {
                string priceText = priceMatch.Groups[1].Value.Replace("$", "").Replace(",", "");
                if (decimal.TryParse(priceText, out decimal price))
                {
                    result.Price = price;
                }
            }
        }
        else
        {
            // Active auction
            result.State = AuctionState.Active;
            
            // Look for current price
            var priceMatch = Regex.Match(html, @"<strong class="""">CURRENT PRICE<\/strong>[^<]*<\/p>[^<]*<p[^>]+>(\$[0-9,]+)<\/p>");
            if (priceMatch.Success && priceMatch.Groups.Count > 1)
            {
                string priceText = priceMatch.Groups[1].Value.Replace("$", "").Replace(",", "");
                if (decimal.TryParse(priceText, out decimal price))
                {
                    result.Price = price;
                }
            }
        }
        
        // Extract inventory number if present
        var inventoryMatch = Regex.Match(html, @"<p class=""text-left font-medium"">Inventory Number<\/p>\s*<p>([0-9]+)<\/p>");
        if (inventoryMatch.Success && inventoryMatch.Groups.Count > 1)
        {
            result.InventoryNumber = inventoryMatch.Groups[1].Value;
        }
        
        return result;
    }
    
    /// <summary>
    /// Creates a stream to monitor live updates for a specific product
    /// </summary>
    /// <param name="productId">The ID of the product to monitor</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of price updates</returns>
    public async IAsyncEnumerable<ProductUpdate> MonitorProductUpdatesAsync(
        int productId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string url = $"https://sse.nellisauction.com/live-products?productId={productId}";
        _logger.LogInformation("Monitoring updates for product ID {ProductId}", productId);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Accept", "text/event-stream");
        
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to SSE stream for product ID {ProductId}", productId);
            yield break;
        }
        
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        string? line;
        
        while (!cancellationToken.IsCancellationRequested && (line = await reader.ReadLineAsync()) != null)
        {
            // Process SSE format
            if (line.StartsWith("data:"))
            {
                string data = line.Substring(5).Trim();
                if (data.StartsWith("connected") || data == "ping")
                {
                    // Connection messages, ignore
                    continue;
                }
                
                ProductUpdate? update = null;
                try
                {
                    update = JsonSerializer.Deserialize<ProductUpdate>(data, _jsonOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing product update: {Data}", data);
                    continue;
                }
                
                if (update != null)
                {
                    yield return update;
                }
            }
        }
    }
}

public class ProductUpdate
{
    public int ProductId { get; set; }
    public decimal CurrentPrice { get; set; }
    public int BidCount { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public enum AuctionState
{
    Active,
    Closed
}

public class AuctionPriceInfo
{
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public string? InventoryNumber { get; set; }
    public AuctionState State { get; set; }
    public DateTimeOffset TimeRetrieved { get; set; }
}

/// <summary>
/// Custom JSON converter for the Nellis Auction date format
/// </summary>
public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Custom format from Nellis: { "__type": "Date", "value": "2025-04-25T02:01:48.208Z" }
            string? type = null;
            string? value = null;
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;
                
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()!;
                    reader.Read();
                    
                    if (propertyName == "__type")
                        type = reader.GetString();
                    else if (propertyName == "value")
                        value = reader.GetString();
                }
            }
            
            if (type == "Date" && value != null)
            {
                return DateTimeOffset.Parse(value);
            }
            
            return DateTimeOffset.MinValue;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            return DateTimeOffset.Parse(reader.GetString()!);
        }
        
        return DateTimeOffset.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("__type", "Date");
        writer.WriteString("value", value.ToString("o"));
        writer.WriteEndObject();
    }
}