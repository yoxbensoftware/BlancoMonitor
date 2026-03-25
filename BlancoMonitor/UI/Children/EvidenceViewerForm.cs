using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class EvidenceViewerForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly DataGridView _evidenceGrid;
    private readonly NeonRichTextLog _previewLog;
    private readonly PictureBox _screenshotBox;
    private readonly ComboBox _runFilter;
    private readonly Button _openFileButton;

    public EvidenceViewerForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "Evidence Viewer";
        Size = new Size(960, 620);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(70, 35);

        var titleLabel = NeonTheme.CreateLabel("▸ EVIDENCE VIEWER", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Filter bar
        var filterPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(910, 40),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var runLabel = new Label { Text = "RUN:", Location = new Point(10, 10), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _runFilter = new ComboBox
        {
            Location = new Point(65, 7),
            Size = new Size(280, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        _runFilter.SelectedIndexChanged += async (_, _) => await LoadEvidence();

        _openFileButton = NeonTheme.CreateButton("📂 OPEN FILE", 120, 28);
        _openFileButton.Location = new Point(360, 5);
        _openFileButton.Click += OpenFile_Click;

        var refreshBtn = NeonTheme.CreateButton("↻ REFRESH", 110, 28);
        refreshBtn.Location = new Point(500, 5);
        refreshBtn.Click += async (_, _) => await LoadRunSessions();

        filterPanel.Controls.AddRange([runLabel, _runFilter, _openFileButton, refreshBtn]);

        // Evidence grid
        _evidenceGrid = new DataGridView
        {
            Location = new Point(20, 100),
            Size = new Size(910, 220),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_evidenceGrid);
        _evidenceGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "TYPE", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "SOURCE URL", FillWeight = 35 },
            new DataGridViewTextBoxColumn { Name = "FilePath", HeaderText = "FILE PATH", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "FileSize", HeaderText = "SIZE", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "CapturedAt", HeaderText = "CAPTURED", FillWeight = 13 },
        ]);
        _evidenceGrid.SelectionChanged += EvidenceGrid_SelectionChanged;

        // Preview area (split: text on left, screenshot on right)
        var previewLabel = NeonTheme.CreateLabel("▸ PREVIEW", isHeader: false);
        previewLabel.ForeColor = NeonTheme.TextAccent;
        previewLabel.Location = new Point(20, 330);

        _previewLog = new NeonRichTextLog
        {
            Location = new Point(20, 355),
            Size = new Size(450, 225),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
        };

        _screenshotBox = new PictureBox
        {
            Location = new Point(480, 355),
            Size = new Size(450, 225),
            BackColor = NeonTheme.Surface,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, filterPanel, _evidenceGrid, previewLabel, _previewLog, _screenshotBox]);
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
                _runFilter.Items.Add($"{s.StartedAt:yyyy-MM-dd HH:mm} — {s.Status}");

            _runFilter.Tag = sessions;
            _runFilter.SelectedIndex = sessions.Count > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load run sessions for evidence viewer", ex);
        }
    }

    private async Task LoadEvidence()
    {
        if (_runFilter.Tag is not List<Domain.Entities.RunSession> sessions) return;
        var idx = _runFilter.SelectedIndex - 1;
        if (idx < 0 || idx >= sessions.Count) return;

        try
        {
            _evidenceGrid.Rows.Clear();
            var visits = await _database.GetPageVisitsByRunAsync(sessions[idx].Id);

            foreach (var visit in visits)
            {
                var items = await _database.GetEvidenceItemsAsync(visit.Id);
                foreach (var item in items)
                {
                    _evidenceGrid.Rows.Add(
                        item.Type.ToString(),
                        visit.Url,
                        item.FilePath,
                        FormatSize(item.FileSizeBytes),
                        item.CreatedAt.ToString("HH:mm:ss"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load evidence items", ex);
        }
    }

    private void EvidenceGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_evidenceGrid.CurrentRow is null) return;
        _previewLog.Clear();
        _screenshotBox.Image = null;

        var row = _evidenceGrid.CurrentRow;
        var filePath = row.Cells["FilePath"].Value?.ToString();
        var type = row.Cells["Type"].Value?.ToString();

        _previewLog.AppendLog($"Type:     {type}", NeonTheme.TextAccent);
        _previewLog.AppendLog($"URL:      {row.Cells["Url"].Value}");
        _previewLog.AppendLog($"File:     {filePath}");
        _previewLog.AppendLog($"Size:     {row.Cells["FileSize"].Value}");
        _previewLog.AppendLog($"Captured: {row.Cells["CapturedAt"].Value}");

        if (filePath is not null && File.Exists(filePath))
        {
            if (type is "Screenshot" or "screenshot")
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    _screenshotBox.Image = Image.FromStream(stream);
                }
                catch
                {
                    _previewLog.AppendLog("(Could not load screenshot)", NeonTheme.Warning);
                }
            }
            else
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    if (content.Length > 5000) content = content[..5000] + "\n... (truncated)";
                    _previewLog.AppendLog("─── FILE CONTENT ───", NeonTheme.TextDim);
                    _previewLog.AppendLog(content);
                }
                catch
                {
                    _previewLog.AppendLog("(Could not read file)", NeonTheme.Warning);
                }
            }
        }
        else
        {
            _previewLog.AppendLog("(File not found on disk)", NeonTheme.TextDim);
        }
    }

    private void OpenFile_Click(object? sender, EventArgs e)
    {
        if (_evidenceGrid.CurrentRow is null) return;
        var filePath = _evidenceGrid.CurrentRow.Cells["FilePath"].Value?.ToString();
        if (filePath is not null && File.Exists(filePath))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to open evidence file", ex);
            }
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB",
    };
}
