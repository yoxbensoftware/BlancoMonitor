using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class UrlManagerForm : Form
{
    private readonly UrlKeywordSetManager _manager;
    private readonly IAppLogger _logger;
    private readonly DataGridView _grid;
    private readonly TextBox _nameBox;
    private readonly TextBox _urlBox;
    private readonly TextBox _keywordsBox;
    private readonly Button _addButton;
    private readonly Button _removeButton;

    public UrlManagerForm(UrlKeywordSetManager manager, IAppLogger logger)
    {
        _manager = manager;
        _logger = logger;

        Text = "URL & Keyword Manager";
        Size = new Size(960, 580);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(20, 20);
        MinimumSize = new Size(750, 450);

        var titleLabel = NeonTheme.CreateLabel("▸ URL & KEYWORD SETS", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Input panel
        var inputPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(910, 100),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var nameLabel = new Label { Text = "Name:", Location = new Point(10, 12), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent };
        _nameBox = new TextBox { Location = new Point(110, 10), Size = new Size(200, 24), BackColor = NeonTheme.Surface, ForeColor = NeonTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        var urlLabel = new Label { Text = "URL:", Location = new Point(10, 42), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent };
        _urlBox = new TextBox { Location = new Point(110, 40), Size = new Size(480, 24), BackColor = NeonTheme.Surface, ForeColor = NeonTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };

        var kwLabel = new Label { Text = "Keywords:", Location = new Point(10, 72), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent };
        _keywordsBox = new TextBox { Location = new Point(110, 70), Size = new Size(480, 24), BackColor = NeonTheme.Surface, ForeColor = NeonTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "comma-separated" };

        _addButton = NeonTheme.CreateButton("+ ADD", 110, 28);
        _addButton.Location = new Point(620, 10);
        _addButton.Click += AddButton_Click;

        _removeButton = NeonTheme.CreateButton("- REMOVE", 110, 28);
        _removeButton.Location = new Point(620, 42);
        _removeButton.Click += RemoveButton_Click;

        inputPanel.Controls.AddRange([nameLabel, _nameBox, urlLabel, _urlBox, kwLabel, _keywordsBox, _addButton, _removeButton]);

        // Grid
        _grid = new DataGridView
        {
            Location = new Point(20, 165),
            Size = new Size(910, 370),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "NAME", FillWeight = 20 },
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "Keywords", HeaderText = "KEYWORDS", FillWeight = 25 },
            new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "ON", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Interval", HeaderText = "INT(s)", FillWeight = 7 },
        ]);

        Controls.AddRange([titleLabel, inputPanel, _grid]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await RefreshGrid();
    }

    private static string NormalizeUrl(string raw)
    {
        raw = raw.Trim();
        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            raw = "https://" + raw;
        }
        return raw;
    }

    private async void AddButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_urlBox.Text))
        {
            _logger.Warning("URL is required");
            return;
        }

        var normalizedUrl = NormalizeUrl(_urlBox.Text);

        var target = new MonitorTarget
        {
            Name = string.IsNullOrWhiteSpace(_nameBox.Text) ? new Uri(normalizedUrl).Host : _nameBox.Text,
            Url = normalizedUrl,
            Keywords = _keywordsBox.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
        };

        await _manager.AddTargetAsync(target);
        _nameBox.Clear();
        _urlBox.Clear();
        _keywordsBox.Clear();
        await RefreshGrid();
    }

    private async void RemoveButton_Click(object? sender, EventArgs e)
    {
        if (_grid.CurrentRow is null) return;

        var name = _grid.CurrentRow.Cells["Name"].Value?.ToString();
        var target = _manager.Targets.FirstOrDefault(t => t.Name == name);
        if (target is not null)
        {
            await _manager.RemoveTargetAsync(target.Id);
            await RefreshGrid();
        }
    }

    private async Task RefreshGrid()
    {
        await _manager.LoadAsync();
        _grid.Rows.Clear();
        foreach (var t in _manager.Targets)
        {
            _grid.Rows.Add(t.Name, t.Url, string.Join(", ", t.Keywords), t.IsEnabled, t.CheckIntervalSeconds);
        }
    }
}
