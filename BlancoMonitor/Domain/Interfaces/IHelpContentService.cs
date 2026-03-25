using BlancoMonitor.Domain.Entities;

namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Provides localized help documentation content organized as a tree hierarchy.
/// Content is keyed by language code and returns a tree of <see cref="HelpTopic"/> nodes.
/// </summary>
public interface IHelpContentService
{
    /// <summary>
    /// Returns the root-level help topics for the specified language.
    /// Each topic may contain children, forming the full documentation tree.
    /// Falls back to English if the requested language is not available.
    /// </summary>
    IReadOnlyList<HelpTopic> GetTopics(string languageCode);

    /// <summary>Supported language codes (e.g. "en", "de", "tr").</summary>
    IReadOnlyList<string> SupportedLanguages { get; }
}
