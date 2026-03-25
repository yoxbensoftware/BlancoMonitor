namespace BlancoMonitor.Domain.Interfaces;

/// <summary>
/// Provides localized strings for the application UI.
/// Supports runtime language switching and fallback to English.
/// </summary>
public interface ILocalizationService
{
    /// <summary>Current language code (e.g. "en", "de", "tr").</summary>
    string CurrentLanguage { get; }

    /// <summary>Available language codes.</summary>
    IReadOnlyList<string> AvailableLanguages { get; }

    /// <summary>Get localized string by key. Falls back to English, then returns key itself.</summary>
    string Get(string key);

    /// <summary>Get localized string with format arguments.</summary>
    string Get(string key, params object[] args);

    /// <summary>Get display name for a language code (e.g. "en" → "English").</summary>
    string GetLanguageDisplayName(string languageCode);

    /// <summary>Change current language. Fires <see cref="LanguageChanged"/>.</summary>
    void SetLanguage(string languageCode);

    /// <summary>Raised when language changes — UI forms should re-apply labels.</summary>
    event EventHandler? LanguageChanged;
}
