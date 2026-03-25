using BlancoMonitor.Domain.ValueObjects;

namespace BlancoMonitor.Domain.Entities;

public sealed class AppConfiguration
{
    public List<MonitorTarget> Targets { get; set; } = [];
    public Dictionary<string, Threshold> Thresholds { get; set; } = new()
    {
        ["TotalTimeMs"] = new Threshold("TotalTimeMs", 3000, 10000),
        ["TimeToFirstByteMs"] = new Threshold("TimeToFirstByteMs", 1000, 5000),
        ["ContentDownloadMs"] = new Threshold("ContentDownloadMs", 2000, 8000),
    };
    public string LogDirectory { get; set; } = "Logs";
    public string ReportDirectory { get; set; } = "Reports";
    public string HistoryDirectory { get; set; } = "History";
    public string EvidenceDirectory { get; set; } = "Evidence";
    public string DataDirectory { get; set; } = "Data";
    public int MaxConcurrentRequests { get; set; } = 2;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int DelayBetweenRequestsMs { get; set; } = 500;
    public string UserAgent { get; set; } = "BlancoMonitor/1.0 (Non-Invasive Site Monitor)";
    public List<string> IgnorePatterns { get; set; } = ["*.pdf", "*.zip", "*.exe"];
    public List<string> Whitelist { get; set; } = [];
    public bool ScreenshotEnabled { get; set; } = false;
    public int HistoryRetentionDays { get; set; } = 90;
}
