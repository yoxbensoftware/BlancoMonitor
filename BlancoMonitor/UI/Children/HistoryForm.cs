using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class HistoryForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly DataGridView _sessionsGrid;
    private readonly DataGridView _detailGrid;
    private readonly NeonRichTextLog _summaryLog;
    private readonly Label _statsLabel;

    public HistoryForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "History";
        Size = new Size(980, 650);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(45, 30);

        var titleLabel = NeonTheme.CreateLabel("▸ RUN HISTORY", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        var refreshBtn = NeonTheme.CreateButton("↻ REFRESH", 110, 30);
        refreshBtn.Location = new Point(840, 12);
        refreshBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        refreshBtn.Click += async (_, _) => await LoadHistory();

        // Stats
        _statsLabel = new Label
        {
            Text = "Total Runs: — | Last Run: —",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 48),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Sessions grid
        var sessLabel = NeonTheme.CreateLabel("SESSIONS", isHeader: false);
        sessLabel.ForeColor = NeonTheme.TextAccent;
        sessLabel.Location = new Point(20, 70);

        _sessionsGrid = new DataGridView
        {
            Location = new Point(20, 95),
            Size = new Size(930, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_sessionsGrid);
        _sessionsGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Started", HeaderText = "STARTED", FillWeight = 18 },
            new DataGridViewTextBoxColumn { Name = "Completed", HeaderText = "COMPLETED", FillWeight = 15 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "TotalUrls", HeaderText = "URLS", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Success", HeaderText = "OK", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Failures", HeaderText = "FAIL", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Warnings", HeaderText = "WARN", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Critical", HeaderText = "CRIT", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "DURATION", FillWeight = 12 },
        ]);
        _sessionsGrid.SelectionChanged += SessionsGrid_SelectionChanged;

        // Detail grid (pages for selected session)
        var detailLabel = NeonTheme.CreateLabel("▸ SESSION PAGES", isHeader: false);
        detailLabel.ForeColor = NeonTheme.TextAccent;
        detailLabel.Location = new Point(20, 305);

        _detailGrid = new DataGridView
        {
            Location = new Point(20, 330),
            Size = new Size(930, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_detailGrid);
        _detailGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "StatusCode", HeaderText = "HTTP", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "TTFB", HeaderText = "TTFB(ms)", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "TotalTime", HeaderText = "TOTAL(ms)", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "ContentSize", HeaderText = "SIZE", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Success", HeaderText = "OK", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "ContentType", HeaderText = "TYPE", FillWeight = 13 },
        ]);

        // Summary log
        var summaryLabel = NeonTheme.CreateLabel("▸ SESSION SUMMARY", isHeader: false);
        summaryLabel.ForeColor = NeonTheme.TextAccent;
        summaryLabel.Location = new Point(20, 480);

        _summaryLog = new NeonRichTextLog
        {
            Location = new Point(20, 505),
            Size = new Size(930, 105),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, refreshBtn, _statsLabel, sessLabel, _sessionsGrid, detailLabel, _detailGrid, summaryLabel, _summaryLog]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await LoadHistory();
    }

    private async Task LoadHistory()
    {
        try
        {
            var totalRuns = await _database.GetTotalRunCountAsync();
            var latestRun = await _database.GetLatestRunSessionAsync();
            _statsLabel.Text = $"Total Runs: {totalRuns} | Last Run: {latestRun?.StartedAt.ToString("yyyy-MM-dd HH:mm") ?? "—"}";

            var sessions = await _database.GetRunSessionsAsync(limit: 50);
            _sessionsGrid.Rows.Clear();

            foreach (var s in sessions)
            {
                var duration = s.CompletedAt.HasValue
                    ? (s.CompletedAt.Value - s.StartedAt).ToString(@"mm\:ss")
                    : "—";

                _sessionsGrid.Rows.Add(
                    s.StartedAt.ToString("yyyy-MM-dd HH:mm"),
                    s.CompletedAt?.ToString("HH:mm:ss") ?? "—",
                    s.Status.ToString(),
                    s.TotalUrls.ToString(),
                    s.SuccessCount.ToString(),
                    s.FailureCount.ToString(),
                    s.WarningCount.ToString(),
                    s.CriticalCount.ToString(),
                    duration);
            }

            _sessionsGrid.Tag = sessions;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load history", ex);
        }
    }

    private async void SessionsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_sessionsGrid.Tag is not List<Domain.Entities.RunSession> sessions) return;
        if (_sessionsGrid.CurrentRow is null) return;

        var idx = _sessionsGrid.CurrentRow.Index;
        if (idx < 0 || idx >= sessions.Count) return;

        var session = sessions[idx];

        try
        {
            // Load pages
            _detailGrid.Rows.Clear();
            var pages = await _database.GetPageVisitsByRunAsync(session.Id);
            foreach (var p in pages)
            {
                _detailGrid.Rows.Add(
                    p.Url,
                    p.StatusCode.ToString(),
                    p.TimeToFirstByteMs.ToString("F0"),
                    p.TotalTimeMs.ToString("F0"),
                    FormatSize(p.ContentLength),
                    p.Success ? "OK" : "FAIL",
                    p.ContentType ?? "—");
            }

            // Load issues summary
            _summaryLog.Clear();
            var issues = await _database.GetIssuesByRunAsync(session.Id);
            var critCount = issues.Count(i => i.Severity == Severity.Critical);
            var warnCount = issues.Count(i => i.Severity == Severity.Warning);

            _summaryLog.AppendLog($"Session: {session.StartedAt:yyyy-MM-dd HH:mm} — {session.Status}", NeonTheme.TextAccent);
            _summaryLog.AppendLog($"Pages: {pages.Count} | Issues: {issues.Count} (Critical: {critCount}, Warning: {warnCount})");

            if (critCount > 0)
                _summaryLog.AppendLog($"⚠ {critCount} CRITICAL issue(s) detected!", NeonTheme.Critical);

            var slowest = pages.OrderByDescending(p => p.TotalTimeMs).FirstOrDefault();
            if (slowest is not null)
                _summaryLog.AppendLog($"Slowest: {slowest.Url} ({slowest.TotalTimeMs:F0}ms)", NeonTheme.Warning);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load session details", ex);
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
