# Intune Commander Documentation

<span class="ic-gradient" style="font-size:1.4rem; font-weight:700;">Intune Management, Commanded.</span>

Intune Commander is a Windows desktop application for managing Microsoft Intune configurations across Commercial, GCC, GCC-High, and DoD cloud environments. Built on .NET 10 with a React 19 / TypeScript frontend hosted in WPF + WebView2, it replaces slow PowerShell scripts with a fast, async-first native application.

---

!!! note "Early release"
    Intune Commander is under active development. The desktop UI currently covers a small number of workspaces, with additional workspaces being added incrementally. The CLI and backend Core library support 30+ object types.
    [Report issues on GitHub.](https://github.com/adamgell/IntuneCommander/issues)

## Quick Links

<div class="grid cards" markdown>

- :material-rocket-launch: **Get Started**

    ---

    Download the MSI, register an Entra ID app, and connect to your first tenant in minutes.

    [:octicons-arrow-right-24: Installation](getting-started/installation.md)

- :material-cloud-outline: **Multi-Cloud**

    ---

    Connect to Commercial, GCC, GCC-High, and DoD tenants from a single profile list.

    [:octicons-arrow-right-24: Multi-Cloud guide](user-guide/multi-cloud.md)

- :material-console: **CLI**

    ---

    Export, import, diff, and manage Intune configurations from the command line.

    [:octicons-arrow-right-24: CLI guide](user-guide/cli.md)

- :material-shield-key: **Graph Permissions**

    ---

    Full reference of every Microsoft Graph permission the app requires, and why.

    [:octicons-arrow-right-24: Graph Permissions](reference/graph-permissions.md)

</div>

---

## What is Intune Commander?

Intune Commander is a ground-up .NET remake of [Micke-K/IntuneManagement](https://github.com/Micke-K/IntuneManagement). The original PowerShell/WPF tool is widely used in the Microsoft 365 community but suffers from UI deadlocks, threading issues, and slow data refresh. Intune Commander solves those problems with compiled .NET code, an async-first architecture, and a modern React UI.

### What's built today

| Area | What works |
|---|---|
| **Desktop UI** | Login & profile management, Overview Dashboard, Settings Catalog workspace, Detection & Remediation workspace, Global Search |
| **CLI** | Export, import (with dry-run), diff, list, profile management, drift alerting |
| **Backend** | 30+ Graph API services built and tested — most are used by the CLI but not yet wired into the desktop UI |

### Core capabilities

| Feature | Details |
|---|---|
| **Multi-cloud** | Commercial, GCC, GCC-High, DoD |
| **Encrypted profile storage** | DataProtection-encrypted local storage — credentials never leave your machine |
| **Smart caching** | LiteDB-backed 24-hour cache per tenant |
| **Native performance** | Compiled .NET 10, async-first — no UI freezes |

### Roadmap (not yet in the desktop UI)

The following have backend/CLI support but no desktop UI workspace yet: Device Configurations, Compliance Policies, Endpoint Security, Administrative Templates, Conditional Access (including PowerPoint export), Applications, App Protection, Enrollment, Autopilot, Assignment Filters, and more. See [Supported Object Types](reference/object-types.md) for the full list.

---

## Platform support

| Platform | Status |
|---|---|
| **Windows** | Supported (Windows 10 April 2018+ or Windows 11) |
