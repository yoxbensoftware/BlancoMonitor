namespace BlancoMonitor.Domain.Interfaces;

public interface IEvidenceCollector
{
    bool IsAvailable { get; }
    Task<string?> CaptureScreenshotAsync(string url, string outputDirectory, CancellationToken ct = default);
}
