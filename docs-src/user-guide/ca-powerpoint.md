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

The GitHub Actions release workflow reads the `SYNCFUSION_LICENSE_KEY` repository secret and passes it as an MSBuild property during `dotnet publish`:

```
-p:SyncfusionLicenseKey="$env:SYNCFUSION_LICENSE_KEY"
```

This bakes the key into the signed binary as assembly metadata at build time. No environment variable is required at runtime, so your end users get watermark-free exports automatically.

## Current limitations

- Commercial cloud only — GCC-High/DoD support is planned.
- Basic policy detail rendering — advanced dependency lookups (named locations resolved by name, etc.) are in progress.

!!! tip "idPowerToys compatibility"
    The deck format is inspired by [idPowerToys](https://github.com/merill/idPowerToys) CA report but is generated natively without a web dependency.
