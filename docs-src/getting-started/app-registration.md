# App Registration Setup

Before Intune Commander can connect to your tenant, you need an **Azure AD app registration** with the appropriate Microsoft Graph delegated permissions.

## Commercial tenants

1. Sign in to the [**Microsoft Entra**](https://entra.microsoft.com) and navigate to **App registrations → New registration**.
2. Give it a name (e.g. `IntuneCommander`).
3. Under **Supported account types**, select *Accounts in this organizational directory only*.
4. Under **Redirect URI**, choose **Mobile and desktop applications** and enter:
   ```
   http://localhost:45132
   ```
5. Click **Register**.

### Add Graph permissions

1. In your new app registration, go to **API permissions → Add a permission → Microsoft Graph → Delegated permissions**.
2. Add the following permissions:

| Permission | Purpose |
|---|---|
| `DeviceManagementConfiguration.ReadWrite.All` | Device Configs, Compliance, Settings Catalog, Endpoint Security |
| `DeviceManagementScripts.ReadWrite.All` | Device Health Scripts, Mac Custom Attributes |
| `DeviceManagementApps.ReadWrite.All` | Applications, App Protection Policies, Policy Sets |
| `DeviceManagementServiceConfig.ReadWrite.All` | Enrollment, Autopilot, Branding, Terms & Conditions |
| `DeviceManagementRBAC.ReadWrite.All` | Roles, Scope Tags |
| `DeviceManagementManagedDevices.Read.All` | Device Categories |
| `Policy.ReadWrite.ConditionalAccess` | Conditional Access Policies |
| `Agreement.ReadWrite.All` | Terms of Use |
| `Organization.Read.All` | Azure Branding (org ID resolution) |
| `OrganizationalBranding.ReadWrite.All` | Azure Branding |
| `Group.Read.All` | Group lookup |
| `GroupMember.Read.All` | Group member enumeration |
| `CloudPC.ReadWrite.All` | Windows 365 Cloud PC *(requires W365 licence)* |

3. Click **Grant admin consent** for your tenant.

!!! info "Full permissions reference"
    For a complete breakdown of every permission and which service uses it, see the [Graph Permissions reference](../reference/graph-permissions.md).

!!! alert "Client secret permissions" 
    Make sure you use application level permissions instead of delegated.

## Government clouds (GCC-High / DoD)

Government clouds require **separate app registrations** in their own portals.

| Cloud | Portal |
|---|---|
| GCC | [portal.azure.com](https://portal.azure.com) (same as Commercial) |
| GCC-High | [portal.azure.us](https://portal.azure.us) |
| DoD | [portal.apps.mil](https://portal.apps.mil) |

The steps are identical — register in the cloud-specific portal and use the same redirect URI (`http://localhost:45132`).

## Next steps

With an app registration ready, [add your first profile](first-login.md) in Intune Commander.
