# Export & Import

One of Intune Commander's core features is bulk-exporting all your configurations to JSON and importing them into any tenant. The export format is compatible with the original [IntuneManagement PowerShell tool](https://github.com/Micke-K/IntuneManagement).

## How export works

1. Connect to the source tenant.
2. Use the **Export** toolbar button (or **File → Export**).
3. Choose an output folder. Intune Commander creates a subfolder per object type:

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

1. Connect to the **destination** tenant.
2. Use **File → Import** and select the export folder.
3. Intune Commander reads each subfolder, creates the objects via Graph API, and updates the migration table with the new IDs.

!!! warning "Assignments during import"
    Group assignments reference group object IDs, which differ between tenants. After import, review assignments and update group references for the destination tenant. Future releases will include a group-mapping UI.

## Compatibility with IntuneManagement (PowerShell)

Exports from the original PowerShell tool can be imported into Intune Commander, and vice versa. The JSON structure is intentionally kept compatible.

## Supported export types

All 30+ object types visible in the navigation are exportable. See [Supported Object Types](../reference/object-types.md) for the complete list.
