# Nellis Scanner - Copilot Context

## Project Overview
Nellis Scanner is a .NET 9 application for tracking auctions on Nellis Auctions, specifically designed to monitor and analyze auction data over time. The application scrapes the Nellis Auctions website, stores data in PostgreSQL, and provides a web interface to view the data.

## Key Components

### NellisScanner.Core
- Contains the core logic for interacting with Nellis Auctions
- Defines data models like `Product`, `Category`, `Location`
- Implements the `INellisScanner` interface for auction data retrieval

### NellisScanner.Web
- Blazor Server application with interactive components
- Uses Tailwind CSS v4 for styling
- Implements recurring job scheduling with Hangfire
- Utilizes Entity Framework Core with PostgreSQL
- ApexCharts for data visualization

## Technical Stack
- .NET 9 (C# 12)
- ASP.NET Core 9 Blazor Server
- PostgreSQL database
- Entity Framework Core
- Hangfire for background processing
- Tailwind CSS v4
- Serilog for logging
- Docker & Docker Compose for deployment

## Key Features
- Real-time auction monitoring (especially electronics category)
- Price history tracking for items over time
- Automatic scanning on recurring schedules (every 8 hours for categories, every 30 minutes for closed auctions)
- Web dashboard for data visualization

## Project Structure
- `src/NellisScanner.Core/` - Core library for auction interaction
- `src/NellisScanner.Web/` - Blazor web application
- `src/NellisScanner.Core.Tests/` - Tests for core functionality
- `src/NellisScanner.Web.Tests/` - Tests for web application

## Database
- Uses PostgreSQL for data storage
- Entity Framework Core for ORM
- Migrations are applied automatically on startup

## Development Workflow
- Tailwind CSS is built using npm script during pre-build event
- Docker Compose is available for running the application with PostgreSQL

## Notes
- Project was 99% built with GitHub Copilot and Claude Sonnet 3.7
- Default build task runs `dotnet build` on the solution file
- The application is designed to run on a schedule to gather auction data