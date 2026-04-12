# syntax=docker/dockerfile:1.7

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG RID=linux-x64
WORKDIR /src

ENV NUGET_PACKAGES=/root/.nuget/packages \
  NUGET_HTTP_CACHE_PATH=/root/.local/share/NuGet/v3-cache

# Copy csproj and restore dependencies (shared projects first for layer caching)
COPY shared/ERP.Contracts/*.csproj ./shared/ERP.Contracts/
COPY shared/ServiceDefaults/*.csproj ./shared/ServiceDefaults/
COPY orchestration/Orchestration/*.csproj ./orchestration/Orchestration/
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet restore -r $RID orchestration/Orchestration/Orchestration.csproj

# Copy source and build
COPY shared/ERP.Contracts/. ./shared/ERP.Contracts/
COPY shared/ServiceDefaults/. ./shared/ServiceDefaults/
COPY orchestration/Orchestration/. ./orchestration/Orchestration/
WORKDIR /src/orchestration/Orchestration
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet build Orchestration.csproj -c Release -r $RID -o /app/build --no-restore

FROM build AS publish
RUN --mount=type=cache,id=nuget-packages,target=/root/.nuget/packages,sharing=shared \
  --mount=type=cache,id=nuget-http-cache,target=/root/.local/share/NuGet/v3-cache,sharing=shared \
  dotnet publish Orchestration.csproj -c Release -r $RID --self-contained false -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orchestration.dll"]
