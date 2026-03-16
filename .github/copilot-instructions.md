# Copilot Instructions

## Project Overview
Intune Commander is a .NET 10 / React 19 / WPF+WebView2 Windows desktop app **and CLI** for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD clouds. It's a ground-up remake of a PowerShell/WPF tool — the migration to compiled .NET specifically targets UI deadlocks and threading issues.

## External Documentation
- Use Context7 first for external frameworks, libraries, SDKs, GitHub Actions, and APIs whenever current behavior matters.
- Prefer Context7 for .NET, React, Zustand, Microsoft.Graph.Beta, Azure.Identity, LiteDB, PowerShell modules, and GitHub Actions before relying on memory.
- Skip Context7 only for purely repository-local code or when no relevant library entry exists.

## Critical: Async-First Rule
- **No `.GetAwaiter().GetResult()`, `.Wait()`, or `.Result` calls — ever.** Always `await`.
- All async methods must accept `CancellationToken`.

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

**Important:** Uses `Microsoft.Graph.Beta` (not the stable `Microsoft.Graph`). All models and `GraphServiceClient` come from `Microsoft.Graph.Beta.*`.

## Architecture

### Projects
- `Intune.Commander.Core` — class library: auth, 30+ Graph services, models, export/import
- `Intune.Commander.DesktopReact` — WPF + WebView2 host; bridge services and `BridgeRouter`
- `Intune.Commander.CLI` — `System.CommandLine`-based CLI: `export`, `import`, `list`, `profile`, `diff`, `alert`, `completion`
- `Intune.Commander.Installer` — Master Packager Dev package (`package.json`) producing MSI + MSIX
- `intune-commander-react/` — React 19 + TypeScript frontend (Vite)

### DI and service lifetimes
`App.xaml.cs` calls `services.AddIntuneCommanderCore()` which registers:
- **Singleton:** `IAuthenticationProvider`, `IntuneGraphClientFactory`, `ProfileService`, `IProfileEncryptionService`, `ICacheService`
- **Transient:** `IExportService`

**Graph API services are NOT registered in DI.** After authentication, the WPF host (and CLI) creates them using `new XxxService(graphClient)`. Bridge services implement `IBridgeService` and are dispatched via `BridgeRouter`.

### Bridge pattern (Desktop UI)
React communicates with .NET via the `ic/1` protocol:
- **Production**: `window.chrome.webview.postMessage(msg)` → `BridgeRouter` → `IBridgeService`
- **Dev mode**: `bridgeClient.ts` falls back to a WebSocket at `ws://localhost:5100/ws/` when WebView2 is unavailable
- Messages: `{ protocol: 'ic/1', id, command, payload }` — responses keyed by `id`, push events by `event` name
- Auth commands use a 120s timeout; all others 10s

### State management
Zustand stores in `intune-commander-react/src/store/` — one store per domain (e.g., `settingsCatalogStore.ts`, `detectionRemediationStore.ts`). Each workspace has its own store.

### Caching
`CacheService` uses LiteDB at `%LocalAppData%\Intune.Commander\cache.db` (AES-encrypted). Cache key = tenant ID + data-type string. Default TTL: 24 hours.

### Profile storage
`ProfileService` stores encrypted JSON at `%LocalAppData%\Intune.Commander\profiles.json`. The file is prefixed with `INTUNEMANAGER_ENC:` when encrypted via DataProtection.

### Multi-cloud
`CloudEndpoints.GetEndpoints(cloud)` returns `(graphBaseUrl, authorityHost)`:
- Commercial & GCC → `https://graph.microsoft.com`
- GCC-High → `https://graph.microsoft.us`
- DoD → `https://dod-graph.microsoft.us`

## Service-per-Type Pattern
Each Intune object type gets its own interface + implementation. All services take `GraphServiceClient` in constructor, use manual `@odata.nextLink` pagination, accept `CancellationToken`, and return `List<T>`.

## Graph API Pagination — Manual `@odata.nextLink` (REQUIRED)
**Do NOT use `PageIterator`** — it silently truncates results on some tenants. All Graph list operations must use manual `while` loop pagination:
```csharp
var response = await _graphClient.DeviceAppManagement.MobileApps
    .GetAsync(req =>
    {
        req.QueryParameters.Top = 999;
        // other query params...
    }, cancellationToken);

var result = new List<MobileApp>();
while (response != null)
{
    if (response.Value != null)
        result.AddRange(response.Value);

    if (!string.IsNullOrEmpty(response.OdataNextLink))
    {
        response = await _graphClient.DeviceAppManagement.MobileApps
            .WithUrl(response.OdataNextLink)
            .GetAsync(cancellationToken: cancellationToken);
    }
    else
    {
        break;
    }
}
```
- Always set `$top=999` on the initial request.
- Use `.WithUrl(response.OdataNextLink)` (requires `using Microsoft.Kiota.Abstractions;`).
- Apply this pattern to **every** service method that lists Graph objects, including `GroupService`.

## Key Conventions

### C# Coding Style
- **C# 12:** primary constructors, collection expressions (`[]`), required members, file-scoped namespaces
- **Nullable reference types enabled** everywhere
- **Private fields:** `_camelCase`; public members: `PascalCase`
- **Namespaces:** `Intune.Commander.Core.*`, `Intune.Commander.DesktopReact.*`, `Intune.Commander.CLI.*`
- **Graph client factory class name:** `IntuneGraphClientFactory` (not `GraphClientFactory`) to avoid collision with `Microsoft.Graph.GraphClientFactory`

### Models and exports
- **Graph SDK models used directly** — no wrapper DTOs. Types like `DeviceConfiguration`, `MobileApp` come from `Microsoft.Graph.Beta.Models`.
- **Export wrappers** for types with assignments (e.g., `CompliancePolicyExport`, `ApplicationExport`) bundle the object + its assignments list.
- **Export format**: Subfolder-per-type (`DeviceConfigurations/`, `CompliancePolicies/`, etc.) with files named `{DisplayName}.json`. A `migration-table.json` at the root maps original IDs to new IDs. Must maintain read compatibility with the original PowerShell tool's JSON format.

### Legacy compatibility constants — DO NOT CHANGE
- `INTUNEMANAGER_ENC:` — file marker prefix for encrypted profiles
- `IntuneManager.Profiles.v1` — DataProtection purpose (fallback decryptor)
- `SetApplicationName("IntuneManager")` in `ServiceCollectionExtensions` — changing this makes all existing encrypted data unreadable
- MSI `UpgradeCode` GUID `29E042C7-F159-466C-9F23-D2695288319A` in `package.json` (`msi.upgradeCode`) — changing this breaks upgrade detection for all installed copies

### DebugLogService (WPF host)
`DebugLogService.Instance` is a singleton with `ObservableCollection<string> Entries` (capped at 2000). Use `DebugLog.Log(category, message)` / `DebugLog.LogError(...)` throughout WPF host code. All logging dispatches to the UI thread.

### PowerShell scripts
- Use **ASCII-only characters** — no Unicode decorations (`━─→✓✗○—`) as they break PowerShell 5.1 parsing
- Save `.ps1` files with ASCII encoding; target PowerShell 5.1+ compatibility

## Build & Test
```bash
# Build all projects
dotnet build

# Run unit tests (excludes integration tests)
dotnet test --filter "Category!=Integration"

# Run a single test class
dotnet test --filter "FullyQualifiedName~ProfileServiceTests"

# Run unit tests with coverage threshold (40% line coverage enforced)
dotnet test /p:CollectCoverage=true /p:Threshold=40 /p:ThresholdType=line /p:ThresholdStat=total

# Run integration tests (requires AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET env vars)
dotnet test --filter "Category=Integration"

# Launch the desktop app
dotnet run --project src/Intune.Commander.DesktopReact

# Run the React frontend dev server
cd intune-commander-react && npm run dev
```

## Testing Conventions
- xUnit with `[Fact]`/`[Theory]`, NSubstitute 5.x for mocking (`Substitute.For<IMyInterface>()`)
- File I/O tests use temp directories with `IDisposable` cleanup
- **`GraphServiceClient` is NOT mockable** (sealed SDK) — services that directly call Graph keep their reflection-based contract tests; NSubstitute is used only for project-owned interfaces
- **NSubstitute patterns:**
  - Return values: `svc.MethodAsync(Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result))`
  - Argument capture: `svc.MethodAsync(Arg.Do<T>(x => captured = x), Arg.Any<CancellationToken>()).Returns(...)`
  - Call verification: `await svc.Received(1).MethodAsync(expectedArg, Arg.Any<CancellationToken>())`
  - No-call assertion: `svc.DidNotReceive().Method(Arg.Any<string>())`
- **CLI tests** that redirect `Console.Out`/`Console.Error` use `[Collection("Console")]` with `DisableParallelization` to prevent concurrent capture conflicts
- **Integration tests** are tagged `[Trait("Category", "Integration")]`. Base class `GraphIntegrationTestBase` provides `GraphServiceClient` from env vars. CRUD tests use `IntTest_AutoCleanup_` prefix and clean up in `finally` blocks.

**Unit tests are required for all new or changed code.** Every new service, model, or behavioral change must include corresponding tests. 40% line coverage threshold is enforced in CI.

## Git Workflow
- **Never commit directly to `main`.** All changes must go through a feature branch and pull request.
- Branch naming: `feature/`, `fix/`, `docs/` prefixes (e.g. `feature/wave7-scripts`, `fix/lazy-load-guard`).
- PRs should be created with `gh pr create` and submitted for Copilot / human review before merging.

## Adding a New Intune Object Type
1. Create `I{Type}Service` interface in `Core/Services/` following the CRUD + `GetAssignmentsAsync` pattern.
2. Create `{Type}Service` implementation taking `GraphServiceClient`, using manual `@odata.nextLink` pagination for listing.
3. If assignments are needed, create `{Type}Export` model in `Core/Models/` bundling object + assignments.
4. Add export/import methods to `ExportService`/`ImportService`.
5. Add tests in `tests/Intune.Commander.Core.Tests/`.

## Adding a New Desktop UI Workspace
1. **React side**: Create a component in `intune-commander-react/src/components/workspace/`, a Zustand store in `src/store/`, and TypeScript types.
2. **Bridge side**: Add a bridge service in `src/Intune.Commander.DesktopReact/Services/` implementing `IBridgeService`.
3. **Register**: Wire the bridge service into `BridgeRouter` and add navigation in the React shell.
See existing workspaces (Settings Catalog, Detection & Remediation) for the full pattern.
