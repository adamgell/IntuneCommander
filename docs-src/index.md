# Intune Commander Documentation

<span class="ic-gradient" style="font-size:1.4rem; font-weight:700;">Intune Management, Commanded.</span>

Intune Commander is a cross-platform desktop application for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD cloud environments. Built on .NET 10 and Avalonia UI, it replaces slow PowerShell scripts with a fast, async-first native application.

---

## Quick Links

<div class="grid cards" markdown>

-   :material-rocket-launch: **Get Started**

    ---

    Download the app, register an Azure AD app, and connect to your first tenant in minutes.

    [:octicons-arrow-right-24: Installation](getting-started/installation.md)

-   :material-cloud-outline: **Multi-Cloud**

    ---

    Connect to Commercial, GCC, GCC-High, and DoD tenants from a single profile list.

    [:octicons-arrow-right-24: Multi-Cloud guide](user-guide/multi-cloud.md)

-   :material-export: **Export & Import**

    ---

    Bulk-export all Intune configurations to JSON and import them into any tenant.

    [:octicons-arrow-right-24: Export & Import](user-guide/export-import.md)

-   :material-shield-key: **Graph Permissions**

    ---

    Full reference of every Microsoft Graph permission the app requires, and why.

    [:octicons-arrow-right-24: Graph Permissions](reference/graph-permissions.md)

</div>

---

## What is Intune Commander?

Intune Commander is a ground-up .NET remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement). The original PowerShell/WPF tool is widely used in the Microsoft 365 community but suffers from UI deadlocks, threading issues, and slow data refresh. Intune Commander solves those problems with compiled .NET code, an async-first architecture, and a modern Avalonia UI.

### Key features

| Feature | Details |
|---|---|
| **30+ object types** | Device Configurations, Compliance, Settings Catalog, Endpoint Security, CA Policies, and more |
| **Bulk export / import** | JSON format compatible with the original PowerShell tool |
| **Multi-cloud** | Commercial · GCC · GCC-High · DoD |
| **Encrypted profile storage** | DataProtection-encrypted local storage — credentials never leave your machine |
| **Smart caching** | LiteDB-backed 24-hour cache per tenant |
| **CA PowerPoint export** | Conditional Access policies rendered as a full PowerPoint deck |
| **Native performance** | Compiled .NET 10, async-first — no UI freezes |

---

## Platform support

| Platform | Status |
|---|---|
| **Windows** | ✅ Fully supported (recommended) |
| **macOS** | ⚠️ Supported with limitations — requires Device Code auth instead of browser popup |
| **Linux** | 🔜 Planned |

!!! note "Early preview"
    Intune Commander is under active development. Expect rough edges and breaking changes between releases. [Report issues on GitHub.](https://github.com/adamgell/IntuneCommander/issues)
