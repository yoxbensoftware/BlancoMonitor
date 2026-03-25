namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Checks for application updates from a remote manifest and
/// downloads / applies patches. Does NOT talk to Git directly.
/// </summary>
public interface IUpdateService
{
    /// <summary>Check if a newer version is available.</summary>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <summary>Download the update package to a temp location.</summary>
    Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>Apply a downloaded update and restart the application.</summary>
    void ApplyUpdateAndRestart(string downloadedPackagePath);
}

/// <summary>
/// Describes an available update returned from the remote manifest.
/// </summary>
public sealed class UpdateInfo
{
    public required string Version { get; init; }
    public required int BuildNumber { get; init; }
    public required string DownloadUrl { get; init; }
    public required long SizeBytes { get; init; }
    public required string ReleaseNotes { get; init; }
    public required DateTime ReleasedAt { get; init; }
    public string? Sha256Hash { get; init; }
    public bool IsMandatory { get; init; }
}
