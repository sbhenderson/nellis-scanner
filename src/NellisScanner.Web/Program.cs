using ApexCharts;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using NellisScanner.Core;
using NellisScanner.Web.Components;
using NellisScanner.Web.Data;
using NellisScanner.Web.Services;
using Serilog;

// Create a bootstrap logger for startup
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger(); // Bootstrap logger will be replaced later

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

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

    //https://github.com/apexcharts/Blazor-ApexCharts
    builder.Services.AddApexCharts(e =>
            {
                e.GlobalOptions = new ApexChartBaseOptions
                {
                    // Debug = true,
                    // Theme = new Theme { Palette = PaletteType.Palette6 }
                };
            });
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts();
    }

    // app.UseHttpsRedirection();

    app.UseStaticFiles();
    app.UseAntiforgery();

    // Configure Hangfire Dashboard
    app.UseHangfireDashboard(options: new DashboardOptions
    {
        Authorization = [new NellisScanner.Web.Utilities.DashboardNoAuthorizationFilter()],
        IgnoreAntiforgeryToken = true,
        IsReadOnlyFunc = context => true,
    });

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
        Log.Information("Running one-time tasks...");
        var scannerService = scope.ServiceProvider.GetRequiredService<AuctionScannerService>();
        await scannerService.ScanEachCategoryAsync(CancellationToken.None);
        await scannerService.UpdateClosedAuctionsAsync(CancellationToken.None);
        Log.Information("Completed one-time tasks...");
    }
    else
    {
        app.Run();
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
