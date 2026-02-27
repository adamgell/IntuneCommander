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

### 2. Update Version in Both csproj Files

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

### 3. Commit

```
git add src/Intune.Commander.Core/Intune.Commander.Core.csproj src/Intune.Commander.Desktop/Intune.Commander.Desktop.csproj
git commit -m "chore: bump version to X.Y.Z"
```

### 4. Push to Main

```
git push origin main
```

### 5. Create and Push Tag

```
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

This triggers the `codesign` GitHub Actions workflow (`.github/workflows/codesign.yml`) which:
1. Builds a self-contained single-file Windows x64 exe
2. Code-signs it via Azure Trusted Signing
3. Creates a GitHub Release with auto-generated notes

### 6. Confirm

Tell the user:
- The tag has been pushed
- The codesign workflow has been triggered
- Link to monitor: `https://github.com/adamgell/IntuneCommander/actions`
- The release will appear at: `https://github.com/adamgell/IntuneCommander/releases/tag/vX.Y.Z`

## Important Notes

- The codesign workflow also supports manual dispatch via `workflow_dispatch` — but pushing a tag is the standard path
- The `-p:Version=` flag in the workflow overrides the csproj values at build time, but we keep csproj in sync so local builds also show the correct version
- The `VersionText` property in `MainWindowViewModel` reads the assembly version at runtime for the bottom-left version display
