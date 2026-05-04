# ⬡ BlancoMonitor

> **Non-invasive website performance monitoring & trace tool**  
> Built with .NET 10 · WinForms · SQLite · Clean Architecture

[![Release](https://img.shields.io/github/v/release/yoxbensoftware/BlancoMonitor?style=flat-square&color=00ff88)](https://github.com/yoxbensoftware/BlancoMonitor/releases/latest)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square)](https://github.com/yoxbensoftware/BlancoMonitor/releases/latest)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

---

## 📸 Overview

BlancoMonitor is a desktop application that monitors website performance without touching the target server's configuration. It simulates real user behavior — navigation, keyword search, link traversal — captures full network traces, detects issues, and generates detailed HTML reports.

```
┌─────────────────────────────────────────────────┐
│  ⬡ BLANCO MONITOR             v.0001  [EN] [▶]  │
├──────────┬──────────────────────────────────────┤
│ Dashboard│  ● Live Monitoring                   │
│ Monitoring│  ◎ Results & Reports                │
│ History  │  ⚠ Warnings & Critical               │
│ Network  │  🔍 Discovery Tools                  │
│ Scenarios│  📋 History                          │
└──────────┴──────────────────────────────────────┘
```

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🌐 **HTTP Monitoring** | Navigate pages, follow redirects, measure TTFB & total load time |
| 🔍 **Keyword Search Simulation** | Simulate user searches, track keyword hit rates |
| 🗺 **Sitemap Discovery** | Auto-discover URLs from sitemap.xml — no manual entry needed |
| 📊 **Performance Analysis** | P95 response times, slow page detection, baseline comparisons |
| ⚠ **Rule Engine** | Configurable thresholds for warnings & critical alerts |
| 📁 **Evidence Collection** | Capture screenshots and response files per page visit |
| 🗄 **SQLite Storage** | Full run history stored locally with WAL-mode SQLite |
| 📄 **HTML Reports** | Rich HTML reports generated after every run |
| 🔄 **Auto-Update** | Checks GitHub for new releases on startup — one-click update |
| 🌍 **Multi-Language** | English, German, Turkish UI |
| 🎨 **Neon Theme** | Dark retro neon-green MDI interface |

---

## 🚀 Quick Start

### Option A — Download Release (Recommended)

1. Go to [**Releases**](https://github.com/yoxbensoftware/BlancoMonitor/releases/latest)
2. Download `BlancoMonitor_vXXXX.zip`
3. Extract anywhere — **no installer needed**
4. Run `BlancoMonitor.exe`

> ✅ Self-contained — .NET runtime **not required** on the target machine.

### Option B — Build from Source

**Requirements:** .NET 10 SDK, Windows

```powershell
git clone https://github.com/yoxbensoftware/BlancoMonitor.git
cd BlancoMonitor
dotnet build BlancoMonitor/BlancoMonitor.csproj -c Release
dotnet run --project BlancoMonitor/BlancoMonitor.csproj
```

---

## 📖 Usage

### 1. Add a URL Set
Open **URL Manager** → Add a name, base URL, and one or more target URLs.

### 2. Configure Keywords
In **URL Manager** → Keyword Sets tab → add search terms to simulate user searches.

### 3. Run Monitoring
Click **▶ Start** on the toolbar (or use the **Run Wizard** for step-by-step setup).  
Monitoring runs in the background — the **Live Monitoring** window shows real-time progress.

### 4. View Results
- **Results** → full run summary with response times, status codes, issues
- **Warnings & Critical** → filtered view of rule violations
- **Network Explorer** → per-request trace for every page visit
- **History** → compare across previous runs

### 5. Open HTML Report
Click **Open Report** in the Results form — opens in Chrome (or your default browser).

---

## 🔄 Auto-Update System

BlancoMonitor checks for updates on every startup by reading  
`update-manifest.json` hosted on GitHub raw content.

**How it works:**
1. App reads manifest → compares build numbers
2. If a newer version exists → **UpdateDialog** is shown
3. User clicks **Update Now** → ZIP is downloaded + verified (SHA256)
4. A PowerShell restart script replaces the old `.exe` on next launch

**To publish a new release** (maintainers only):
```powershell
git tag v0002
git push origin v0002
```
GitHub Actions automatically:
- Builds a self-contained `win-x64` single-file executable
- Creates a GitHub Release with the ZIP asset
- Updates `update-manifest.json` — clients will be notified on next startup

---

## 🏗 Architecture

```
BlancoMonitor/
├── Domain/              # Entities, interfaces, enums, value objects
├── Application/         # Orchestration, DTOs, service interfaces
├── Infrastructure/      # SQLite, HTTP, analysis, reporting, updater
└── UI/
    ├── Children/        # MDI child forms (12 windows)
    ├── Controls/        # NeonRichTextLog, BlancoLogo
    ├── Theme/           # NeonTheme — centralized styling
    └── Utilities/       # BrowserHelper (Chrome-first URL opener)
```

**Key design decisions:**
- **Clean Architecture** — UI has no direct DB or HTTP dependencies
- **Dependency Injection** — all services wired in `Program.cs`
- **MDI Host** — all windows are child windows of `MdiParentForm`
- **SQLite WAL mode** — concurrent reads during active monitoring runs
- **Self-contained publish** — single `.exe`, no runtime installation

---

## 🗄 Data Model (Summary)

| Entity | Purpose |
|--------|---------|
| `UrlSet` | Named group of URLs to monitor |
| `RunSession` | Container for one complete monitoring run |
| `PageVisit` | Per-URL measurement (response time, status, issues) |
| `NetworkRequest` | Individual HTTP request captured during a page visit |
| `DetectedIssue` | Rule violation linked to a specific page |
| `DailySummary` | Pre-computed daily aggregates for trend charts |
| `BaselineComparison` | Run-over-run delta per URL |

See [`BlancoMonitor/DATA_MODEL.md`](BlancoMonitor/DATA_MODEL.md) for the full ER diagram and index strategy.

---

## ⚙ Configuration

All settings are stored in `blanco_settings.json` (auto-created on first run):

```json
{
  "DefaultTimeoutSeconds": 30,
  "MaxConcurrentRequests": 5,
  "EnableScreenshots": false,
  "ThresholdWarningMs": 2000,
  "ThresholdCriticalMs": 5000
}
```

Target URLs and keyword sets are stored in `blanco_config.json`.

---

## 🧪 Tests

```powershell
dotnet test BlancoMonitor.Tests/BlancoMonitor.Tests.csproj
```

76 unit tests covering domain logic, rule engine, performance analysis, data mapping, and infrastructure services.

---

## 🛠 Tech Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10, C# 14 |
| UI | Windows Forms (WinForms) |
| Database | SQLite via `Microsoft.Data.Sqlite` |
| HTTP | `HttpClient` with custom trace capture |
| Reporting | Custom HTML generator |
| CI/CD | GitHub Actions (`release.yml`) |
| Packaging | Self-contained `win-x64` single-file publish |

---

## 📄 License

MIT — see [LICENSE](LICENSE) for details.

---

<p align="center">
  Made with ⬡ by <a href="https://github.com/yoxbensoftware">yoxbensoftware</a>
</p>
