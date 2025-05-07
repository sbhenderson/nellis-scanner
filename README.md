# Nellis Scanner

A .NET 9 application for tracking auctions on Nellis Auctions. The goal is to determine the quality of the marketplace by measuring a few public indicators i.e. the price.

1. Grab the first X listings on some frequency
2. Keep track of future listings for a given inventory ID
3. Store data in a PostgreSQL instance
4. Analyze the data and draw some conclusions

I am currently hosting it off [my homelab here](https://nellis-scanner.external.henderson.engineering/), but this is obviously subject to availability and whether this persists or not.

## Foreword

This repository is 99% agentic AI using GitHub Copilot with Claude Sonnet 3.7. This is merely a test project.

## Background

I pass by the Nellis Auctions building on 99 in the Katy area often, and on weekends, I see a **lot** of people picking up their orders. Far more than I would have expected although when you look at the number of items in the inventory, it becomes somewhat obvious why this could be the case. So I created an account and just watched a few auctions. These are confusing (neither good nor bad) signs:

1. A few items that supposedly closed "came back" a day or two later. Were there that many returns? Are there hidden reserve prices?
2. The 15% buyer's premium is not added to the displayed cost which may give you a false sense of a "deal" when comparing with other retailers.

The only way to draw conclusions is to have data, and that's the motivation for this project.

## Features

- **Real-time Auction Monitoring**: Tracks auctions from Nellis Auctions with retail price high-to-low sorting.
- **Price History Tracking**: Records price and bid history for auctions over time.
- **Automatic Scanning**: Scans for new auctions every 5 minutes, with more frequent checks for auctions closing soon.
- **Web Interface**: Blazor Server-rendered UI for viewing current auctions and their price history.

## Architecture

This solution consists of two main components:

1. **NellisScanner.Core**: .NET 9 class library for parsing Nellis Auction data, containing the data models and parsing logic.
2. **NellisScanner.Web**: ASP.NET Core 9 Blazor Server application that uses the core library and provides a web interface.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop) and Docker Compose

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

## References

1. This [repository](https://github.com/Brudderbot/nellisAuction) revealed the hidden query parameter that returns data in JSON instead of HTML: `&_data=routes%2Fsearch` as well as the importance of the cookie for location.
2. Tailwind CSS v4 was more of a pain to get installed correctly. Used [this](https://steven-giesel.com/blogPost/364c43d2-b31e-4377-8001-ac75ce78cdc6) and [this](https://www.billtalkstoomuch.com/2025/02/12/installing-tailwind-css-v4-in-a-blazor-webapp/) as guidance. Saw some interesting discussion in this [thread](https://github.com/tailwindlabs/tailwindcss/discussions/15679).

## Notice

Relating to Nellis Auction, the website/company/brand, this project reserves no rights relating to it and any content downloaded through the course of this work. Their [terms](https://www.nellisauction.com/terms).

## License

[MIT](LICENSE)
