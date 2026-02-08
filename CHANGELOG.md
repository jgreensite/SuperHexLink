# Changelog

All notable changes to SuperHexLink are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased] — v0.3.0 Senior Code Review Uplift

### Added
- `BACKLOG.md` — structured product roadmap with Epics, Features, Stories (now 7 Epics)
- `CHANGELOG.md` — this file
- `.github/ISSUE_TEMPLATE/` — bug report, feature request, and story templates
- `.github/PULL_REQUEST_TEMPLATE.md` — PR checklist
- `.github/CODEOWNERS` — auto-review assignment
- `.github/dependabot.yml` — automated NuGet + GitHub Actions dependency scanning
- `.editorconfig` — C# naming conventions, var preferences, brace rules
- `LICENSE` — MIT license
- `docs/SECURITY.md` — threat model, trust boundaries, input validation, secrets management
- `docs/PERFORMANCE.md` — performance budgets, profiling guide, known hotspots, optimization patterns
- `docs/ONBOARDING.md` — day-1 guide for new developers with exercises and cheat sheet
- `docs/adr/` — 4 Architecture Decision Records (CoreLogic separation, backing state, snapshot undo, structured logging)
- `HexGrid.Distance()` — cube-coordinate Manhattan distance method in CoreLogic
- `HexGridDistanceTests` — 7 tests (same hex, neighbor, symmetry, triangle inequality, large/negative coords)
- `BaseHexStateBoundaryTests` — 10 tests (null fields, empty strings, missing JSON properties, extreme values, equality)
- `CoreLogic.csproj` + `CoreLogic.Tests.csproj` — recreated project files (were gitignored)
- XML documentation on all core classes (`GameSpawner`, `HexSpawner`, `Hex`, `SpawnerBase`, `HexGrid`, `Corner`, `EdgeSpawner`, `CornerSpawner`)
- `EnsureValidGameSpawnerState` method restored in `HexSpawner`
- `HexGridConfig.CreateDefault()` factory method
- `Corner.Init()` method (replaces constructor for MonoBehaviour compliance)
- `corelogic-tests` CI job runs on every push (not just PRs)

### Changed
- **GameConstants**: `static` fields → `const`/`static readonly`; removed empty `Start()`; renamed `materialMap` → `MaterialMap`; compound assignments; simplified `new` expressions
- **SpawnerBase**: Promoted `IsConfiguredEmpty` to `protected static` base method; removed unused imports
- **HexSpawner**: PascalCase method names (`IsReplaceable`, `IsNumberedLandType`, `IsRenderedType`); structured logging via `ActionLogger`; removed all commented-out code blocks
- **GameSpawner**: Replaced ~40 `Debug.Log` calls in `LoadState` with structured `ActionLogger` logging; added XML docs; removed commented-out code
- **Hex**: Fixed indentation inconsistency; consolidated `NotSelect()` into `Deselect()`; simplified `Neighbours()`
- **Helpers**: Converted from `MonoBehaviour` to `static class`; fixed typo `GetChilObjectLights` → `GetChildObjectLights`; added `StringComparison.OrdinalIgnoreCase`
- **Extensions**: Fixed typo `Veertex0` → `Vertex0`
- **Rng**: Replaced deprecated `RNGCryptoServiceProvider` with `RandomNumberGenerator.Create()`
- **Corner**: Removed constructor (MonoBehaviour); added `Init()` method; removed empty `Start()`/`Update()`
- **EdgeSpawner/CornerSpawner**: Removed large commented-out code blocks; cleaned unused imports
- **HexGrid**: Added `[Serializable]` attribute; XML docs; robust `IsInBounds`
- **LegacyMapConverter**: Replaced `int.Parse` with `int.TryParse` for defensive null handling
- **MapValidationWindow**: Fixed dead `summaryStyle` variable; fixed orphaned block (missing `if` condition); removed trailing junk comment
- **SelectLand**: Updated `NotSelect()` → `Deselect()` references
- **README.md**: Rewritten with badges (CI, Unity, .NET, License), architecture diagram, tables, proper quick-start
- **CONTRIBUTING.md**: Consolidated root file with `docs/CONTRIBUTING.md`; added CoreLogic `dotnet test` to "Run Tests Locally"
- **GameConstants.cs**: Fixed all lint warnings (braces, ternary, formatting, dictionary spacing)
- **CI workflow**: Fixed artifact name collision; added dedicated `corelogic-tests` job
- **BACKLOG.md**: Checked off completed items; added Epic 7 (Security, Performance, Onboarding)
- **`.gitignore`**: Added `!CoreLogic/**/*.csproj` exception; added `/Prompts/` exclusion

### Removed
- `test-log.txt` — stale test artifact
- `Prompts/` — dev scratch (AI image prompts, superseded backlog)
- Commented-out material fields and `CS` reference in `HexSpawner`
- Commented-out code blocks in `EdgeSpawner` and `CornerSpawner`
- Empty `Start()` method in `GameConstants`
- Duplicate `isConfiguredEmpty` / `isReplaceableLandType` methods in `HexSpawner`
- `NotSelect()` method in `Hex` (consolidated into `Deselect()`)

---

## [v0.2.0] — 2025-12-21 — Map Editor v2

### Added
- Map Editor window (`SuperHexLink > Map Editor Tool`) with hex selection, validation, and auto-fix
- `MapValidator` and `MapValidationResult` for comprehensive board validation
- `HexReplacementPipeline` with `HarbourReplacementRule` for rule-based hex placement
- `HexStateRepairer` for fixing corrupted/legacy map data on load
- `LegacyMapConverter` for converting older save formats
- `ActionLogger` and `ActionLogSettings` for structured logging
- Undo/Redo system using `HexSnapshotService` with byte-array snapshots
- `HexPlacementRuleEngine` with delegate-based adjacency checks
- Unity EditMode tests: `HarbourPlacementSnapshotTests`, `HexSnapshotServiceTests`, `MapEditorUndoTests`, `MapLifecycleTests`
- Pre-push git hooks with `install-git-hooks.ps1`
- Cross-platform Pester tests for build scripts

### Changed
- `GameSpawner.LoadState` now handles both current and legacy JSON formats
- `HexSpawner` integrates with `HexReplacementPipeline` for harbour placement
- CI workflow expanded with changed-only build guard for PRs

---

## [v0.1.0] — 2025-11-01 — Core Board

### Added
- Hexagonal grid generation with configurable dimensions
- `CoreLogic` standalone .NET Standard 2.0 library (grid math, serialization)
- `CoreLogicAdapter` for Unity ↔ CoreLogic type conversions
- Save/Load board state to JSON
- Hex type system (forest, pasture, field, hill, mountain, sea, desert, harbour, gold)
- Number token assignment with probability-based colouring
- Odin Inspector integration for editor tooling
- GitHub Actions CI with CoreLogic build + test
- `docs/` documentation suite (Architecture, Testing, Dev Setup, Contributing)
