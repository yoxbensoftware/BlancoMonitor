using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class LiveMonitoringForm : Form
{
    private readonly MonitoringOrchestrator _orchestrator;
    private readonly UrlKeywordSetManager _urlManager;
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _config;

    private readonly NeonRichTextLog _logView;
    private readonly DataGridView _liveGrid;
    private readonly Label _statusLabel;
    private readonly Label _uptimeLabel;
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly NumericUpDown _intervalBox;
    private readonly ProgressBar _progressBar;

    private CancellationTokenSource? _cts;
    private readonly System.Windows.Forms.Timer _uptimeTimer;
    private DateTime _startTime;
    private int _cycleCount;

    public LiveMonitoringForm(
        MonitoringOrchestrator orchestrator,
        UrlKeywordSetManager urlManager,
        IAppLogger logger,
        AppConfiguration config)
    {
        _orchestrator = orchestrator;
        _urlManager = urlManager;
        _logger = logger;
        _config = config;

        Text = "Live Monitoring";
        Size = new Size(1050, 700);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(35, 25);
        MinimumSize = new Size(850, 550);

        var titleLabel = NeonTheme.CreateLabel("▸ LIVE MONITORING (CONTINUOUS)", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Controls panel
        var controlPanel = new Panel
        {
            Location = new Point(20, 50),
            Size = new Size(1000, 45),
            BackColor = NeonTheme.Surface,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _startButton = NeonTheme.CreateButton("▶ START LIVE", 130, 32);
        _startButton.Location = new Point(10, 6);
        _startButton.Click += StartButton_Click;

        _stopButton = NeonTheme.CreateButton("■ STOP", 90, 32);
        _stopButton.Location = new Point(150, 6);
        _stopButton.Enabled = false;
        _stopButton.Click += (_, _) => _cts?.Cancel();

        var intLabel = new Label { Text = "INTERVAL(s):", Location = new Point(260, 14), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _intervalBox = new NumericUpDown
        {
            Location = new Point(400, 11),
            Size = new Size(80, 24),
            Minimum = 10,
            Maximum = 3600,
            Value = 60,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };

        _statusLabel = new Label
        {
            Text = "STATUS: IDLE",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontLarge,
            Location = new Point(500, 12),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        _uptimeLabel = new Label
        {
            Text = "",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            Location = new Point(740, 15),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        controlPanel.Controls.AddRange([_startButton, _stopButton, intLabel, _intervalBox, _statusLabel, _uptimeLabel]);

        // Progress
        _progressBar = new ProgressBar
        {
            Location = new Point(20, 103),
            Size = new Size(1000, 6),
            Style = ProgressBarStyle.Continuous,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        // Live grid
        _liveGrid = new DataGridView
        {
            Location = new Point(20, 118),
            Size = new Size(1000, 260),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        NeonTheme.StyleDataGridView(_liveGrid);
        _liveGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Cycle", HeaderText = "CYCLE", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "Url", HeaderText = "URL", FillWeight = 30 },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "HTTP", FillWeight = 7 },
            new DataGridViewTextBoxColumn { Name = "TTFB", HeaderText = "TTFB(ms)", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Total", HeaderText = "TOTAL(ms)", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Alerts", HeaderText = "ALERTS", FillWeight = 8 },
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "TIME", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "RESULT", FillWeight = 8 },
        ]);

        // Log
        var logLabel = NeonTheme.CreateLabel("▸ LIVE TRACE LOG", isHeader: false);
        logLabel.ForeColor = NeonTheme.TextAccent;
        logLabel.Location = new Point(20, 388);

        _logView = new NeonRichTextLog
        {
            Location = new Point(20, 413),
            Size = new Size(1000, 240),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        // Uptime timer
        _uptimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _uptimeTimer.Tick += (_, _) =>
        {
            if (_cts is not null)
            {
                var elapsed = DateTime.Now - _startTime;
                _uptimeLabel.Text = $"UPTIME: {elapsed:hh\\:mm\\:ss} | CYCLES: {_cycleCount}";
            }
        };

        Controls.AddRange([titleLabel, controlPanel, _progressBar, _liveGrid, logLabel, _logView]);
        NeonTheme.Apply(this);

        // Subscribe to logger for real-time monitoring output
        _logger.LogEntryAdded += OnLogEntry;
    }

    private void OnLogEntry(DateTime timestamp, Severity severity, string message)
    {
        var color = severity switch
        {
            Severity.Warning => NeonTheme.Warning,
            Severity.Critical => NeonTheme.Critical,
            _ => NeonTheme.TextPrimary,
        };
        _logView.AppendLog($"[{timestamp:HH:mm:ss.fff}] {message}", color);
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        await _urlManager.LoadAsync();
        var targets = _urlManager.Targets.Where(t => t.IsEnabled).ToList();

        if (targets.Count == 0)
        {
            _logView.AppendLog("No enabled targets. Add URLs first.", NeonTheme.Warning);
            return;
        }

        _startButton.Enabled = false;
        _stopButton.Enabled = true;
        _intervalBox.Enabled = false;
        _statusLabel.Text = "STATUS: RUNNING";
        _statusLabel.ForeColor = NeonTheme.TextAccent;
        _cycleCount = 0;
        _startTime = DateTime.Now;
        _uptimeTimer.Start();

        _cts = new CancellationTokenSource();
        var intervalMs = (int)_intervalBox.Value * 1000;

        _logView.AppendLog($"Live monitoring started — {targets.Count} targets, interval {_intervalBox.Value}s", NeonTheme.TextAccent);
        _logger.Info("Live monitoring started");

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _cycleCount++;
                _logView.AppendLog($"─── Cycle #{_cycleCount} ───", NeonTheme.TextDim);

                var progress = new Progress<Application.Dto.MonitoringProgress>(p =>
                {
                    _progressBar.Maximum = Math.Max(1, p.TotalCount);
                    _progressBar.Value = Math.Min(p.CurrentIndex, p.TotalCount);
                });

                var summary = await Task.Run(
                    () => _orchestrator.RunAsync(_config, targets, progress, _cts.Token),
                    _cts.Token);

                // Populate live grid
                foreach (var result in summary.Results)
                {
                    var m = result.Metrics;
                    var alertCount = result.Alerts.Count;
                    var rowIndex = _liveGrid.Rows.Add(
                        _cycleCount.ToString(),
                        result.Url,
                        m?.StatusCode.ToString() ?? "—",
                        m?.TimeToFirstByteMs.ToString("F0") ?? "—",
                        m?.TotalTimeMs.ToString("F0") ?? "—",
                        alertCount.ToString(),
                        DateTime.Now.ToString("HH:mm:ss"),
                        result.Success ? "OK" : "FAIL");

                    var row = _liveGrid.Rows[rowIndex];
                    if (result.Alerts.Any(a => a.Severity == Severity.Critical))
                        row.DefaultCellStyle.ForeColor = NeonTheme.Critical;
                    else if (alertCount > 0)
                        row.DefaultCellStyle.ForeColor = NeonTheme.Warning;
                }

                // Trim old rows
                while (_liveGrid.Rows.Count > 500)
                    _liveGrid.Rows.RemoveAt(0);

                _logView.AppendLog($"Cycle #{_cycleCount} complete — {summary.Results.Count} results, waiting {_intervalBox.Value}s", NeonTheme.Success);

                await Task.Delay(intervalMs, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "STATUS: STOPPED";
            _statusLabel.ForeColor = NeonTheme.Warning;
            _logView.AppendLog("Live monitoring stopped", NeonTheme.Warning);
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "STATUS: ERROR";
            _statusLabel.ForeColor = NeonTheme.Critical;
            _logView.AppendLog($"Error: {ex.Message}", NeonTheme.Critical);
            _logger.Error("Live monitoring error", ex);
        }
        finally
        {
            _startButton.Enabled = true;
            _stopButton.Enabled = false;
            _intervalBox.Enabled = true;
            _uptimeTimer.Stop();
            _cts.Dispose();
            _cts = null;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _logger.LogEntryAdded -= OnLogEntry;
        _cts?.Cancel();
        _cts?.Dispose();
        _uptimeTimer.Dispose();
        base.OnFormClosed(e);
    }
}
