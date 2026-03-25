using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

/// <summary>
/// Update notification dialog — shows version comparison, release notes,
/// download progress, and confirm/later/skip options.
/// </summary>
public sealed class UpdateDialog : Form
{
    private readonly ILocalizationService _loc;
    private readonly IUpdateService _updateService;
    private readonly IVersionProvider _versionProvider;
    private readonly UpdateInfo _update;
    private readonly ISettingsService _settings;
    private readonly IAppLogger _logger;

    private readonly Label _headerLabel;
    private readonly Label _currentVersionLabel;
    private readonly Label _latestVersionLabel;
    private readonly Label _sizeLabel;
    private readonly RichTextBox _releaseNotesBox;
    private readonly ProgressBar _progressBar;
    private readonly Label _progressLabel;
    private readonly Button _updateBtn;
    private readonly Button _laterBtn;
    private readonly Button _skipBtn;

    public UpdateDialog(
        UpdateInfo update,
        ILocalizationService loc,
        IUpdateService updateService,
        IVersionProvider versionProvider,
        ISettingsService settings,
        IAppLogger logger)
    {
        _update = update;
        _loc = loc;
        _updateService = updateService;
        _versionProvider = versionProvider;
        _settings = settings;
        _logger = logger;

        Text = _loc.Get("Update.Title");
        Size = new Size(540, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        int y = 15;
        const int margin = 20;
        int contentWidth = ClientSize.Width - margin * 2;

        // Logo
        var logo = new BlancoLogo { CurrentSize = BlancoLogo.LogoSize.Small, Location = new Point(margin, y) };
        Controls.Add(logo);

        // Header
        _headerLabel = new Label
        {
            Text = _loc.Get("Update.Header"),
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            ForeColor = NeonTheme.TextAccent,
            AutoSize = false,
            Size = new Size(contentWidth - 60, 40),
            Location = new Point(margin + 56, y + 4),
            BackColor = Color.Transparent,
        };
        Controls.Add(_headerLabel);
        y += 55;

        // Separator
        Controls.Add(CreateSeparator(margin, y, contentWidth));
        y += 12;

        // Current version
        _currentVersionLabel = CreateInfoLabel($"{_loc.Get("Update.Current")}  {_versionProvider.DisplayVersion}", margin, y);
        Controls.Add(_currentVersionLabel);
        y += 22;

        // Latest version
        _latestVersionLabel = new Label
        {
            Text = $"{_loc.Get("Update.Latest")}  {_update.Version}",
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.Success,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        Controls.Add(_latestVersionLabel);
        y += 22;

        // Size
        _sizeLabel = CreateInfoLabel($"{_loc.Get("Update.Size")}  {FormatBytes(_update.SizeBytes)}", margin, y);
        Controls.Add(_sizeLabel);
        y += 28;

        // Release notes header
        var notesHeader = new Label
        {
            Text = _loc.Get("Update.ReleaseNotes"),
            Font = new Font("Consolas", 9.5f, FontStyle.Bold),
            ForeColor = NeonTheme.TextAccent,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        Controls.Add(notesHeader);
        y += 20;

        // Release notes box
        _releaseNotesBox = new RichTextBox
        {
            Text = _update.ReleaseNotes,
            Location = new Point(margin, y),
            Size = new Size(contentWidth, 120),
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFontSmall,
            ScrollBars = RichTextBoxScrollBars.Vertical,
        };
        Controls.Add(_releaseNotesBox);
        y += 130;

        // Progress bar (hidden initially)
        _progressBar = new ProgressBar
        {
            Location = new Point(margin, y),
            Size = new Size(contentWidth, 20),
            Style = ProgressBarStyle.Continuous,
            Visible = false,
        };
        Controls.Add(_progressBar);

        _progressLabel = new Label
        {
            Text = "",
            Location = new Point(margin, y + 22),
            Size = new Size(contentWidth, 18),
            Font = NeonTheme.MonoFontSmall,
            ForeColor = NeonTheme.TextDim,
            BackColor = Color.Transparent,
            Visible = false,
        };
        Controls.Add(_progressLabel);
        y += 14;

        // Separator
        Controls.Add(CreateSeparator(margin, y, contentWidth));
        y += 16;

        // Buttons
        _updateBtn = NeonTheme.CreateButton(_loc.Get("Update.BtnUpdate"), 160, 34);
        _updateBtn.ForeColor = NeonTheme.Success;
        _updateBtn.Location = new Point(margin, y);
        _updateBtn.Click += UpdateBtn_Click;

        _laterBtn = NeonTheme.CreateButton(_loc.Get("Update.BtnLater"), 100, 34);
        _laterBtn.Location = new Point(margin + 170, y);
        _laterBtn.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _skipBtn = NeonTheme.CreateButton(_loc.Get("Update.BtnSkip"), 180, 34);
        _skipBtn.Location = new Point(ClientSize.Width - margin - 180, y);
        _skipBtn.ForeColor = NeonTheme.TextDim;
        _skipBtn.Click += (_, _) =>
        {
            _settings.LastSkippedVersion = _update.Version;
            _settings.Save();
            DialogResult = DialogResult.Ignore;
            Close();
        };

        Controls.AddRange([_updateBtn, _laterBtn, _skipBtn]);

        NeonTheme.Apply(this);

        // Restore specific overrides
        _releaseNotesBox.BackColor = NeonTheme.Surface;
        _latestVersionLabel.ForeColor = NeonTheme.Success;
        _updateBtn.ForeColor = NeonTheme.Success;
        _skipBtn.ForeColor = NeonTheme.TextDim;
    }

    private async void UpdateBtn_Click(object? sender, EventArgs e)
    {
        _updateBtn.Enabled = false;
        _laterBtn.Enabled = false;
        _skipBtn.Enabled = false;
        _progressBar.Visible = true;
        _progressLabel.Visible = true;

        try
        {
            var progress = new Progress<int>(percent =>
            {
                if (InvokeRequired)
                    BeginInvoke(() => UpdateProgress(percent));
                else
                    UpdateProgress(percent);
            });

            var packagePath = await _updateService.DownloadUpdateAsync(_update, progress);

            _progressLabel.Text = _loc.Get("Update.Applying");
            _progressBar.Value = 100;

            await Task.Delay(500);

            _updateService.ApplyUpdateAndRestart(packagePath);
        }
        catch (Exception ex)
        {
            _logger.Error("Update download/apply failed", ex);
            _progressLabel.Text = _loc.Get("Update.Failed", ex.Message);
            _progressLabel.ForeColor = NeonTheme.Critical;

            _updateBtn.Enabled = true;
            _laterBtn.Enabled = true;
            _skipBtn.Enabled = true;
        }
    }

    private void UpdateProgress(int percent)
    {
        _progressBar.Value = Math.Clamp(percent, 0, 100);
        _progressLabel.Text = _loc.Get("Update.Downloading", percent);
    }

    private static Label CreateInfoLabel(string text, int x, int y) => new()
    {
        Text = text,
        Font = NeonTheme.MonoFont,
        ForeColor = NeonTheme.TextPrimary,
        AutoSize = true,
        Location = new Point(x, y),
        BackColor = Color.Transparent,
    };

    private static Panel CreateSeparator(int x, int y, int width) => new()
    {
        Location = new Point(x, y),
        Size = new Size(width, 1),
        BackColor = NeonTheme.Border,
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
