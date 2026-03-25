using BlancoMonitor.Application.Dto;
using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class MonitoringForm : Form
{
    private readonly MonitoringOrchestrator _orchestrator;
    private readonly UrlKeywordSetManager _urlManager;
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _config;

    private readonly NeonRichTextLog _logView;
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private readonly Label _progressLabel;
    private readonly Button _startButton;
    private readonly Button _lazyStartButton;
    private readonly Button _pauseButton;
    private readonly Button _stopButton;
    private readonly DataGridView _liveGrid;

    private CancellationTokenSource? _cts;
    private ManualResetEventSlim? _pauseEvent;
    private bool _isPaused;
    private MonitoringSummary? _lastSummary;

    public MonitoringForm(
        MonitoringOrchestrator orchestrator,
        UrlKeywordSetManager urlManager,
        IAppLogger logger,
        AppConfiguration config)
    {
        _orchestrator = orchestrator;
        _urlManager = urlManager;
        _logger = logger;
        _config = config;

        Text = "Monitoring";
        Size = new Size(1050, 680);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(30, 30);
        MinimumSize = new Size(800, 500);

        // Title
        var titleLabel = NeonTheme.CreateLabel("▸ MONITORING ENGINE", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Controls panel
        _startButton = NeonTheme.CreateButton("▶ START", 110, 34);
        _startButton.Location = new Point(20, 50);
        _startButton.Click += StartButton_Click;

        _lazyStartButton = NeonTheme.CreateButton("🐢 LAZY START", 130, 34);
        _lazyStartButton.Location = new Point(140, 50);
        _lazyStartButton.Click += LazyStartButton_Click;

        _pauseButton = NeonTheme.CreateButton("⏸ PAUSE", 110, 34);
        _pauseButton.Location = new Point(280, 50);
        _pauseButton.Enabled = false;
        _pauseButton.Click += PauseButton_Click;

        _stopButton = NeonTheme.CreateButton("■ STOP", 110, 34);
        _stopButton.Location = new Point(400, 50);
        _stopButton.Enabled = false;
        _stopButton.Click += StopButton_Click;

        _statusLabel = new Label
        {
            Text = "STATUS: IDLE",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontLarge,
            Location = new Point(540, 58),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Progress
        _progressBar = new ProgressBar
        {
            Location = new Point(20, 95),
            Size = new Size(995, 8),
            Style = ProgressBarStyle.Continuous,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _progressLabel = new Label
        {
            Text = string.Empty,
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(20, 108),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Live results grid
        _liveGrid = new DataGridView
        {
            Location = new Point(20, 130),
            Size = new Size(995, 230),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_liveGrid);
        _liveGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "HTTP", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "TTFB", HeaderText = "TTFB(ms)", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "TOTAL(ms)", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "SIZE", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Alerts", HeaderText = "ALERTS", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "RESULT", FillWeight = 10 },
        ]);

        // Log
        var logLabel = NeonTheme.CreateLabel("▸ TRACE LOG", isHeader: false);
        logLabel.ForeColor = NeonTheme.TextAccent;
        logLabel.Location = new Point(20, 370);

        _logView = new NeonRichTextLog
        {
            Location = new Point(20, 395),
            Size = new Size(995, 240),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        Controls.AddRange([titleLabel, _startButton, _lazyStartButton, _pauseButton, _stopButton, _statusLabel,
            _progressBar, _progressLabel, _liveGrid, logLabel, _logView]);

        NeonTheme.Apply(this);

        // Subscribe to logger
        _logger.LogEntryAdded += OnLogEntry;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
        => await RunMonitoringAsync(lazyMode: false);

    private async void LazyStartButton_Click(object? sender, EventArgs e)
        => await RunMonitoringAsync(lazyMode: true);

    private async Task RunMonitoringAsync(bool lazyMode)
    {
        await _urlManager.LoadAsync();
        var targets = _urlManager.Targets.Where(t => t.IsEnabled).ToList();

        if (targets.Count == 0)
        {
            _logger.Warning("No enabled targets. Add URLs in the URL Manager first.");
            return;
        }

        _startButton.Enabled = false;
        _lazyStartButton.Enabled = false;
        _stopButton.Enabled = true;
        _pauseButton.Enabled = true;
        _isPaused = false;
        _liveGrid.Rows.Clear();

        var modeLabel = lazyMode ? "LAZY RUNNING 🐢" : "RUNNING";
        _statusLabel.Text = $"STATUS: {modeLabel}";
        _statusLabel.ForeColor = NeonTheme.TextAccent;

        if (lazyMode)
            _logger.Info("🐢 Lazy mode: ~10s delay between pages, random subset");

        _cts = new CancellationTokenSource();
        _pauseEvent = new ManualResetEventSlim(true);

        var progress = new Progress<MonitoringProgress>(p =>
        {
            _progressBar.Maximum = Math.Max(1, p.TotalCount);
            _progressBar.Value = Math.Min(p.CurrentIndex, p.TotalCount);
            _progressLabel.Text = p.StatusMessage;
        });

        try
        {
            _lastSummary = await Task.Run(
                () => _orchestrator.RunAsync(_config, targets, progress, _cts.Token, _pauseEvent, lazyMode),
                _cts.Token);

            PopulateResults(_lastSummary);
            _statusLabel.Text = "STATUS: COMPLETED";
            _statusLabel.ForeColor = NeonTheme.Success;

            if (_lastSummary.ReportPath is not null)
                _logger.Info($"Report saved: {_lastSummary.ReportPath}");
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "STATUS: CANCELLED";
            _statusLabel.ForeColor = NeonTheme.Warning;
            _logger.Warning("Monitoring cancelled by user");
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "STATUS: ERROR";
            _statusLabel.ForeColor = NeonTheme.Critical;
            _logger.Error("Monitoring failed", ex);
        }
        finally
        {
            _startButton.Enabled = true;
            _lazyStartButton.Enabled = true;
            _stopButton.Enabled = false;
            _pauseButton.Enabled = false;
            _pauseButton.Text = "⏸ PAUSE";
            _isPaused = false;
            _cts.Dispose();
            _cts = null;
            _pauseEvent?.Dispose();
            _pauseEvent = null;
        }
    }

    private void StopButton_Click(object? sender, EventArgs e)
    {
        // If paused, resume first so the cancellation can propagate
        _pauseEvent?.Set();
        _cts?.Cancel();
    }

    private void PauseButton_Click(object? sender, EventArgs e)
    {
        if (_pauseEvent is null) return;

        _isPaused = !_isPaused;

        if (_isPaused)
        {
            _pauseEvent.Reset();
            _pauseButton.Text = "▶ RESUME";
            _statusLabel.Text = "STATUS: PAUSED";
            _statusLabel.ForeColor = NeonTheme.Warning;
            _logger.Info("Monitoring paused");
        }
        else
        {
            _pauseEvent.Set();
            _pauseButton.Text = "⏸ PAUSE";
            _statusLabel.Text = "STATUS: RUNNING";
            _statusLabel.ForeColor = NeonTheme.TextAccent;
            _logger.Info("Monitoring resumed");
        }
    }

    private void PopulateResults(MonitoringSummary summary)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => PopulateResults(summary));
            return;
        }

        _liveGrid.Rows.Clear();
        foreach (var result in summary.Results)
        {
            var m = result.Metrics;
            var alertCount = result.Alerts.Count;
            var rowIndex = _liveGrid.Rows.Add(
                result.Url,
                m?.StatusCode.ToString() ?? "—",
                m?.TimeToFirstByteMs.ToString("F0") ?? "—",
                m?.TotalTimeMs.ToString("F0") ?? "—",
                FormatSize(m?.ContentLength ?? 0),
                alertCount.ToString(),
                result.Success ? "OK" : "FAIL");

            var row = _liveGrid.Rows[rowIndex];
            if (result.Alerts.Any(a => a.Severity == Severity.Critical))
            {
                row.DefaultCellStyle.ForeColor = NeonTheme.Critical;
            }
            else if (alertCount > 0)
            {
                row.DefaultCellStyle.ForeColor = NeonTheme.Warning;
            }
        }
    }

    private void OnLogEntry(DateTime timestamp, Severity severity, string message)
    {
        var color = severity switch
        {
            Severity.Warning => NeonTheme.Warning,
            Severity.Critical => NeonTheme.Critical,
            _ => GetInfoColor(message),
        };
        _logView.AppendLog($"[{timestamp:HH:mm:ss.fff}] {message}", color);
    }

    private static Color GetInfoColor(string message)
    {
        // Page load start marker
        if (message.StartsWith("► ") || message.StartsWith("▶ "))
            return NeonTheme.LogCyan;

        // Page summary with TTFB/Sub stats — color by Full page load time
        if (message.Contains("TTFB=") && message.Contains("Full="))
        {
            var fullMs = ExtractMs(message, "Full=");
            if (fullMs > 5000) return NeonTheme.Critical;
            if (fullMs > 3000) return NeonTheme.Warning;
            return NeonTheme.TextAccent;
        }

        // Request/DOM stats line
        if (message.StartsWith("→ ") || message.StartsWith("  → "))
            return message.Contains("failed") && !message.Contains("0 failed")
                ? NeonTheme.Warning
                : NeonTheme.LogWhite;

        // HTTP request with body (primary page fetch) — color by response time
        if (message.Contains("(with body)"))
        {
            var ms = ExtractTrailingMs(message);
            if (ms > 5000) return NeonTheme.Critical;
            if (ms > 3000) return NeonTheme.Warning;
            return NeonTheme.TextPrimary;
        }

        // Discovery and historical data (secondary info)
        if (message.StartsWith("Discovered ") || message.StartsWith("Historical data saved"))
            return NeonTheme.TextDim;

        return NeonTheme.TextPrimary;
    }

    /// <summary>Extracts ms value after a named prefix like "Full=" or "TTFB=".</summary>
    private static double ExtractMs(string message, string prefix)
    {
        var idx = message.IndexOf(prefix, StringComparison.Ordinal);
        if (idx < 0) return 0;
        idx += prefix.Length;
        var end = idx;
        while (end < message.Length && (char.IsDigit(message[end]) || message[end] == '.'))
            end++;
        return double.TryParse(message.AsSpan(idx, end - idx),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    /// <summary>Extracts "— XXXms" trailing duration from HTTP request log lines.</summary>
    private static double ExtractTrailingMs(string message)
    {
        // Format: "[200] GET url — 196ms (with body)"
        var dashIdx = message.LastIndexOf('—');
        if (dashIdx < 0) return 0;
        var sub = message.AsSpan(dashIdx + 1).Trim();
        var end = 0;
        while (end < sub.Length && (char.IsDigit(sub[end]) || sub[end] == '.'))
            end++;
        return double.TryParse(sub[..end],
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1}K",
        _ => $"{bytes / (1024.0 * 1024.0):F1}M",
    };

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logger.LogEntryAdded -= OnLogEntry;
        _pauseEvent?.Set();
        _cts?.Cancel();
        _cts?.Dispose();
        _pauseEvent?.Dispose();
        base.OnFormClosed(e);
    }
}
