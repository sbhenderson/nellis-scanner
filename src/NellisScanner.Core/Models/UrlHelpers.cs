using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NellisScanner.Core.Models;

/// <summary>
/// Helper methods for building URLs and working with query parameters
/// </summary>
public static class UrlHelpers
{
    /// <summary>
    /// Gets the URL-friendly taxonomy string for a given category
    /// </summary>
    public static string GetCategoryTaxonomyParameter(Category category)
    {
        return category switch
        {
            Category.Electronics => "Electronics",
            Category.HomeAndHousehold => "Home & Household Essentials",
            Category.HomeImprovement => "Home Improvement",
            Category.SmartHome => "Smart Home",
            Category.OfficeAndSchool => "Office & School Supplies",
            Category.Automotive => "Automotive",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets a properly formatted pagination parameter for Nellis Auction
    /// </summary>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="pageNumber">Zero-based page number</param>
    public static string GetPaginationParameter(int pageSize = 120, int pageNumber = 0)
    {
        return $"s:{pageSize},n:{pageNumber}";
    }

    /// <summary>
    /// Get the description attribute value for an enum value
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute == null ? value.ToString() : attribute.Description;
    }
    /// <summary>
    /// Generates a URL for a product page on Nellis Auction
    /// </summary>
    /// <param name="productId">The ID of the product</param>
    /// <param name="productName">The name of the product (optional)</param>
    /// <returns>The URL to the product page</returns>
    public static string GenerateProductUrl(int productId, string? productName = null)
    {
        if (!string.IsNullOrWhiteSpace(productName))
        {
            // Create URL-friendly name by replacing spaces with dashes and removing special chars
            var urlFriendlyName = Regex.Replace(productName, @"[^a-zA-Z0-9\s-]", "")
                .Replace(" ", "-");
            return $"https://www.nellisauction.com/p/{urlFriendlyName}/{productId}";
        }

        return $"https://www.nellisauction.com/p/{productId}";
    }
}