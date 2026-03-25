using System.Diagnostics;
using Microsoft.Win32;

namespace BlancoMonitor.UI.Utilities;

/// <summary>
/// Opens URLs in Google Chrome if installed, otherwise falls back to the
/// system default browser. Supports http/https/file:// schemes.
/// </summary>
public static class BrowserHelper
{
    private static readonly string[] ChromePaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Google\Chrome\Application\chrome.exe"),
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    ];

    /// <summary>Opens the given URL in Chrome if available, otherwise the default browser.</summary>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        // Ensure the URL has a scheme
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out _)) return;

        // Try Chrome first
        var chromePath = GetChromePath();
        if (chromePath is not null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = chromePath,
                    Arguments = $"\"{url}\"",
                    UseShellExecute = false,
                });
                return;
            }
            catch { /* fall through to default */ }
        }

        // Fallback: default browser
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open URL:\n{url}\n\n{ex.Message}",
                "Browser Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>Opens a local file in the default associated application (e.g. HTML in browser).</summary>
    public static void OpenFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        var chromePath = GetChromePath();
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (chromePath is not null && (ext == ".html" || ext == ".htm"))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = chromePath,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                });
                return;
            }
            catch { /* fall through */ }
        }

        Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
    }

    /// <summary>Returns true if the given string looks like a clickable URL.</summary>
    public static bool IsUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("www.", StringComparison.OrdinalIgnoreCase));

    // ── Internals ────────────────────────────────────────────────

    private static string? _cachedChromePath;
    private static bool _chromeLookedUp;

    private static string? GetChromePath()
    {
        if (_chromeLookedUp) return _cachedChromePath;
        _chromeLookedUp = true;

        // 1. Check known file system locations
        foreach (var path in ChromePaths)
        {
            if (File.Exists(path))
            {
                _cachedChromePath = path;
                return path;
            }
        }

        // 2. Check registry (HKCU then HKLM)
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using var key = root.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
                var regPath = key?.GetValue(null)?.ToString();
                if (regPath is not null && File.Exists(regPath))
                {
                    _cachedChromePath = regPath;
                    return regPath;
                }
            }
            catch { /* registry may be unavailable */ }
        }

        return null;
    }
}
