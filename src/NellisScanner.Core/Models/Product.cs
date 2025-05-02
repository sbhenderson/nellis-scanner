using System.Text.Json.Serialization;

namespace NellisScanner.Core.Models;

public class Product
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("grade")]
    public Grade? Grade { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("inventoryNumber")]
    public string? InventoryNumber { get; set; }
    public long InventoryNumberLong => InventoryNumber is not null ? long.Parse(InventoryNumber) : -1;
    [JsonPropertyName("photos")]
    public List<Photo> Photos { get; set; } = new();
    [JsonPropertyName("retailPrice")]
    public decimal RetailPrice { get; set; }
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
    [JsonPropertyName("bidCount")]
    public int BidCount { get; set; }
    [JsonPropertyName("currentPrice")]
    public decimal CurrentPrice { get; set; }
    [JsonPropertyName("openTime")]
    public DateTimeOffset OpenTime { get; set; }
    [JsonPropertyName("closeTime")]
    public DateTimeOffset CloseTime { get; set; }
    [JsonPropertyName("initialCloseTime")]
    public DateTimeOffset InitialCloseTime { get; set; }
    [JsonPropertyName("isClosed")]
    public bool IsClosed { get; set; }
    [JsonPropertyName("marketStatus")]
    public string? MarketStatus { get; set; }
    [JsonPropertyName("location")]
    public Location? Location { get; set; }
    [JsonPropertyName("originType")]
    public string? OriginType { get; set; }
    [JsonPropertyName("extensionInterval")]
    public int ExtensionInterval { get; set; }
    [JsonPropertyName("projectExtended")]
    public bool ProjectExtended { get; set; }
    
    // Navigation property for EF Core
    public List<PriceHistory> PriceHistory { get; set; } = new();
}

public class Grade
{
    [JsonPropertyName("categoryType")]
    public CategoryType? CategoryType { get; set; }
    [JsonPropertyName("assemblyType")]
    public TypeDescription? AssemblyType { get; set; }
    [JsonPropertyName("missingPartsType")]
    public TypeDescription? MissingPartsType { get; set; }
    [JsonPropertyName("functionalType")]
    public TypeDescription? FunctionalType { get; set; }
    [JsonPropertyName("conditionType")]
    public TypeDescription? ConditionType { get; set; }
    [JsonPropertyName("damageType")]
    public TypeDescription? DamageType { get; set; }
    [JsonPropertyName("packageType")]
    public TypeDescription? PackageType { get; set; }
    public decimal Rating { get; set; }
}

public class CategoryType
{
    public int Id { get; set; }
    public string? Description { get; set; }
}

public class TypeDescription
{
    public int Id { get; set; }
    public string? Description { get; set; }
}

public class Photo
{
    public string? Url { get; set; }
    public string? Name { get; set; }
    public string? FullPath { get; set; }
}

public class Location
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool Offsite { get; set; }
    public string? Timezone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public int ZipCode { get; set; }
    public override string ToString()
    {
        return Name ?? "Null";
    }
}

public class PriceHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public int BidCount { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    
    // Navigation property for EF Core
    public Product? Product { get; set; }
}