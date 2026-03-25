# BlancoMonitor — Architecture Document

## 1. High-Level Architecture (Textual Diagram)
 
```
┌──────────────────────────────────────────────────────────────────────┐
│                     PRESENTATION LAYER (WinForms MDI)                │
│  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────────────┐  │
│  │ Dashboard  │ │ URL/Keyword│ │ Monitoring │ │ Results/Reports  │  │
│  │   Form     │ │  Manager   │ │   Form     │ │     Form         │  │
│  └─────┬──────┘ └─────┬──────┘ └─────┬──────┘ └────────┬─────────┘  │
│        │              │              │                  │            │
│  ┌─────┴──────────────┴──────────────┴──────────────────┴─────────┐  │
│  │                    MdiParentForm + NeonTheme                   │  │
│  └────────────────────────────┬───────────────────────────────────┘  │
└───────────────────────────────┼──────────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────────┐
│                     APPLICATION LAYER                                │
│  ┌────────────────────────────┴───────────────────────────────────┐  │
│  │               MonitoringOrchestrator                           │  │
│  │  (coordinates all engines, collects results, fires events)     │  │
│  └─────┬──────────┬──────────┬───────────┬──────────┬────────────┘  │
│        │          │          │           │          │                │
│  ┌─────┴────┐ ┌───┴────┐ ┌──┴───┐ ┌────┴───┐ ┌───┴──────────┐     │
│  │UrlKeyword│ │Config  │ │DTO   │ │Progress│ │  Historical  │     │
│  │SetManager│ │Service │ │Models│ │Reporting│ │  Comparison  │     │
│  └──────────┘ └────────┘ └──────┘ └────────┘ └──────────────┘     │
└───────────────────────────────┼──────────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────────┐
│                       DOMAIN LAYER                                   │
│  ┌────────────────────────────┴───────────────────────────────────┐  │
│  │                    Entities & Interfaces                       │  │
│  │  MonitorTarget │ ScenarioDefinition │ PerformanceMetric        │  │
│  │  NetworkTrace  │ MonitoringResult   │ Alert │ Threshold        │  │
│  │  HistoricalRecord │ AppConfiguration                          │  │
│  ├────────────────────────────────────────────────────────────────┤  │
│  │  INetworkClient      │ IScenarioEngine   │ IDiscoveryEngine   │  │
│  │  IPerformanceAnalyzer │ IRuleEngine       │ IEvidenceCollector │  │
│  │  IReportGenerator    │ IHistoricalStore  │ IAppLogger         │  │
│  │  IConfigurationStore                                          │  │
│  └────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────┼──────────────────────────────────────┘
                                │
┌───────────────────────────────┼──────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                               │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │
│  │  HttpNetwork │ │  HttpScenario│ │  Sitemap     │                 │
│  │  Client      │ │  Engine      │ │  Discovery   │                 │
│  └──────────────┘ └──────────────┘ └──────────────┘                 │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │
│  │  Performance │ │  Rule Engine │ │  Evidence    │                 │
│  │  Analyzer    │ │  Impl        │ │  Collector   │                 │
│  └──────────────┘ └──────────────┘ └──────────────┘                 │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │
│  │  File Logger │ │  HTML Report │ │  JSON History│                 │
│  │              │ │  Generator   │ │  Store       │                 │
│  └──────────────┘ └──────────────┘ └──────────────┘                 │
│  ┌──────────────┐                                                   │
│  │  JSON Config │                                                   │
│  │  Manager     │                                                   │
│  └──────────────┘                                                   │
└──────────────────────────────────────────────────────────────────────┘
```

## 2. Layer Breakdown

### Presentation Layer
- **MdiParentForm**: MDI container, menu system, status bar, neon-themed shell
- **DashboardForm**: Live overview of all targets with status indicators
- **UrlManagerForm**: CRUD for monitored URLs and keyword sets
- **MonitoringForm**: Real-time monitoring with live network trace log
- **ResultsForm**: Tabular and detailed result viewer with historical comparison
- **SettingsForm**: Threshold configuration, ignore rules, whitelist
- **NeonTheme**: Centralized theme engine (black bg, green text, retro terminal)
- **NeonDataGridView / NeonListBox**: Custom-themed controls

### Application Layer
- **MonitoringOrchestrator**: Coordinates discovery → scenario execution → analysis → alerting → reporting
- **UrlKeywordSetManager**: Manages target URL collections and search keyword sets
- **DTOs**: MonitoringSummary, PerformanceReport for UI consumption

### Domain Layer
- **Entities**: MonitorTarget, ScenarioDefinition, PerformanceMetric, NetworkTrace, MonitoringResult, Alert, HistoricalRecord, AppConfiguration
- **Value Objects**: Threshold (immutable, equality by value)
- **Enums**: Severity, MonitorStatus, ScenarioActionType, TrendDirection, ComparisonOperator
- **Interfaces**: All abstractions live here — dependency inversion principle

### Infrastructure Layer
- **HttpNetworkClient**: Core HTTP executor with Stopwatch-based timing
- **HttpScenarioEngine**: Executes multi-step navigation scenarios via HttpClient
- **SitemapDiscoveryEngine**: Parses robots.txt and XML sitemaps, crawls internal links
- **PerformanceAnalyzerImpl**: Statistical analysis of timing metrics
- **RuleEngineImpl**: Threshold evaluation, severity assignment, alert generation
- **HeadlessEvidenceCollector**: Stub for Playwright-based screenshot capture
- **FileLogger**: Structured file-based logging with rotation
- **HtmlReportGenerator**: Generates styled HTML reports
- **JsonHistoricalStore**: File-based JSON persistence for historical data
- **JsonConfigurationManager**: JSON-based configuration persistence

## 3. Class/Module Responsibilities

| Module | Responsibility |
|--------|---------------|
| MonitoringOrchestrator | Single entry point for a monitoring run; coordinates all engines sequentially |
| HttpNetworkClient | Makes HTTP requests, measures TTFB/total time/content download, captures headers |
| HttpScenarioEngine | Executes ordered ScenarioStep lists (Navigate, Search, Wait) via HttpNetworkClient |
| SitemapDiscoveryEngine | Fetches /robots.txt → parses sitemap URLs → extracts all <loc> entries |
| PerformanceAnalyzerImpl | Computes avg/p50/p95/p99/max for each metric across result sets |
| RuleEngineImpl | Evaluates each metric against configured Threshold, produces Alert list |
| HeadlessEvidenceCollector | Optional: captures page screenshots via headless browser (Playwright plug-in) |
| FileLogger | Thread-safe file append with timestamp, severity, and message formatting |
| HtmlReportGenerator | Renders MonitoringSummary into a self-contained HTML file with inline CSS |
| JsonHistoricalStore | Saves/loads HistoricalRecord per URL as JSON files in a data directory |
| JsonConfigurationManager | Reads/writes AppConfiguration as a single JSON file |
| UrlKeywordSetManager | In-memory management of MonitorTarget list with persistence delegation |

## 4. Data Flow (Step-by-Step)

```
1. [User] → Configures URLs + keywords in UrlManagerForm
2. [User] → Clicks "Start Monitoring" in MonitoringForm
3. [MonitoringOrchestrator] receives List<MonitorTarget>
4. FOR EACH MonitorTarget:
   4a. [DiscoveryEngine].DiscoverUrls(target.Url)
       → Fetches /sitemap.xml, parses <loc> entries
       → Returns List<string> discoveredUrls
   4b. FOR EACH discoveredUrl:
       4b-i.   [ScenarioEngine].ExecuteAsync(scenario, url)
               → [HttpNetworkClient].SendAsync(GET url)
               → Measures: TTFB, TotalTime, ContentDownload, StatusCode
               → Returns NetworkTrace
       4b-ii.  IF keywords configured:
               → [ScenarioEngine] constructs search URL (url + ?q=keyword)
               → [HttpNetworkClient].SendAsync(GET searchUrl)
               → Returns NetworkTrace for search
       4b-iii. [PerformanceAnalyzer].Analyze(traces)
               → Computes PerformanceMetric (avg, p95, etc.)
       4b-iv.  [RuleEngine].Evaluate(metrics, thresholds)
               → Produces List<Alert> (warning/critical)
       4b-v.   [EvidenceCollector].CaptureAsync(url) [if enabled]
               → Saves screenshot to evidence directory
       4b-vi.  Assembles MonitoringResult
   4c. [HistoricalStore].SaveAsync(results)
   4d. [HistoricalStore].Compare(current, baseline)
       → Detects regressions/improvements
5. [MonitoringOrchestrator] assembles MonitoringSummary
6. [ReportGenerator].GenerateAsync(summary)
   → Writes HTML report to reports directory
7. [AppLogger] logs all steps with timestamps
8. [UI] receives progress updates via IProgress<T>
9. [ResultsForm] displays final results + alerts
```

## 5. Technology Decisions

### Why HttpClient over Playwright (Default)?

| Factor | HttpClient | Playwright |
|--------|-----------|------------|
| **No external dependencies** | ✅ Built into .NET | ❌ Requires browser binaries (~200MB) |
| **Precision timing** | ✅ Stopwatch + ResponseHeadersRead | ⚠️ Good but abstracted |
| **Resource usage** | ✅ Minimal (no browser process) | ❌ Heavy (Chromium process per context) |
| **Non-invasive** | ✅ Pure HTTP, no UI at all | ⚠️ Headless but still spawns processes |
| **Speed** | ✅ Microsecond-level | ⚠️ Millisecond-level overhead |
| **JavaScript rendering** | ❌ No JS execution | ✅ Full browser engine |
| **Screenshots** | ❌ Not possible | ✅ Built-in |

**Decision**: Use HttpClient as the default engine. Playwright is pluggable via `IScenarioEngine` and `IEvidenceCollector` interfaces for when JavaScript rendering or screenshots are needed. The architecture supports swapping engines without changing application or domain layers.

### Other Technology Choices
- **JSON persistence** via System.Text.Json (built-in, fast, AOT-friendly)
- **No ORM** — file-based storage is appropriate for a desktop monitoring tool
- **No DI container** — manual constructor injection keeps it simple for WinForms
- **CancellationToken** throughout for graceful shutdown
- **IProgress<T>** for UI updates from background operations

## 6. Non-Invasive Safe Behavior

1. **No browser UI**: HttpClient makes pure HTTP/HTTPS requests — no Chrome/Edge window
2. **Respectful crawling**: Honors robots.txt, configurable delays between requests
3. **Rate limiting**: MaxConcurrentRequests defaults to 2, configurable
4. **User-Agent identification**: Sends identifiable UA string (configurable)
5. **No state mutation**: Read-only monitoring — never submits forms, never creates accounts
6. **Timeout protection**: All requests have configurable timeouts (default 30s)
7. **Graceful cancellation**: CancellationToken propagated through all async chains
8. **No credential storage**: Does not handle login flows or store credentials
9. **Evidence is local-only**: Screenshots/logs stored locally, never transmitted
10. **Configurable scope**: Whitelist/ignore patterns prevent accidental crawling of external sites
