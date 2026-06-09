# syntax=docker/dockerfile:1
# Multi-stage build for the RBMS ASP.NET Core 9 Web API.
# Build context is expected to be the repo root (so `backend/` is visible).
#   docker build -f infra/docker/api.Dockerfile -t rbms-api .

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY backend/*.sln ./backend/
COPY backend/ ./backend/
WORKDIR /src/backend
RUN dotnet restore "src/RBMS.Api/RBMS.Api.csproj"

# Publish a trimmed, release build.
RUN dotnet publish "src/RBMS.Api/RBMS.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl is used by the container/ALB health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Run as a non-root user.
RUN groupadd --system --gid 1001 appgroup \
    && useradd --system --uid 1001 --gid appgroup appuser

COPY --from=build /app/publish ./
RUN chown -R appuser:appgroup /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RBMS.Api.dll"]
