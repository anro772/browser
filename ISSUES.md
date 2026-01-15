# Known Issues & Improvement Plan

This document lists all identified issues that need to be addressed before Phase 4.

---

## 🚨 CRITICAL ISSUES

### 1. Fire-and-Forget Async Operations
**Location:** [App.xaml.cs:62,68](BrowserApp.UI/App.xaml.cs#L62)

**Problem:**
```csharp
_ = blockingService.InitializeAsync();
_ = networkLogger.StartAsync();
```
- Services initialize without awaiting completion
- Silent failures if initialization fails
- App continues without critical services working

**Impact:** Rules and logging might not work with no error shown to user

---

### 2. XSS Vulnerabilities in Code Injectors
**Locations:**
- [CSSInjector.cs:37-42](BrowserApp.UI/Services/CSSInjector.cs#L37)
- [JSInjector.cs:40-65](BrowserApp.UI/Services/JSInjector.cs#L40)

**Problem:**
- No sanitization of injected CSS/JS
- Can inject `</style><script>alert(1)</script>` in CSS
- JS code directly embedded in template literal

**Impact:** Malicious rules could execute arbitrary JavaScript, full XSS vulnerability

---

### 3. UI Thread Performance Bottleneck
**Location:** [NetworkMonitorViewModel.cs:69-87](BrowserApp.UI/ViewModels/NetworkMonitorViewModel.cs#L69)

**Problem:**
```csharp
Application.Current?.Dispatcher.Invoke(() => {
    Requests.Insert(0, request);
    // Updates on EVERY request
});
```
- Every network request triggers `Dispatcher.Invoke`
- UI thread overwhelmed with high traffic (>100 requests/sec)
- Inserting at position 0 forces list reindexing

**Impact:** UI freezes, dropped requests, poor user experience

---

### 4. No Rule Evaluation Caching
**Location:** [RuleEngine.cs:61-106](BrowserApp.UI/Services/RuleEngine.cs#L61)

**Problem:**
- Re-evaluates all rules for every request
- URL pattern matching repeated for identical URLs
- O(rules × actions) complexity: 50 rules × 10 actions × 100 req/sec = 50,000 evaluations/sec

**Impact:** High CPU usage, poor performance

---

## ⚠️ HIGH PRIORITY ISSUES

### 5. Memory Leaks
**Locations:**
- [MainViewModel.cs](BrowserApp.UI/ViewModels/MainViewModel.cs) - Doesn't unsubscribe from navigation events
- [CSSInjector.cs](BrowserApp.UI/Services/CSSInjector.cs) - No IDisposable implementation
- [JSInjector.cs](BrowserApp.UI/Services/JSInjector.cs) - No IDisposable implementation

**Problem:**
- ViewModels subscribe to events but never unsubscribe
- Injector services hold reference to CoreWebView2
- Services never disposed

**Impact:** Memory grows ~1MB/minute, prevents WebView2 cleanup

---

### 6. Lost Modern Window Chrome
**Location:** [MainWindow.xaml:1](BrowserApp.UI/MainWindow.xaml#L1)

**Problem:**
- Switched from `FluentWindow` to standard `Window`
- Lost modern title bar aesthetic
- Should use `WindowChrome` to get both modern look AND standard buttons

**Impact:** Less modern appearance

---

### 7. Database Performance Issues
**Locations:**
- [BrowserDbContext.cs:35-81](BrowserApp.Data/BrowserDbContext.cs#L35) - No indexes on common queries
- [NetworkLogRepository.cs:95](BrowserApp.Data/Repositories/NetworkLogRepository.cs#L95) - `ClearAllAsync()` loads all into memory

**Problem:**
- Missing composite indexes for `(Timestamp, WasBlocked)` stats queries
- `ClearAllAsync()` loads all entities before deletion
- Fixed 1-second batch interval regardless of load

**Impact:** Slow queries as data grows, OutOfMemoryException with millions of records

---

### 8. Async Void Event Handlers
**Locations:**
- [MainWindow.xaml.cs:46](BrowserApp.UI/MainWindow.xaml.cs#L46) - `async void MainWindow_Loaded`
- [NavigationService.cs:151](BrowserApp.UI/Services/NavigationService.cs#L151) - `async void OnNavigationCompleted`
- [App.xaml.cs:170](BrowserApp.UI/App.xaml.cs#L170) - `async void OnExit`

**Problem:**
- Unhandled exceptions crash the app
- Cleanup may not complete before shutdown

**Impact:** Poor error handling, potential data loss

---

## 📋 CODE QUALITY ISSUES

### 9. Silent Failures & Empty Catch Blocks
**Locations:**
- [RequestInterceptor.cs:120-124](BrowserApp.UI/Services/RequestInterceptor.cs#L120) - Catches all, only Debug.WriteLine
- [ErrorLogger.cs:65-68,84-87](BrowserApp.UI/ErrorLogger.cs#L65) - If logging fails, error swallowed

**Problem:**
- Critical blocking failures invisible to users
- No fallback logging mechanism

**Impact:** Complete loss of error visibility

---

### 10. SOLID Violations
**Location:** [RuleEngine.cs](BrowserApp.UI/Services/RuleEngine.cs)

**Problem:**
- Handles rule loading, caching, evaluation, AND JSON deserialization
- Too many responsibilities

**Impact:** Hard to test, tight coupling, difficult maintenance

---

### 11. Code Duplication
**Locations:**
- [NetworkRequest.cs:60-74](BrowserApp.Core/Models/NetworkRequest.cs#L60) - Byte formatting
- [NetworkLogger.cs:260-269](BrowserApp.UI/Services/NetworkLogger.cs#L260) - Same byte formatting
- [RuleEngine.cs:182-219](BrowserApp.UI/Services/RuleEngine.cs#L182) - Manual entity mapping

**Problem:**
- Duplicate formatting logic in multiple places
- Should use AutoMapper or source generators

**Impact:** Inconsistent formatting, brittle code

---

### 12. Missing Null Checks
**Location:** [NavigationService.cs:92](BrowserApp.UI/Services/NavigationService.cs#L92)

**Problem:**
```csharp
_coreWebView2?.Navigate(url)  // Null-conditional insufficient
```
- If Navigate fails, no error handling
- Nullable CoreWebView2 not properly guarded

**Impact:** NullReferenceException in edge cases, silent navigation failures

---

## 🏗️ ARCHITECTURE ISSUES

### 13. Service Lifetime Violations
**Location:** [App.xaml.cs:104-115](BrowserApp.UI/App.xaml.cs#L104)

**Problem:**
- Singleton services using IServiceScopeFactory correctly
- But no lifetime validation
- No scope validation in repositories
- Potential cross-scope contamination

**Impact:** Random ObjectDisposedException errors

---

### 14. Missing Unit of Work Pattern
**Locations:** All repositories

**Problem:**
- Repositories work independently
- No transaction support for multi-table operations

**Impact:** Cannot atomically save rules + network logs

---

### 15. No Caching Strategy
**Location:** Throughout codebase

**Problem:**
- No LRU cache for (URL, ruleSet) → RuleEvaluationResult
- No cache invalidation when rules reload

**Impact:** 90% cache hit rate possible but not implemented, massive performance loss

---

## 🎨 UI/UX ISSUES

### 16. Network Monitor UX
**Location:** [NetworkMonitorView.xaml:166-207](BrowserApp.UI/Views/NetworkMonitorView.xaml#L166)

**Problem:**
- Virtualization enabled but inefficient
- No scroll position preservation during updates
- No grouping/hierarchical view
- No request deduplication

**Impact:** Poor UX with high request volume

---

### 17. Rule Manager Missing Features
**Location:** [RuleManagerView.xaml](BrowserApp.UI/Views/RuleManagerView.xaml)

**Problem:**
- No rule editing UI (only toggle/delete)
- No visual rule builder
- No test rule against URL feature
- No import/export individual rules

**Impact:** Limited functionality

---

## 🔒 SECURITY ISSUES

### 18. Input Validation Missing
**Locations:**
- [MainViewModel.cs:56](BrowserApp.UI/ViewModels/MainViewModel.cs#L56) - No URL validation
- [RuleManagerViewModel.cs:183-190](BrowserApp.UI/ViewModels/RuleManagerViewModel.cs#L183) - No JSON schema validation

**Problem:**
- No validation before navigation (javascript:, file:// URLs)
- Deserializes rule JSON without validation

**Impact:** Malformed rules could crash app or bypass security

---

## 📊 PERFORMANCE TARGETS

### Current Performance (Estimated):
- Rule Evaluation: ~500 requests/sec
- Network Monitor UI: Freezes above 100 requests/sec
- Database Writes: ~1,000 logs/sec
- Memory Usage: ~150MB baseline, grows 1MB/min (leak)

### Target Performance:
- Rule Evaluation: 5,000+ requests/sec (90% cache hit)
- Network Monitor UI: Smooth at 1,000+ requests/sec
- Database Writes: 10,000+ logs/sec
- Memory Usage: ~100MB baseline, stable (no leaks)

---

## 🎯 IMPLEMENTATION PRIORITY

1. **Critical**: Issues 1-4 (Fire-and-forget, XSS, UI performance, caching)
2. **High**: Issues 5-8 (Memory leaks, window chrome, database, async void)
3. **Medium**: Issues 9-15 (Code quality, architecture)
4. **Low**: Issues 16-18 (UX enhancements, minor security)

---

**Created:** January 15, 2026
**Status:** Ready for remediation
