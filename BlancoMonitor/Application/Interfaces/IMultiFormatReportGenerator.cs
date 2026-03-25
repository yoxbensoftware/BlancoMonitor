using BlancoMonitor.Application.Dto;

namespace BlancoMonitor.Application.Interfaces;

/// <summary>
/// Application-layer interface for multi-format report generation.
/// Extends beyond the legacy IReportGenerator with structured ReportData support.
/// </summary>
public interface IMultiFormatReportGenerator
{
    /// <summary>Generate full HTML report from assembled report data.</summary>
    Task<string> GenerateHtmlAsync(ReportData data, string outputDirectory, CancellationToken ct = default);

    /// <summary>Generate JSON report from assembled report data.</summary>
    Task<string> GenerateJsonAsync(ReportData data, string outputDirectory, CancellationToken ct = default);

    /// <summary>Generate CSV export from assembled report data.</summary>
    Task<string> GenerateCsvAsync(ReportData data, string outputDirectory, CancellationToken ct = default);

    /// <summary>Generate all report formats and return paths.</summary>
    Task<ReportPaths> GenerateAllAsync(ReportData data, string outputDirectory, CancellationToken ct = default);
}

/// <summary>Paths to all generated report files.</summary>
public sealed class ReportPaths
{
    public string? HtmlPath { get; set; }
    public string? JsonPath { get; set; }
    public string? CsvPath { get; set; }
}
