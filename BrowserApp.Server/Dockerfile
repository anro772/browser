# syntax=docker/dockerfile:1.6
#
# Slate backend (BrowserApp.Server) — multi-stage build that produces a small
# ASP.NET runtime image. Designed for Fly.io but works anywhere that runs OCI
# containers.
#
# Build context: must be the repo root (not BrowserApp.Server/) because we need
# to copy three sibling projects: Server, Core, Data.
#
# Build locally:
#   docker build -f BrowserApp.Server/Dockerfile -t slate-api .
# Run locally:
#   docker run -p 8080:8080 -e ConnectionStrings__PostgreSQL=... slate-api

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Copy csproj files first as a discrete layer so dotnet restore caches.
COPY ["BrowserApp.Server/BrowserApp.Server.csproj", "BrowserApp.Server/"]
COPY ["BrowserApp.Core/BrowserApp.Core.csproj",     "BrowserApp.Core/"]
COPY ["BrowserApp.Data/BrowserApp.Data.csproj",     "BrowserApp.Data/"]
RUN dotnet restore "BrowserApp.Server/BrowserApp.Server.csproj"

# Now copy the rest of the source for build + publish.
COPY BrowserApp.Server/ BrowserApp.Server/
COPY BrowserApp.Core/   BrowserApp.Core/
COPY BrowserApp.Data/   BrowserApp.Data/

WORKDIR /src/BrowserApp.Server
RUN dotnet publish "BrowserApp.Server.csproj" \
        -c Release \
        -o /app/publish \
        --no-restore \
        /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime
WORKDIR /app

# Fly.io expects the app to listen on the port advertised in fly.toml (8080).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=1

EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BrowserApp.Server.dll"]
