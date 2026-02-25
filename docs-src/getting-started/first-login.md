# First Login

With the app installed and an app registration created, here's how to connect to your first tenant.

## Add a profile

1. Launch **Intune Commander** — you'll land on the login screen.
2. Fill in the required fields:

| Field | Description |
| --- | --- |
| **Name** | A friendly label for this profile (e.g. `Contoso-Prod`) |
| **Tenant ID** | Your Entra ID tenant ID (GUID) |
| **Client ID** | The Application (client) ID from your app registration |
| **Cloud** | `Commercial`, `GCC`, `GCCHigh`, or `DoD` |
| **Auth Method** | `Interactive` (delegated permission) or `Client Secret` (application permission) |

1. Click **Save Profile** — the profile is encrypted and stored locally.
2. Select the profile and click **Connect**. A browser window will open for interactive sign-in.

## Import profiles from a JSON file

If you manage multiple tenants you can import them all at once:

1. Click **Import Profiles** on the login screen.
2. Select a `.json` file containing one or more profiles.
3. Profiles are merged — duplicates (same Tenant ID + Client ID) are skipped automatically.

A ready-to-use template is available at [`.github/profile-template.json`](https://github.com/adamgell/IntuneCommander/blob/main/.github/profile-template.json):

```json
[
  {
    "name": "Contoso-Prod",
    "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "cloud": "Commercial",
    "authMethod": "Interactive"
  }
]
```

Valid `cloud` values: `Commercial` · `GCC` · `GCCHigh` · `DoD`  
Valid `authMethod` values: `Interactive` · `ClientSecret` (add `"clientSecret"` field when using this)

## After connecting

Once connected, Intune Commander loads your tenant's data asynchronously — the UI is immediately usable while background loading completes. Use the left-hand navigation to browse object types.
