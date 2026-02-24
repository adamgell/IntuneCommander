# Plan: Split MainWindow.axaml into Separate Views

## Decisions
- **Scope**: Full decomposition (Toolbar, Nav, ItemList, DetailPane + 32 type-specific detail views)
- **DataContext**: All sub-views inherit `MainWindowViewModel` directly (no new child VMs)
- **Detail granularity**: One UserControl per type (32 files), hosted by a routing `DetailPaneView`
- **`$parent[ScrollViewer]` bindings**: Safe as long as each detail UserControl declares `x:DataType="vm:MainWindowViewModel"`
- **Phase approach**: Phase 1 is highest-value (32 detail views), Phases 2+ are the shell decomposition

## File Inventory

### Files to create

**Phase 1 — Detail views (x:DataType="vm:MainWindowViewModel")**
- `Views/Detail/ConfigurationDetailView.axaml` + `.cs`
- `Views/Detail/CompliancePolicyDetailView.axaml` + `.cs`
- `Views/Detail/SettingsCatalogDetailView.axaml` + `.cs`
- `Views/Detail/ConditionalAccessDetailView.axaml` + `.cs`
- `Views/Detail/AssignmentFilterDetailView.axaml` + `.cs`
- `Views/Detail/PolicySetDetailView.axaml` + `.cs`
- `Views/Detail/ApplicationDetailView.axaml` + `.cs`
- `Views/Detail/EndpointSecurityDetailView.axaml` + `.cs`
- `Views/Detail/AdministrativeTemplateDetailView.axaml` + `.cs`
- `Views/Detail/EnrollmentConfigurationDetailView.axaml` + `.cs`
- `Views/Detail/AppProtectionPolicyDetailView.axaml` + `.cs`
- `Views/Detail/ManagedDeviceAppConfigDetailView.axaml` + `.cs`
- `Views/Detail/TargetedManagedAppConfigDetailView.axaml` + `.cs`
- `Views/Detail/TermsAndConditionsDetailView.axaml` + `.cs`
- `Views/Detail/ScopeTagDetailView.axaml` + `.cs`
- `Views/Detail/RoleDefinitionDetailView.axaml` + `.cs`
- `Views/Detail/BrandingProfileDetailView.axaml` + `.cs`
- `Views/Detail/AzureBrandingDetailView.axaml` + `.cs`
- `Views/Detail/AutopilotProfileDetailView.axaml` + `.cs`
- `Views/Detail/DeviceHealthScriptDetailView.axaml` + `.cs`
- `Views/Detail/MacCustomAttributeDetailView.axaml` + `.cs`
- `Views/Detail/FeatureUpdateProfileDetailView.axaml` + `.cs`
- `Views/Detail/NamedLocationDetailView.axaml` + `.cs`
- `Views/Detail/AuthStrengthPolicyDetailView.axaml` + `.cs`
- `Views/Detail/AuthContextClassRefDetailView.axaml` + `.cs`
- `Views/Detail/TermsOfUseDetailView.axaml` + `.cs`
- `Views/Detail/AppAssignmentDetailView.axaml` + `.cs`
- `Views/Detail/DynamicGroupDetailView.axaml` + `.cs`
- `Views/Detail/AssignedGroupDetailView.axaml` + `.cs`
- `Views/Detail/DeviceManagementScriptDetailView.axaml` + `.cs`
- `Views/Detail/DeviceShellScriptDetailView.axaml` + `.cs`
- `Views/Detail/ComplianceScriptDetailView.axaml` + `.cs`
- `Views/Detail/NoSelectionDetailView.axaml` (placeholder shown when nothing selected)
- `Views/DetailPaneView.axaml` + `.cs` (hosts 32 toggled detail view references)

**Phase 2 — Shell decomposition**
- `Views/ToolbarView.axaml` + `.cs`
- `Views/NavSidebarView.axaml` + `.cs`
- `Views/ItemListView.axaml` + `.cs` (contains MainDataGrid — must absorb code-behind grid management)
- `Views/MenuBarView.axaml` + `.cs`
- `Views/StatusBarView.axaml` + `.cs`

### Files to modify
- `Views/MainWindow.axaml` — stripped to ~100 lines shell using 5-6 sub-view references
- `Views/MainWindow.axaml.cs` — move DataGrid column management to ItemListView.axaml.cs; fix FindControl for Toolbar buttons
- `.csproj` — new files auto-discovered (Avalonia uses glob), no explicit changes needed

## Key Technical Notes

### `$parent[ScrollViewer]` binding pattern
Each type-specific detail view must declare `x:DataType="vm:MainWindowViewModel"` so `$parent` traversal continues to work. The ScrollViewer is *inside* the UserControl, so the escape still resolves to it correctly.

### DataGrid code-behind
`MainWindow.axaml.cs` handles `RebuildDataGridColumns()` and `BindDataGridSource()` imperatively, watching PropertyChanged on VM. When `MainDataGrid` moves to `ItemListView`, this logic moves to `ItemListView.axaml.cs`. It accesses the VM via `(MainWindowViewModel)DataContext`.

### FindControl fixes required
In `MainWindow.axaml.cs` these named controls must be accessible after moving:
- `ImportButton` → moves inside `ToolbarView`; handler moves to `ToolbarView.axaml.cs`, wired via event or command
- `GroupLookupButton` → same
- `ColumnChooserButton` → moves to `ItemListView.axaml.cs`
- `OverviewNavButton` → moves into `NavSidebarView.axaml.cs`

### DetailPaneView routing
`DetailPaneView.axaml` contains 32 `<views:XxxDetailView>` plus `<views:NoSelectionDetailView>`, each with `IsVisible="{Binding Selected* != null}"`. This reduces the per-type views to pure content blocks.

### NoSelection placeholder
The 32-way MultiBinding "Select an item" placeholder moves to `NoSelectionDetailView.axaml`, visibility bound with the same MultiBinding logic.

## Verification Steps
1. `dotnet build` — no compile errors after each phase
2. Launch app, connect to tenant — confirm all 32 detail panels render
3. Verify `$parent[ScrollViewer]` computed columns still display (e.g. platform column in Device Configurations)
4. Verify DataGrid columns rebuild on category switch
5. Verify Import, GroupLookup, ColumnChooser buttons function
6. `dotnet test --filter "Category!=Integration"` — all unit tests pass unchanged

## Further Considerations
- Each `XxxDetailView.axaml` is an independent UserControl — free to use different layouts, type-specific controls (syntax highlighting for scripts, charts for health scripts, etc.), spacing and grouping without affecting other types
- `ToolbarView` has two `Click` event handlers (`ImportButton_Click`, `GroupLookupButton_Click`) that open dialogs using the DI container. Options: **(A)** move as-is to `ToolbarView.axaml.cs` (needs DI service locator access), or **(B)** convert to existing VM `[RelayCommand]` bindings (cleaner — decide upfront)
