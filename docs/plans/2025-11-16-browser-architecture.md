# Browser Architecture Design
**Project:** AI-Powered Privacy Browser for Windows
**Date:** November 16, 2025
**Timeline:** 4-5 months development
**Platform:** Windows 10/11 only

---

## Executive Summary

A Windows-native, Chromium-based browser with AI-assisted browsing customization. Dual-purpose tool for:
1. **Businesses:** Monitor/control browsing, enforce policies via channels
2. **Power Users:** Lightweight browser with AI-powered privacy controls

**Key Features:**
- Built-in ad/tracker blocking with JSON rule system
- AI copilot sidebar for assistance and rule generation
- Business channels for policy enforcement
- Public marketplace for community rules
- Modern Windows 11 Fluent Design UI

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
- **Hosting:** Local development machine

### Key Libraries
- **WPF UI** (lepoco/wpfui) - Modern Fluent controls, Mica/Acrylic
- **Microsoft.Web.WebView2** - Browser engine
- **Microsoft.EntityFrameworkCore.Sqlite** - Local data
- **Npgsql.EntityFrameworkCore.PostgreSQL** - Server data
- **System.Net.Http** - LLM/API communication

---

## Architecture Overview

### High-Level Architecture (3-Tier)

```
┌─────────────────────────────────────────────────────┐
│              Client (Browser App)                   │
│  ┌───────────────────────────────────────────────┐  │
│  │ WPF UI Layer (Views, Styles, Animations)     │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ ViewModel Layer (UI Logic, Commands)         │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ Service Layer (Business Logic)               │  │
│  │  • BrowserService  • NetworkService          │  │
│  │  • RuleEngine      • InjectionService        │  │
│  │  • LLMClient       • SyncService             │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ Data Layer (SQLite - EF Core)                │  │
│  │  • Rules  • Profiles  • NetworkLogs          │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                          │
                HTTP/JSON │
                          ▼
┌─────────────────────────────────────────────────────┐
│              Server (Self-Hosted)                   │
│  ┌───────────────────────────────────────────────┐  │
│  │ REST API (.NET 8 Web API)                    │  │
│  │  • /api/marketplace/*                        │  │
│  │  • /api/channels/*                           │  │
│  │  • /api/ai/chat                              │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ PostgreSQL Database                          │  │
│  │  • MarketplaceRules  • Channels              │  │
│  │  • Users             • ChannelAuditLog       │  │
│  └───────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────┐  │
│  │ LLM Service (Ollama)                         │  │
│  │  • Model: Llama 3.2 7B / Mistral 7B          │  │
│  │  • Endpoint: http://localhost:11434          │  │
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

## Client Application Architecture

### Module Breakdown

#### 1. Browser Core Module
**Responsibility:** Core browsing functionality

**Components:**
- `MainWindow.xaml` - Main shell with WPF UI navigation
- `WebView2Host.cs` - WebView2 control wrapper
- `NavigationService.cs` - URL navigation, history, back/forward
- `TabManager.cs` - Future: multi-tab support (start single-tab)
- `SearchEngineService.cs` - Handle search queries (Google/DuckDuckGo/etc.)

**Features:**
- Address bar with URL/search detection
- Navigation controls (back, forward, refresh)
- WebView2 password manager (built-in Edge credentials)
- Session persistence (cookies, local storage)

#### 2. Network Interception Module
**Responsibility:** Monitor and control network requests

**Components:**
- `RequestInterceptor.cs` - Hooks `WebResourceRequested` event
- `RuleEngine.cs` - Evaluates requests against active rules
- `BlockingService.cs` - Blocks/allows based on rules
- `NetworkLogger.cs` - Logs to SQLite for monitoring

**Flow:**
```
Request → RequestInterceptor → RuleEngine
                                    ↓
                         Match rule? → Block / Allow
                                    ↓
                            NetworkLogger (SQLite)
```

#### 3. Content Injection Module
**Responsibility:** Inject CSS/JS into pages

**Components:**
- `CSSInjector.cs` - Inject stylesheets via `ExecuteScriptAsync`
- `JSInjector.cs` - Inject JavaScript for DOM manipulation
- `InjectionTimingService.cs` - Control timing (DOMContentLoaded, load)

**Capabilities:**
- Hide elements (cookie banners, ads)
- Auto-click buttons (reject cookies)
- Style modifications (dark mode, fonts)
- DOM manipulation (remove trackers)

#### 4. Rule Management Module
**Responsibility:** Store, sync, and apply rules

**Components:**
- `Rule.cs` - Model: id, name, site, rules array, enabled, priority
- `RuleRepository.cs` - SQLite CRUD operations
- `RuleSyncService.cs` - Sync with server (channels/marketplace)
- `ProfileManager.cs` - Switch between Work/Personal/Custom profiles
- `RuleParser.cs` - Parse JSON rules into executable objects

**Rule Storage:**
- Local: SQLite (user rules, cached channel rules)
- Server: PostgreSQL (marketplace, channels)

#### 5. AI Copilot Module
**Responsibility:** AI assistance and rule generation

**Components:**
- `CopilotSidebar.xaml` - Chat interface
- `LLMClient.cs` - HTTP client to server LLM endpoint
- `ContextProvider.cs` - Sends page URL/title/network requests to AI
- `ConversationManager.cs` - Maintains chat history
- `RuleGeneratorService.cs` - Parse AI JSON responses into rules

**Capabilities:**
- General chat ("Summarize this page")
- Privacy help ("Is this site safe?", "What trackers are here?")
- Rule generation ("Block Facebook trackers", "Hide cookie walls")

#### 6. UI/Presentation Layer
**Responsibility:** Visual components

**Components:**
- `SidebarPanel.xaml` - Collapsible right panel (350px)
- `NetworkMonitorView.xaml` - DataGrid with request logs
- `RuleBuilderView.xaml` - Manual rule creation form
- `SettingsView.xaml` - Preferences, profiles, server config
- `MarketplaceView.xaml` - Browse/download public rules
- `ChannelManagerView.xaml` - Join/manage business channels

---

## Rule System

### JSON Rule Format

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Privacy Pack - News Sites",
  "description": "Blocks trackers and hides cookie banners on news websites",
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
| `redirect` | Redirect to different URL | `from`, `to` (future) |
| `modify_headers` | Change request headers | `headers` (future) |

### Resource Types
- `script` - JavaScript files
- `stylesheet` - CSS files
- `image` - Images
- `xhr` / `fetch` - AJAX requests
- `document` - Main HTML page
- `font`, `media`, `websocket`, etc.

### Pre-built Templates

Ship with browser:
1. **Privacy Mode** - Block trackers (AdGuard-style lists)
2. **Hide Cookie Banners** - CSS for common consent dialogs
3. **Block Ads** - Common ad networks
4. **Dark Mode** - Force dark styles
5. **Reader Mode** - Simplify articles (future)

---

## Profile System

### Profile Types

**1. Personal Profile (Default)**
- User's custom rules
- Marketplace downloads
- Local storage only
- Optional cloud backup

**2. Work Profile (Business Mode)**
- Join business "Channels" via password
- Channel rules auto-sync from server
- Rules marked as "enforced" (cannot disable)
- Admin updates → instant sync to all members

**3. Custom Profiles**
- Named profiles: "Shopping", "Banking", "Social Media"
- Quick switch between rule sets
- Per-profile settings

### Channel System

**Admin Flow:**
1. Create channel via API/dashboard
2. Set name, description, password
3. Add rules to channel
4. Share Channel ID + password with employees

**Employee Flow:**
1. Settings → Channels → Join Channel
2. Enter Channel ID + password
3. Browser downloads all channel rules
4. Rules auto-sync every 15 minutes
5. Cannot disable enforced rules

**Database Schema (Server):**
```sql
CREATE TABLE Channels (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedBy UUID NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    IsActive BOOLEAN DEFAULT TRUE
);

CREATE TABLE ChannelRules (
    ChannelId UUID REFERENCES Channels(Id),
    RuleJson JSONB NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (ChannelId, (RuleJson->>'id'))
);

CREATE TABLE UserChannels (
    UserId UUID NOT NULL,
    ChannelId UUID REFERENCES Channels(Id),
    JoinedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (UserId, ChannelId)
);

CREATE TABLE ChannelAuditLog (
    Id UUID PRIMARY KEY,
    ChannelId UUID REFERENCES Channels(Id),
    UserId UUID,
    Action VARCHAR(50),  -- 'rule_blocked', 'page_visited'
    Metadata JSONB,
    Timestamp TIMESTAMP DEFAULT NOW()
);
```

---

## Server Architecture

### REST API Endpoints

**Marketplace:**
```
GET    /api/marketplace/rules              # List public rules
GET    /api/marketplace/rules/{id}         # Get specific rule
POST   /api/marketplace/rules              # Publish rule (auth required)
GET    /api/marketplace/rules/search?q=... # Search rules
```

**Channels:**
```
GET    /api/channels                       # List available channels
POST   /api/channels                       # Create channel (admin only)
GET    /api/channels/{id}                  # Get channel details
GET    /api/channels/{id}/rules            # Get all rules for channel
POST   /api/channels/{id}/join             # Join channel (password required)
DELETE /api/channels/{id}/leave            # Leave channel
POST   /api/channels/{id}/rules            # Add rule to channel (admin)
```

**AI:**
```
POST   /api/ai/chat                        # Send message to LLM
Body: {
  "message": "Block trackers on this site",
  "context": {
    "url": "https://example.com",
    "pageTitle": "Example Page",
    "activeRequests": [...]
  }
}
```

**Auth:**
```
POST   /api/auth/login                     # Simple username/password
POST   /api/auth/register                  # Create account
GET    /api/users/me                       # Current user info
```

### LLM Service

**Implementation:**
- Ollama running on `http://localhost:11434`
- Model: Llama 3.2 7B or Mistral 7B (fast enough for 7900XTX)
- .NET wrapper API at `/api/ai/chat`

**System Prompt:**
```
You are an AI browser assistant helping users with privacy and productivity.

Capabilities:
- Explain websites, trackers, and privacy risks
- Generate browser rules in JSON format to block/hide content
- Summarize pages and answer questions

Current page: {url}
Page title: {title}
Active network requests: {requests}

When generating rules, use this JSON format:
{
  "type": "block|inject_css|inject_js",
  "match": { "urlPattern": "..." },
  ...
}

Always explain what the rule does before applying.
```

---

## Data Flow Scenarios

### Scenario 1: User Browses Page (Offline)

```
1. User enters URL → NavigationService.Navigate()
2. WebView2 starts navigation
3. RequestInterceptor hooks WebResourceRequested event
4. For each request:
   a. RuleEngine.Evaluate(request, activeRules)
   b. If blocked → cancel request
   c. If allowed → NetworkLogger.Log(request)
5. NavigationCompleted event fires
6. InjectionService checks for inject_css/inject_js rules
7. Execute injections via ExecuteScriptAsync
8. NetworkMonitor UI updates with logged requests
```

### Scenario 2: User Asks AI for Help

```
1. User types: "Block trackers on this site"
2. CopilotViewModel collects context:
   - Current URL from NavigationService
   - Page title from WebView2
   - Recent requests from NetworkLogger
3. LLMClient.SendMessage(POST /api/ai/chat)
4. Server forwards to Ollama with system prompt
5. LLM generates response with JSON rule
6. CopilotViewModel shows preview panel
7. User clicks "Apply"
8. RuleRepository.Save(rule)
9. RuleEngine reloads rules
10. Page refreshes with rule active
```

### Scenario 3: Employee Joins Channel

```
1. User → Settings → Channels → Join
2. Enter channel_id="acme-corp", password="***"
3. Browser sends POST /api/channels/acme-corp/join
4. Server validates password
5. Server returns all channel rules as JSON array
6. Browser:
   a. RuleRepository.SaveChannelRules(rules)
   b. Mark rules as "enforced" in DB
   c. Start background sync (15 min interval)
7. RuleEngine reloads with enforced rules
8. UI shows "🔒 Enforced by Acme Corp" badges
```

### Scenario 4: Download from Marketplace

```
1. User → Settings → Marketplace
2. Browse available rules
3. Click "Install" on "Privacy Pack"
4. Browser sends GET /api/marketplace/rules/123
5. Server increments download_count
6. Browser shows preview dialog
7. User confirms → RuleRepository.Save(rule)
8. Rule enabled immediately
```

---

## UI/UX Design

### Main Window Layout

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
│                                    │                        │
│                                    │                        │
│                                    │                        │
│                                    │  [← Collapse]          │
├────────────────────────────────────┴────────────────────────┤
│ 🛡️ 47 blocked • 2.3 MB saved • Work Profile Active         │
└────────────────────────────────────────────────────────────┘
```

### Sidebar Tabs

**Tab 1: 💬 AI Copilot**
- Chat interface with message history
- AI response cards with rule previews
- "Apply" / "Dismiss" buttons for generated rules
- Context-aware responses (knows current page)

**Tab 2: 📊 Network Monitor**
- DataGrid with columns: URL, Status, Type, Size
- Filter buttons: All, Blocked, 3rd Party, Scripts, Images
- Export to CSV button
- Stats: Total requests, Blocked count, Data saved

**Tab 3: ⚙️ Rules Panel**
- List of active rules with toggle switches
- "🔒 Enforced" badges for channel rules
- Edit/Delete buttons for personal rules
- "+ New Rule" button for manual creation

### Color Scheme (Phase 1: Basic)

**Light Mode:**
- Background: `#F3F3F3`
- Surface: `#FFFFFF`
- Accent: `#0078D4` (Windows Blue)
- Success: `#107C10` (Green)
- Danger: `#D13438` (Red)

**Dark Mode:**
- Background: `#202020`
- Surface: `#2C2C2C`
- Accent: `#0078D4`
- Success: `#107C10`
- Danger: `#D13438`

**Phase 2: Enhanced (Future)**
- Glassmorphism backgrounds
- Gradient accents
- Mica/Acrylic effects
- Smooth animations (400ms with EaseOut)

### Typography
- Headers: Segoe UI 20pt Semibold
- Body: Segoe UI 14pt Regular
- Code/URLs: Consolas 12pt

---

## Development Phases

### Phase 1: Core Browser (Weeks 1-4)
**Goal:** Working browser with basic navigation

- ✅ WPF project setup with WPF UI
- ✅ WebView2 integration
- ✅ Address bar + navigation controls
- ✅ SQLite database setup (EF Core)
- ✅ Basic MVVM structure
- ✅ Search engine integration (Google/DDG)
- ✅ WebView2 password manager enabled

**Deliverable:** Can browse web, save history, looks clean

### Phase 2: Network Monitoring (Weeks 5-6)
**Goal:** See and log network activity

- ✅ RequestInterceptor implementation
- ✅ NetworkLogger to SQLite
- ✅ NetworkMonitor UI (DataGrid)
- ✅ Basic filtering (show/hide blocked)
- ✅ Export to CSV

**Deliverable:** Full visibility into network requests

### Phase 3: Rule System (Weeks 7-9)
**Goal:** Block requests and inject CSS/JS

- ✅ Rule model and JSON parser
- ✅ RuleEngine evaluation logic
- ✅ BlockingService implementation
- ✅ CSSInjector and JSInjector
- ✅ RuleRepository (SQLite CRUD)
- ✅ Manual rule creation UI
- ✅ Pre-built templates (5 default rules)

**Deliverable:** Users can create rules manually, block trackers

### Phase 4: Server + Marketplace (Weeks 10-11)
**Goal:** Central server for rules

- ✅ .NET Web API setup
- ✅ PostgreSQL database
- ✅ Marketplace endpoints
- ✅ MarketplaceView UI in browser
- ✅ Rule sync service
- ✅ Upload/download rules

**Deliverable:** Users can share rules via marketplace

### Phase 5: Business Channels (Week 12)
**Goal:** Multi-user policy enforcement

- ✅ Channel creation endpoints
- ✅ Channel join/leave logic
- ✅ Enforced rules (cannot disable)
- ✅ Background sync (15 min interval)
- ✅ ChannelManager UI
- ✅ Audit logging

**Deliverable:** Businesses can deploy policies to employees

### Phase 6: AI Integration (Weeks 13-15)
**Goal:** AI copilot for assistance and rule generation

- ✅ Ollama setup (7B model)
- ✅ LLM wrapper API
- ✅ CopilotSidebar UI
- ✅ Context provider (page info, network requests)
- ✅ Rule generation from natural language
- ✅ AI response preview + apply flow

**Deliverable:** Users can ask AI to generate rules

### Phase 7: Profiles & Settings (Week 16)
**Goal:** Multiple profiles, preferences

- ✅ ProfileManager implementation
- ✅ Work/Personal/Custom profiles
- ✅ Profile switching UI
- ✅ Settings panel (server config, preferences)
- ✅ Cloud backup for personal rules (optional)

**Deliverable:** Users can switch between rule sets

### Phase 8: Polish & Testing (Weeks 17-20)
**Goal:** Production-ready, thesis-demo quality

- ✅ Bug fixes and edge cases
- ✅ Performance optimization
- ✅ UI polish (consistent spacing, icons)
- ✅ Error handling and user feedback
- ✅ Help/onboarding screens
- ✅ Demo preparation (laptop ↔ main PC setup)
- ✅ Documentation for thesis

**Deliverable:** Stable, demo-ready browser

### Phase 9 (Optional): Visual Enhancements
**Goal:** Add glassmorphism, animations, effects

- ⏸️ Glassmorphism sidebar
- ⏸️ Smooth animations (400ms EaseOut)
- ⏸️ Micro-interactions (hover, press effects)
- ⏸️ Mica background
- ⏸️ Gradient accents
- ⏸️ Loading states and skeletons

**Deliverable:** Stunning visual presentation

---

## Project Structure

```
BrowserApp/
├── BrowserApp.sln
│
├── BrowserApp.UI/                      # WPF Application
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
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
│   │   ├── CopilotViewModel.cs
│   │   ├── NetworkMonitorViewModel.cs
│   │   ├── RuleBuilderViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Styles/
│   │   ├── BasicTheme.xaml             # Phase 1
│   │   ├── Colors.xaml
│   │   ├── Animations.xaml             # Phase 9 (optional)
│   │   └── Glassmorphism.xaml          # Phase 9 (optional)
│   │
│   ├── Controls/
│   │   ├── RuleCard.xaml               # Custom controls
│   │   └── NetworkRequestRow.xaml
│   │
│   └── Resources/
│       ├── Icons/
│       └── DefaultRules/               # Pre-built templates
│           ├── privacy-mode.json
│           ├── cookie-banners.json
│           └── block-ads.json
│
├── BrowserApp.Core/                    # Business Logic
│   ├── Services/
│   │   ├── BrowserService.cs
│   │   ├── NavigationService.cs
│   │   ├── NetworkService.cs
│   │   ├── RequestInterceptor.cs
│   │   ├── RuleEngine.cs
│   │   ├── BlockingService.cs
│   │   ├── CSSInjector.cs
│   │   ├── JSInjector.cs
│   │   ├── InjectionTimingService.cs
│   │   ├── ProfileManager.cs
│   │   ├── RuleSyncService.cs
│   │   └── SearchEngineService.cs
│   │
│   ├── Models/
│   │   ├── Rule.cs
│   │   ├── RuleMatch.cs
│   │   ├── NetworkRequest.cs
│   │   ├── Profile.cs
│   │   ├── Channel.cs
│   │   └── MarketplaceRule.cs
│   │
│   ├── Interfaces/
│   │   ├── IBrowserService.cs
│   │   ├── IRuleEngine.cs
│   │   ├── INetworkService.cs
│   │   └── ILLMClient.cs
│   │
│   └── Utilities/
│       ├── UrlMatcher.cs
│       └── RuleValidator.cs
│
├── BrowserApp.Data/                    # Data Access (Client)
│   ├── BrowserDbContext.cs
│   ├── Entities/
│   │   ├── RuleEntity.cs
│   │   ├── ProfileEntity.cs
│   │   ├── NetworkLogEntity.cs
│   │   └── ChannelCacheEntity.cs
│   │
│   ├── Repositories/
│   │   ├── RuleRepository.cs
│   │   ├── ProfileRepository.cs
│   │   └── NetworkLogRepository.cs
│   │
│   └── Migrations/
│
├── BrowserApp.AI/                      # AI Integration
│   ├── LLMClient.cs
│   ├── PromptBuilder.cs
│   ├── ContextProvider.cs
│   └── RuleGeneratorService.cs
│
└── BrowserApp.Server/                  # Server Application
    ├── Program.cs
    ├── appsettings.json
    │
    ├── Controllers/
    │   ├── MarketplaceController.cs
    │   ├── ChannelsController.cs
    │   ├── AIController.cs
    │   └── AuthController.cs
    │
    ├── Services/
    │   ├── OllamaService.cs
    │   ├── ChannelService.cs
    │   └── MarketplaceService.cs
    │
    ├── Data/
    │   ├── ServerDbContext.cs
    │   ├── Entities/
    │   │   ├── Channel.cs
    │   │   ├── ChannelRule.cs
    │   │   ├── MarketplaceRule.cs
    │   │   ├── User.cs
    │   │   └── UserChannel.cs
    │   │
    │   └── Migrations/
    │
    └── Models/
        ├── Requests/
        └── Responses/
```

---

## Database Schemas

### Client Database (SQLite)

```sql
-- Rules table
CREATE TABLE Rules (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    Site TEXT NOT NULL,            -- "*" for global, domain for specific
    Enabled INTEGER DEFAULT 1,
    Priority INTEGER DEFAULT 10,
    RulesJson TEXT NOT NULL,       -- JSON array of rule objects
    Source TEXT,                   -- "local", "marketplace", "channel"
    ChannelId TEXT,                -- NULL for local/marketplace rules
    IsEnforced INTEGER DEFAULT 0,  -- 1 for channel rules
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Profiles table
CREATE TABLE Profiles (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,            -- "Personal", "Work", "Custom"
    Type TEXT NOT NULL,            -- "personal", "work", "custom"
    IsActive INTEGER DEFAULT 0,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- ProfileRules (many-to-many)
CREATE TABLE ProfileRules (
    ProfileId TEXT NOT NULL,
    RuleId TEXT NOT NULL,
    PRIMARY KEY (ProfileId, RuleId),
    FOREIGN KEY (ProfileId) REFERENCES Profiles(Id),
    FOREIGN KEY (RuleId) REFERENCES Rules(Id)
);

-- NetworkLogs table
CREATE TABLE NetworkLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Url TEXT NOT NULL,
    Method TEXT,
    StatusCode INTEGER,
    ResourceType TEXT,
    ContentType TEXT,
    Size INTEGER,
    WasBlocked INTEGER DEFAULT 0,
    BlockedByRuleId TEXT,
    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BlockedByRuleId) REFERENCES Rules(Id)
);

-- ChannelCache (cached channel info)
CREATE TABLE ChannelCache (
    ChannelId TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    JoinedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    LastSyncedAt TEXT
);

-- BrowsingHistory (optional)
CREATE TABLE BrowsingHistory (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Url TEXT NOT NULL,
    Title TEXT,
    VisitedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Settings table
CREATE TABLE Settings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

### Server Database (PostgreSQL)

```sql
-- Users table
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) DEFAULT 'user',  -- 'user', 'admin'
    CreatedAt TIMESTAMP DEFAULT NOW()
);

-- MarketplaceRules table
CREATE TABLE MarketplaceRules (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    AuthorId UUID NOT NULL,
    RulesJson JSONB NOT NULL,
    Tags TEXT[],
    DownloadCount INTEGER DEFAULT 0,
    Rating DECIMAL(3,2) DEFAULT 0.0,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (AuthorId) REFERENCES Users(Id)
);

-- Channels table
CREATE TABLE Channels (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(100) NOT NULL,
    Description TEXT,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedBy UUID NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (CreatedBy) REFERENCES Users(Id)
);

-- ChannelRules table
CREATE TABLE ChannelRules (
    ChannelId UUID NOT NULL,
    RuleId UUID NOT NULL,
    RulesJson JSONB NOT NULL,
    CreatedAt TIMESTAMP DEFAULT NOW(),
    UpdatedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (ChannelId, RuleId),
    FOREIGN KEY (ChannelId) REFERENCES Channels(Id) ON DELETE CASCADE
);

-- UserChannels (membership)
CREATE TABLE UserChannels (
    UserId UUID NOT NULL,
    ChannelId UUID NOT NULL,
    JoinedAt TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY (UserId, ChannelId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (ChannelId) REFERENCES Channels(Id) ON DELETE CASCADE
);

-- ChannelAuditLog (optional analytics)
CREATE TABLE ChannelAuditLog (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    ChannelId UUID NOT NULL,
    UserId UUID,
    Action VARCHAR(50) NOT NULL,  -- 'rule_blocked', 'page_visited'
    Metadata JSONB,
    Timestamp TIMESTAMP DEFAULT NOW(),
    FOREIGN KEY (ChannelId) REFERENCES Channels(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

---

## Configuration Files

### Client: appsettings.json

```json
{
  "Server": {
    "ApiBaseUrl": "http://localhost:5000",
    "LLMEndpoint": "http://localhost:11434"
  },
  "UI": {
    "Theme": "Auto",
    "SidebarWidth": 350,
    "EnableAnimations": true,
    "AnimationSpeed": "Normal"
  },
  "Browser": {
    "DefaultSearchEngine": "Google",
    "UserDataFolder": "%LOCALAPPDATA%/BrowserApp/UserData",
    "EnablePasswordManager": true
  },
  "Sync": {
    "ChannelSyncIntervalMinutes": 15,
    "AutoSyncOnStartup": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
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
    "Model": "llama3.2:7b",
    "Temperature": 0.7,
    "MaxTokens": 2000
  },
  "Authentication": {
    "JwtSecret": "your-secret-key-here",
    "TokenExpirationMinutes": 1440
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

---

## Security Considerations

### Client
- **Password Storage:** WebView2 built-in (Edge credential manager)
- **Local Data:** SQLite database (unencrypted for MVP, can add DPAPI later)
- **Network:** HTTPS for server communication (certificate validation)

### Server
- **Authentication:** JWT tokens (simple username/password)
- **Channel Passwords:** BCrypt hashing
- **User Passwords:** BCrypt hashing
- **API Rate Limiting:** Basic throttling on AI endpoints
- **Input Validation:** Sanitize rule JSON, validate URLs

### Rules
- **Validation:** Check for dangerous patterns (no wildcard localhost, etc.)
- **Sandboxing:** WebView2 runs in separate process
- **User Consent:** Always show preview before applying AI-generated rules

---

## Performance Targets

### Client
- **Cold Start:** < 2 seconds to main window
- **Navigation:** < 100ms overhead from interception
- **Rule Evaluation:** < 10ms per request
- **UI Responsiveness:** 60 FPS animations (when enabled)
- **Memory:** < 200MB base + WebView2 overhead

### Server
- **API Response:** < 200ms for marketplace/channel queries
- **LLM Response:** 2-5 seconds for rule generation (7B model)
- **Concurrent Users:** Handle 10-20 simultaneous connections (demo scale)

### Network
- **Sync:** < 1MB data transfer for channel sync
- **Marketplace:** Lazy loading, paginated results

---

## Testing Strategy

### Unit Tests
- Core services (RuleEngine, RuleParser, UrlMatcher)
- Rule validation logic
- Profile switching
- Network request matching

### Integration Tests
- WebView2 navigation
- Request interception and blocking
- CSS/JS injection
- Database operations (CRUD)

### Manual Testing
- Browse top 20 websites with rules active
- AI rule generation accuracy
- Channel join/sync workflow
- Profile switching
- Export/import rules

### Demo Preparation
- Test on laptop → main PC connection (Tailscale)
- Verify LLM responds within 5 seconds
- Prepare 5-10 demo scenarios
- Record video backup in case of live demo issues

---

## Deployment

### Client Distribution
- **Installer:** MSIX package (Windows 10/11)
- **Dependencies:** .NET 8 Runtime, WebView2 Runtime (auto-install)
- **Size:** ~50-80MB installer

### Server Setup (Development)
- **PostgreSQL:** Docker container or local install
- **Ollama:** Download and install from ollama.ai
- **API:** Run via `dotnet run` or publish as self-contained executable

### Demo Setup
1. Main PC: Run server (API + Ollama)
2. Main PC: Test browser locally
3. Laptop: Connect via Tailscale
4. Laptop: Configure client to point to main PC IP

---

## Future Enhancements (Post-Thesis)

### Features
- Multi-tab support
- Bookmarks and favorites
- Browser extensions API (limited subset)
- Screenshot comparison (before/after rules)
- Advanced analytics dashboard
- Import/export profiles
- Scheduled rule activation (work hours only)

### Technical
- Migrate to WinUI 3 (if stability improves)
- Cross-platform (Avalonia UI)
- Local-only mode (no server dependency)
- Encrypted local database (DPAPI)
- Custom DNS-over-HTTPS
- Certificate pinning

### AI
- Multi-model support (switch between OpenAI/Claude/local)
- Fine-tuned model on adblock rules
- Automatic rule suggestions based on browsing
- Privacy score for websites

---

## Success Metrics (Thesis Evaluation)

### Quantitative
- **Blocking Effectiveness:** % of trackers blocked vs uBlock Origin
- **Performance:** Overhead added to page load time
- **Rule Accuracy:** AI-generated rules success rate (work as intended)
- **User Efficiency:** Time saved per browsing session

### Qualitative
- **System Usability Scale (SUS):** Target ≥ 75
- **User Satisfaction:** Survey feedback
- **Ease of Use:** Can non-technical users create rules?
- **AI Usefulness:** Do users prefer AI vs manual rule creation?

### Technical
- **Stability:** Crash-free browsing for 1-hour sessions
- **Compatibility:** Works on top 50 websites without breakage
- **Sync Reliability:** Channel rules sync without conflicts

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| WebView2 limitations | High | Have CefSharp as backup plan |
| LLM hallucinations | Medium | Always show rule preview, require user approval |
| Performance overhead | Medium | Optimize RuleEngine, use caching |
| Rule conflicts | Medium | Priority system, last-write-wins |
| Server downtime | Low | Offline-first design, local cache |
| AMD GPU compatibility | Low | Fallback to CPU mode for Ollama |
| Network issues (demo) | Medium | Record video backup, test Tailscale beforehand |

---

## Conclusion

This architecture provides a solid foundation for a thesis-quality browser with:
- ✅ Clean separation of concerns (MVVM + services)
- ✅ Offline-first design (works without server)
- ✅ Scalable rule system (JSON-based, extensible)
- ✅ Modern UI (WPF UI with Fluent Design)
- ✅ AI integration (rule generation, assistance)
- ✅ Business features (channels, enforcement)
- ✅ Room for visual enhancements (phase 9)

**Timeline:** 4-5 months is realistic for Phases 1-8, with Phase 9 as optional polish.

**Next Steps:**
1. Set up development environment (Visual Studio 2022, .NET 8, PostgreSQL, Ollama)
2. Initialize WPF project with WPF UI
3. Start Phase 1: Core browser navigation
