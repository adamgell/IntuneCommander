# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Intune Commander is a **.NET 10 / React 19 / WPF+WebView2** Windows desktop application for managing Microsoft Intune configurations across multiple cloud environments (Commercial, GCC, GCC-High, DoD). It is a ground-up remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement) (PowerShell/WPF).

## Build & Run

```bash
# Build all projects
dotnet build

# Run unit tests (excludes integration tests)
dotnet test --filter "Category!=Integration"

# Run unit tests with coverage threshold (40% line coverage enforced)
dotnet test /p:CollectCoverage=true /p:Threshold=40 /p:ThresholdType=line /p:ThresholdStat=total

# Run integration tests (requires AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET env vars)
dotnet test --filter "Category=Integration"

# Run a single test class
dotnet test --filter "FullyQualifiedName~ProfileServiceTests"

# Run the desktop application (WPF + WebView2 host)
dotnet run --project src/Intune.Commander.DesktopReact

# Run the React frontend dev server (for UI development)
cd intune-commander-react && npm run dev
```

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10, C# 12 |
| Desktop Host | WPF + WebView2 (Windows-only) |
| Frontend | React 19, TypeScript, Vite |
| State Management | Zustand |
| .NET ↔ React Bridge | `ic/1` protocol via `window.chrome.webview.postMessage` |
| Authentication | Azure.Identity 1.17.x |
| Graph API | **Microsoft.Graph.Beta** 5.130.x-preview |
| Cache | LiteDB 5.0.x (encrypted via DataProtection) |
| Profile storage | `Microsoft.AspNetCore.DataProtection` |
| DI | `Microsoft.Extensions.DependencyInjection` 10.0.x |
| Testing | xUnit, NSubstitute 5.3.x |

**Important:** The project uses `Microsoft.Graph.Beta` (not the stable `Microsoft.Graph` package). All models and the `GraphServiceClient` come from `Microsoft.Graph.Beta.*`.

## Architecture

### Project layout

```
src/
  Intune.Commander.Core/        # Business logic (.NET 10 class library)
    Auth/                        # IAuthenticationProvider, InteractiveBrowserAuthProvider, IntuneGraphClientFactory
    Models/                      # Enums (CloudEnvironment, AuthMethod), TenantProfile, ProfileStore,
                                 #   CloudEndpoints, MigrationEntry/Table, export DTOs, CacheEntry, GroupAssignmentResult
    Services/                    # 30+ Graph API services + ProfileService, CacheService, ExportService, ImportService
    Extensions/                  # ServiceCollectionExtensions (AddIntuneCommanderCore)
  Intune.Commander.DesktopReact/ # WPF + WebView2 host
    Services/                    # Bridge services (IBridgeService implementations), BridgeRouter
    MainWindow.xaml              # WPF window hosting WebView2
  Intune.Commander.Installer/    # Master Packager Dev package (MSI + MSIX)
intune-commander-react/          # React 19 + TypeScript frontend (Vite)
  src/
    components/                  # UI components organized by feature (login/, shell/, workspace/)
    store/                       # Zustand stores — one per domain
    bridge/                      # Typed bridge client for .NET interop
tests/
  Intune.Commander.Core.Tests/   # xUnit tests mirroring src structure
```

### DI and service lifetimes

`App.xaml.cs` calls `services.AddIntuneCommanderCore()` and registers bridge services.

`AddIntuneCommanderCore()` registers:

- **Singleton:** `IAuthenticationProvider`, `IntuneGraphClientFactory`, `ProfileService`, `IProfileEncryptionService`, `ICacheService`
- **Transient:** `IExportService`

**Graph API services are NOT registered in DI.** After authentication, the WPF host creates them using `new XxxService(graphClient)` and passes them to bridge services. Bridge services implement `IBridgeService` and handle messages from the React frontend via the `BridgeRouter`.

### Authentication and multi-cloud

`IntuneGraphClientFactory.CreateClientAsync(profile)` creates a `GraphServiceClient` using `Azure.Identity` credentials and the correct endpoint from `CloudEndpoints.GetEndpoints(profile.Cloud)`.

Cloud endpoints in `CloudEndpoints.cs`:

- Commercial & GCC → `https://graph.microsoft.com`
- GCC-High → `https://graph.microsoft.us`
- DoD → `https://dod-graph.microsoft.us`

### Navigation and data flow

The React frontend manages navigation via its shell component and Zustand stores. Each workspace (e.g., Settings Catalog, Detection & Remediation) has its own Zustand store that communicates with the .NET backend through the typed bridge client. The bridge client sends messages via `window.chrome.webview.postMessage` using the `ic/1` protocol, and the WPF host's `BridgeRouter` dispatches them to the appropriate `IBridgeService` implementation.

Currently 3 workspaces are built in the desktop UI: Overview Dashboard, Settings Catalog, and Detection & Remediation. The Core library has 30+ services ready to be wired into additional workspaces.

### Caching

`CacheService` uses LiteDB with an AES-encrypted database file at `%LocalAppData%\Intune.Commander\cache.db`. The DB password is generated once and stored encrypted via `Microsoft.AspNetCore.DataProtection` in `cache-key.bin`. Cache entries have a 24-hour default TTL and are keyed by tenant ID + data-type string.

### Profile storage

`ProfileService` persists `ProfileStore` (list of `TenantProfile`) to `%LocalAppData%\Intune.Commander\profiles.json`. When `IProfileEncryptionService` is injected (always the case in production), the file is prefixed with `INTUNEMANAGER_ENC:` and the payload is DataProtection-encrypted. Plaintext files are migrated to encrypted on next save. On first launch after upgrade from a pre-rename build, `ProfileService.LoadAsync` detects and auto-migrates data from the legacy `%LocalAppData%\IntuneManager\profiles.json` path.

> **Legacy compatibility constants** (do not change without careful consideration): `INTUNEMANAGER_ENC:` marker, `IntuneManager.Profiles.v1` DataProtection purpose (fallback decryptor), `SetApplicationName("IntuneManager")` in `ServiceCollectionExtensions` (changing this makes all existing encrypted data unreadable), and MSI `UpgradeCode` GUID `29E042C7-F159-466C-9F23-D2695288319A` in `src/Intune.Commander.Installer/package.json` (`msi.upgradeCode`) (changing this breaks upgrade detection for all installed copies).

### DebugLogService

`DebugLogService.Instance` is a singleton with an `ObservableCollection<string> Entries` (capped at 2000). All logging dispatches to the UI thread. Use `DebugLog.Log(category, message)` / `DebugLog.LogError(...)` throughout the WPF host code.

### Export/Import format

Each object type exports to its own subfolder under the chosen output directory (e.g., `DeviceConfigurations/`, `CompliancePolicies/`, etc.). Files are named `{DisplayName}.json` containing the serialized Graph Beta model. A `migration-table.json` at the root maps original IDs to new IDs after import.

## Git Workflow

- **Never commit directly to `main`.** All changes must go through a feature branch and pull request.
- Branch naming: `feature/`, `fix/`, `docs/` prefixes (e.g. `feature/wave7-scripts`, `fix/lazy-load-guard`).
- PRs should be created with `gh pr create` and submitted for Copilot / human review before merging.

## Coding Conventions

- **C# 12:** primary constructors, collection expressions (`[]`), required members, file-scoped namespaces
- **Nullable reference types enabled** everywhere
- **Private fields:** `_camelCase`; public: `PascalCase`
- **Namespaces:** `Intune.Commander.Core.*`, `Intune.Commander.DesktopReact.*`
- **React frontend:** TypeScript strict mode, Zustand for state, bridge client for .NET interop
- **Graph client factory class name:** `IntuneGraphClientFactory` (not `GraphClientFactory`) to avoid collision with `Microsoft.Graph.GraphClientFactory`

## Key Architecture Decisions

- **Azure.Identity over raw MSAL** — `TokenCredential` abstraction, one code path for all clouds
- **Microsoft.Graph.Beta SDK models directly** — no custom model layer; custom DTOs (`*Export` models) only where the Graph model needs augmenting for export
- **Separate app registration per cloud** — GCC-High/DoD require isolated app registrations
- **Graph services created post-auth, not in DI** — services that require a `GraphServiceClient` are instantiated in the WPF host after the user authenticates, not at startup
- **LiteDB cache keyed by tenant ID** — multiple tenant profiles can share the same cache database; TTL is 24 hours
- **Hobby project** — keep solutions pragmatic; avoid over-engineering

## CI Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| CI — Test & Coverage | `.github/workflows/ci-test.yml` | All pushes + PRs to main | Unit tests with 40% line coverage threshold (coverlet.msbuild) |
| CI — Integration Tests | `.github/workflows/ci-integration.yml` | Push/PR to main + manual | Graph API integration tests against live tenant |
| Build Release Artifacts | `.github/workflows/build-release.yml` | Push, PR, and manual dispatch | Builds unsigned desktop/CLI artifacts and a test MSI |

- Unit test CI uses `--filter "Category!=Integration"` to skip integration tests
- Integration CI requires repository secrets: `AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`
- Coverage threshold is enforced via `/p:Threshold=40` — failing builds if line coverage drops below 40%

## Testing Conventions

**Unit tests are required for all new or changed code.** Every new service, model, or behavioral change in `Intune.Commander.Core` must include corresponding tests. PRs without adequate test coverage will not be merged.

### Unit tests (`tests/Intune.Commander.Core.Tests/`)
- xUnit with `[Fact]`/`[Theory]`, NSubstitute 5.x for mocking (`Substitute.For<IMyInterface>()`)
- Service contract tests verify interface conformance, method signatures, return types, and `CancellationToken` parameters via reflection
- File I/O tests use temp directories with `IDisposable` cleanup
- **NSubstitute patterns**:
  - Return values: `svc.MethodAsync(Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result))`
  - Argument capture: `svc.MethodAsync(Arg.Do<T>(x => captured = x), Arg.Any<CancellationToken>()).Returns(...)`
  - Call verification: `await svc.Received(1).MethodAsync(expectedArg, Arg.Any<CancellationToken>())`
  - No-call assertion: `svc.DidNotReceive().Method(Arg.Any<string>())`
- **`GraphServiceClient` is NOT mockable** (sealed SDK) — services that directly call Graph keep their reflection-based contract tests; NSubstitute is used only for project-owned interfaces (`IXxxService`, `ICacheService`, etc.)

### Integration tests (`tests/Intune.Commander.Core.Tests/Integration/`)

- Tagged with `[Trait("Category", "Integration")]` — **always** use this trait for any test hitting Graph API
- Base class `GraphIntegrationTestBase` provides `GraphServiceClient` from env vars and `ShouldSkip()` for graceful no-op when credentials are missing
- Read-only tests (List + Get) are safe for any tenant
- CRUD tests use `IntTest_AutoCleanup_` prefix and clean up in `finally` blocks
- Setup script: `scripts/Setup-IntegrationTestApp.ps1` creates the app registration with all required Graph permissions (see `docs/GRAPH-PERMISSIONS.md`)

## PowerShell Scripts

- Scripts must use **ASCII-only characters** — no Unicode decorations (e.g., `━─→✓✗○—`) as they break PowerShell 5.1 parsing
- Save `.ps1` files with ASCII encoding
- Target PowerShell 5.1+ compatibility
