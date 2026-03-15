# Installation

## MSI Installer (recommended)

Intune Commander is distributed as a **signed Windows x64 MSI installer** — no .NET runtime installation required.

1. Go to the [**GitHub Releases**](https://github.com/adamgell/IntuneCommander/releases) page.
2. Download **`IntuneCommander-{version}-x64.msi`** from the latest release.
3. Run the MSI installer.

The installer will:

- Install to **`C:\Program Files\Intune Commander\`**
- Create a **Start Menu shortcut**
- Include the **CLI tool** (`ic.exe`) alongside the desktop app
- Add the install directory to your **system PATH** (so `ic` is available from any terminal)

!!! tip "Windows SmartScreen"
    The MSI and all executables are code-signed via Azure Trusted Signing. If Windows SmartScreen shows a warning on first run, it is because the certificate is building reputation — click **More info > Run anyway** to proceed.

!!! info "WebView2 Requirement"
    The desktop app requires the Microsoft Edge WebView2 Runtime. It is pre-installed on Windows 10 (April 2018+) and all Windows 11 machines. If missing, the installer will prompt you to install it from [microsoft.com/edge/webview2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).

## Standalone CLI

If you only need the command-line tool, download **`ic.exe`** directly from the [Releases](https://github.com/adamgell/IntuneCommander/releases) page. It is a self-contained single-file executable.

## Build from source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) and npm
- Visual Studio 2022, JetBrains Rider, or VS Code with C# Dev Kit

### Steps

```bash
# Clone the repository
git clone https://github.com/adamgell/IntuneCommander.git
cd IntuneCommander

# Build the React frontend
cd intune-commander-react && npm install && npm run build && cd ..

# Build all .NET projects
dotnet build

# Run the desktop app (dev mode — React hot-reloads via Vite)
cd intune-commander-react && npm run dev   # Terminal 1: start Vite dev server
dotnet run --project src/Intune.Commander.DesktopReact   # Terminal 2: launch WPF host
```

## Next steps

Once the app is running, you'll need to [register an Entra ID app](app-registration.md) before you can connect to a tenant.
