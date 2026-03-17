# Export & Import

One of Intune Commander's core features is bulk-exporting all your configurations to JSON and importing them into any tenant. The export format is compatible with the original [IntuneManagement PowerShell tool](https://github.com/Micke-K/IntuneManagement).

!!! info "CLI only"
    Export and import are currently available through the **CLI** (`ic.exe`) only. A desktop UI for export/import is on the roadmap.

## How export works

```bash
ic export --profile Contoso-Prod --output ./export --types all
```

Intune Commander creates a subfolder per object type:

```
IntuneExport/
├── DeviceConfigurations/
│   ├── My Windows Policy.json
│   └── ...
├── CompliancePolicies/
├── SettingsCatalog/
├── EndpointSecurity/
├── Applications/
│   └── (includes assignment lists)
├── ConditionalAccess/
└── migration-table.json    ← ID mapping for import
```

Each `.json` file contains the raw Microsoft Graph Beta model for that object, including assignments where applicable.

The `migration-table.json` at the root maps original object IDs to new IDs created during import, enabling re-runs without duplicating objects.

## How import works

```bash
ic import --folder ./export --profile Contoso-Dev
ic import --folder ./export --profile Contoso-Dev --dry-run
```

The CLI reads each subfolder, creates the objects via Graph API, and updates the migration table with the new IDs. Use `--dry-run` to validate the export folder structure and JSON payloads without authenticating to Graph or creating objects.

!!! warning "Assignments during import"
    Group assignments reference group object IDs, which differ between tenants. After import, review assignments and update group references for the destination tenant.

## Compatibility with IntuneManagement (PowerShell)

Exports from the original PowerShell tool can be imported into Intune Commander, and vice versa. The JSON structure is intentionally kept compatible.

## Supported export types

All 30+ object types supported by the Core library are exportable via CLI. See [Supported Object Types](../reference/object-types.md) for the complete list.
