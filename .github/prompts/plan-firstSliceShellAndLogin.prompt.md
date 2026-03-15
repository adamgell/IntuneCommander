## Plan: First Slice Shell And Login

Build a Windows-first .NET desktop host with an embedded React + TypeScript frontend, but constrain the first slice to visual and behavioral parity for the app shell and login experience only. Reuse Intune.Commander.Core for profile loading and authentication orchestration, and keep theming deliberately low priority.

**Steps**

1. Establish the host shell: create the .NET desktop container that boots immediately, hosts the React app in an embedded webview, and exposes a minimal desktop bridge for app startup, profile loading, login, and shell state. This blocks all later steps.
2. Define the first-slice backend contract: include only startup state, saved profiles, cloud/auth enums, profile CRUD actions needed by the login screen, connect action, busy/error/status messages, and a minimal connected-shell state. Exclude category data loading beyond placeholder content.
3. Mirror the login screen from the current app in React using the existing layout from `LoginView.axaml`: two-column split, left branding panel, right form, saved profile picker, profile fields, auth/cloud selectors, device-code panel, status, progress, and error surfaces. Match structure and spacing first; exact Avalonia theming is explicitly out of scope.
4. Mirror the connected shell chrome from `MainWindow.axaml`, `MainToolbarControl.axaml`, and `NavSidebarControl.axaml`: top menu/header region, toolbar region, left navigation rail, main content panel, and bottom status strip. Use placeholders for overview/list/detail content in this slice.
5. Extract navigation metadata from the current desktop VM into frontend-friendly structures so the shell looks familiar immediately. Reuse the current category grouping and labels, but do not implement per-category loading yet.
6. Wire login and connect behavior through the .NET bridge to existing Core services and preserve the current async-first behavior: the UI renders first, saved profiles load asynchronously, and connect runs without blocking shell paint.
7. Add a simple diagnostics lane for the first slice: expose busy state, status text, and error text so the mirrored shell feels alive during profile load and connect attempts.
8. Verify the slice visually and behaviorally against the current Avalonia app, then use it as the baseline for future category-by-category migration.

**Relevant files**

- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/Views/LoginView.axaml` — source layout for the first-slice login screen
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/Views/MainWindow.axaml` — source layout for the overall shell composition and visibility rules
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/Views/Controls/NavSidebarControl.axaml` — source layout for the left navigation rail
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/Views/Controls/MainToolbarControl.axaml` — source layout for the connected toolbar/header band
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/ViewModels/LoginViewModel.cs` — backend behavior to preserve for saved profiles, validation, auth method switching, and device code state
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/ViewModels/MainWindowViewModel.Connection.cs` — async startup, profile loading, and connect flow to preserve behind the new bridge
- `e:/Repo/IntuneCommander/src/Intune.Commander.Desktop/ViewModels/MainWindowViewModel.Navigation.cs` — category groups and labels to reuse for the mirrored shell
- `e:/Repo/IntuneCommander/src/Intune.Commander.Core` — preserved backend authority for profiles, auth, and future feature migration
- `e:/Repo/IntuneCommander/webview-first-slice-mockups.html` — optional visual reference for early webview-oriented shell exploration

**Verification**

1. Launch the desktop host and confirm the React shell appears before profile loading finishes.
2. Confirm the login screen mirrors the current two-column structure and contains the same major controls and status surfaces.
3. Confirm saved profiles load asynchronously from Core and populate the mirrored picker without blocking initial paint.
4. Confirm connect action updates busy, status, and error state through the bridge and transitions into the mirrored connected shell.
5. Confirm the connected shell shows familiar chrome: toolbar, grouped nav, content region, and status strip, even if category content is still placeholder.
6. Compare the slice side-by-side with the current Avalonia login and shell to validate “similar feel” rather than pixel-perfect parity.

**Decisions**

- Included: shell chrome, login UI, saved profile workflows, connect flow, basic diagnostics/status, familiar navigation structure.
- Excluded: deep theming fidelity, real list/detail category migration, overview dashboard parity, export/import UI, advanced shell commands.
- Technical direction: React + TypeScript frontend, .NET desktop host, Core-backed bridge, async-first startup.

**Exact Host Choice**

- Primary recommendation: `Intune.Commander.DesktopReact` as a WPF desktop host on `net10.0-windows`, embedding the React app with `Microsoft.Web.WebView2`.
- Why this host: lowest Windows-first migration friction, mature windowing model, strong WebView2 integration, straightforward native menus/dialogs/tray support, and no need to buy into a second UI framework when React owns the rendered surface.
- Rejected host for this slice: WinUI 3 adds packaging/tooling complexity for limited benefit when the actual UI is React; WinForms is simpler but a weaker fit for a modern shell; Avalonia-with-webview keeps cross-platform abstraction you no longer need and does not improve the embedded React story enough to justify it.
- Rust-core alternative: not recommended for the current rewrite. It would turn a UI rewrite into a backend/platform rewrite by replacing the existing Core investment in Azure.Identity, Microsoft Graph SDK usage, profile encryption compatibility, cache behavior, and export/import logic. Revisit only if there is a proven backend bottleneck or a long-term product strategy that requires Rust ownership beyond the shell.

- Bridge guidance: keep the WPF layer thin. It should own window lifecycle, native integration, and WebView hosting only. Put profile/auth/connect logic in a dedicated .NET backend service boundary over Core, then expose typed async commands/events to the React app.

**Further Considerations**

1. Use placeholders intentionally in the connected content region so the shell can be validated before category migration begins.
2. Keep frontend state narrow in slice one; avoid recreating the current monolithic desktop VM in TypeScript.
3. Treat this slice as the contract-shaping phase: if the bridge is clean here, later category migrations will be much easier.

**Open Decisions**

**Must Decide Before Coding**

1. Frontend packaging and layout: confirm the React app will be a Vite-based frontend living beside the new WPF host, with production assets bundled into the desktop output and a separate dev-mode local server only for development.
2. Bridge contract shape: choose typed async command/event messages between WPF and React rather than an ad hoc JSON bus, and decide whether that contract is intentionally long-lived enough to version now.
3. State ownership boundary: lock that .NET owns profiles, authentication, connect lifecycle, status, and navigation metadata, while React owns form state, local view state, and rendering.
4. Slice-one login scope: decide whether profile workflows include full create, update, delete, and select behavior, or whether the first slice is limited to load, edit, select, and connect.
5. Auth flows in slice one: decide whether both interactive browser and device code flows are fully operational on day one, or whether one path is postponed or presented as unavailable.
6. Connected-shell definition: decide what the first successful post-login state must contain beyond chrome, such as tenant/profile summary, user identity, or status-only placeholders.
7. Unimplemented navigation behavior: decide whether not-yet-migrated categories are visible as placeholders, visible but disabled, or hidden entirely.
8. Desktop-native responsibility split: decide what remains native in WPF for this slice, specifically menus, dialogs, external browser launch, window lifecycle, and any tray or settings entry points.
9. Coexistence strategy: decide whether the new WPF + React host ships beside the current Avalonia desktop app on this branch until parity improves, rather than replacing the startup path immediately.
10. Security boundary for the webview: decide whether navigation is locked to local app content only and how strict host-to-web message validation must be from the start.

**Can Defer Until After The Shell Boots**

1. Styling depth: exact theming fidelity, font matching, polish, and visual refinement beyond structural parity.
2. Expanded shell content: whether the connected placeholder area should become a minimal overview page before category-by-category migration begins.
3. Longer-term bridge ergonomics: whether generated types, RPC helpers, or contract codegen are worth adding once the initial command/event surface is stable.
4. Testing depth for the frontend: whether to stay with bridge-level tests plus manual acceptance initially or add browser/component tests after the first slice is running.
5. Native feature expansion: file pickers, richer desktop menus, notifications, tray behavior, and any platform integrations not required for login and shell parity.
6. Migration sequencing after slice one: the exact order for moving overview, list/detail pages, export/import surfaces, and other feature areas into React.
