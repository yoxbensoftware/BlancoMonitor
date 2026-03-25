using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

/// <summary>
/// Step-by-step wizard for creating a new monitoring run.
/// Steps: 1. Select Targets → 2. Configure → 3. Thresholds → 4. Review → 5. Run
/// </summary>
public sealed class RunWizardForm : Form
{
    private readonly UrlKeywordSetManager _urlManager;
    private readonly IAppLogger _logger;
    private readonly AppConfiguration _config;

    private readonly Panel _stepIndicatorPanel;
    private readonly Panel _contentPanel;
    private readonly Button _backButton;
    private readonly Button _nextButton;
    private readonly Button _cancelButton;
    private readonly Label _stepTitleLabel;

    private int _currentStep;
    private const int TotalSteps = 5;
    private readonly string[] _stepTitles =
    [
        "SELECT TARGETS",
        "CONFIGURATION",
        "THRESHOLDS",
        "REVIEW",
        "RUNNING",
    ];

    // Step 1 — target selection
    private readonly CheckedListBox _targetList = null!;

    // Step 2 — config
    private readonly NumericUpDown _timeoutBox = null!;
    private readonly NumericUpDown _concurrencyBox = null!;
    private readonly NumericUpDown _delayBox = null!;
    private readonly CheckBox _screenshotCheck = null!;

    // Step 3 — thresholds
    private readonly NumericUpDown _ttfbWarnBox = null!;
    private readonly NumericUpDown _ttfbCritBox = null!;
    private readonly NumericUpDown _totalWarnBox = null!;
    private readonly NumericUpDown _totalCritBox = null!;

    // Result
    public List<MonitorTarget> SelectedTargets { get; } = [];
    public bool WizardCompleted { get; private set; }

    public RunWizardForm(UrlKeywordSetManager urlManager, IAppLogger logger, AppConfiguration config)
    {
        _urlManager = urlManager;
        _logger = logger;
        _config = config;

        Text = "New Monitoring Run — Wizard";
        Size = new Size(920, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Step indicator (top bar)
        _stepIndicatorPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(920, 55),
            BackColor = NeonTheme.Surface,
            Dock = DockStyle.Top,
        };
        BuildStepIndicator();

        // Step title
        _stepTitleLabel = new Label
        {
            Text = "▸ STEP 1: SELECT TARGETS",
            Font = NeonTheme.HeaderFont,
            ForeColor = NeonTheme.TextAccent,
            Location = new Point(20, 65),
            AutoSize = true,
            BackColor = Color.Transparent,
        };

        // Content panel (swapped per step)
        _contentPanel = new Panel
        {
            Location = new Point(20, 95),
            Size = new Size(860, 380),
            BackColor = NeonTheme.Background,
        };

        // Navigation buttons
        _backButton = NeonTheme.CreateButton("◀ BACK", 100, 34);
        _backButton.Location = new Point(20, 490);
        _backButton.Click += (_, _) => NavigateStep(-1);
        _backButton.Enabled = false;

        _nextButton = NeonTheme.CreateButton("NEXT ▶", 120, 34);
        _nextButton.Location = new Point(660, 490);
        _nextButton.Click += (_, _) => NavigateStep(1);

        _cancelButton = NeonTheme.CreateButton("CANCEL", 100, 34);
        _cancelButton.Location = new Point(790, 490);
        _cancelButton.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        // Initialize step controls
        _targetList = new CheckedListBox
        {
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFont,
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true,
            Size = new Size(830, 340),
            Location = new Point(10, 10),
        };

        _timeoutBox = CreateNumericUpDown(_config.DefaultTimeoutSeconds, 1, 300);
        _concurrencyBox = CreateNumericUpDown(_config.MaxConcurrentRequests, 1, 20);
        _delayBox = CreateNumericUpDown(_config.DelayBetweenRequestsMs, 0, 10000);
        _screenshotCheck = new CheckBox
        {
            Text = "Enable screenshots",
            ForeColor = NeonTheme.TextPrimary,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            AutoSize = true,
            Checked = _config.ScreenshotEnabled,
        };

        _ttfbWarnBox = CreateNumericUpDown(1000, 0, 60000);
        _ttfbCritBox = CreateNumericUpDown(5000, 0, 60000);
        _totalWarnBox = CreateNumericUpDown(3000, 0, 60000);
        _totalCritBox = CreateNumericUpDown(10000, 0, 60000);

        Controls.AddRange([_stepIndicatorPanel, _stepTitleLabel, _contentPanel, _backButton, _nextButton, _cancelButton]);
        NeonTheme.Apply(this);

        Load += async (_, _) =>
        {
            await LoadTargets();
            ShowStep(0);
        };
    }

    private void BuildStepIndicator()
    {
        _stepIndicatorPanel.Controls.Clear();
        var stepWidth = 170;
        for (int i = 0; i < TotalSteps; i++)
        {
            var isActive = i == _currentStep;
            var isPast = i < _currentStep;

            var lbl = new Label
            {
                Text = $" {i + 1} ",
                Font = new Font("Consolas", 12f, FontStyle.Bold),
                ForeColor = isActive ? NeonTheme.TextAccent : isPast ? NeonTheme.TextPrimary : NeonTheme.TextDim,
                BackColor = isActive ? NeonTheme.SurfaceHover : Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(32, 32),
                Location = new Point(15 + i * stepWidth, 12),
            };

            var nameLbl = new Label
            {
                Text = _stepTitles[i],
                Font = NeonTheme.MonoFontSmall,
                ForeColor = isActive ? NeonTheme.TextAccent : isPast ? NeonTheme.TextPrimary : NeonTheme.TextDim,
                AutoSize = true,
                Location = new Point(50 + i * stepWidth, 18),
                BackColor = Color.Transparent,
            };

            _stepIndicatorPanel.Controls.AddRange([lbl, nameLbl]);

            // Connector line
            if (i < TotalSteps - 1)
            {
                var connector = new Panel
                {
                    Location = new Point(50 + i * stepWidth + 100, 27),
                    Size = new Size(40, 2),
                    BackColor = isPast ? NeonTheme.TextPrimary : NeonTheme.Border,
                };
                _stepIndicatorPanel.Controls.Add(connector);
            }
        }
    }

    private void NavigateStep(int direction)
    {
        if (direction > 0 && !ValidateCurrentStep())
            return;

        _currentStep = Math.Clamp(_currentStep + direction, 0, TotalSteps - 1);
        ShowStep(_currentStep);
    }

    private void ShowStep(int step)
    {
        _currentStep = step;
        _contentPanel.Controls.Clear();
        _stepTitleLabel.Text = $"▸ STEP {step + 1}: {_stepTitles[step]}";
        _backButton.Enabled = step > 0 && step < TotalSteps - 1;
        _nextButton.Text = step == TotalSteps - 2 ? "▶ START" : "NEXT ▶";
        _nextButton.Visible = step < TotalSteps - 1;
        _cancelButton.Visible = step < TotalSteps - 1;
        BuildStepIndicator();

        switch (step)
        {
            case 0: BuildStep1_Targets(); break;
            case 1: BuildStep2_Config(); break;
            case 2: BuildStep3_Thresholds(); break;
            case 3: BuildStep4_Review(); break;
            case 4: BuildStep5_Complete(); break;
        }
    }

    // ── Step 1: Select Targets ──
    private void BuildStep1_Targets()
    {
        var hint = new Label
        {
            Text = "Select the URLs to monitor in this run:",
            ForeColor = NeonTheme.TextDim,
            Font = NeonTheme.MonoFontSmall,
            AutoSize = true,
            Location = new Point(10, 4),
            BackColor = Color.Transparent,
        };

        var selectAllBtn = NeonTheme.CreateButton("SELECT ALL", 130, 28);
        selectAllBtn.Location = new Point(710, 0);
        selectAllBtn.Click += (_, _) =>
        {
            for (int i = 0; i < _targetList.Items.Count; i++)
                _targetList.SetItemChecked(i, true);
        };

        _targetList.Location = new Point(10, 32);
        _targetList.Size = new Size(830, 330);

        _contentPanel.Controls.AddRange([hint, selectAllBtn, _targetList]);
    }

    // ── Step 2: Configuration ──
    private void BuildStep2_Config()
    {
        var y = 10;
        AddLabeledControl("Timeout (seconds):", _timeoutBox, ref y);
        AddLabeledControl("Max Concurrency:", _concurrencyBox, ref y);
        AddLabeledControl("Delay between requests (ms):", _delayBox, ref y);

        _screenshotCheck.Location = new Point(400, y + 10);
        _contentPanel.Controls.Add(_screenshotCheck);
    }

    // ── Step 3: Thresholds ──
    private void BuildStep3_Thresholds()
    {
        var warnHeader = new Label { Text = "WARNING", Location = new Point(250, 10), AutoSize = true, ForeColor = NeonTheme.Warning, Font = NeonTheme.MonoFontSmall, BackColor = Color.Transparent };
        var critHeader = new Label { Text = "CRITICAL", Location = new Point(400, 10), AutoSize = true, ForeColor = NeonTheme.Critical, Font = NeonTheme.MonoFontSmall, BackColor = Color.Transparent };
        _contentPanel.Controls.AddRange([warnHeader, critHeader]);

        var y = 40;
        AddThresholdRow("TTFB (ms):", _ttfbWarnBox, _ttfbCritBox, ref y);
        AddThresholdRow("Total Time (ms):", _totalWarnBox, _totalCritBox, ref y);
    }

    // ── Step 4: Review ──
    private void BuildStep4_Review()
    {
        var review = new RichTextBox
        {
            Location = new Point(10, 10),
            Size = new Size(830, 350),
            ReadOnly = true,
            BackColor = NeonTheme.Background,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFont,
            BorderStyle = BorderStyle.None,
        };

        var checkedCount = _targetList.CheckedItems.Count;
        review.AppendText($"═══ RUN SUMMARY ═══\n\n");
        review.AppendText($"  Targets:      {checkedCount}\n");
        review.AppendText($"  Timeout:      {_timeoutBox.Value}s\n");
        review.AppendText($"  Concurrency:  {_concurrencyBox.Value}\n");
        review.AppendText($"  Delay:        {_delayBox.Value}ms\n");
        review.AppendText($"  Screenshots:  {(_screenshotCheck.Checked ? "Yes" : "No")}\n\n");
        review.AppendText($"  TTFB Warn:    {_ttfbWarnBox.Value}ms\n");
        review.AppendText($"  TTFB Crit:    {_ttfbCritBox.Value}ms\n");
        review.AppendText($"  Total Warn:   {_totalWarnBox.Value}ms\n");
        review.AppendText($"  Total Crit:   {_totalCritBox.Value}ms\n\n");

        review.AppendText($"  TARGETS:\n");
        foreach (var item in _targetList.CheckedItems)
            review.AppendText($"    ► {item}\n");

        _contentPanel.Controls.Add(review);
    }

    // ── Step 5: Complete ──
    private void BuildStep5_Complete()
    {
        // Collect selected targets
        SelectedTargets.Clear();
        if (_targetList.Tag is List<MonitorTarget> allTargets)
        {
            foreach (int idx in _targetList.CheckedIndices)
            {
                if (idx < allTargets.Count)
                    SelectedTargets.Add(allTargets[idx]);
            }
        }

        WizardCompleted = true;

        var doneLabel = new Label
        {
            Text = "✓ WIZARD COMPLETE",
            Font = NeonTheme.HeaderFont,
            ForeColor = NeonTheme.Success,
            AutoSize = true,
            Location = new Point(10, 30),
            BackColor = Color.Transparent,
        };

        var info = new Label
        {
            Text = $"Monitoring run configured with {SelectedTargets.Count} target(s).\n" +
                   "The monitoring form will now open to execute this run.",
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(10, 70),
            BackColor = Color.Transparent,
        };

        var closeBtn = NeonTheme.CreateButton("▶ LAUNCH", 140, 40);
        closeBtn.Location = new Point(10, 140);
        closeBtn.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

        _contentPanel.Controls.AddRange([doneLabel, info, closeBtn]);
    }

    private bool ValidateCurrentStep()
    {
        return _currentStep switch
        {
            0 when _targetList.CheckedItems.Count == 0 =>
                ShowValidation("Select at least one target URL"),
            _ => true,
        };
    }

    private bool ShowValidation(string message)
    {
        _logger.Warning(message);
        MessageBox.Show(message, "Validation", MessageBoxButtons.OK, MessageBoxIcon.None);
        return false;
    }

    private async Task LoadTargets()
    {
        await _urlManager.LoadAsync();
        _targetList.Items.Clear();
        foreach (var t in _urlManager.Targets)
        {
            _targetList.Items.Add($"{t.Name} — {t.Url}", t.IsEnabled);
        }
        _targetList.Tag = _urlManager.Targets.ToList();
    }

    private void AddLabeledControl(string labelText, Control control, ref int y)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(10, y + 3),
            AutoSize = true,
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
        };
        control.Location = new Point(400, y);
        _contentPanel.Controls.AddRange([label, control]);
        y += 35;
    }

    private void AddThresholdRow(string labelText, NumericUpDown warnBox, NumericUpDown critBox, ref int y)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(10, y + 3),
            AutoSize = true,
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
        };
        warnBox.Location = new Point(250, y);
        critBox.Location = new Point(400, y);
        _contentPanel.Controls.AddRange([label, warnBox, critBox]);
        y += 35;
    }

    private static NumericUpDown CreateNumericUpDown(int value, int min, int max)
    {
        return new NumericUpDown
        {
            Size = new Size(120, 24),
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }
}
