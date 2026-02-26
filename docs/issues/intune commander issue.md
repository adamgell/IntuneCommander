# [Feature] Devices & Users — correlated view linking managed devices to Entra user owners with full attribute set and caching

## Summary

Add a Devices & Users view that fetches all managed devices and all Entra users, caches both independently using the existing CacheService pattern, then joins them client-side on userId/id for display in a single unified, searchable DataGrid.

## Plan

### 1. Two new services in `Intune.Commander.Core/Services`

**ManagedDeviceService** — wraps `GET /beta/deviceManagement/managedDevices` with full `$select` (all available properties on `ManagedDevice`). Pages via `OdataNextLink` exactly as `CompliancePolicyService` does. No `$expand` needed — user resolution is handled separately.

**EntraUserService** — wraps `GET /beta/users` with full `$select` across all standard and extension attribute properties: core identity fields, department, jobTitle, officeLocation, usageLocation, onPremisesSamAccountName, onPremisesExtensionAttributes (contains extensionAttribute1–15), assignedLicenses, accountEnabled, etc. Pages via `OdataNextLink` with `$top=999`.

Both services follow the identical pattern to `CompliancePolicyService` — constructor takes `GraphServiceClient`, no DI registration at startup, instantiated post-auth.

**Cache integration** in the ViewModel (not inside the services themselves, matching existing app behavior):

- Cache keys: `"ManagedDevices"` → `List<ManagedDevice>`, `"EntraUsers"` → `List<User>`
- Cache TTL: default 24h (same as `DefaultTtl` in `CacheService`)
- Both collections cached independently so either can be force-refreshed without invalidating the other

**Required Graph permissions:** `DeviceManagementManagedDevices.Read.All`, `User.Read.All`

### 2. `DevicesUsersViewModel` in `Intune.Commander.Desktop/ViewModels`

- **On load:** check `_cacheService.Get<ManagedDevice>(tenantId, "ManagedDevices")` and `_cacheService.Get<User>(tenantId, "EntraUsers")` — use cached data if present, otherwise fetch and call `_cacheService.Set(...)` after
- **Join:** `devices.Join(users, d => d.UserId, u => u.Id, (d, u) => new DeviceUserEntry(d, u))` — devices with no matching user get a null-user entry (unassigned devices are still shown)
- `ObservableCollection<DeviceUserEntry>` as the DataGrid source
- **SearchText** filters client-side across: device name, user display name, UPN, department, OS, compliance state, model
- **LoadCommand** — async, sets `IsBusy`, reports item count in status bar, respects `CancellationToken`
- **ForceRefreshCommand** — calls `_cacheService.Invalidate(tenantId, "ManagedDevices")` and `_cacheService.Invalidate(tenantId, "EntraUsers")` then re-fetches
- **ExportCsvCommand** — exports current filtered `DeviceUserEntry` collection to CSV
- **Cache metadata** surfaced in UI: "Last refreshed: X ago (N devices, M users)" via `_cacheService.GetMetadata(...)`

### 3. `DeviceUserEntry` DTO in `Intune.Commander.Core/Models`

Flat record combining properties from `ManagedDevice` and `User` — no Graph SDK types exposed directly to the view. Keeps the DataGrid columns decoupled from SDK model changes.

### 4. `DevicesUsersView.axaml` in `Intune.Commander.Desktop/Views`

- Cache status bar at top: last refreshed timestamp + item counts + Refresh button
- Single TextBox search bar (filters live)
- DataGrid — sortable columns covering key device fields and key user fields side by side
- Navigation entry added to the existing sidebar

## Implementation Touch Points

| Area | Files |
|------|-------|
| Service contracts | `IManagedDeviceService.cs`, `ManagedDeviceService.cs` (new); expand `$select` in existing `UserService.ListUsersAsync` |
| DTO | `DeviceUserEntry.cs` in `Core/Models` |
| ViewModel wiring | `MainWindowViewModel.cs` (cache keys, service field, collections), `.Connection.cs` (instantiation/teardown), `.Navigation.cs` (sidebar registration + column configs), `.Loading.cs` (two-collection cache-hit/miss logic), `.Search.cs` (filter predicate) |
| View wiring | `MainWindow.axaml.cs` — `OnViewModelPropertyChanged` (both branches), `IsActiveFilteredCollection`, `BindDataGridSource` |
| Tests | `ManagedDeviceServiceTests.cs`, `DeviceUserEntryTests.cs` |
| Docs/scripts | Permission documentation, `CHANGELOG.md` |

## Acceptance Criteria

- [ ] Both devices and users are cached via `CacheService` using the established `tenantId` + `dataType` key pattern
- [ ] Cache hit on second open — no Graph calls made if cache is valid
- [ ] Force refresh invalidates both cache entries and re-fetches
- [ ] Devices with no matching user record are still displayed (user columns blank)
- [ ] Single search filters across all string columns client-side
- [ ] CSV export reflects current filtered state
- [ ] Cache metadata (last refreshed, item count) visible in the view
- [ ] No hardcoded org-specific values

## Priority

P3 (Enhancement)