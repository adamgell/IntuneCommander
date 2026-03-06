# Plan: Fix DeviceConfiguration detail pane with $select optimization

## Problem

PR #150 adds `$select` to `ListDeviceConfigurationsAsync` with metadata-only fields:
`id`, `displayName`, `description`, `createdDateTime`, `lastModifiedDateTime`, `version`, `roleScopeTagIds`

This breaks the Desktop detail/settings pane because:
- `DeviceConfiguration` is polymorphic — derived types (e.g., `Windows10CustomConfiguration`,
  `MacOSCustomConfiguration`) carry their settings in type-specific properties
- `$select` on a polymorphic collection only returns the selected base-type fields
- `OnSelectedConfigurationChanged` calls `ExtractGraphObjectSettings(value)` which serializes
  the selected object to JSON and extracts all non-metadata properties as settings
- With `$select`, those derived properties are never populated, so the settings pane is empty

## Chosen approach: Fetch full object on selection

Keep `$select` for list queries (performance) and fetch the full object via
`GetDeviceConfigurationAsync(id)` when the user selects an item.

### Changes required

1. **`MainWindowViewModel.Selection.cs` — `OnSelectedConfigurationChanged`**
   - When `value?.Id != null`, call `_configurationProfileService!.GetDeviceConfigurationAsync(id)`
   - Replace the selected item with the full object before extracting settings
   - Show a brief loading state while the fetch is in progress
   - Fall back to the list object if the fetch fails

2. **Consider applying the same pattern to other polymorphic types**
   - `DeviceCompliancePolicy` (if `$select` is added to its list query)
   - Any other Graph types where `$select` strips derived properties

### Why not the alternatives?

- **Remove $select**: Loses the performance optimization that PR #150 introduced
- **Widen $select**: Graph API does not support selecting derived-type properties
  on polymorphic OData collections — only base-type fields are selectable

### Scope

- Minimal change: one async fetch call in the selection handler
- No service interface changes needed (`GetDeviceConfigurationAsync` already exists)
- No new dependencies
