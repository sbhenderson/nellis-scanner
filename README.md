# Nellis Scanner

A .NET 10 application for tracking auctions on Nellis Auctions. The goal is to determine the quality of the marketplace by measuring a few public indicators i.e. the price.

1. Grab the first X listings on some frequency
2. Keep track of future listings for a given inventory ID
3. Store data in a PostgreSQL instance
4. Analyze the data and draw some conclusions

I am currently hosting it off [my homelab here](https://nellis-scanner.external.henderson.engineering/), but this is obviously subject to availability and whether this persists or not.

## Foreword

This repository is 99% agentic AI. It was originally built with GitHub Copilot on Claude Sonnet 3.5/3.7, and was later modernized to .NET 10 with a refreshed UI using newer models. This is merely a test project.

## Background

I pass by the Nellis Auctions building on 99 in the Katy area often, and on weekends, I see a **lot** of people picking up their orders. Far more than I would have expected although when you look at the number of items in the inventory, it becomes somewhat obvious why this could be the case. So I created an account and just watched a few auctions. These are confusing (neither good nor bad) signs:

1. A few items that supposedly closed "came back" a day or two later. Were there that many returns? Are there hidden reserve prices?
2. The 15% buyer's premium is not added to the displayed cost which may give you a false sense of a "deal" when comparing with other retailers.

The only way to draw conclusions is to have data, and that's the motivation for this project.

## Features

- **Real-time Auction Monitoring**: Tracks auctions from Nellis Auctions with retail price high-to-low sorting.
- **Price History Tracking**: Records price and bid history for auctions over time.
- **Discount Insights**: Each listing shows the current discount vs. retail price.
- **Automatic Scanning**: Scans for new auctions on a recurring schedule, with more frequent checks for auctions closing soon.
- **Web Interface**: Blazor Server-rendered UI with a modern Tailwind CSS v4 design for viewing current auctions and their price history.

## Architecture

This solution consists of two main components:

1. **NellisScanner.Core**: .NET 10 class library for parsing Nellis Auction data, containing the data models and parsing logic.
2. **NellisScanner.Web**: ASP.NET Core 10 Blazor Server application that uses the core library and provides a web interface.

Data is stored in PostgreSQL via Entity Framework Core 10, background jobs are scheduled with Hangfire, and charts are rendered with ApexCharts (Blazor-ApexCharts).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop) and Docker Compose
- [Node.js](https://nodejs.org/) (for Tailwind CSS builds during development)

## Running the Application

### Using Docker Compose (Recommended)

1. Clone the repository:

   ```sh
   git clone https://github.com/yourusername/nellis-scanner.git
   cd nellis-scanner
   ```

2. Set environment variables for the PostgreSQL database (optional):

   ```sh
   # Linux/macOS
   export POSTGRES_USER=your_username
   export POSTGRES_PASSWORD=your_secure_password
   ```

   ```powershell
   # Windows (PowerShell)
   $env:POSTGRES_USER="your_username"
   $env:POSTGRES_PASSWORD="your_secure_password"
   ```

   If you don't set these variables, the default values will be used:
   - Username: nellis_user
   - Password: nellis_password

3. Build and start the containers:

   ```sh
   docker compose up -d
   ```

4. Access the application at:
   - Web UI: http://localhost:8080
   - Hangfire Dashboard: http://localhost:8080/hangfire

### Development Setup

1. Clone the repository and navigate to the project directory.

2. Install the .NET EF Core tools if you haven't already:

   ```sh
   dotnet tool install --global dotnet-ef
   ```

3. Start a PostgreSQL database (using Docker or install locally).

4. Update the connection string in `src/NellisScanner.Web/appsettings.json` if needed.

5. Run database migrations:

   ```sh
   cd src/NellisScanner.Web
   dotnet ef database update
   ```

6. Run the application:

   ```sh
   dotnet run
   ```

## Modernization Notes (.NET 10)

The codebase was upgraded from .NET 9 to .NET 10. Notable changes:

- All projects target `net10.0` and all NuGet packages were bumped to their .NET 10-compatible versions (EF Core 10, Npgsql 10, Serilog 10, Blazor-ApexCharts 7, bUnit 2, xunit, etc.).
- **Serilog 10**: the bootstrap logger now uses `CreateLogger()` with `preserveStaticLogger: true` instead of the removed/deprecated frozen `CreateBootstrapLogger()` pattern.
- **bUnit 2**: `TestContext` → `BunitContext` and `RenderComponent<T>()` → `Render<T>()`.
- **EF Core 10**: the in-memory bulk-upsert helper no longer copies navigation properties via reflection — setting a navigation to `null` now marks the entity as `Deleted`.
- **Docker**: base images updated to `mcr.microsoft.com/dotnet/sdk:10.0-noble` and `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`.
- **UI facelift**: refreshed layout, navigation, dashboard cards, auction cards (with discount badges), and tables using a modern Tailwind CSS v4 theme.

## References

1. This [repository](https://github.com/Brudderbot/nellisAuction) revealed the hidden query parameter that returns data in JSON instead of HTML: `&_data=routes%2Fsearch` as well as the importance of the cookie for location.
2. Tailwind CSS v4 was more of a pain to get installed correctly. Used [this](https://steven-giesel.com/blogPost/364c43d2-b31e-4377-8001-ac75ce78cdc6) and [this](https://www.billtalkstoomuch.com/2025/02/12/installing-tailwind-css-v4-in-a-blazor-webapp/) as guidance. Saw some interesting discussion in this [thread](https://github.com/tailwindlabs/tailwindcss/discussions/15679).

## Notice

Relating to Nellis Auction, the website/company/brand, this project reserves no rights relating to it and any content downloaded through the course of this work. Their [terms](https://www.nellisauction.com/terms).

## License

[MIT](LICENSE)
