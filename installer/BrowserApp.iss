; Inno Setup script for Slate.
; Build:
;   1. winget install JRSoftware.InnoSetup       (one-time)
;   2. From repo root: ./publish.ps1
;   3. iscc installer\BrowserApp.iss             (or open in Inno Setup, F9)
; Output: publish\installer\Slate-Setup-0.1.0.exe

#define AppName        "Slate"
#define AppPublisher   "Slate"
#define AppVersion     "0.1.0"
#define ExeName        "BrowserApp.UI.exe"
#define AppIcon        "..\logo\slate-graphite.ico"

[Setup]
; AppId is the installer's stable identity. KEEP THIS GUID CONSTANT across versions
; so future installs upgrade-in-place instead of installing side-by-side.
AppId={{EA077E53-7595-4870-A505-4691CD2DA8A1}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\app-icon.ico
SetupIconFile={#AppIcon}
OutputDir=..\publish\installer
OutputBaseFilename={#AppName}-Setup-{#AppVersion}
Compression=lzma2/ultra
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
; Published app payload from publish.ps1.
Source: "..\publish\win-x64\*"; DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs
; App icon for Apps & Features and Start Menu shortcut (crisper than extracting
; from the exe at small sizes).
Source: "{#AppIcon}"; DestDir: "{app}"; DestName: "app-icon.ico"; \
    Flags: ignoreversion
; WebView2 evergreen bootstrapper (~2 MB).
; Download once from https://go.microsoft.com/fwlink/p/?LinkId=2124703
; and place at installer\MicrosoftEdgeWebview2Setup.exe before building.
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; \
    Flags: deleteafterinstall; Check: WebView2RuntimeMissing

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#ExeName}"; IconFilename: "{app}\app-icon.ico"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#ExeName}"; IconFilename: "{app}\app-icon.ico"; Tasks: desktopicon

[Run]
; Silently install WebView2 runtime if absent — no-op when already present.
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
    StatusMsg: "Installing WebView2 runtime..."; Check: WebView2RuntimeMissing; \
    Flags: waituntilterminated
; Optional post-install launch.
Filename: "{app}\{#ExeName}"; Description: "Launch {#AppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Leave %LOCALAPPDATA%\BrowserApp alone on uninstall so the user's profiles,
; bookmarks, and history survive a reinstall. Add an explicit cleanup tool
; later if a "wipe everything" option is wanted.

[Code]
function WebView2RuntimeMissing(): Boolean;
var
  Version: string;
begin
  // The evergreen WebView2 runtime registers its version under EdgeUpdate Clients.
  // Per Microsoft docs the canonical client GUID is the one below; on 64-bit
  // Windows it lives under the WOW6432Node hive even for system-wide installs.
  Result := not (
    RegQueryStringValue(HKLM,
      'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
      'pv', Version)
    or
    RegQueryStringValue(HKCU,
      'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
      'pv', Version)
  );
end;
