# syntax=docker/dockerfile:1.7

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages \
  NUGET_HTTP_CACHE_PATH=/root/.local/share/NuGet/v3-cache

# Copy shared projects first
COPY ../shared/ERP.Contracts/*.csproj ./shared/ERP.Contracts/
COPY ../shared/ServiceDefaults/*.csproj ./shared/ServiceDefaults/
COPY Orchestration/*.csproj ./Orchestration/
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet restore -r linux-x64 Orchestration/Orchestration.csproj

# Copy source and build
COPY ../shared/ERP.Contracts/. ./shared/ERP.Contracts/
COPY ../shared/ServiceDefaults/. ./shared/ServiceDefaults/
COPY Orchestration/. ./Orchestration/
WORKDIR /src/Orchestration
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet build Orchestration.csproj -c Release -r linux-x64 -o /app/build --no-restore

FROM build AS publish
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet publish Orchestration.csproj -c Release -r linux-x64 --self-contained false -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orchestration.dll"]
