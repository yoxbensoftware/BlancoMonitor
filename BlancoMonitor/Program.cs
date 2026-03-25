using BlancoMonitor.Application.Services;
using BlancoMonitor.Infrastructure.Analysis;
using BlancoMonitor.Infrastructure.Browser;
using BlancoMonitor.Infrastructure.Configuration;
using BlancoMonitor.Infrastructure.Database;
using BlancoMonitor.Infrastructure.Discovery;
using BlancoMonitor.Infrastructure.Evidence;
using BlancoMonitor.Infrastructure.History;
using BlancoMonitor.Infrastructure.Help;
using BlancoMonitor.Infrastructure.Localization;
using BlancoMonitor.Infrastructure.Logging;
using BlancoMonitor.Infrastructure.Network;
using BlancoMonitor.Infrastructure.Reporting;
using BlancoMonitor.Infrastructure.Updater;
using BlancoMonitor.Infrastructure.Versioning;
using BlancoMonitor.UI;

namespace BlancoMonitor;

internal static class Program
{
    // ── Update manifest URL ─────────────────────────────────────────
    // Hosted on GitHub raw content — updated automatically by the release workflow.
    // Replace GITHUB_USER/GITHUB_REPO with your actual repository path.
    // Example: https://raw.githubusercontent.com/ozgen/blanco-monitor/main/update-manifest.json
    private const string UpdateManifestUrl =
        "https://raw.githubusercontent.com/GITHUB_USER/GITHUB_REPO/main/update-manifest.json";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // ── Core services ───────────────────────────────────────
        var versionProvider = new AssemblyVersionProvider();
        var settings = new JsonSettingsService("blanco_settings.json");
        var loc = new DictionaryLocalizationService(settings.Language);

        // Infrastructure
        var configStore = new JsonConfigurationManager("blanco_config.json");
        var config = configStore.LoadAsync().GetAwaiter().GetResult();
        var logDbPath = Path.Combine(config.DataDirectory, "blanco_logs.db");
        Directory.CreateDirectory(config.DataDirectory);
        var logger = new StructuredLogger(config.LogDirectory, logDbPath);
        var networkClient = new HttpNetworkClient(config.UserAgent, config.DefaultTimeoutSeconds, logger);

        logger.Info($"BlancoMonitor {versionProvider.DisplayVersion} starting (lang={settings.Language})");

        // Database (SQLite)
        var dbPath = Path.Combine(config.DataDirectory, "blanco.db");
        Directory.CreateDirectory(config.DataDirectory);
        var database = new BlancoDatabase(dbPath, logger);
        database.InitializeAsync().GetAwaiter().GetResult();

        // Engines
        var scenarioEngine = new HttpScenarioEngine(networkClient, logger, config.DelayBetweenRequestsMs);
        var discoveryEngine = new SitemapDiscoveryEngine(networkClient, logger);
        var analyzer = new PerformanceAnalyzerImpl();
        var ruleEngine = new RuleEngineImpl();
        var evidenceCollector = new HeadlessEvidenceCollector();
        var reportGenerator = new HtmlReportGenerator();
        var historicalStore = new JsonHistoricalStore(config.HistoryDirectory, logger);

        // Update service
        var updateService = new ManifestUpdateService(versionProvider, logger, UpdateManifestUrl);

        // Help content
        var helpContent = new DictionaryHelpContentService();

        // Application services
        var orchestrator = new MonitoringOrchestrator(
            scenarioEngine, discoveryEngine, analyzer, ruleEngine,
            evidenceCollector, reportGenerator, historicalStore, database, logger);
        var urlManager = new UrlKeywordSetManager(configStore, logger);

        // UI
        var mainForm = new MdiParentForm(
            orchestrator, urlManager, logger, historicalStore, database,
            discoveryEngine, config, loc, versionProvider, settings, helpContent, updateService);

        System.Windows.Forms.Application.Run(mainForm);

        // Cleanup
        updateService.Dispose();
        database.Dispose();
        networkClient.Dispose();
        logger.Dispose();
    }
}