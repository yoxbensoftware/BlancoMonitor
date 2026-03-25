# BlancoMonitor — Data Model Reference

## 1. Entity Relationship Diagram (Text)

```
┌──────────────┐
│   UrlSet     │ ← Top-level configuration group
├──────────────┤
│ Id (PK)      │
│ Name         │ 
│ BaseUrl      │
└──────┬───────┘
       │ 1:N
       ├──────────────────┐──────────────────┐
       ▼                  ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ UrlSetEntry  │  │ KeywordSet   │  │  Scenario    │
├──────────────┤  ├──────────────┤  ├──────────────┤
│ UrlSetId(FK) │  │ UrlSetId(FK) │  │ UrlSetId(FK) │
│ Url          │  │ KeywordsCsv  │  │ StepsJson    │
│ IsDiscovered │  │ Name         │  │ Name         │
└──────────────┘  └──────────────┘  └──────┬───────┘
                                           │ 0..1:N
                                           ▼
┌──────────────┐                   ┌───────────────────┐
│  RunSession  │ ←─── 1:N ───────▶│ ScenarioExecution  │
├──────────────┤                   ├───────────────────┤
│ Id (PK)      │                   │ RunSessionId (FK) │
│ StartedAt    │                   │ ScenarioId (FK?)  │
│ Status       │                   │ UrlSetId (FK)     │
│ TotalUrls    │                   │ Status            │
│ ReportPath   │                   └────────┬──────────┘
└──────┬───────┘                            │ 1:N
       │                                    ▼
       │                           ┌──────────────┐
       │                           │  PageVisit   │ ← Core measurement row
       │                           ├──────────────┤
       │ 1:N (denormalized FK)     │ ScenExecId   │
       └──────────────────────────▶│ RunSessionId │
                                   │ Url          │
                                   │ TotalTimeMs  │
                                   │ StatusCode   │
                                   └──────┬───────┘
                                          │ 1:N        1:N         1:N
                            ┌─────────────┼────────────┼───────────┐
                            ▼             ▼            ▼           ▼
                   ┌────────────────┐ ┌────────────┐ ┌──────────────┐
                   │ NetworkRequest │ │DetectedIssue│ │ EvidenceItem │
                   ├────────────────┤ ├────────────┤ ├──────────────┤
                   │ PageVisitId    │ │PageVisitId │ │ PageVisitId  │
                   │ Url, Method    │ │RunSessionId│ │ Type         │
                   │ StatusCode     │ │Severity    │ │ FilePath     │
                   │ TotalTimeMs    │ │Category    │ │ FileSizeBytes│
                   └────────────────┘ │Confidence  │ └──────────────┘
                                      └────────────┘

┌──────────────────┐                  ┌─────────────────────┐
│  DailySummary    │                  │ BaselineComparison  │
├──────────────────┤                  ├─────────────────────┤
│ UrlSetId (FK)    │                  │ RunSessionId (FK)   │
│ Date (UNIQUE w/) │                  │ BaselineRunId (FK?) │
│ AvgResponseMs    │                  │ Url                 │
│ P95ResponseMs    │                  │ DeltaMs / Percent   │
│ AvailabilityPct  │                  │ Trend               │
└──────────────────┘                  └─────────────────────┘
```

## 2. Entity Definitions Summary

| Entity | Purpose | Parent FK(s) |
|--------|---------|-------------|
| **UrlSet** | Named group of URLs to monitor | — |
| **UrlSetEntry** | Individual URL within a set | `UrlSet.Id` |
| **KeywordSet** | Search keywords for a URL set | `UrlSet.Id` |
| **Scenario** | Reusable monitoring workflow template | `UrlSet.Id` |
| **RunSession** | Top-level container for one monitoring run | — |
| **ScenarioExecution** | One scenario executed within a run | `RunSession.Id`, `UrlSet.Id`, `Scenario.Id?` |
| **PageVisit** | One URL visited with performance metrics | `ScenarioExecution.Id`, `RunSession.Id` |
| **NetworkRequest** | Individual HTTP request during a page visit | `PageVisit.Id` |
| **DetectedIssue** | Issue found during monitoring | `PageVisit.Id`, `RunSession.Id` |
| **EvidenceItem** | Screenshot/file evidence for a page visit | `PageVisit.Id` |
| **DailySummary** | Pre-computed daily aggregates per UrlSet | `UrlSet.Id` |
| **BaselineComparison** | Run-over-run performance comparison per URL | `RunSession.Id`, `RunSession.Id?` |

## 3. Relationships Explained

### Configuration Layer (setup-time)
- **UrlSet → UrlSetEntry** (1:N): Each URL set contains one or more URLs. Entries can be manually added or auto-discovered via sitemap.
- **UrlSet → KeywordSet** (1:N): Each URL set has keyword groups used for search simulation.
- **UrlSet → Scenario** (1:N): Reusable monitoring workflow templates are scoped to a URL set.

### Execution Layer (run-time)
- **RunSession → ScenarioExecution** (1:N): Each run can execute multiple scenarios (typically one per target URL set).
- **ScenarioExecution → Scenario** (N:1, optional): An execution may reference a scenario template, or run ad-hoc.
- **ScenarioExecution → UrlSet** (N:1): Every execution targets a specific URL set.
- **ScenarioExecution → PageVisit** (1:N): Each execution produces one PageVisit per URL monitored.

### Measurement Layer (per-URL)
- **PageVisit → NetworkRequest** (1:N): Each page visit captures multiple HTTP requests (navigation, keyword searches).
- **PageVisit → DetectedIssue** (1:N): Issues detected by the rule engine are linked to the specific page.
- **PageVisit → EvidenceItem** (1:N): Screenshots and response captures are metadata rows; actual files live on disk.
- **DetectedIssue → RunSession** (N:1, denormalized): Allows querying all issues for a run without joining through PageVisit → ScenarioExecution.

### Reporting Layer (computed)
- **DailySummary → UrlSet** (N:1): One summary row per UrlSet per calendar date. UPSERT ensures idempotent aggregation.
- **BaselineComparison → RunSession** (N:1, two FKs): Compares current run against a baseline run, per URL.

## 4. Indexing Strategy

### Design Principles
1. **Every FK gets an index** — Enables fast CASCADE deletes and join lookups.
2. **Time-series access patterns** — DESC indexes on `StartedAt`, `Timestamp` for "latest first" queries.
3. **URL-based history** — Indexes on `Url` columns for cross-run trend queries.
4. **Unique constraint** — `DailySummary(UrlSetId, Date)` prevents duplicate daily aggregates and enables UPSERT.
5. **Performance diagnostics** — `PageVisit(TotalTimeMs DESC)` enables fast "slowest pages" queries.

### Index Map

| Table | Index | Columns | Purpose |
|-------|-------|---------|---------|
| RunSession | IX_RunSession_StartedAt | `StartedAt DESC` | Dashboard "recent runs" list |
| RunSession | IX_RunSession_Status | `Status` | Filter by running/completed |
| UrlSetEntry | IX_UrlSetEntry_UrlSetId | `UrlSetId` | FK lookup |
| KeywordSet | IX_KeywordSet_UrlSetId | `UrlSetId` | FK lookup |
| Scenario | IX_Scenario_UrlSetId | `UrlSetId` | FK lookup |
| ScenarioExecution | IX_ScenarioExec_RunSessionId | `RunSessionId` | FK lookup |
| ScenarioExecution | IX_ScenarioExec_UrlSetId | `UrlSetId` | FK lookup |
| PageVisit | IX_PageVisit_ScenExecId | `ScenarioExecutionId` | FK lookup |
| PageVisit | IX_PageVisit_RunSessionId | `RunSessionId` | All visits for a run |
| PageVisit | IX_PageVisit_Url | `Url` | Cross-run history for a URL |
| PageVisit | IX_PageVisit_Timestamp | `Timestamp DESC` | Time-series ordering |
| PageVisit | IX_PageVisit_TotalTimeMs | `TotalTimeMs DESC` | Slowest page analysis |
| NetworkRequest | IX_NetworkReq_PageVisitId | `PageVisitId` | FK lookup |
| NetworkRequest | IX_NetworkReq_Url | `Url` | Sub-request analysis |
| DetectedIssue | IX_DetectedIssue_PageVisitId | `PageVisitId` | FK lookup |
| DetectedIssue | IX_DetectedIssue_RunSessionId | `RunSessionId` | All issues in a run |
| DetectedIssue | IX_DetectedIssue_Url | `Url` | Issue history per URL |
| DetectedIssue | IX_DetectedIssue_Severity | `Severity` | Filter critical/warning |
| EvidenceItem | IX_Evidence_PageVisitId | `PageVisitId` | FK lookup |
| DailySummary | IX_DailySummary_UrlSet_Date | `UrlSetId, Date DESC` | Trend queries |
| DailySummary | UX_DailySummary_Unique | `UrlSetId, Date` (UNIQUE) | UPSERT support |
| BaselineComparison | IX_BaselineComp_RunSessionId | `RunSessionId` | All comparisons for a run |
| BaselineComparison | IX_BaselineComp_Url | `Url` | Trend per URL |

### SQLite Performance Pragmas
```sql
PRAGMA journal_mode = WAL;        -- Concurrent reads during writes
PRAGMA synchronous = NORMAL;      -- Safe with WAL, better performance
PRAGMA foreign_keys = ON;         -- Enforce referential integrity
PRAGMA cache_size = -8000;        -- 8MB page cache
PRAGMA temp_store = MEMORY;       -- Temp tables in RAM
```

## 5. Example Data Flow — One Complete Run

```
User clicks [Start Monitoring]
│
├─ 1. RunSession created ──────────────────────────────────────────
│      INSERT RunSession (Id=RS-001, Status=Running, StartedAt=now)
│
├─ 2. Discovery Phase ────────────────────────────────────────────
│      For each MonitorTarget:
│        → SitemapDiscoveryEngine discovers URLs
│        → ScenarioExecution created per target
│          INSERT ScenarioExecution (RunSessionId=RS-001, UrlSetId=US-001)
│
├─ 3. Monitoring Phase (per URL) ─────────────────────────────────
│      For each URL in allUrls:
│        a) HttpScenarioEngine.NavigateAsync(url)
│        b) HttpScenarioEngine.SearchAsync(url, keyword) × N
│        c) PerformanceAnalyzer.Analyze(traces)
│        d) RuleEngine.Evaluate(metrics, thresholds) → alerts
│        e) EvidenceCollector.CaptureScreenshot() (optional)
│        f) HistoricalStore.Save() (JSON — legacy)
│        │
│        └── DB Persistence ──────────────────────────────
│              INSERT PageVisit (
│                  Id=PV-001, Url="https://blanco.de/page1",
│                  TotalTimeMs=1250, StatusCode=200,
│                  RunSessionId=RS-001, ScenExecId=SE-001
│              )
│              INSERT NetworkRequest × N (
│                  PageVisitId=PV-001, Url=..., TotalTimeMs=...
│              )    ← batch insert in single transaction
│              INSERT DetectedIssue × M (
│                  PageVisitId=PV-001, RunSessionId=RS-001,
│                  Severity=Warning, Category=Performance,
│                  MetricName="TotalTimeMs", ActualValue=1250
│              )
│              INSERT EvidenceItem (
│                  PageVisitId=PV-001, Type=Screenshot,
│                  FilePath="Evidence/screenshot_001.png"
│              )
│
├─ 4. Finalization ───────────────────────────────────────────────
│      UPDATE RunSession SET
│          Status=Completed, CompletedAt=now,
│          TotalUrls=15, SuccessCount=14, FailureCount=1,
│          WarningCount=3, CriticalCount=1,
│          AverageResponseTimeMs=850, MaxResponseTimeMs=4200
│
│      UPDATE ScenarioExecution SET
│          Status=Completed, PagesCompleted=15,
│          DurationMs=32500
│
├─ 5. Baseline Comparison ───────────────────────────────────────
│      Fetch previous RunSession (RS-000)
│      For each URL in current run:
│        Compare current vs baseline metrics
│        INSERT BaselineComparison (
│            Url="https://blanco.de/page1",
│            RunSessionId=RS-001, BaselineRunSessionId=RS-000,
│            CurrentAvgMs=1250, BaselineAvgMs=1100,
│            DeltaMs=+150, DeltaPercent=+13.6%,
│            Trend=Degrading
│        )
│
└─ 6. Report Generation ─────────────────────────────────────────
       HtmlReportGenerator writes report file
       Path stored in RunSession.ReportPath
```

## 6. Multi-Run Comparison Queries

### Find regressions across last 5 runs
```sql
SELECT bc.Url, bc.DeltaPercent, bc.Trend,
       rs.StartedAt AS RunDate
FROM BaselineComparison bc
JOIN RunSession rs ON rs.Id = bc.RunSessionId
WHERE bc.Trend = 2  -- Degrading
ORDER BY rs.StartedAt DESC
LIMIT 50;
```

### Daily availability trend for a UrlSet
```sql
SELECT Date, AvailabilityPercent, AvgResponseTimeMs,
       CriticalIssues, WarningIssues
FROM DailySummary
WHERE UrlSetId = @UrlSetId
ORDER BY Date DESC
LIMIT 30;
```

### Slowest pages in a run
```sql
SELECT Url, TotalTimeMs, StatusCode, Success
FROM PageVisit
WHERE RunSessionId = @RunSessionId
ORDER BY TotalTimeMs DESC
LIMIT 10;
```

### Issue history for a specific URL
```sql
SELECT di.Severity, di.Category, di.Title,
       di.ActualValue, di.ThresholdValue, di.Timestamp,
       rs.Name AS RunName
FROM DetectedIssue di
JOIN RunSession rs ON rs.Id = di.RunSessionId
WHERE di.Url = @Url
ORDER BY di.Timestamp DESC
LIMIT 50;
```

## 7. Runtime → Persistence Model Mapping

| Runtime Model | → | Persistence Entity | Mapping Notes |
|--------------|---|-------------------|---------------|
| `MonitoringSummary` | → | `RunSession` | Aggregate stats stored as columns |
| `MonitorTarget` | → | `UrlSet` + `UrlSetEntry` | Target.Id maps to UrlSet.Id |
| `MonitoringResult` | → | `PageVisit` | Metrics inlined (no separate table) |
| `NetworkTrace` | → | `NetworkRequest` | Headers serialized as JSON strings |
| `Alert` | → | `DetectedIssue` | Category auto-derived from MetricName |
| `ScenarioDefinition` | → | `Scenario` | Steps serialized as JSON |
| Screenshot path | → | `EvidenceItem` | File metadata only; file on disk |
| *(computed)* | → | `DailySummary` | Aggregated post-run |
| *(computed)* | → | `BaselineComparison` | Auto-generated: current vs previous run |

All mappings are handled by `DataMapper.cs` in the Application layer.
