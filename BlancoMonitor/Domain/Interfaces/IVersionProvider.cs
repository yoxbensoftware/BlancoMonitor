namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Provides the current application version using v.NNNN format.
/// Reads from assembly metadata at runtime.
/// </summary>
public interface IVersionProvider
{
    /// <summary>Display version, e.g. "v.0001".</summary>
    string DisplayVersion { get; }

    /// <summary>Numeric build number, e.g. 1.</summary>
    int BuildNumber { get; }

    /// <summary>Full assembly version for diagnostics.</summary>
    string FullVersion { get; }
}
