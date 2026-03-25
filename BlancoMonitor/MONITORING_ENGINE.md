# BlancoMonitor — Monitoring Engine Design

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                    MonitoringOrchestrator                        │
│                   (Application Layer)                            │
│  RunAsync() → per-target: MonitorSingleUrlAsync()               │
└──────────┬───────────────┬──────────────┬───────────────────────┘
           │               │              │
           ▼               ▼              ▼
┌──────────────┐  ┌──────────────┐  ┌────────────────┐
│ ScenarioEngine│  │ Performance  │  │   RuleEngine   │
│  (Headless)   │  │  Analyzer    │  │  (Advanced)    │
│               │  │              │  │                │
│ NavigateFull  │  │ Classify     │  │ Per-PageType   │
│ SearchFull    │  │ Filter noise │  │ Confidence     │
│ User Sim      │  │ Breakdown    │  │ Grouping       │
│ Referer chain │  │ API isolate  │  │ 11 rule types  │
└───────┬───────┘  └──────────────┘  └────────────────┘
        │
        ▼
┌──────────────────┐    ┌──────────────────────────────────────┐
│  NetworkClient   │    │         Analysis Utilities            │
│  (HTTP Engine)   │    │                                      │
│                  │    │  RequestClassifier  PageTypeDetector  │
│ FetchPage +      │    │  NoiseFilter        IssueGrouper     │
│ SubResources     │    │                                      │
│ HTML parsing     │    └──────────────────────────────────────┘
│ Parallel fetch   │
│ Regex extraction │
└──────────────────┘
```

## 2. Class Design

### HttpNetworkClient (Infrastructure/Network)
The HTTP engine simulates browser-level network behavior without a visible browser.

| Method | Purpose |
|--------|---------|
| `SendAsync(url, method)` | Single HTTP request with TTFB + content timing |
| `SendAsync(HttpRequestMessage)` | Custom request with headers |
| `FetchPageWithResourcesAsync(url, referer)` | **Full page load**: fetches HTML, parses for sub-resources, fetches them in parallel |

**Sub-resource discovery** uses compiled regex patterns to extract:
- `<link href="...">` (CSS, favicons, preload)
- `<script src="...">` (JavaScript)
- `<img src="...">` (Images)
- `url(...)` (CSS backgrounds, fonts)
- `<source src="...">` (Video/picture elements)

**Parallel fetching**: Sub-resources are fetched with `SemaphoreSlim(6)` throttle to simulate browser behavior (browsers open ~6 connections per host). Max 60 sub-resources per page.

### HttpScenarioEngine (Infrastructure/Browser)
Simulates real user behavior through a series of page loads.

| Method | Purpose |
|--------|---------|
| `NavigateAsync(url)` | Simple single-request navigation (legacy) |
| `NavigateFullAsync(url, referer)` | Full page load with sub-resources + classification |
| `SearchAsync(baseUrl, keyword)` | Simple search (legacy) |
| `SearchFullAsync(baseUrl, keyword, referer)` | Full search with sub-resource capture |
| `ExecuteAsync(scenario)` | Multi-step scenario with user simulation |

**User simulation features:**
- **Referer chain**: Each page passes its URL as the `Referer` header to the next
- **Jittered delays**: `baseDelay + random(0..baseDelay/2)` between pages
- **Scroll simulation**: 800–1200ms delay for `ScrollPage` action
- **Step sequencing**: Navigate → Search → ClickLink → Wait → Scroll

### PerformanceAnalyzerImpl (Infrastructure/Analysis)
Converts raw network traces into structured performance metrics.

**Analysis pipeline:**
1. **Classify** all requests via `RequestClassifier`
2. **Partition** signal vs noise via `NoiseFilter`
3. **Compute** primary document TTFB, content download, total time
4. **Estimate** DOM ready = primary HTML + max(blocking CSS + sync JS)
5. **Build** per-category `ResourceBreakdown` (count, duration, bytes, failures)
6. **Isolate** API traces for P95/avg/max API latency
7. **Rank** top 5 slowest endpoints

### RuleEngineImpl (Infrastructure/Analysis)
11 rule types with per-page-type thresholds and confidence levels.

## 3. Scenario Execution Flow

```
User triggers "Start Monitoring"
│
├── 1. Discovery Phase
│   └── SitemapDiscoveryEngine: robots.txt → sitemap.xml → URL list
│
├── 2. For each URL:
│   │
│   ├── 2a. NavigateFullAsync(url)
│   │   ├── Send GET request with realistic headers
│   │   ├── Measure TTFB (ResponseHeadersRead timing)
│   │   ├── Read HTML body
│   │   ├── Parse HTML for <link>, <script>, <img>, url() references
│   │   ├── Resolve relative URLs to absolute
│   │   ├── Fetch up to 60 sub-resources in parallel (6 concurrent)
│   │   ├── Classify each request (HTML/API/JS/CSS/Image/Font/Analytics/3P)
│   │   ├── Detect page type (Homepage/Product/Search/Category/...)
│   │   └── Return PageLoadResult with all traces
│   │
│   ├── 2b. SearchFullAsync(url, keyword) × N keywords
│   │   ├── Build search URL: {scheme}://{host}/search?q={keyword}
│   │   ├── Set Referer to previous page URL
│   │   ├── Fetch search results page + sub-resources
│   │   └── Force PageType = Search
│   │
│   ├── 2c. Analyzer.Analyze(allTraces, url)
│   │   ├── Classify + filter noise
│   │   ├── Compute TTFB, download, total, DOMReady, FullPageLoad
│   │   ├── Build ResourceBreakdowns per category
│   │   ├── Isolate API latencies (avg, P95, max)
│   │   └── Rank top 5 slowest endpoints
│   │
│   ├── 2d. RuleEngine.EvaluateAdvanced(metric, traces, thresholds)
│   │   ├── Apply per-page-type thresholds
│   │   ├── Check 11 rule types (see below)
│   │   ├── Group repeated issues
│   │   └── Set confidence (Suspected/Confirmed/Persistent)
│   │
│   └── 2e. Persist to SQLite
│       ├── INSERT PageVisit
│       ├── INSERT NetworkRequest (batch)
│       ├── INSERT DetectedIssue per alert
│       └── INSERT EvidenceItem (if screenshot)
│
├── 3. Finalize RunSession (aggregate stats)
│
└── 4. Build BaselineComparisons (vs previous run)
```

## 4. Network Capture Strategy

### What is captured per request:
| Field | Source |
|-------|--------|
| URL | Request URI |
| Method | HTTP method |
| Status Code | Response status |
| TTFB (ms) | Stopwatch: send → ResponseHeadersRead |
| Content Download (ms) | Stopwatch: headers → body complete |
| Total Time (ms) | Stopwatch: send → body complete |
| Content-Type | Response header |
| Content-Length | Byte count |
| Request Headers | All request headers |
| Response Headers | All response headers |
| Redirect Chain | Not captured in HTTP mode (planned for Playwright) |
| Error Message | Exception message if failed |
| Request Category | Classified by RequestClassifier |
| Is Third-Party | Host comparison with page domain |
| Initiator URL | Parent page URL (for sub-resources) |

### RequestClassifier logic (priority order):
1. **Analytics domain** → `Analytics` (Google Analytics, GTM, Facebook, Hotjar, etc.)
2. **Content-Type match** → `Html`, `Api`, `JavaScript`, `Css`, `Image`, `Font`
3. **URL extension fallback** → `.js`, `.css`, `.jpg`, `.woff2`, etc.
4. **URL path pattern** → `/api/`, `/graphql`, `/v1/` → `Api`
5. **Default** → `Other`

### NoiseFilter logic:
Excluded from performance scoring:
- Analytics category (always)
- Tracking pixels (third-party images < 1KB)
- Known noise paths (`/collect`, `/beacon`, `/pixel`, `/track`)
- Prefetch requests (`Purpose: prefetch` header)

## 5. Issue Detection Algorithm

### 11 Rule Types:

| # | Rule | Severity Logic | Threshold Source |
|---|------|---------------|-----------------|
| 1 | **TTFB** | Warning/Critical | Per-page-type (e.g., Homepage: 800ms/2000ms) |
| 2 | **Total Page Load** | Warning/Critical | Per-page-type |
| 3 | **DOM Ready Estimate** | Warning/Critical | 70% of total threshold |
| 4 | **API Average Latency** | Warning/Critical | Per-page-type API thresholds |
| 5 | **API P95 Latency** | Critical | 1.5× API critical threshold |
| 6 | **HTTP Status Code** | Warning (4xx) / Critical (5xx) | Fixed |
| 7 | **Failed Request Count** | Notice (<5%) / Warning (5-20%) / Critical (>20%) | Percentage |
| 8 | **Excessive Requests** | Notice | Per-page-type max count |
| 9 | **Slow Individual Endpoints** | Warning | Duration > API critical |
| 10 | **Third-Party Bloat** | Info | > 50% of requests are 3P |
| 11 | **Large Transfer Size** | Notice (>3MB) / Warning (>5MB) | Fixed |

Plus: **Legacy threshold rules** (ContentDownloadMs from user config)
Plus: **Failed sub-resource rules** (per-category severity: API/HTML=Critical, JS/CSS=Warning, other=Notice)

### Confidence Algorithm:

```
For each alert:
  1. Assign GroupKey = "{MetricName}|{Severity}|{NormalizedUrlPath}"
     (URL normalization: /product/123 → /product/{id})

  2. Group alerts by GroupKey

  3. Set confidence:
     - OccurrenceCount == 1     → Suspected
     - OccurrenceCount >= 3     → Confirmed
     - Found in priorAlerts     → Persistent
       (Also: Notice → Warning promotion for persistent issues)

  4. Merge message: "[×{count}] {message}"

  5. Sort: Severity DESC → Confidence DESC → OccurrenceCount DESC
```

### Per-Page-Type Thresholds:

| PageType | TTFB W/C (ms) | Total W/C (ms) | API W/C (ms) | Max Requests |
|----------|---------------|-----------------|--------------|-------------|
| Homepage | 800 / 2000 | 2500 / 6000 | 500 / 2000 | 80 |
| Product | 1000 / 3000 | 3000 / 8000 | 800 / 3000 | 100 |
| Search | 1200 / 4000 | 4000 / 10000 | 1000 / 4000 | 60 |
| Category | 1000 / 3000 | 3500 / 9000 | 800 / 3000 | 90 |
| Checkout | 800 / 2000 | 2000 / 5000 | 500 / 1500 | 50 |
| API | 300 / 1000 | 500 / 2000 | 300 / 1000 | 1 |
| Static | 500 / 1500 | 1500 / 4000 | 1000 / 3000 | 30 |
| Unknown | 1000 / 3000 | 3000 / 10000 | 800 / 3000 | 100 |

## 6. Example Execution

### Input:
```
Target: blanco.de
Keywords: ["Armatur", "Spüle", "SILGRANIT"]
```

### Execution trace:
```
═══ Monitoring blanco.de ═══

▶ Full page load: https://blanco.de/
  [200] GET https://blanco.de/ — 420ms (with body)
  Discovered 34 sub-resources:
    [200] GET https://blanco.de/assets/main.css — 85ms
    [200] GET https://blanco.de/assets/app.js — 120ms
    [200] GET https://blanco.de/assets/vendor.js — 95ms
    [200] GET https://blanco.de/images/hero.webp — 180ms
    [200] GET https://fonts.googleapis.com/css2?family=... — 45ms  [Font/3P]
    [200] GET https://www.googletagmanager.com/gtm.js — 110ms  [Analytics]
    ... (28 more)
  → 35 requests, 0 failed, DOM≈625ms, Full=1850ms, Type=Homepage

▶ Full search: 'Armatur' → https://blanco.de/search?q=Armatur
  [200] GET https://blanco.de/search?q=Armatur — 890ms
  Discovered 22 sub-resources:
    [200] GET https://blanco.de/api/search?q=Armatur — 650ms  [API]
    [200] GET https://blanco.de/assets/search.js — 70ms
    ... (19 more)
  → 23 requests, 0 failed, Full=2100ms

▶ Full search: 'Spüle' → https://blanco.de/search?q=Sp%C3%BCle
  [200] GET https://blanco.de/search?q=Sp%C3%BCle — 920ms
  → 21 requests, 0 failed, Full=2300ms

▶ Full search: 'SILGRANIT' → https://blanco.de/search?q=SILGRANIT
  [200] GET https://blanco.de/search?q=SILGRANIT — 1100ms
  → 24 requests, 1 failed, Full=2800ms

═══ Analysis Results ═══

PerformanceMetric:
  URL:                https://blanco.de/
  PageType:           Homepage
  TTFB:               420ms
  DOM Ready (est):    625ms
  Full Page Load:     1850ms
  Total Requests:     103 (across all page loads)
  Failed Requests:    1
  Third-Party:        12
  API Avg:            780ms
  API P95:            1050ms
  Transfer:           2.4MB

ResourceBreakdown:
  Html:        4 requests,   320ms avg,  145KB
  JavaScript: 18 requests,    92ms avg,  890KB
  Css:         6 requests,    78ms avg,  210KB
  Image:      42 requests,   125ms avg,  980KB
  Api:         8 requests,   780ms avg,   45KB
  Font:        7 requests,    55ms avg,  180KB
  Analytics:  14 requests,    95ms avg,   12KB  [NOISE]
  Other:       4 requests,    60ms avg,   30KB

SlowestEndpoints:
  1. /search?q=SILGRANIT      1100ms  [Html]
  2. /api/search?q=Armatur     650ms  [API]
  3. /api/search?q=SILGRANIT   580ms  [API]
  4. /images/hero.webp         180ms  [Image]
  5. /assets/app.js            120ms  [JavaScript]

Alerts (6):
  ⚠  [Confirmed] API Avg = 780ms exceeds warning (500ms)
  ⚠  [Suspected] Slow Api endpoint: /api/search?q=Armatur = 650ms
  ℹ  [Suspected] 1/103 requests failed (1.0%)
  ℹ  [Suspected] Excessive requests: 103 (max: 80 for Homepage)
  ℹ  [Suspected] High 3rd-party ratio: 12/103 requests
  ℹ  [×3 Confirmed] Search results slow: avg 2400ms
```

## 7. File Map

```
Domain/
  Enums/DomainEnums.cs          → RequestCategory, ConfidenceLevel, PageType, Severity(+Notice)
  Entities/
    NetworkTrace.cs             → +Category, +InitiatorUrl, +IsThirdParty
    PerformanceMetric.cs        → +ResourceBreakdowns, +API metrics, +SlowEndpoints, +PageType
    Alert.cs                    → +Confidence, +GroupKey, +OccurrenceCount
    PageLoadResult.cs           → NEW: primary trace + sub-resources + page type
  ValueObjects/
    PageTypeThresholds.cs       → NEW: per-page-type threshold config with defaults
  Interfaces/
    INetworkClient.cs           → +FetchPageWithResourcesAsync
    IScenarioEngine.cs          → +NavigateFullAsync, +SearchFullAsync
    IRuleEngine.cs              → +EvaluateAdvanced

Infrastructure/
  Network/
    HttpNetworkClient.cs        → Sub-resource discovery (HTML regex parsing), parallel fetch
  Browser/
    HttpScenarioEngine.cs       → User simulation, referer chains, jittered delays
  Analysis/
    PerformanceAnalyzerImpl.cs  → Classification, noise filtering, API isolation
    RuleEngineImpl.cs           → 11 rule types, per-page-type thresholds, confidence
    RequestClassifier.cs        → NEW: request type classification engine
    NoiseFilter.cs              → NEW: analytics/font/tracking noise filter
    PageTypeDetector.cs         → NEW: URL-based page type detection
    IssueGrouper.cs             → NEW: issue grouping + deduplication + confidence

Application/
  Services/
    MonitoringOrchestrator.cs   → Updated to use NavigateFullAsync + EvaluateAdvanced
```
