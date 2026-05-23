# Slate Browser

An AI-powered privacy browser built with C# WPF, WebView2, and Entity Framework Core. Features tabbed browsing, content blocking, network monitoring, rule-based page modification, a cloud-synced marketplace and channel system, an AI copilot sidebar (local or hosted LLM), and a distributable Windows installer.

## Features

### Browsing
- WebView2 (Chromium) engine with tabbed browsing
- Chrome-style tab strip: inline `+` after the last tab + sticky `+` when tabs overflow; vertical wheel scrolls horizontally
- Toggleable **bookmarks bar** under the address bar (`Ctrl+Shift+B`, persists across restarts; middle-click → new tab)
- Address bar with autocomplete (history + bookmarks, debounced)
- Session persistence (tabs restored on relaunch)
- Back/forward, refresh, home, zoom controls
- Configurable search engine (Google, Bing, DuckDuckGo, custom)
- Certificate error warnings with proceed/go-back

### Content Blocking & Rules
- Three independent blocking layers, all feeding the same dashboard counter:
  - **Custom rules** (block patterns, header mods, CSS/JS injection per-site with wildcard URL matching)
  - **`FilterListService`** — EasyList + EasyPrivacy (~137k patterns) parsed in C# instead of routed through `declarativeNetRequest`, so every block stays visible to the dashboard and network monitor. Surfaces in the UI as **"Included ABP"**. Toggling **Ad Blocker** in Settings disables it in real time (no restart).
  - **Adblock Plus extension** (bundled, ~80MB unpacked, toggled from Settings) — cosmetic filtering (network blocking layer is intentionally off so attribution stays end-to-end visible)
- 5 built-in templates (Privacy Mode, Block Ads, Hide Cookie Banners, Dark Mode, Hide Social Widgets)
- Rule manager UI with dense table layout, source attribution (Local / Marketplace / Channel / Enforced), priority system
- Rule marketplace and channel sharing
- **Privacy modes are functional** (not just labels):
  - **Relaxed** — only user-created (`local`) + channel-enforced rules apply
  - **Standard** — all enabled rules apply (default)
  - **Strict** — all enabled rules + built-in tracker hostname blocklist

### Privacy & Monitoring
- Real-time network request capture with filtering and CSV export
- Live privacy dashboard with session-scoped **Detected / Blocked / Saved** stats — `BlockingService` is the single source of truth, polled every 500ms so allowed-but-detected requests still tick the counter
- The network monitor reads the same `BlockingService` counters → both panels stay 1:1 in sync (clearing either one zeros both)
- Network monitor row attribution ("Blocked by") persists across sessions via DB columns — restart-safe
- **Expanded Network Monitor** modal (`1100×740`, opens from the sidebar's expand icon) — full DB history (capped at 5,000 rows), filter chips, in-memory URL/host search, detail pane with rule attribution + monospace URL + headers/timing placeholders
- Sidebar Network Monitor is trimmed to the most recent 100 rows for snappy redraws; the modal is the place to dig through everything
- All data stored locally (no cloud sync)
- Multi-profile support with isolated data directories

### AI Copilot Sidebar
- **Ollama-powered** AI assistant embedded in the sidebar — works with any Ollama model (default: Llama 3.2)
- **Markdown rendering** in assistant replies — headings, bullet/numbered lists, bold/italic, inline code, fenced code blocks with Copy button
- Follow-up prompt chips after each reply (Explain simpler / Tell me more / Key risks / Next steps)
- Regenerate-last-answer button (hover-revealed alongside Copy)
- Vision-model detection — screenshot button auto-hides when the selected model lacks vision support
- Page-context awareness — toggle to include current page text/URL in prompts
- **Remote LLM hosting** — optional bearer-token auth for hosted Ollama behind a Cloudflare Tunnel + Caddy proxy (see [LLM Hosting](docs/llm-hosting.md))

### Cloud Sync (Marketplace + Channels)
- **Marketplace** — cloud-hosted catalog of rule packs, browsable and installable with one click. 60-second auto-refresh while the workspace is visible.
- **Channels** — cloud-hosted team rule sharing. Public channels appear in the catalog; **private channels** are joinable via **"Join with code"** (channel ID + password).
- **Backend**: .NET 8 API on Fly.io + Neon Postgres (free tier). Deployment guide in [docs/deploy.md](docs/deploy.md).
- **Offline fallback** — when the server is unreachable, locally installed packs and joined channels still render from SQLite cache.

### Management
- Browsing history grouped by day (Today / Yesterday / weekday / date) with host avatar, page title, and time — single-click row to navigate, hover-revealed `…` menu (Open in new tab / Copy URL / Delete entry)
- Bookmarks (sidebar removed; lives in the bookmarks bar now)
- Download manager with file-type chip, source host, single-click row to open file, permanent folder icon to reveal in Explorer; inviting empty-state with **Choose folder** / **Open downloads** CTAs
- Extension support (Manifest V3 unpacked extensions, `.crx` install)
- **Debug console** with file-tail integration (`info_*.log` + `errors_*.log` via `FileSystemWatcher`), search, level/category filters, copy-entry context menu
- **Per-install unique identity** — each fresh install generates a `user_<6hex>` handle to prevent ownership impersonation across machines. Editable in Settings → Account with a Copy button for multi-machine pairing.

### UI / UX
- Lightened dark theme (warm obsidian surfaces, indigo accent), aligned to the Claude Design `browser-profiles/` bundle
- **Active-profile pill** in the title bar — avatar + name + privacy mode dot + live ABP shield indicator (visible when the ad blocker is on)
- 440px right-hand sidebar with 6 icon-tab panels (Copilot / Privacy Dashboard / Downloads / Network Monitor / History / Logs)
- Privacy dashboard hero "Mode" tile with radial accent glow, accent-strip stat tiles, softer rounded progress bars, segmented Quick Actions tile row
- Source attribution in the Rules list — slate dot for Local, amber bookmark notch for Marketplace, channel-hued stripe + pulsing dot for Channel rules, rose lock for Enforced
- Per-channel deterministic color identity (FNV-1a hash over channel name → 16-hue palette) — same channel renders the same color across avatar, name, and rule rows
- Workspace polish: dense table-style Rules grid, featured "Editor's Pick" hero card in Marketplace + 3-up grid, single-line channel rows with "Live · Nm ago" chip, compact profile rows
- Reusable empty-state slab (`Controls/WorkspaceEmptyState`) shared across Rules / Marketplace / Channels
- Centered modal tools workspace (`1280x820`, click-outside-to-close) for Rules, Extensions, Marketplace, Channels, Profiles, Settings
- Custom draggable titlebar with restore-on-drag from maximized, double-click maximize toggle
- Session recovery with auto-save (30s interval) and crash detection — sentinel deletion runs synchronously at the top of `OnExit` so it survives the WebView2 dispose latency
- **Lazy view resolution** at startup — only the default Copilot sidebar and the workspace container are eager-resolved; the other 5 sidebar panels and 6 workspace dialogs are deferred until first reveal, shaving ~300–500 ms off cold start
- **Profile recovery** — if `profiles.json` is missing or corrupt, `ProfileService` scans `%LOCALAPPDATA%\BrowserApp\Profiles\` for folders containing a `browser.db` and re-registers them automatically (the corrupt file is preserved as `profiles.json.corrupt_<ts>`). No more silent data orphaning if the index file is damaged
- Bundled Adblock Plus welcome / day-1 marketing tabs are suppressed on fresh installs (the extension still installs and functions normally — only the popup tabs are dropped)

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
└── BrowserApp.Tests/        # Unit tests
```

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Client UI** | WPF + [WPF UI](https://wpfui.lepo.co/) (Fluent Design 2.0) |
| **Browser Engine** | WebView2 (Edge/Chromium) |
| **Client DB** | SQLite + EF Core 8 |
| **Server API** | .NET 8 Web API (Fly.io) |
| **Server DB** | PostgreSQL (Neon) |
| **LLM** | Ollama (Llama 3.2) — local or hosted via Cloudflare Tunnel |
| **Architecture** | MVVM + CommunityToolkit.Mvvm, Repository Pattern |
| **Testing** | xUnit + Moq |

## Build & Run (Development)

Prerequisites: Windows 10/11, .NET 8 SDK, WebView2 Runtime

```bash
dotnet restore
dotnet build
dotnet run --project BrowserApp.UI
dotnet test
```

Server is optional for everyday browsing — only needed for Marketplace/Channels sync:
```bash
dotnet run --project BrowserApp.Server    # requires local PostgreSQL
```

## Packaging & Distribution

Slate ships as a Windows installer built with Inno Setup. The published binary
is self-contained (no .NET runtime required on the target machine), precompiled
with ReadyToRun for fast cold start, and NOT single-file (avoids first-launch
temp-extraction penalty).

```powershell
.\publish.ps1                         # produces publish\win-x64\
iscc installer\BrowserApp.iss         # produces publish\installer\Slate-Setup-0.1.0.exe
```

Prerequisites for building the installer:
- .NET 8 SDK
- [Inno Setup](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`)
- WebView2 bootstrapper at `installer\MicrosoftEdgeWebview2Setup.exe` ([download](https://go.microsoft.com/fwlink/p/?LinkId=2124703))

See [docs/deploy.md](docs/deploy.md) for cloud server deployment and
[docs/llm-hosting.md](docs/llm-hosting.md) for the self-hosted LLM setup.

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
4. Check blocking: logs show `BLOCKED: <url> by rule: <rule-name>` for custom rules and `BLOCKED: <url> by Filter List (EasyList/EasyPrivacy)` for the "Included ABP" path (FilterListService)
5. Wipe runtime extensions safely: `Remove-Item "$env:LOCALAPPDATA\BrowserApp\Extensions" -Recurse -Force` — the bundled ad blocker is re-materialized from `BrowserApp.UI/Resources/BuiltInExtensions/` on next launch
6. Dashboard / monitor counter mismatch: both panels read from `BlockingService` — if they drift, check `NetworkMonitorViewModel.SyncCountersFromBlockingService` and the `_liveCountersTimer` in `PrivacyDashboardViewModel`
7. Profile data appears empty / new "Default" profile after a launch: check `%LOCALAPPDATA%\BrowserApp\profiles.json` — if a `profiles.json.corrupt_<ts>` sibling exists, `ProfileService` ran folder-based recovery. Open the Profiles workspace to find your data registered as `Recovered (xxxxxxxx)` and rename if needed

## Documentation

- [Project Context](docs/project_context.md) — Architecture, decisions, and full development history
- [Deploy Guide](docs/deploy.md) — Deploying the server to Fly.io + Neon Postgres
- [LLM Hosting](docs/llm-hosting.md) — Self-hosting Ollama via Cloudflare Tunnel + Caddy

## Known Limitations

- YouTube video ads still play (server-side ad insertion)
- Some dynamic ad networks may bypass blocking
- CSS/JS injections occur after initial page load
- Profile switching requires app restart
- Copilot requires Ollama installed locally, or a hosted endpoint configured in `appsettings.json`

## Acknowledgments

- [WPF UI](https://github.com/lepoco/wpfui)
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
