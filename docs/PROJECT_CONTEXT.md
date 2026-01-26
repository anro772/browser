# AI-Powered Privacy Browser - Project Context
**Purpose:** Single reference document for all development sessions
**Last Updated:** January 2026

---

## Project Vision

> **This browser must be visually stunning and modern - like Comet Browser.**
> 1. **Power Users** - Beautiful, fast browser with AI-powered privacy controls
> 2. **Businesses** - Policy enforcement via channels
>
> **Design Goal:** Modern consumer app that happens to have business features.

### What We're Building

A Windows-native, Chromium-based browser with:
- Built-in ad/tracker blocking via JSON rule system
- AI copilot sidebar for assistance and rule generation
- Business channels for policy enforcement
- Public marketplace for community rules
- Modern Windows 11 Fluent Design UI

### Key Decisions

- **Database:** PostgreSQL (server), SQLite (client)
- **Auth:** Username field only (proper auth later)
- **Development:** Run locally, Docker for deployment only

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Client UI** | WPF + WPF UI (Fluent Design 2.0) |
| **Browser Engine** | WebView2 (Edge/Chromium) |
| **Client DB** | SQLite + EF Core |
| **Server API** | .NET 8 Web API |
| **Server DB** | PostgreSQL |
| **LLM** | Ollama (Llama 3.2 / Mistral 7B) |
| **Architecture** | MVVM pattern |

### Key Libraries
- **WPF UI** (lepoco/wpfui) - Modern Fluent controls
- **Microsoft.Web.WebView2** - Browser engine
- **Microsoft.EntityFrameworkCore.Sqlite** - Client data
- **Npgsql.EntityFrameworkCore.PostgreSQL** - Server data
- **CommunityToolkit.Mvvm** - MVVM helpers

---

## Phase Status

| Phase | Status | Key Features |
|-------|--------|--------------|
| **1. Core Browser** | ✅ | WebView2, navigation, SQLite, MVVM |
| **2. Network Monitoring** | ✅ | RequestInterceptor, NetworkLogger, filtering |
| **3. Rule System** | ✅ | RuleEngine, CSS/JS injection, templates |
| **4. Marketplace** | ✅ | Server API, MarketplaceView, rule sync |
| **5. Channels** | ✅ | Channel CRUD, join/leave, audit logging |
| **6. AI Integration** | 🔄 Current | Ollama, CopilotSidebar, rule generation |
| **7. Profiles** | ⏳ | Profile switching, settings panel |
| **8. Polish** | ⏳ | Bug fixes, performance, demo prep |

---

## Local Development (Preferred)

> **IMPORTANT:** Run locally for development. Docker is for deployment only.

### Prerequisites
- PostgreSQL installed locally
- Connection: `localhost:5432`, database `browserapp`, user `postgres`, password `1234`

### Quick Start
```bash
# Terminal 1: Start server
dotnet run --project BrowserApp.Server

# Terminal 2: Start client
dotnet run --project BrowserApp.UI
```

### URLs
- **Server:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger
- **Database GUI:** DBeaver or pgAdmin (localhost:5432, browserapp, postgres/1234)

### Docker (Deployment Only)
```bash
docker compose up --build    # Start
docker compose down          # Stop
```

---

## Key Files Reference

### Client Application (BrowserApp.UI)

| Purpose | File |
|---------|------|
| Entry point, DI setup | `App.xaml.cs` |
| Main window shell | `MainWindow.xaml` / `.cs` |
| Marketplace UI | `Views/MarketplaceView.xaml` |
| Channels UI | `Views/ChannelsView.xaml` |
| Password dialog | `Views/PasswordDialog.xaml` |
| Network monitor | `Views/NetworkMonitorView.xaml` |
| Marketplace logic | `ViewModels/MarketplaceViewModel.cs` |
| Channels logic | `ViewModels/ChannelsViewModel.cs` |
| Marketplace API calls | `Services/MarketplaceApiClient.cs` |
| Channel API calls | `Services/ChannelApiClient.cs` |
| Rule download/sync | `Services/RuleSyncService.cs` |
| Channel membership sync | `Services/ChannelSyncService.cs` |
| Client settings | `appsettings.json` |

### Core Library (BrowserApp.Core)

| Purpose | File |
|---------|------|
| Rule blocking logic | `Services/RuleEngine.cs` |
| Network interception | `Services/RequestInterceptor.cs` |
| CSS injection | `Services/CSSInjector.cs` |
| JS injection | `Services/JSInjector.cs` |
| API interfaces | `Interfaces/IMarketplaceApiClient.cs` |
| | `Interfaces/IChannelApiClient.cs` |
| | `Interfaces/IChannelSyncService.cs` |
| | `Interfaces/IRuleSyncService.cs` |
| Shared DTOs | `DTOs/*.cs` |

### Data Layer (BrowserApp.Data)

| Purpose | File |
|---------|------|
| Client DB context | `BrowserDbContext.cs` |
| Rule entity | `Entities/RuleEntity.cs` |
| Channel membership | `Entities/ChannelMembershipEntity.cs` |
| Network log entity | `Entities/NetworkLogEntity.cs` |
| Rule repository | `Repositories/RuleRepository.cs` |
| Membership repository | `Repositories/ChannelMembershipRepository.cs` |
| Migrations | `Migrations/*.cs` |

### Server (BrowserApp.Server)

| Purpose | File |
|---------|------|
| Server entry, migrations | `Program.cs` |
| Marketplace endpoints | `Controllers/MarketplaceController.cs` |
| Channel endpoints | `Controllers/ChannelController.cs` |
| Health check | `Controllers/HealthController.cs` |
| Channel business logic | `Services/ChannelService.cs` |
| Marketplace logic | `Services/MarketplaceService.cs` |
| Server DB context | `Data/ServerDbContext.cs` |
| Entity mappings | `Mappers/ChannelMapper.cs` |
| Server settings | `appsettings.Development.json` |
| Migrations | `Data/Migrations/*.cs` |

### Configuration Files

| Purpose | File |
|---------|------|
| Solution file | `BrowserApp.sln` |
| Docker (deployment) | `docker-compose.yml` |
| Client config | `BrowserApp.UI/appsettings.json` |
| Server config | `BrowserApp.Server/appsettings.Development.json` |
| This document | `docs/PROJECT_CONTEXT.md` |

---

## API Endpoints

### Health
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/health` | Server health check |

### Marketplace (`/api/marketplace`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/rules` | List rules (paginated) |
| GET | `/rules/{id}` | Get rule by ID |
| POST | `/rules` | Upload rule |
| GET | `/search` | Search rules |
| POST | `/rules/{id}/download` | Increment downloads |
| DELETE | `/rules/{id}` | Delete rule |

### Channels (`/api/channel`)
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/channels` | List channels |
| GET | `/channels/{id}` | Get channel |
| POST | `/channels` | Create channel |
| POST | `/channels/{id}/join` | Join (password required) |
| POST | `/channels/{id}/leave` | Leave channel |
| GET | `/channels/{id}/rules` | Get channel rules |
| POST | `/channels/{id}/rules` | Add rule to channel |
| GET | `/user/{username}/channels` | Get user's channels |

---

## Database Schemas

### Server (PostgreSQL)

**Key Tables:**
- `Users` - Id, Username, CreatedAt
- `MarketplaceRules` - Id, Name, Description, Site, RulesJson, AuthorId, DownloadCount, Tags
- `Channels` - Id, Name, Description, OwnerId, PasswordHash, MemberCount, IsActive
- `ChannelRules` - Id, ChannelId, Name, RulesJson, IsEnforced
- `ChannelMembers` - Id, ChannelId, UserId, JoinedAt, LastSyncedAt
- `ChannelAuditLogs` - Id, ChannelId, UserId, Action, Metadata, Timestamp

### Client (SQLite)

**Key Tables:**
- `Rules` - Id, Name, Site, RulesJson, Source, ChannelId, IsEnforced, MarketplaceId
- `ChannelMemberships` - Id, ChannelId, ChannelName, Username, JoinedAt, LastSyncedAt
- `NetworkLogs` - Id, Url, Method, StatusCode, IsBlocked, Timestamp

---

## Rule System

### JSON Rule Format
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Privacy Pack",
  "description": "Blocks trackers and hides cookie banners",
  "site": "*.example.com",
  "enabled": true,
  "priority": 10,
  "rules": [
    {
      "type": "block",
      "match": { "urlPattern": "*tracker*", "resourceType": "script" }
    },
    {
      "type": "inject_css",
      "css": ".cookie-banner { display: none !important; }"
    },
    {
      "type": "inject_js",
      "timing": "dom_ready",
      "js": "document.querySelector('[data-consent=reject]')?.click();"
    }
  ]
}
```

### Rule Types
| Type | Purpose | Key Parameters |
|------|---------|----------------|
| `block` | Cancel network request | `urlPattern`, `resourceType` |
| `inject_css` | Add CSS to page | `css`, `timing` |
| `inject_js` | Run JavaScript | `js`, `timing` |

### Rule Sources
| Source | Description | User Control |
|--------|-------------|--------------|
| `local` | User-created rules | Full control |
| `marketplace` | Downloaded from server | Can disable |
| `channel` | Business policy | Cannot disable (enforced) |

### Resource Types
`script`, `stylesheet`, `image`, `xhr`, `fetch`, `document`

---

## Channel System

### How Channels Work

**Creating a Channel:**
1. User clicks "Create Channel" in ChannelsView
2. Enters name, description, password
3. Server creates channel, adds owner as member
4. Client saves local membership

**Joining a Channel:**
1. User clicks "Join" on a channel
2. Enters password in PasswordDialog
3. Server validates password, adds membership
4. Client saves local membership and syncs rules

**Syncing Rules:**
1. User clicks "Sync Now" or sync happens automatically
2. Client calls `GET /api/channel/channels/{id}/rules`
3. Server returns rules, updates LastSyncedAt
4. Client saves rules locally with `IsEnforced = true`

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│              Client (WPF Browser App)                │
│  ┌─────────────────────────────────────────────┐    │
│  │ Views (XAML) → ViewModels → Services        │    │
│  └─────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────┐    │
│  │ SQLite: Rules, Memberships, NetworkLogs     │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
                        │ HTTP/JSON
                        ▼
┌─────────────────────────────────────────────────────┐
│              Server (.NET 8 Web API)                 │
│  ┌─────────────────────────────────────────────┐    │
│  │ Controllers → Services → Repositories       │    │
│  └─────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────┐    │
│  │ PostgreSQL: Users, Rules, Channels, Audit   │    │
│  └─────────────────────────────────────────────┘    │
│  ┌─────────────────────────────────────────────┐    │
│  │ Ollama LLM (Phase 6)                        │    │
│  └─────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

### Design Principles
1. **Offline-first** - Browser works without server (cached rules)
2. **MVVM** - UI completely independent of business logic
3. **DI** - All services injected via constructor
4. **Interfaces** - Program to interfaces, not implementations

---

## Service Registration (App.xaml.cs)

```csharp
// API Clients
services.AddSingleton<MarketplaceApiClient>();
services.AddSingleton<IMarketplaceApiClient>(sp => sp.GetRequiredService<MarketplaceApiClient>());
services.AddSingleton<ChannelApiClient>();
services.AddSingleton<IChannelApiClient>(sp => sp.GetRequiredService<ChannelApiClient>());

// Sync Services
services.AddSingleton<RuleSyncService>();
services.AddSingleton<IRuleSyncService>(sp => sp.GetRequiredService<RuleSyncService>());
services.AddSingleton<ChannelSyncService>();
services.AddSingleton<IChannelSyncService>(sp => sp.GetRequiredService<ChannelSyncService>());

// ViewModels
services.AddTransient<MarketplaceViewModel>();
services.AddTransient<ChannelsViewModel>();
```

---

## Data Flows

### User Browses a Page
```
1. User enters URL → NavigationService
2. WebView2 starts navigation
3. RequestInterceptor hooks WebResourceRequested
4. For each request:
   - RuleEngine.Evaluate(request, activeRules)
   - If blocked → cancel request
   - NetworkLogger.Log(request)
5. NavigationCompleted fires
6. CSSInjector/JSInjector apply matching rules
```

### Download from Marketplace
```
1. User opens Marketplace tab
2. MarketplaceViewModel.LoadRulesAsync()
3. User clicks "Install" on a rule
4. RuleSyncService.DownloadAndInstallRuleAsync()
5. Rule saved to SQLite with Source="marketplace"
6. RuleEngine reloads active rules
```

---

## Code Conventions

### C#
- PascalCase: classes, methods, properties
- camelCase: private fields (`_fieldName`), parameters
- Interfaces: prefix with `I` (e.g., `IRuleEngine`)
- Always use `async/await` for I/O
- Constructor injection for dependencies

### XAML
- Use WPF UI components (`Wpf.Ui` namespace)
- Bind to ViewModels, no business logic in code-behind
- DataContext set in XAML or constructor

### Adding New Features
1. Create interface in `BrowserApp.Core/Interfaces/`
2. Implement in `BrowserApp.UI/Services/` or `BrowserApp.Core/Services/`
3. Register in `App.xaml.cs` DI container
4. Inject into ViewModels via constructor
5. Create View + ViewModel if UI needed

---

## Development Tools

### Skills (invoke with `/command`)
| Command | Use For |
|---------|---------|
| `/feature-dev` | New features (guided development) |
| `/frontend-design` | UI work (Fluent Design) |
| `/commit` | Create commits |
| `/code-review` | Review PRs |

### Task Agents
| Agent | Use For |
|-------|---------|
| `Explore` | Find code, understand structure |
| `code-architect` | Design feature architecture |
| `code-reviewer` | Review for bugs, security |

### MCP Tools
- **context7** - Query library documentation
- **playwright** - Browser automation testing

---

## Configuration

### Client (appsettings.json)
```json
{
  "MarketplaceApi": {
    "BaseUrl": "http://localhost:5000"
  },
  "Browser": {
    "DefaultSearchEngine": "Google"
  }
}
```

### Server (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Port=5432;Database=browserapp;Username=postgres;Password=1234"
  }
}
```

---

## Quality Checklist

Before completing any feature:
- [ ] Code follows MVVM pattern
- [ ] Services use interfaces
- [ ] Registered in DI container
- [ ] Tested manually (run both server and client)
- [ ] No hardcoded values (use config)
- [ ] Error handling in place

---

## Quick Reference

### Start Development
```bash
# 1. Start server
dotnet run --project BrowserApp.Server

# 2. Start client (new terminal)
dotnet run --project BrowserApp.UI
```

### Build & Test
```bash
dotnet build BrowserApp.sln
dotnet test BrowserApp.Tests
```

### Check Database
- Use DBeaver: localhost:5432, browserapp, postgres/1234

### Common Issues
- **Server won't start:** Check PostgreSQL is running
- **Client can't connect:** Ensure server is on http://localhost:5000
- **Migrations failed:** Delete `browserapp` database and restart server

---

**End of Project Context**

*This document is the sole reference for all development sessions.*
