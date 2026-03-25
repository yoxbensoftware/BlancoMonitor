using BlancoMonitor.Application.Services;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Children;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI;

public sealed class MdiParentForm : Form
{
    private readonly MenuStrip _menuStrip;
    private readonly StatusStrip _statusStrip;
    private readonly ToolStripStatusLabel _statusLabel;
    private readonly ToolStripStatusLabel _timeLabel;
    private readonly System.Windows.Forms.Timer _clockTimer;

    // Services (injected)
    private readonly MonitoringOrchestrator _orchestrator;
    private readonly UrlKeywordSetManager _urlManager;
    private readonly IAppLogger _logger;
    private readonly IHistoricalStore _historicalStore;
    private readonly IBlancoDatabase _database;
    private readonly IDiscoveryEngine _discoveryEngine;
    private readonly Domain.Entities.AppConfiguration _config;
    private readonly ILocalizationService _loc;
    private readonly IVersionProvider _version;
    private readonly ISettingsService _settings;
    private readonly IUpdateService? _updateService;
    private readonly IHelpContentService _helpContent;

    public MdiParentForm(
        MonitoringOrchestrator orchestrator,
        UrlKeywordSetManager urlManager,
        IAppLogger logger,
        IHistoricalStore historicalStore,
        IBlancoDatabase database,
        IDiscoveryEngine discoveryEngine,
        Domain.Entities.AppConfiguration config,
        ILocalizationService loc,
        IVersionProvider version,
        ISettingsService settings,
        IHelpContentService helpContent,
        IUpdateService? updateService = null)
    {
        _orchestrator = orchestrator;
        _urlManager = urlManager;
        _logger = logger;
        _historicalStore = historicalStore;
        _database = database;
        _discoveryEngine = discoveryEngine;
        _config = config;
        _loc = loc;
        _version = version;
        _settings = settings;
        _helpContent = helpContent;
        _updateService = updateService;

        Text = _loc.Get("App.Title", _version.DisplayVersion);
        Size = new Size(1440, 900);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;
        Icon = SystemIcons.Shield;

        // Developer tag — top-right corner
        var devLabel = new Label
        {
            Text = "Oz",
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            ForeColor = NeonTheme.TextAccent,
            BackColor = Color.Transparent,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        devLabel.Location = new Point(ClientSize.Width - devLabel.PreferredWidth - 16, 4);
        Controls.Add(devLabel);
        devLabel.BringToFront();

        // MDI client area background + embedded dashboard
        foreach (Control ctrl in Controls)
        {
            if (ctrl is MdiClient mdiClient)
            {
                mdiClient.BackColor = NeonTheme.Background;
                BuildDashboardBackground(mdiClient);
                break;
            }
        }

        // Menu
        _menuStrip = BuildMenuStrip();
        Controls.Add(_menuStrip);
        MainMenuStrip = _menuStrip;

        // Status bar
        _statusLabel = new ToolStripStatusLabel(_loc.Get("Status.Ready"))
        {
            ForeColor = NeonTheme.TextPrimary,
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _timeLabel = new ToolStripStatusLabel(DateTime.Now.ToString("HH:mm:ss"))
        {
            ForeColor = NeonTheme.TextDim,
            Alignment = ToolStripItemAlignment.Right,
        };
        _statusStrip = new StatusStrip();
        _statusStrip.Items.AddRange([_statusLabel, _timeLabel]);
        Controls.Add(_statusStrip);

        // Clock timer
        _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _clockTimer.Tick += (_, _) => _timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        _clockTimer.Start();

        NeonTheme.Apply(this);

        // Check for updates on startup
        Shown += async (_, _) =>
        {
            await CheckForUpdatesAsync();
        };
    }

    private MenuStrip BuildMenuStrip()
    {
        var menu = new MenuStrip();

        var newRunItem = new ToolStripMenuItem(_loc.Get("Menu.NewRun"));
        newRunItem.Click += (_, _) => LaunchRunWizard();
        menu.Items.Add(newRunItem);

        var urlItem = new ToolStripMenuItem(_loc.Get("Menu.UrlMgmt"));
        urlItem.Click += (_, _) => OpenChildForm<UrlManagerForm>();
        menu.Items.Add(urlItem);

        var scenarioItem = new ToolStripMenuItem(_loc.Get("Menu.Scenarios"));
        scenarioItem.Click += (_, _) => OpenChildForm<ScenarioManagerForm>();
        menu.Items.Add(scenarioItem);

        var discoveryItem = new ToolStripMenuItem(_loc.Get("Menu.Discovery"));
        discoveryItem.Click += (_, _) => OpenChildForm<DiscoveryToolsForm>();
        menu.Items.Add(discoveryItem);

        var liveItem = new ToolStripMenuItem(_loc.Get("Menu.Live"));
        liveItem.Click += (_, _) => OpenChildForm<LiveMonitoringForm>();
        menu.Items.Add(liveItem);

        var warningsItem = new ToolStripMenuItem(_loc.Get("Menu.Warnings"));
        warningsItem.Click += (_, _) => OpenChildForm<WarningsCriticalForm>();
        menu.Items.Add(warningsItem);

        var networkItem = new ToolStripMenuItem(_loc.Get("Menu.Network"));
        networkItem.Click += (_, _) => OpenChildForm<NetworkExplorerForm>();
        menu.Items.Add(networkItem);

        var reportsItem = new ToolStripMenuItem(_loc.Get("Menu.Reports"));
        reportsItem.Click += (_, _) => OpenChildForm<ResultsForm>();
        menu.Items.Add(reportsItem);

        var historyItem = new ToolStripMenuItem(_loc.Get("Menu.History"));
        historyItem.Click += (_, _) => OpenChildForm<HistoryForm>();
        menu.Items.Add(historyItem);

        // More menu (10-11)
        var moreMenu = new ToolStripMenuItem(_loc.Get("Menu.More"));
        moreMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.Settings"), null, (_, _) => OpenChildForm<SettingsForm>()));
        moreMenu.DropDownItems.Add(new ToolStripSeparator());
        moreMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.About"), null, (_, _) => ShowAbout()));
        moreMenu.DropDownItems.Add(new ToolStripSeparator());
        moreMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.Exit"), null, (_, _) => Close()));
        menu.Items.Add(moreMenu);

        // Help menu
        var helpItem = new ToolStripMenuItem(_loc.Get("Menu.Help"));
        helpItem.Click += (_, _) => ShowHelp();
        menu.Items.Add(helpItem);

        // Language menu (🌐)
        var langMenu = new ToolStripMenuItem("🌐 " + _loc.GetLanguageDisplayName(_loc.CurrentLanguage));
        foreach (var lang in _loc.AvailableLanguages)
        {
            var code = lang;
            var displayName = _loc.GetLanguageDisplayName(code);
            var item = new ToolStripMenuItem(displayName);
            if (code == _loc.CurrentLanguage)
            {
                item.Checked = true;
                item.Font = new Font(item.Font, FontStyle.Bold);
            }
            item.Click += (_, _) => SwitchLanguage(code);
            langMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(langMenu);

        // Window menu
        var windowMenu = new ToolStripMenuItem(_loc.Get("Menu.Window"));
        windowMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.Cascade"), null, (_, _) => LayoutMdi(MdiLayout.Cascade)));
        windowMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.TileH"), null, (_, _) => LayoutMdi(MdiLayout.TileHorizontal)));
        windowMenu.DropDownItems.Add(new ToolStripMenuItem(_loc.Get("Menu.TileV"), null, (_, _) => LayoutMdi(MdiLayout.TileVertical)));
        menu.Items.Add(windowMenu);
        menu.MdiWindowListItem = windowMenu;

        return menu;
    }

    private void OpenChildForm<T>() where T : Form
    {
        foreach (var child in MdiChildren)
        {
            if (child is T existing)
            {
                existing.Activate();
                return;
            }
        }

        Form form = typeof(T).Name switch
        {
            nameof(UrlManagerForm) => new UrlManagerForm(_urlManager, _logger),
            nameof(MonitoringForm) => new MonitoringForm(_orchestrator, _urlManager, _logger, _config),
            nameof(ResultsForm) => new ResultsForm(_database, _logger),
            nameof(SettingsForm) => new SettingsForm(_urlManager, _logger, _loc, _settings),
            nameof(NetworkExplorerForm) => new NetworkExplorerForm(_database, _logger),
            nameof(WarningsCriticalForm) => new WarningsCriticalForm(_database, _logger),
            nameof(EvidenceViewerForm) => new EvidenceViewerForm(_database, _logger),
            nameof(ScenarioManagerForm) => new ScenarioManagerForm(_database, _logger),
            nameof(DiscoveryToolsForm) => new DiscoveryToolsForm(_discoveryEngine, _logger),
            nameof(LiveMonitoringForm) => new LiveMonitoringForm(_orchestrator, _urlManager, _logger, _config),
            nameof(HistoryForm) => new HistoryForm(_database, _logger),
            _ => throw new InvalidOperationException($"Unknown form type: {typeof(T).Name}"),
        };

        form.MdiParent = this;
        form.Show();
    }

    private void LaunchRunWizard()
    {
        using var wizard = new RunWizardForm(_urlManager, _logger, _config);
        if (wizard.ShowDialog(this) == DialogResult.OK && wizard.WizardCompleted)
        {
            OpenChildForm<MonitoringForm>();
        }
    }

    private void ShowAbout()
    {
        using var about = new AboutForm(_loc, _version);
        about.ShowDialog(this);
    }

    private void ShowHelp()
    {
        using var help = new HelpForm(_helpContent, _loc);
        help.ShowDialog(this);
    }

    private async Task CheckForUpdatesAsync()
    {
        if (_updateService is null || !_settings.CheckForUpdatesOnStartup)
            return;

        try
        {
            var update = await _updateService.CheckForUpdateAsync();
            if (update is null)
                return;

            // Skip if user previously chose to skip this version
            if (_settings.LastSkippedVersion == update.Version)
                return;

            using var dialog = new UpdateDialog(update, _loc, _updateService, _version, _settings, _logger);
            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Update check failed: {ex.Message}");
        }
    }

    public void SetStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetStatus(message));
            return;
        }

        _statusLabel.Text = message;
    }

    private void BuildDashboardBackground(MdiClient mdiClient)
    {
        var bannerText =
            "  ██████╗ ██╗      █████╗ ███╗   ██╗ ██████╗ ██████╗\n" +
            "  ██╔══██╗██║     ██╔══██╗████╗  ██║██╔════╝██╔═══██╗\n" +
            "  ██████╔╝██║     ███████║██╔██╗ ██║██║     ██║   ██║\n" +
            "  ██╔══██╗██║     ██╔══██╗██║╚██╗██║██║     ██║   ██║\n" +
            "  ██████╔╝███████╗██║  ██║██║ ╚████║╚██████╗╚██████╔╝\n" +
            "  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚═════╝\n" +
            "  [ NON-INVASIVE WEBSITE MONITOR & TRACE TOOL ]";

        var statItems = new[] { ("STATUS", "IDLE"), ("TARGETS", "0"), ("LAST RUN", "—"), ("ALERTS", "0") };

        using var bannerFont = new Font("Consolas", 8f);
        var bannerSize = TextRenderer.MeasureText(bannerText, bannerFont);
        var statsY = 45 + bannerSize.Height + 10;

        mdiClient.Paint += (_, e) =>
        {
            var g = e.Graphics;

            // Title
            TextRenderer.DrawText(g, "⬡ BLANCO MONITOR", NeonTheme.HeaderFont,
                new Point(20, 15), NeonTheme.TextPrimary);

            // ASCII banner
            TextRenderer.DrawText(g, bannerText, bannerFont,
                new Point(20, 45), NeonTheme.TextDim);

            // Stats panel background
            var statsRect = new Rectangle(20, statsY, 730, 60);
            using var surfaceBrush = new SolidBrush(NeonTheme.Surface);
            g.FillRectangle(surfaceBrush, statsRect);

            // Stats items
            var sx = 35;
            foreach (var (title, value) in statItems)
            {
                TextRenderer.DrawText(g, title, NeonTheme.MonoFontSmall,
                    new Point(sx, statsY + 8), NeonTheme.TextDim);
                TextRenderer.DrawText(g, value, NeonTheme.MonoFontLarge,
                    new Point(sx, statsY + 28), NeonTheme.TextAccent);
                sx += 175;
            }

            // Version hint
            TextRenderer.DrawText(g, "FILE → New Session to start monitoring", NeonTheme.MonoFontSmall,
                new Point(20, statsY + 70), NeonTheme.TextDim);
        };

        mdiClient.Invalidate();
    }

    private void SwitchLanguage(string languageCode)
    {
        if (languageCode == _loc.CurrentLanguage)
            return;

        _settings.Language = languageCode;
        _settings.Save();
        _loc.SetLanguage(languageCode);

        MessageBox.Show(
            _loc.Get("Settings.LanguageRestart"),
            _loc.Get("Settings.Title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.None);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _clockTimer.Dispose();
        base.OnFormClosed(e);
    }
}
