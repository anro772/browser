# Privacy Browser

A privacy-focused browser built with C# WPF, WebView2, and Entity Framework Core. It includes tabbed browsing, content blocking, network monitoring, rule-based page modification, session persistence, extension support, and a single-window tools workspace.

## Status

**Phase 10 Complete** | 364 tests passing

## Features

### Browsing
- WebView2 (Chromium) engine with tabbed browsing
- Address bar with autocomplete (history + bookmarks, debounced)
- Session persistence (tabs restored on relaunch)
- Back/forward, refresh, home, zoom controls
- Configurable search engine (Google, Bing, DuckDuckGo, custom)
- Certificate error warnings with proceed/go-back

### Content Blocking & Rules
- Request blocking (ads, trackers, custom patterns)
- CSS/JS injection per-site with wildcard URL matching
- 5 built-in templates (Privacy Mode, Block Ads, Hide Cookie Banners, Dark Mode, Hide Social Widgets)
- Rule manager UI with priority system
- Rule marketplace and channel sharing

### Privacy & Monitoring
- Real-time network request capture with filtering and CSV export
- Privacy dashboard with blocking stats
- All data stored locally (no cloud sync)
- Multi-profile support with isolated data directories

### Management
- Browsing history with search
- Bookmarks
- Download manager with progress tracking
- Extension support (Manifest V3 unpacked extensions)
- Log viewer

### UI / UX
- Dark graphite/blue theme (dark mode only)
- Single-window tools workspace for Rules, Extensions, Marketplace, Channels, Profiles, and Settings
- Custom draggable titlebar with modern overlay transitions

## Project Structure

```
BrowserApp/
├── BrowserApp.UI/           # WPF application (presentation)
│   ├── Views/               # XAML views, dialogs, and workspace pages
│   ├── ViewModels/          # MVVM view models
│   ├── Models/              # UI models (BrowserTabItem, DownloadItemModel)
│   ├── Controls/            # Custom controls (DownloadNotification, CertificateWarningBar)
│   ├── Services/            # RequestInterceptor, CSS/JS injectors, ProfileService
│   ├── Converters/          # WPF value converters
│   └── Resources/           # Rule templates
├── BrowserApp.Core/         # Business logic
│   ├── Models/              # Domain models (Rule, NetworkRequest)
│   ├── Services/            # RuleEngine, BlockingService
│   └── Interfaces/          # Service contracts
├── BrowserApp.Data/         # Data access (EF Core + SQLite)
│   ├── Entities/            # DB entities
│   ├── Repositories/        # Data repositories
│   └── Migrations/          # EF Core migrations
├── BrowserApp.Server/       # Marketplace API server
└── BrowserApp.Tests/        # Unit tests (364 tests)
```

## Tech Stack

- **.NET 8** (Windows) with **WPF** + [WPF UI](https://wpfui.lepo.co/) (Fluent Design)
- **WebView2** (Chromium engine)
- **SQLite** with Entity Framework Core 8
- **MVVM** with CommunityToolkit.Mvvm, Repository Pattern
- **xUnit** + Moq for testing

## Build & Run

Prerequisites: Windows 10/11, .NET 8 SDK, WebView2 Runtime

```bash
dotnet restore
dotnet build
dotnet run --project BrowserApp.UI
dotnet test
```

## Data Locations

```
%LOCALAPPDATA%\BrowserApp\
├── Profiles\{profile-id}\
│   ├── browser.db            # SQLite database
│   └── UserData\             # WebView2 data (cookies, sessions)
├── active_profile.txt        # Current profile ID
└── Logs\
    ├── info_<date>.log
    └── errors_<date>.log
```

### Database Tables
- `Rules`, `Settings` - App configuration
- `BrowsingHistory`, `Bookmarks` - User data
- `Downloads`, `Extensions` - Management
- `TabSessions` - Session persistence
- `NetworkLogs` - Request history
- `Channels`, `ChannelMemberships` - Rule sharing

## Migrations

```bash
# Migrations run automatically on startup. Manual commands:
dotnet ef migrations add MigrationName --project BrowserApp.Data --startup-project BrowserApp.UI
dotnet ef database update --project BrowserApp.Data --startup-project BrowserApp.UI
```

## Troubleshooting

1. Check logs at `%LOCALAPPDATA%\BrowserApp\Logs\`
2. Reset database: `Remove-Item "$env:LOCALAPPDATA\BrowserApp\Profiles" -Recurse -Force`
3. Verify WebView2 Runtime is installed
4. Check blocking: logs show `BLOCKED: <url> by rule: <rule-name>`

## Documentation

- [Project Context](docs/project_context.md) - Architecture, decisions, and development history

## Known Limitations

- YouTube video ads still play (server-side ad insertion)
- Some dynamic ad networks may bypass blocking
- CSS/JS injections occur after initial page load
- Profile switching requires app restart

## Acknowledgments

- [WPF UI](https://github.com/lepoco/wpfui)
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)

---

**Last Updated**: February 17, 2026 | **Build**: 364/364 tests passing
