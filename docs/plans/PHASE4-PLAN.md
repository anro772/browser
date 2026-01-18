# Phase 4: Server + Marketplace Implementation Plan

**Status:** Planning
**Goal:** Central server for rules marketplace with Docker deployment
**Timeline:** ~2 weeks

---

## Overview

Phase 4 introduces the **server infrastructure** that enables:
- Rules marketplace (browse, upload, download community rules)
- Foundation for channels (Phase 5)
- Foundation for AI copilot (Phase 6)

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Client (Browser App)                      │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ MarketplaceView.xaml    │    SettingsView.xaml        │  │
│  │ - Browse rules          │    - Server URL config      │  │
│  │ - Search/filter         │    - Connection status      │  │
│  │ - Install/uninstall     │    - Username               │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ ApiClient.cs            │    RuleSyncService.cs       │  │
│  │ - HTTP to server        │    - Pull marketplace rules │  │
│  │ - Error handling        │    - Cache locally          │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                         HTTP/JSON
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Server (Docker)                           │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ .NET 8 Web API (Port 5000)                            │  │
│  │                                                        │  │
│  │ GET  /api/marketplace/rules         - List all rules  │  │
│  │ GET  /api/marketplace/rules/{id}    - Get single rule │  │
│  │ POST /api/marketplace/rules         - Upload rule     │  │
│  │ GET  /api/marketplace/search?q=...  - Search rules    │  │
│  │ GET  /api/health                    - Health check    │  │
│  └───────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │ PostgreSQL (Port 5432)                                │  │
│  │ - Users (username only, no auth)                      │  │
│  │ - MarketplaceRules (name, rules, author, downloads)   │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Step 1: Create Server Project Structure

```
BrowserApp.Server/
├── BrowserApp.Server.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Dockerfile
├── docker-compose.yml
│
├── Controllers/
│   ├── MarketplaceController.cs
│   └── HealthController.cs
│
├── Services/
│   └── MarketplaceService.cs
│
├── Data/
│   ├── ServerDbContext.cs
│   └── Entities/
│       ├── User.cs
│       └── MarketplaceRule.cs
│
├── Models/
│   ├── Requests/
│   │   └── UploadRuleRequest.cs
│   └── Responses/
│       ├── RuleResponse.cs
│       └── RuleListResponse.cs
│
└── Migrations/
```

### Step 2: Database Schema (PostgreSQL)

```sql
-- Users (simple, no auth for now)
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Username VARCHAR(50) UNIQUE NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- Marketplace Rules
CREATE TABLE MarketplaceRules (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    Site VARCHAR(500) NOT NULL,
    RulesJson JSONB NOT NULL,
    AuthorId UUID NOT NULL,
    DownloadCount INTEGER DEFAULT 0,
    Tags TEXT[],
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (AuthorId) REFERENCES Users(Id)
);

-- Indexes
CREATE INDEX idx_rules_author ON MarketplaceRules(AuthorId);
CREATE INDEX idx_rules_downloads ON MarketplaceRules(DownloadCount DESC);
CREATE INDEX idx_rules_tags ON MarketplaceRules USING GIN(Tags);
```

### Step 3: API Endpoints

| Method | Endpoint | Description | Request | Response |
|--------|----------|-------------|---------|----------|
| GET | `/api/health` | Health check | - | `{ status: "healthy" }` |
| GET | `/api/marketplace/rules` | List rules (paginated) | `?page=1&limit=20` | `RuleListResponse` |
| GET | `/api/marketplace/rules/{id}` | Get single rule | - | `RuleResponse` |
| POST | `/api/marketplace/rules` | Upload rule | `UploadRuleRequest` | `RuleResponse` |
| GET | `/api/marketplace/search` | Search rules | `?q=privacy&tags=tracker` | `RuleListResponse` |
| POST | `/api/marketplace/rules/{id}/download` | Increment download count | - | `RuleResponse` |

### Step 4: Docker Setup

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BrowserApp.Server/BrowserApp.Server.csproj", "BrowserApp.Server/"]
RUN dotnet restore "BrowserApp.Server/BrowserApp.Server.csproj"
COPY . .
WORKDIR "/src/BrowserApp.Server"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BrowserApp.Server.dll"]
```

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: BrowserApp.Server/Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__PostgreSQL=Host=db;Database=browserapp;Username=postgres;Password=browserapp123
    depends_on:
      - db

  db:
    image: postgres:16-alpine
    ports:
      - "5432:5432"
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=browserapp123
      - POSTGRES_DB=browserapp
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### Step 5: Client Updates

**New Files:**
```
BrowserApp.UI/
├── Services/
│   ├── ApiClient.cs           # HTTP client to server
│   └── RuleSyncService.cs     # Sync marketplace rules
│
├── Views/
│   ├── MarketplaceView.xaml   # Browse/search/install rules
│   └── SettingsView.xaml      # Server config, username
│
└── ViewModels/
    ├── MarketplaceViewModel.cs
    └── SettingsViewModel.cs
```

**Client Database Updates:**
- Add `Source` column to Rules: `"local"`, `"marketplace"`, `"channel"`
- Add `MarketplaceId` column for synced rules
- Add `Settings` table for server URL and username

### Step 6: UI Design (MarketplaceView)

```
┌────────────────────────────────────────────────────────────┐
│ Marketplace                                    [🔍 Search] │
├────────────────────────────────────────────────────────────┤
│ Categories: [All] [Privacy] [Ads] [Social] [Dark Mode]     │
├────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────────────────┐   │
│ │ 🛡️ Privacy Pack - News Sites                        │   │
│ │ Blocks trackers on major news websites              │   │
│ │ By: john_doe • ⬇️ 1,234 downloads                   │   │
│ │ Tags: [privacy] [trackers] [news]                   │   │
│ │                                    [Install] [View] │   │
│ └──────────────────────────────────────────────────────┘   │
│ ┌──────────────────────────────────────────────────────┐   │
│ │ 🍪 Cookie Banner Killer                              │   │
│ │ Hides cookie consent dialogs on all sites           │   │
│ │ By: privacy_guru • ⬇️ 5,678 downloads               │   │
│ │ Tags: [cookies] [gdpr] [banners]                    │   │
│ │                                    [Install] [View] │   │
│ └──────────────────────────────────────────────────────┘   │
│                                                            │
│ [< Previous]                              [Next >]         │
└────────────────────────────────────────────────────────────┘
```

---

## Task Breakdown

### Server Development (5-6 tasks)

- [ ] **Task 1:** Create BrowserApp.Server project with .NET 8 Web API
- [ ] **Task 2:** Set up PostgreSQL with EF Core + migrations
- [ ] **Task 3:** Implement MarketplaceController + MarketplaceService
- [ ] **Task 4:** Create Dockerfile and docker-compose.yml
- [ ] **Task 5:** Test API with Postman/curl
- [ ] **Task 6:** Add basic error handling and logging

### Client Development (5-6 tasks)

- [ ] **Task 7:** Create ApiClient service for HTTP communication
- [ ] **Task 8:** Create RuleSyncService for marketplace sync
- [ ] **Task 9:** Create MarketplaceView + MarketplaceViewModel
- [ ] **Task 10:** Create SettingsView + SettingsViewModel (server config)
- [ ] **Task 11:** Update Rule model with Source/MarketplaceId
- [ ] **Task 12:** Add marketplace button to MainWindow sidebar

### Integration (2-3 tasks)

- [ ] **Task 13:** End-to-end test: upload rule from client
- [ ] **Task 14:** End-to-end test: download rule from marketplace
- [ ] **Task 15:** Test Docker deployment on laptop

---

## Success Criteria

### Server
- [ ] Docker container starts with `docker-compose up`
- [ ] API responds to health check
- [ ] Can create user (just username)
- [ ] Can upload rule with author attribution
- [ ] Can list/search/download rules
- [ ] PostgreSQL data persists between restarts

### Client
- [ ] Can configure server URL in settings
- [ ] Can set username
- [ ] Can browse marketplace
- [ ] Can search/filter rules
- [ ] Can install rule from marketplace
- [ ] Installed rules appear in Rule Manager
- [ ] Can uninstall marketplace rules

### Integration
- [ ] Rule uploaded on PC visible on laptop
- [ ] Download count increments correctly
- [ ] Offline mode works (cached rules)

---

## Technical Notes

### No Authentication (Phase 4)
- Users identified by username only
- Anyone can upload rules
- No password required
- Real auth added in Phase 5 with channels

### Offline-First Design
- Client caches all installed marketplace rules locally
- Works without server connection
- Syncs when server available

### Error Handling
- Graceful degradation when server unavailable
- Clear error messages in UI
- Retry logic for transient failures

---

## Open Questions

1. **Rule versioning?** - Should we track versions and allow updates?
   - *Recommendation:* Not in Phase 4, add later if needed

2. **Rule ratings/reviews?** - Should users rate rules?
   - *Recommendation:* Not in Phase 4, download count is enough

3. **Rule deletion?** - Can authors delete their rules?
   - *Recommendation:* Yes, soft delete (mark inactive)

4. **Duplicate detection?** - Prevent uploading same rule twice?
   - *Recommendation:* Warn but allow (different name = different rule)

---

## Ready to Start?

This plan creates:
1. **New server project** with Docker deployment
2. **Marketplace API** for rules sharing
3. **Client integration** for browsing/installing rules
4. **Settings panel** for server configuration

**Estimated effort:** 2 weeks
**Dependencies:** PostgreSQL, Docker Desktop

**Next step:** Confirm plan, then start with Task 1 (create server project)
