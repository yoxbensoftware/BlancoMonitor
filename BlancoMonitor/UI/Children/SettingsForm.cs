using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.Domain.ValueObjects;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class SettingsForm : Form
{
    private readonly UrlKeywordSetManager _manager;
    private readonly IAppLogger _logger;
    private readonly ILocalizationService _loc;
    private readonly ISettingsService _settings;

    // General settings
    private readonly NumericUpDown _timeoutBox;
    private readonly NumericUpDown _concurrencyBox;
    private readonly NumericUpDown _delayBox;
    private readonly TextBox _userAgentBox;
    private readonly CheckBox _screenshotCheck;
    private readonly TextBox _ignoreBox;

    // Threshold settings
    private readonly NumericUpDown _ttfbWarn;
    private readonly NumericUpDown _ttfbCrit;
    private readonly NumericUpDown _totalWarn;
    private readonly NumericUpDown _totalCrit;
    private readonly NumericUpDown _dlWarn;
    private readonly NumericUpDown _dlCrit;

    // Language & updates
    private readonly ComboBox _languageCombo;
    private readonly CheckBox _checkUpdatesBox;

    private readonly Button _saveButton;

    public SettingsForm(UrlKeywordSetManager manager, IAppLogger logger,
        ILocalizationService loc, ISettingsService settings)
    {
        _manager = manager;
        _logger = logger;
        _loc = loc;
        _settings = settings;

        Text = _loc.Get("Settings.Title");
        Size = new Size(800, 700);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(50, 50);
        AutoScroll = true;
        MinimumSize = new Size(700, 550);

        var titleLabel = NeonTheme.CreateLabel(_loc.Get("Settings.Header"), isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // === General group ===
        var generalGroup = CreateGroup(_loc.Get("Settings.General"), 20, 50, 750, 210);

        AddLabeledControl(generalGroup, _loc.Get("Settings.Timeout"), 30,
            _timeoutBox = CreateNumericUpDown(30, 1, 300));
        AddLabeledControl(generalGroup, _loc.Get("Settings.Concurrency"), 60,
            _concurrencyBox = CreateNumericUpDown(2, 1, 20));
        AddLabeledControl(generalGroup, _loc.Get("Settings.Delay"), 90,
            _delayBox = CreateNumericUpDown(500, 0, 10000));
        AddLabeledControl(generalGroup, _loc.Get("Settings.UserAgent"), 120,
            _userAgentBox = CreateTextBox("BlancoMonitor/1.0"));

        _screenshotCheck = new CheckBox
        {
            Text = _loc.Get("Settings.Screenshots"),
            Location = new Point(200, 152),
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = NeonTheme.TextPrimary,
            BackColor = Color.Transparent,
        };
        generalGroup.Controls.Add(_screenshotCheck);

        AddLabeledControl(generalGroup, _loc.Get("Settings.Ignore"), 180,
            _ignoreBox = CreateTextBox("*.pdf, *.zip, *.exe"));

        // === Thresholds group ===
        var thresholdGroup = CreateGroup(_loc.Get("Settings.Thresholds"), 20, 275, 750, 180);

        var warnLabel = new Label { Text = _loc.Get("Settings.Warning"), Location = new Point(310, 22), AutoSize = true, ForeColor = NeonTheme.Warning, BackColor = Color.Transparent, Font = NeonTheme.MonoFontSmall };
        var critLabel = new Label { Text = _loc.Get("Settings.Critical"), Location = new Point(470, 22), AutoSize = true, ForeColor = NeonTheme.Critical, BackColor = Color.Transparent, Font = NeonTheme.MonoFontSmall };
        thresholdGroup.Controls.AddRange([warnLabel, critLabel]);

        AddThresholdRow(thresholdGroup, _loc.Get("Settings.Ttfb"), 45,
            _ttfbWarn = CreateNumericUpDown(1000, 0, 60000),
            _ttfbCrit = CreateNumericUpDown(5000, 0, 60000));
        AddThresholdRow(thresholdGroup, _loc.Get("Settings.TotalTime"), 75,
            _totalWarn = CreateNumericUpDown(3000, 0, 60000),
            _totalCrit = CreateNumericUpDown(10000, 0, 60000));
        AddThresholdRow(thresholdGroup, _loc.Get("Settings.Download"), 105,
            _dlWarn = CreateNumericUpDown(2000, 0, 60000),
            _dlCrit = CreateNumericUpDown(8000, 0, 60000));

        // === Language & Updates group ===
        var langGroup = CreateGroup(_loc.Get("Settings.LanguageGroup"), 20, 470, 750, 110);

        var langLabel = new Label
        {
            Text = _loc.Get("Settings.Language"),
            Location = new Point(15, 35),
            Size = new Size(170, 20),
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
        };

        _languageCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(200, 32),
            Size = new Size(220, 24),
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };

        foreach (var lang in _loc.AvailableLanguages)
            _languageCombo.Items.Add($"{_loc.GetLanguageDisplayName(lang)}  ({lang})");

        // Select current language
        var currentIdx = _loc.AvailableLanguages.ToList().IndexOf(_loc.CurrentLanguage);
        if (currentIdx >= 0) _languageCombo.SelectedIndex = currentIdx;

        _checkUpdatesBox = new CheckBox
        {
            Text = _loc.Get("Settings.CheckUpdates"),
            Location = new Point(200, 65),
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            ForeColor = NeonTheme.TextPrimary,
            BackColor = Color.Transparent,
            Checked = _settings.CheckForUpdatesOnStartup,
        };

        langGroup.Controls.AddRange([langLabel, _languageCombo, _checkUpdatesBox]);

        // Save button
        _saveButton = NeonTheme.CreateButton(_loc.Get("Btn.Save"), 160, 40);
        _saveButton.Location = new Point(20, 600);
        _saveButton.Click += SaveButton_Click;

        Controls.AddRange([titleLabel, generalGroup, thresholdGroup, langGroup, _saveButton]);
        NeonTheme.Apply(this);

        // Restore combo styling after theme
        _languageCombo.BackColor = NeonTheme.Surface;
        _languageCombo.ForeColor = NeonTheme.TextPrimary;

        Load += async (_, _) => await LoadSettings();
    }

    private async Task LoadSettings()
    {
        await _manager.LoadAsync();
        var config = _manager.GetConfiguration();

        _timeoutBox.Value = config.DefaultTimeoutSeconds;
        _concurrencyBox.Value = config.MaxConcurrentRequests;
        _delayBox.Value = config.DelayBetweenRequestsMs;
        _userAgentBox.Text = config.UserAgent;
        _screenshotCheck.Checked = config.ScreenshotEnabled;
        _ignoreBox.Text = string.Join(", ", config.IgnorePatterns);

        if (config.Thresholds.TryGetValue("TimeToFirstByteMs", out var ttfb))
        {
            _ttfbWarn.Value = (decimal)ttfb.WarningValue;
            _ttfbCrit.Value = (decimal)ttfb.CriticalValue;
        }
        if (config.Thresholds.TryGetValue("TotalTimeMs", out var total))
        {
            _totalWarn.Value = (decimal)total.WarningValue;
            _totalCrit.Value = (decimal)total.CriticalValue;
        }
        if (config.Thresholds.TryGetValue("ContentDownloadMs", out var dl))
        {
            _dlWarn.Value = (decimal)dl.WarningValue;
            _dlCrit.Value = (decimal)dl.CriticalValue;
        }
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        var config = _manager.GetConfiguration();
        config.DefaultTimeoutSeconds = (int)_timeoutBox.Value;
        config.MaxConcurrentRequests = (int)_concurrencyBox.Value;
        config.DelayBetweenRequestsMs = (int)_delayBox.Value;
        config.UserAgent = _userAgentBox.Text;
        config.ScreenshotEnabled = _screenshotCheck.Checked;
        config.IgnorePatterns = _ignoreBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        config.Thresholds["TimeToFirstByteMs"] = new Threshold("TimeToFirstByteMs", (double)_ttfbWarn.Value, (double)_ttfbCrit.Value);
        config.Thresholds["TotalTimeMs"] = new Threshold("TotalTimeMs", (double)_totalWarn.Value, (double)_totalCrit.Value);
        config.Thresholds["ContentDownloadMs"] = new Threshold("ContentDownloadMs", (double)_dlWarn.Value, (double)_dlCrit.Value);

        await _manager.SaveConfigurationAsync(config);

        // Save language & update preferences
        var selectedLangIdx = _languageCombo.SelectedIndex;
        if (selectedLangIdx >= 0 && selectedLangIdx < _loc.AvailableLanguages.Count)
        {
            var newLang = _loc.AvailableLanguages[selectedLangIdx];
            if (newLang != _settings.Language)
            {
                _settings.Language = newLang;
                MessageBox.Show(
                    _loc.Get("Settings.LanguageRestart"),
                    _loc.Get("Settings.Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.None);
            }
        }

        _settings.CheckForUpdatesOnStartup = _checkUpdatesBox.Checked;
        _settings.Save();

        _logger.Info(_loc.Get("Settings.Saved"));
    }

    private static GroupBox CreateGroup(string text, int x, int y, int w, int h)
    {
        return new GroupBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, h),
            ForeColor = NeonTheme.TextAccent,
            Font = NeonTheme.MonoFontSmall,
        };
    }

    private static void AddLabeledControl(Control parent, string labelText, int y, Control control)
    {
        var label = new Label
        {
            Text = labelText,
            Location = new Point(15, y + 3),
            Size = new Size(170, 20),
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
        };
        control.Location = new Point(200, y);
        parent.Controls.AddRange([label, control]);
    }

    private static void AddThresholdRow(Control parent, string label, int y, NumericUpDown warn, NumericUpDown crit)
    {
        var lbl = new Label
        {
            Text = label,
            Location = new Point(15, y + 3),
            Size = new Size(170, 20),
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
        };
        warn.Location = new Point(300, y);
        crit.Location = new Point(460, y);
        parent.Controls.AddRange([lbl, warn, crit]);
    }

    private static NumericUpDown CreateNumericUpDown(decimal value, decimal min, decimal max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Size = new Size(120, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }

    private static TextBox CreateTextBox(string text)
    {
        return new TextBox
        {
            Text = text,
            Size = new Size(400, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }
}
