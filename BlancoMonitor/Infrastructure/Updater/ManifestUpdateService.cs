using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlancoMonitor.Domain.Interfaces;

namespace BlancoMonitor.Infrastructure.Updater;

/// <summary>
/// Manifest-based update service. Checks a remote JSON manifest for the
/// latest version, downloads the update package (ZIP), and launches
/// an updater shim that replaces files while the app is closed.
///
/// Architecture:
///   CI/CD push → build ZIP + update-manifest.json → host on static server
///   Client     → GET manifest → compare build numbers → download ZIP → apply
/// </summary>
public sealed class ManifestUpdateService : IUpdateService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IVersionProvider _versionProvider;
    private readonly IAppLogger _logger;
    private readonly string _manifestUrl;
    private readonly string _appDirectory;

    public ManifestUpdateService(
        IVersionProvider versionProvider,
        IAppLogger logger,
        string manifestUrl)
    {
        _versionProvider = versionProvider;
        _logger = logger;
        _manifestUrl = manifestUrl;
        _appDirectory = AppContext.BaseDirectory;

        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("BlancoMonitor-Updater/1.0");
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.Info($"Checking for updates at {_manifestUrl}");

            var manifest = await _http.GetFromJsonAsync<UpdateManifest>(_manifestUrl, JsonOpts, ct);
            if (manifest is null)
            {
                _logger.Warning("Update manifest returned null");
                return null;
            }

            if (manifest.LatestBuildNumber <= _versionProvider.BuildNumber)
            {
                _logger.Info($"Already up to date (current: {_versionProvider.BuildNumber}, latest: {manifest.LatestBuildNumber})");
                return null;
            }

            _logger.Info($"Update available: {manifest.LatestVersion} (build {manifest.LatestBuildNumber})");

            return new UpdateInfo
            {
                Version = manifest.LatestVersion,
                BuildNumber = manifest.LatestBuildNumber,
                DownloadUrl = manifest.DownloadUrl,
                SizeBytes = manifest.SizeBytes,
                ReleaseNotes = manifest.ReleaseNotes ?? "No release notes available.",
                ReleasedAt = manifest.ReleasedAt,
                Sha256Hash = manifest.Sha256Hash,
                IsMandatory = manifest.IsMandatory,
            };
        }
        catch (Exception ex)
        {
            _logger.Error("Update check failed", ex);
            return null;
        }
    }

    public async Task<string> DownloadUpdateAsync(UpdateInfo update, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        _logger.Info($"Downloading update {update.Version} from {update.DownloadUrl}");

        var tempDir = Path.Combine(Path.GetTempPath(), "BlancoMonitor_Update");
        Directory.CreateDirectory(tempDir);
        var targetPath = Path.Combine(tempDir, $"blanco_update_{update.BuildNumber}.zip");

        // Clean up any previous download
        if (File.Exists(targetPath))
            File.Delete(targetPath);

        using var response = await _http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? update.SizeBytes;
        long downloaded = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920);

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            downloaded += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)(downloaded * 100 / totalBytes);
                progress?.Report(percent);
            }
        }

        // Verify hash if provided
        if (!string.IsNullOrEmpty(update.Sha256Hash))
        {
            fileStream.Position = 0;
            var hashBytes = await SHA256.HashDataAsync(fileStream, ct);
            var hash = Convert.ToHexStringLower(hashBytes);

            if (!hash.Equals(update.Sha256Hash, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(targetPath);
                throw new InvalidOperationException("Update package hash verification failed — download may be corrupted.");
            }

            _logger.Info("Update hash verified successfully");
        }

        _logger.Info($"Update downloaded to {targetPath}");
        return targetPath;
    }

    public void ApplyUpdateAndRestart(string downloadedPackagePath)
    {
        // Strategy: Write a small PowerShell script that:
        // 1. Waits for this process to exit
        // 2. Extracts the ZIP over the app directory
        // 3. Relaunches the application
        // 4. Cleans up the temp files

        var appExePath = Environment.ProcessPath ?? Path.Combine(_appDirectory, "BlancoMonitor.exe");
        var scriptPath = Path.Combine(Path.GetTempPath(), "blanco_update.ps1");

        var backupDir = Path.Combine(_appDirectory, "_backup").Replace("\\", "\\\\");
        var appDir = _appDirectory.Replace("\\", "\\\\");
        var pkgPath = downloadedPackagePath.Replace("\\", "\\\\");
        var exePath = appExePath.Replace("\\", "\\\\");
        var sPath = scriptPath.Replace("\\", "\\\\");

        var script = string.Join(Environment.NewLine,
            "# BlancoMonitor Updater Script",
            "Start-Sleep -Seconds 2",
            "",
            "# Wait for process to fully exit",
            "$proc = Get-Process -Name \"BlancoMonitor\" -ErrorAction SilentlyContinue",
            "if ($proc) { $proc | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue }",
            "",
            "try {",
            "    # Backup current version",
            "    $backupDir = \"" + backupDir + "\"",
            "    if (Test-Path $backupDir) { Remove-Item $backupDir -Recurse -Force }",
            "    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null",
            "",
            "    Get-ChildItem -Path \"" + appDir + "\" -File | ForEach-Object {",
            "        if ($_.Name -ne '_backup') {",
            "            Copy-Item $_.FullName -Destination $backupDir -Force",
            "        }",
            "    }",
            "",
            "    # Extract update",
            "    Expand-Archive -Path \"" + pkgPath + "\" -DestinationPath \"" + appDir + "\" -Force",
            "",
            "    # Cleanup",
            "    Remove-Item \"" + pkgPath + "\" -Force -ErrorAction SilentlyContinue",
            "    Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue",
            "",
            "    # Relaunch",
            "    Start-Process \"" + exePath + "\"",
            "}",
            "catch {",
            "    # Rollback on failure",
            "    if (Test-Path $backupDir) {",
            "        Get-ChildItem -Path $backupDir -File | ForEach-Object {",
            "            Copy-Item $_.FullName -Destination \"" + appDir + "\" -Force",
            "        }",
            "        Remove-Item $backupDir -Recurse -Force -ErrorAction SilentlyContinue",
            "    }",
            "",
            "    [System.Windows.Forms.MessageBox]::Show(",
            "        \"Update failed. Previous version has been restored.`n`nError: $_\",",
            "        \"BlancoMonitor Update Error\",",
            "        [System.Windows.Forms.MessageBoxButtons]::OK,",
            "        [System.Windows.Forms.MessageBoxIcon]::Error",
            "    )",
            "",
            "    Start-Process \"" + exePath + "\"",
            "}",
            "",
            "# Self-cleanup",
            "Remove-Item \"" + sPath + "\" -Force -ErrorAction SilentlyContinue"
        );

        File.WriteAllText(scriptPath, script);

        _logger.Info("Launching updater script and shutting down...");

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = true,
            CreateNoWindow = true,
        });

        // Exit the application — the script will handle the rest
        Environment.Exit(0);
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>
/// Remote update manifest structure — hosted as a static JSON file.
/// CI/CD writes this file on each release build.
/// </summary>
public sealed class UpdateManifest
{
    public required string LatestVersion { get; init; }
    public required int LatestBuildNumber { get; init; }
    public required string DownloadUrl { get; init; }
    public required long SizeBytes { get; init; }
    public string? ReleaseNotes { get; init; }
    public required DateTime ReleasedAt { get; init; }
    public string? Sha256Hash { get; init; }
    public bool IsMandatory { get; init; }
    public string? MinimumSupportedVersion { get; init; }
}
