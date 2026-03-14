# Intune Commander - .NET Remake

![Intune Logo](docs/images/logo_small.png)

Intune Commander is a desktop application for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD cloud environments. It reimagines the PowerShell-based IntuneManagement tool as a compiled, async-first application that eliminates the UI freezes, threading deadlocks, and data refresh issues common in PowerShell WPF tools.

It supports multi-cloud and multi-tenant profiles with encrypted local storage, manages over 30 Intune object types (device configurations, compliance policies, conditional access policies, applications, and more), and provides bulk export/import in a JSON format compatible with the original PowerShell tool. Additional features include Conditional Access PowerPoint export, global search across all cached object types, debug logging, and raw JSON inspection.

### UI Frontends

Intune Commander ships with two UI frontends that share the same .NET Core backend:

| Frontend | Host | Status | Description |
|----------|------|--------|-------------|
| **React** (new) | WPF + WebView2 | Active development | Modern React 19 / TypeScript UI with Zustand state management, communicating with .NET services via a typed async bridge protocol |
| **Avalonia** (legacy) | Avalonia 11.3.x | Maintained | Original cross-platform desktop UI with CommunityToolkit.Mvvm, LiveCharts dashboards, and full feature coverage |

The React frontend is the primary focus going forward. It currently supports the Settings Catalog workspace, global search, profile management, and auto-reconnect, with additional workspaces being ported incrementally.

> **Platform Notes**
>
> - **Windows** is the recommended and fully supported platform.
> - The React frontend requires Windows (WPF + WebView2).
> - **macOS** support is coming soon but requires special care — code signing, notarization, and platform-specific auth flows (Device Code instead of interactive browser) all need dedicated attention.
> - The Avalonia frontend supports macOS (with Device Code auth) and Linux (headless/Core scenarios planned).

## Project Overview

### Goals

- **Multi-cloud support:** Commercial, GCC, GCC-High, DoD tenants
- **Multi-tenant:** Easy switching between tenant environments with profile management
- **Native performance:** Compiled .NET code eliminates PowerShell threading issues
- **Modern UI:** React 19 + TypeScript frontend with WPF/WebView2 host (Avalonia legacy frontend also available)
- **Backward compatible:** Import/export compatible with PowerShell version JSON format

## Technology Stack

### Core / Backend

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10, C# 12 |
| Authentication | Azure.Identity 1.17.x |
| Graph API | **Microsoft.Graph.Beta** 5.130.x-preview |
| Cache | LiteDB 5.0.x (AES-encrypted via DataProtection) |
| PowerPoint Export | Syncfusion.Presentation.Net.Core 28.1.x |
| DI | Microsoft.Extensions.DependencyInjection 10.0.x |
| Testing | xUnit |

### React Frontend (primary)

| Component | Technology |
|-----------|-----------|
| UI Framework | React 19, TypeScript 5.7 |
| Build Tool | Vite 6.3 |
| State Management | Zustand 5.0 |
| Desktop Host | WPF (.NET 10) + Microsoft.Web.WebView2 |
| IPC | Custom `ic/1` async bridge protocol (JSON-RPC style) |

### Avalonia Frontend (legacy)

| Component | Technology |
|-----------|-----------|
| UI Framework | Avalonia 11.3.x (`.axaml` files, FluentTheme) |
| MVVM | CommunityToolkit.Mvvm 8.2.x |
| Charts | LiveChartsCore.SkiaSharpView.Avalonia |

> **Note:** This project uses `Microsoft.Graph.Beta`, **not** the stable `Microsoft.Graph` package. All models and `GraphServiceClient` come from `Microsoft.Graph.Beta.*`.

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 18+ and npm (for the React frontend)
- Visual Studio 2022, JetBrains Rider, or VS Code with C# Dev Kit
- An Azure AD app registration with appropriate Microsoft Graph permissions (for use with the beta Microsoft Graph SDK/endpoint)
- (Optional) Syncfusion license key for PowerPoint export feature - see [Syncfusion Licensing](#syncfusion-licensing)

### Build & Run

```bash
# Build all .NET projects
dotnet build

# Run unit tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~ProfileServiceTests"

# Run the React frontend (WPF + WebView2 host)
cd intune-commander-react && npm install && npm run dev   # Start Vite dev server
dotnet run --project src/Intune.Commander.DesktopReact     # Launch WPF host (loads from localhost:5173)

# Run the Avalonia frontend (legacy)
dotnet run --project src/Intune.Commander.Desktop
```

### Profile Management

Intune Commander stores connection details as **profiles** (tenant ID, client ID, cloud, auth method). Profiles are persisted locally in an encrypted file and never leave your machine.

**Manually adding a profile:**

1. Launch the app — you'll land on the login screen
2. Fill in Tenant ID, Client ID, Cloud, and (optionally) Client Secret
3. Click **Save Profile** to persist it for future sessions

**Importing profiles from a JSON file:**

1. Click **Import Profiles** on the login screen
2. Select a `.json` file containing one or more profile definitions
3. Profiles are merged in — duplicates (same Tenant ID + Client ID) are skipped automatically
4. The imported profiles appear immediately in the **Saved Profiles** dropdown

A ready-to-use template is available at [`.github/profile-template.json`](.github/profile-template.json). Download it, fill in your real Tenant IDs and Client IDs, and import it directly.

**Supported JSON shapes:**

```json
// Array of profiles (recommended)
[
  {
    "name": "Contoso-Prod",
    "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "cloud": "Commercial",
    "authMethod": "Interactive"
  }
]
```

Valid `cloud` values: `Commercial`, `GCC`, `GCCHigh`, `DoD`
Valid `authMethod` values: `Interactive` (browser popup), `ClientSecret` (include `"clientSecret"` field)

### App Registration Setup

1. Go to **Azure Portal → Entra ID → App Registrations → New registration**
2. Name your app (e.g. `IntuneCommander-Dev`)
3. Set **Redirect URI** to `http://localhost:45132` (Mobile and desktop applications)
4. Under **API permissions**, add `Microsoft Graph → Delegated → DeviceManagementConfiguration.ReadWrite.All` and related Intune scopes
5. Grant admin consent for the tenant

For **Government clouds** (GCC-High, DoD), register separate apps in the respective Azure portals (`portal.azure.us`, `portal.apps.mil`).

### Authentication Methods

| Method | Description |
|--------|-------------|
| **Interactive** (default) | Browser popup with persistent token cache |
| **Device Code** | Code-based flow for environments without browser access |
| **Client Secret** | Unattended service principal authentication |

## Architecture Summary

```
src/
  Intune.Commander.Core/           # Business logic (.NET 10 class library)
    Auth/                          # Azure.Identity credential providers
    Models/                        # Enums, TenantProfile, ProfileStore, DTOs, CacheEntry
    Services/                      # 30+ Graph API services + ProfileService, CacheService, ExportService
    Extensions/                    # DI registration (AddIntuneCommanderCore)
  Intune.Commander.DesktopReact/   # WPF + WebView2 host for React frontend
    Bridge/                        # BridgeRouter, BridgeMessage protocol, IBridgeService
    Services/                      # AuthBridge, ProfileBridge, SettingsCatalogBridge, SearchBridge
    Models/                        # DTOs for bridge responses
  Intune.Commander.Desktop/        # Avalonia UI application (legacy)
    Views/                         # MainWindow, LoginView, OverviewView, DebugLogWindow, RawJsonWindow
    ViewModels/                    # MainWindowViewModel, LoginViewModel, OverviewViewModel
    Services/                      # DebugLogService (in-memory log, UI-thread-safe)
intune-commander-react/            # React 19 + TypeScript frontend
  src/
    bridge/                        # WebView2 interop (typed async bridge client)
    components/                    # login/, shell/, workspace/ components
    store/                         # Zustand stores (appStore, searchStore, settingsCatalogStore)
    types/                         # TypeScript models and interfaces
    styles/                        # CSS design tokens and component styles
tests/
  Intune.Commander.Core.Tests/     # xUnit tests (200+ cases)
```

### Bridge Architecture (React frontend)

The React frontend communicates with .NET Core services through a typed async bridge over WebView2's `postMessage` channel. The `ic/1` protocol supports commands (request/response) and events (push notifications). The WPF host is intentionally thin — it owns only the window lifecycle, WebView2 hosting, and file dialogs, while React owns all UI rendering and state management.

Graph API services are created **after** authentication (`new XxxService(graphClient)`) — they are not registered in DI at startup.

See [CLAUDE.md](CLAUDE.md) for full architectural decisions.

## Supported Intune Object Types

Device Configurations · Compliance Policies · Settings Catalog · Endpoint Security ·
Administrative Templates · Enrollment Configurations · App Protection Policies ·
Managed Device App Configurations · Targeted Managed App Configurations ·
Terms and Conditions · Scope Tags · Role Definitions · Intune Branding · Azure Branding ·
Autopilot Profiles · Device Health Scripts · Mac Custom Attributes · Feature Updates ·
Named Locations · Authentication Strengths · Authentication Contexts · Terms of Use ·
Conditional Access · Assignment Filters · Policy Sets · Applications ·
Application Assignments · Dynamic Groups · Assigned Groups

## Features

### Global Search

Search across all 24+ cached Intune object types instantly from the top bar. Results are grouped by category with direct navigation to the matching item's workspace. Search runs against locally cached data for instant results with no additional Graph API calls.

### Settings Catalog Workspace

Master-detail view for Settings Catalog policies with:
- Policy list showing platform, profile type, scope tags, assignment status, and setting count
- Detail panel with full policy metadata, resolved group assignments, and settings grouped by category
- Human-readable setting names and values (MSFT prefixes stripped, enums resolved)

### Auto-Reconnect

On startup, the app automatically attempts to reconnect using the last active profile. If authentication succeeds silently, you go straight to the connected shell. If it fails, you land on the login screen without an error.

### Offline CLI Validation Workflow

For CLI/Core hardening, the recommended workflow is:

1. Run a one-time authenticated export with normalized JSON:
   `ic export --profile <name> --output <folder> --normalize`
2. Re-run file-only validation as often as needed:
   `ic import --folder <folder> --dry-run`
3. Compare baseline/current snapshots without touching Graph:
   `ic diff --baseline <baseline-folder> --current <current-folder> --format markdown`

`ic import --dry-run` validates the export folder structure and JSON payloads without authenticating to Graph or creating objects in Intune, but it will not catch Graph-side constraints that only appear during a real create call. If malformed files are found, the command returns a non-zero exit code and includes a structured `validationErrors` array while continuing to scan the remaining files. For larger repeatable runs, use `scripts/stress-import-export.sh` with either `--profile <name>` for a fresh export or `--seed <folder>` to reuse an existing normalized export.

Examples:

- Fresh export + offline stress run:
  `scripts/stress-import-export.sh --profile <name> --workspace artifacts/stress-run`
- Reuse an existing normalized export:
  `scripts/stress-import-export.sh --seed artifacts/tenant-export --scale 5 --mutate-count 10`

The stress harness writes dry-run JSON, diff reports, and a lightweight benchmark summary into the selected workspace so you can track CLI/Core behavior over time.

### Conditional Access PowerPoint Export

Export Conditional Access policies to a comprehensive PowerPoint presentation with:

- Cover slide with tenant name and export timestamp
- Tenant summary with policy counts
- Policy inventory table showing all policies
- Detailed slides for each policy (conditions, grant controls, assignments)

**Usage:**

1. Navigate to the Conditional Access category
2. Load CA policies
3. Click "📊 Export PowerPoint" button
4. Choose save location
5. Open the generated `.pptx` file

**Current Limitations (v1):**

- Commercial cloud only (GCC/GCC-High/DoD support planned for future release)
- Basic policy details (advanced dependency lookups deferred)
- Feature-level parity with idPowerToys CA decks (not pixel-perfect template matching)

### Syncfusion Licensing

The PowerPoint export feature uses Syncfusion.Presentation.Net.Core, which requires a license key.

**End users of the official `.exe` release do not need a key** — it is baked into the binary at build time.

**Community License (FREE):**

- For companies/individuals with < $1M annual revenue
- Maximum 5 developers
- Register at: <https://www.syncfusion.com/sales/communitylicense>

**Commercial License:**

- Required for companies exceeding Community License thresholds
- Visit: <https://www.syncfusion.com/sales/products>

**Setup for local development or self-builds:**
Set environment variable: `SYNCFUSION_LICENSE_KEY=your-license-key-here`

The app will run without a license key but will display watermarks on exported PowerPoint files.

**How the released binary gets the key:**
The tag-triggered `codesign.yml` workflow reads the `SYNCFUSION_LICENSE_KEY` secret from the `codesigning` environment and passes it as `-p:SyncfusionLicenseKey=...` during `dotnet publish`. It is baked into the binary as assembly metadata before Azure Trusted Signing runs. The key never appears in source code or git history. Store it in your secret manager (e.g. 1Password) and add it as a secret in the `codesigning` GitHub Actions environment named `SYNCFUSION_LICENSE_KEY`.

## Acknowledgments

This project is a ground-up remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement), a PowerShell/WPF-based Intune management tool.
Additional thanks to Merill Fernando for originally creating [idPowerToys](https://github.com/merill/idPowerToys).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on submitting pull requests, code standards, and development workflow.
