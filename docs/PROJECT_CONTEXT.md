# AI-Powered Privacy Browser - Project Context
**Purpose:** Single reference document for all development sessions
**Last Updated:** January 2026

---

## Project Vision

> **This browser must be visually stunning and modern - like Comet Browser.**
> It is NOT just a business tool. The end product should appeal to:
> 1. **Power Users** - Who want a beautiful, fast browser with AI-powered privacy controls
> 2. **Businesses** - Who need policy enforcement via channels
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

- **Database:** PostgreSQL for server, SQLite for client
- **Deployment:** Docker for easy portability
- **Auth:** Skip for now - just username field (add proper auth later)
- **Marketplace:** Upload/download rules with user attribution
- **Channels:** Phase 5 - enforced rules for businesses
- **AI Copilot:** Phase 6 - Ollama integration

---

## Tech Stack

### Client Application
- **Framework:** C# / .NET 8
- **UI:** WPF with WPF UI (Fluent Design 2.0)
- **Browser Engine:** WebView2 Evergreen Runtime (Edge/Chromium)
- **Database:** SQLite with Entity Framework Core
- **Architecture:** MVVM pattern

### Server (Self-Hosted)
- **API:** .NET 8 Web API (REST)
- **Database:** PostgreSQL
- **LLM:** Ollama (7B model - Llama 3.2 or Mistral)
- **Deployment:** Docker containers

### Key Libraries
- **WPF UI** (lepoco/wpfui) - Modern Fluent controls, Mica/Acrylic
- **Microsoft.Web.WebView2** - Browser engine
- **Microsoft.EntityFrameworkCore.Sqlite** - Local data
- **Npgsql.EntityFrameworkCore.PostgreSQL** - Server data

---

## Development Tools

### Skills (User-Invocable via `/command`)

| Skill | Command | When to Use |
|-------|---------|-------------|
| Feature Development | `/feature-dev` | Starting new features - guided development with architecture focus |
| Frontend Design | `/frontend-design` | UI work - creates distinctive, production-grade interfaces |
| Create Commit | `/commit` | After completing work |
| Commit + PR | `/commit-push-pr` | Ready for review |
| Code Review | `/code-review` | Reviewing PRs |

**Important:** Always use `/feature-dev` for new features and `/frontend-design` for UI work.

### Task Agents (via Task tool)

| Agent | Type | When to Use |
|-------|------|-------------|
| Explore | `Explore` | Finding code, understanding structure. Levels: "quick", "medium", "very thorough" |
| Plan | `Plan` | Designing implementation approach, step-by-step plans |
| Code Architect | `feature-dev:code-architect` | Design feature architecture - specific files, data flows |
| Code Explorer | `feature-dev:code-explorer` | Deep analysis - trace execution paths, map layers |
| Code Reviewer | `feature-dev:code-reviewer` | Review for bugs, security, quality. Use after EVERY implementation |

### MCP Servers & Tools

> **Important:** Use the **plugin versions** of MCP tools (prefixed with `mcp__plugin_`)

#### context7 - Library Documentation

**Purpose:** Fetch up-to-date documentation for any library

**Tools:**
- `mcp__plugin_context7_context7__resolve-library-id` - Find library ID
- `mcp__plugin_context7_context7__query-docs` - Query documentation

**Example Usage:**
```
1. resolve-library-id:
   libraryName="WebView2"
   query="how to intercept network requests"

2. query-docs:
   libraryId="/microsoftedge/webview2browser" (from step 1)
   query="intercept HTTP requests"
```

#### playwright - Browser Automation

**Purpose:** Automate browser testing and interactions

**Key Tools:**
- `browser_navigate` - Go to URL
- `browser_snapshot` - Accessibility snapshot (better than screenshot)
- `browser_click` - Click elements
- `browser_type` - Type text
- `browser_take_screenshot` - Capture screenshot
- `browser_console_messages` - Get console logs
- `browser_network_requests` - Get network activity

**Example Usage:**
```
1. browser_navigate: url="http://localhost:5000"
2. browser_snapshot: Get page structure
3. browser_click: element="Submit button", ref="button[0]"
4. browser_take_screenshot: filename="test-result.png"
```

#### shadcn-ui - UI Component Reference

**Purpose:** Access shadcn/ui v4 component source code and demos

**Tools:**
- `mcp__shadcn-ui__list_components` - List all 46 available components
- `mcp__shadcn-ui__get_component_demo` - Get usage demo code
- `mcp__shadcn-ui__list_blocks` - List pre-built UI blocks

**Note:** shadcn-ui is React/Tailwind-based. Use for **design patterns only**, adapt to WPF.

---

## Standard Workflows

### Workflow 1: Implementing a New Feature

```
┌─────────────────────────────────────────┐
│ New Feature Request                      │
└─────────────────────────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 1. CODE-EXPLORER     │  Analyze existing code
    │ (Task agent)         │  relevant to feature
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 2. CODE-ARCHITECT    │  Design architecture
    │ (Task agent)         │  files, components, flows
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 3. CONTEXT7          │  Query documentation
    │ (MCP)                │  for APIs needed
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 4. IMPLEMENT         │  Write code with TDD
    │                      │  (test first)
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 5. CODE-REVIEWER     │  Deep quality check
    │ (Task agent)         │  bugs, security, quality
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 6. PLAYWRIGHT        │  Test functionality
    │ (MCP)                │  visual verification
    └──────────────────────┘
            │
            ▼
    ┌──────────────────────┐
    │ 7. /COMMIT           │  Commit changes
    │ (Skill)              │
    └──────────────────────┘
            │
            ▼
         DONE
```

### Workflow 2: Fixing a Bug

```
1. EXPLORE      → Find relevant code (Explore agent)
2. CONTEXT7     → Check API documentation if needed
3. TEST         → Write failing test (TDD)
4. FIX          → Implement fix
5. VERIFY       → Ensure test passes
6. REVIEW       → Check for regressions (code-reviewer)
```

### Workflow 3: Research Unknown API

```
1. context7: resolve-library-id for the library
2. context7: query-docs with specific question
3. Apply knowledge to implementation
```

### Workflow 4: Test UI Visually

```
1. playwright: browser_navigate to test page
2. playwright: browser_snapshot (accessibility tree)
3. playwright: browser_click / browser_type (interact)
4. playwright: browser_take_screenshot (visual check)
```

---

## Project Structure

```
BrowserApp/
├── BrowserApp.sln
│
├── BrowserApp.UI/                      # WPF Application
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   │
│   ├── Views/
│   │   ├── SidebarPanel.xaml
│   │   ├── CopilotView.xaml
│   │   ├── NetworkMonitorView.xaml
│   │   ├── RuleBuilderView.xaml
│   │   ├── SettingsView.xaml
│   │   ├── MarketplaceView.xaml
│   │   └── ChannelManagerView.xaml
│   │
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── SidebarViewModel.cs
│   │   ├── NetworkMonitorViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Styles/
│   │   ├── BasicTheme.xaml
│   │   └── Colors.xaml
│   │
│   └── Resources/
│       └── DefaultRules/               # Pre-built templates
│
├── BrowserApp.Core/                    # Business Logic
│   ├── Services/
│   │   ├── NavigationService.cs
│   │   ├── RequestInterceptor.cs
│   │   ├── RuleEngine.cs
│   │   ├── BlockingService.cs
│   │   ├── CSSInjector.cs
│   │   ├── JSInjector.cs
│   │   └── RuleSyncService.cs
│   │
│   ├── Models/
│   │   ├── Rule.cs
│   │   ├── NetworkRequest.cs
│   │   ├── Profile.cs
│   │   └── Channel.cs
│   │
│   └── Interfaces/
│       ├── IRuleEngine.cs
│       └── INetworkService.cs
│
├── BrowserApp.Data/                    # Data Access (Client)
│   ├── BrowserDbContext.cs
│   ├── Entities/
│   │   ├── RuleEntity.cs
│   │   ├── ProfileEntity.cs
│   │   └── NetworkLogEntity.cs
│   └── Repositories/
│       └── RuleRepository.cs
│
├── BrowserApp.AI/                      # AI Integration
│   ├── LLMClient.cs
│   └── RuleGeneratorService.cs
│
├── BrowserApp.Server/                  # Server Application
│   ├── Controllers/
│   │   ├── MarketplaceController.cs
│   │   ├── ChannelsController.cs
│   │   └── AIController.cs
│   ├── Services/
│   │   └── OllamaService.cs
│   └── Data/
│       └── ServerDbContext.cs
│
└── BrowserApp.Tests/                   # Tests
```

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│              Client (Browser App)                    │
│  ┌───────────────────────────────────────────────┐  │
│  │ WPF UI Layer (Views, Styles, Animations)      │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ ViewModel Layer (UI Logic, Commands)          │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ Service Layer (Business Logic)                │  │
│  │  • BrowserService  • NetworkService           │  │
│  │  • RuleEngine      • InjectionService         │  │
│  │  • LLMClient       • SyncService              │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ Data Layer (SQLite - EF Core)                 │  │
│  │  • Rules  • Profiles  • NetworkLogs           │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                          │
                  HTTP/JSON │
                          ▼
┌─────────────────────────────────────────────────────┐
│              Server (Self-Hosted)                    │
│  ┌───────────────────────────────────────────────┐  │
│  │ REST API (.NET 8 Web API)                     │  │
│  │  • /api/marketplace/*                         │  │
│  │  • /api/channels/*                            │  │
│  │  • /api/ai/chat                               │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ PostgreSQL Database                           │  │
│  │  • MarketplaceRules  • Channels               │  │
│  │  • Users             • ChannelAuditLog        │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ LLM Service (Ollama)                          │  │
│  │  • Model: Llama 3.2 7B / Mistral 7B           │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### Design Principles

1. **Offline-first:** Browser works without server (cached rules)
2. **Server-enhanced:** Connect for AI + marketplace features
3. **MVVM separation:** UI completely independent of business logic
4. **Modular services:** Each component has single responsibility
5. **Configuration-driven:** Visual effects toggleable via config

---

## Client Modules

### 1. Browser Core Module
**Responsibility:** Core browsing functionality

**Components:**
- `MainWindow.xaml` - Main shell with navigation
- `WebView2Host.cs` - WebView2 control wrapper
- `NavigationService.cs` - URL navigation, history, back/forward
- `SearchEngineService.cs` - Handle search queries

**Features:**
- Address bar with URL/search detection
- Navigation controls (back, forward, refresh)
- WebView2 password manager (built-in Edge credentials)

### 2. Network Interception Module
**Responsibility:** Monitor and control network requests

**Components:**
- `RequestInterceptor.cs` - Hooks `WebResourceRequested` event
- `RuleEngine.cs` - Evaluates requests against active rules
- `BlockingService.cs` - Blocks/allows based on rules
- `NetworkLogger.cs` - Logs to SQLite

**Flow:**
```
Request → RequestInterceptor → RuleEngine
                                    ↓
                         Match rule? → Block / Allow
                                    ↓
                            NetworkLogger (SQLite)
```

### 3. Content Injection Module
**Responsibility:** Inject CSS/JS into pages

**Components:**
- `CSSInjector.cs` - Inject stylesheets via `ExecuteScriptAsync`
- `JSInjector.cs` - Inject JavaScript for DOM manipulation
- `InjectionTimingService.cs` - Control timing (DOMContentLoaded, load)

**Capabilities:**
- Hide elements (cookie banners, ads)
- Auto-click buttons (reject cookies)
- Style modifications (dark mode)

### 4. Rule Management Module
**Responsibility:** Store, sync, and apply rules

**Components:**
- `Rule.cs` - Model: id, name, site, rules array, enabled, priority
- `RuleRepository.cs` - SQLite CRUD operations
- `RuleSyncService.cs` - Sync with server
- `RuleParser.cs` - Parse JSON rules

---

## Rule System (Core Feature)

### JSON Rule Format

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Privacy Pack - News Sites",
  "description": "Blocks trackers and hides cookie banners",
  "site": "*.news.com",
  "enabled": true,
  "priority": 10,
  "rules": [
    {
      "type": "block",
      "match": {
        "urlPattern": "*tracker.com/*",
        "resourceType": "script"
      }
    },
    {
      "type": "inject_css",
      "match": {
        "urlPattern": "*news.com/*"
      },
      "css": ".cookie-banner, .gdpr-wall { display: none !important; }"
    },
    {
      "type": "inject_js",
      "match": {
        "urlPattern": "*news.com/*"
      },
      "timing": "dom_ready",
      "js": "document.querySelector('[data-consent=\"reject\"]')?.click();"
    }
  ]
}
```

### Rule Types

| Type | Description | Parameters |
|------|-------------|------------|
| `block` | Cancel network request | `urlPattern`, `resourceType`, `method` |
| `inject_css` | Add CSS to page | `css`, `timing` |
| `inject_js` | Run JavaScript | `js`, `timing` |

### Resource Types
- `script` - JavaScript files
- `stylesheet` - CSS files
- `image` - Images
- `xhr` / `fetch` - AJAX requests
- `document` - Main HTML page

### Rule Sources

| Source | Description | User Control |
|--------|-------------|--------------|
| `local` | User-created | Full control |
| `marketplace` | Downloaded from server | Can disable |
| `channel` | Business policy | Cannot disable (enforced) |

### Pre-built Templates
1. **Privacy Mode** - Block trackers
2. **Hide Cookie Banners** - CSS for consent dialogs
3. **Block Ads** - Common ad networks
4. **Dark Mode** - Force dark styles

---

## Profile System

### Profile Types

**1. Personal Profile (Default)**
- User's custom rules
- Marketplace downloads
- Local storage only

**2. Work Profile (Business Mode)**
- Join business "Channels" via password
- Channel rules auto-sync from server
- Rules marked as "enforced" (cannot disable)

**3. Custom Profiles**
- Named profiles: "Shopping", "Banking"
- Quick switch between rule sets

### Channel System

**Admin Flow:**
1. Create channel via API
2. Set name, description, password
3. Add rules to channel
4. Share Channel ID + password

**Employee Flow:**
1. Settings → Channels → Join
2. Enter Channel ID + password
3. Browser downloads all channel rules
4. Rules auto-sync every 15 minutes
5. Cannot disable enforced rules

---

## Phase Status

### Completed Phases

**Phase 1: Core Browser** ✅
- WPF project with WPF UI
- WebView2 integration
- Address bar + navigation
- SQLite database (EF Core)
- MVVM structure
- Search engine integration
- WebView2 password manager

**Phase 2: Network Monitoring** ✅
- RequestInterceptor implementation
- NetworkLogger to SQLite
- NetworkMonitor UI (DataGrid)
- Filtering (blocked, 3rd party)
- Export to CSV

**Phase 3: Rule System** ✅
- Rule model and JSON parser
- RuleEngine evaluation logic
- BlockingService
- CSSInjector and JSInjector
- RuleRepository (SQLite CRUD)
- Manual rule creation UI
- 5 pre-built templates

**Phase 4: Server + Marketplace** ✅
- .NET Web API setup
- PostgreSQL database
- Docker deployment
- 7 marketplace endpoints
- Client services registered
- MarketplaceApiClient + RuleSyncService

### Current: Phase 5 - Business Channels

**Goal:** Multi-user policy enforcement

**Features Needed:**
- Channel creation (name, password, rules)
- Join channel with password
- Enforced rules (cannot disable)
- Background sync (15 min interval)
- ChannelManager UI
- Audit logging

**API Endpoints:**
```
POST   /api/channels              # Create channel (admin)
GET    /api/channels/{id}         # Get channel details
GET    /api/channels/{id}/rules   # Get all rules for channel
POST   /api/channels/{id}/join    # Join (password required)
DELETE /api/channels/{id}/leave   # Leave channel
POST   /api/channels/{id}/rules   # Add rule to channel (admin)
```

**Client UI:**
- ChannelsView.xaml - Browse/join channels
- Display "Enforced" badges on channel rules
- Background sync timer service

**Verification:** Create channel, join from different client, rules sync and enforce

### Future Phases

**Phase 6: AI Integration**
- Ollama setup (7B model)
- LLM wrapper API `/api/ai/chat`
- CopilotSidebar UI
- Context provider (page info, network requests)
- Rule generation from natural language
- Preview + apply flow

**Phase 7: Profiles & Settings**
- ProfileManager implementation
- Work/Personal/Custom profiles
- Profile switching UI
- Settings panel (server config)

**Phase 8: Polish & Testing**
- Bug fixes and edge cases
- Performance optimization
- UI consistency
- Demo preparation

---

## Server Reference

### Docker Commands

```bash
# Start server (from project root)
docker compose up --build

# Stop server
docker compose down

# View logs
docker logs browserapp-api
docker logs browserapp-db
```

**Server URL:** http://localhost:5000
**Swagger UI:** http://localhost:5000/swagger

### Existing API Endpoints (Phase 4)

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/health` | Health check |
| GET | `/api/marketplace/rules` | List rules (paginated) |
| GET | `/api/marketplace/rules/{id}` | Get specific rule |
| POST | `/api/marketplace/rules` | Upload rule |
| GET | `/api/marketplace/search` | Search rules |
| POST | `/api/marketplace/rules/{id}/download` | Increment downloads |
| DELETE | `/api/marketplace/rules/{id}` | Delete rule |

### Sample API Usage

**Upload a Rule:**
```bash
curl -X POST http://localhost:5000/api/marketplace/rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Privacy Pack",
    "description": "Blocks trackers",
    "site": "*",
    "priority": 50,
    "rulesJson": "[{\"type\":\"block\",\"match\":{\"urlPattern\":\"*tracker*\"}}]",
    "authorUsername": "john_doe",
    "tags": ["privacy", "tracking"]
  }'
```

**Get All Rules:**
```bash
curl http://localhost:5000/api/marketplace/rules
```

---

## Database Schemas

### Server Database (PostgreSQL)

**Users:**
```sql
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Username VARCHAR(50) UNIQUE NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW()
);
```

**MarketplaceRules:**
```sql
CREATE TABLE MarketplaceRules (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    Description VARCHAR(2000),
    Site VARCHAR(500) NOT NULL,
    Priority INTEGER DEFAULT 10,
    RulesJson JSONB NOT NULL,
    AuthorId UUID REFERENCES Users(Id),
    DownloadCount INTEGER DEFAULT 0,
    Tags TEXT[],
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW()
);
```

**Channels (Phase 5):**
```sql
CREATE TABLE Channels (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedBy UUID NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW()
);

CREATE TABLE ChannelRules (
    ChannelId UUID REFERENCES Channels(Id) ON DELETE CASCADE,
    RuleId UUID NOT NULL,
    RulesJson JSONB NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (ChannelId, RuleId)
);

CREATE TABLE UserChannels (
    UserId UUID NOT NULL,
    ChannelId UUID REFERENCES Channels(Id) ON DELETE CASCADE,
    JoinedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (UserId, ChannelId)
);

CREATE TABLE ChannelAuditLog (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ChannelId UUID REFERENCES Channels(Id) ON DELETE CASCADE,
    UserId UUID,
    Action VARCHAR(50) NOT NULL,
    Metadata JSONB,
    Timestamp TIMESTAMP DEFAULT NOW()
);
```

### Client Database (SQLite)

**Rules:**
```sql
CREATE TABLE Rules (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    Site TEXT NOT NULL,
    Enabled INTEGER DEFAULT 1,
    Priority INTEGER DEFAULT 10,
    RulesJson TEXT NOT NULL,
    Source TEXT,              -- "local", "marketplace", "channel"
    ChannelId TEXT,
    IsEnforced INTEGER DEFAULT 0,
    MarketplaceId TEXT,
    CreatedAt TEXT,
    UpdatedAt TEXT
);
```

**ChannelCache:**
```sql
CREATE TABLE ChannelCache (
    ChannelId TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    JoinedAt TEXT,
    LastSyncedAt TEXT
);
```

---

## Client Services Reference

### Already Registered (App.xaml.cs)

```csharp
services.AddSingleton<MarketplaceApiClient>();
services.AddSingleton<IMarketplaceApiClient>(...);
services.AddSingleton<RuleSyncService>();
services.AddSingleton<IRuleSyncService>(...);
```

### Usage Pattern

```csharp
// Inject in ViewModel
public MyViewModel(
    IMarketplaceApiClient apiClient,
    IRuleSyncService syncService)
{
    _apiClient = apiClient;
    _syncService = syncService;
}

// Get rules from server
var response = await _apiClient.GetRulesTypedAsync(page: 1, pageSize: 20);

// Download and install a rule
var rule = await _syncService.DownloadAndInstallRuleAsync(marketplaceRuleId);

// Check server availability
bool isAvailable = await _syncService.IsServerAvailableAsync();
```

---

## UI Layout Reference

```
┌────────────────────────────────────────────────────────────┐
│ ☰  [← → ⟳]  [🔒 example.com                    ] [Profile▼]│
├────────────────────────────────────────────────────────────┤
│                                    │                        │
│                                    │  ┌──────────────────┐ │
│                                    │  │ [💬] [📊] [⚙️]   │ │
│                                    │  └──────────────────┘ │
│        WebView2 Content            │                        │
│        (Full Chromium Browser)     │  Active Tab:           │
│                                    │  • AI Copilot          │
│                                    │  • Network Monitor     │
│                                    │  • Rules Panel         │
│                                    │  • Channels (Phase 5)  │
│                                    │                        │
│                                    │  [← Collapse]          │
├────────────────────────────────────┴────────────────────────┤
│ 🛡️ 47 blocked • 2.3 MB saved • Work Profile Active         │
└────────────────────────────────────────────────────────────┘
```

### Color Scheme

**Light Mode:**
- Background: `#F3F3F3`
- Surface: `#FFFFFF`
- Accent: `#0078D4` (Windows Blue)

**Dark Mode:**
- Background: `#202020`
- Surface: `#2C2C2C`
- Accent: `#0078D4`

---

## Configuration Files

### Client: appsettings.json

```json
{
  "Server": {
    "ApiBaseUrl": "http://localhost:5000"
  },
  "MarketplaceApi": {
    "BaseUrl": "http://localhost:5000"
  },
  "Browser": {
    "DefaultSearchEngine": "Google",
    "UserDataFolder": "%LOCALAPPDATA%/BrowserApp/UserData"
  },
  "Sync": {
    "ChannelSyncIntervalMinutes": 15,
    "AutoSyncOnStartup": true
  }
}
```

### Server: appsettings.json

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=browserapp;Username=postgres;Password=***"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.2:7b"
  }
}
```

---

## Code Conventions

### C#
- PascalCase for classes, methods, properties
- camelCase for private fields, parameters
- Prefix interfaces with `I` (e.g., `IRuleEngine`)
- Use `async`/`await` for I/O operations
- Dependency injection via constructor

### XAML
- Use WPF UI components (Wpf.Ui namespace)
- Binding over code-behind
- No business logic in code-behind
- DataContext set to ViewModel

### Tests
- In BrowserApp.Tests project
- Named: `ClassNameTests.cs`
- Arrange-Act-Assert pattern
- Write test first (TDD)

---

## Quality Gates

### Feature-Level
- [ ] Tests written BEFORE implementation
- [ ] Tests pass
- [ ] Code follows MVVM (UI ↔ ViewModel ↔ Service)
- [ ] No business logic in XAML code-behind
- [ ] Manual verification completed

### Phase-Level
- [ ] All phase todos completed
- [ ] All tests pass
- [ ] Phase deliverable works as specified
- [ ] Code review completed (code-reviewer agent)

### Never Skip
- Never claim "it works" without running it
- Never mark todo complete without verification
- Never merge phase without code review
- Never implement without tests

---

## Anti-Patterns to Avoid

| Bad | Good |
|-----|------|
| "It should work now" | "I verified it works by [evidence]" |
| Skipping tests "to save time" | TDD from start |
| "Let me try this..." | Systematic debugging |
| Marking todos complete prematurely | Verify first |
| Business logic in XAML code-behind | MVVM always |
| Guess-and-check debugging | Systematic investigation |

---

## Performance Targets

### Client
- **Cold Start:** < 2 seconds to main window
- **Navigation:** < 100ms overhead from interception
- **Rule Evaluation:** < 10ms per request
- **UI Responsiveness:** 60 FPS animations
- **Memory:** < 200MB base + WebView2 overhead

### Server
- **API Response:** < 200ms for queries
- **LLM Response:** 2-5 seconds (7B model)

---

## Session Start Checklist

1. Read this document (PROJECT_CONTEXT.md)
2. Check git status
3. Ask: "Where did we leave off?"
4. Create TodoWrite for current work
5. Use appropriate tools for the task

---

## Key Reminders

1. **Use skills** - `/feature-dev` for features, `/frontend-design` for UI
2. **Use agents** - `code-architect` before building, `code-reviewer` after
3. **Use context7** - Query docs before implementing unknown APIs
4. **Use playwright** - Test UI behavior, don't assume it works
5. **Keep it simple** - Don't over-engineer, implement only what's needed
6. **Verify everything** - Show evidence, don't just claim it works

---

## File Quick Reference

| What | Where |
|------|-------|
| Main window | `BrowserApp.UI/MainWindow.xaml` |
| Views | `BrowserApp.UI/Views/` |
| ViewModels | `BrowserApp.UI/ViewModels/` |
| Services | `BrowserApp.Core/Services/` |
| Models | `BrowserApp.Core/Models/` |
| DB Context (client) | `BrowserApp.Data/BrowserDbContext.cs` |
| DB Context (server) | `BrowserApp.Server/Data/ServerDbContext.cs` |
| Server Controllers | `BrowserApp.Server/Controllers/` |
| Docker config | `docker-compose.yml` |
| App settings (client) | `BrowserApp.UI/appsettings.json` |
| App settings (server) | `BrowserApp.Server/appsettings.json` |

---

## Data Flow Scenarios

### Scenario 1: User Browses Page

```
1. User enters URL → NavigationService.Navigate()
2. WebView2 starts navigation
3. RequestInterceptor hooks WebResourceRequested
4. For each request:
   a. RuleEngine.Evaluate(request, activeRules)
   b. If blocked → cancel request
   c. If allowed → NetworkLogger.Log(request)
5. NavigationCompleted fires
6. InjectionService checks for inject_css/inject_js rules
7. Execute injections via ExecuteScriptAsync
```

### Scenario 2: Employee Joins Channel

```
1. User → Settings → Channels → Join
2. Enter channel_id, password
3. POST /api/channels/{id}/join
4. Server validates password
5. Server returns channel rules as JSON
6. Browser:
   a. RuleRepository.SaveChannelRules(rules)
   b. Mark rules as "enforced"
   c. Start background sync (15 min)
7. UI shows "🔒 Enforced" badges
```

### Scenario 3: Download from Marketplace

```
1. User → Marketplace
2. Browse available rules
3. Click "Install"
4. GET /api/marketplace/rules/{id}
5. Browser shows preview
6. User confirms → RuleRepository.Save(rule)
7. Rule enabled immediately
```

---

**End of Project Context**

*This document should be the sole reference for all development sessions.*
