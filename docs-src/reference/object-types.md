# Supported Object Types

Intune Commander's Core library and CLI support 30+ Intune and Entra ID object types. All types listed below can be exported via the CLI and (where the Graph API allows) created, updated, and deleted programmatically.

!!! info "Desktop UI coverage"
    The desktop app currently has dedicated workspaces for **Settings Catalog** and **Detection & Remediation** (Device Health Scripts). All other types are accessible via the CLI or Global Search (cached data). Additional workspaces are being added incrementally.

## Device Management

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Device Configurations | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Compliance Policies | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Settings Catalog | ✅ | ✅ | ✅ | ✅ | — | — |
| Endpoint Security | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Administrative Templates | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Feature Update Profiles | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Quality Update Profiles | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| Driver Update Profiles | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Assignment Filters | ✅ | ✅ | — | — | — | — |
| Device Categories | ✅ | — | — | ✅ | ✅ | ✅ |
| ADMX Files | ✅ | ✅ | ✅ | ✅ | — | ✅ |

## Enrollment

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Enrollment Configurations | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Autopilot Profiles | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Apple DEP | ✅ | — | — | — | — | — |
| Terms & Conditions | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Scripts & Compliance

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Device Health Scripts | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Device Shell Scripts (macOS) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Mac Custom Attributes | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Compliance Scripts | ✅ | — | — | — | — | — |

## Apps & App Management

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Applications | ✅ | ✅ | — | — | — | — |
| App Protection Policies | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Managed App Configurations | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Policy Sets | ✅ | — | — | — | — | — |

## Tenant Administration

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Intune Branding | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Azure Branding | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Role Definitions | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Scope Tags | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Conditional Access & Identity

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Conditional Access Policies | ✅ | ✅ | — | — | — | — |
| Authentication Strengths | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Authentication Contexts | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Named Locations | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Terms of Use | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Windows 365

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Cloud PC Provisioning Policies | ✅ | — | — | — | — | — |
| Cloud PC User Settings | ✅ | — | — | — | — | — |

## Groups

| Object Type | List | Export | Import | Create | Update | Delete |
| --- | :---: | :---: | :---: | :---: | :---: | :---: |
| Groups (lookup & members) | ✅ | — | — | — | — | — |

!!! note "Import creates; it does not update"
    The import flow creates new objects in the destination tenant. It does not diff or update existing objects. Re-running an import will skip objects that already exist in the migration table.
