namespace BlancoMonitor.Domain.Entities;

/// <summary>
/// Represents a single help topic node in the documentation tree.
/// Supports hierarchical structure with parent-child relationships.
/// </summary>
public sealed class HelpTopic
{
    /// <summary>Unique key used to identify this topic (e.g. "overview", "modules.monitoring").</summary>
    public required string Key { get; init; }

    /// <summary>Localized display title shown in the TreeView.</summary>
    public required string Title { get; init; }

    /// <summary>Localized rich-text content (plain text with newlines) displayed in the content panel.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Optional icon hint for TreeView rendering (e.g. "info", "shield", "gear").</summary>
    public string IconHint { get; init; } = string.Empty;

    /// <summary>Child topics — enables nested tree hierarchy.</summary>
    public List<HelpTopic> Children { get; init; } = [];
}
