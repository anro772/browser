# Slate Installer

Produces `Slate-Setup-{version}.exe` — a self-contained Inno Setup installer
that drops the published browser into `Program Files`, creates a Start Menu
shortcut, and auto-installs the WebView2 runtime if missing.

## One-time setup

```powershell
winget install JRSoftware.InnoSetup
```

Then download the WebView2 evergreen bootstrapper once (it's ~2 MB) and save
it as `installer/MicrosoftEdgeWebview2Setup.exe`:

- Download URL: https://go.microsoft.com/fwlink/p/?LinkId=2124703
- The file is signed by Microsoft; it's a no-op on machines where the runtime
  is already installed (every Win 10 Aug-2021+ and all of Win 11).

This file is intentionally **not** committed yet — drop it in once and the
installer compiles.

## Build the installer

From the repo root:

```powershell
./publish.ps1               # publishes to publish\win-x64
iscc installer\BrowserApp.iss
```

The compiled setup lands at `publish\installer\Slate-Setup-0.1.0.exe`.

## Releasing a new version

1. Bump `<Version>` and `<FileVersion>` in `BrowserApp.UI/BrowserApp.UI.csproj`.
2. Bump `#define AppVersion` at the top of `installer/BrowserApp.iss`.
3. Rebuild (`./publish.ps1` then `iscc …`).
4. Running the new installer over an old one upgrades in place because the
   `AppId` GUID in the `.iss` is constant.

## Notes

- **No code signing yet.** SmartScreen will warn first-time users.
  Signing requires a code-signing cert and is a separate follow-up.
- **Profile data survives uninstall** — `%LOCALAPPDATA%\BrowserApp\` is left
  intact. Add a "delete my data" task later if needed.
- **Rebrand:** find-and-replace `Slate` in `BrowserApp.UI.csproj` and
  `installer/BrowserApp.iss` only. The internal assembly name stays
  `BrowserApp.UI.exe` so existing test profiles keep working.
