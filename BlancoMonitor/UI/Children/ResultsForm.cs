using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class ResultsForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;

    private readonly DataGridView _sessionsGrid;
    private readonly DataGridView _pagesGrid;
    private readonly NeonRichTextLog _detailView;
    private readonly Button _refreshButton;
    private readonly Button _openReportButton;
    private readonly Label _summaryLabel;

    private RunSession? _selectedSession;

    public ResultsForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "Results";
        Size = new Size(1100, 700);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(40, 40);

        var titleLabel = NeonTheme.CreateLabel("▸ MONITORING RESULTS", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        _refreshButton = NeonTheme.CreateButton("↻ REFRESH", 120, 30);
        _refreshButton.Location = new Point(850, 12);
        _refreshButton.Click += async (_, _) => await LoadSessionsAsync();

        _openReportButton = NeonTheme.CreateButton("📄 OPEN REPORT", 150, 30);
        _openReportButton.Location = new Point(690, 12);
        _openReportButton.Enabled = false;
        _openReportButton.Click += OpenReport_Click;

        // Summary bar
        _summaryLabel = new Label
        {
            Text = "No data — run a monitoring session first.",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 48),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Sessions grid (top)
        var sessionsLabel = NeonTheme.CreateLabel("▸ RUN SESSIONS", isHeader: false);
        sessionsLabel.ForeColor = NeonTheme.TextAccent;
        sessionsLabel.Location = new Point(20, 68);

        _sessionsGrid = new DataGridView
        {
            Location = new Point(20, 90),
            Size = new Size(1050, 160),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_sessionsGrid);
        _sessionsGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "StartedAt",  HeaderText = "STARTED",     FillWeight = 16 },
            new DataGridViewTextBoxColumn { Name = "Duration",   HeaderText = "DURATION",     FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Status",     HeaderText = "STATUS",       FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "TotalUrls",  HeaderText = "URLS",         FillWeight = 6  },
            new DataGridViewTextBoxColumn { Name = "Success",    HeaderText = "OK",           FillWeight = 6  },
            new DataGridViewTextBoxColumn { Name = "Failure",    HeaderText = "FAIL",         FillWeight = 6  },
            new DataGridViewTextBoxColumn { Name = "Warnings",   HeaderText = "WARN",         FillWeight = 6  },
            new DataGridViewTextBoxColumn { Name = "Critical",   HeaderText = "CRIT",         FillWeight = 6  },
            new DataGridViewTextBoxColumn { Name = "AvgMs",      HeaderText = "AVG(ms)",      FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "MaxMs",      HeaderText = "MAX(ms)",      FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Report",     HeaderText = "REPORT",       FillWeight = 14 },
        ]);
        _sessionsGrid.SelectionChanged += SessionsGrid_SelectionChanged;

        // Pages grid (middle)
        var pagesLabel = NeonTheme.CreateLabel("▸ PAGE VISITS", isHeader: false);
        pagesLabel.ForeColor = NeonTheme.TextAccent;
        pagesLabel.Location = new Point(20, 260);

        _pagesGrid = new DataGridView
        {
            Location = new Point(20, 282),
            Size = new Size(1050, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_pagesGrid);
        _pagesGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Time",     HeaderText = "TIME",      FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Url",      HeaderText = "URL",        FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "Status",   HeaderText = "HTTP",       FillWeight = 7  },
            new DataGridViewTextBoxColumn { Name = "TTFB",     HeaderText = "TTFB(ms)",   FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Total",    HeaderText = "TOTAL(ms)",  FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Size",     HeaderText = "SIZE",       FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Result",   HeaderText = "RESULT",     FillWeight = 8  },
            new DataGridViewTextBoxColumn { Name = "Error",    HeaderText = "ERROR",      FillWeight = 20 },
        ]);
        _pagesGrid.SelectionChanged += PagesGrid_SelectionChanged;

        // Detail log (bottom)
        var detailLabel = NeonTheme.CreateLabel("▸ DETAIL", isHeader: false);
        detailLabel.ForeColor = NeonTheme.TextAccent;
        detailLabel.Location = new Point(20, 492);

        _detailView = new NeonRichTextLog
        {
            Location = new Point(20, 514),
            Size = new Size(1050, 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([
            titleLabel, _refreshButton, _openReportButton, _summaryLabel,
            sessionsLabel, _sessionsGrid,
            pagesLabel, _pagesGrid,
            detailLabel, _detailView,
        ]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await LoadSessionsAsync();
    }

    // ── Old constructor compat (MdiParentForm still passes only IAppLogger) ──
    // Removed: use the new (IBlancoDatabase, IAppLogger) constructor

    public async Task LoadSessionsAsync()
    {
        if (InvokeRequired) { BeginInvoke(async () => await LoadSessionsAsync()); return; }

        try
        {
            var sessions = await _database.GetRunSessionsAsync(limit: 50);

            _sessionsGrid.Rows.Clear();
            foreach (var s in sessions.OrderByDescending(x => x.StartedAt))
            {
                var duration = s.CompletedAt.HasValue
                    ? (s.CompletedAt.Value - s.StartedAt).ToString(@"mm\:ss")
                    : "—";

                var rowIdx = _sessionsGrid.Rows.Add(
                    s.StartedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    duration,
                    s.Status.ToString(),
                    s.TotalUrls,
                    s.SuccessCount,
                    s.FailureCount,
                    s.WarningCount,
                    s.CriticalCount,
                    s.AverageResponseTimeMs > 0 ? $"{s.AverageResponseTimeMs:F0}" : "—",
                    s.MaxResponseTimeMs > 0 ? $"{s.MaxResponseTimeMs:F0}" : "—",
                    string.IsNullOrEmpty(s.ReportPath) ? "—" : Path.GetFileName(s.ReportPath));

                _sessionsGrid.Rows[rowIdx].Tag = s;

                // Color code by critical/warning
                if (s.CriticalCount > 0)
                    _sessionsGrid.Rows[rowIdx].DefaultCellStyle.ForeColor = NeonTheme.Critical;
                else if (s.WarningCount > 0)
                    _sessionsGrid.Rows[rowIdx].DefaultCellStyle.ForeColor = NeonTheme.Warning;
            }

            var total = sessions.Count;
            var totalUrls = sessions.Sum(s => s.TotalUrls);
            var totalCrit = sessions.Sum(s => s.CriticalCount);
            _summaryLabel.Text = total == 0
                ? "No data — run a monitoring session first."
                : $"  {total} session(s) | {totalUrls} total URLs checked | {totalCrit} critical alerts";
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load run sessions", ex);
            _summaryLabel.Text = $"ERROR: {ex.Message}";
        }
    }

    private async void SessionsGrid_SelectionChanged(object? sender, EventArgs e)
    {
        _pagesGrid.Rows.Clear();
        _detailView.Clear();
        _openReportButton.Enabled = false;
        _selectedSession = null;

        if (_sessionsGrid.CurrentRow?.Tag is not RunSession session) return;

        _selectedSession = session;

        // Enable report button if path exists
        if (!string.IsNullOrEmpty(session.ReportPath) && File.Exists(session.ReportPath))
            _openReportButton.Enabled = true;

        try
        {
            var pages = await _database.GetPageVisitsByRunAsync(session.Id);
            foreach (var p in pages.OrderBy(x => x.Timestamp))
            {
                var rowIdx = _pagesGrid.Rows.Add(
                    p.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                    p.Url,
                    p.StatusCode > 0 ? p.StatusCode.ToString() : "—",
                    p.TimeToFirstByteMs > 0 ? $"{p.TimeToFirstByteMs:F0}" : "—",
                    p.TotalTimeMs > 0 ? $"{p.TotalTimeMs:F0}" : "—",
                    FormatSize(p.ContentLength),
                    p.Success ? "OK" : "FAIL",
                    p.ErrorMessage ?? string.Empty);

                _pagesGrid.Rows[rowIdx].Tag = p;

                if (!p.Success)
                    _pagesGrid.Rows[rowIdx].DefaultCellStyle.ForeColor = NeonTheme.Critical;
                else if (p.TotalTimeMs > 5000)
                    _pagesGrid.Rows[rowIdx].DefaultCellStyle.ForeColor = NeonTheme.Warning;
            }

            // Session summary in detail view
            _detailView.AppendLog($"Session: {session.Id}", NeonTheme.TextDim);
            _detailView.AppendLog($"Started:  {session.StartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}", NeonTheme.TextAccent);
            _detailView.AppendLog($"Status:   {session.Status}  |  URLs: {session.TotalUrls}  |  OK: {session.SuccessCount}  FAIL: {session.FailureCount}", NeonTheme.TextPrimary);
            _detailView.AppendLog($"Alerts:   WARN={session.WarningCount}  CRIT={session.CriticalCount}", session.CriticalCount > 0 ? NeonTheme.Critical : NeonTheme.Warning);
            _detailView.AppendLog($"Timing:   avg={session.AverageResponseTimeMs:F0}ms  max={session.MaxResponseTimeMs:F0}ms", NeonTheme.TextPrimary);
            if (!string.IsNullOrEmpty(session.ReportPath))
                _detailView.AppendLog($"Report:   {session.ReportPath}", NeonTheme.LogCyan);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load page visits", ex);
        }
    }

    private async void PagesGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_pagesGrid.CurrentRow?.Tag is not PageVisit page) return;

        _detailView.Clear();
        _detailView.AppendLog($"URL:     {page.Url}", NeonTheme.TextAccent);
        _detailView.AppendLog($"Time:    {page.Timestamp.ToLocalTime():HH:mm:ss}   Status: {page.StatusCode}", NeonTheme.TextPrimary);
        _detailView.AppendLog($"TTFB:    {page.TimeToFirstByteMs:F0}ms   Total: {page.TotalTimeMs:F0}ms   Download: {page.ContentDownloadMs:F0}ms", NeonTheme.TextPrimary);
        _detailView.AppendLog($"Size:    {FormatSize(page.ContentLength)}   Type: {page.ContentType ?? "—"}", NeonTheme.TextDim);
        _detailView.AppendLog($"Result:  {(page.Success ? "OK ✓" : "FAIL ✗")}", page.Success ? NeonTheme.Success : NeonTheme.Critical);

        if (!string.IsNullOrEmpty(page.ErrorMessage))
            _detailView.AppendLog($"Error:   {page.ErrorMessage}", NeonTheme.Critical);

        try
        {
            var issues = await _database.GetIssuesByRunAsync(page.RunSessionId);
            var pageIssues = issues.Where(i => i.Url == page.Url).ToList();
            if (pageIssues.Count > 0)
            {
                _detailView.AppendLog($"", NeonTheme.TextDim);
                _detailView.AppendLog($"Issues ({pageIssues.Count}):", NeonTheme.Warning);
                foreach (var issue in pageIssues)
                    _detailView.AppendLog($"  [{issue.Severity}] {issue.Title}: {issue.Description}", issue.Severity == Severity.Critical ? NeonTheme.Critical : NeonTheme.Warning);
            }
        }
        catch { /* non-critical */ }
    }

    private void OpenReport_Click(object? sender, EventArgs e)
    {
        var path = _selectedSession?.ReportPath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        else
        {
            _logger.Warning("No report found. Run a monitoring session first.");
            MessageBox.Show("No report file found for the selected session.", "Report",
                MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "—";
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1}KB";
        return $"{bytes / (1024.0 * 1024):F1}MB";
    }
}

