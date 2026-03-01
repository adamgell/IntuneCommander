# Conditional Access PowerPoint Export

Intune Commander can export all your Conditional Access policies to a fully-formatted PowerPoint presentation — ideal for audits, compliance reviews, and architecture documentation.

## Usage

1. Navigate to **Conditional Access** in the left-hand menu.
2. Wait for the policies to load.
3. Click the **Export PowerPoint** button in the toolbar.
4. Choose a save location.
5. Open the generated `.pptx` file in PowerPoint or any compatible application.

## What's in the deck

The generated presentation includes:

- **Cover slide** — tenant name and export timestamp
- **Tenant summary** — total policy counts by state (enabled / disabled / report-only)
- **Policy inventory table** — all policies at a glance
- **Per-policy detail slides** — for each policy:
  - Conditions (users, cloud apps, device platforms, locations, risk levels)
  - Grant controls (MFA, compliant device, etc.)
  - Session controls
  - Assignment scope

## Syncfusion licence

The PowerPoint export feature uses [Syncfusion.Presentation.Net.Core](https://www.syncfusion.com/powerpoint-framework/net). A licence key is required to remove watermarks from exported files.

End users of the **official signed `.exe` release** do not need to do anything — the key is baked into the binary at build time.

### Community Licence (free)

Syncfusion offers a free community licence for:

- Individuals or companies with **less than $1M annual revenue**, **and**
- **5 or fewer developers**

[Register for a community licence →](https://www.syncfusion.com/sales/communitylicense)

### Setting your licence key (development / self-build)

Set the environment variable before launching the app:

```
SYNCFUSION_LICENSE_KEY=your-key-here
```

The app works without a key but will display a watermark on exported slides.

### How the released `.exe` has the key embedded

The tag-triggered `codesign.yml` workflow reads `SYNCFUSION_LICENSE_KEY` from the `codesigning` GitHub Actions environment and passes it as an MSBuild property during `dotnet publish`:

```
-p:SyncfusionLicenseKey="$env:SYNCFUSION_LICENSE_KEY"
```

This bakes the key into the binary as assembly metadata **before** Azure Trusted Signing runs, so the signed `.exe` carries the key. No environment variable is required at runtime — your end users get watermark-free exports automatically.

!!! info "Why embedding the key in the binary is safe"
    The Syncfusion license key is **not a secret credential**. It is a JWT-style token that the Syncfusion library validates locally and offline against embedded product/version metadata. It does not authenticate to any Syncfusion service at runtime — there is no network call, no account access, and no API key exposure. Embedding it in a distributed binary is the pattern Syncfusion's own documentation explicitly describes (`SyncfusionLicenseProvider.RegisterLicense("YOUR_LICENSE_KEY")`). The `AssemblyMetadata` approach used here is strictly better than hardcoding the value in source because the key never appears in git history.

## Current limitations

- Commercial cloud only — GCC-High/DoD support is planned.
- Basic policy detail rendering — advanced dependency lookups (named locations resolved by name, etc.) are in progress.

!!! tip "idPowerToys compatibility"
    The deck format is inspired by [idPowerToys](https://github.com/merill/idPowerToys) CA report but is generated natively without a web dependency.
