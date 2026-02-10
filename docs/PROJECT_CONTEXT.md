# AI-Powered Privacy Browser - Project Context
**Purpose:** Single reference document for all development sessions
**Last Updated:** February 2026

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
| **6. AI Integration** | 🔄 Partial | Ollama, CopilotSidebar, rule generation |
| **7. Tabs & Profiles** | ✅ | Tab system, profile switching, bookmarks, settings |
| **8. Polish** | ✅ | Bug fixes, WebView2 init fix, event leak fixes |

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
| Tab strip management | `ViewModels/TabStripViewModel.cs` |
| Bookmark management | `ViewModels/BookmarkViewModel.cs` |
| History panel | `ViewModels/HistoryViewModel.cs` |
| Network monitor | `ViewModels/NetworkMonitorViewModel.cs` |
| New tab page | `ViewModels/NewTabPageViewModel.cs` |
| Profile management | `ViewModels/ProfileSelectorViewModel.cs` |
| Tab model (per-tab WebView2) | `Models/BrowserTabItem.cs` |
| Bookmarks sidebar | `Views/BookmarksPanel.xaml` |
| Profile selector dialog | `Views/ProfileSelectorView.xaml` |
| New tab page overlay | `Views/NewTabPageView.xaml` |
| Tab strip control | `Controls/TabItemControl.xaml` |
| Converters (color, visibility) | `Converters/*.cs` |
| Profile service | `Services/ProfileService.cs` |
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
| Bookmark entity | `Entities/BookmarkEntity.cs` |
| History entity | `Entities/BrowsingHistoryEntity.cs` |
| Rule repository | `Repositories/RuleRepository.cs` |
| Bookmark repository | `Repositories/BookmarkRepository.cs` |
| History repository | `Repositories/BrowsingHistoryRepository.cs` |
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
- `BrowsingHistory` - Id, Url, Title, VisitedAt
- `Bookmarks` - Id, Url, Title, CreatedAt

**Note:** Each profile has its own SQLite database at `%LOCALAPPDATA%/BrowserApp/Profiles/{guid}/browser.db`

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

### User Browses a Page (Tab System)
```
1. User enters URL → MainViewModel.Navigate()
2. Gets active tab via TabStripViewModel.ActiveTab
3. Calls tab.Navigate(url) → per-tab CoreWebView2.Navigate()
4. Per-tab RequestInterceptor hooks WebResourceRequested
5. For each request:
   - RuleEngine.Evaluate(request, activeRules)
   - If blocked → cancel request
   - RequestCaptured event → NetworkLogger.LogRequestAsync()
6. NavigationCompleted fires
7. Per-tab CSSInjector/JSInjector apply matching rules
8. MainViewModel records history via IBrowsingHistoryRepository
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

## Tab System (Phase 7)

### Architecture

Each tab is a `BrowserTabItem` with its own independent WebView2 instance, `RequestInterceptor`, `CSSInjector`, and `JSInjector`. Tabs share a single `CoreWebView2Environment` (owned by `TabStripViewModel`).

### Two-Phase Tab Initialization (Critical)

WebView2 requires an HWND (window handle) from the visual tree before `EnsureCoreWebView2Async` can complete. Tabs use a two-phase init:

```
Phase 1: tab.CreateWebView()           → Creates WPF WebView2 control
         TabAdded event fires           → MainWindow adds to WebViewHost grid (gets HWND)
Phase 2: tab.InitializeCoreAsync(...)   → EnsureCoreWebView2Async succeeds
         TabReady event fires           → MainWindow wires network logger + downloads
         ActivateTab()                  → Shows WebView2, hides others
```

**WARNING:** Calling `EnsureCoreWebView2Async` before the WebView2 is in the visual tree will cause a silent deadlock (no exception, no timeout — just hangs forever).

### Key Events on TabStripViewModel
| Event | When | MainWindow Uses It To |
|-------|------|-----------------------|
| `TabAdded` | After WebView2 created, before CoreWebView2 init | Add WebView2 to visual tree |
| `TabReady` | After CoreWebView2 fully initialized | Wire network logger + downloads |
| `TabRemoved` | Tab being closed | Remove WebView2 from visual tree |
| `ActiveTabChanged` | User switches tabs | Show/hide WebView2s, manage overlay |

### NewTabPage Overlay
- `NewTabPageOverlay` (ZIndex=10) shows over WebView2 when a tab has no URL
- Hidden automatically when active tab's `SourceChanged` fires with a non-empty URL
- Also hidden via `SearchPerformed` event when user searches from the new tab page

---

## Profile System (Phase 7)

Profiles provide isolated browsing contexts. Each profile gets:
- Own SQLite database (`browser.db`)
- Own settings file (`settings.json`)
- Own WebView2 user data folder (`UserData/`)

Path structure: `%LOCALAPPDATA%/BrowserApp/Profiles/{guid}/`

Profile switching requires app restart (industry standard). The active profile ID is stored in `%LOCALAPPDATA%/BrowserApp/active_profile.txt`.

---

## Bugs Fixed (Phase 8 Review)

### Critical Fixes
| Bug | Root Cause | Fix |
|-----|-----------|-----|
| **WebView2 init deadlock** | `EnsureCoreWebView2Async` called before visual tree | Two-phase init (CreateWebView → add to tree → InitializeCoreAsync) |
| **NewTabPage overlay stuck** | Overlay shown on activation, never hidden on navigation | Subscribe to `SourceChanged`, hide overlay when URL becomes non-empty |
| **Network Monitor dead** | Subscribed to global singleton interceptor (never initialized) | Subscribe to active tab's per-tab `RequestInterceptor` via `TabStripViewModel` |
| **History nav broken** | Used global `INavigationService` (no WebView2 attached) | Navigate via `TabStripViewModel.ActiveTab?.Navigate()` |
| **Profile colors invisible** | WPF binding `Color="{Binding Color}"` can't convert string→Color | Added `StringToBrushConverter`, use `Background` binding |
| **FrequentSites LINQ crash** | `.First()` inside EF Core GroupBy projection not translatable | Split into two queries (aggregate in SQL, titles client-side) |

### Memory Leak Fixes
| Bug | Root Cause | Fix |
|-----|-----------|-----|
| **BrowserTabItem lambda leak** | Anonymous lambda on `NavigationCompleted` can't be unsubscribed | Extracted to named method `OnNavigationCompletedForInjection`, unsubscribe in Dispose |
| **BookmarkViewModel event leak** | `SourceChanged` subscribed on every tab switch, never unsubscribed from previous | Track `_subscribedTab`, unsubscribe before subscribing to new tab |

### Pattern: Global Singleton → Active Tab

The old architecture had global singleton services (`INavigationService`, `IRequestInterceptor`) that were wired to a single WebView2. With the tab system, each tab creates its own instances. ViewModels that need to interact with the browser now take `TabStripViewModel` and access `ActiveTab` instead of the global singletons.

---

## Architecture Assessment (Feb 2026)

**Architecture Score: 8/10 | Implementation Readiness: 4/10**

### Strengths
- Clean layered separation (Core/Data/UI/Server) with interface boundaries
- Extensible rule engine with LRU caching (MemoryCache, 100MB limit)
- Multi-profile support with isolated data directories
- Marketplace + Channels concept is genuinely differentiating
- 130 unit tests across services, repositories, ViewModels
- Proper async/await patterns throughout

### Missing for Production Browser
- Session persistence (tabs lost on restart)
- Crash recovery
- Certificate/security warnings UI
- Address bar autocomplete/suggestions
- Password manager / autofill
- Downloads manager panel (notifications exist, no management)
- Extension support (WebView2 API exists, not wired up)
- Search engine customization

### WebView2 Extension Support
WebView2 supports loading **unpacked** Chromium extensions via `CoreWebView2Profile.AddBrowserExtensionAsync(path)`. Limitations:
- Must be Manifest V3 (V2 deprecated as of mid-2025)
- Load from local folder only, no Chrome Web Store browsing
- Some `chrome.*` APIs are restricted in WebView2
- Extensions persist per-profile (fits existing profile system)
- Potential future feature: extension management UI + sideloading popular extensions

---

**End of Project Context**

*This document is the sole reference for all development sessions.*
