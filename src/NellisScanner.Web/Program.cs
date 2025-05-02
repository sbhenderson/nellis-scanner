using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Web.Components;
using NellisScanner.Web.Data;
using NellisScanner.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure PostgreSQL and EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NellisScannerDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure HttpClient for NellisScanner
builder.Services.AddHttpClient<NellisScanner.Core.NellisScanner>();
builder.Services.AddTransient<INellisScanner, NellisScanner.Core.NellisScanner>();

// Configure Hangfire with PostgreSQL
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer();

// Register Scanner Service
builder.Services.AddScoped<AuctionScannerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Configure Hangfire Dashboard
app.UseHangfireDashboard();

// Configure recurring jobs
RecurringJob.AddOrUpdate<AuctionScannerService>(
    "scan-each-category",
    service => service.ScanEachCategoryAsync(CancellationToken.None),
    "0 */8 * * *");  // Run every 8 hours

RecurringJob.AddOrUpdate<AuctionScannerService>(
    "update-closed-auctions",
    service => service.UpdateClosedAuctionsAsync(CancellationToken.None),
    "*/30 * * * *");  // Run every 30 minutes

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Create a scope to apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NellisScannerDbContext>();
    // Only run migrations if we're using a relational database provider
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        db.Database.Migrate();
    }
}

if (app.Configuration.GetValue<bool?>("RunOnce") == true)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Running one-time tasks...");
    var scannerService = scope.ServiceProvider.GetRequiredService<AuctionScannerService>();
    await scannerService.ScanEachCategoryAsync(CancellationToken.None);
    await scannerService.UpdateClosedAuctionsAsync(CancellationToken.None);
    logger.LogInformation("Completed one-time tasks...");
}
else
{
    app.Run();
}
