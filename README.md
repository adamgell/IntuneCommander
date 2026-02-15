# IntuneManager - .NET Remake

A cross-platform Intune management tool built with .NET 8 and Avalonia UI, designed to overcome PowerShell WPF limitations in threading, UI refresh, and data caching.

## Project Overview

### Origin
This project is a ground-up remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement), a PowerShell/WPF-based Intune management tool.

### Goals
- **Multi-cloud support:** Commercial, GCC, GCC-High, DoD tenants
- **Multi-tenant:** Easy switching between tenant environments
- **Native performance:** Compiled .NET code eliminates PowerShell threading issues
- **Cross-platform:** Linux and macOS support
- **Backward compatible:** Import/export compatible with PowerShell version JSON format

### Non-Goals (Removed from original)
- ADMX import tooling
- Exact feature parity with PowerShell version

## Technology Stack

### Core Technologies
- **.NET 8** - Latest LTS framework
- **C#** - Primary language
- **Avalonia 11.x** - Cross-platform XAML UI framework
- **Azure.Identity** - Modern authentication (no direct MSAL dependency)
- **Microsoft.Graph 5.x** - Official Graph SDK with native multi-cloud support

### Key Libraries
- **CommunityToolkit.Mvvm** - MVVM source generators
- **System.Text.Json** - JSON serialization for export/import
- **Avalonia.Controls.DataGrid** - Data grid UI component
- **xUnit** - Unit testing framework

### Deployment Targets
- **Phase 1-5:** Windows desktop application
- **Phase 6+:** Docker containerization for headless operations

## Architecture Decisions

### Authentication
- **Interactive Browser Login:** Default authentication method for interactive sessions
- **Client Secret Authentication:** App-only authentication support for automated scenarios
- **Multi-cloud support:** Cloud environment selection (Commercial, GCC, GCC-High, DoD)
- **Profile-based:** Named configurations stored locally with encryption

### Object Model
- Use Microsoft.Graph SDK models directly where possible
- Custom DTOs only for export/import serialization
- Maintain JSON schema compatibility with PowerShell version

### Export/Import Strategy
- **Backward compatible:** Read PowerShell version JSON exports
- **Migration table:** Preserve ID mapping concept for cross-tenant imports
- **Dependency resolution:** Handle object dependencies (Policy Sets, assignments, etc.)

## Project Structure

```
IntuneManager/
├── docs/                              # Planning and architecture docs
│   ├── PLANNING.md                    # Detailed project plan
│   ├── ARCHITECTURE.md                # Technical architecture
│   ├── DECISIONS.md                   # Key decision log
│   └── NEXT-STEPS.md                  # Implementation roadmap
├── src/
│   ├── IntuneManager.Core/            # Shared business logic
│   │   ├── Auth/                      # Authentication providers
│   │   │   ├── IAuthenticationProvider.cs
│   │   │   ├── InteractiveBrowserAuthProvider.cs
│   │   │   ├── ClientSecretAuthProvider.cs
│   │   │   └── CompositeAuthenticationProvider.cs
│   │   ├── Services/                  # Graph API services
│   │   │   ├── IntuneService.cs       # Device Configuration CRUD
│   │   │   ├── ExportService.cs       # JSON export
│   │   │   ├── ImportService.cs       # JSON import with creation
│   │   │   └── ProfileService.cs      # Tenant profile management
│   │   ├── Models/                    # Data models
│   │   │   ├── CloudEnvironment.cs
│   │   │   ├── TenantProfile.cs
│   │   │   ├── MigrationTable.cs
│   │   │   └── CloudEndpoints.cs
│   │   ├── Extensions/                # Utility extensions
│   │   └── ServiceCollectionExtensions.cs
│   ├── IntuneManager.Desktop/         # Avalonia UI application
│   │   ├── Views/                     # XAML views
│   │   │   ├── LoginView.axaml        # Tenant/client ID and auth method selection
│   │   │   └── MainWindow.axaml       # Main interface with toolbar, DataGrid, detail pane
│   │   ├── ViewModels/                # MVVM view models
│   │   │   ├── ViewModelBase.cs
│   │   │   ├── LoginViewModel.cs
│   │   │   └── MainWindowViewModel.cs
│   │   └── App.axaml.cs               # Application entry point with DI
│   └── IntuneManager.Cli/             # CLI tool (Phase 6)
└── tests/
    └── IntuneManager.Core.Tests/      # Unit tests
        ├── Models/                    # Model tests
        ├── Services/                  # Service tests
        └── 30+ unit tests covering core functionality
```

## Current Status

**Stage:** Phase 1 Complete - Foundation Implementation  
**Next Steps:** Expanding object type support and UI enhancements

### Implemented Features
✅ Multi-cloud authentication (Interactive Browser + Client Secret)  
✅ Graph API integration with IntuneService  
✅ Export/Import services with JSON serialization  
✅ Profile management for tenant configurations  
✅ Basic Avalonia UI with login and main window  
✅ Dependency injection setup  
✅ Comprehensive unit test coverage (30+ tests)

### In Progress
🔄 Additional Intune object type support  
🔄 UI polish and user experience improvements

### Planned
⏳ Multi-tenant profile switching  
⏳ Advanced import features (dependency resolution, conflict handling)  
⏳ Certificate authentication  
⏳ Managed Identity support  
⏳ CLI interface  
⏳ Docker containerization

## Quick Links

- [Detailed Project Plan](docs/PLANNING.md)
- [Technical Architecture](docs/ARCHITECTURE.md)
- [Decision Log](docs/DECISIONS.md)
- [Next Steps](docs/NEXT-STEPS.md)
- [AI Assistant Guidance](CLAUDE.md)

## Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or JetBrains Rider (or VS Code with C# Dev Kit)
- Git
- Azure AD app registration with appropriate Microsoft Graph permissions

### Building the Project

```bash
# Clone the repository
git clone https://github.com/adamgell/IntuneGUI.git
cd IntuneGUI

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the desktop application
dotnet run --project src/IntuneManager.Desktop

# Run tests
dotnet test
```

### Authentication Setup

For interactive browser authentication:
1. Create an Azure AD app registration
2. Configure redirect URIs for public client flows
3. Grant required Microsoft Graph API permissions (DeviceManagementConfiguration.ReadWrite.All, etc.)
4. Note your Tenant ID and Client ID

For client secret authentication:
1. Follow the same steps as above
2. Create a client secret in your Azure AD app registration
3. Grant admin consent for application permissions

## Contributing

Contributions are welcome! Please ensure:
- All code follows the established architecture patterns
- UI operations remain async and non-blocking
- Unit tests are included for new functionality
- Backward compatibility with PowerShell JSON format is maintained

## License

TBD

## Acknowledgments

Based on [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement) - PowerShell/WPF implementation
