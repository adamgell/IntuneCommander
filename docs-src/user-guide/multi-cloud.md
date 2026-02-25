# Multi-Cloud Support

Intune Commander supports all four Microsoft cloud environments from a single application. Each cloud uses a separate app registration and connects to the appropriate Graph API endpoint.

## Cloud endpoints

| Cloud | Graph API | Authority |
|---|---|---|
| **Commercial** | `graph.microsoft.com` | `login.microsoftonline.com` |
| **GCC** | `graph.microsoft.com` | `login.microsoftonline.com` |
| **GCC-High** | `graph.microsoft.us` | `login.microsoftonline.us` |
| **DoD** | `dod-graph.microsoft.us` | `login.microsoftonline.us` |

!!! note "GCC vs Commercial"
    GCC is essentially the Commercial cloud with additional compliance features. GCC tenants use the same Graph endpoint and authority as Commercial, and the app registration process is identical — you just create a **separate app registration** in the Commercial portal targeted to the GCC tenant with GCC-specific admin consent.

## App registrations per cloud

Each cloud environment requires its own app registration in the respective portal:

| Cloud | Portal |
|---|---|
| Commercial / GCC | [portal.azure.com](https://portal.azure.com) |
| GCC-High | [portal.azure.us](https://portal.azure.us) |
| DoD | [portal.apps.mil](https://portal.apps.mil) |

See [App Registration Setup](../getting-started/app-registration.md) for step-by-step instructions.

## Profile-based switching

Each saved profile stores the cloud environment alongside the tenant and client IDs. Switching clouds is as simple as selecting a different profile on the login screen — no reconfiguration needed.

## Windows 365 (Cloud PC) note

Cloud PC features require the tenant to have an active Windows 365 licence. Without a licence, the Cloud PC endpoints return HTTP 403 regardless of app permissions.
