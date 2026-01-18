# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore
COPY ["BrowserApp.Server/BrowserApp.Server.csproj", "BrowserApp.Server/"]
RUN dotnet restore "BrowserApp.Server/BrowserApp.Server.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/BrowserApp.Server"
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrowserApp.Server.dll"]
