# Single-Window Architecture And Dark Theme Overhaul Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Convert manager/settings flows to a true single-window UX, fix window dragging, and replace the current purple-heavy visual system with a premium dark-only theme.

**Architecture:** Keep `MainWindow` as the only app shell and move tools (Rules, Extensions, Marketplace, Channels, Profiles, Settings) into an in-window workspace host rendered inside `MainWindow`. Reuse existing viewmodels/services and migrate window content into reusable workspace user controls. Keep legacy dialog wrappers only as temporary compatibility shells, then remove launch paths.

**Tech Stack:** WPF (.NET 8), WPF UI (`FluentWindow`), MVVM Toolkit, existing XAML style dictionaries, xUnit for regression tests.

---

### Task 1: Restore Broken Manager Launches (Regression Hotfix)

**Files:**
- Modify: `BrowserApp.UI/Styles/SurfaceStyles.xaml`
- Modify: `BrowserApp.UI/Views/SettingsView.xaml`
- Modify: `BrowserApp.UI/Views/ExtensionManagerView.xaml`
- Modify: `BrowserApp.UI/Views/RuleManagerView.xaml`
- Modify: `BrowserApp.UI/Views/MarketplaceView.xaml`
- Modify: `BrowserApp.UI/Views/ChannelsView.xaml`
- Modify: `BrowserApp.UI/Views/ProfileSelectorView.xaml`
- Test: `BrowserApp.Tests/Styles/DialogWindowBehaviorTests.cs`

**Step 1: Write the failing test**
- Add a test asserting `ModernDialogWindowStyle` does not contain a `Setter` for `WindowStartupLocation`.
- Add/adjust tests to ensure each manager window explicitly sets `WindowStartupLocation="CenterOwner"` at root level.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~DialogWindowBehaviorTests" -v minimal`
Expected: FAIL showing invalid style assumptions.

**Step 3: Write minimal implementation**
- Remove `WindowStartupLocation` from `ModernDialogWindowStyle` (non-styleable CLR property).
- Add `WindowStartupLocation="CenterOwner"` directly to each manager window root element.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~DialogWindowBehaviorTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Styles/SurfaceStyles.xaml BrowserApp.UI/Views/SettingsView.xaml BrowserApp.UI/Views/ExtensionManagerView.xaml BrowserApp.UI/Views/RuleManagerView.xaml BrowserApp.UI/Views/MarketplaceView.xaml BrowserApp.UI/Views/ChannelsView.xaml BrowserApp.UI/Views/ProfileSelectorView.xaml BrowserApp.Tests/Styles/DialogWindowBehaviorTests.cs
git commit -m "fix(ui): restore manager windows by moving WindowStartupLocation to window roots"
```

### Task 2: Introduce Single-Window Workspace State

**Files:**
- Create: `BrowserApp.UI/Models/WorkspaceSection.cs`
- Modify: `BrowserApp.UI/ViewModels/MainViewModel.cs`
- Test: `BrowserApp.Tests/ViewModels/MainViewModelWorkspaceTests.cs`

**Step 1: Write the failing test**
- Add tests for:
  - `ActiveWorkspaceSection` default.
  - `IsWorkspaceOpen` toggling.
  - `OpenWorkspace(section)` sets section + opens workspace.
  - `CloseWorkspace()` closes without touching tab state.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainViewModelWorkspaceTests" -v minimal`
Expected: FAIL due missing properties/commands.

**Step 3: Write minimal implementation**
- Add enum members: `None`, `Rules`, `Extensions`, `Marketplace`, `Channels`, `Profiles`, `Settings`.
- Add `ActiveWorkspaceSection`, `IsWorkspaceOpen`, and relay commands in `MainViewModel`.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainViewModelWorkspaceTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Models/WorkspaceSection.cs BrowserApp.UI/ViewModels/MainViewModel.cs BrowserApp.Tests/ViewModels/MainViewModelWorkspaceTests.cs
git commit -m "feat(ui): add workspace state model for single-window tools"
```

### Task 3: Build Workspace Host In Main Window

**Files:**
- Create: `BrowserApp.UI/Views/Workspaces/WorkspaceHostView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/WorkspaceHostView.xaml.cs`
- Modify: `BrowserApp.UI/MainWindow.xaml`
- Modify: `BrowserApp.UI/MainWindow.xaml.cs`
- Modify: `BrowserApp.UI/App.xaml.cs`
- Test: `BrowserApp.Tests/Styles/MainWindowWorkspaceLayoutTests.cs`

**Step 1: Write the failing test**
- Add tests that assert `MainWindow.xaml` contains:
  - A workspace host container.
  - A close action control.
  - No direct reliance on modal manager windows for toolbar actions.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainWindowWorkspaceLayoutTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Add workspace panel/sheet overlay in `MainWindow` row 2.
- Register `WorkspaceHostView` in DI.
- Resolve and inject it in `MainWindow` constructor.
- Bind visibility/content to `MainViewModel` workspace properties.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainWindowWorkspaceLayoutTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Views/Workspaces/WorkspaceHostView.xaml BrowserApp.UI/Views/Workspaces/WorkspaceHostView.xaml.cs BrowserApp.UI/MainWindow.xaml BrowserApp.UI/MainWindow.xaml.cs BrowserApp.UI/App.xaml.cs BrowserApp.Tests/Styles/MainWindowWorkspaceLayoutTests.cs
git commit -m "feat(ui): add in-window workspace host to main shell"
```

### Task 4: Convert Tool Windows Into Reusable Workspace Pages

**Files:**
- Create: `BrowserApp.UI/Views/Workspaces/RulesWorkspaceView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/ExtensionsWorkspaceView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/MarketplaceWorkspaceView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/ChannelsWorkspaceView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/ProfilesWorkspaceView.xaml`
- Create: `BrowserApp.UI/Views/Workspaces/SettingsWorkspaceView.xaml`
- Modify: `BrowserApp.UI/Views/RuleManagerView.xaml`
- Modify: `BrowserApp.UI/Views/ExtensionManagerView.xaml`
- Modify: `BrowserApp.UI/Views/MarketplaceView.xaml`
- Modify: `BrowserApp.UI/Views/ChannelsView.xaml`
- Modify: `BrowserApp.UI/Views/ProfileSelectorView.xaml`
- Modify: `BrowserApp.UI/Views/SettingsView.xaml`
- Modify: `BrowserApp.UI/App.xaml.cs`

**Step 1: Write the failing test**
- Add tests asserting each workspace view exists and is resolved by DI.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~WorkspaceViewResolutionTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Extract each manager window body into a `UserControl` workspace view.
- Keep window wrappers thin (hosting the same workspace control) for temporary compatibility.
- Register workspace views in DI as singleton/transient consistent with existing viewmodel lifetimes.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~WorkspaceViewResolutionTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Views/Workspaces BrowserApp.UI/Views/RuleManagerView.xaml BrowserApp.UI/Views/ExtensionManagerView.xaml BrowserApp.UI/Views/MarketplaceView.xaml BrowserApp.UI/Views/ChannelsView.xaml BrowserApp.UI/Views/ProfileSelectorView.xaml BrowserApp.UI/Views/SettingsView.xaml BrowserApp.UI/App.xaml.cs
git commit -m "refactor(ui): extract manager screens into reusable workspace user controls"
```

### Task 5: Rewire Toolbar UX To Open Workspace (No Extra Windows)

**Files:**
- Modify: `BrowserApp.UI/MainWindow.xaml`
- Modify: `BrowserApp.UI/MainWindow.xaml.cs`
- Modify: `BrowserApp.UI/ViewModels/MainViewModel.cs`
- Test: `BrowserApp.Tests/Styles/MainToolbarInteractionTests.cs`

**Step 1: Write the failing test**
- Assert toolbar click handlers no longer call `ShowDialog()` for manager actions.
- Assert actions call workspace-open methods with correct section mappings.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainToolbarInteractionTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Replace modal-opening handlers (`RulesButton_Click`, etc.) with `OpenWorkspace(...)`.
- Move tool actions from crowded top-right cluster to a cleaner “Tools” surface in `WorkspaceHostView` (rail/list/menu).
- Keep one quick-access control in top bar (e.g., “Tools” button) plus essential nav controls only.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainToolbarInteractionTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/MainWindow.xaml BrowserApp.UI/MainWindow.xaml.cs BrowserApp.UI/ViewModels/MainViewModel.cs BrowserApp.Tests/Styles/MainToolbarInteractionTests.cs
git commit -m "feat(ui): route manager actions into single-window workspace"
```

### Task 6: Fix Main Window Drag Behavior

**Files:**
- Modify: `BrowserApp.UI/MainWindow.xaml`
- Modify: `BrowserApp.UI/MainWindow.xaml.cs`
- Test: `BrowserApp.Tests/Styles/MainWindowDragBehaviorTests.cs`

**Step 1: Write the failing test**
- Assert there is an explicit draggable region declaration in `MainWindow.xaml`.
- Assert code-behind includes drag handler entry points.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainWindowDragBehaviorTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Add dedicated drag region in top chrome area.
- In code-behind, handle mouse-down on non-interactive chrome and call `DragMove()`.
- Preserve maximize/restore behavior (double-click support) and avoid drag from address bar/buttons/tabs.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MainWindowDragBehaviorTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/MainWindow.xaml BrowserApp.UI/MainWindow.xaml.cs BrowserApp.Tests/Styles/MainWindowDragBehaviorTests.cs
git commit -m "fix(ui): restore draggable custom titlebar behavior"
```

### Task 7: Replace Purple Palette With Premium Neutral Dark Palette

**Files:**
- Modify: `BrowserApp.UI/Styles/BrowserTheme.xaml`
- Modify: `BrowserApp.UI/Styles/SidebarStyles.xaml`
- Modify: `BrowserApp.UI/Styles/AddressBarStyles.xaml`
- Modify: `BrowserApp.UI/Styles/TabStyles.xaml`
- Modify: `BrowserApp.UI/Controls/TabItemControl.xaml`
- Test: `BrowserApp.Tests/Styles/BrowserThemePaletteTests.cs`

**Step 1: Write the failing test**
- Assert accent palette no longer uses old purple hexes.
- Assert key dark-surface contrast resources are present.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~BrowserThemePaletteTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Replace old accent set with neutral-cool dark palette (graphite surfaces, blue/cyan accent).
- Keep same resource keys so existing bindings remain stable.
- Adjust hover/active/selection shades to improve legibility and “premium” contrast.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~BrowserThemePaletteTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Styles/BrowserTheme.xaml BrowserApp.UI/Styles/SidebarStyles.xaml BrowserApp.UI/Styles/AddressBarStyles.xaml BrowserApp.UI/Styles/TabStyles.xaml BrowserApp.UI/Controls/TabItemControl.xaml BrowserApp.Tests/Styles/BrowserThemePaletteTests.cs
git commit -m "feat(ui): apply premium dark palette and remove purple-heavy visuals"
```

### Task 8: Motion And Density Pass For Premium Feel

**Files:**
- Modify: `BrowserApp.UI/Styles/MotionStyles.xaml`
- Modify: `BrowserApp.UI/Styles/SurfaceStyles.xaml`
- Modify: `BrowserApp.UI/MainWindow.xaml`
- Test: `BrowserApp.Tests/Styles/MotionStylesTests.cs`

**Step 1: Write the failing test**
- Add tests for consistent non-blocking entrance styles and target durations/easing keys for workspace transitions.

**Step 2: Run test to verify it fails**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MotionStylesTests" -v minimal`
Expected: FAIL.

**Step 3: Write minimal implementation**
- Tune transition timing for workspace open/close and panel switches.
- Reduce jitter by avoiding conflicting opacity defaults.
- Normalize padding/radius scale for denser but readable high-end app feel.

**Step 4: Run test to verify it passes**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~MotionStylesTests" -v minimal`
Expected: PASS.

**Step 5: Commit**

```bash
git add BrowserApp.UI/Styles/MotionStyles.xaml BrowserApp.UI/Styles/SurfaceStyles.xaml BrowserApp.UI/MainWindow.xaml BrowserApp.Tests/Styles/MotionStylesTests.cs
git commit -m "polish(ui): refine motion and density for premium interaction feel"
```

### Task 9: Verification Gate

**Files:**
- Modify: `docs/project_context.md`

**Step 1: Run focused tests**

Run:
- `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj --filter "FullyQualifiedName~DialogWindowBehaviorTests|FullyQualifiedName~MainViewModelWorkspaceTests|FullyQualifiedName~MainWindowWorkspaceLayoutTests|FullyQualifiedName~MainToolbarInteractionTests|FullyQualifiedName~MainWindowDragBehaviorTests|FullyQualifiedName~BrowserThemePaletteTests|FullyQualifiedName~MotionStylesTests" -v minimal`

Expected: PASS for all targeted suites.

**Step 2: Run full tests**

Run: `dotnet test .\BrowserApp.Tests\BrowserApp.Tests.csproj -v minimal`
Expected: all passing.

**Step 3: Build UI**

Run: `dotnet build .\BrowserApp.UI\BrowserApp.UI.csproj -v minimal`
Expected: `0 Warning(s), 0 Error(s)`.

**Step 4: Manual UX checklist (Windows desktop)**
- Main window can be dragged from designated drag region.
- Clicking Rules/Extensions/Marketplace/Channels/Profiles/Settings opens in-app workspace, not separate taskbar windows.
- Workspace closes cleanly and returns focus to active tab.
- Theme is dark-only with reduced purple bias and readable contrast in grids/forms/lists.
- Animations are smooth and do not cause blank states or delayed content.

**Step 5: Commit docs**

```bash
git add docs/project_context.md
git commit -m "docs: update context for single-window architecture and dark theme v2"
```

