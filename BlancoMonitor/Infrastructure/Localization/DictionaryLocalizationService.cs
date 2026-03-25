using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Localization;

/// <summary>
/// Dictionary-based localization service. Translations are embedded in code
/// for zero-dependency deployment. New languages: add a dictionary + register it.
/// Fallback chain: requested language → English → raw key.
/// </summary>
public sealed class DictionaryLocalizationService : ILocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _current = null!;
    private Dictionary<string, string> _fallback = null!;

    public string CurrentLanguage { get; private set; } = "en";
    public IReadOnlyList<string> AvailableLanguages { get; }

    public event EventHandler? LanguageChanged;

    public DictionaryLocalizationService(string initialLanguage = "en")
    {
        RegisterEnglish();
        RegisterGerman();
        RegisterTurkish();

        AvailableLanguages = _translations.Keys.Order().ToList().AsReadOnly();

        _fallback = _translations["en"];
        SetLanguage(initialLanguage);
    }

    public string Get(string key)
    {
        if (_current.TryGetValue(key, out var value))
            return value;
        if (_fallback.TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    public string Get(string key, params object[] args)
    {
        var template = Get(key);
        try { return string.Format(template, args); }
        catch { return template; }
    }

    public string GetLanguageDisplayName(string languageCode) => languageCode.ToLowerInvariant() switch
    {
        "en" => "English",
        "de" => "Deutsch",
        "tr" => "Türkçe",
        _ => languageCode,
    };

    public void SetLanguage(string languageCode)
    {
        var code = languageCode.ToLowerInvariant();
        if (!_translations.TryGetValue(code, out var dict))
        {
            code = "en";
            dict = _fallback;
        }

        CurrentLanguage = code;
        _current = dict;
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    // ════════════════════════════════════════════════════════════
    //  ENGLISH
    // ════════════════════════════════════════════════════════════

    private void RegisterEnglish()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ── Window titles ───────────────────────────────────────
        d["App.Title"] = "⬡ BLANCO MONITOR {0}";
        d["Dashboard.Title"] = "Dashboard";
        d["Settings.Title"] = "Settings";
        d["About.Title"] = "About — BlancoMonitor";
        d["Monitoring.Title"] = "Monitoring";
        d["Results.Title"] = "Reports";
        d["UrlManager.Title"] = "URL / Keyword Management";
        d["ScenarioManager.Title"] = "Scenario Management";
        d["DiscoveryTools.Title"] = "Discovery Tools";
        d["LiveMonitoring.Title"] = "Live Monitoring";
        d["WarningsCritical.Title"] = "Warnings / Critical";
        d["NetworkExplorer.Title"] = "Network Explorer";
        d["EvidenceViewer.Title"] = "Evidence Viewer";
        d["History.Title"] = "History";

        // ── Menu items ──────────────────────────────────────────
        d["Menu.NewRun"] = "&New Monitoring Run";
        d["Menu.UrlMgmt"] = "&URL / Keyword Mgmt";
        d["Menu.Scenarios"] = "&Scenario Mgmt";
        d["Menu.Discovery"] = "&Discovery Tools";
        d["Menu.Live"] = "&Live Monitoring";
        d["Menu.Warnings"] = "&Warnings / Critical";
        d["Menu.Network"] = "N&etwork Explorer";
        d["Menu.Reports"] = "&Reports";
        d["Menu.History"] = "&History";
        d["Menu.More"] = "▸ MORE";
        d["Menu.Settings"] = "&Settings";
        d["Menu.About"] = "&About";
        d["Menu.Dashboard"] = "&Dashboard";
        d["Menu.Exit"] = "E&xit";
        d["Menu.Window"] = "&WINDOW";
        d["Menu.Cascade"] = "&Cascade";
        d["Menu.TileH"] = "Tile &Horizontal";
        d["Menu.TileV"] = "Tile &Vertical";
        d["Menu.Help"] = "&HELP";

        // ── Help form ──────────────────────────────────────────
        d["Help.Title"] = "Help — BlancoMonitor";
        d["Help.Header"] = "⬡ BLANCOMONITOR HELP";
        d["Help.Search"] = "Search:";
        d["Help.SelectSubtopic"] = "Select a subtopic from the tree for details.";

        // ── Common buttons
        d["Btn.Save"] = "💾 SAVE";
        d["Btn.Close"] = "CLOSE";
        d["Btn.Cancel"] = "CANCEL";
        d["Btn.Next"] = "NEXT ▸";
        d["Btn.Back"] = "◂ BACK";
        d["Btn.Start"] = "▶ START";
        d["Btn.Stop"] = "■ STOP";
        d["Btn.Refresh"] = "↻ REFRESH";
        d["Btn.AcceptClose"] = "ACCEPT & CLOSE";

        // ── Status bar ──────────────────────────────────────────
        d["Status.Ready"] = "READY";
        d["Status.Running"] = "RUNNING...";
        d["Status.Completed"] = "COMPLETED";
        d["Status.Error"] = "ERROR";

        // ── Settings form ───────────────────────────────────────
        d["Settings.Header"] = "▸ CONFIGURATION";
        d["Settings.General"] = "GENERAL";
        d["Settings.Timeout"] = "Timeout (s):";
        d["Settings.Concurrency"] = "Concurrency:";
        d["Settings.Delay"] = "Delay (ms):";
        d["Settings.UserAgent"] = "User Agent:";
        d["Settings.Screenshots"] = "Enable screenshots (requires Playwright)";
        d["Settings.Ignore"] = "Ignore:";
        d["Settings.Thresholds"] = "THRESHOLDS (ms)";
        d["Settings.Warning"] = "WARNING";
        d["Settings.Critical"] = "CRITICAL";
        d["Settings.Ttfb"] = "TTFB:";
        d["Settings.TotalTime"] = "Total Time:";
        d["Settings.Download"] = "Download:";
        d["Settings.Saved"] = "Settings saved";
        d["Settings.Language"] = "Language:";
        d["Settings.LanguageGroup"] = "LANGUAGE & UPDATES";
        d["Settings.CheckUpdates"] = "Check for updates on startup";
        d["Settings.LanguageRestart"] = "Language will be applied on next restart.";

        // ── About screen ────────────────────────────────────────
        d["About.Version"] = "Version {0}  —  .NET 10  —  Developed by Oz";
        d["About.Tagline"] = "Non-invasive website performance monitoring & evidence collection tool";
        d["About.WhatDoes"] = "✓  WHAT THIS TOOL DOES";
        d["About.WhatDoesNot"] = "✗  WHAT THIS TOOL DOES NOT DO";
        d["About.Safeguards"] = "⛨  TECHNICAL SAFEGUARDS";
        d["About.HowItWorks"] = "◈  HOW IT WORKS";
        d["About.Technology"] = "⚙  TECHNOLOGY";
        d["About.UsagePolicy"] = "⚖  USAGE POLICY";
        d["About.PolicyToggle"] = "I understand and accept the usage policy";

        d["About.Does.1"] = "Monitors website performance by sending standard HTTP GET requests";
        d["About.Does.2"] = "Simulates realistic user navigation patterns with controlled pacing";
        d["About.Does.3"] = "Collects response timing data (TTFB, download, total load time)";
        d["About.Does.4"] = "Analyzes network requests and resource loading performance";
        d["About.Does.5"] = "Detects potential issues: slow pages, broken resources, errors";
        d["About.Does.6"] = "Generates detailed HTML, JSON, and CSV performance reports";
        d["About.Does.7"] = "Compares runs to identify regressions and improvements";

        d["About.DoesNot.1"] = "No XSS (Cross-Site Scripting) attacks — no scripts are injected";
        d["About.DoesNot.2"] = "No SQL injection — no database queries are sent to targets";
        d["About.DoesNot.3"] = "No exploit attempts — no vulnerability scanning or penetration testing";
        d["About.DoesNot.4"] = "No brute force — no authentication or login attempts";
        d["About.DoesNot.5"] = "No load testing — request rate is deliberately limited";
        d["About.DoesNot.6"] = "No DDoS behavior — single-threaded, paced requests only";
        d["About.DoesNot.7"] = "No security bypass — no attempts to circumvent access controls";
        d["About.DoesNot.8"] = "No form submissions — no POST/PUT/DELETE by default";
        d["About.DoesNot.9"] = "No cookie theft — no credential harvesting or session hijacking";

        d["About.HowItWorks.Body"] = "BlancoMonitor behaves like a well-mannered web browser. It visits\n" +
                                      "pages at a controlled pace, records timing data, and analyzes the\n" +
                                      "results — exactly how a human user would experience the site,\n" +
                                      "but with precise measurements.\n\n" +
                                      "All requests use standard HTTP GET with a clearly identified\n" +
                                      "User-Agent string. Server administrators can easily identify\n" +
                                      "and allow/block this traffic. The tool respects robots.txt,\n" +
                                      "rate limits, and domain boundaries.";

        d["About.Policy.Body"] = "This tool is designed exclusively for authorized performance\n" +
                                  "monitoring of websites you own or have explicit permission to\n" +
                                  "monitor. Users are responsible for ensuring compliance with\n" +
                                  "applicable laws, terms of service, and organizational policies.\n\n" +
                                  "By using this tool, you acknowledge that:\n" +
                                  "  • You have authorization to monitor the target websites\n" +
                                  "  • You will not modify configurations to perform attacks\n" +
                                  "  • You accept responsibility for your usage of this tool\n" +
                                  "  • Generated reports may contain sensitive timing data";

        d["About.Safeguard.RateLimit.Name"] = "Rate Limiting";
        d["About.Safeguard.RateLimit.Detail"] = "Configurable delay between requests (default 500ms). Prevents any resemblance to automated attack or scraping behavior.";
        d["About.Safeguard.Scope.Name"] = "Scope Restriction";
        d["About.Safeguard.Scope.Detail"] = "Monitoring stays within configured domain boundaries. The tool never follows links to external domains or expands scope.";
        d["About.Safeguard.Whitelist.Name"] = "Domain Whitelist";
        d["About.Safeguard.Whitelist.Detail"] = "Only explicitly configured URLs and domains are contacted. No automatic discovery outside the defined target set.";
        d["About.Safeguard.Methods.Name"] = "Safe HTTP Methods";
        d["About.Safeguard.Methods.Detail"] = "Only HTTP GET requests by default. No POST, PUT, PATCH, or DELETE methods are used — no server-side state is modified.";
        d["About.Safeguard.Pacing.Name"] = "Controlled Pacing";
        d["About.Safeguard.Pacing.Detail"] = "Sequential request processing with configurable concurrency. Typical traffic profile: 1-2 requests per second maximum.";
        d["About.Safeguard.UserAgent.Name"] = "Identified User-Agent";
        d["About.Safeguard.UserAgent.Detail"] = "All requests carry the 'BlancoMonitor/1.0' User-Agent string. Server admins can easily identify and manage this traffic.";

        // ── Update dialog ───────────────────────────────────────
        d["Update.Title"] = "Update Available";
        d["Update.Header"] = "A new version of BlancoMonitor is available!";
        d["Update.Current"] = "Current version:";
        d["Update.Latest"] = "Latest version:";
        d["Update.Size"] = "Download size:";
        d["Update.ReleaseNotes"] = "Release notes:";
        d["Update.Confirm"] = "Would you like to update now?";
        d["Update.BtnUpdate"] = "UPDATE NOW";
        d["Update.BtnLater"] = "LATER";
        d["Update.BtnSkip"] = "SKIP THIS VERSION";
        d["Update.Downloading"] = "Downloading update... {0}%";
        d["Update.Applying"] = "Applying update — the application will restart...";
        d["Update.Failed"] = "Update failed: {0}";
        d["Update.UpToDate"] = "You are running the latest version.";
        d["Update.CheckFailed"] = "Could not check for updates: {0}";
        d["Update.RestartRequired"] = "The application will restart to complete the update.";

        _translations["en"] = d;
    }

    // ════════════════════════════════════════════════════════════
    //  GERMAN
    // ════════════════════════════════════════════════════════════

    private void RegisterGerman()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        d["App.Title"] = "⬡ BLANCO MONITOR {0}";
        d["Dashboard.Title"] = "Übersicht";
        d["Settings.Title"] = "Einstellungen";
        d["About.Title"] = "Über — BlancoMonitor";
        d["Monitoring.Title"] = "Überwachung";
        d["Results.Title"] = "Berichte";
        d["UrlManager.Title"] = "URL / Stichwort-Verwaltung";
        d["ScenarioManager.Title"] = "Szenario-Verwaltung";
        d["DiscoveryTools.Title"] = "Entdeckungswerkzeuge";
        d["LiveMonitoring.Title"] = "Live-Überwachung";
        d["WarningsCritical.Title"] = "Warnungen / Kritisch";
        d["NetworkExplorer.Title"] = "Netzwerk-Explorer";
        d["EvidenceViewer.Title"] = "Beweisbetrachter";
        d["History.Title"] = "Verlauf";

        d["Menu.NewRun"] = "&Neue Überwachung";
        d["Menu.UrlMgmt"] = "&URL / Stichwörter";
        d["Menu.Scenarios"] = "&Szenarien";
        d["Menu.Discovery"] = "Ent&deckung";
        d["Menu.Live"] = "&Live-Überwachung";
        d["Menu.Warnings"] = "&Warnungen / Kritisch";
        d["Menu.Network"] = "&Netzwerk-Explorer";
        d["Menu.Reports"] = "&Berichte";
        d["Menu.History"] = "Ver&lauf";
        d["Menu.More"] = "▸ MEHR";
        d["Menu.Settings"] = "&Einstellungen";
        d["Menu.About"] = "Ü&ber";
        d["Menu.Dashboard"] = "Ü&bersicht";
        d["Menu.Exit"] = "&Beenden";
        d["Menu.Window"] = "&FENSTER";
        d["Menu.Cascade"] = "&Kaskade";
        d["Menu.TileH"] = "&Horizontal anordnen";
        d["Menu.TileV"] = "&Vertikal anordnen";
        d["Menu.Help"] = "&HILFE";

        d["Help.Title"] = "Hilfe — BlancoMonitor";
        d["Help.Header"] = "⬡ BLANCOMONITOR HILFE";
        d["Help.Search"] = "Suche:";
        d["Help.SelectSubtopic"] = "Wählen Sie ein Unterthema aus dem Baum für Details.";

        d["Btn.Save"] = "💾 SPEICHERN";
        d["Btn.Close"] = "SCHLIEßEN";
        d["Btn.Cancel"] = "ABBRECHEN";
        d["Btn.Next"] = "WEITER ▸";
        d["Btn.Back"] = "◂ ZURÜCK";
        d["Btn.Start"] = "▶ STARTEN";
        d["Btn.Stop"] = "■ STOPP";
        d["Btn.Refresh"] = "↻ AKTUALISIEREN";
        d["Btn.AcceptClose"] = "AKZEPTIEREN & SCHLIEßEN";

        d["Status.Ready"] = "BEREIT";
        d["Status.Running"] = "LÄUFT...";
        d["Status.Completed"] = "ABGESCHLOSSEN";
        d["Status.Error"] = "FEHLER";

        d["Settings.Header"] = "▸ KONFIGURATION";
        d["Settings.General"] = "ALLGEMEIN";
        d["Settings.Timeout"] = "Zeitlimit (s):";
        d["Settings.Concurrency"] = "Parallelität:";
        d["Settings.Delay"] = "Verzögerung (ms):";
        d["Settings.UserAgent"] = "User Agent:";
        d["Settings.Screenshots"] = "Screenshots aktivieren (erfordert Playwright)";
        d["Settings.Ignore"] = "Ignorieren:";
        d["Settings.Thresholds"] = "SCHWELLENWERTE (ms)";
        d["Settings.Warning"] = "WARNUNG";
        d["Settings.Critical"] = "KRITISCH";
        d["Settings.Ttfb"] = "TTFB:";
        d["Settings.TotalTime"] = "Gesamtzeit:";
        d["Settings.Download"] = "Download:";
        d["Settings.Saved"] = "Einstellungen gespeichert";
        d["Settings.Language"] = "Sprache:";
        d["Settings.LanguageGroup"] = "SPRACHE & UPDATES";
        d["Settings.CheckUpdates"] = "Beim Start nach Updates suchen";
        d["Settings.LanguageRestart"] = "Die Sprache wird beim nächsten Neustart angewendet.";

        d["About.Version"] = "Version {0}  —  .NET 10  —  Entwickelt von Oz";
        d["About.Tagline"] = "Nicht-invasives Website-Performance-Monitoring & Beweiserfassungstool";
        d["About.WhatDoes"] = "✓  WAS DIESES TOOL MACHT";
        d["About.WhatDoesNot"] = "✗  WAS DIESES TOOL NICHT MACHT";
        d["About.Safeguards"] = "⛨  TECHNISCHE SICHERHEITSVORKEHRUNGEN";
        d["About.HowItWorks"] = "◈  WIE ES FUNKTIONIERT";
        d["About.Technology"] = "⚙  TECHNOLOGIE";
        d["About.UsagePolicy"] = "⚖  NUTZUNGSRICHTLINIE";
        d["About.PolicyToggle"] = "Ich verstehe und akzeptiere die Nutzungsrichtlinie";

        d["About.Does.1"] = "Überwacht die Website-Leistung durch Standard-HTTP-GET-Anfragen";
        d["About.Does.2"] = "Simuliert realistische Benutzernavigation mit kontrolliertem Tempo";
        d["About.Does.3"] = "Erfasst Antwortzeiten (TTFB, Download, Gesamtladezeit)";
        d["About.Does.4"] = "Analysiert Netzwerkanfragen und Ressourcen-Ladeleistung";
        d["About.Does.5"] = "Erkennt potenzielle Probleme: langsame Seiten, defekte Ressourcen";
        d["About.Does.6"] = "Erstellt detaillierte Berichte in HTML, JSON und CSV";
        d["About.Does.7"] = "Vergleicht Durchläufe zur Erkennung von Regressionen";

        d["About.DoesNot.1"] = "Kein XSS — es werden keine Skripte injiziert";
        d["About.DoesNot.2"] = "Keine SQL-Injection — keine Datenbankabfragen an Ziele";
        d["About.DoesNot.3"] = "Keine Exploit-Versuche — kein Schwachstellenscanning";
        d["About.DoesNot.4"] = "Kein Brute-Force — keine Anmeldeversuche";
        d["About.DoesNot.5"] = "Kein Lasttest — Anfragerate bewusst begrenzt";
        d["About.DoesNot.6"] = "Kein DDoS — nur sequenzielle, getaktete Anfragen";
        d["About.DoesNot.7"] = "Keine Sicherheitsumgehung — kein Umgehen von Zugriffskontrollen";
        d["About.DoesNot.8"] = "Keine Formularübermittlungen — standardmäßig kein POST/PUT/DELETE";
        d["About.DoesNot.9"] = "Kein Cookie-Diebstahl — keine Sitzungsübernahme";

        d["About.HowItWorks.Body"] = "BlancoMonitor verhält sich wie ein wohlerzogener Webbrowser.\n" +
                                      "Es besucht Seiten in kontrolliertem Tempo, zeichnet Zeitdaten\n" +
                                      "auf und analysiert die Ergebnisse — genau so, wie ein Benutzer\n" +
                                      "die Website erleben würde, aber mit präzisen Messungen.\n\n" +
                                      "Alle Anfragen verwenden Standard-HTTP-GET mit einer klar\n" +
                                      "identifizierten User-Agent-Zeichenfolge.";

        d["About.Policy.Body"] = "Dieses Tool ist ausschließlich für die autorisierte\n" +
                                  "Leistungsüberwachung von Websites konzipiert, die Sie\n" +
                                  "besitzen oder für die Sie eine ausdrückliche Genehmigung\n" +
                                  "zur Überwachung haben.\n\n" +
                                  "Mit der Nutzung erkennen Sie an, dass:\n" +
                                  "  • Sie die Berechtigung zur Überwachung haben\n" +
                                  "  • Sie keine Konfigurationen für Angriffe ändern\n" +
                                  "  • Sie die Verantwortung für Ihre Nutzung übernehmen\n" +
                                  "  • Berichte sensible Zeitdaten enthalten können";

        d["About.Safeguard.RateLimit.Name"] = "Ratenbegrenzung";
        d["About.Safeguard.RateLimit.Detail"] = "Konfigurierbare Verzögerung zwischen Anfragen (Standard 500ms).";
        d["About.Safeguard.Scope.Name"] = "Bereichsbeschränkung";
        d["About.Safeguard.Scope.Detail"] = "Überwachung bleibt innerhalb konfigurierter Domänengrenzen.";
        d["About.Safeguard.Whitelist.Name"] = "Domänen-Whitelist";
        d["About.Safeguard.Whitelist.Detail"] = "Nur explizit konfigurierte URLs und Domänen werden kontaktiert.";
        d["About.Safeguard.Methods.Name"] = "Sichere HTTP-Methoden";
        d["About.Safeguard.Methods.Detail"] = "Standardmäßig nur HTTP-GET. Kein POST, PUT, PATCH oder DELETE.";
        d["About.Safeguard.Pacing.Name"] = "Kontrolliertes Tempo";
        d["About.Safeguard.Pacing.Detail"] = "Sequenzielle Verarbeitung. Maximal 1-2 Anfragen pro Sekunde.";
        d["About.Safeguard.UserAgent.Name"] = "Identifizierter User-Agent";
        d["About.Safeguard.UserAgent.Detail"] = "Alle Anfragen tragen den 'BlancoMonitor/1.0' User-Agent.";

        d["Update.Title"] = "Update verfügbar";
        d["Update.Header"] = "Eine neue Version von BlancoMonitor ist verfügbar!";
        d["Update.Current"] = "Aktuelle Version:";
        d["Update.Latest"] = "Neueste Version:";
        d["Update.Size"] = "Downloadgröße:";
        d["Update.ReleaseNotes"] = "Versionshinweise:";
        d["Update.Confirm"] = "Möchten Sie jetzt aktualisieren?";
        d["Update.BtnUpdate"] = "JETZT AKTUALISIEREN";
        d["Update.BtnLater"] = "SPÄTER";
        d["Update.BtnSkip"] = "VERSION ÜBERSPRINGEN";
        d["Update.Downloading"] = "Update wird heruntergeladen... {0}%";
        d["Update.Applying"] = "Update wird angewendet — Anwendung startet neu...";
        d["Update.Failed"] = "Update fehlgeschlagen: {0}";
        d["Update.UpToDate"] = "Sie verwenden die neueste Version.";
        d["Update.CheckFailed"] = "Updateprüfung nicht möglich: {0}";
        d["Update.RestartRequired"] = "Die Anwendung wird neu gestartet, um das Update abzuschließen.";

        _translations["de"] = d;
    }

    // ════════════════════════════════════════════════════════════
    //  TURKISH
    // ════════════════════════════════════════════════════════════

    private void RegisterTurkish()
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        d["App.Title"] = "⬡ BLANCO MONITOR {0}";
        d["Dashboard.Title"] = "Gösterge Paneli";
        d["Settings.Title"] = "Ayarlar";
        d["About.Title"] = "Hakkında — BlancoMonitor";
        d["Monitoring.Title"] = "İzleme";
        d["Results.Title"] = "Raporlar";
        d["UrlManager.Title"] = "URL / Anahtar Kelime Yönetimi";
        d["ScenarioManager.Title"] = "Senaryo Yönetimi";
        d["DiscoveryTools.Title"] = "Keşif Araçları";
        d["LiveMonitoring.Title"] = "Canlı İzleme";
        d["WarningsCritical.Title"] = "Uyarılar / Kritik";
        d["NetworkExplorer.Title"] = "Ağ Gezgini";
        d["EvidenceViewer.Title"] = "Kanıt Görüntüleyici";
        d["History.Title"] = "Geçmiş";

        d["Menu.NewRun"] = "&Yeni İzleme";
        d["Menu.UrlMgmt"] = "&URL / Anahtar Kelime";
        d["Menu.Scenarios"] = "&Senaryolar";
        d["Menu.Discovery"] = "Keşif &Araçları";
        d["Menu.Live"] = "&Canlı İzleme";
        d["Menu.Warnings"] = "U&yarılar / Kritik";
        d["Menu.Network"] = "A&ğ Gezgini";
        d["Menu.Reports"] = "&Raporlar";
        d["Menu.History"] = "&Geçmiş";
        d["Menu.More"] = "▸ DAHA FAZLA";
        d["Menu.Settings"] = "&Ayarlar";
        d["Menu.About"] = "&Hakkında";
        d["Menu.Dashboard"] = "&Gösterge Paneli";
        d["Menu.Exit"] = "&Çıkış";
        d["Menu.Window"] = "&PENCERE";
        d["Menu.Cascade"] = "&Basamaklı";
        d["Menu.TileH"] = "&Yatay Döşe";
        d["Menu.TileV"] = "&Dikey Döşe";
        d["Menu.Help"] = "&YARDIM";

        d["Help.Title"] = "Yardım — BlancoMonitor";
        d["Help.Header"] = "⬡ BLANCOMONITOR YARDIM";
        d["Help.Search"] = "Ara:";
        d["Help.SelectSubtopic"] = "Ayrıntılar için ağaçtan bir alt konu seçin.";

        d["Btn.Save"] = "💾 KAYDET";
        d["Btn.Close"] = "KAPAT";
        d["Btn.Cancel"] = "İPTAL";
        d["Btn.Next"] = "İLERİ ▸";
        d["Btn.Back"] = "◂ GERİ";
        d["Btn.Start"] = "▶ BAŞLAT";
        d["Btn.Stop"] = "■ DURDUR";
        d["Btn.Refresh"] = "↻ YENİLE";
        d["Btn.AcceptClose"] = "KABUL ET VE KAPAT";

        d["Status.Ready"] = "HAZIR";
        d["Status.Running"] = "ÇALIŞIYOR...";
        d["Status.Completed"] = "TAMAMLANDI";
        d["Status.Error"] = "HATA";

        d["Settings.Header"] = "▸ YAPILANDIRMA";
        d["Settings.General"] = "GENEL";
        d["Settings.Timeout"] = "Zaman Aşımı (s):";
        d["Settings.Concurrency"] = "Eşzamanlılık:";
        d["Settings.Delay"] = "Gecikme (ms):";
        d["Settings.UserAgent"] = "User Agent:";
        d["Settings.Screenshots"] = "Ekran görüntüleri etkinleştir (Playwright gerektirir)";
        d["Settings.Ignore"] = "Yoksay:";
        d["Settings.Thresholds"] = "EŞIK DEĞERLERİ (ms)";
        d["Settings.Warning"] = "UYARI";
        d["Settings.Critical"] = "KRİTİK";
        d["Settings.Ttfb"] = "TTFB:";
        d["Settings.TotalTime"] = "Toplam Süre:";
        d["Settings.Download"] = "İndirme:";
        d["Settings.Saved"] = "Ayarlar kaydedildi";
        d["Settings.Language"] = "Dil:";
        d["Settings.LanguageGroup"] = "DİL VE GÜNCELLEMELER";
        d["Settings.CheckUpdates"] = "Başlangıçta güncellemeleri kontrol et";
        d["Settings.LanguageRestart"] = "Dil bir sonraki yeniden başlatmada uygulanacaktır.";

        d["About.Version"] = "Sürüm {0}  —  .NET 10  —  Oz tarafından geliştirildi";
        d["About.Tagline"] = "Zarar vermeyen web sitesi performans izleme ve kanıt toplama aracı";
        d["About.WhatDoes"] = "✓  BU ARAÇ NE YAPAR";
        d["About.WhatDoesNot"] = "✗  BU ARAÇ NE YAPMAZ";
        d["About.Safeguards"] = "⛨  TEKNİK GÜVENLİK ÖNLEMLERİ";
        d["About.HowItWorks"] = "◈  NASIL ÇALIŞIR";
        d["About.Technology"] = "⚙  TEKNOLOJİ";
        d["About.UsagePolicy"] = "⚖  KULLANIM POLİTİKASI";
        d["About.PolicyToggle"] = "Kullanım politikasını anlıyor ve kabul ediyorum";

        d["About.Does.1"] = "Standart HTTP GET istekleri ile web sitesi performansını izler";
        d["About.Does.2"] = "Kontrollü hızda gerçekçi kullanıcı navigasyonu simüle eder";
        d["About.Does.3"] = "Yanıt zamanlama verilerini toplar (TTFB, indirme, toplam yükleme)";
        d["About.Does.4"] = "Ağ isteklerini ve kaynak yükleme performansını analiz eder";
        d["About.Does.5"] = "Potansiyel sorunları tespit eder: yavaş sayfalar, bozuk kaynaklar";
        d["About.Does.6"] = "HTML, JSON ve CSV formatlarında ayrıntılı raporlar oluşturur";
        d["About.Does.7"] = "Gerileme ve iyileştirmeleri belirlemek için çalıştırmaları karşılaştırır";

        d["About.DoesNot.1"] = "XSS yok — hiçbir betik enjekte edilmez";
        d["About.DoesNot.2"] = "SQL enjeksiyonu yok — hedeflere veritabanı sorgusu gönderilmez";
        d["About.DoesNot.3"] = "Exploit denemesi yok — güvenlik açığı taraması yapılmaz";
        d["About.DoesNot.4"] = "Kaba kuvvet yok — kimlik doğrulama denemesi yapılmaz";
        d["About.DoesNot.5"] = "Yük testi yok — istek hızı kasıtlı olarak sınırlıdır";
        d["About.DoesNot.6"] = "DDoS yok — yalnızca sıralı, tempolu istekler";
        d["About.DoesNot.7"] = "Güvenlik atlatma yok — erişim kontrollerini aşma girişimi yok";
        d["About.DoesNot.8"] = "Form gönderimi yok — varsayılan olarak POST/PUT/DELETE yok";
        d["About.DoesNot.9"] = "Çerez hırsızlığı yok — oturum ele geçirme yapılmaz";

        d["About.HowItWorks.Body"] = "BlancoMonitor terbiyeli bir web tarayıcısı gibi davranır.\n" +
                                      "Sayfaları kontrollü bir hızda ziyaret eder, zamanlama verilerini\n" +
                                      "kaydeder ve sonuçları analiz eder — tıpkı bir kullanıcının\n" +
                                      "siteyi deneyimleyeceği gibi, ancak kesin ölçümlerle.\n\n" +
                                      "Tüm istekler açıkça tanımlanmış bir User-Agent ile\n" +
                                      "standart HTTP GET kullanır.";

        d["About.Policy.Body"] = "Bu araç, yalnızca sahip olduğunuz veya izleme izniniz olan\n" +
                                  "web sitelerinin yetkili performans izlemesi için tasarlanmıştır.\n\n" +
                                  "Bu aracı kullanarak şunları kabul edersiniz:\n" +
                                  "  • Hedef web sitelerini izleme yetkiniz var\n" +
                                  "  • Saldırı yapmak için yapılandırmaları değiştirmeyeceksiniz\n" +
                                  "  • Kullanımınızın sorumluluğunu kabul ediyorsunuz\n" +
                                  "  • Raporlar hassas zamanlama verileri içerebilir";

        d["About.Safeguard.RateLimit.Name"] = "Hız Sınırlaması";
        d["About.Safeguard.RateLimit.Detail"] = "İstekler arası yapılandırılabilir gecikme (varsayılan 500ms).";
        d["About.Safeguard.Scope.Name"] = "Kapsam Kısıtlaması";
        d["About.Safeguard.Scope.Detail"] = "İzleme yapılandırılmış alan sınırları içinde kalır.";
        d["About.Safeguard.Whitelist.Name"] = "Alan Adı Beyaz Listesi";
        d["About.Safeguard.Whitelist.Detail"] = "Yalnızca açıkça yapılandırılmış URL'ler ve alan adları ile iletişim kurulur.";
        d["About.Safeguard.Methods.Name"] = "Güvenli HTTP Metotları";
        d["About.Safeguard.Methods.Detail"] = "Varsayılan olarak yalnızca HTTP GET. POST, PUT, PATCH veya DELETE kullanılmaz.";
        d["About.Safeguard.Pacing.Name"] = "Kontrollü Hız";
        d["About.Safeguard.Pacing.Detail"] = "Sıralı istek işleme. Saniyede en fazla 1-2 istek.";
        d["About.Safeguard.UserAgent.Name"] = "Tanımlı User-Agent";
        d["About.Safeguard.UserAgent.Detail"] = "Tüm istekler 'BlancoMonitor/1.0' User-Agent dizesini taşır.";

        d["Update.Title"] = "Güncelleme Mevcut";
        d["Update.Header"] = "BlancoMonitor'ün yeni bir sürümü mevcut!";
        d["Update.Current"] = "Mevcut sürüm:";
        d["Update.Latest"] = "En son sürüm:";
        d["Update.Size"] = "İndirme boyutu:";
        d["Update.ReleaseNotes"] = "Sürüm notları:";
        d["Update.Confirm"] = "Şimdi güncellemek ister misiniz?";
        d["Update.BtnUpdate"] = "ŞİMDİ GÜNCELLE";
        d["Update.BtnLater"] = "SONRA";
        d["Update.BtnSkip"] = "BU SÜRÜMÜ ATLA";
        d["Update.Downloading"] = "Güncelleme indiriliyor... %{0}";
        d["Update.Applying"] = "Güncelleme uygulanıyor — uygulama yeniden başlayacak...";
        d["Update.Failed"] = "Güncelleme başarısız: {0}";
        d["Update.UpToDate"] = "En son sürümü kullanıyorsunuz.";
        d["Update.CheckFailed"] = "Güncellemeler kontrol edilemedi: {0}";
        d["Update.RestartRequired"] = "Güncellemeyi tamamlamak için uygulama yeniden başlatılacak.";

        _translations["tr"] = d;
    }
}
