using System.ComponentModel;
using System.Reflection;

namespace NellisScanner.Core.Models;

/// <summary>
/// Helper methods for building URLs and working with query parameters
/// </summary>
internal static class UrlHelpers
{
    /// <summary>
    /// Gets the URL-friendly taxonomy string for a given category
    /// </summary>
    public static string GetCategoryTaxonomyParameter(Category category)
    {
        return category switch
        {
            Category.Electronics => "Electronics",
            Category.HomeAndHousehold => "Home+%26+Household+Essentials",
            Category.HomeImprovement => "Home+Improvement",
            Category.SmartHome => "Smart+Home",
            Category.OfficeAndSchool => "Office+%26+School+Supplies",
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
}