# Nellis Scanner

A .NET 9 application for tracking electronics auctions on Nellis Auctions.

## Features

- **Real-time Auction Monitoring**: Tracks electronics auctions from Nellis Auctions with retail price high-to-low sorting.
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
   ```
   git clone https://github.com/yourusername/nellis-scanner.git
   cd nellis-scanner
   ```

2. Set environment variables for the PostgreSQL database (optional):
   ```
   # Linux/macOS
   export POSTGRES_USER=your_username
   export POSTGRES_PASSWORD=your_secure_password

   # Windows (PowerShell)
   $env:POSTGRES_USER="your_username"
   $env:POSTGRES_PASSWORD="your_secure_password"
   ```

   If you don't set these variables, the default values will be used:
   - Username: nellis_user
   - Password: nellis_password

3. Build and start the containers:
   ```
   docker-compose up -d
   ```

4. Access the application at:
   - Web UI: http://localhost:8080
   - Hangfire Dashboard: http://localhost:8080/hangfire

### Development Setup

1. Clone the repository and navigate to the project directory.

2. Install the .NET EF Core tools if you haven't already:
   ```
   dotnet tool install --global dotnet-ef
   ```

3. Start a PostgreSQL database (using Docker or install locally).

4. Update the connection string in `src/NellisScanner.Web/appsettings.json` if needed.

5. Run database migrations:
   ```
   cd src/NellisScanner.Web
   dotnet ef database update
   ```

6. Run the application:
   ```
   dotnet run
   ```

## License

[MIT](LICENSE)
