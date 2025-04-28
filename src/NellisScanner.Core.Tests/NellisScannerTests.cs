using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace NellisScanner.Core.Tests;

public class NellisScannerTests
{
    private readonly ITestOutputHelper _output;
    private readonly NellisScanner _scanner;
    
    public NellisScannerTests(ITestOutputHelper output)
    {
        _output = output;
        var logger = new TestLogger<NellisScanner>(_output);
        _scanner = new NellisScanner(new HttpClient(), logger);
    }
    
    [Fact]
    public async Task GetElectronicsHighToLow_ReturnsProducts()
    {
        // Act
        var result = await _scanner.GetElectronicsHighToLowAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Products);
        
        // Output some information about the first product
        var firstProduct = result.Products.First();
        _output.WriteLine($"First product: {firstProduct.Title}");
        _output.WriteLine($"Retail price: ${firstProduct.RetailPrice}");
        _output.WriteLine($"Current price: ${firstProduct.CurrentPrice}");
        _output.WriteLine($"Close time: {firstProduct.CloseTime}");
    }
    
    [Fact]
    public async Task GetAuctionPriceInfo_ReturnsData()
    {
        // First get an auction ID from the search
        var products = await _scanner.GetElectronicsHighToLowAsync();
        var productId = products.Products.First().Id;
        var productTitle = products.Products.First().Title ?? string.Empty;
        
        // Act
        var result = await _scanner.GetAuctionPriceInfoAsync(productId, productTitle);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductId);
        Assert.True(result.Price > 0);
        
        // Output the results
        _output.WriteLine($"Product ID: {result.ProductId}");
        _output.WriteLine($"Price: ${result.Price}");
        _output.WriteLine($"State: {result.State}");
        _output.WriteLine($"Inventory Number: {result.InventoryNumber}");
    }
}

/// <summary>
/// Helper logger that outputs to Xunit's output
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;

    public TestLogger(ITestOutputHelper output)
    {
        _output = output;
    }

    IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        
        if (exception != null)
        {
            _output.WriteLine(exception.ToString());
        }
    }
    
    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        public void Dispose() { }
    }
}