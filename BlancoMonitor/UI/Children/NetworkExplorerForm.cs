using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class NetworkExplorerForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly DataGridView _requestGrid;
    private readonly NeonRichTextLog _detailLog;
    private readonly ComboBox _runFilter;
    private readonly ComboBox _categoryFilter;
    private readonly Button _refreshButton;
    private readonly Label _summaryLabel;

    public NetworkExplorerForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "Network Explorer";
        Size = new Size(1000, 650);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(60, 30);

        var titleLabel = NeonTheme.CreateLabel("▸ NETWORK EXPLORER", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Filters
        var filterPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(950, 40),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var runLabel = new Label { Text = "RUN:", Location = new Point(10, 10), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _runFilter = new ComboBox
        {
            Location = new Point(65, 7),
            Size = new Size(250, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        _runFilter.SelectedIndexChanged += async (_, _) => await LoadRequests();

        var catLabel = new Label { Text = "TYPE:", Location = new Point(330, 10), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _categoryFilter = new ComboBox
        {
            Location = new Point(395, 7),
            Size = new Size(160, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        _categoryFilter.Items.AddRange(["ALL", "Document", "Stylesheet", "Script", "Image", "Font", "API", "Media", "ThirdParty", "Other"]);
        _categoryFilter.SelectedIndex = 0;
        _categoryFilter.SelectedIndexChanged += async (_, _) => await LoadRequests();

        _refreshButton = NeonTheme.CreateButton("↻ REFRESH", 110, 28);
        _refreshButton.Location = new Point(580, 5);
        _refreshButton.Click += async (_, _) => await LoadRunSessions();

        filterPanel.Controls.AddRange([runLabel, _runFilter, catLabel, _categoryFilter, _refreshButton]);

        // Summary
        _summaryLabel = new Label
        {
            Text = "Requests: 0 | Total Size: 0 B | Avg Time: — ms",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 98),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Request grid
        _requestGrid = new DataGridView
        {
            Location = new Point(20, 120),
            Size = new Size(950, 280),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_requestGrid);
        _requestGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "METHOD", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "HTTP", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "TYPE", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "SIZE", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "TIME(ms)", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "ThirdParty", HeaderText = "3RD", FillWeight = 5 },
            new DataGridViewTextBoxColumn { Name = "Initiator", HeaderText = "INITIATOR", FillWeight = 15 },
        ]);
        _requestGrid.SelectionChanged += RequestGrid_SelectionChanged;

        // Detail log
        var detailLabel = NeonTheme.CreateLabel("▸ REQUEST DETAIL", isHeader: false);
        detailLabel.ForeColor = NeonTheme.TextAccent;
        detailLabel.Location = new Point(20, 410);

        _detailLog = new NeonRichTextLog
        {
            Location = new Point(20, 435),
            Size = new Size(950, 175),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, filterPanel, _summaryLabel, _requestGrid, detailLabel, _detailLog]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await LoadRunSessions();
    }

    private async Task LoadRunSessions()
    {
        try
        {
            var sessions = await _database.GetRunSessionsAsync(limit: 30);
            _runFilter.Items.Clear();
            _runFilter.Items.Add("(select run)");
            foreach (var s in sessions)
            {
                _runFilter.Items.Add($"{s.StartedAt:yyyy-MM-dd HH:mm} — {s.Status}");
            }
            _runFilter.Tag = sessions;
            _runFilter.SelectedIndex = sessions.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load run sessions", ex);
        }
    }

    private async Task LoadRequests()
    {
        if (_runFilter.Tag is not List<Domain.Entities.RunSession> sessions) return;
        var idx = _runFilter.SelectedIndex - 1;
        if (idx < 0 || idx >= sessions.Count) return;

        try
        {
            var visits = await _database.GetPageVisitsByRunAsync(sessions[idx].Id);
            _requestGrid.Rows.Clear();

            var filterCat = _categoryFilter.SelectedItem?.ToString();
            var totalSize = 0L;
            var totalTime = 0.0;
            var count = 0;

            foreach (var visit in visits)
            {
                var requests = await _database.GetNetworkRequestsAsync(visit.Id);
                foreach (var req in requests)
                {
                    var category = GuessCategory(req.ContentType, req.Url);
                    if (filterCat != "ALL" && !string.Equals(category, filterCat, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var isThirdParty = !string.IsNullOrEmpty(visit.Url) && !string.IsNullOrEmpty(req.Url)
                        && Uri.TryCreate(visit.Url, UriKind.Absolute, out var visitUri)
                        && Uri.TryCreate(req.Url, UriKind.Absolute, out var reqUri)
                        && !string.Equals(visitUri.Host, reqUri.Host, StringComparison.OrdinalIgnoreCase);

                    _requestGrid.Rows.Add(
                        req.Url,
                        req.Method,
                        req.StatusCode.ToString(),
                        category,
                        FormatSize(req.ContentLength),
                        req.TotalTimeMs.ToString("F0"),
                        isThirdParty ? "✓" : "",
                        "—");

                    totalSize += req.ContentLength;
                    totalTime += req.TotalTimeMs;
                    count++;
                }
            }

            _summaryLabel.Text = $"Requests: {count} | Total Size: {FormatSize(totalSize)} | Avg Time: {(count > 0 ? totalTime / count : 0):F0} ms";
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load network requests", ex);
        }
    }

    private void RequestGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_requestGrid.CurrentRow is null) return;
        _detailLog.Clear();

        var row = _requestGrid.CurrentRow;
        _detailLog.AppendLog($"URL:       {row.Cells["Url"].Value}", NeonTheme.TextAccent);
        _detailLog.AppendLog($"Method:    {row.Cells["Method"].Value}");
        _detailLog.AppendLog($"Status:    {row.Cells["Status"].Value}");
        _detailLog.AppendLog($"Category:  {row.Cells["Category"].Value}");
        _detailLog.AppendLog($"Size:      {row.Cells["Size"].Value}");
        _detailLog.AppendLog($"Duration:  {row.Cells["Duration"].Value} ms");
        _detailLog.AppendLog($"3rd Party: {row.Cells["ThirdParty"].Value}");
        _detailLog.AppendLog($"Initiator: {row.Cells["Initiator"].Value}");
    }

    private static string GuessCategory(string? contentType, string url)
    {
        if (string.IsNullOrEmpty(contentType)) return "Other";
        if (contentType.Contains("html")) return "Document";
        if (contentType.Contains("css")) return "Stylesheet";
        if (contentType.Contains("javascript") || contentType.Contains("ecmascript")) return "Script";
        if (contentType.Contains("image")) return "Image";
        if (contentType.Contains("font") || url.Contains(".woff") || url.Contains(".ttf")) return "Font";
        if (contentType.Contains("json") || contentType.Contains("xml")) return "API";
        if (contentType.Contains("video") || contentType.Contains("audio")) return "Media";
        return "Other";
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
