using Microsoft.Data.Sqlite;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Database;

/// <summary>
/// SQLite-backed implementation of <see cref="IBlancoDatabase"/>.
/// Uses raw ADO.NET for maximum control and minimal overhead.
/// Connection pooling is handled by Microsoft.Data.Sqlite internally.
/// </summary>
public sealed class BlancoDatabase : IBlancoDatabase
{
    private readonly string _connectionString;
    private readonly IAppLogger _logger;

    public BlancoDatabase(string databasePath, IAppLogger logger)
    {
        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────────
    //  Initialization
    // ────────────────────────────────────────────────────────────

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct);

        foreach (var pragma in DatabaseSchema.PragmaStatements)
            await ExecuteNonQueryAsync(conn, pragma, ct);

        foreach (var ddl in DatabaseSchema.CreateTableStatements)
            await ExecuteNonQueryAsync(conn, ddl, ct);

        foreach (var idx in DatabaseSchema.CreateIndexStatements)
            await ExecuteNonQueryAsync(conn, idx, ct);

        _logger.Info("Database initialized (SQLite)");
    }

    // ────────────────────────────────────────────────────────────
    //  RunSession
    // ────────────────────────────────────────────────────────────

    public async Task<Guid> InsertRunSessionAsync(RunSession s, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO RunSession (Id, Name, StartedAt, CompletedAt, Status,
                TotalUrls, SuccessCount, FailureCount, WarningCount, CriticalCount,
                AverageResponseTimeMs, MaxResponseTimeMs, TotalDurationMs, ReportPath, Notes)
            VALUES (@Id, @Name, @StartedAt, @CompletedAt, @Status,
                @TotalUrls, @SuccessCount, @FailureCount, @WarningCount, @CriticalCount,
                @AverageResponseTimeMs, @MaxResponseTimeMs, @TotalDurationMs, @ReportPath, @Notes);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddRunSessionParams(cmd, s);
        await cmd.ExecuteNonQueryAsync(ct);
        return s.Id;
    }

    public async Task UpdateRunSessionAsync(RunSession s, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE RunSession SET
                Name = @Name, CompletedAt = @CompletedAt, Status = @Status,
                TotalUrls = @TotalUrls, SuccessCount = @SuccessCount, FailureCount = @FailureCount,
                WarningCount = @WarningCount, CriticalCount = @CriticalCount,
                AverageResponseTimeMs = @AverageResponseTimeMs, MaxResponseTimeMs = @MaxResponseTimeMs,
                TotalDurationMs = @TotalDurationMs, ReportPath = @ReportPath, Notes = @Notes
            WHERE Id = @Id;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        AddRunSessionParams(cmd, s);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<RunSession?> GetRunSessionAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM RunSession WHERE Id = @Id;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadRunSession(r) : null;
    }

    public async Task<List<RunSession>> GetRunSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM RunSession ORDER BY StartedAt DESC LIMIT @Limit OFFSET @Offset;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Limit", limit);
        cmd.Parameters.AddWithValue("@Offset", offset);
        return await ReadListAsync(cmd, ReadRunSession, ct);
    }

    public async Task<RunSession?> GetLatestRunSessionAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM RunSession ORDER BY StartedAt DESC LIMIT 1;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadRunSession(r) : null;
    }

    public async Task<int> GetTotalRunCountAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT COUNT(*) FROM RunSession;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result);
    }

    // ────────────────────────────────────────────────────────────
    //  UrlSet + Entries + Keywords
    // ────────────────────────────────────────────────────────────

    public async Task<Guid> InsertUrlSetAsync(UrlSet u, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO UrlSet (Id, Name, BaseUrl, IsActive, CheckIntervalSeconds, CreatedAt, UpdatedAt)
            VALUES (@Id, @Name, @BaseUrl, @IsActive, @CheckIntervalSeconds, @CreatedAt, @UpdatedAt);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", u.Id.ToString());
        cmd.Parameters.AddWithValue("@Name", u.Name);
        cmd.Parameters.AddWithValue("@BaseUrl", u.BaseUrl);
        cmd.Parameters.AddWithValue("@IsActive", u.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@CheckIntervalSeconds", u.CheckIntervalSeconds);
        cmd.Parameters.AddWithValue("@CreatedAt", u.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", u.UpdatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
        return u.Id;
    }

    public async Task UpdateUrlSetAsync(UrlSet u, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE UrlSet SET Name = @Name, BaseUrl = @BaseUrl, IsActive = @IsActive,
                CheckIntervalSeconds = @CheckIntervalSeconds, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", u.Id.ToString());
        cmd.Parameters.AddWithValue("@Name", u.Name);
        cmd.Parameters.AddWithValue("@BaseUrl", u.BaseUrl);
        cmd.Parameters.AddWithValue("@IsActive", u.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@CheckIntervalSeconds", u.CheckIntervalSeconds);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<UrlSet>> GetUrlSetsAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM UrlSet ORDER BY Name;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return await ReadListAsync(cmd, ReadUrlSet, ct);
    }

    public async Task<UrlSet?> GetUrlSetAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM UrlSet WHERE Id = @Id;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadUrlSet(r) : null;
    }

    public async Task DeleteUrlSetAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM UrlSet WHERE Id = @Id;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task InsertUrlSetEntryAsync(UrlSetEntry e, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO UrlSetEntry (Id, UrlSetId, Url, Label, IsDiscovered, IsActive, CreatedAt)
            VALUES (@Id, @UrlSetId, @Url, @Label, @IsDiscovered, @IsActive, @CreatedAt);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", e.Id.ToString());
        cmd.Parameters.AddWithValue("@UrlSetId", e.UrlSetId.ToString());
        cmd.Parameters.AddWithValue("@Url", e.Url);
        cmd.Parameters.AddWithValue("@Label", (object?)e.Label ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsDiscovered", e.IsDiscovered ? 1 : 0);
        cmd.Parameters.AddWithValue("@IsActive", e.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@CreatedAt", e.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<UrlSetEntry>> GetUrlSetEntriesAsync(Guid urlSetId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM UrlSetEntry WHERE UrlSetId = @UrlSetId ORDER BY CreatedAt;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@UrlSetId", urlSetId.ToString());
        return await ReadListAsync(cmd, ReadUrlSetEntry, ct);
    }

    public async Task InsertKeywordSetAsync(KeywordSet k, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO KeywordSet (Id, UrlSetId, Name, KeywordsCsv, CreatedAt)
            VALUES (@Id, @UrlSetId, @Name, @KeywordsCsv, @CreatedAt);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", k.Id.ToString());
        cmd.Parameters.AddWithValue("@UrlSetId", k.UrlSetId.ToString());
        cmd.Parameters.AddWithValue("@Name", k.Name);
        cmd.Parameters.AddWithValue("@KeywordsCsv", k.KeywordsCsv);
        cmd.Parameters.AddWithValue("@CreatedAt", k.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<KeywordSet>> GetKeywordSetsAsync(Guid urlSetId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM KeywordSet WHERE UrlSetId = @UrlSetId ORDER BY Name;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@UrlSetId", urlSetId.ToString());
        return await ReadListAsync(cmd, ReadKeywordSet, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  Scenario
    // ────────────────────────────────────────────────────────────

    public async Task<Guid> InsertScenarioAsync(Scenario s, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO Scenario (Id, Name, Description, UrlSetId, StepsJson, CreatedAt, UpdatedAt, IsActive)
            VALUES (@Id, @Name, @Description, @UrlSetId, @StepsJson, @CreatedAt, @UpdatedAt, @IsActive);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", s.Id.ToString());
        cmd.Parameters.AddWithValue("@Name", s.Name);
        cmd.Parameters.AddWithValue("@Description", (object?)s.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UrlSetId", s.UrlSetId.ToString());
        cmd.Parameters.AddWithValue("@StepsJson", s.StepsJson);
        cmd.Parameters.AddWithValue("@CreatedAt", s.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@UpdatedAt", s.UpdatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@IsActive", s.IsActive ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(ct);
        return s.Id;
    }

    public async Task UpdateScenarioAsync(Scenario s, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE Scenario SET
                Name = @Name, Description = @Description, StepsJson = @StepsJson,
                UpdatedAt = @UpdatedAt, IsActive = @IsActive
            WHERE Id = @Id;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", s.Id.ToString());
        cmd.Parameters.AddWithValue("@Name", s.Name);
        cmd.Parameters.AddWithValue("@Description", (object?)s.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StepsJson", s.StepsJson);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@IsActive", s.IsActive ? 1 : 0);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<Scenario>> GetScenariosAsync(Guid? urlSetId = null, CancellationToken ct = default)
    {
        var sql = urlSetId.HasValue
            ? "SELECT * FROM Scenario WHERE UrlSetId = @UrlSetId ORDER BY Name;"
            : "SELECT * FROM Scenario ORDER BY Name;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        if (urlSetId.HasValue)
            cmd.Parameters.AddWithValue("@UrlSetId", urlSetId.Value.ToString());
        return await ReadListAsync(cmd, ReadScenario, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  ScenarioExecution
    // ────────────────────────────────────────────────────────────

    public async Task<Guid> InsertScenarioExecutionAsync(ScenarioExecution e, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO ScenarioExecution (Id, RunSessionId, ScenarioId, UrlSetId,
                StartedAt, CompletedAt, DurationMs, Status, TotalPages, PagesCompleted, ErrorMessage)
            VALUES (@Id, @RunSessionId, @ScenarioId, @UrlSetId,
                @StartedAt, @CompletedAt, @DurationMs, @Status, @TotalPages, @PagesCompleted, @ErrorMessage);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", e.Id.ToString());
        cmd.Parameters.AddWithValue("@RunSessionId", e.RunSessionId.ToString());
        cmd.Parameters.AddWithValue("@ScenarioId", e.ScenarioId.HasValue ? e.ScenarioId.Value.ToString() : DBNull.Value);
        cmd.Parameters.AddWithValue("@UrlSetId", e.UrlSetId.ToString());
        cmd.Parameters.AddWithValue("@StartedAt", e.StartedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@CompletedAt", e.CompletedAt.HasValue ? e.CompletedAt.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("@DurationMs", e.DurationMs);
        cmd.Parameters.AddWithValue("@Status", (int)e.Status);
        cmd.Parameters.AddWithValue("@TotalPages", e.TotalPages);
        cmd.Parameters.AddWithValue("@PagesCompleted", e.PagesCompleted);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)e.ErrorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
        return e.Id;
    }

    public async Task UpdateScenarioExecutionAsync(ScenarioExecution e, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE ScenarioExecution SET
                CompletedAt = @CompletedAt, DurationMs = @DurationMs, Status = @Status,
                TotalPages = @TotalPages, PagesCompleted = @PagesCompleted, ErrorMessage = @ErrorMessage
            WHERE Id = @Id;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", e.Id.ToString());
        cmd.Parameters.AddWithValue("@CompletedAt", e.CompletedAt.HasValue ? e.CompletedAt.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("@DurationMs", e.DurationMs);
        cmd.Parameters.AddWithValue("@Status", (int)e.Status);
        cmd.Parameters.AddWithValue("@TotalPages", e.TotalPages);
        cmd.Parameters.AddWithValue("@PagesCompleted", e.PagesCompleted);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)e.ErrorMessage ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<ScenarioExecution>> GetExecutionsAsync(Guid runSessionId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM ScenarioExecution WHERE RunSessionId = @RunSessionId ORDER BY StartedAt;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@RunSessionId", runSessionId.ToString());
        return await ReadListAsync(cmd, ReadScenarioExecution, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  PageVisit
    // ────────────────────────────────────────────────────────────

    public async Task InsertPageVisitAsync(PageVisit v, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO PageVisit (Id, ScenarioExecutionId, RunSessionId, Url,
                StatusCode, TimeToFirstByteMs, ContentDownloadMs, TotalTimeMs,
                ContentLength, ContentType, Success, ErrorMessage, Timestamp, DurationMs)
            VALUES (@Id, @ScenarioExecutionId, @RunSessionId, @Url,
                @StatusCode, @TimeToFirstByteMs, @ContentDownloadMs, @TotalTimeMs,
                @ContentLength, @ContentType, @Success, @ErrorMessage, @Timestamp, @DurationMs);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", v.Id.ToString());
        cmd.Parameters.AddWithValue("@ScenarioExecutionId", v.ScenarioExecutionId.ToString());
        cmd.Parameters.AddWithValue("@RunSessionId", v.RunSessionId.ToString());
        cmd.Parameters.AddWithValue("@Url", v.Url);
        cmd.Parameters.AddWithValue("@StatusCode", v.StatusCode);
        cmd.Parameters.AddWithValue("@TimeToFirstByteMs", v.TimeToFirstByteMs);
        cmd.Parameters.AddWithValue("@ContentDownloadMs", v.ContentDownloadMs);
        cmd.Parameters.AddWithValue("@TotalTimeMs", v.TotalTimeMs);
        cmd.Parameters.AddWithValue("@ContentLength", v.ContentLength);
        cmd.Parameters.AddWithValue("@ContentType", (object?)v.ContentType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Success", v.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)v.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Timestamp", v.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@DurationMs", v.DurationMs);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<PageVisit>> GetPageVisitsAsync(Guid scenarioExecutionId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM PageVisit WHERE ScenarioExecutionId = @Id ORDER BY Timestamp;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", scenarioExecutionId.ToString());
        return await ReadListAsync(cmd, ReadPageVisit, ct);
    }

    public async Task<List<PageVisit>> GetPageVisitsByRunAsync(Guid runSessionId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM PageVisit WHERE RunSessionId = @Id ORDER BY Timestamp;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", runSessionId.ToString());
        return await ReadListAsync(cmd, ReadPageVisit, ct);
    }

    public async Task<List<PageVisit>> GetPageVisitsByUrlAsync(string url, int limit = 100, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM PageVisit WHERE Url = @Url ORDER BY Timestamp DESC LIMIT @Limit;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Url", url);
        cmd.Parameters.AddWithValue("@Limit", limit);
        return await ReadListAsync(cmd, ReadPageVisit, ct);
    }

    public async Task<List<PageVisit>> GetSlowestPagesAsync(Guid runSessionId, int top = 10, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM PageVisit WHERE RunSessionId = @Id ORDER BY TotalTimeMs DESC LIMIT @Top;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", runSessionId.ToString());
        cmd.Parameters.AddWithValue("@Top", top);
        return await ReadListAsync(cmd, ReadPageVisit, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  NetworkRequest
    // ────────────────────────────────────────────────────────────

    public async Task InsertNetworkRequestAsync(NetworkRequest nr, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct);
        await InsertNetworkRequestCoreAsync(conn, nr, ct);
    }

    public async Task InsertNetworkRequestsAsync(IEnumerable<NetworkRequest> requests, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(ct);
        await using var tx = conn.BeginTransaction();
        foreach (var nr in requests)
            await InsertNetworkRequestCoreAsync(conn, nr, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<List<NetworkRequest>> GetNetworkRequestsAsync(Guid pageVisitId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM NetworkRequest WHERE PageVisitId = @Id ORDER BY Timestamp;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", pageVisitId.ToString());
        return await ReadListAsync(cmd, ReadNetworkRequest, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  DetectedIssue
    // ────────────────────────────────────────────────────────────

    public async Task InsertDetectedIssueAsync(DetectedIssue di, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO DetectedIssue (Id, PageVisitId, RunSessionId, Severity, Category,
                Title, Description, MetricName, ActualValue, ThresholdValue, Confidence, Url, Timestamp)
            VALUES (@Id, @PageVisitId, @RunSessionId, @Severity, @Category,
                @Title, @Description, @MetricName, @ActualValue, @ThresholdValue, @Confidence, @Url, @Timestamp);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", di.Id.ToString());
        cmd.Parameters.AddWithValue("@PageVisitId", di.PageVisitId.ToString());
        cmd.Parameters.AddWithValue("@RunSessionId", di.RunSessionId.ToString());
        cmd.Parameters.AddWithValue("@Severity", (int)di.Severity);
        cmd.Parameters.AddWithValue("@Category", (int)di.Category);
        cmd.Parameters.AddWithValue("@Title", di.Title);
        cmd.Parameters.AddWithValue("@Description", di.Description);
        cmd.Parameters.AddWithValue("@MetricName", di.MetricName);
        cmd.Parameters.AddWithValue("@ActualValue", di.ActualValue);
        cmd.Parameters.AddWithValue("@ThresholdValue", di.ThresholdValue);
        cmd.Parameters.AddWithValue("@Confidence", di.Confidence);
        cmd.Parameters.AddWithValue("@Url", di.Url);
        cmd.Parameters.AddWithValue("@Timestamp", di.Timestamp.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<DetectedIssue>> GetIssuesByRunAsync(Guid runSessionId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM DetectedIssue WHERE RunSessionId = @Id ORDER BY Severity DESC, Timestamp;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", runSessionId.ToString());
        return await ReadListAsync(cmd, ReadDetectedIssue, ct);
    }

    public async Task<List<DetectedIssue>> GetIssuesByUrlAsync(string url, int limit = 100, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM DetectedIssue WHERE Url = @Url ORDER BY Timestamp DESC LIMIT @Limit;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Url", url);
        cmd.Parameters.AddWithValue("@Limit", limit);
        return await ReadListAsync(cmd, ReadDetectedIssue, ct);
    }

    public async Task<List<DetectedIssue>> GetIssuesBySeverityAsync(Severity severity, int limit = 100, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM DetectedIssue WHERE Severity = @Severity ORDER BY Timestamp DESC LIMIT @Limit;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Severity", (int)severity);
        cmd.Parameters.AddWithValue("@Limit", limit);
        return await ReadListAsync(cmd, ReadDetectedIssue, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  EvidenceItem
    // ────────────────────────────────────────────────────────────

    public async Task InsertEvidenceItemAsync(EvidenceItem ei, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO EvidenceItem (Id, PageVisitId, Type, FilePath, Description, FileSizeBytes, CreatedAt)
            VALUES (@Id, @PageVisitId, @Type, @FilePath, @Description, @FileSizeBytes, @CreatedAt);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", ei.Id.ToString());
        cmd.Parameters.AddWithValue("@PageVisitId", ei.PageVisitId.ToString());
        cmd.Parameters.AddWithValue("@Type", (int)ei.Type);
        cmd.Parameters.AddWithValue("@FilePath", ei.FilePath);
        cmd.Parameters.AddWithValue("@Description", (object?)ei.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FileSizeBytes", ei.FileSizeBytes);
        cmd.Parameters.AddWithValue("@CreatedAt", ei.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<EvidenceItem>> GetEvidenceItemsAsync(Guid pageVisitId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM EvidenceItem WHERE PageVisitId = @Id ORDER BY CreatedAt;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", pageVisitId.ToString());
        return await ReadListAsync(cmd, ReadEvidenceItem, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  DailySummary
    // ────────────────────────────────────────────────────────────

    public async Task UpsertDailySummaryAsync(DailySummary ds, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO DailySummary (Id, UrlSetId, Date, TotalRuns, TotalPageVisits,
                AvgResponseTimeMs, P95ResponseTimeMs, MaxResponseTimeMs, MinResponseTimeMs,
                TotalIssues, CriticalIssues, WarningIssues, AvailabilityPercent, TotalDataTransferredBytes)
            VALUES (@Id, @UrlSetId, @Date, @TotalRuns, @TotalPageVisits,
                @AvgResponseTimeMs, @P95ResponseTimeMs, @MaxResponseTimeMs, @MinResponseTimeMs,
                @TotalIssues, @CriticalIssues, @WarningIssues, @AvailabilityPercent, @TotalDataTransferredBytes)
            ON CONFLICT(UrlSetId, Date) DO UPDATE SET
                TotalRuns = @TotalRuns, TotalPageVisits = @TotalPageVisits,
                AvgResponseTimeMs = @AvgResponseTimeMs, P95ResponseTimeMs = @P95ResponseTimeMs,
                MaxResponseTimeMs = @MaxResponseTimeMs, MinResponseTimeMs = @MinResponseTimeMs,
                TotalIssues = @TotalIssues, CriticalIssues = @CriticalIssues,
                WarningIssues = @WarningIssues, AvailabilityPercent = @AvailabilityPercent,
                TotalDataTransferredBytes = @TotalDataTransferredBytes;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", ds.Id.ToString());
        cmd.Parameters.AddWithValue("@UrlSetId", ds.UrlSetId.ToString());
        cmd.Parameters.AddWithValue("@Date", ds.Date.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@TotalRuns", ds.TotalRuns);
        cmd.Parameters.AddWithValue("@TotalPageVisits", ds.TotalPageVisits);
        cmd.Parameters.AddWithValue("@AvgResponseTimeMs", ds.AvgResponseTimeMs);
        cmd.Parameters.AddWithValue("@P95ResponseTimeMs", ds.P95ResponseTimeMs);
        cmd.Parameters.AddWithValue("@MaxResponseTimeMs", ds.MaxResponseTimeMs);
        cmd.Parameters.AddWithValue("@MinResponseTimeMs", ds.MinResponseTimeMs);
        cmd.Parameters.AddWithValue("@TotalIssues", ds.TotalIssues);
        cmd.Parameters.AddWithValue("@CriticalIssues", ds.CriticalIssues);
        cmd.Parameters.AddWithValue("@WarningIssues", ds.WarningIssues);
        cmd.Parameters.AddWithValue("@AvailabilityPercent", ds.AvailabilityPercent);
        cmd.Parameters.AddWithValue("@TotalDataTransferredBytes", ds.TotalDataTransferredBytes);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<DailySummary>> GetDailySummariesAsync(Guid urlSetId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM DailySummary
            WHERE UrlSetId = @UrlSetId AND Date >= @From AND Date <= @To
            ORDER BY Date DESC;
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@UrlSetId", urlSetId.ToString());
        cmd.Parameters.AddWithValue("@From", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@To", to.ToString("yyyy-MM-dd"));
        return await ReadListAsync(cmd, ReadDailySummary, ct);
    }

    public async Task<DailySummary?> GetLatestDailySummaryAsync(Guid urlSetId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM DailySummary WHERE UrlSetId = @UrlSetId ORDER BY Date DESC LIMIT 1;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@UrlSetId", urlSetId.ToString());
        await using var r = await cmd.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? ReadDailySummary(r) : null;
    }

    // ────────────────────────────────────────────────────────────
    //  BaselineComparison
    // ────────────────────────────────────────────────────────────

    public async Task InsertBaselineComparisonAsync(BaselineComparison bc, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO BaselineComparison (Id, Url, RunSessionId, BaselineRunSessionId,
                CurrentAvgMs, CurrentP95Ms, CurrentMaxMs, CurrentStatusCode,
                BaselineAvgMs, BaselineP95Ms, BaselineMaxMs, BaselineStatusCode,
                DeltaMs, DeltaPercent, Trend, CreatedAt)
            VALUES (@Id, @Url, @RunSessionId, @BaselineRunSessionId,
                @CurrentAvgMs, @CurrentP95Ms, @CurrentMaxMs, @CurrentStatusCode,
                @BaselineAvgMs, @BaselineP95Ms, @BaselineMaxMs, @BaselineStatusCode,
                @DeltaMs, @DeltaPercent, @Trend, @CreatedAt);
            """;
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", bc.Id.ToString());
        cmd.Parameters.AddWithValue("@Url", bc.Url);
        cmd.Parameters.AddWithValue("@RunSessionId", bc.RunSessionId.ToString());
        cmd.Parameters.AddWithValue("@BaselineRunSessionId", bc.BaselineRunSessionId.HasValue ? bc.BaselineRunSessionId.Value.ToString() : DBNull.Value);
        cmd.Parameters.AddWithValue("@CurrentAvgMs", bc.CurrentAvgMs);
        cmd.Parameters.AddWithValue("@CurrentP95Ms", bc.CurrentP95Ms);
        cmd.Parameters.AddWithValue("@CurrentMaxMs", bc.CurrentMaxMs);
        cmd.Parameters.AddWithValue("@CurrentStatusCode", bc.CurrentStatusCode);
        cmd.Parameters.AddWithValue("@BaselineAvgMs", bc.BaselineAvgMs);
        cmd.Parameters.AddWithValue("@BaselineP95Ms", bc.BaselineP95Ms);
        cmd.Parameters.AddWithValue("@BaselineMaxMs", bc.BaselineMaxMs);
        cmd.Parameters.AddWithValue("@BaselineStatusCode", bc.BaselineStatusCode);
        cmd.Parameters.AddWithValue("@DeltaMs", bc.DeltaMs);
        cmd.Parameters.AddWithValue("@DeltaPercent", bc.DeltaPercent);
        cmd.Parameters.AddWithValue("@Trend", (int)bc.Trend);
        cmd.Parameters.AddWithValue("@CreatedAt", bc.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<BaselineComparison>> GetComparisonsAsync(Guid runSessionId, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM BaselineComparison WHERE RunSessionId = @Id ORDER BY DeltaPercent DESC;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", runSessionId.ToString());
        return await ReadListAsync(cmd, ReadBaselineComparison, ct);
    }

    public async Task<List<BaselineComparison>> GetComparisonsByUrlAsync(string url, int limit = 50, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM BaselineComparison WHERE Url = @Url ORDER BY CreatedAt DESC LIMIT @Limit;";
        await using var conn = await OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Url", url);
        cmd.Parameters.AddWithValue("@Limit", limit);
        return await ReadListAsync(cmd, ReadBaselineComparison, ct);
    }

    // ────────────────────────────────────────────────────────────
    //  Private helpers
    // ────────────────────────────────────────────────────────────

    private async Task<SqliteConnection> OpenAsync(CancellationToken ct)
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<List<T>> ReadListAsync<T>(SqliteCommand cmd, Func<SqliteDataReader, T> reader, CancellationToken ct)
    {
        var list = new List<T>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            list.Add(reader(r));
        return list;
    }

    private async Task InsertNetworkRequestCoreAsync(SqliteConnection conn, NetworkRequest nr, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO NetworkRequest (Id, PageVisitId, Url, Method, StatusCode,
                TimeToFirstByteMs, TotalTimeMs, ContentDownloadMs, ContentType, ContentLength,
                RequestHeadersJson, ResponseHeadersJson, RedirectChainJson, ErrorMessage, Timestamp)
            VALUES (@Id, @PageVisitId, @Url, @Method, @StatusCode,
                @TimeToFirstByteMs, @TotalTimeMs, @ContentDownloadMs, @ContentType, @ContentLength,
                @RequestHeadersJson, @ResponseHeadersJson, @RedirectChainJson, @ErrorMessage, @Timestamp);
            """;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Id", nr.Id.ToString());
        cmd.Parameters.AddWithValue("@PageVisitId", nr.PageVisitId.ToString());
        cmd.Parameters.AddWithValue("@Url", nr.Url);
        cmd.Parameters.AddWithValue("@Method", nr.Method);
        cmd.Parameters.AddWithValue("@StatusCode", nr.StatusCode);
        cmd.Parameters.AddWithValue("@TimeToFirstByteMs", nr.TimeToFirstByteMs);
        cmd.Parameters.AddWithValue("@TotalTimeMs", nr.TotalTimeMs);
        cmd.Parameters.AddWithValue("@ContentDownloadMs", nr.ContentDownloadMs);
        cmd.Parameters.AddWithValue("@ContentType", (object?)nr.ContentType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ContentLength", nr.ContentLength);
        cmd.Parameters.AddWithValue("@RequestHeadersJson", (object?)nr.RequestHeadersJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ResponseHeadersJson", (object?)nr.ResponseHeadersJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RedirectChainJson", (object?)nr.RedirectChainJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)nr.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Timestamp", nr.Timestamp.ToString("O"));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // ── Row readers ─────────────────────────────────────────────

    private static void AddRunSessionParams(SqliteCommand cmd, RunSession s)
    {
        cmd.Parameters.AddWithValue("@Id", s.Id.ToString());
        cmd.Parameters.AddWithValue("@Name", (object?)s.Name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@StartedAt", s.StartedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@CompletedAt", s.CompletedAt.HasValue ? s.CompletedAt.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("@Status", (int)s.Status);
        cmd.Parameters.AddWithValue("@TotalUrls", s.TotalUrls);
        cmd.Parameters.AddWithValue("@SuccessCount", s.SuccessCount);
        cmd.Parameters.AddWithValue("@FailureCount", s.FailureCount);
        cmd.Parameters.AddWithValue("@WarningCount", s.WarningCount);
        cmd.Parameters.AddWithValue("@CriticalCount", s.CriticalCount);
        cmd.Parameters.AddWithValue("@AverageResponseTimeMs", s.AverageResponseTimeMs);
        cmd.Parameters.AddWithValue("@MaxResponseTimeMs", s.MaxResponseTimeMs);
        cmd.Parameters.AddWithValue("@TotalDurationMs", s.TotalDurationMs);
        cmd.Parameters.AddWithValue("@ReportPath", (object?)s.ReportPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)s.Notes ?? DBNull.Value);
    }

    private static RunSession ReadRunSession(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Name = r.IsDBNull(r.GetOrdinal("Name")) ? null : r.GetString(r.GetOrdinal("Name")),
        StartedAt = DateTime.Parse(r.GetString(r.GetOrdinal("StartedAt"))),
        CompletedAt = r.IsDBNull(r.GetOrdinal("CompletedAt")) ? null : DateTime.Parse(r.GetString(r.GetOrdinal("CompletedAt"))),
        Status = (MonitorStatus)r.GetInt32(r.GetOrdinal("Status")),
        TotalUrls = r.GetInt32(r.GetOrdinal("TotalUrls")),
        SuccessCount = r.GetInt32(r.GetOrdinal("SuccessCount")),
        FailureCount = r.GetInt32(r.GetOrdinal("FailureCount")),
        WarningCount = r.GetInt32(r.GetOrdinal("WarningCount")),
        CriticalCount = r.GetInt32(r.GetOrdinal("CriticalCount")),
        AverageResponseTimeMs = r.GetDouble(r.GetOrdinal("AverageResponseTimeMs")),
        MaxResponseTimeMs = r.GetDouble(r.GetOrdinal("MaxResponseTimeMs")),
        TotalDurationMs = r.GetDouble(r.GetOrdinal("TotalDurationMs")),
        ReportPath = r.IsDBNull(r.GetOrdinal("ReportPath")) ? null : r.GetString(r.GetOrdinal("ReportPath")),
        Notes = r.IsDBNull(r.GetOrdinal("Notes")) ? null : r.GetString(r.GetOrdinal("Notes")),
    };

    private static UrlSet ReadUrlSet(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Name = r.GetString(r.GetOrdinal("Name")),
        BaseUrl = r.GetString(r.GetOrdinal("BaseUrl")),
        IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
        CheckIntervalSeconds = r.GetInt32(r.GetOrdinal("CheckIntervalSeconds")),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
        UpdatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("UpdatedAt"))),
    };

    private static UrlSetEntry ReadUrlSetEntry(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        UrlSetId = Guid.Parse(r.GetString(r.GetOrdinal("UrlSetId"))),
        Url = r.GetString(r.GetOrdinal("Url")),
        Label = r.IsDBNull(r.GetOrdinal("Label")) ? null : r.GetString(r.GetOrdinal("Label")),
        IsDiscovered = r.GetInt32(r.GetOrdinal("IsDiscovered")) == 1,
        IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
    };

    private static KeywordSet ReadKeywordSet(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        UrlSetId = Guid.Parse(r.GetString(r.GetOrdinal("UrlSetId"))),
        Name = r.GetString(r.GetOrdinal("Name")),
        KeywordsCsv = r.GetString(r.GetOrdinal("KeywordsCsv")),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
    };

    private static Scenario ReadScenario(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Name = r.GetString(r.GetOrdinal("Name")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        UrlSetId = Guid.Parse(r.GetString(r.GetOrdinal("UrlSetId"))),
        StepsJson = r.GetString(r.GetOrdinal("StepsJson")),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
        UpdatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("UpdatedAt"))),
        IsActive = r.GetInt32(r.GetOrdinal("IsActive")) == 1,
    };

    private static ScenarioExecution ReadScenarioExecution(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        RunSessionId = Guid.Parse(r.GetString(r.GetOrdinal("RunSessionId"))),
        ScenarioId = r.IsDBNull(r.GetOrdinal("ScenarioId")) ? null : Guid.Parse(r.GetString(r.GetOrdinal("ScenarioId"))),
        UrlSetId = Guid.Parse(r.GetString(r.GetOrdinal("UrlSetId"))),
        StartedAt = DateTime.Parse(r.GetString(r.GetOrdinal("StartedAt"))),
        CompletedAt = r.IsDBNull(r.GetOrdinal("CompletedAt")) ? null : DateTime.Parse(r.GetString(r.GetOrdinal("CompletedAt"))),
        DurationMs = r.GetDouble(r.GetOrdinal("DurationMs")),
        Status = (MonitorStatus)r.GetInt32(r.GetOrdinal("Status")),
        TotalPages = r.GetInt32(r.GetOrdinal("TotalPages")),
        PagesCompleted = r.GetInt32(r.GetOrdinal("PagesCompleted")),
        ErrorMessage = r.IsDBNull(r.GetOrdinal("ErrorMessage")) ? null : r.GetString(r.GetOrdinal("ErrorMessage")),
    };

    private static PageVisit ReadPageVisit(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        ScenarioExecutionId = Guid.Parse(r.GetString(r.GetOrdinal("ScenarioExecutionId"))),
        RunSessionId = Guid.Parse(r.GetString(r.GetOrdinal("RunSessionId"))),
        Url = r.GetString(r.GetOrdinal("Url")),
        StatusCode = r.GetInt32(r.GetOrdinal("StatusCode")),
        TimeToFirstByteMs = r.GetDouble(r.GetOrdinal("TimeToFirstByteMs")),
        ContentDownloadMs = r.GetDouble(r.GetOrdinal("ContentDownloadMs")),
        TotalTimeMs = r.GetDouble(r.GetOrdinal("TotalTimeMs")),
        ContentLength = r.GetInt64(r.GetOrdinal("ContentLength")),
        ContentType = r.IsDBNull(r.GetOrdinal("ContentType")) ? null : r.GetString(r.GetOrdinal("ContentType")),
        Success = r.GetInt32(r.GetOrdinal("Success")) == 1,
        ErrorMessage = r.IsDBNull(r.GetOrdinal("ErrorMessage")) ? null : r.GetString(r.GetOrdinal("ErrorMessage")),
        Timestamp = DateTime.Parse(r.GetString(r.GetOrdinal("Timestamp"))),
        DurationMs = r.GetDouble(r.GetOrdinal("DurationMs")),
    };

    private static NetworkRequest ReadNetworkRequest(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        PageVisitId = Guid.Parse(r.GetString(r.GetOrdinal("PageVisitId"))),
        Url = r.GetString(r.GetOrdinal("Url")),
        Method = r.GetString(r.GetOrdinal("Method")),
        StatusCode = r.GetInt32(r.GetOrdinal("StatusCode")),
        TimeToFirstByteMs = r.GetDouble(r.GetOrdinal("TimeToFirstByteMs")),
        TotalTimeMs = r.GetDouble(r.GetOrdinal("TotalTimeMs")),
        ContentDownloadMs = r.GetDouble(r.GetOrdinal("ContentDownloadMs")),
        ContentType = r.IsDBNull(r.GetOrdinal("ContentType")) ? null : r.GetString(r.GetOrdinal("ContentType")),
        ContentLength = r.GetInt64(r.GetOrdinal("ContentLength")),
        RequestHeadersJson = r.IsDBNull(r.GetOrdinal("RequestHeadersJson")) ? null : r.GetString(r.GetOrdinal("RequestHeadersJson")),
        ResponseHeadersJson = r.IsDBNull(r.GetOrdinal("ResponseHeadersJson")) ? null : r.GetString(r.GetOrdinal("ResponseHeadersJson")),
        RedirectChainJson = r.IsDBNull(r.GetOrdinal("RedirectChainJson")) ? null : r.GetString(r.GetOrdinal("RedirectChainJson")),
        ErrorMessage = r.IsDBNull(r.GetOrdinal("ErrorMessage")) ? null : r.GetString(r.GetOrdinal("ErrorMessage")),
        Timestamp = DateTime.Parse(r.GetString(r.GetOrdinal("Timestamp"))),
    };

    private static DetectedIssue ReadDetectedIssue(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        PageVisitId = Guid.Parse(r.GetString(r.GetOrdinal("PageVisitId"))),
        RunSessionId = Guid.Parse(r.GetString(r.GetOrdinal("RunSessionId"))),
        Severity = (Severity)r.GetInt32(r.GetOrdinal("Severity")),
        Category = (IssueCategory)r.GetInt32(r.GetOrdinal("Category")),
        Title = r.GetString(r.GetOrdinal("Title")),
        Description = r.GetString(r.GetOrdinal("Description")),
        MetricName = r.GetString(r.GetOrdinal("MetricName")),
        ActualValue = r.GetDouble(r.GetOrdinal("ActualValue")),
        ThresholdValue = r.GetDouble(r.GetOrdinal("ThresholdValue")),
        Confidence = r.GetDouble(r.GetOrdinal("Confidence")),
        Url = r.GetString(r.GetOrdinal("Url")),
        Timestamp = DateTime.Parse(r.GetString(r.GetOrdinal("Timestamp"))),
    };

    private static EvidenceItem ReadEvidenceItem(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        PageVisitId = Guid.Parse(r.GetString(r.GetOrdinal("PageVisitId"))),
        Type = (EvidenceType)r.GetInt32(r.GetOrdinal("Type")),
        FilePath = r.GetString(r.GetOrdinal("FilePath")),
        Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")),
        FileSizeBytes = r.GetInt64(r.GetOrdinal("FileSizeBytes")),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
    };

    private static DailySummary ReadDailySummary(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        UrlSetId = Guid.Parse(r.GetString(r.GetOrdinal("UrlSetId"))),
        Date = DateTime.Parse(r.GetString(r.GetOrdinal("Date"))),
        TotalRuns = r.GetInt32(r.GetOrdinal("TotalRuns")),
        TotalPageVisits = r.GetInt32(r.GetOrdinal("TotalPageVisits")),
        AvgResponseTimeMs = r.GetDouble(r.GetOrdinal("AvgResponseTimeMs")),
        P95ResponseTimeMs = r.GetDouble(r.GetOrdinal("P95ResponseTimeMs")),
        MaxResponseTimeMs = r.GetDouble(r.GetOrdinal("MaxResponseTimeMs")),
        MinResponseTimeMs = r.GetDouble(r.GetOrdinal("MinResponseTimeMs")),
        TotalIssues = r.GetInt32(r.GetOrdinal("TotalIssues")),
        CriticalIssues = r.GetInt32(r.GetOrdinal("CriticalIssues")),
        WarningIssues = r.GetInt32(r.GetOrdinal("WarningIssues")),
        AvailabilityPercent = r.GetDouble(r.GetOrdinal("AvailabilityPercent")),
        TotalDataTransferredBytes = r.GetInt64(r.GetOrdinal("TotalDataTransferredBytes")),
    };

    private static BaselineComparison ReadBaselineComparison(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(r.GetOrdinal("Id"))),
        Url = r.GetString(r.GetOrdinal("Url")),
        RunSessionId = Guid.Parse(r.GetString(r.GetOrdinal("RunSessionId"))),
        BaselineRunSessionId = r.IsDBNull(r.GetOrdinal("BaselineRunSessionId")) ? null : Guid.Parse(r.GetString(r.GetOrdinal("BaselineRunSessionId"))),
        CurrentAvgMs = r.GetDouble(r.GetOrdinal("CurrentAvgMs")),
        CurrentP95Ms = r.GetDouble(r.GetOrdinal("CurrentP95Ms")),
        CurrentMaxMs = r.GetDouble(r.GetOrdinal("CurrentMaxMs")),
        CurrentStatusCode = r.GetInt32(r.GetOrdinal("CurrentStatusCode")),
        BaselineAvgMs = r.GetDouble(r.GetOrdinal("BaselineAvgMs")),
        BaselineP95Ms = r.GetDouble(r.GetOrdinal("BaselineP95Ms")),
        BaselineMaxMs = r.GetDouble(r.GetOrdinal("BaselineMaxMs")),
        BaselineStatusCode = r.GetInt32(r.GetOrdinal("BaselineStatusCode")),
        DeltaMs = r.GetDouble(r.GetOrdinal("DeltaMs")),
        DeltaPercent = r.GetDouble(r.GetOrdinal("DeltaPercent")),
        Trend = (TrendDirection)r.GetInt32(r.GetOrdinal("Trend")),
        CreatedAt = DateTime.Parse(r.GetString(r.GetOrdinal("CreatedAt"))),
    };

    public void Dispose() { /* connections are per-call, nothing to dispose */ }
}
