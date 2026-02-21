# Copilot Instructions

## Project Overview
Intune Commander is a .NET 10 / Avalonia UI desktop app for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD clouds. It's a ground-up remake of a PowerShell/WPF tool — the migration to compiled .NET specifically targets UI deadlocks and threading issues.

## Build & Test
```bash
dotnet build                                                    # Build all projects
dotnet test --filter "Category!=Integration"                    # Unit tests only (no credentials needed)
dotnet test --filter "FullyQualifiedName~ProfileServiceTests"   # Single test class
dotnet test --filter "Category=Integration"                     # Integration tests (needs AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET)
dotnet test /p:CollectCoverage=true /p:Threshold=40 /p:ThresholdType=line /p:ThresholdStat=total  # With 40% coverage gate
dotnet run --project src/IntuneManager.Desktop                  # Launch the app
```

## Technology
- Runtime: .NET 10, C# 12
- UI: Avalonia 11.3.x (`.axaml` files), CommunityToolkit.Mvvm 8.2.x
- Auth: Azure.Identity 1.17.x
- **Graph API: `Microsoft.Graph.Beta` 5.x-preview** — NOT the stable `Microsoft.Graph` package. All models and `GraphServiceClient` come from `Microsoft.Graph.Beta.*`.
- Cache: LiteDB 5.0.x (AES-encrypted), Charts: LiveChartsCore.SkiaSharpView.Avalonia

## Architecture

**Two projects:** `IntuneManager.Core` (class library) and `IntuneManager.Desktop` (Avalonia app).

**DI setup:** `App.axaml.cs` calls `services.AddIntuneManagerCore()` then registers `MainWindowViewModel` as transient.
- Singletons: `IAuthenticationProvider`, `IntuneGraphClientFactory`, `ProfileService`, `IProfileEncryptionService`, `ICacheService`
- Transient: `IExportService`
- **Graph API services are NOT in DI.** After login, `MainWindowViewModel` creates them with `new XxxService(graphClient)` — all `I*Service` fields in the VM are nullable until `ConnectAsync` runs.

**MVVM:** ViewModels are `partial class` extending `ViewModelBase` (provides `IsBusy`, `ErrorMessage`, `DebugLog`). Use `[ObservableProperty]` and `[RelayCommand]`. Avalonia's ViewLocator resolves `FooViewModel` → `FooView` by naming convention.

**`MainWindowViewModel` is split into partial classes** by concern: `.Connection.cs`, `.Loading.cs`, `.Navigation.cs`, `.Selection.cs`, `.Detail.cs`, `.ExportImport.cs`, `.Search.cs`, `.AppAssignments.cs`, `.ConditionalAccessExport.cs`. The VM holds 30+ `ObservableCollection<T>` properties (one per Intune type). Navigation is driven by `NavCategories`; each category loads lazily via `_*Loaded` boolean flags and `ICacheService`.

**Auth/multi-cloud:** `IntuneGraphClientFactory.CreateClientAsync(profile)` builds a `GraphServiceClient` using `Azure.Identity` with the endpoint from `CloudEndpoints.GetEndpoints(cloud)`. GCC-High → `https://graph.microsoft.us`; DoD → `https://dod-graph.microsoft.us`. GCC-High/DoD require separate app registrations.

**Caching:** `CacheService` uses LiteDB at `%LocalAppData%\IntuneManager\cache.db` (AES password in `cache-key.bin` via DataProtection). Entries keyed by tenant ID + type string, 24-hour TTL.

**Profile storage:** `ProfileService` writes `%LocalAppData%\IntuneManager\profiles.json`. Encrypted files are prefixed with `INTUNEMANAGER_ENC:`.

**DebugLogService:** Singleton (`DebugLogService.Instance`), `ObservableCollection<string>` capped at 2000 entries. Access via `ViewModelBase.DebugLog` (protected property). Use `DebugLog.Log(category, message)` / `DebugLog.LogError(...)`. All updates dispatch to UI thread. Observable property updates from background threads must use `Dispatcher.UIThread.Post()`.

## Critical: Async-First UI Rule
- **No `.GetAwaiter().GetResult()`, `.Wait()`, or `.Result` on the UI thread — ever.**
- Fire-and-forget for non-blocking loads: `_ = LoadProfilesAsync();`
- All `[RelayCommand]` methods returning `Task` get automatic `CancellationToken` support from CommunityToolkit.Mvvm.

## Graph API Pagination — Manual `@odata.nextLink` (REQUIRED)
**Do NOT use `PageIterator`** — it silently truncates results on some tenants. Use manual `while` loop on `OdataNextLink`:
```csharp
var response = await _graphClient.DeviceManagement.DeviceConfigurations
    .GetAsync(req => { req.QueryParameters.Top = 999; }, cancellationToken);

var result = new List<DeviceConfiguration>();
while (response != null)
{
    if (response.Value != null) result.AddRange(response.Value);
    if (!string.IsNullOrEmpty(response.OdataNextLink))
        response = await _graphClient.DeviceManagement.DeviceConfigurations
            .WithUrl(response.OdataNextLink)
            .GetAsync(cancellationToken: cancellationToken);
    else break;
}
```
- Always set `$top=999` on the initial request.
- `.WithUrl(...)` requires `using Microsoft.Kiota.Abstractions;`.
- Apply to **every** service method that lists Graph objects, including `GroupService`.

## Service-per-Type Pattern
Each Intune type gets `I{Type}Service` + `{Type}Service` in `Core/Services/`. All take `GraphServiceClient` in constructor, accept `CancellationToken`, return `List<T>`. Currently ~25 types including: ConfigurationProfile, CompliancePolicy, Application, AppProtectionPolicy, ConditionalAccessPolicy, EndpointSecurity, EnrollmentConfiguration, FeatureUpdateProfile, DeviceHealthScript, AdministrativeTemplate, SettingsCatalog, AutopilotDevice, ScopeTag, AssignmentFilter, GroupService, TermsAndConditions, RoleDefinition, NamedLocation, and more.

## Coding Conventions
- **C# 12:** primary constructors, collection expressions (`[]`), required members, file-scoped namespaces
- **Nullable reference types enabled** everywhere; no `#nullable disable`
- **Private fields:** `_camelCase`; public: `PascalCase`
- **Namespaces:** `IntuneManager.Core.*` and `IntuneManager.Desktop.*`
- **XAML:** always set `x:DataType` — `AvaloniaUseCompiledBindingsByDefault` is on
- **Factory class name:** `IntuneGraphClientFactory` (not `GraphClientFactory`) to avoid collision with `Microsoft.Graph.GraphClientFactory`
- **Graph SDK models used directly** — no wrapper DTOs except `*Export` classes (e.g., `CompliancePolicyExport`) that bundle an object + its assignments list for export
- **Computed columns:** `DataGridColumnConfig` uses `"Computed:"` prefix in `BindingPath` for values derived in code-behind (e.g., platform from OData type)

## Testing Conventions

### Unit tests (`tests/IntuneManager.Core.Tests/`)
- xUnit `[Fact]`/`[Theory]`, no mocking framework
- Service contract tests verify interface conformance via reflection (method signatures, return types, `CancellationToken` params)
- File I/O tests use temp directories with `IDisposable` cleanup
- Test factory methods use `Make*` naming (e.g., `MakeProfile`); test doubles use `Spy*` prefix

### Integration tests (`tests/IntuneManager.Core.Tests/Integration/`)
- **Always** tag with `[Trait("Category", "Integration")]`
- Base class `GraphIntegrationTestBase` provides `GraphServiceClient` from env vars and `ShouldSkip()` for graceful no-op
- CRUD tests prefix created objects with `IntTest_AutoCleanup_` and clean up in `finally` blocks
- Use `RetryOnTransientFailureAsync()` helper (3 attempts, exponential backoff) to handle Graph API transient 500 errors

## Export/Import Format
Each type exports to its own subfolder (e.g., `DeviceConfigurations/`, `CompliancePolicies/`). Files named `{DisplayName}.json` with serialized Graph Beta model. `migration-table.json` at root maps original IDs to new IDs. Must maintain read compatibility with the original PowerShell tool's JSON format.

## PowerShell Scripts
- **ASCII-only characters** — no Unicode decorations (`━─→✓✗○—`) — they break PowerShell 5.1 parsing
- Save `.ps1` files with ASCII encoding; target PowerShell 5.1+ compatibility

## Adding a New Intune Object Type
1. Create `I{Type}Service` + `{Type}Service` in `Core/Services/` (manual pagination, `CancellationToken`, `List<T>` return)
2. If assignments needed, create `{Type}Export` in `Core/Models/`
3. Add export/import to `ExportService`/`ImportService`
4. Wire into `MainWindowViewModel`: collection, selection property, column configs, nav category, lazy-load logic with `_*Loaded` flag
5. Add tests in `tests/IntuneManager.Core.Tests/`