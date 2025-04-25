using System.Text.Json.Serialization;

namespace NellisScanner.Core.Models;

public class SearchResponse
{
    [JsonPropertyName("currentShoppingLocation")]
    public Location? CurrentShoppingLocation { get; set; }
    
    [JsonPropertyName("discoverySource")]
    public string? DiscoverySource { get; set; }
    
    [JsonPropertyName("facets")]
    public Facets? Facets { get; set; }
    
    [JsonPropertyName("filterCount")]
    public int FilterCount { get; set; }
    
    [JsonPropertyName("algolia")]
    public AlgoliaInfo? Algolia { get; set; }
    
    [JsonPropertyName("products")]
    public List<Product> Products { get; set; } = new();
    
    [JsonPropertyName("trendingProducts")]
    public List<Product> TrendingProducts { get; set; } = new();
    
    [JsonPropertyName("searchResultsCount")]
    public int SearchResultsCount { get; set; }
    
    [JsonPropertyName("selectedFilters")]
    public List<string>? SelectedFilters { get; set; }
    
    [JsonPropertyName("autocompleteFilters")]
    public AutocompleteFilters? AutocompleteFilters { get; set; }
}

public class Facets
{
    [JsonPropertyName("category")]
    public Dictionary<string, int>? Category { get; set; }
    
    [JsonPropertyName("locationName")]
    public Dictionary<string, int>? LocationName { get; set; }
    
    [JsonPropertyName("auctionEventName")]
    public Dictionary<string, int>? AuctionEventName { get; set; }
    
    [JsonPropertyName("auctionEventType")]
    public Dictionary<string, int>? AuctionEventType { get; set; }
    
    [JsonPropertyName("starRating")]
    public Dictionary<string, int>? StarRating { get; set; }
    
    [JsonPropertyName("suggestedRetail")]
    public Dictionary<string, int>? SuggestedRetail { get; set; }
    
    [JsonPropertyName("taxonomy1")]
    public Dictionary<string, int>? Taxonomy1 { get; set; }
    
    [JsonPropertyName("taxonomy2")]
    public Dictionary<string, int>? Taxonomy2 { get; set; }
}

public class AlgoliaInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("nbPages")]
    public int NumberOfPages { get; set; }
    
    [JsonPropertyName("nbHits")]
    public int NumberOfHits { get; set; }
    
    [JsonPropertyName("query")]
    public string? Query { get; set; }
    
    [JsonPropertyName("hitsPerPage")]
    public int HitsPerPage { get; set; }
    
    [JsonPropertyName("queryID")]
    public string? QueryId { get; set; }
    
    [JsonPropertyName("indexUsed")]
    public string? IndexUsed { get; set; }
}

public class AutocompleteFilters
{
    [JsonPropertyName("filters")]
    public string? Filters { get; set; }
    
    [JsonPropertyName("facetFilters")]
    public List<List<string>>? FacetFilters { get; set; }
    
    [JsonPropertyName("numericFilters")]
    public List<string>? NumericFilters { get; set; }
    
    [JsonPropertyName("filterCount")]
    public int FilterCount { get; set; }
}