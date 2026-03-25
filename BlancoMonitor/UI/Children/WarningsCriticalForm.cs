using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class WarningsCriticalForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly DataGridView _issueGrid;
    private readonly NeonRichTextLog _detailLog;
    private readonly ComboBox _severityFilter;
    private readonly ComboBox _runFilter;
    private readonly Label _statsLabel;

    public WarningsCriticalForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "Warnings & Critical";
        Size = new Size(1000, 620);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(50, 40);

        var titleLabel = NeonTheme.CreateLabel("▸ WARNINGS & CRITICAL ISSUES", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Filters
        var filterPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(950, 40),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var sevLabel = new Label { Text = "SEVERITY:", Location = new Point(10, 10), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _severityFilter = new ComboBox
        {
            Location = new Point(120, 7),
            Size = new Size(140, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        _severityFilter.Items.AddRange(["ALL", "Notice", "Warning", "Critical"]);
        _severityFilter.SelectedIndex = 0;
        _severityFilter.SelectedIndexChanged += async (_, _) => await LoadIssues();

        var runLabel = new Label { Text = "RUN:", Location = new Point(280, 10), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _runFilter = new ComboBox
        {
            Location = new Point(330, 7),
            Size = new Size(250, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        _runFilter.SelectedIndexChanged += async (_, _) => await LoadIssues();

        var refreshBtn = NeonTheme.CreateButton("↻ REFRESH", 110, 28);
        refreshBtn.Location = new Point(600, 5);
        refreshBtn.Click += async (_, _) => await LoadRunSessions();

        filterPanel.Controls.AddRange([sevLabel, _severityFilter, runLabel, _runFilter, refreshBtn]);

        // Stats
        _statsLabel = new Label
        {
            Text = "Critical: 0 | Warning: 0 | Notice: 0",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 98),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Issue grid
        _issueGrid = new DataGridView
        {
            Location = new Point(20, 120),
            Size = new Size(950, 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_issueGrid);
        _issueGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "SEV", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "TYPE", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "TITLE", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "DETAIL", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Confidence", HeaderText = "CONF", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Detected", HeaderText = "TIME", FillWeight = 10 },
        ]);
        _issueGrid.SelectionChanged += IssueGrid_SelectionChanged;

        // Detail view
        var detailLabel = NeonTheme.CreateLabel("▸ ISSUE DETAIL", isHeader: false);
        detailLabel.ForeColor = NeonTheme.TextAccent;
        detailLabel.Location = new Point(20, 390);

        _detailLog = new NeonRichTextLog
        {
            Location = new Point(20, 415),
            Size = new Size(950, 165),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, filterPanel, _statsLabel, _issueGrid, detailLabel, _detailLog]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await LoadRunSessions();
    }

    private async Task LoadRunSessions()
    {
        try
        {
            var sessions = await _database.GetRunSessionsAsync(limit: 30);
            _runFilter.Items.Clear();
            _runFilter.Items.Add("(all runs)");
            foreach (var s in sessions)
                _runFilter.Items.Add($"{s.StartedAt:yyyy-MM-dd HH:mm} — {s.Status}");

            _runFilter.Tag = sessions;
            _runFilter.SelectedIndex = sessions.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load run sessions for warnings", ex);
        }
    }

    private async Task LoadIssues()
    {
        if (_runFilter.Tag is not List<Domain.Entities.RunSession> sessions) return;

        try
        {
            _issueGrid.Rows.Clear();
            List<Domain.Entities.DetectedIssue> issues;

            var runIdx = _runFilter.SelectedIndex - 1;
            if (runIdx >= 0 && runIdx < sessions.Count)
                issues = await _database.GetIssuesByRunAsync(sessions[runIdx].Id);
            else
                issues = [];

            var sevFilter = _severityFilter.SelectedItem?.ToString();
            int critCount = 0, warnCount = 0, noticeCount = 0;

            foreach (var issue in issues)
            {
                var sevName = issue.Severity.ToString();
                if (issue.Severity == Severity.Critical) critCount++;
                else if (issue.Severity == Severity.Warning) warnCount++;
                else noticeCount++;

                if (sevFilter != "ALL" && !string.Equals(sevName, sevFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var rowIndex = _issueGrid.Rows.Add(
                    sevName,
                    issue.Category.ToString(),
                    issue.Url,
                    issue.Title,
                    issue.Description,
                    issue.Confidence.ToString("P0"),
                    issue.Timestamp.ToString("HH:mm:ss"));

                var row = _issueGrid.Rows[rowIndex];
                row.DefaultCellStyle.ForeColor = issue.Severity switch
                {
                    Severity.Critical => NeonTheme.Critical,
                    Severity.Warning => NeonTheme.Warning,
                    _ => NeonTheme.TextDim,
                };
            }

            _statsLabel.Text = $"Critical: {critCount} | Warning: {warnCount} | Notice: {noticeCount}";
            _statsLabel.ForeColor = critCount > 0 ? NeonTheme.Critical : warnCount > 0 ? NeonTheme.Warning : NeonTheme.TextDim;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load issues", ex);
        }
    }

    private void IssueGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_issueGrid.CurrentRow is null) return;
        _detailLog.Clear();

        var row = _issueGrid.CurrentRow;
        var sevText = row.Cells["Severity"].Value?.ToString();
        var sevColor = sevText switch
        {
            "Critical" => NeonTheme.Critical,
            "Warning" => NeonTheme.Warning,
            _ => NeonTheme.TextDim,
        };

        _detailLog.AppendLog($"SEVERITY:    {row.Cells["Severity"].Value}", sevColor);
        _detailLog.AppendLog($"CATEGORY:    {row.Cells["Category"].Value}", NeonTheme.TextAccent);
        _detailLog.AppendLog($"URL:         {row.Cells["Url"].Value}");
        _detailLog.AppendLog($"TITLE:       {row.Cells["Title"].Value}");
        _detailLog.AppendLog($"DETAIL:      {row.Cells["Description"].Value}");
        _detailLog.AppendLog($"CONFIDENCE:  {row.Cells["Confidence"].Value}");
        _detailLog.AppendLog($"DETECTED:    {row.Cells["Detected"].Value}");
    }
}
