# Intune Commander

![Intune Logo](docs/images/logo_small.png)

Intune Commander is a desktop application for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD cloud environments. It is a ground-up remake of the PowerShell-based [IntuneManagement](https://github.com/Micke-K/IntuneManagement) tool, rebuilt as a compiled .NET application with a modern React frontend.

> **Early release.** The desktop UI currently covers a small number of Intune workspaces (see [What's Built](#whats-built)). The backend Core library and CLI support 30+ object types, and additional UI workspaces are being added incrementally. Contributions welcome!

## Installation

### MSI Installer (recommended)

1. Go to the [**GitHub Releases**](https://github.com/adamgell/IntuneCommander/releases) page
2. Download **`IntuneCommander-{version}-x64.msi`**
3. Run the MSI — installs to `C:\Program Files\Intune Commander\` with a Start Menu shortcut
4. The CLI tool (`ic.exe`) is included and added to your system PATH automatically

The MSI and all executables are code-signed via Azure Trusted Signing.

### MSIX Package

An **`IntuneCommander-{version}-x64.msix`** is also available on the Releases page. This is the format used for eventual Microsoft Store distribution. For direct sideload installation the signing certificate must be trusted on your machine.

### Standalone CLI

If you only need the CLI tool, download **`ic.exe`** from the same release page. It's a self-contained single-file executable — no installation required.

> **Platform note:** Windows only. The desktop app requires WebView2 Runtime (pre-installed on Windows 10 April 2018+ and all Windows 11 machines).

## What's Built

### Desktop App (React + WPF/WebView2)

- **Login & Profile Management** — multi-tenant profiles with encrypted local storage, auto-reconnect on startup
- **Overview Dashboard** — device compliance metrics at a glance
- **Settings Catalog Workspace** — master-detail view with policy list, full metadata, resolved group assignments, and human-readable settings grouped by category
- **Detection & Remediation Workspace** — device health scripts with deployment status and monitoring
- **Global Search** — instant search across all 25+ cached Intune object types from the top bar, grouped by category

### CLI (`ic.exe`)

- `ic export` — bulk export Intune configurations to JSON (compatible with the original PowerShell tool's format)
- `ic import` — import configurations with `--dry-run` for offline validation
- `ic diff` — compare two export snapshots and generate markdown reports
- `ic list` — list objects of a given type from your tenant
- `ic profile` — manage saved connection profiles
- `ic alert` — check for policy drift
- `ic completion` — shell completions for PowerShell/bash/zsh

### Backend (Core Library)

The Core library has full Graph API service coverage for 30+ Intune object types — the services are built and tested, but most are not yet wired into the desktop UI. The CLI uses them directly.

## Not Yet Built (Roadmap)

The following features have backend support in the Core library but **do not have a desktop UI workspace yet**:

- Device Configurations, Compliance Policies, Endpoint Security, Administrative Templates
- Conditional Access (including PowerPoint export)
- Applications, App Protection Policies, App Configuration Policies
- Enrollment Configurations, Autopilot Profiles
- Assignment Filters, Policy Sets, Scope Tags, Role Definitions
- Named Locations, Authentication Strengths/Contexts, Terms of Use
- Intune Branding, Azure Branding, Feature Updates
- Dynamic Groups, Assigned Groups
- Bulk export/import UI (available via CLI only)

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+ and npm
- Visual Studio 2022, JetBrains Rider, or VS Code with C# Dev Kit
- An Entra ID app registration with Microsoft Graph permissions

### Build & Run

```bash
# Build all .NET projects
dotnet build

# Run unit tests
dotnet test

# Run the desktop app (React + WPF/WebView2)
cd intune-commander-react && npm install && npm run dev   # Start Vite dev server
dotnet run --project src/Intune.Commander.DesktopReact     # Launch WPF host (loads from localhost:5173)
```

### App Registration

1. Go to **Azure Portal > Entra ID > App Registrations > New registration**
2. Set **Redirect URI** to `http://localhost:45132` (Mobile and desktop applications)
3. Add `Microsoft Graph > Delegated > DeviceManagementConfiguration.ReadWrite.All` and related Intune scopes
4. Grant admin consent

For **Government clouds** (GCC-High, DoD), register separate apps in the respective Azure portals.

### Profile Management

Intune Commander stores connection details as **profiles** (tenant ID, client ID, cloud, auth method). Profiles are encrypted locally and never leave your machine.

A ready-to-use template is available at [`.github/profile-template.json`](.github/profile-template.json).

| Auth Method | Description |
|-------------|-------------|
| **Interactive** (default) | Browser popup with persistent token cache |
| **Device Code** | Code-based flow for environments without browser access |
| **Client Secret** | Unattended service principal authentication |

Valid `cloud` values: `Commercial`, `GCC`, `GCCHigh`, `DoD`

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10, C# 12 |
| UI | React 19, TypeScript 5.7, Vite 6.3, Zustand 5.0 |
| Desktop Host | WPF + Microsoft.Web.WebView2 |
| Authentication | Azure.Identity 1.17.x |
| Graph API | Microsoft.Graph.Beta 5.130.x-preview |
| Cache | LiteDB 5.0.x (AES-encrypted) |
| Installer | Master Packager Dev (MSI + MSIX) |
| Testing | xUnit (200+ tests) |

## Architecture

```
src/
  Intune.Commander.Core/           # Business logic, 30+ Graph API services
  Intune.Commander.DesktopReact/   # WPF + WebView2 host (thin shell)
  Intune.Commander.CLI/            # Command-line interface (ic.exe)
  Intune.Commander.Installer/      # Master Packager Dev package (MSI + MSIX)
intune-commander-react/            # React 19 + TypeScript frontend
tests/
  Intune.Commander.Core.Tests/     # xUnit tests (200+ cases)
```

The React frontend communicates with .NET services through a typed async bridge (`ic/1` protocol) over WebView2's `postMessage` channel. The WPF host is intentionally thin — React owns all UI rendering and state.

See [CLAUDE.md](CLAUDE.md) for full architectural decisions.

## Acknowledgments

This project is a ground-up remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement).
Additional thanks to Merill Fernando for [idPowerToys](https://github.com/merill/idPowerToys).

Intune Commander is packaged using [Master Packager Dev \(mpdev\)](https://www.masterpackager.com/developer). It produces both MSI and MSIX installers for x64 and ARM64, with Azure Trusted Signing integrated for signed release builds.


## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on submitting pull requests, code standards, and development workflow.
