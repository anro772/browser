# Quick Reference - WebView2 & WPF UI
**Project:** AI-Powered Privacy Browser
**Purpose:** Essential documentation links and code snippets for development

---

## 📚 Official Documentation Links

### **WebView2 Core Documentation**

1. **[Get Started with WebView2 in WPF](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf)**
   - Basic WPF project setup
   - WebView2 initialization
   - Navigation and address bar
   - Event handling basics

2. **[Custom Management of Network Requests](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/webresourcerequested)**
   - WebResourceRequested event
   - Blocking requests
   - Modifying headers
   - Resource type filtering

3. **[Call Web-Side Code from Native-Side Code](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/javascript)**
   - ExecuteScriptAsync usage
   - JavaScript injection
   - CSS injection patterns
   - Interop between C# and JS

4. **[Manage User Data Folders](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/user-data-folder)**
   - UserDataFolder configuration
   - Cookie and session persistence
   - Multiple WebView2 instances
   - Storage location management

5. **[Basic Authentication for WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/basic-authentication)**
   - Password autofill settings
   - BasicAuthenticationRequested event
   - Credential storage

6. **[Overview of WebView2 Features and APIs](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/overview-features-apis)**
   - Complete API reference
   - Feature capabilities
   - Platform support

7. **[WebView2 Release Notes](https://learn.microsoft.com/en-us/microsoft-edge/webview2/release-notes/)**
   - Latest updates
   - Breaking changes
   - New features

### **API Reference**

8. **[CoreWebView2 Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2)**
   - Main WebView2 API surface
   - Methods and properties
   - Event definitions

9. **[CoreWebView2Settings Class](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2settings)**
   - Browser settings configuration
   - Password autofill toggle
   - JavaScript enabled/disabled

10. **[WebView2 WPF Control](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.wpf.webview2)**
    - WPF-specific control API
    - XAML integration
    - CreationProperties

### **WPF UI (Modern Styling)**

11. **[WPF UI Documentation](https://wpfui.lepo.co/)**
    - Getting started guide
    - Control reference
    - Theming and styling

12. **[WPF UI GitHub Repository](https://github.com/lepoco/wpfui)**
    - Source code
    - Demo applications
    - Issue tracking

13. **[WPF UI Gallery App](https://apps.microsoft.com/store/detail/wpf-ui/9N9LKV8R9VGM)**
    - Interactive control showcase
    - Install: `winget install 'WPF UI'`

---

## 🔧 Essential Code Snippets

### **1. Basic WebView2 Setup (WPF)**

#### XAML:
```xml
<Window x:Class="BrowserApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
        Title="Browser" Height="800" Width="1200">
    <Grid>
        <wv2:WebView2 Name="webView" Source="https://www.google.com" />
    </Grid>
</Window>
```

#### C# (Code-behind):
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeAsync();
    }

    async void InitializeAsync()
    {
        await webView.EnsureCoreWebView2Async(null);

        // Now CoreWebView2 is available
        webView.CoreWebView2.Settings.IsPasswordAutofillEnabled = true;
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
    }
}
```

### **2. Configure UserDataFolder (Persistent Sessions)**

```csharp
// Set custom user data folder for cookie/session persistence
string userDataFolder = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "BrowserApp",
    "UserData"
);

// Method 1: Using CreationProperties (before initialization)
webView.CreationProperties = new CoreWebView2CreationProperties
{
    UserDataFolder = userDataFolder
};

// Method 2: Using CoreWebView2Environment
var environment = await CoreWebView2Environment.CreateAsync(
    browserExecutableFolder: null,
    userDataFolder: userDataFolder
);
await webView.EnsureCoreWebView2Async(environment);
```

### **3. Navigation and Address Bar**

```csharp
// Detect if input is URL or search query
private void NavigateToUrlOrSearch(string input)
{
    string url;

    if (IsValidUrl(input))
    {
        // It's a URL
        url = input.StartsWith("http") ? input : "https://" + input;
    }
    else
    {
        // It's a search query
        string searchEngine = "https://www.google.com/search?q=";
        url = searchEngine + Uri.EscapeDataString(input);
    }

    webView.CoreWebView2.Navigate(url);
}

private bool IsValidUrl(string input)
{
    return Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
        || input.Contains(".");
}

// Navigation events
webView.CoreWebView2.NavigationStarting += (s, e) =>
{
    // Update address bar, show loading indicator
};

webView.CoreWebView2.NavigationCompleted += (s, e) =>
{
    // Hide loading, update UI
    bool success = e.IsSuccess;
};
```

### **4. Request Interception and Blocking**

```csharp
// Add filter for specific resource types and URL patterns
webView.CoreWebView2.AddWebResourceRequestedFilter(
    "*",  // All URLs
    CoreWebView2WebResourceContext.All  // All resource types
);

// Handle intercepted requests
webView.CoreWebView2.WebResourceRequested += WebView_WebResourceRequested;

private void WebView_WebResourceRequested(
    object sender,
    CoreWebView2WebResourceRequestedEventArgs args)
{
    string url = args.Request.Uri;
    CoreWebView2WebResourceContext resourceType = args.ResourceContext;

    // Example: Block trackers
    if (url.Contains("tracker.com") || url.Contains("analytics"))
    {
        // Block by returning without setting Response
        args.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(
            null, 403, "Blocked", "");
        return;
    }

    // Example: Modify headers
    if (resourceType == CoreWebView2WebResourceContext.Document)
    {
        args.Request.Headers.SetHeader("Custom-Header", "CustomValue");
    }

    // Log request
    LogNetworkRequest(url, resourceType.ToString(), "Allowed");
}
```

### **5. Resource Type Filtering**

```csharp
// Filter specific resource types
CoreWebView2WebResourceContext.All          // All resources
CoreWebView2WebResourceContext.Document     // HTML documents
CoreWebView2WebResourceContext.Image        // Images (jpg, png, etc.)
CoreWebView2WebResourceContext.Script       // JavaScript files
CoreWebView2WebResourceContext.Stylesheet   // CSS files
CoreWebView2WebResourceContext.Xhr          // XMLHttpRequest
CoreWebView2WebResourceContext.Fetch        // Fetch API requests
CoreWebView2WebResourceContext.Font         // Web fonts
CoreWebView2WebResourceContext.Media        // Audio/video
CoreWebView2WebResourceContext.WebSocket    // WebSocket connections
```

### **6. CSS Injection**

```csharp
// Wait for page to load
webView.CoreWebView2.NavigationCompleted += async (s, e) =>
{
    if (e.IsSuccess)
    {
        await InjectCssAsync();
    }
};

private async Task InjectCssAsync()
{
    string cssCode = @"
        .cookie-banner,
        .gdpr-wall,
        [class*='cookie'],
        [id*='cookie'] {
            display: none !important;
        }

        body {
            filter: brightness(0.9);  /* Dark mode effect */
        }
    ";

    string script = $@"
        (function() {{
            var style = document.createElement('style');
            style.type = 'text/css';
            style.innerHTML = `{cssCode}`;
            document.head.appendChild(style);
        }})();
    ";

    await webView.CoreWebView2.ExecuteScriptAsync(script);
}
```

### **7. JavaScript Injection**

```csharp
// Execute JavaScript and get result
private async Task<string> ExecuteJavaScriptAsync(string script)
{
    string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
    // Result is JSON-encoded, may need to deserialize
    return result;
}

// Examples:
// Get page title
string title = await ExecuteJavaScriptAsync("document.title");

// Auto-click reject cookies button
await ExecuteJavaScriptAsync(@"
    const rejectBtn = document.querySelector('[data-consent=""reject""]');
    if (rejectBtn) rejectBtn.click();
");

// Remove all ads by class
await ExecuteJavaScriptAsync(@"
    document.querySelectorAll('.ad, .advertisement').forEach(el => el.remove());
");

// Get all links on page
string linksJson = await ExecuteJavaScriptAsync(@"
    JSON.stringify(
        Array.from(document.querySelectorAll('a'))
            .map(a => ({ text: a.textContent, href: a.href }))
    )
");
```

### **8. Timing for Injections**

```csharp
// DOM Content Loaded (early injection)
webView.CoreWebView2.DOMContentLoaded += async (s, e) =>
{
    // DOM is ready, but images/styles may still be loading
    await InjectEarlyScriptAsync();
};

// Navigation Completed (late injection)
webView.CoreWebView2.NavigationCompleted += async (s, e) =>
{
    // Page fully loaded
    await InjectLateScriptAsync();
};

// Add script to execute on every page load
await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
    console.log('Browser: Initialized');
");
```

### **9. Password Manager Configuration**

```csharp
// Enable password autofill and autosave
webView.CoreWebView2.Settings.IsPasswordAutofillEnabled = true;

// Passwords are automatically saved when:
// 1. User submits form with type="password" input
// 2. Navigation occurs after submission
// 3. User accepts browser's save prompt

// Password storage location:
// %LOCALAPPDATA%\BrowserApp\UserData\Default\Login Data
```

### **10. Back/Forward/Refresh**

```csharp
// Navigation methods
private void GoBack()
{
    if (webView.CoreWebView2.CanGoBack)
        webView.CoreWebView2.GoBack();
}

private void GoForward()
{
    if (webView.CoreWebView2.CanGoForward)
        webView.CoreWebView2.GoForward();
}

private void Refresh()
{
    webView.CoreWebView2.Reload();
}

private void Stop()
{
    webView.CoreWebView2.Stop();
}
```

### **11. Get Current URL**

```csharp
// Get current URL
string currentUrl = webView.CoreWebView2.Source;

// Listen for URL changes
webView.CoreWebView2.SourceChanged += (s, e) =>
{
    string newUrl = webView.CoreWebView2.Source;
    UpdateAddressBar(newUrl);
};
```

### **12. Handle New Windows/Popups**

```csharp
// Control how new windows open
webView.CoreWebView2.NewWindowRequested += (s, e) =>
{
    // Option 1: Block popup
    e.Handled = true;

    // Option 2: Open in same window
    webView.CoreWebView2.Navigate(e.Uri);
    e.Handled = true;

    // Option 3: Allow popup (default behavior)
    // Don't set e.Handled
};
```

---

## 🎨 WPF UI Essential Snippets

### **1. Basic Window with WPF UI**

```xml
<ui:FluentWindow x:Class="BrowserApp.MainWindow"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
                 Title="Browser"
                 Height="800"
                 Width="1200"
                 ExtendsContentIntoTitleBar="True"
                 WindowBackdropType="Mica">

    <Grid>
        <!-- Content here -->
    </Grid>

</ui:FluentWindow>
```

### **2. Modern Button**

```xml
<ui:Button Content="Go"
           Icon="{ui:SymbolIcon ChevronRight24}"
           Appearance="Primary"
           Click="GoButton_Click"/>
```

### **3. Address Bar (TextBox)**

```xml
<ui:TextBox x:Name="addressBar"
            PlaceholderText="Search or enter URL"
            Icon="{ui:SymbolIcon Search24}"
            KeyDown="AddressBar_KeyDown"/>
```

### **4. Navigation Bar Layout**

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>

    <!-- Navigation Buttons -->
    <StackPanel Grid.Column="0" Orientation="Horizontal" Margin="8">
        <ui:Button Icon="{ui:SymbolIcon ArrowLeft24}"
                   ToolTip="Back" Click="Back_Click"/>
        <ui:Button Icon="{ui:SymbolIcon ArrowRight24}"
                   ToolTip="Forward" Click="Forward_Click"/>
        <ui:Button Icon="{ui:SymbolIcon ArrowClockwise24}"
                   ToolTip="Refresh" Click="Refresh_Click"/>
    </StackPanel>

    <!-- Address Bar -->
    <ui:TextBox Grid.Column="1"
                x:Name="addressBar"
                Margin="8"
                PlaceholderText="Search or enter URL"/>

    <!-- Profile Menu -->
    <ui:Button Grid.Column="2"
               Content="Profile"
               Margin="8"/>
</Grid>
```

### **5. Sidebar Panel**

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="350"/>
    </Grid.ColumnDefinitions>

    <!-- Main Content -->
    <wv2:WebView2 Grid.Column="0" x:Name="webView"/>

    <!-- Sidebar -->
    <Border Grid.Column="1"
            Background="{ui:ThemeResource CardBackground}"
            CornerRadius="8"
            Margin="8">
        <Grid>
            <!-- Sidebar content -->
        </Grid>
    </Border>
</Grid>
```

### **6. Tab Control (Future Multi-Tab)**

```xml
<ui:TabControl>
    <ui:TabItem Header="AI Copilot" Icon="{ui:SymbolIcon Bot24}">
        <!-- AI content -->
    </ui:TabItem>
    <ui:TabItem Header="Network" Icon="{ui:SymbolIcon Globe24}">
        <!-- Network monitor -->
    </ui:TabItem>
    <ui:TabItem Header="Rules" Icon="{ui:SymbolIcon Shield24}">
        <!-- Rules panel -->
    </ui:TabItem>
</ui:TabControl>
```

### **7. DataGrid for Network Monitor**

```xml
<DataGrid x:Name="networkGrid"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          CanUserAddRows="False"
          AlternatingRowBackground="{ui:ThemeResource SubtleFillColorSecondary}">
    <DataGrid.Columns>
        <DataGridTextColumn Header="URL" Binding="{Binding Url}" Width="*"/>
        <DataGridTextColumn Header="Status" Binding="{Binding Status}" Width="Auto"/>
        <DataGridTextColumn Header="Type" Binding="{Binding ResourceType}" Width="Auto"/>
        <DataGridTextColumn Header="Size" Binding="{Binding Size}" Width="Auto"/>
    </DataGrid.Columns>
</DataGrid>
```

---

## 📦 NuGet Packages Required

```xml
<!-- In .csproj file -->
<ItemGroup>
    <PackageReference Include="Microsoft.Web.WebView2" Version="1.0.*" />
    <PackageReference Include="WPF-UI" Version="4.1.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.*" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.*" />
</ItemGroup>
```

Or via Package Manager Console:
```powershell
Install-Package Microsoft.Web.WebView2
Install-Package WPF-UI
Install-Package Microsoft.EntityFrameworkCore.Sqlite
Install-Package Microsoft.Extensions.DependencyInjection
Install-Package Microsoft.Extensions.Configuration
Install-Package Microsoft.Extensions.Configuration.Json
```

---

## 🚀 Project Creation Commands

```bash
# Create solution and projects
dotnet new sln -n BrowserApp

# WPF application
dotnet new wpf -n BrowserApp.UI -f net8.0-windows
dotnet sln add BrowserApp.UI

# Class libraries
dotnet new classlib -n BrowserApp.Core -f net8.0
dotnet sln add BrowserApp.Core

dotnet new classlib -n BrowserApp.Data -f net8.0
dotnet sln add BrowserApp.Data

dotnet new classlib -n BrowserApp.AI -f net8.0
dotnet sln add BrowserApp.AI

# Web API (Server)
dotnet new webapi -n BrowserApp.Server -f net8.0
dotnet sln add BrowserApp.Server

# Add project references
cd BrowserApp.UI
dotnet add reference ../BrowserApp.Core
dotnet add reference ../BrowserApp.Data
dotnet add reference ../BrowserApp.AI

# Install packages in UI project
dotnet add package Microsoft.Web.WebView2
dotnet add package WPF-UI
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

---

## 🔍 Common Issues and Solutions

### **Issue: WebView2 Runtime Not Found**
**Solution:**
```csharp
try
{
    await webView.EnsureCoreWebView2Async(null);
}
catch (Exception ex)
{
    MessageBox.Show(
        "WebView2 Runtime is not installed. Please install it from: " +
        "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
        "Error",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
}
```

### **Issue: Cookies Not Persisting**
**Solution:** Ensure UserDataFolder is set BEFORE initialization:
```csharp
// WRONG - Too late
await webView.EnsureCoreWebView2Async(null);
webView.CoreWebView2.UserDataFolder = "..."; // Won't work!

// RIGHT - Before initialization
webView.CreationProperties = new CoreWebView2CreationProperties
{
    UserDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrowserApp", "UserData")
};
await webView.EnsureCoreWebView2Async(null);
```

### **Issue: JavaScript Injection Not Working**
**Solution:** Wait for DOM to be ready:
```csharp
// WRONG - May run before DOM is ready
await webView.CoreWebView2.ExecuteScriptAsync("...");

// RIGHT - Wait for NavigationCompleted
webView.CoreWebView2.NavigationCompleted += async (s, e) =>
{
    if (e.IsSuccess)
    {
        await webView.CoreWebView2.ExecuteScriptAsync("...");
    }
};
```

### **Issue: Request Interception Filter Not Working**
**Solution:** Add filter AFTER CoreWebView2 is initialized:
```csharp
// WRONG - CoreWebView2 is null
webView.CoreWebView2.AddWebResourceRequestedFilter("*", ...); // Crash!

// RIGHT - After initialization
await webView.EnsureCoreWebView2Async(null);
webView.CoreWebView2.AddWebResourceRequestedFilter("*",
    CoreWebView2WebResourceContext.All);
```

---

## 📝 Development Checklist

### **Phase 1: Core Browser (First Session)**
- [ ] Create WPF project
- [ ] Install NuGet packages
- [ ] Add WebView2 control to MainWindow
- [ ] Configure UserDataFolder for persistence
- [ ] Enable password autofill
- [ ] Implement address bar with URL/search detection
- [ ] Add back/forward/refresh buttons
- [ ] Apply WPF UI styling
- [ ] Test navigation to 5-10 websites
- [ ] Verify cookies persist after app restart

### **Phase 2: Network Monitoring**
- [ ] Add WebResourceRequested event handler
- [ ] Create NetworkRequest model class
- [ ] Log requests to console (verify interception works)
- [ ] Create NetworkLogger service (save to SQLite)
- [ ] Build network monitor UI (DataGrid)
- [ ] Add filtering (blocked, 3rd party, type)
- [ ] Add export to CSV functionality
- [ ] Test on tracker-heavy sites

### **Phase 3: Rule System**
- [ ] Create Rule model (JSON schema)
- [ ] Implement RuleEngine (evaluate rules)
- [ ] Add BlockingService (cancel requests)
- [ ] Implement CSS injection
- [ ] Implement JS injection
- [ ] Create pre-built rule templates
- [ ] Add manual rule creation UI
- [ ] Test blocking and injection on real sites

---

## 🎯 Key Principles to Remember

1. **Always initialize CoreWebView2 before accessing its properties:**
   ```csharp
   await webView.EnsureCoreWebView2Async(null);
   // Now safe to use webView.CoreWebView2.*
   ```

2. **Use async/await for all WebView2 operations:**
   - `EnsureCoreWebView2Async`
   - `ExecuteScriptAsync`
   - `NavigateAsync` (if you create custom nav)

3. **Set UserDataFolder BEFORE initialization** (via CreationProperties or Environment)

4. **Filter requests efficiently** - Don't use `*` unless necessary, specify resource types

5. **Handle navigation events for UI updates:**
   - `NavigationStarting` → Show loading
   - `NavigationCompleted` → Hide loading, update address bar
   - `SourceChanged` → Update address bar

6. **Inject scripts at the right time:**
   - Early DOM manipulation → `DOMContentLoaded`
   - After full page load → `NavigationCompleted`
   - On every page → `AddScriptToExecuteOnDocumentCreatedAsync`

7. **WPF UI uses XAML namespaces:**
   ```xml
   xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
   ```

8. **Follow MVVM pattern:**
   - No business logic in code-behind
   - ViewModels for UI logic
   - Services for business logic

---

## 💾 Quick File Templates

### **App.xaml (WPF UI Theme)**
```xml
<Application x:Class="BrowserApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ui:ThemesDictionary Theme="Dark" />
                <ui:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

### **appsettings.json**
```json
{
  "Browser": {
    "DefaultSearchEngine": "Google",
    "UserDataFolder": "%LOCALAPPDATA%/BrowserApp/UserData",
    "EnablePasswordManager": true
  },
  "Server": {
    "ApiBaseUrl": "http://localhost:5000",
    "LLMEndpoint": "http://localhost:11434"
  },
  "UI": {
    "Theme": "Auto",
    "SidebarWidth": 350
  }
}
```

---

## 🎨 Icon Resources (WPF UI Symbols)

Common icons you'll use:
- `ChevronLeft24` / `ChevronRight24` - Back/Forward
- `ArrowClockwise24` - Refresh
- `Search24` - Search
- `Globe24` - Network/Web
- `Shield24` - Security/Rules
- `Bot24` - AI
- `Settings24` - Settings
- `Home24` - Home
- `Star24` - Favorites
- `Lock24` - HTTPS/Secure

Usage:
```xml
<ui:Button Icon="{ui:SymbolIcon ChevronLeft24}"/>
```

---

**Last Updated:** December 2025
**Next Update:** After Phase 1 completion (add learnings/gotchas)
