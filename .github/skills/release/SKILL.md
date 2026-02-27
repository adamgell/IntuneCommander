---
name: release
description: "Create a versioned release of Intune Commander. Use when: user says release, version bump, ship, tag, publish, build release, cut a release, deploy new version. Handles version updates in csproj files, git commit, tag, and push to trigger the codesign workflow."
argument-hint: "Version number (e.g. 0.5.0 or 0.5.0-beta1)"
---

# Release Intune Commander

## When to Use

- User asks to create a new release, version bump, ship, tag, or publish
- User says "do a release" or "release X.Y.Z"

## Prerequisites

- Must be on the `main` branch with a clean working tree (no unrelated uncommitted changes)
- The version argument must follow semver: `MAJOR.MINOR.PATCH` or `MAJOR.MINOR.PATCH-prerelease`

## Procedure

### 1. Validate

- Confirm the current branch is `main`
- Run `git status --short` to check for uncommitted changes
- If there are unrelated uncommitted changes, ask the user before proceeding
- Confirm the version number with the user if not explicitly provided

### 2. Update CHANGELOG.md

Read the current `CHANGELOG.md`. The `## [Unreleased]` section contains all changes since the last release.

1. Rename `## [Unreleased]` to `## [X.Y.Z] — YYYY-MM-DD` (use today's date)
2. Add a fresh empty `## [Unreleased]` section above it
3. Keep all the content (Added, Changed, Fixed, etc.) under the new versioned heading
4. Use `git log --oneline <previous-tag>..HEAD` to check for any commits not yet captured in the changelog — add them to the appropriate section if missing

The changelog follows [Keep a Changelog](https://keepachangelog.com/) format with these sections:
- **Added** — new features
- **Changed** — changes to existing functionality
- **Fixed** — bug fixes
- **Removed** — removed features
- **Documentation** — docs-only changes
- **Build & Validation** — CI/build changes

### 3. Update Version in Both csproj Files

Update these three properties in **both** project files:

- `src/Intune.Commander.Core/Intune.Commander.Core.csproj`
- `src/Intune.Commander.Desktop/Intune.Commander.Desktop.csproj`

Set:
```xml
<Version>X.Y.Z</Version>
<AssemblyVersion>X.Y.Z.0</AssemblyVersion>
<FileVersion>X.Y.Z.0</FileVersion>
```

For prerelease versions (e.g. `0.5.0-beta1`), `Version` keeps the suffix but `AssemblyVersion` and `FileVersion` use only the numeric part:
```xml
<Version>0.5.0-beta1</Version>
<AssemblyVersion>0.5.0.0</AssemblyVersion>
<FileVersion>0.5.0.0</FileVersion>
```

### 4. Commit

```
git add CHANGELOG.md src/Intune.Commander.Core/Intune.Commander.Core.csproj src/Intune.Commander.Desktop/Intune.Commander.Desktop.csproj
git commit -m "release: v X.Y.Z"
```

### 5. Push to Main

```
git push origin main
```

### 6. Create and Push Tag

```
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

This triggers the `codesign` GitHub Actions workflow (`.github/workflows/codesign.yml`) which:
1. Builds a self-contained single-file Windows x64 exe
2. Code-signs it via Azure Trusted Signing
3. Creates a GitHub Release with auto-generated notes

### 7. Confirm with Release Summary

After all steps complete, print a formatted release summary:

```
══════════════════════════════════════════════════
  Intune Commander vX.Y.Z — Released!
══════════════════════════════════════════════════

  Tag:       vX.Y.Z
  Commit:    <short-hash>
  Date:      YYYY-MM-DD

  What's New:
    - <bullet summary of Added items>

  Changes:
    - <bullet summary of Changed items>

  Fixes:
    - <bullet summary of Fixed items>

  Links:
    Actions:  https://github.com/adamgell/IntuneCommander/actions
    Release:  https://github.com/adamgell/IntuneCommander/releases/tag/vX.Y.Z

  The codesign workflow is building your signed exe now.
══════════════════════════════════════════════════
```

Populate the "What's New", "Changes", and "Fixes" sections from the changelog entries for this version. Omit any section that has no entries. Keep each bullet to one line.

## Important Notes

- The codesign workflow also supports manual dispatch via `workflow_dispatch` — but pushing a tag is the standard path
- The `-p:Version=` flag in the workflow overrides the csproj values at build time, but we keep csproj in sync so local builds also show the correct version
- The `VersionText` property in `MainWindowViewModel` reads the assembly version at runtime for the bottom-left version display
