# AI-Powered Privacy Browser

A modern, privacy-focused web browser built with C# WPF, WebView2, and Entity Framework Core. Features advanced content blocking, network monitoring, and rule-based page modification.

## 🚀 Current Status

**Phase 3 Complete** | 66/66 Tests Passing | All Systems Operational

- ✅ **Core Browser** - Full-featured WebView2 browser with modern UI
- ✅ **Network Monitoring** - Real-time request capture with filtering and export
- ✅ **Rule System** - Advanced blocking and injection with 5 pre-built templates

> **Note:** See [ISSUES.md](ISSUES.md) for known issues and improvement plan before Phase 4

## 📸 Features

### Core Browser
- WebView2-powered browsing (Chromium engine)
- Smart address bar (URL or search detection)
- Session persistence (cookies, passwords)
- Modern Fluent Design UI (WPF UI framework)
- Back/Forward navigation, Refresh, Home

### Network Monitor
- Capture all HTTP requests in real-time
- Filter by: Blocked, 3rd Party, Type (XHR, Script, Image, etc.)
- Export to CSV
- View request details (URL, method, status, size)
- Integrated sidebar with collapsible panel

### Rule System
- **Request Blocking** - Block ads, trackers, and unwanted content
- **CSS Injection** - Modify page styles (hide elements, dark mode)
- **JS Injection** - Execute scripts on page load
- **Template Library** - 5 pre-built rule sets:
  - Privacy Mode (blocks Google Analytics, Facebook Pixel, etc.)
  - Block Ads (blocks ad networks)
  - Hide Cookie Banners (CSS injection)
  - Force Dark Mode (CSS filter)
  - Hide Social Widgets
- **Rule Manager UI** - Enable/disable rules, load templates, view active rules
- **Wildcard Matching** - Flexible URL patterns (`*`, `?`)
- **Priority System** - Control rule evaluation order

## 🗂️ Project Structure

```
BrowserApp/
├── BrowserApp.UI/           # WPF application (presentation layer)
│   ├── Views/              # XAML windows and controls
│   ├── ViewModels/         # MVVM view models
│   ├── Services/           # UI services (navigation, interception)
│   ├── Resources/          # Embedded resources (rule templates)
│   └── ErrorLogger.cs      # File-based error logging
├── BrowserApp.Core/         # Business logic layer
│   ├── Models/             # Domain models (Rule, NetworkRequest)
│   ├── Services/           # Core services (RuleEngine, injectors)
│   ├── Interfaces/         # Service contracts
│   └── Utilities/          # Helper classes (UrlMatcher)
├── BrowserApp.Data/         # Data access layer
│   ├── Entities/           # EF Core entities
│   ├── Repositories/       # Data repositories
│   ├── Migrations/         # EF Core migrations
│   └── BrowserDbContext.cs # Database context
└── BrowserApp.Tests/        # Unit tests (66 tests)
```

## 🛠️ Technology Stack

- **Framework**: .NET 8 (Windows)
- **UI**: WPF with [WPF UI](https://wpfui.lepo.co/) (Fluent Design)
- **Browser Engine**: [WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/) (Chromium)
- **Database**: SQLite with Entity Framework Core 8.0
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Architecture**: MVVM with Repository Pattern
- **Testing**: xUnit with Moq

## 📦 Installation

### Prerequisites
- Windows 10/11
- .NET 8 SDK
- WebView2 Runtime (usually pre-installed)

### Build & Run

```bash
# Clone repository
git clone <repo-url>
cd browser

# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project BrowserApp.UI

# Run tests
dotnet test
```

## 📍 Data Locations

### Application Data
```
%LOCALAPPDATA%\BrowserApp\
├── browser.db              # SQLite database
├── UserData\               # WebView2 user data (cookies, sessions)
└── Logs\                   # Error and info logs
    ├── info_<date>.log     # Daily info log
    ├── errors_<date>.log   # Daily error log
    └── error_<timestamp>.txt  # Individual errors
```

### Database Schema
- `BrowsingHistory` - Visited URLs
- `NetworkLogs` - HTTP request history
- `Rules` - Blocking/injection rules
- `Settings` - App configuration
- `__EFMigrationsHistory` - Migration tracking

## 🔧 Configuration

### Database Migrations

```bash
# Create new migration (from BrowserApp.Data/)
dotnet ef migrations add MigrationName --startup-project ..\BrowserApp.UI

# Apply migrations (happens automatically on startup)
dotnet ef database update --startup-project ..\BrowserApp.UI

# Reset database (development)
Remove-Item "$env:LOCALAPPDATA\BrowserApp\browser.db" -Force
```

### Logging

All errors and events are logged to `%LOCALAPPDATA%\BrowserApp\Logs\`

```csharp
// Usage in code
ErrorLogger.LogInfo("Application started");
ErrorLogger.LogError("Operation failed", exception);

// View logs
explorer %LOCALAPPDATA%\BrowserApp\Logs
```

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~RuleEngineTests.Evaluate_WithBlockAction_ReturnsBlock"
```

**Test Coverage:**
- Core Services (RuleEngine, BlockingService, Injectors)
- Utilities (UrlMatcher)
- Repositories (RuleRepository, NetworkLogRepository)
- 66 passing tests

## 🛡️ Rule System

### Rule JSON Format

```json
{
  "id": "unique-guid",
  "name": "Block Ads",
  "description": "Blocks common ad networks",
  "site": "*",
  "priority": 95,
  "rules": [
    {
      "type": "block",
      "match": { "urlPattern": "*googlesyndication.com/*" }
    },
    {
      "type": "injectCss",
      "match": { "urlPattern": "https://example.com/*" },
      "css": ".ad { display: none !important; }"
    },
    {
      "type": "injectJs",
      "match": { "urlPattern": "*" },
      "js": "console.log('Hello from injected script');"
    }
  ]
}
```

### Creating Custom Rules

1. Open Rule Manager (Shield icon in toolbar)
2. Click "Load Template" for pre-built rules
3. Enable/disable rules as needed
4. Custom rules can be added via JSON files (manual import coming in Phase 4)

## 🐛 Troubleshooting

### App Won't Start
1. Check logs: `%LOCALAPPDATA%\BrowserApp\Logs\errors_<date>.log`
2. Delete database: `Remove-Item "$env:LOCALAPPDATA\BrowserApp\browser.db" -Force`
3. Verify WebView2 Runtime is installed

### Rules Not Working
1. Check logs for: `BlockingService initialized with X active rules`
2. Verify template was loaded: `Template 'Block Ads' added to database`
3. Look for blocking events: `BLOCKED: <url> by rule: <rule-name>`

### Database Errors
```powershell
# Reset database to clean state
Remove-Item "$env:LOCALAPPDATA\BrowserApp\*" -Recurse -Force
```

## 📚 Documentation

- [Quick Reference](docs/QUICK_REFERENCE.md) - Code snippets and API docs
- [Development Protocol](docs/DEVELOPMENT_PROTOCOL.md) - Coding standards
- [Skills Guide](docs/SKILLS_GUIDE.md) - Available skills/commands

## 🗺️ Roadmap

### Phase 4: Enhanced Features (Planned)
- [ ] Real-time log viewer UI
- [ ] Manual rule builder (visual rule editor)
- [ ] Advanced YouTube ad blocking (element hiding, auto-skip)
- [ ] Rule import/export
- [ ] Rule marketplace/sharing
- [ ] Per-site rule override
- [ ] Privacy dashboard (stats, bandwidth saved)

### Phase 5: AI Integration (Future)
- [ ] AI copilot sidebar
- [ ] Smart content summarization
- [ ] Automatic rule suggestions
- [ ] Natural language rule creation

## 📊 Performance

- **Startup Time**: ~500ms
- **Request Interception Overhead**: <10ms per request
- **Database Size**: ~5MB after 1000 network logs
- **Memory Usage**: ~150MB (base) + WebView2 overhead

## 🔒 Privacy Features

✅ **Currently Working:**
- Request blocking (ads, trackers)
- Privacy mode template (blocks analytics)
- Network monitoring (no data leaves device)
- Local storage only (no cloud sync)

⚠️ **Known Limitations:**
- YouTube video ads still play (server-side ad insertion)
- Some dynamic ad networks may bypass blocking
- CSS/JS injections occur after page load

## 🤝 Contributing

1. Follow MVVM architecture
2. Add tests for new features
3. Use EF Core migrations for schema changes
4. Log important events with ErrorLogger
5. Update documentation

## 📄 License

*License to be determined*

## 🙏 Acknowledgments

- [WPF UI](https://github.com/lepoco/wpfui) - Modern WPF controls
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Chromium browser engine
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - ORM framework

---

**Last Updated**: January 15, 2026
**Version**: Phase 3 Complete
**Build Status**: ✅ Passing (66/66 tests)
