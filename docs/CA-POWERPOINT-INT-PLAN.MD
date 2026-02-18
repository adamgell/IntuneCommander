# CA PowerPoint Integration Plan (idPowerToys)

## Objective
Integrate Conditional Access PowerPoint generation into Intune Commander with a native in-app export flow and feature parity with idPowerToys CA deck capabilities, starting with Commercial cloud support.

## Decisions (Locked)
- **Integration model:** Embed/adapt in Core service layer (no sidecar for v1).
- **PPT library:** Syncfusion in-app.
- **Parity target:** Feature parity (not strict visual/pixel-perfect parity).
- **Cloud scope for v1:** Commercial first.
- **Follow-up scope:** GCC, GCC High, DoD in a subsequent wave.

## Success Criteria
- User can export a CA PowerPoint from the desktop app with one action.
- Export runs asynchronously, supports cancellation, and does not block startup/UI thread.
- CA data used for deck generation is pagination-safe and complete for large tenants.
- Output includes high-value parity sections (policy summaries, conditions, grants, assignments, dependencies).
- Build and test pipeline remains green after integration.

## Architecture

### Core export service
- `IConditionalAccessPptExportService`
- `ConditionalAccessPptExportService`

### Data acquisition
Reuse existing CA and dependency services in Core (avoid ad-hoc direct fetch logic).

### Mapping layer
Graph models -> presentation models tailored for slide generation.

### Desktop integration
- New command in `MainWindowViewModel.cs`
- Toolbar/menu trigger in `MainWindow.axaml`

### Output
User-selected file path, timestamped default filename, `.pptx` format.

## Implementation Plan

### Phase 1 — Foundations and Contracts
1. Add feature plan reference in `SERVICE-IMPLEMENTATION-PLAN.md`.
2. Add Syncfusion package dependencies in:
   - `IntuneManager.Core.csproj`
   - `IntuneManager.Desktop.csproj`
3. Add startup/license initialization in `App.axaml.cs`.
4. Add service contract + implementation skeletons under `Services`.

### Phase 2 — Data Aggregation and Mapping
1. Build CA export aggregation using existing services:
   - `ConditionalAccessPolicyService.cs`
   - Existing related services for named locations, terms of use, auth strength/context, apps, groups.
2. Create presentation models for slides:
   - Policy summary
   - Conditions and controls
   - Included/excluded targets
   - Linked object lookups
3. Implement normalization/parsing rules to align with idPowerToys feature output.

### Phase 3 — Slide Generation Engine
1. Implement PowerPoint generation pipeline in `ConditionalAccessPptExportService`:
   - Cover slide
   - Tenant summary slide
   - Policy inventory slide(s)
   - Per-policy detail slide(s)
2. Add formatting helpers and reusable slide components for consistency.
3. Add filename strategy and metadata stamping (tenant, export time, profile).

### Phase 4 — Desktop Command and UX
1. Add CA export command in `MainWindowViewModel.cs`.
2. Add CA export button/menu action in `MainWindow.axaml`.
3. Wire save dialog and completion/error notifications in `MainWindow.axaml.cs`.
4. Ensure action is enabled only when CA data and connection context are available.

### Phase 5 — Test and Validation
1. Add Core tests in `Services` for:
   - Mapping correctness
   - Pagination completeness
   - Generation smoke test for valid non-empty `.pptx` output
2. Add error-path tests for:
   - Missing dependencies
   - Malformed policy payloads
   - Cancellation token behavior
3. Validate desktop behavior manually:
   - Status/progress updates
   - No UI blocking
   - Clear user-facing error messages

### Phase 6 — Documentation and Release
1. Update usage and prerequisites in `README.md`.
2. Add release entry in `CHANGELOG.md`.
3. Document v1 limitations:
   - Commercial-first
   - Diagram export deferred
   - Feature-level parity target

## Risks
- Syncfusion runtime/licensing setup could fail if not initialized early.
- Large tenants may increase export duration; cancellation and progress updates are required.
- Upstream idPowerToys behavior can evolve; parity should be maintained via explicit acceptance criteria, not implementation coupling.

## Out of Scope (v1)
- GCC/GCC High/DoD support.
- Diagram export integration.
- Strict pixel-perfect template cloning.

## Follow-up Wave
- Add sovereign cloud endpoint/profile support.
- Add optional diagram export.
- Add template/theme customization options.
- Expand PPT export to additional categories after CA stabilization.