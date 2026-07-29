# v2.4.4 Maintenance Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the project safer to install and release by unifying version metadata, moving writable data to AppData, validating bundled assets, and enforcing build/test checks in CI.

**Architecture:** Keep bundled read-only assets under the application directory and keep user-owned settings/history under `%LOCALAPPDATA%\\CassieWordCheck`. Centralize path and migration logic in `Models/AppDataPaths.cs`; expose a pure project integrity validator for xUnit and CI. Use `Directory.Build.props` as the local version source, with release tags overriding version properties during publish.

**Tech Stack:** .NET 8, WPF, C# 12, xUnit, GitHub Actions, PowerShell/Batch release scripts, JSON locale files.

## Global Constraints

- Keep the application Windows-only and preserve the existing public behavior of settings, history, wordlist loading, and localization fallback.
- Do not overwrite existing user data during migration; copy legacy files only when the AppData destination is absent.
- All user-facing logical changes must add a top entry to `Views/AboutWindow.xaml.cs` `ChangelogText`.
- The built-in wordlist is parsed with the same whitespace/comment rules as `WordList.LoadFromFile`.
- Locale validation compares every locale against the union of all locale keys and reports missing, extra, malformed, or empty entries.

### Task 1: Define failing regression tests

**Files:**
- Create: `CassieWordCheck.Tests/AppDataPathsTests.cs`
- Create: `CassieWordCheck.Tests/ProjectIntegrityValidatorTests.cs`

- [ ] Add tests proving legacy `appsettings.json` and `history.json` are copied to a new user directory only when the destination file does not exist.
- [ ] Add tests proving locale validation catches a missing key and accepts the repository's current locale set.
- [ ] Add tests proving wordlist validation accepts the current `data/cassie-text.txt` format and rejects an empty wordlist.
- [ ] Run the focused tests and observe the expected compile/failure state before production implementation.

### Task 2: Implement path migration and version source

**Files:**
- Create: `Directory.Build.props`
- Create: `Models/AppDataPaths.cs`
- Modify: `Models/Settings.cs`
- Modify: `Models/HistoryStore.cs`
- Modify: `Models/AppInfo.cs`
- Modify: `CassieWordCheck.csproj`
- Modify: `Views/AboutWindow.xaml`

- [ ] Set version `2.4.4` and file/assembly version `2.4.4.0` in `Directory.Build.props`; remove duplicated version properties from the project file.
- [ ] Make `AppInfo.Version` read the executing assembly version rather than storing another hard-coded version.
- [ ] Resolve default settings/history paths under `%LOCALAPPDATA%\\CassieWordCheck`.
- [ ] Create the target directory on save and migrate legacy files from `<application>\\data` without overwriting newer AppData files.
- [ ] Preserve constructor-injected custom paths used by existing tests and callers.
- [ ] Make About-window design-time version text match `2.4.4` and describe folder publish/install packaging accurately.
- [ ] Run all settings/history/path tests and then the full test project.

### Task 3: Add project asset integrity validation

**Files:**
- Create: `Resources/Services/ProjectIntegrityValidator.cs`
- Modify: `CassieWordCheck.Tests/ProjectIntegrityValidatorTests.cs`

- [ ] Implement a validator that checks the built-in wordlist exists, contains at least one parsed word, and has no duplicate normalized tokens.
- [ ] Implement locale JSON parsing and key-set validation across `Resources/Locales/*.json`, including non-empty values and consistent self-description keys.
- [ ] Return structured issues so tests and CI can print actionable file/key diagnostics.
- [ ] Run the validator tests against the repository root and confirm the current assets pass.

### Task 4: Enforce build/test and release consistency in CI

**Files:**
- Modify: `.github/workflows/release.yml`
- Modify: `publish.bat`
- Modify: `setup.iss`

- [ ] Add a `ci` workflow triggered by pushes and pull requests to restore, build, test, and run asset validation.
- [ ] Make release workflow derive a normalized version from the tag, pass it to `dotnet publish`, and fail if the tag is not a valid `vX.Y.Z` version.
- [ ] Ensure published locale files and bundled data are copied to the publish output.
- [ ] Update local publish script to use the centralized version and describe folder publish plus installer packaging correctly.
- [ ] Update Inno Setup default version to `2.4.4` and keep the tag-provided version path intact.

### Task 5: Synchronize documentation and changelog

**Files:**
- Modify: `README.md`
- Modify: `CONTRIBUTING.md`
- Modify: `docs/project-knowledge/conventions.md`
- Modify: `docs/project-knowledge/tech-stack.md`
- Modify: `docs/project-knowledge/index.md`
- Modify: `Views/AboutWindow.xaml.cs`

- [ ] Document AppData ownership and legacy migration behavior.
- [ ] Replace stale single-file-publish instructions with the current self-contained folder publish and Inno Setup flow.
- [ ] Document xUnit coverage and CI validation commands.
- [ ] Correct the project knowledge entry that says no test project exists.
- [ ] Add the `v2.4.4` changelog entry covering version unification, AppData migration, CI checks, and asset validation.

### Task 6: Final verification

- [ ] Run repository searches for stale versions, stale single-file claims, and obsolete data paths.
- [ ] Run all available local tests and build checks; report the exact limitation if the local environment lacks the .NET SDK.
- [ ] Inspect `git diff --check`, final status, and the complete changed-file list.
