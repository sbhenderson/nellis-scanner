# Use the official .NET SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:9.0-noble AS build
RUN apt update && apt install -y curl gnupg2 apt-transport-https ca-certificates lsb-release \
    && curl -sL https://deb.nodesource.com/setup_22.x | bash - \
    && apt install -y nodejs \
    && apt clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy csproj files and restore dependencies
COPY ["src/NellisScanner.Web/NellisScanner.Web.csproj", "src/NellisScanner.Web/"]
COPY ["src/NellisScanner.Core/NellisScanner.Core.csproj", "src/NellisScanner.Core/"]
RUN dotnet restore "src/NellisScanner.Web/NellisScanner.Web.csproj"

# Copy the rest of the source code
COPY ["src/NellisScanner.Web/", "src/NellisScanner.Web/"]
COPY ["src/NellisScanner.Core/", "src/NellisScanner.Core/"]

# Build the application
WORKDIR "/app/src/NellisScanner.Web"
RUN npm install && dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish --no-restore -c Release -o /app/publish

# Use the Ubuntu Chiseled ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble-chiseled-extra AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Copy the published application
COPY --from=publish /app/publish .

# Set the entry point
ENTRYPOINT ["dotnet", "NellisScanner.Web.dll"]