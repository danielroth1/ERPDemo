# syntax=docker/dockerfile:1.7

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages \
  NUGET_HTTP_CACHE_PATH=/root/.local/share/NuGet/v3-cache

# Copy csproj and restore dependencies
COPY ApiGateway/*.csproj ./ApiGateway/
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=locked \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=locked \
  dotnet restore ApiGateway/ApiGateway.csproj

# Copy source code and build
COPY ApiGateway/. ./ApiGateway/
WORKDIR /src/ApiGateway
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=locked \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=locked \
  dotnet build -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=locked \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=locked \
  dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

# Enable remote debugging (VS Code) via vsdbg
RUN apk add --no-cache bash curl \
  && mkdir -p /vsdbg \
  && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg \
  && apk del bash curl

# Copy published output
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "ApiGateway.dll"]
