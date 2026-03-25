using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class DiscoveryToolsForm : Form
{
    private readonly IDiscoveryEngine _discoveryEngine;
    private readonly IAppLogger _logger;
    private readonly TextBox _urlBox;
    private readonly Button _discoverButton;
    private readonly Button _sitemapButton;
    private readonly DataGridView _resultsGrid;
    private readonly NeonRichTextLog _logView;
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;

    private CancellationTokenSource? _cts;

    public DiscoveryToolsForm(IDiscoveryEngine discoveryEngine, IAppLogger logger)
    {
        _discoveryEngine = discoveryEngine;
        _logger = logger;

        Text = "Discovery Tools";
        Size = new Size(960, 640);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(55, 30);
        MinimumSize = new Size(750, 500);

        var titleLabel = NeonTheme.CreateLabel("▸ DISCOVERY TOOLS", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Input panel
        var inputPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(910, 45),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var urlLabel = new Label { Text = "TARGET:", Location = new Point(10, 13), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _urlBox = new TextBox
        {
            Location = new Point(110, 10),
            Size = new Size(395, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "https://example.com",
        };

        _discoverButton = NeonTheme.CreateButton("🔍 CRAWL", 110, 30);
        _discoverButton.Location = new Point(520, 7);
        _discoverButton.Click += DiscoverButton_Click;

        _sitemapButton = NeonTheme.CreateButton("📋 SITEMAP", 110, 30);
        _sitemapButton.Location = new Point(640, 7);
        _sitemapButton.Click += SitemapButton_Click;

        var cancelBtn = NeonTheme.CreateButton("■ STOP", 90, 30);
        cancelBtn.Location = new Point(760, 7);
        cancelBtn.Click += (_, _) => _cts?.Cancel();

        inputPanel.Controls.AddRange([urlLabel, _urlBox, _discoverButton, _sitemapButton, cancelBtn]);

        // Progress
        _progressBar = new ProgressBar
        {
            Location = new Point(20, 103),
            Size = new Size(910, 6),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Visible = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _statusLabel = new Label
        {
            Text = "Ready — enter a URL and choose discovery method",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 115),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Results grid
        _resultsGrid = new DataGridView
        {
            Location = new Point(20, 140),
            Size = new Size(910, 240),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_resultsGrid);
        _resultsGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Index", HeaderText = "#", FillWeight = 5 },
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "DISCOVERED URL", FillWeight = 70 },
            new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "SOURCE", FillWeight = 12 },
            new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "USE", FillWeight = 8 },
        ]);

        // Log
        var logLabel = NeonTheme.CreateLabel("▸ DISCOVERY LOG", isHeader: false);
        logLabel.ForeColor = NeonTheme.TextAccent;
        logLabel.Location = new Point(20, 390);

        _logView = new NeonRichTextLog
        {
            Location = new Point(20, 415),
            Size = new Size(910, 180),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, inputPanel, _progressBar, _statusLabel, _resultsGrid, logLabel, _logView]);
        NeonTheme.Apply(this);
    }

    private async void DiscoverButton_Click(object? sender, EventArgs e)
    {
        var url = NormalizeUrl(_urlBox.Text);
        if (string.IsNullOrWhiteSpace(url)) return;

        await RunDiscovery("Crawl", async ct =>
            await _discoveryEngine.DiscoverUrlsAsync(url, ct));
    }

    private async void SitemapButton_Click(object? sender, EventArgs e)
    {
        var url = NormalizeUrl(_urlBox.Text);
        if (string.IsNullOrWhiteSpace(url)) return;

        var sitemapUrl = url.TrimEnd('/') + "/sitemap.xml";
        await RunDiscovery("Sitemap", async ct =>
            await _discoveryEngine.ParseSitemapAsync(sitemapUrl, ct));
    }

    private async Task RunDiscovery(string source, Func<CancellationToken, Task<List<string>>> operation)
    {
        _cts = new CancellationTokenSource();
        _discoverButton.Enabled = false;
        _sitemapButton.Enabled = false;
        _progressBar.Visible = true;
        _statusLabel.Text = $"Discovering ({source})...";
        _statusLabel.ForeColor = NeonTheme.TextAccent;
        _resultsGrid.Rows.Clear();
        _logView.AppendLog($"Starting {source} discovery...", NeonTheme.TextAccent);

        try
        {
            var urls = await Task.Run(() => operation(_cts.Token), _cts.Token);

            for (int i = 0; i < urls.Count; i++)
            {
                _resultsGrid.Rows.Add((i + 1).ToString(), urls[i], source, true);
            }

            _statusLabel.Text = $"Found {urls.Count} URLs via {source}";
            _statusLabel.ForeColor = NeonTheme.Success;
            _logView.AppendLog($"Discovery complete: {urls.Count} URLs found", NeonTheme.Success);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Discovery cancelled";
            _statusLabel.ForeColor = NeonTheme.Warning;
            _logView.AppendLog("Discovery cancelled by user", NeonTheme.Warning);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Discovery failed";
            _statusLabel.ForeColor = NeonTheme.Critical;
            _logView.AppendLog($"Error: {ex.Message}", NeonTheme.Critical);
            _logger.Error("Discovery failed", ex);
        }
        finally
        {
            _discoverButton.Enabled = true;
            _sitemapButton.Enabled = true;
            _progressBar.Visible = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private static string NormalizeUrl(string raw)
    {
        raw = raw.Trim();
        if (string.IsNullOrEmpty(raw)) return raw;
        if (!raw.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            raw = "https://" + raw;
        return raw;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        base.OnFormClosed(e);
    }
}
