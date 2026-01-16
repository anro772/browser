# uBlock Origin Integration Design

**⚠️ STATUS: NOT IMPLEMENTED - REMOVED ON 2026-01-15**

**Reason for Removal:** Network-level ad blocking has fundamental limitations:
- Cannot block first-party ads (YouTube serves ads from same domain as videos)
- Exception rules required for site functionality whitelist too many ads
- Requires cosmetic filtering (DOM manipulation) for complete coverage
- Maintenance burden too high (weekly filter list updates, constant cat-and-mouse game)

**See [ADBLOCKER-REMOVED.md](ADBLOCKER-REMOVED.md) for full removal documentation.**

The design below was successfully implemented and tested but ultimately removed due to these limitations. The implementation successfully blocked 90%+ of third-party ads but could not block YouTube ads without breaking YouTube functionality.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Network Request                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
          ┌──────────────────────────────┐
          │   RequestInterceptor         │
          │   (WebView2 handler)         │
          └──────────────┬───────────────┘
                         │
                         ▼
          ┌──────────────────────────────┐
          │   AdBlockerService           │◄─── Filter Lists
          │   (Fast domain/pattern match)│     (60K+ rules)
          └──────────────┬───────────────┘
                         │
                    ┌────┴────┐
                    │         │
              Blocked?     Allowed?
                    │         │
                    │         ▼
                    │    ┌──────────────┐
                    │    │  RuleEngine  │◄─── Custom Rules
                    │    │  (Complex)   │     (User defined)
                    │    └──────┬───────┘
                    │           │
                    │      Block/Allow?
                    │           │
                    └───────────┴──────► Final Decision
```

## Components

### 1. AdBlockerService
**Purpose:** Fast lookup of ad/tracker domains using uBlock Origin filters

**Responsibilities:**
- Download filter lists from uBlock Origin repository
- Parse filter syntax into optimized data structures
- Provide O(1) or O(log n) lookups for URL matching
- Update filters periodically (daily/weekly)
- Cache compiled filters to disk for fast startup

**Data Structures:**
```csharp
public class AdBlockerService : IAdBlockerService
{
    // Fast lookups
    private HashSet<string> _exactDomains;           // ||example.com^
    private HashSet<string> _exactUrls;              // |https://example.com/ad.js|
    private List<Regex> _regexPatterns;              // /banner\d+\.jpg/
    private Dictionary<string, List<string>> _domainSpecificRules; // example.com##.ad-div

    // Bloom filter for quick "definitely not blocked" check
    private BloomFilter _bloomFilter;

    // Cosmetic filters (CSS hiding)
    private Dictionary<string, List<string>> _cosmeticFilters; // domain → [".ad-class", "#ad-id"]

    // Statistics
    private long _totalFilters;
    private long _exactMatches;
    private long _patternMatches;
}
```

### 2. Filter Parser
**Purpose:** Parse uBlock Origin filter syntax into data structures

**Filter Types:**

#### Network Filters (Block requests)
```
||doubleclick.net^                  → Exact domain match
|https://ads.example.com/banner.js| → Exact URL match
/ad-banner-\d+\.png/                → Regex pattern
@@||example.com^                    → Whitelist (exception)
```

#### Cosmetic Filters (Hide elements)
```
##.ad-banner                        → Global CSS selector
example.com##.sponsor               → Domain-specific CSS selector
example.com#@#.legitcontent         → Whitelist CSS selector
```

#### Advanced Options
```
||ads.com^$script                   → Only block if resource type is script
||tracker.com^$third-party          → Only block if third-party request
||analytics.com^$domain=example.com → Only on specific domain
```

### 3. Filter List Sources

**Primary Lists (included):**
1. **uBlock filters** - uBlock Origin's own filters (~7,000 rules)
   - URL: https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/filters.txt

2. **uBlock filters - Privacy** - Privacy/tracking protection (~300 rules)
   - URL: https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/privacy.txt

3. **uBlock filters - Badware risks** - Malware protection (~500 rules)
   - URL: https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/badware.txt

4. **EasyList** - Primary ad blocking list (~60,000 rules)
   - URL: https://easylist.to/easylist/easylist.txt

5. **EasyPrivacy** - Tracking/analytics blocking (~20,000 rules)
   - URL: https://easylist.to/easylist/easyprivacy.txt

**Optional Lists (user-configurable):**
- **AdGuard Base** - Alternative to EasyList
- **Fanboy's Annoyance** - Cookie notices, social widgets
- **Fanboy's Social** - Social media buttons

**Total: ~90,000 filters when all primary lists enabled**

### 4. Performance Optimizations

#### Bloom Filter (First-Pass Check)
```csharp
// 99.9% accurate "definitely not blocked" check
// False positive rate: 0.1% (acceptable)
// Memory: ~2MB for 90K filters
if (!_bloomFilter.MightContain(domain))
{
    return false; // Definitely not blocked, skip expensive checks
}
```

#### Hash Set Lookups (Exact Matches)
```csharp
// O(1) lookup for exact domains
// Handles 80% of ad/tracker blocks
if (_exactDomains.Contains(domain))
{
    return true; // Blocked
}
```

#### Trie for Wildcard Patterns
```csharp
// O(m) lookup where m = URL length
// Handles patterns like "ad-*.js"
var node = _trie.FindLongestMatch(url);
if (node?.IsBlocked == true)
{
    return true;
}
```

#### Regex as Last Resort
```csharp
// Only ~5% of rules need regex
// Cache compiled Regex objects
foreach (var pattern in _regexPatterns)
{
    if (pattern.IsMatch(url))
    {
        return true;
    }
}
```

## Implementation Plan

### Phase 1: Basic Domain Blocking (Quick Win)
**Goal:** 80% ad blocking in 2-3 hours

1. Create `AdBlockerService` class
2. Download EasyList
3. Parse simple rules: `||domain.com^`
4. Integrate into `RequestInterceptor`

**Expected Result:** 40,000+ domain blocks active

### Phase 2: Pattern Matching
**Goal:** 95% ad blocking

1. Add wildcard pattern support: `*ad-banner*.js`
2. Add regex pattern support: `/banner\d+/`
3. Implement Trie for efficient matching

**Expected Result:** 60,000+ network filters active

### Phase 3: Advanced Options
**Goal:** 99% ad blocking with minimal false positives

1. Parse filter options: `$script`, `$third-party`, `$domain=`
2. Implement resource type filtering
3. Implement exception rules (@@)

**Expected Result:** Full uBlock Origin compatibility

### Phase 4: Cosmetic Filtering
**Goal:** Hide ad placeholders

1. Parse cosmetic filters: `##.ad-div`
2. Inject CSS via CSSInjector
3. Domain-specific cosmetic rules

**Expected Result:** Clean page appearance (no blank ad spaces)

### Phase 5: Maintenance & Updates
**Goal:** Keep filters current

1. Auto-update filters daily
2. Cache compiled filters to disk
3. Background updates without blocking

## Data Flow

### Startup Sequence
```
1. App.xaml.cs: ConfigureServices()
   ↓
2. Register AdBlockerService as Singleton
   ↓
3. AdBlockerService.InitializeAsync()
   ↓
4. Check for cached compiled filters
   ├─ Found → Load from cache (fast ~100ms)
   └─ Not found → Download & parse (~2-3 seconds)
   ↓
5. Build optimized data structures
   ↓
6. Ready to block ads
```

### Request Handling
```
1. RequestInterceptor captures network request
   ↓
2. AdBlockerService.ShouldBlock(url)
   ├─ Bloom filter check (0.001ms)
   ├─ Hash set lookup (0.01ms)
   ├─ Trie pattern match (0.1ms)
   └─ Regex match (1ms - rare)
   ↓
3. If blocked → Return immediately
   ↓
4. If not blocked → Check RuleEngine for custom rules
   ↓
5. Final decision: Block or Allow
```

## File Structure

```
BrowserApp.AdBlocker/
├── Services/
│   ├── AdBlockerService.cs           // Main service
│   ├── FilterListDownloader.cs       // Download filter lists
│   └── FilterListUpdater.cs          // Background updates
├── Parsing/
│   ├── FilterParser.cs                // Parse filter syntax
│   ├── NetworkFilterParser.cs        // Parse network rules
│   └── CosmeticFilterParser.cs       // Parse cosmetic rules
├── Matching/
│   ├── DomainMatcher.cs               // Exact domain matching
│   ├── PatternMatcher.cs              // Wildcard patterns
│   ├── RegexMatcher.cs                // Regex patterns
│   └── BloomFilter.cs                 // Bloom filter implementation
├── Models/
│   ├── FilterRule.cs                  // Parsed filter model
│   ├── FilterOptions.cs               // Filter option flags
│   └── CompiledFilters.cs             // Serializable filter cache
└── Interfaces/
    ├── IAdBlockerService.cs
    ├── IFilterParser.cs
    └── IUrlMatcher.cs
```

## Configuration

### appsettings.json
```json
{
  "AdBlocker": {
    "Enabled": true,
    "FilterLists": [
      {
        "Name": "uBlock filters",
        "Url": "https://raw.githubusercontent.com/uBlockOrigin/uAssets/master/filters/filters.txt",
        "Enabled": true
      },
      {
        "Name": "EasyList",
        "Url": "https://easylist.to/easylist/easylist.txt",
        "Enabled": true
      },
      {
        "Name": "EasyPrivacy",
        "Url": "https://easylist.to/easylist/easyprivacy.txt",
        "Enabled": true
      }
    ],
    "UpdateInterval": "1.00:00:00",  // Update daily
    "CacheDirectory": "FilterCache",
    "EnableBloomFilter": true,
    "EnableStatistics": true
  }
}
```

## Performance Targets

| Metric | Target | Notes |
|--------|--------|-------|
| Startup time (cold) | < 3 seconds | Download + parse filters |
| Startup time (cached) | < 100ms | Load from disk cache |
| Lookup time (average) | < 0.01ms | Hash set lookup |
| Lookup time (worst) | < 1ms | Regex match |
| Memory usage | < 100MB | All filters + bloom filter |
| Block rate | > 95% | On typical ad-heavy sites |
| False positive rate | < 0.1% | Legitimate content blocked |

## Benefits vs Current System

| Feature | Current (RuleEngine) | With AdBlocker |
|---------|---------------------|----------------|
| Filter count | 5 rules | 90,000+ filters |
| Block rate | 11.6% | 95-99% |
| Lookup speed | ~0.1ms (cached) | ~0.01ms (hash) |
| Memory usage | ~10MB | ~100MB |
| Maintenance | Manual rule creation | Auto-updated lists |
| False positives | Low (few rules) | Very low (battle-tested) |

## Integration with Existing System

### Current Flow
```csharp
// RequestInterceptor.cs
var result = await _blockingService.ShouldBlockAsync(request);
if (result.ShouldBlock) { /* block */ }
```

### New Flow
```csharp
// RequestInterceptor.cs
// Fast check: AdBlocker (99% of blocks)
if (_adBlockerService.ShouldBlock(request.Url, request.ResourceType))
{
    return BlockResult.Blocked("AdBlocker");
}

// Slow check: RuleEngine (custom rules, injections)
var result = await _blockingService.ShouldBlockAsync(request);
if (result.ShouldBlock) { /* block */ }
```

## Testing Strategy

### Unit Tests
- Filter parser (parse all filter types)
- Domain matcher (exact matches)
- Pattern matcher (wildcards)
- Regex matcher (complex patterns)
- Bloom filter (false positive rate)

### Integration Tests
- Load real EasyList
- Test against known ad domains
- Verify block rate on test sites
- Performance benchmarks

### Real-World Tests
- Test on CNN.com (heavy ads)
- Test on YouTube (video ads)
- Test on news sites (trackers)
- Verify no false positives on legitimate sites

## Fallback Strategy

If AdBlocker service fails:
1. Log error but don't crash app
2. Fall back to RuleEngine
3. Display warning in UI
4. Retry filter download in background

## Future Enhancements

1. **User-configurable filter lists**
   - UI to enable/disable lists
   - Import custom lists

2. **Whitelist management**
   - Disable blocking on specific sites
   - Temporary disable for troubleshooting

3. **Statistics dashboard**
   - Total ads blocked (lifetime)
   - Block rate per site
   - Top blocked domains

4. **Filter list subscriptions**
   - Community-maintained lists
   - Language-specific lists
   - Region-specific lists

## License Compliance

All filter lists are GPL v3 licensed:
- ✅ Free to use in private projects
- ✅ Free to use in open source projects
- ⚠️ Must include license attribution
- ⚠️ Modifications must be open sourced if distributed

**For your private use:** No restrictions, completely free.
