namespace BlancoMonitor.Infrastructure.Database;

/// <summary>
/// Contains all SQLite DDL statements for the BlancoMonitor database.
/// Tables, indexes, and triggers are versioned here for auditability.
/// </summary>
internal static class DatabaseSchema
{
    public const int SchemaVersion = 1;

    public static readonly string[] CreateTableStatements =
    [
        // ── UrlSet ──────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS UrlSet (
            Id              TEXT PRIMARY KEY NOT NULL,
            Name            TEXT NOT NULL,
            BaseUrl         TEXT NOT NULL,
            IsActive        INTEGER NOT NULL DEFAULT 1,
            CheckIntervalSeconds INTEGER NOT NULL DEFAULT 60,
            CreatedAt       TEXT NOT NULL,
            UpdatedAt       TEXT NOT NULL
        );
        """,

        // ── UrlSetEntry ─────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS UrlSetEntry (
            Id              TEXT PRIMARY KEY NOT NULL,
            UrlSetId        TEXT NOT NULL,
            Url             TEXT NOT NULL,
            Label           TEXT,
            IsDiscovered    INTEGER NOT NULL DEFAULT 0,
            IsActive        INTEGER NOT NULL DEFAULT 1,
            CreatedAt       TEXT NOT NULL,
            FOREIGN KEY (UrlSetId) REFERENCES UrlSet(Id) ON DELETE CASCADE
        );
        """,

        // ── KeywordSet ──────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS KeywordSet (
            Id              TEXT PRIMARY KEY NOT NULL,
            UrlSetId        TEXT NOT NULL,
            Name            TEXT NOT NULL,
            KeywordsCsv     TEXT NOT NULL DEFAULT '',
            CreatedAt       TEXT NOT NULL,
            FOREIGN KEY (UrlSetId) REFERENCES UrlSet(Id) ON DELETE CASCADE
        );
        """,

        // ── Scenario ────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS Scenario (
            Id              TEXT PRIMARY KEY NOT NULL,
            Name            TEXT NOT NULL,
            Description     TEXT,
            UrlSetId        TEXT NOT NULL,
            StepsJson       TEXT NOT NULL DEFAULT '[]',
            CreatedAt       TEXT NOT NULL,
            UpdatedAt       TEXT NOT NULL,
            IsActive        INTEGER NOT NULL DEFAULT 1,
            FOREIGN KEY (UrlSetId) REFERENCES UrlSet(Id) ON DELETE CASCADE
        );
        """,

        // ── RunSession ──────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS RunSession (
            Id              TEXT PRIMARY KEY NOT NULL,
            Name            TEXT,
            StartedAt       TEXT NOT NULL,
            CompletedAt     TEXT,
            Status          INTEGER NOT NULL DEFAULT 0,
            TotalUrls       INTEGER NOT NULL DEFAULT 0,
            SuccessCount    INTEGER NOT NULL DEFAULT 0,
            FailureCount    INTEGER NOT NULL DEFAULT 0,
            WarningCount    INTEGER NOT NULL DEFAULT 0,
            CriticalCount   INTEGER NOT NULL DEFAULT 0,
            AverageResponseTimeMs REAL NOT NULL DEFAULT 0,
            MaxResponseTimeMs     REAL NOT NULL DEFAULT 0,
            TotalDurationMs       REAL NOT NULL DEFAULT 0,
            ReportPath      TEXT,
            Notes           TEXT
        );
        """,

        // ── ScenarioExecution ───────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS ScenarioExecution (
            Id              TEXT PRIMARY KEY NOT NULL,
            RunSessionId    TEXT NOT NULL,
            ScenarioId      TEXT,
            UrlSetId        TEXT NOT NULL,
            StartedAt       TEXT NOT NULL,
            CompletedAt     TEXT,
            DurationMs      REAL NOT NULL DEFAULT 0,
            Status          INTEGER NOT NULL DEFAULT 0,
            TotalPages      INTEGER NOT NULL DEFAULT 0,
            PagesCompleted  INTEGER NOT NULL DEFAULT 0,
            ErrorMessage    TEXT,
            FOREIGN KEY (RunSessionId) REFERENCES RunSession(Id) ON DELETE CASCADE,
            FOREIGN KEY (ScenarioId)   REFERENCES Scenario(Id)    ON DELETE SET NULL,
            FOREIGN KEY (UrlSetId)     REFERENCES UrlSet(Id)      ON DELETE CASCADE
        );
        """,

        // ── PageVisit ───────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS PageVisit (
            Id                    TEXT PRIMARY KEY NOT NULL,
            ScenarioExecutionId   TEXT NOT NULL,
            RunSessionId          TEXT NOT NULL,
            Url                   TEXT NOT NULL,
            StatusCode            INTEGER NOT NULL DEFAULT 0,
            TimeToFirstByteMs     REAL NOT NULL DEFAULT 0,
            ContentDownloadMs     REAL NOT NULL DEFAULT 0,
            TotalTimeMs           REAL NOT NULL DEFAULT 0,
            ContentLength         INTEGER NOT NULL DEFAULT 0,
            ContentType           TEXT,
            Success               INTEGER NOT NULL DEFAULT 0,
            ErrorMessage          TEXT,
            Timestamp             TEXT NOT NULL,
            DurationMs            REAL NOT NULL DEFAULT 0,
            FOREIGN KEY (ScenarioExecutionId) REFERENCES ScenarioExecution(Id) ON DELETE CASCADE,
            FOREIGN KEY (RunSessionId)        REFERENCES RunSession(Id)        ON DELETE CASCADE
        );
        """,

        // ── NetworkRequest ──────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS NetworkRequest (
            Id                    TEXT PRIMARY KEY NOT NULL,
            PageVisitId           TEXT NOT NULL,
            Url                   TEXT NOT NULL,
            Method                TEXT NOT NULL DEFAULT 'GET',
            StatusCode            INTEGER NOT NULL DEFAULT 0,
            TimeToFirstByteMs     REAL NOT NULL DEFAULT 0,
            TotalTimeMs           REAL NOT NULL DEFAULT 0,
            ContentDownloadMs     REAL NOT NULL DEFAULT 0,
            ContentType           TEXT,
            ContentLength         INTEGER NOT NULL DEFAULT 0,
            RequestHeadersJson    TEXT,
            ResponseHeadersJson   TEXT,
            RedirectChainJson     TEXT,
            ErrorMessage          TEXT,
            Timestamp             TEXT NOT NULL,
            FOREIGN KEY (PageVisitId) REFERENCES PageVisit(Id) ON DELETE CASCADE
        );
        """,

        // ── DetectedIssue ───────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS DetectedIssue (
            Id              TEXT PRIMARY KEY NOT NULL,
            PageVisitId     TEXT NOT NULL,
            RunSessionId    TEXT NOT NULL,
            Severity        INTEGER NOT NULL DEFAULT 0,
            Category        INTEGER NOT NULL DEFAULT 0,
            Title           TEXT NOT NULL,
            Description     TEXT NOT NULL DEFAULT '',
            MetricName      TEXT NOT NULL DEFAULT '',
            ActualValue     REAL NOT NULL DEFAULT 0,
            ThresholdValue  REAL NOT NULL DEFAULT 0,
            Confidence      REAL NOT NULL DEFAULT 1.0,
            Url             TEXT NOT NULL DEFAULT '',
            Timestamp       TEXT NOT NULL,
            FOREIGN KEY (PageVisitId)  REFERENCES PageVisit(Id)  ON DELETE CASCADE,
            FOREIGN KEY (RunSessionId) REFERENCES RunSession(Id) ON DELETE CASCADE
        );
        """,

        // ── EvidenceItem ────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS EvidenceItem (
            Id              TEXT PRIMARY KEY NOT NULL,
            PageVisitId     TEXT NOT NULL,
            Type            INTEGER NOT NULL DEFAULT 0,
            FilePath        TEXT NOT NULL,
            Description     TEXT,
            FileSizeBytes   INTEGER NOT NULL DEFAULT 0,
            CreatedAt       TEXT NOT NULL,
            FOREIGN KEY (PageVisitId) REFERENCES PageVisit(Id) ON DELETE CASCADE
        );
        """,

        // ── DailySummary ────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS DailySummary (
            Id                        TEXT PRIMARY KEY NOT NULL,
            UrlSetId                  TEXT NOT NULL,
            Date                      TEXT NOT NULL,
            TotalRuns                 INTEGER NOT NULL DEFAULT 0,
            TotalPageVisits           INTEGER NOT NULL DEFAULT 0,
            AvgResponseTimeMs         REAL NOT NULL DEFAULT 0,
            P95ResponseTimeMs         REAL NOT NULL DEFAULT 0,
            MaxResponseTimeMs         REAL NOT NULL DEFAULT 0,
            MinResponseTimeMs         REAL NOT NULL DEFAULT 0,
            TotalIssues               INTEGER NOT NULL DEFAULT 0,
            CriticalIssues            INTEGER NOT NULL DEFAULT 0,
            WarningIssues             INTEGER NOT NULL DEFAULT 0,
            AvailabilityPercent       REAL NOT NULL DEFAULT 100.0,
            TotalDataTransferredBytes INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (UrlSetId) REFERENCES UrlSet(Id) ON DELETE CASCADE
        );
        """,

        // ── BaselineComparison ──────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS BaselineComparison (
            Id                    TEXT PRIMARY KEY NOT NULL,
            Url                   TEXT NOT NULL,
            RunSessionId          TEXT NOT NULL,
            BaselineRunSessionId  TEXT,
            CurrentAvgMs          REAL NOT NULL DEFAULT 0,
            CurrentP95Ms          REAL NOT NULL DEFAULT 0,
            CurrentMaxMs          REAL NOT NULL DEFAULT 0,
            CurrentStatusCode     INTEGER NOT NULL DEFAULT 0,
            BaselineAvgMs         REAL NOT NULL DEFAULT 0,
            BaselineP95Ms         REAL NOT NULL DEFAULT 0,
            BaselineMaxMs         REAL NOT NULL DEFAULT 0,
            BaselineStatusCode    INTEGER NOT NULL DEFAULT 0,
            DeltaMs               REAL NOT NULL DEFAULT 0,
            DeltaPercent          REAL NOT NULL DEFAULT 0,
            Trend                 INTEGER NOT NULL DEFAULT 3,
            CreatedAt             TEXT NOT NULL,
            FOREIGN KEY (RunSessionId)         REFERENCES RunSession(Id) ON DELETE CASCADE,
            FOREIGN KEY (BaselineRunSessionId) REFERENCES RunSession(Id) ON DELETE SET NULL
        );
        """,
    ];

    public static readonly string[] CreateIndexStatements =
    [
        // ── Time-series queries (most critical for performance) ──
        "CREATE INDEX IF NOT EXISTS IX_RunSession_StartedAt       ON RunSession(StartedAt DESC);",
        "CREATE INDEX IF NOT EXISTS IX_RunSession_Status           ON RunSession(Status);",

        // ── Foreign key lookups ─────────────────────────────────
        "CREATE INDEX IF NOT EXISTS IX_UrlSetEntry_UrlSetId        ON UrlSetEntry(UrlSetId);",
        "CREATE INDEX IF NOT EXISTS IX_KeywordSet_UrlSetId         ON KeywordSet(UrlSetId);",
        "CREATE INDEX IF NOT EXISTS IX_Scenario_UrlSetId           ON Scenario(UrlSetId);",
        "CREATE INDEX IF NOT EXISTS IX_ScenarioExec_RunSessionId   ON ScenarioExecution(RunSessionId);",
        "CREATE INDEX IF NOT EXISTS IX_ScenarioExec_UrlSetId       ON ScenarioExecution(UrlSetId);",
        "CREATE INDEX IF NOT EXISTS IX_PageVisit_ScenExecId        ON PageVisit(ScenarioExecutionId);",
        "CREATE INDEX IF NOT EXISTS IX_PageVisit_RunSessionId      ON PageVisit(RunSessionId);",
        "CREATE INDEX IF NOT EXISTS IX_NetworkReq_PageVisitId      ON NetworkRequest(PageVisitId);",
        "CREATE INDEX IF NOT EXISTS IX_DetectedIssue_PageVisitId   ON DetectedIssue(PageVisitId);",
        "CREATE INDEX IF NOT EXISTS IX_DetectedIssue_RunSessionId  ON DetectedIssue(RunSessionId);",
        "CREATE INDEX IF NOT EXISTS IX_Evidence_PageVisitId        ON EvidenceItem(PageVisitId);",
        "CREATE INDEX IF NOT EXISTS IX_BaselineComp_RunSessionId   ON BaselineComparison(RunSessionId);",

        // ── URL-based history queries ───────────────────────────
        "CREATE INDEX IF NOT EXISTS IX_PageVisit_Url               ON PageVisit(Url);",
        "CREATE INDEX IF NOT EXISTS IX_PageVisit_Timestamp         ON PageVisit(Timestamp DESC);",
        "CREATE INDEX IF NOT EXISTS IX_DetectedIssue_Url           ON DetectedIssue(Url);",
        "CREATE INDEX IF NOT EXISTS IX_DetectedIssue_Severity      ON DetectedIssue(Severity);",
        "CREATE INDEX IF NOT EXISTS IX_BaselineComp_Url            ON BaselineComparison(Url);",
        "CREATE INDEX IF NOT EXISTS IX_NetworkReq_Url              ON NetworkRequest(Url);",

        // ── Reporting / dashboard queries ───────────────────────
        "CREATE INDEX IF NOT EXISTS IX_DailySummary_UrlSet_Date    ON DailySummary(UrlSetId, Date DESC);",
        "CREATE UNIQUE INDEX IF NOT EXISTS UX_DailySummary_Unique  ON DailySummary(UrlSetId, Date);",

        // ── Performance: slow page analysis ─────────────────────
        "CREATE INDEX IF NOT EXISTS IX_PageVisit_TotalTimeMs       ON PageVisit(TotalTimeMs DESC);",
    ];

    public static readonly string[] PragmaStatements =
    [
        "PRAGMA journal_mode = WAL;",
        "PRAGMA synchronous = NORMAL;",
        "PRAGMA foreign_keys = ON;",
        "PRAGMA cache_size = -8000;",
        "PRAGMA temp_store = MEMORY;",
    ];
}
