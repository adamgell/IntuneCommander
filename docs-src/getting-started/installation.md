# Installation

Intune Commander is distributed as a **self-contained Windows x64 executable** — no .NET runtime installation required.

## Download

1. Go to the [**GitHub Releases**](https://github.com/adamgell/IntuneCommander/releases) page.
2. Under the latest release, download **`Intune.Commander.Desktop.exe`**.
3. Run the executable — no installer needed.

!!! tip "Windows SmartScreen"
    The binary on the releases page is code-signed. If Windows SmartScreen still shows a warning on first run, it is because the certificate is building reputation over time — this is normal for a new signing certificate.

    Click **More info → Run anyway** to proceed. Once the certificate has accumulated enough download and run history, the warning will stop appearing automatically.

## Build from source

If you prefer to build from source:

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022, JetBrains Rider, or VS Code with C# Dev Kit

### Steps

```bash
# Clone the repository
git clone https://github.com/adamgell/IntuneCommander.git
cd IntuneCommander

# restore all nuget packages
dotnet restore

# Build all projects
dotnet build

# Run the app
dotnet run --project src/Intune.Commander.Desktop
```

## Next steps

Once the app is running, you'll need to [register an Entra ID app](app-registration.md) before you can connect to a tenant.
