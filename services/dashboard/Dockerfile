# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY DashboardAnalytics/*.csproj ./DashboardAnalytics/
RUN dotnet restore DashboardAnalytics/DashboardAnalytics.csproj

# Copy source code and build
COPY DashboardAnalytics/. ./DashboardAnalytics/
WORKDIR /src/DashboardAnalytics
RUN dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
EXPOSE 8080

# Copy published output
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "DashboardAnalytics.dll"]
