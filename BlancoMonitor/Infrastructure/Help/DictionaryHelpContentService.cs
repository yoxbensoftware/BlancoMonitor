using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Help;

/// <summary>
/// Dictionary-based help content provider with embedded EN/DE/TR documentation.
/// Returns a hierarchical tree of <see cref="HelpTopic"/> nodes for the help viewer.
/// </summary>
public sealed class DictionaryHelpContentService : IHelpContentService
{
    private readonly Dictionary<string, List<HelpTopic>> _content = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> SupportedLanguages { get; }

    public DictionaryHelpContentService()
    {
        _content["en"] = BuildEnglish();
        _content["de"] = BuildGerman();
        _content["tr"] = BuildTurkish();
        SupportedLanguages = _content.Keys.Order().ToList().AsReadOnly();
    }

    public IReadOnlyList<HelpTopic> GetTopics(string languageCode)
    {
        var code = languageCode.ToLowerInvariant();
        return _content.TryGetValue(code, out var topics) ? topics : _content["en"];
    }

    // ════════════════════════════════════════════════════════════════
    //  ENGLISH
    // ════════════════════════════════════════════════════════════════

    private static List<HelpTopic> BuildEnglish() =>
    [
        new HelpTopic
        {
            Key = "overview",
            Title = "Overview",
            IconHint = "info",
            Content =
                "BLANCOMONITOR — NON-INVASIVE WEBSITE MONITORING TOOL\n" +
                "════════════════════════════════════════════════════════\n\n" +
                "BlancoMonitor is a desktop application designed to monitor\n" +
                "website performance and detect potential issues — without\n" +
                "interfering with or harming the target system in any way.\n\n" +
                "WHAT IT DOES:\n" +
                "  • Monitors response times (TTFB, download, total load)\n" +
                "  • Detects slow pages, broken resources, and errors\n" +
                "  • Analyzes network behavior and resource loading\n" +
                "  • Generates detailed performance reports (HTML/JSON/CSV)\n" +
                "  • Compares runs over time to spot regressions\n" +
                "  • Collects evidence for documentation purposes\n\n" +
                "WHAT MAKES IT DIFFERENT:\n" +
                "Unlike load testing, penetration testing, or security\n" +
                "scanning tools, BlancoMonitor behaves like a single,\n" +
                "well-mannered web browser. It visits pages at a controlled\n" +
                "pace, records measurements, and reports findings.\n\n" +
                "It does NOT inject code, attempt exploits, generate\n" +
                "artificial load, or perform any form of attack.\n\n" +
                "TRUST & SAFETY:\n" +
                "This tool is safe by design. Every request uses standard\n" +
                "HTTP GET with a clearly identified User-Agent string.\n" +
                "Server administrators can easily identify and manage\n" +
                "BlancoMonitor traffic. Rate limiting, scope restrictions,\n" +
                "and domain whitelisting ensure responsible usage."
        },
        new HelpTopic
        {
            Key = "quickstart",
            Title = "Quick Start",
            IconHint = "play",
            Content =
                "GETTING STARTED WITH BLANCOMONITOR\n" +
                "════════════════════════════════════\n\n" +
                "Follow these steps to run your first monitoring session:\n\n" +
                "STEP 1 — DEFINE URLs\n" +
                "  Open 'URL / Keyword Mgmt' from the main menu.\n" +
                "  Create a new URL set and add the pages you want to monitor.\n" +
                "  Example: https://www.example.com\n\n" +
                "STEP 2 — ADD KEYWORDS (OPTIONAL)\n" +
                "  In the same management screen, define keyword sets.\n" +
                "  Keywords are used by discovery and search scenarios.\n" +
                "  Example: \"contact\", \"products\", \"login\"\n\n" +
                "STEP 3 — CONFIGURE SETTINGS\n" +
                "  Open 'Settings' from the MORE menu.\n" +
                "  Adjust timeout, delay between requests, and thresholds.\n" +
                "  Default settings are safe and suitable for most cases.\n\n" +
                "STEP 4 — START A MONITORING RUN\n" +
                "  Click 'New Monitoring Run' from the main menu.\n" +
                "  The Run Wizard will guide you through:\n" +
                "    • Selecting URL sets\n" +
                "    • Choosing keyword sets\n" +
                "    • Picking scenarios\n" +
                "    • Reviewing configuration\n" +
                "    • Starting the run\n\n" +
                "STEP 5 — REVIEW RESULTS\n" +
                "  After the run completes, results appear automatically.\n" +
                "  Use 'Reports' to generate detailed HTML/CSV reports.\n" +
                "  Use 'Warnings / Critical' to see detected issues.\n" +
                "  Use 'History' to compare with previous runs."
        },
        new HelpTopic
        {
            Key = "modules",
            Title = "Main Modules",
            IconHint = "grid",
            Content =
                "BlancoMonitor is organized into specialized modules,\n" +
                "each accessible from the main menu bar.\n\n" +
                "Select a module below for detailed information.",
            Children =
            [
                new HelpTopic
                {
                    Key = "modules.monitoring",
                    Title = "Monitoring Engine",
                    Content =
                        "MONITORING ENGINE\n" +
                        "══════════════════\n\n" +
                        "The core of BlancoMonitor. When you start a run, the\n" +
                        "monitoring engine:\n\n" +
                        "  1. Loads your URL set and keyword configuration\n" +
                        "  2. Executes selected scenarios sequentially\n" +
                        "  3. Sends standard HTTP GET requests to each URL\n" +
                        "  4. Measures response times with precision\n" +
                        "  5. Analyzes network traces and resource loading\n" +
                        "  6. Applies rule engine to detect issues\n" +
                        "  7. Classifies findings as Warning or Critical\n" +
                        "  8. Stores all results in the local database\n\n" +
                        "KEY FEATURES:\n" +
                        "  • Configurable delay between requests (rate limiting)\n" +
                        "  • Sequential processing — no parallel bombardment\n" +
                        "  • Automatic noise filtering for reliable results\n" +
                        "  • Page type detection (home, product, search, etc.)\n" +
                        "  • Request classification (document, script, image, etc.)\n\n" +
                        "The engine respects configured boundaries and never\n" +
                        "exceeds the defined scope of monitoring."
                },
                new HelpTopic
                {
                    Key = "modules.scenarios",
                    Title = "Scenario Engine",
                    Content =
                        "SCENARIO ENGINE\n" +
                        "════════════════\n\n" +
                        "Scenarios define structured navigation patterns that\n" +
                        "simulate realistic user behavior.\n\n" +
                        "BUILT-IN SCENARIOS:\n" +
                        "  • Page Load — Visit a URL and measure load time\n" +
                        "  • Navigation — Follow a sequence of pages\n" +
                        "  • Search — Use keywords to search on the target site\n" +
                        "  • Discovery — Explore linked pages within scope\n\n" +
                        "CUSTOM SCENARIOS:\n" +
                        "  You can define custom scenarios by specifying:\n" +
                        "  • Target URLs or URL patterns\n" +
                        "  • Keywords for search operations\n" +
                        "  • Navigation sequences\n" +
                        "  • Expected behaviors\n\n" +
                        "All scenarios use controlled pacing and stay within\n" +
                        "the configured domain boundaries."
                },
                new HelpTopic
                {
                    Key = "modules.urlkeyword",
                    Title = "URL & Keyword Management",
                    Content =
                        "URL & KEYWORD MANAGEMENT\n" +
                        "═════════════════════════\n\n" +
                        "Organize your monitoring targets into sets.\n\n" +
                        "URL SETS:\n" +
                        "  • Group related URLs into named sets\n" +
                        "  • Example: 'Production Site', 'Staging Environment'\n" +
                        "  • Each URL can have a label and expected behavior\n" +
                        "  • URLs are validated before monitoring starts\n\n" +
                        "KEYWORD SETS:\n" +
                        "  • Define search terms for keyword-based scenarios\n" +
                        "  • Example: 'product names', 'navigation terms'\n" +
                        "  • Keywords are used in search and discovery\n\n" +
                        "IMPORTANT:\n" +
                        "  Only URLs you explicitly add are visited.\n" +
                        "  BlancoMonitor never follows links outside your\n" +
                        "  defined URL set unless discovery mode is enabled\n" +
                        "  within the configured scope."
                },
                new HelpTopic
                {
                    Key = "modules.discovery",
                    Title = "Discovery Tools",
                    Content =
                        "DISCOVERY TOOLS\n" +
                        "════════════════\n\n" +
                        "Discovery helps you find additional pages within a\n" +
                        "target website's scope.\n\n" +
                        "SITEMAP DISCOVERY:\n" +
                        "  • Reads the target site's sitemap.xml\n" +
                        "  • Extracts URLs within the defined domain\n" +
                        "  • Suggests pages you may want to monitor\n\n" +
                        "LINK DISCOVERY:\n" +
                        "  • Follows internal links from known pages\n" +
                        "  • Stays within configured domain boundaries\n" +
                        "  • Respects robots.txt directives\n\n" +
                        "Discovery results are suggestions — you decide\n" +
                        "which URLs to add to your monitoring set."
                },
                new HelpTopic
                {
                    Key = "modules.reporting",
                    Title = "Reporting",
                    Content =
                        "REPORTING\n" +
                        "══════════\n\n" +
                        "BlancoMonitor generates comprehensive reports in\n" +
                        "multiple formats.\n\n" +
                        "REPORT FORMATS:\n" +
                        "  • HTML — Rich, visual report for stakeholders\n" +
                        "  • JSON — Machine-readable for integrations\n" +
                        "  • CSV  — Spreadsheet-compatible for analysis\n\n" +
                        "REPORT CONTENTS:\n" +
                        "  • Executive summary with key metrics\n" +
                        "  • Detailed per-URL performance breakdown\n" +
                        "  • Warning and Critical issue listings\n" +
                        "  • Network trace analysis\n" +
                        "  • Resource loading waterfall data\n" +
                        "  • Actionable recommendations\n\n" +
                        "Reports are saved to the configured output directory\n" +
                        "and can be shared with team members."
                },
                new HelpTopic
                {
                    Key = "modules.history",
                    Title = "History & Comparison",
                    Content =
                        "HISTORY & COMPARISON\n" +
                        "═════════════════════\n\n" +
                        "Track performance trends over time.\n\n" +
                        "FEATURES:\n" +
                        "  • View all past monitoring runs\n" +
                        "  • Compare two runs side by side\n" +
                        "  • Identify regressions and improvements\n" +
                        "  • Track baseline metrics over time\n" +
                        "  • Daily summary aggregation\n\n" +
                        "Comparisons highlight:\n" +
                        "  • Response time changes (faster / slower)\n" +
                        "  • New issues that appeared\n" +
                        "  • Issues that were resolved\n" +
                        "  • Overall trend direction"
                },
                new HelpTopic
                {
                    Key = "modules.settings",
                    Title = "Settings",
                    Content =
                        "SETTINGS\n" +
                        "═════════\n\n" +
                        "Configure BlancoMonitor behavior from the Settings screen.\n\n" +
                        "GENERAL:\n" +
                        "  • Timeout — Maximum wait time per request (seconds)\n" +
                        "  • Concurrency — Number of parallel requests (default: 1)\n" +
                        "  • Delay — Pause between requests (milliseconds)\n" +
                        "  • User Agent — HTTP User-Agent string sent with requests\n\n" +
                        "THRESHOLDS:\n" +
                        "  • Warning level — Response times above this trigger a warning\n" +
                        "  • Critical level — Response times above this trigger a critical alert\n" +
                        "  • Configurable for TTFB, total time, and download time\n\n" +
                        "LANGUAGE & UPDATES:\n" +
                        "  • Choose your preferred language (English/German/Turkish)\n" +
                        "  • Enable or disable automatic update checking\n" +
                        "  • Language changes take effect on next restart"
                },
                new HelpTopic
                {
                    Key = "modules.updater",
                    Title = "Update System",
                    Content =
                        "UPDATE SYSTEM\n" +
                        "══════════════\n\n" +
                        "BlancoMonitor includes an automatic update mechanism.\n\n" +
                        "HOW IT WORKS:\n" +
                        "  1. On startup, the app checks a remote update manifest\n" +
                        "  2. If a newer version exists, you are notified\n" +
                        "  3. The update dialog shows:\n" +
                        "     • Current vs. latest version number\n" +
                        "     • Download size\n" +
                        "     • Release notes\n" +
                        "  4. You can choose to:\n" +
                        "     • UPDATE NOW — Download and install immediately\n" +
                        "     • LATER — Remind on next startup\n" +
                        "     • SKIP THIS VERSION — Don't ask about this version again\n\n" +
                        "SAFETY:\n" +
                        "  • Downloads are verified with SHA256 checksums\n" +
                        "  • A backup of the current version is created before update\n" +
                        "  • If the update fails, the previous version is restored\n" +
                        "  • The application restarts automatically after update"
                }
            ]
        },
        new HelpTopic
        {
            Key = "safety",
            Title = "Safety & Monitoring Boundaries",
            IconHint = "shield",
            Content =
                "SAFETY & NON-INVASIVE MONITORING BOUNDARIES\n" +
                "═══════════════════════════════════════════════\n\n" +
                "BlancoMonitor is designed to be completely safe and\n" +
                "non-invasive. This section explains the technical\n" +
                "safeguards built into the application.\n\n" +
                "THIS APPLICATION DOES NOT:\n" +
                "  ✗ Execute XSS (Cross-Site Scripting) payloads\n" +
                "  ✗ Attempt SQL Injection or any injection attacks\n" +
                "  ✗ Perform brute-force or credential attacks\n" +
                "  ✗ Bypass authentication or security controls\n" +
                "  ✗ Generate artificial load or stress on targets\n" +
                "  ✗ Simulate DDoS or high-concurrency attack patterns\n" +
                "  ✗ Perform fuzzing, penetration testing, or vuln scanning\n\n" +
                "TECHNICAL SAFEGUARDS:\n\n" +
                "  ⛨ RATE LIMITING\n" +
                "    Configurable delay between requests (default 500ms).\n" +
                "    Prevents any resemblance to automated attack behavior.\n\n" +
                "  ⛨ SCOPE RESTRICTION\n" +
                "    Monitoring stays within configured domain boundaries.\n" +
                "    The tool never follows links to external domains.\n\n" +
                "  ⛨ DOMAIN WHITELIST\n" +
                "    Only explicitly configured URLs are contacted.\n" +
                "    No automatic discovery outside the defined target set.\n\n" +
                "  ⛨ SAFE HTTP METHODS\n" +
                "    Only HTTP GET requests by default.\n" +
                "    No POST, PUT, PATCH, DELETE — no server state is modified.\n\n" +
                "  ⛨ CONTROLLED PACING\n" +
                "    Sequential request processing with configurable concurrency.\n" +
                "    Typical profile: 1-2 requests per second maximum.\n\n" +
                "  ⛨ IDENTIFIED USER-AGENT\n" +
                "    All requests carry the 'BlancoMonitor/1.0' User-Agent.\n" +
                "    Server admins can easily identify and manage this traffic.\n\n" +
                "USAGE POLICY:\n" +
                "  This tool must only be used for authorized performance\n" +
                "  monitoring of websites you own or have explicit permission\n" +
                "  to monitor. Users are responsible for ensuring compliance\n" +
                "  with applicable laws and organizational policies."
        },
        new HelpTopic
        {
            Key = "localization",
            Title = "Language Support",
            IconHint = "globe",
            Content =
                "LANGUAGE SUPPORT\n" +
                "══════════════════\n\n" +
                "BlancoMonitor supports multiple languages:\n\n" +
                "  • English (default)\n" +
                "  • Deutsch (German)\n" +
                "  • Türkçe (Turkish)\n\n" +
                "HOW TO CHANGE LANGUAGE:\n" +
                "  1. Open Settings from the MORE menu\n" +
                "  2. Find the 'Language & Updates' section\n" +
                "  3. Select your preferred language from the dropdown\n" +
                "  4. Click Save\n" +
                "  5. Restart the application for changes to take effect\n\n" +
                "WHAT IS TRANSLATED:\n" +
                "  • All menus and menu items\n" +
                "  • Buttons and labels\n" +
                "  • Settings screen\n" +
                "  • About screen\n" +
                "  • Update dialog\n" +
                "  • Help documentation\n" +
                "  • Status messages\n\n" +
                "Your language preference is saved automatically and\n" +
                "applied on every subsequent launch."
        },
        new HelpTopic
        {
            Key = "faq",
            Title = "FAQ",
            IconHint = "question",
            Content =
                "FREQUENTLY ASKED QUESTIONS\n" +
                "═══════════════════════════\n\n" +
                "Q: Does the app open a real Chrome browser?\n" +
                "A: No. BlancoMonitor uses a headless HTTP client — no\n" +
                "   visible browser window is opened. All requests are\n" +
                "   standard HTTP calls, similar to what a web browser\n" +
                "   does internally but without rendering the page.\n\n" +
                "Q: Can the app overload or harm the target website?\n" +
                "A: No. The application uses rate limiting (default 500ms\n" +
                "   delay between requests) and sequential processing.\n" +
                "   It generates traffic equivalent to a single user\n" +
                "   browsing slowly. There is no risk of overloading.\n\n" +
                "Q: What is the difference between Warning and Critical?\n" +
                "A: Warning indicates a performance concern — the page\n" +
                "   responded slower than the configured threshold but\n" +
                "   is still functional. Critical indicates a serious\n" +
                "   issue — the page is unresponsive, returns errors,\n" +
                "   or fails to load essential resources.\n\n" +
                "Q: Are screenshots always taken?\n" +
                "A: No. Screenshots require Playwright to be installed\n" +
                "   and enabled in Settings. By default, monitoring uses\n" +
                "   HTTP-only mode without any screenshot capability.\n\n" +
                "Q: How does the automatic update work?\n" +
                "A: On startup, BlancoMonitor checks a remote manifest\n" +
                "   for new versions. If available, you are prompted to\n" +
                "   update. Downloads are verified with SHA256 checksums.\n" +
                "   A backup is created before updating, and rollback\n" +
                "   occurs automatically if the update fails.\n\n" +
                "Q: Can I change the language later?\n" +
                "A: Yes. Go to Settings, change the language, save, and\n" +
                "   restart the application. Your preference is remembered.\n\n" +
                "Q: Where are monitoring results stored?\n" +
                "A: Results are stored in a local SQLite database in your\n" +
                "   application data directory. Reports can be exported\n" +
                "   as HTML, JSON, or CSV files.\n\n" +
                "Q: Does the tool send data to external servers?\n" +
                "A: No. BlancoMonitor only contacts the URLs you define\n" +
                "   and the update manifest URL. No telemetry, analytics,\n" +
                "   or usage data is sent anywhere.\n\n" +
                "Q: Can I use this tool for security testing?\n" +
                "A: No. BlancoMonitor is exclusively a performance\n" +
                "   monitoring tool. It does not perform any security\n" +
                "   testing, vulnerability scanning, or exploit attempts.\n" +
                "   Use dedicated security tools for those purposes."
        }
    ];

    // ════════════════════════════════════════════════════════════════
    //  GERMAN
    // ════════════════════════════════════════════════════════════════

    private static List<HelpTopic> BuildGerman() =>
    [
        new HelpTopic
        {
            Key = "overview",
            Title = "Überblick",
            IconHint = "info",
            Content =
                "BLANCOMONITOR — NICHT-INVASIVES WEBSITE-MONITORING-TOOL\n" +
                "════════════════════════════════════════════════════════════\n\n" +
                "BlancoMonitor ist eine Desktop-Anwendung zur Überwachung\n" +
                "der Website-Performance und Erkennung potenzieller\n" +
                "Probleme — ohne das Zielsystem in irgendeiner Weise\n" +
                "zu beeinträchtigen oder zu schädigen.\n\n" +
                "WAS ES MACHT:\n" +
                "  • Überwacht Antwortzeiten (TTFB, Download, Gesamtladezeit)\n" +
                "  • Erkennt langsame Seiten, defekte Ressourcen und Fehler\n" +
                "  • Analysiert Netzwerkverhalten und Ressourcenladung\n" +
                "  • Erstellt detaillierte Performance-Berichte (HTML/JSON/CSV)\n" +
                "  • Vergleicht Durchläufe zur Erkennung von Regressionen\n" +
                "  • Sammelt Nachweise für Dokumentationszwecke\n\n" +
                "WAS ES ANDERS MACHT:\n" +
                "Anders als Lasttest-, Penetrationstest- oder Security-\n" +
                "Scanning-Tools verhält sich BlancoMonitor wie ein einzelner,\n" +
                "wohlerzogener Webbrowser. Es besucht Seiten in kontrolliertem\n" +
                "Tempo, zeichnet Messungen auf und berichtet Ergebnisse.\n\n" +
                "Es injiziert KEINEN Code, versucht KEINE Exploits, erzeugt\n" +
                "KEINE künstliche Last und führt KEINE Angriffe durch.\n\n" +
                "VERTRAUEN & SICHERHEIT:\n" +
                "Dieses Tool ist sicher konzipiert. Jede Anfrage verwendet\n" +
                "Standard-HTTP-GET mit einem klar identifizierten User-Agent.\n" +
                "Server-Administratoren können BlancoMonitor-Verkehr leicht\n" +
                "erkennen und verwalten."
        },
        new HelpTopic
        {
            Key = "quickstart",
            Title = "Schnellstart",
            IconHint = "play",
            Content =
                "ERSTE SCHRITTE MIT BLANCOMONITOR\n" +
                "═════════════════════════════════\n\n" +
                "Folgen Sie diesen Schritten für Ihre erste Überwachungssitzung:\n\n" +
                "SCHRITT 1 — URLs DEFINIEREN\n" +
                "  Öffnen Sie 'URL / Stichwörter' aus dem Hauptmenü.\n" +
                "  Erstellen Sie ein neues URL-Set und fügen Sie die Seiten hinzu,\n" +
                "  die Sie überwachen möchten.\n\n" +
                "SCHRITT 2 — STICHWÖRTER HINZUFÜGEN (OPTIONAL)\n" +
                "  Im selben Verwaltungsbildschirm können Sie Stichwort-Sets\n" +
                "  definieren. Stichwörter werden für Entdeckung und Suche verwendet.\n\n" +
                "SCHRITT 3 — EINSTELLUNGEN KONFIGURIEREN\n" +
                "  Öffnen Sie 'Einstellungen' aus dem MEHR-Menü.\n" +
                "  Passen Sie Zeitlimit, Verzögerung und Schwellenwerte an.\n" +
                "  Standardeinstellungen sind sicher und für die meisten Fälle geeignet.\n\n" +
                "SCHRITT 4 — ÜBERWACHUNGSLAUF STARTEN\n" +
                "  Klicken Sie auf 'Neue Überwachung' im Hauptmenü.\n" +
                "  Der Assistent führt Sie durch:\n" +
                "    • URL-Sets auswählen\n" +
                "    • Stichwort-Sets wählen\n" +
                "    • Szenarien auswählen\n" +
                "    • Konfiguration überprüfen\n" +
                "    • Lauf starten\n\n" +
                "SCHRITT 5 — ERGEBNISSE PRÜFEN\n" +
                "  Nach Abschluss des Laufs erscheinen die Ergebnisse automatisch.\n" +
                "  Verwenden Sie 'Berichte' für detaillierte Berichte.\n" +
                "  Verwenden Sie 'Warnungen / Kritisch' für erkannte Probleme.\n" +
                "  Verwenden Sie 'Verlauf' zum Vergleich mit früheren Läufen."
        },
        new HelpTopic
        {
            Key = "modules",
            Title = "Hauptmodule",
            IconHint = "grid",
            Content =
                "BlancoMonitor ist in spezialisierte Module organisiert,\n" +
                "die jeweils über die Hauptmenüleiste zugänglich sind.\n\n" +
                "Wählen Sie unten ein Modul für detaillierte Informationen.",
            Children =
            [
                new HelpTopic
                {
                    Key = "modules.monitoring",
                    Title = "Überwachungsmodul",
                    Content =
                        "ÜBERWACHUNGSMODUL\n" +
                        "══════════════════\n\n" +
                        "Das Herzstück von BlancoMonitor. Wenn Sie einen Lauf starten:\n\n" +
                        "  1. Lädt Ihr URL-Set und die Keyword-Konfiguration\n" +
                        "  2. Führt ausgewählte Szenarien sequenziell aus\n" +
                        "  3. Sendet Standard-HTTP-GET-Anfragen an jede URL\n" +
                        "  4. Misst Antwortzeiten mit Präzision\n" +
                        "  5. Analysiert Netzwerk-Traces und Ressourcenladung\n" +
                        "  6. Wendet die Regelengine zur Problemerkennung an\n" +
                        "  7. Klassifiziert Ergebnisse als Warnung oder Kritisch\n" +
                        "  8. Speichert alle Ergebnisse in der lokalen Datenbank\n\n" +
                        "HAUPTMERKMALE:\n" +
                        "  • Konfigurierbare Verzögerung zwischen Anfragen\n" +
                        "  • Sequenzielle Verarbeitung — kein paralleles Bombardement\n" +
                        "  • Automatische Rauschfilterung für zuverlässige Ergebnisse\n" +
                        "  • Seitentyp-Erkennung (Startseite, Produkt, Suche, etc.)\n" +
                        "  • Anfrageklassifizierung (Dokument, Skript, Bild, etc.)"
                },
                new HelpTopic
                {
                    Key = "modules.scenarios",
                    Title = "Szenario-Engine",
                    Content =
                        "SZENARIO-ENGINE\n" +
                        "════════════════\n\n" +
                        "Szenarien definieren strukturierte Navigationsmuster, die\n" +
                        "realistisches Benutzerverhalten simulieren.\n\n" +
                        "EINGEBAUTE SZENARIEN:\n" +
                        "  • Seitenladung — URL besuchen und Ladezeit messen\n" +
                        "  • Navigation — Einer Seitensequenz folgen\n" +
                        "  • Suche — Stichwörter auf der Zielseite suchen\n" +
                        "  • Entdeckung — Verlinkte Seiten im Scope erkunden\n\n" +
                        "Alle Szenarien verwenden kontrolliertes Tempo und bleiben\n" +
                        "innerhalb der konfigurierten Domänengrenzen."
                },
                new HelpTopic
                {
                    Key = "modules.urlkeyword",
                    Title = "URL- & Stichwort-Verwaltung",
                    Content =
                        "URL- & STICHWORT-VERWALTUNG\n" +
                        "════════════════════════════\n\n" +
                        "Organisieren Sie Ihre Überwachungsziele in Sets.\n\n" +
                        "URL-SETS:\n" +
                        "  • Gruppieren Sie zusammengehörige URLs in benannte Sets\n" +
                        "  • Beispiel: 'Produktionsseite', 'Staging-Umgebung'\n" +
                        "  • Jede URL kann ein Label und erwartetes Verhalten haben\n\n" +
                        "STICHWORT-SETS:\n" +
                        "  • Definieren Sie Suchbegriffe für keyword-basierte Szenarien\n" +
                        "  • Stichwörter werden bei Suche und Entdeckung verwendet\n\n" +
                        "WICHTIG:\n" +
                        "  Nur URLs, die Sie explizit hinzufügen, werden besucht."
                },
                new HelpTopic
                {
                    Key = "modules.discovery",
                    Title = "Entdeckungswerkzeuge",
                    Content =
                        "ENTDECKUNGSWERKZEUGE\n" +
                        "═════════════════════\n\n" +
                        "Discovery hilft Ihnen, zusätzliche Seiten innerhalb des\n" +
                        "Zielwebsite-Scopes zu finden.\n\n" +
                        "SITEMAP-ENTDECKUNG:\n" +
                        "  • Liest die sitemap.xml der Zielseite\n" +
                        "  • Extrahiert URLs innerhalb der definierten Domäne\n" +
                        "  • Schlägt Seiten vor, die Sie überwachen möchten\n\n" +
                        "LINK-ENTDECKUNG:\n" +
                        "  • Folgt internen Links von bekannten Seiten\n" +
                        "  • Bleibt innerhalb konfigurierter Domänengrenzen\n" +
                        "  • Respektiert robots.txt-Anweisungen"
                },
                new HelpTopic
                {
                    Key = "modules.reporting",
                    Title = "Berichterstellung",
                    Content =
                        "BERICHTERSTELLUNG\n" +
                        "══════════════════\n\n" +
                        "BlancoMonitor erstellt umfassende Berichte in\n" +
                        "mehreren Formaten.\n\n" +
                        "BERICHTSFORMATE:\n" +
                        "  • HTML — Visueller Bericht für Stakeholder\n" +
                        "  • JSON — Maschinenlesbar für Integrationen\n" +
                        "  • CSV  — Tabellenkalkulationskompatibel\n\n" +
                        "BERICHTSINHALTE:\n" +
                        "  • Zusammenfassung mit Schlüsselmetriken\n" +
                        "  • Detaillierte Per-URL Performance-Aufschlüsselung\n" +
                        "  • Warnung- und Kritisch-Problemlisten\n" +
                        "  • Netzwerk-Trace-Analyse\n" +
                        "  • Handlungsempfehlungen"
                },
                new HelpTopic
                {
                    Key = "modules.history",
                    Title = "Verlauf & Vergleich",
                    Content =
                        "VERLAUF & VERGLEICH\n" +
                        "════════════════════\n\n" +
                        "Verfolgen Sie Performance-Trends über die Zeit.\n\n" +
                        "FUNKTIONEN:\n" +
                        "  • Alle vergangenen Überwachungsläufe anzeigen\n" +
                        "  • Zwei Läufe nebeneinander vergleichen\n" +
                        "  • Regressionen und Verbesserungen identifizieren\n" +
                        "  • Baseline-Metriken über die Zeit verfolgen\n" +
                        "  • Tägliche Zusammenfassungsaggregation"
                },
                new HelpTopic
                {
                    Key = "modules.settings",
                    Title = "Einstellungen",
                    Content =
                        "EINSTELLUNGEN\n" +
                        "══════════════\n\n" +
                        "Konfigurieren Sie das BlancoMonitor-Verhalten.\n\n" +
                        "ALLGEMEIN:\n" +
                        "  • Zeitlimit — Maximale Wartezeit pro Anfrage\n" +
                        "  • Parallelität — Anzahl paralleler Anfragen\n" +
                        "  • Verzögerung — Pause zwischen Anfragen\n" +
                        "  • User Agent — HTTP User-Agent-String\n\n" +
                        "SCHWELLENWERTE:\n" +
                        "  • Warnungsstufe — Ab hier wird eine Warnung ausgelöst\n" +
                        "  • Kritisch-Stufe — Ab hier wird ein kritischer Alarm ausgelöst\n\n" +
                        "SPRACHE & UPDATES:\n" +
                        "  • Bevorzugte Sprache wählen (Englisch/Deutsch/Türkisch)\n" +
                        "  • Automatische Updateprüfung aktivieren/deaktivieren"
                },
                new HelpTopic
                {
                    Key = "modules.updater",
                    Title = "Update-System",
                    Content =
                        "UPDATE-SYSTEM\n" +
                        "══════════════\n\n" +
                        "BlancoMonitor enthält ein automatisches Update-System.\n\n" +
                        "FUNKTIONSWEISE:\n" +
                        "  1. Beim Start prüft die App ein Remote-Update-Manifest\n" +
                        "  2. Bei neuer Version werden Sie benachrichtigt\n" +
                        "  3. Der Update-Dialog zeigt:\n" +
                        "     • Aktuelle vs. neueste Versionsnummer\n" +
                        "     • Downloadgröße\n" +
                        "     • Versionshinweise\n" +
                        "  4. Sie können wählen:\n" +
                        "     • JETZT AKTUALISIEREN\n" +
                        "     • SPÄTER — Beim nächsten Start erinnern\n" +
                        "     • VERSION ÜBERSPRINGEN\n\n" +
                        "SICHERHEIT:\n" +
                        "  • Downloads werden mit SHA256-Prüfsummen verifiziert\n" +
                        "  • Backup wird vor dem Update erstellt\n" +
                        "  • Bei Fehler wird die vorherige Version wiederhergestellt"
                }
            ]
        },
        new HelpTopic
        {
            Key = "safety",
            Title = "Sicherheit & Monitoring-Grenzen",
            IconHint = "shield",
            Content =
                "SICHERHEIT & NICHT-INVASIVE MONITORING-GRENZEN\n" +
                "═══════════════════════════════════════════════════\n\n" +
                "BlancoMonitor ist so konzipiert, dass es vollkommen sicher\n" +
                "und nicht-invasiv ist.\n\n" +
                "DIESE ANWENDUNG FÜHRT NICHT DURCH:\n" +
                "  ✗ XSS (Cross-Site Scripting) Payloads\n" +
                "  ✗ SQL-Injection oder Injection-Angriffe\n" +
                "  ✗ Brute-Force- oder Credential-Angriffe\n" +
                "  ✗ Umgehung von Authentifizierung oder Sicherheitskontrollen\n" +
                "  ✗ Künstliche Belastung oder Stress auf Zielsysteme\n" +
                "  ✗ DDoS- oder Hochlast-Szenarien\n" +
                "  ✗ Fuzzing, Penetrationstests oder Schwachstellen-Scans\n\n" +
                "TECHNISCHE SICHERHEITSVORKEHRUNGEN:\n\n" +
                "  ⛨ RATENBEGRENZUNG\n" +
                "    Konfigurierbare Verzögerung (Standard 500ms).\n\n" +
                "  ⛨ BEREICHSBESCHRÄNKUNG\n" +
                "    Überwachung bleibt innerhalb der Domänengrenzen.\n\n" +
                "  ⛨ DOMÄNEN-WHITELIST\n" +
                "    Nur konfigurierte URLs werden kontaktiert.\n\n" +
                "  ⛨ SICHERE HTTP-METHODEN\n" +
                "    Nur GET. Kein POST, PUT, PATCH, DELETE.\n\n" +
                "  ⛨ KONTROLLIERTES TEMPO\n" +
                "    Sequenziell, max. 1-2 Anfragen pro Sekunde.\n\n" +
                "  ⛨ IDENTIFIZIERTER USER-AGENT\n" +
                "    'BlancoMonitor/1.0' in jeder Anfrage.\n\n" +
                "NUTZUNGSRICHTLINIE:\n" +
                "  Nur für autorisierte Performance-Überwachung von Websites,\n" +
                "  die Sie besitzen oder überwachen dürfen."
        },
        new HelpTopic
        {
            Key = "localization",
            Title = "Sprachunterstützung",
            IconHint = "globe",
            Content =
                "SPRACHUNTERSTÜTZUNG\n" +
                "═════════════════════\n\n" +
                "BlancoMonitor unterstützt mehrere Sprachen:\n\n" +
                "  • English (Standard)\n" +
                "  • Deutsch\n" +
                "  • Türkçe (Türkisch)\n\n" +
                "SPRACHE ÄNDERN:\n" +
                "  1. Öffnen Sie Einstellungen über das MEHR-Menü\n" +
                "  2. Finden Sie den Abschnitt 'Sprache & Updates'\n" +
                "  3. Wählen Sie Ihre bevorzugte Sprache\n" +
                "  4. Klicken Sie Speichern\n" +
                "  5. Starten Sie die Anwendung neu\n\n" +
                "Ihre Sprachpräferenz wird automatisch gespeichert."
        },
        new HelpTopic
        {
            Key = "faq",
            Title = "FAQ",
            IconHint = "question",
            Content =
                "HÄUFIG GESTELLTE FRAGEN\n" +
                "════════════════════════\n\n" +
                "F: Öffnet die App einen echten Chrome-Browser?\n" +
                "A: Nein. BlancoMonitor verwendet einen headless HTTP-Client.\n" +
                "   Es wird kein sichtbares Browserfenster geöffnet.\n\n" +
                "F: Kann die App die Zielwebsite überlasten?\n" +
                "A: Nein. Die Anwendung verwendet Ratenbegrenzung und\n" +
                "   sequenzielle Verarbeitung. Der generierte Verkehr\n" +
                "   entspricht einem einzelnen, langsam browsenden Benutzer.\n\n" +
                "F: Was ist der Unterschied zwischen Warnung und Kritisch?\n" +
                "A: Warnung zeigt ein Performance-Problem an — die Seite\n" +
                "   reagiert langsamer als der konfigurierte Schwellenwert.\n" +
                "   Kritisch zeigt ein ernstes Problem an — die Seite ist\n" +
                "   nicht erreichbar oder gibt Fehler zurück.\n\n" +
                "F: Werden immer Screenshots erstellt?\n" +
                "A: Nein. Screenshots erfordern Playwright und müssen in\n" +
                "   den Einstellungen aktiviert werden.\n\n" +
                "F: Wie funktioniert das automatische Update?\n" +
                "A: Beim Start prüft BlancoMonitor ein Remote-Manifest.\n" +
                "   Downloads werden mit SHA256 verifiziert. Bei Fehler\n" +
                "   wird die vorherige Version wiederhergestellt.\n\n" +
                "F: Kann ich die Sprache später ändern?\n" +
                "A: Ja. In den Einstellungen Sprache ändern, speichern\n" +
                "   und neu starten.\n\n" +
                "F: Wo werden die Ergebnisse gespeichert?\n" +
                "A: In einer lokalen SQLite-Datenbank. Berichte können\n" +
                "   als HTML, JSON oder CSV exportiert werden.\n\n" +
                "F: Kann ich dieses Tool für Sicherheitstests verwenden?\n" +
                "A: Nein. BlancoMonitor ist ausschließlich ein Performance-\n" +
                "   Monitoring-Tool. Es führt keine Sicherheitstests durch."
        }
    ];

    // ════════════════════════════════════════════════════════════════
    //  TURKISH
    // ════════════════════════════════════════════════════════════════

    private static List<HelpTopic> BuildTurkish() =>
    [
        new HelpTopic
        {
            Key = "overview",
            Title = "Genel Bakış",
            IconHint = "info",
            Content =
                "BLANCOMONITOR — MÜDAHALESİZ WEB SİTESİ İZLEME ARACI\n" +
                "════════════════════════════════════════════════════════\n\n" +
                "BlancoMonitor, web sitesi performansını izlemek ve olası\n" +
                "sorunları tespit etmek için tasarlanmış bir masaüstü\n" +
                "uygulamasıdır — hedef sisteme hiçbir şekilde müdahale\n" +
                "etmeden veya zarar vermeden.\n\n" +
                "NE YAPAR:\n" +
                "  • Yanıt sürelerini izler (TTFB, indirme, toplam yükleme)\n" +
                "  • Yavaş sayfaları, bozuk kaynakları ve hataları tespit eder\n" +
                "  • Ağ davranışını ve kaynak yüklemeyi analiz eder\n" +
                "  • Ayrıntılı performans raporları oluşturur (HTML/JSON/CSV)\n" +
                "  • Gerileme tespiti için çalıştırmaları karşılaştırır\n" +
                "  • Dokümantasyon için kanıt toplar\n\n" +
                "FARKLI KILAN:\n" +
                "Yük testi, penetrasyon testi veya güvenlik tarama\n" +
                "araçlarından farklı olarak, BlancoMonitor tek bir terbiyeli\n" +
                "web tarayıcısı gibi davranır. Sayfaları kontrollü bir hızda\n" +
                "ziyaret eder, ölçümleri kaydeder ve bulguları raporlar.\n\n" +
                "Kod enjekte ETMEZ, exploit DENEMEZ, yapay yük OLUŞTURMAZ\n" +
                "ve hiçbir saldırı türü GERÇEKLEŞTIRMEZ.\n\n" +
                "GÜVEN VE GÜVENLİK:\n" +
                "Bu araç tasarım gereği güvenlidir. Her istek, açıkça\n" +
                "tanımlanmış bir User-Agent ile standart HTTP GET kullanır.\n" +
                "Sunucu yöneticileri BlancoMonitor trafiğini kolayca\n" +
                "tanıyabilir ve yönetebilir."
        },
        new HelpTopic
        {
            Key = "quickstart",
            Title = "Hızlı Başlangıç",
            IconHint = "play",
            Content =
                "BLANCOMONITOR İLE İLK ADIMLAR\n" +
                "══════════════════════════════\n\n" +
                "İlk izleme oturumunuz için şu adımları izleyin:\n\n" +
                "ADIM 1 — URL'LERİ TANIMLAYIN\n" +
                "  Ana menüden 'URL / Anahtar Kelime' öğesini açın.\n" +
                "  Yeni bir URL seti oluşturun ve izlemek istediğiniz\n" +
                "  sayfaları ekleyin.\n\n" +
                "ADIM 2 — ANAHTAR KELİMELER EKLEYİN (İSTEĞE BAĞLI)\n" +
                "  Aynı yönetim ekranında anahtar kelime setleri tanımlayın.\n" +
                "  Anahtar kelimeler keşif ve arama senaryoları için kullanılır.\n\n" +
                "ADIM 3 — AYARLARI YAPILANDIRIN\n" +
                "  DAHA FAZLA menüsünden 'Ayarlar' öğesini açın.\n" +
                "  Zaman aşımı, gecikme ve eşik değerlerini ayarlayın.\n" +
                "  Varsayılan ayarlar güvenli ve çoğu durum için uygundur.\n\n" +
                "ADIM 4 — İZLEME ÇALIŞTIRMASI BAŞLATIN\n" +
                "  Ana menüden 'Yeni İzleme' öğesine tıklayın.\n" +
                "  Çalıştırma Sihirbazı sizi şunlarda yönlendirecektir:\n" +
                "    • URL setlerini seçme\n" +
                "    • Anahtar kelime setlerini seçme\n" +
                "    • Senaryoları seçme\n" +
                "    • Yapılandırmayı gözden geçirme\n" +
                "    • Çalıştırmayı başlatma\n\n" +
                "ADIM 5 — SONUÇLARI İNCELEYİN\n" +
                "  Çalıştırma tamamlandıktan sonra sonuçlar otomatik görünür.\n" +
                "  Ayrıntılı raporlar için 'Raporlar' bölümünü kullanın.\n" +
                "  Tespit edilen sorunlar için 'Uyarılar / Kritik' bölümünü kullanın.\n" +
                "  Önceki çalıştırmalarla karşılaştırma için 'Geçmiş' bölümünü kullanın."
        },
        new HelpTopic
        {
            Key = "modules",
            Title = "Ana Modüller",
            IconHint = "grid",
            Content =
                "BlancoMonitor, ana menü çubuğundan erişilebilen\n" +
                "özelleştirilmiş modüllerle organize edilmiştir.\n\n" +
                "Ayrıntılı bilgi için aşağıdan bir modül seçin.",
            Children =
            [
                new HelpTopic
                {
                    Key = "modules.monitoring",
                    Title = "İzleme Motoru",
                    Content =
                        "İZLEME MOTORU\n" +
                        "══════════════\n\n" +
                        "BlancoMonitor'ün çekirdeği. Bir çalıştırma başlattığınızda:\n\n" +
                        "  1. URL setinizi ve anahtar kelime yapılandırmanızı yükler\n" +
                        "  2. Seçilen senaryoları sırayla çalıştırır\n" +
                        "  3. Her URL'ye standart HTTP GET istekleri gönderir\n" +
                        "  4. Yanıt sürelerini hassas olarak ölçer\n" +
                        "  5. Ağ izlerini ve kaynak yüklemesini analiz eder\n" +
                        "  6. Sorun tespiti için kural motorunu uygular\n" +
                        "  7. Bulguları Uyarı veya Kritik olarak sınıflandırır\n" +
                        "  8. Tüm sonuçları yerel veritabanında saklar\n\n" +
                        "TEMEL ÖZELLİKLER:\n" +
                        "  • İstekler arası yapılandırılabilir gecikme\n" +
                        "  • Sıralı işleme — paralel bombardıman yok\n" +
                        "  • Güvenilir sonuçlar için otomatik gürültü filtreleme\n" +
                        "  • Sayfa türü tespiti (ana sayfa, ürün, arama, vb.)\n" +
                        "  • İstek sınıflandırma (belge, betik, resim, vb.)"
                },
                new HelpTopic
                {
                    Key = "modules.scenarios",
                    Title = "Senaryo Motoru",
                    Content =
                        "SENARYO MOTORU\n" +
                        "═══════════════\n\n" +
                        "Senaryolar, gerçekçi kullanıcı davranışını simüle eden\n" +
                        "yapılandırılmış navigasyon kalıpları tanımlar.\n\n" +
                        "YERLEŞİK SENARYOLAR:\n" +
                        "  • Sayfa Yükleme — URL'yi ziyaret et ve yükleme süresini ölç\n" +
                        "  • Navigasyon — Bir sayfa dizisini takip et\n" +
                        "  • Arama — Hedef sitede anahtar kelimeler ile ara\n" +
                        "  • Keşif — Kapsam dahilindeki bağlantılı sayfaları keşfet\n\n" +
                        "Tüm senaryolar kontrollü hız kullanır ve yapılandırılmış\n" +
                        "alan adı sınırları dahilinde kalır."
                },
                new HelpTopic
                {
                    Key = "modules.urlkeyword",
                    Title = "URL ve Anahtar Kelime Yönetimi",
                    Content =
                        "URL VE ANAHTAR KELİME YÖNETİMİ\n" +
                        "════════════════════════════════\n\n" +
                        "İzleme hedeflerinizi setler halinde düzenleyin.\n\n" +
                        "URL SETLERİ:\n" +
                        "  • İlgili URL'leri adlandırılmış setlere gruplayın\n" +
                        "  • Örnek: 'Üretim Sitesi', 'Test Ortamı'\n" +
                        "  • Her URL'nin bir etiketi ve beklenen davranışı olabilir\n\n" +
                        "ANAHTAR KELİME SETLERİ:\n" +
                        "  • Anahtar kelime tabanlı senaryolar için arama terimlerini tanımlayın\n" +
                        "  • Anahtar kelimeler arama ve keşifte kullanılır\n\n" +
                        "ÖNEMLİ:\n" +
                        "  Yalnızca açıkça eklediğiniz URL'ler ziyaret edilir."
                },
                new HelpTopic
                {
                    Key = "modules.discovery",
                    Title = "Keşif Araçları",
                    Content =
                        "KEŞİF ARAÇLARI\n" +
                        "═══════════════\n\n" +
                        "Keşif, hedef web sitesinin kapsamında ek sayfalar\n" +
                        "bulmanıza yardımcı olur.\n\n" +
                        "SITEMAP KEŞFİ:\n" +
                        "  • Hedef sitenin sitemap.xml dosyasını okur\n" +
                        "  • Tanımlı alan içindeki URL'leri çıkarır\n" +
                        "  • İzlemek isteyebileceğiniz sayfaları önerir\n\n" +
                        "BAĞLANTI KEŞFİ:\n" +
                        "  • Bilinen sayfalardan iç bağlantıları takip eder\n" +
                        "  • Yapılandırılmış alan sınırları dahilinde kalır\n" +
                        "  • robots.txt direktiflerine uyar"
                },
                new HelpTopic
                {
                    Key = "modules.reporting",
                    Title = "Raporlama",
                    Content =
                        "RAPORLAMA\n" +
                        "══════════\n\n" +
                        "BlancoMonitor birden fazla formatta kapsamlı\n" +
                        "raporlar oluşturur.\n\n" +
                        "RAPOR FORMATLARI:\n" +
                        "  • HTML — Paydaşlar için görsel rapor\n" +
                        "  • JSON — Entegrasyonlar için makine tarafından okunabilir\n" +
                        "  • CSV  — Analiz için elektronik tablo uyumlu\n\n" +
                        "RAPOR İÇERİĞİ:\n" +
                        "  • Anahtar metriklerle yönetici özeti\n" +
                        "  • URL bazında ayrıntılı performans dökümü\n" +
                        "  • Uyarı ve Kritik sorun listeleri\n" +
                        "  • Ağ izi analizi\n" +
                        "  • Eyleme geçirilebilir öneriler"
                },
                new HelpTopic
                {
                    Key = "modules.history",
                    Title = "Geçmiş ve Karşılaştırma",
                    Content =
                        "GEÇMİŞ VE KARŞILAŞTIRMA\n" +
                        "════════════════════════\n\n" +
                        "Zaman içindeki performans trendlerini takip edin.\n\n" +
                        "ÖZELLİKLER:\n" +
                        "  • Tüm geçmiş izleme çalıştırmalarını görüntüleyin\n" +
                        "  • İki çalıştırmayı yan yana karşılaştırın\n" +
                        "  • Gerileme ve iyileştirmeleri belirleyin\n" +
                        "  • Temel metrikleri zaman içinde takip edin\n" +
                        "  • Günlük özet toplamı"
                },
                new HelpTopic
                {
                    Key = "modules.settings",
                    Title = "Ayarlar",
                    Content =
                        "AYARLAR\n" +
                        "════════\n\n" +
                        "BlancoMonitor davranışını Ayarlar ekranından yapılandırın.\n\n" +
                        "GENEL:\n" +
                        "  • Zaman Aşımı — İstek başına maksimum bekleme süresi\n" +
                        "  • Eşzamanlılık — Paralel istek sayısı\n" +
                        "  • Gecikme — İstekler arasındaki duraklama\n" +
                        "  • User Agent — İsteklerle gönderilen HTTP User-Agent dizesi\n\n" +
                        "EŞİK DEĞERLERİ:\n" +
                        "  • Uyarı seviyesi — Bu değerin üstü uyarı tetikler\n" +
                        "  • Kritik seviyesi — Bu değerin üstü kritik alarm tetikler\n\n" +
                        "DİL VE GÜNCELLEMELER:\n" +
                        "  • Tercih edilen dili seçin (İngilizce/Almanca/Türkçe)\n" +
                        "  • Otomatik güncelleme kontrolünü etkinleştirin/devre dışı bırakın"
                },
                new HelpTopic
                {
                    Key = "modules.updater",
                    Title = "Güncelleme Sistemi",
                    Content =
                        "GÜNCELLEME SİSTEMİ\n" +
                        "════════════════════\n\n" +
                        "BlancoMonitor otomatik güncelleme mekanizması içerir.\n\n" +
                        "NASIL ÇALIŞIR:\n" +
                        "  1. Başlangıçta uygulama uzak güncelleme manifestosunu kontrol eder\n" +
                        "  2. Daha yeni bir sürüm varsa bilgilendirilirsiniz\n" +
                        "  3. Güncelleme diyaloğu şunları gösterir:\n" +
                        "     • Mevcut ve en son sürüm numarası\n" +
                        "     • İndirme boyutu\n" +
                        "     • Sürüm notları\n" +
                        "  4. Şunları seçebilirsiniz:\n" +
                        "     • ŞİMDİ GÜNCELLE\n" +
                        "     • SONRA — Bir sonraki başlangıçta hatırlat\n" +
                        "     • BU SÜRÜMÜ ATLA\n\n" +
                        "GÜVENLİK:\n" +
                        "  • İndirmeler SHA256 ile doğrulanır\n" +
                        "  • Güncelleme öncesi yedek oluşturulur\n" +
                        "  • Başarısızlık durumunda önceki sürüm geri yüklenir"
                }
            ]
        },
        new HelpTopic
        {
            Key = "safety",
            Title = "Güvenlik ve İzleme Sınırları",
            IconHint = "shield",
            Content =
                "GÜVENLİK VE MÜDAHALESİZ İZLEME SINIRLARI\n" +
                "═══════════════════════════════════════════════\n\n" +
                "BlancoMonitor tamamen güvenli ve müdahalesiz olacak\n" +
                "şekilde tasarlanmıştır.\n\n" +
                "BU UYGULAMA ŞUNLARI YAPMAZ:\n" +
                "  ✗ XSS (Cross-Site Scripting) payload'ları göndermez\n" +
                "  ✗ SQL Injection veya enjeksiyon saldırıları denemez\n" +
                "  ✗ Kaba kuvvet veya kimlik bilgisi saldırıları yapmaz\n" +
                "  ✗ Yetkilendirme veya güvenlik kontrollerini atlatmaz\n" +
                "  ✗ Hedef sisteme yapay yük bindirmez\n" +
                "  ✗ DDoS veya yüksek yoğunluklu saldırı simülasyonu yapmaz\n" +
                "  ✗ Fuzzing, penetrasyon testi veya zafiyet taraması yapmaz\n\n" +
                "TEKNİK GÜVENLİK ÖNLEMLERİ:\n\n" +
                "  ⛨ HIZ SINIRLAMASI\n" +
                "    İstekler arası yapılandırılabilir gecikme (varsayılan 500ms).\n\n" +
                "  ⛨ KAPSAM KISITLAMASI\n" +
                "    İzleme yapılandırılmış alan sınırları dahilinde kalır.\n\n" +
                "  ⛨ ALAN ADI BEYAZ LİSTESİ\n" +
                "    Yalnızca yapılandırılmış URL'ler ile iletişim kurulur.\n\n" +
                "  ⛨ GÜVENLİ HTTP METOTLARI\n" +
                "    Yalnızca GET. POST, PUT, PATCH, DELETE kullanılmaz.\n\n" +
                "  ⛨ KONTROLLÜ HIZ\n" +
                "    Sıralı işleme, saniyede en fazla 1-2 istek.\n\n" +
                "  ⛨ TANIMLI USER-AGENT\n" +
                "    Her istekte 'BlancoMonitor/1.0' User-Agent dizesi.\n\n" +
                "KULLANIM POLİTİKASI:\n" +
                "  Yalnızca sahip olduğunuz veya izleme izniniz olan\n" +
                "  web sitelerinin yetkili performans izlemesi için kullanın."
        },
        new HelpTopic
        {
            Key = "localization",
            Title = "Dil Desteği",
            IconHint = "globe",
            Content =
                "DİL DESTEĞİ\n" +
                "═════════════\n\n" +
                "BlancoMonitor birden fazla dili destekler:\n\n" +
                "  • English (varsayılan)\n" +
                "  • Deutsch (Almanca)\n" +
                "  • Türkçe\n\n" +
                "DİL DEĞİŞTİRME:\n" +
                "  1. DAHA FAZLA menüsünden Ayarlar'ı açın\n" +
                "  2. 'Dil ve Güncellemeler' bölümünü bulun\n" +
                "  3. Açılır menüden tercih ettiğiniz dili seçin\n" +
                "  4. Kaydet'e tıklayın\n" +
                "  5. Değişikliklerin geçerli olması için uygulamayı yeniden başlatın\n\n" +
                "Dil tercihiniz otomatik olarak kaydedilir ve her\n" +
                "sonraki başlatmada uygulanır."
        },
        new HelpTopic
        {
            Key = "faq",
            Title = "SSS",
            IconHint = "question",
            Content =
                "SIK SORULAN SORULAR\n" +
                "═════════════════════\n\n" +
                "S: Uygulama gerçek bir Chrome tarayıcı açıyor mu?\n" +
                "C: Hayır. BlancoMonitor headless HTTP istemcisi kullanır.\n" +
                "   Görünür bir tarayıcı penceresi açılmaz.\n\n" +
                "S: Uygulama hedef web sitesini aşırı yükleyebilir mi?\n" +
                "C: Hayır. Uygulama hız sınırlaması ve sıralı işleme\n" +
                "   kullanır. Oluşturulan trafik, yavaş gezinen tek bir\n" +
                "   kullanıcıya eşdeğerdir. Aşırı yükleme riski yoktur.\n\n" +
                "S: Uyarı ve Kritik arasındaki fark nedir?\n" +
                "C: Uyarı bir performans endişesini gösterir — sayfa\n" +
                "   yapılandırılmış eşikten yavaş yanıt vermiş ancak\n" +
                "   hâlâ çalışıyor. Kritik ciddi bir sorunu gösterir —\n" +
                "   sayfa erişilemez, hata döndürüyor veya temel\n" +
                "   kaynaklar yüklenemiyor.\n\n" +
                "S: Ekran görüntüleri her zaman alınıyor mu?\n" +
                "C: Hayır. Ekran görüntüleri Playwright kurulumunu gerektirir\n" +
                "   ve Ayarlar'da etkinleştirilmelidir.\n\n" +
                "S: Otomatik güncelleme nasıl çalışır?\n" +
                "C: Başlangıçta BlancoMonitor yeni sürümler için uzak\n" +
                "   manifestoyu kontrol eder. İndirmeler SHA256 ile\n" +
                "   doğrulanır. Güncelleme başarısız olursa önceki sürüm\n" +
                "   otomatik olarak geri yüklenir.\n\n" +
                "S: Dili sonradan değiştirebilir miyim?\n" +
                "C: Evet. Ayarlar'da dili değiştirin, kaydedin ve\n" +
                "   uygulamayı yeniden başlatın. Tercihiniz hatırlanır.\n\n" +
                "S: İzleme sonuçları nerede saklanıyor?\n" +
                "C: Uygulama veri dizininizdeki yerel SQLite veritabanında.\n" +
                "   Raporlar HTML, JSON veya CSV olarak dışa aktarılabilir.\n\n" +
                "S: Bu aracı güvenlik testi için kullanabilir miyim?\n" +
                "C: Hayır. BlancoMonitor yalnızca bir performans izleme\n" +
                "   aracıdır. Güvenlik testi, zafiyet taraması veya\n" +
                "   exploit denemesi gerçekleştirmez."
        }
    ];
}
