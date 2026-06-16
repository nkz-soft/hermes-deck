# Build context: repo root (contains hermes-deck.sln, NuGet.config, and src/)

# Stage 1: Build and publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and NuGet config first for layer caching
COPY NuGet.config ./
COPY hermes-deck.sln ./

# Copy project files
COPY src/HermesDeck.Api/HermesDeck.Api.csproj src/HermesDeck.Api/
COPY src/HermesDeck.AppHost/HermesDeck.AppHost.csproj src/HermesDeck.AppHost/
COPY src/HermesDeck.ServiceDefaults/HermesDeck.ServiceDefaults.csproj src/HermesDeck.ServiceDefaults/

# Restore dependencies
RUN dotnet restore src/HermesDeck.Api/HermesDeck.Api.csproj

# Copy remaining source
COPY src/HermesDeck.Api/ src/HermesDeck.Api/
COPY src/HermesDeck.ServiceDefaults/ src/HermesDeck.ServiceDefaults/

# Publish
RUN dotnet publish src/HermesDeck.Api/HermesDeck.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HermesDeck.Api.dll"]
