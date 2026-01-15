# Changelog

All notable changes to the AI-Powered Privacy Browser project will be documented in this file.

## [Phase 3 Complete] - 2026-01-15

### Added
- **Error Logging System** ([ErrorLogger.cs](BrowserApp.UI/ErrorLogger.cs))
  - File-based logging to `%LOCALAPPDATA%\BrowserApp\Logs\`
  - Separate files: `info_<date>.log`, `errors_<date>.log`, `error_<timestamp>.txt`
  - Thread-safe logging with lock mechanism
  - Logs startup events, rule loading, blocking events, and exceptions

- **EF Core Migrations**
  - Initial migration: `20260115135118_InitialCreate.cs`
  - Automatic migration on app startup via `Database.Migrate()`
  - Legacy database detection and recreation
  - Proper schema management for all tables (BrowsingHistory, NetworkLogs, Rules, Settings)

- **Rule System Logging**
  - BlockingService logs: Rule count on init, blocked requests with URL and rule name
  - RuleManagerViewModel logs: Template loading, rule engine reloads
  - Comprehensive request blocking logs: `BLOCKED: <url> by rule: <rule-name>`

- **Enhanced Error Handling**
  - Global exception handlers (AppDomain.UnhandledException, DispatcherUnhandledException)
  - Try-catch blocks in critical areas (MainWindow, RuleManager, Database init)
  - Detailed error messages with inner exceptions and stack traces

- **Embedded Resources Configuration**
  - Template JSON files configured as embedded resources in csproj
  - `<EmbeddedResource Include="Resources\DefaultRules\*.json" />`
  - Enables template loading without file system dependencies

### Fixed
- **Template Loading Crash**
  - Issue: `InverseBoolConverter` referenced before being added to resources
  - Fix: Moved converter definitions to XAML resources with `xmlns:views` namespace
  - File: [RuleManagerView.xaml](BrowserApp.UI/Views/RuleManagerView.xaml#L11-L14)

- **Database Schema Errors**
  - Issue: "no such table: Rules" when database existed from pre-migration version
  - Fix: Added automatic database recreation on migration failure
  - File: [App.xaml.cs](BrowserApp.UI/App.xaml.cs#L136-L168)

- **Missing Database Columns**
  - Issue: "no such column: Rules.ChannelId" and "Rules.Description"
  - Fix: Updated manual table creation SQL to include all RuleEntity properties
  - Later replaced with proper EF Core migrations

### Changed
- **Database Initialization**
  - From: Manual `EnsureCreated()` with manual table creation
  - To: EF Core `Database.Migrate()` with proper migration support
  - File: [App.xaml.cs](BrowserApp.UI/App.xaml.cs#L145)

- **Error Reporting**
  - From: MessageBox dialogs (caused UI crash loops)
  - To: File-based logging with silent error handling
  - Prevents app crashes, enables post-mortem debugging

- **Startup Logging**
  - Added detailed logging for each initialization step
  - Helps identify startup failures and performance issues
  - File: [App.xaml.cs](BrowserApp.UI/App.xaml.cs#L29-L76)

### Documentation
- **Updated** [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)
  - Added "Error Logging & Debugging" section with log locations and usage
  - Added "Database Management" section with migration commands
  - Added "Rule System Status" with architecture diagram
  - Added "Troubleshooting Guide" with common errors and solutions
  - Updated development checklist to mark Phase 3 complete

- **Created** [README.md](README.md)
  - Comprehensive project overview with current status
  - Technology stack and architecture details
  - Installation and configuration instructions
  - Data locations and database schema
  - Testing and troubleshooting guides

- **Created** [CHANGELOG.md](CHANGELOG.md) (this file)

### Testing
- All 66 tests passing
- Build: 0 warnings, 0 errors
- Manual testing: Template loading, rule blocking, network monitoring verified

### Known Issues
- **YouTube Video Ads**: Video ads still play due to server-side ad insertion from `googlevideo.com`. Display ads and tracking are blocked successfully.
- **CSS/JS Injection Timing**: Injections occur after NavigationCompleted, causing brief flash of original content
- **Ad Network Updates**: Ad networks change domains; templates may need periodic updates

### Performance
- Startup time: ~500ms
- Request blocking overhead: <10ms per request
- Log file accumulation: Consider implementing log rotation (future enhancement)

---

## [Phase 2 Complete] - 2025-12-XX

### Added
- Network monitoring with request capture
- DataGrid UI in sidebar for request display
- Filtering by blocked status, 3rd party, resource type
- CSV export functionality
- NetworkLogger service with SQLite persistence
- RequestInterceptor service for WebView2 integration

---

## [Phase 1 Complete] - 2025-11-XX

### Added
- Initial WPF project setup with .NET 8
- WebView2 browser control integration
- Modern Fluent Design UI with WPF UI framework
- Smart address bar with URL/search detection
- Back/Forward/Refresh navigation
- Session persistence (cookies, UserDataFolder)
- Password autofill support
- Basic project structure (UI, Core, Data layers)
- Entity Framework Core with SQLite
- MVVM architecture with CommunityToolkit.Mvvm

---

## Format

This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.

### Change Types
- `Added` - New features
- `Changed` - Changes to existing functionality
- `Deprecated` - Soon-to-be removed features
- `Removed` - Removed features
- `Fixed` - Bug fixes
- `Security` - Vulnerability fixes
