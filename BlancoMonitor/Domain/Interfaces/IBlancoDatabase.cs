using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;

namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Unified database abstraction for all BlancoMonitor persistence.
/// Implementations handle connection management, schema init, and CRUD.
/// </summary>
public interface IBlancoDatabase : IDisposable
{
    Task InitializeAsync(CancellationToken ct = default);

    // ── RunSession ──────────────────────────────────────────────
    Task<Guid> InsertRunSessionAsync(RunSession session, CancellationToken ct = default);
    Task UpdateRunSessionAsync(RunSession session, CancellationToken ct = default);
    Task<RunSession?> GetRunSessionAsync(Guid id, CancellationToken ct = default);
    Task<List<RunSession>> GetRunSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default);

    // ── UrlSet + Entries + Keywords ─────────────────────────────
    Task<Guid> InsertUrlSetAsync(UrlSet urlSet, CancellationToken ct = default);
    Task UpdateUrlSetAsync(UrlSet urlSet, CancellationToken ct = default);
    Task<List<UrlSet>> GetUrlSetsAsync(CancellationToken ct = default);
    Task<UrlSet?> GetUrlSetAsync(Guid id, CancellationToken ct = default);
    Task DeleteUrlSetAsync(Guid id, CancellationToken ct = default);

    Task InsertUrlSetEntryAsync(UrlSetEntry entry, CancellationToken ct = default);
    Task<List<UrlSetEntry>> GetUrlSetEntriesAsync(Guid urlSetId, CancellationToken ct = default);

    Task InsertKeywordSetAsync(KeywordSet keywordSet, CancellationToken ct = default);
    Task<List<KeywordSet>> GetKeywordSetsAsync(Guid urlSetId, CancellationToken ct = default);

    // ── Scenario ────────────────────────────────────────────────
    Task<Guid> InsertScenarioAsync(Scenario scenario, CancellationToken ct = default);
    Task UpdateScenarioAsync(Scenario scenario, CancellationToken ct = default);
    Task<List<Scenario>> GetScenariosAsync(Guid? urlSetId = null, CancellationToken ct = default);

    // ── ScenarioExecution ───────────────────────────────────────
    Task<Guid> InsertScenarioExecutionAsync(ScenarioExecution exec, CancellationToken ct = default);
    Task UpdateScenarioExecutionAsync(ScenarioExecution exec, CancellationToken ct = default);
    Task<List<ScenarioExecution>> GetExecutionsAsync(Guid runSessionId, CancellationToken ct = default);

    // ── PageVisit ───────────────────────────────────────────────
    Task InsertPageVisitAsync(PageVisit visit, CancellationToken ct = default);
    Task<List<PageVisit>> GetPageVisitsAsync(Guid scenarioExecutionId, CancellationToken ct = default);
    Task<List<PageVisit>> GetPageVisitsByRunAsync(Guid runSessionId, CancellationToken ct = default);
    Task<List<PageVisit>> GetPageVisitsByUrlAsync(string url, int limit = 100, CancellationToken ct = default);

    // ── NetworkRequest ──────────────────────────────────────────
    Task InsertNetworkRequestAsync(NetworkRequest request, CancellationToken ct = default);
    Task InsertNetworkRequestsAsync(IEnumerable<NetworkRequest> requests, CancellationToken ct = default);
    Task<List<NetworkRequest>> GetNetworkRequestsAsync(Guid pageVisitId, CancellationToken ct = default);

    // ── DetectedIssue ───────────────────────────────────────────
    Task InsertDetectedIssueAsync(DetectedIssue issue, CancellationToken ct = default);
    Task<List<DetectedIssue>> GetIssuesByRunAsync(Guid runSessionId, CancellationToken ct = default);
    Task<List<DetectedIssue>> GetIssuesByUrlAsync(string url, int limit = 100, CancellationToken ct = default);
    Task<List<DetectedIssue>> GetIssuesBySeverityAsync(Severity severity, int limit = 100, CancellationToken ct = default);

    // ── EvidenceItem ────────────────────────────────────────────
    Task InsertEvidenceItemAsync(EvidenceItem item, CancellationToken ct = default);
    Task<List<EvidenceItem>> GetEvidenceItemsAsync(Guid pageVisitId, CancellationToken ct = default);

    // ── DailySummary ────────────────────────────────────────────
    Task UpsertDailySummaryAsync(DailySummary summary, CancellationToken ct = default);
    Task<List<DailySummary>> GetDailySummariesAsync(Guid urlSetId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<DailySummary?> GetLatestDailySummaryAsync(Guid urlSetId, CancellationToken ct = default);

    // ── BaselineComparison ──────────────────────────────────────
    Task InsertBaselineComparisonAsync(BaselineComparison comparison, CancellationToken ct = default);
    Task<List<BaselineComparison>> GetComparisonsAsync(Guid runSessionId, CancellationToken ct = default);
    Task<List<BaselineComparison>> GetComparisonsByUrlAsync(string url, int limit = 50, CancellationToken ct = default);

    // ── Reporting queries ───────────────────────────────────────
    Task<List<PageVisit>> GetSlowestPagesAsync(Guid runSessionId, int top = 10, CancellationToken ct = default);
    Task<int> GetTotalRunCountAsync(CancellationToken ct = default);
    Task<RunSession?> GetLatestRunSessionAsync(CancellationToken ct = default);
}
