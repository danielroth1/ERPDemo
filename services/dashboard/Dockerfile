# syntax=docker/dockerfile:1.7

# Build stage – run natively on the host (ARM64 Mac) and cross-compile for linux-x64
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages \
  NUGET_HTTP_CACHE_PATH=/root/.local/share/NuGet/v3-cache

# Copy csproj and restore dependencies
COPY DashboardAnalytics/*.csproj ./DashboardAnalytics/
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet restore -r linux-x64 DashboardAnalytics/DashboardAnalytics.csproj

# Copy source code and build
COPY DashboardAnalytics/. ./DashboardAnalytics/
WORKDIR /src/DashboardAnalytics
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet build DashboardAnalytics.csproj -c Release -r linux-x64 -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet publish DashboardAnalytics.csproj -c Release -r linux-x64 --self-contained false -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
LABEL org.opencontainers.image.source=https://github.com/danielroth1/ERPDemo

WORKDIR /app
EXPOSE 8080

# Enable remote debugging (VS Code) via vsdbg
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl \
  && mkdir -p /vsdbg \
  && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
  && apt-get purge -y --auto-remove curl \
  && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "DashboardAnalytics.dll"]
