using System.ComponentModel;

namespace NellisScanner.Core.Models;

/// <summary>
/// Categories available for filtering in Nellis Auction
/// </summary>
public enum Category
{
    [Description("All Categories")]
    All,
    
    [Description("Electronics")]
    Electronics,
    
    [Description("Home & Household Essentials")]
    HomeAndHousehold,
    
    [Description("Home Improvement")]
    HomeImprovement,
    
    [Description("Smart Home")]
    SmartHome,
    
    [Description("Office & School Supplies")]
    OfficeAndSchool,
    
    [Description("Automotive")]
    Automotive
}