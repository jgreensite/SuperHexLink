# Product Backlog

> Structured as **Epics → Features → Stories**. Each story is sized so a junior engineer can complete it in 1–3 days. Acceptance criteria are explicit and testable.

**Last updated**: February 2026
**Current milestone**: `v0.3.0` — Senior Code Review Uplift

---

## How to use this backlog

1. Pick a story from the **current milestone** that is `[ ]` (not started).
2. Create a feature branch: `git checkout -b feature/<story-slug>`.
3. Implement, write/update tests, update docs.
4. Open a PR referencing the story ID (e.g. `Closes #E1-F2-S3`).
5. Tick the checkbox when merged.

---

## Epic 1: Board State Persistence & Integrity

> **Goal**: A player can save a board, close the game, reopen it, load the board, and see an identical result every time — including edges and corners.

### Feature 1.1: Hex Save/Load (✅ Complete)

- [x] **E1-F1-S1** Extract CoreLogic library with grid math and serialization
- [x] **E1-F1-S2** Implement backing state pattern in `Hex.cs` (`BaseHexState` ↔ `HexState`)
- [x] **E1-F1-S3** Create `CoreLogicAdapter` for Unity ↔ CoreLogic type conversion
- [x] **E1-F1-S4** Add unit tests for hex serialization round-trips
- [x] **E1-F1-S5** Add `HexStateRepairer` to fix corrupted/legacy data on load
- [x] **E1-F1-S6** Add `LegacyMapConverter` with defensive `int.TryParse` parsing

### Feature 1.2: Edge & Corner Reconstruction

- [ ] **E1-F2-S1** Implement `EdgeSpawner.BuildMe` — instantiate edge prefabs from saved `EdgeSpawnerState`
  - **AC**: After load, edges appear between hexes matching the saved state.
  - **Files**: `Assets/Scripts/EdgeSpawner.cs`
  - **Tests**: Add `EdgeSpawnerStateTests.cs` in `Assets/Tests/Editor/`

- [ ] **E1-F2-S2** Implement `CornerSpawner.BuildMe` — instantiate corner prefabs from saved `CornerSpawnerState`
  - **AC**: After load, corners (settlements/cities) appear at vertices matching the saved state.
  - **Files**: `Assets/Scripts/CornerSpawner.cs`
  - **Tests**: Add `CornerSpawnerStateTests.cs` in `Assets/Tests/Editor/`

- [x] **E1-F2-S3** End-to-end save/load validation test
  - **AC**: Generate board → Save → Clear → Load → Assert hex/edge/corner counts and types match.
  - **Files**: `Assets/Tests/Editor/SaveLoadRoundTripTests.cs`

### Feature 1.3: Deterministic Board Recreation

- [ ] **E1-F3-S1** Hash-based board comparison utility
  - **AC**: `BoardHasher.ComputeHash(state)` returns a deterministic hash. Same board → same hash.
  - **Files**: `Assets/Scripts/Utils/BoardHasher.cs`, test in `Assets/Tests/Editor/`

- [ ] **E1-F3-S2** Deterministic recreation test: Load → Save → Load → Compare hashes
  - **AC**: Two consecutive load/save cycles produce identical hashes.

---

## Epic 2: CI/CD & Developer Experience

> **Goal**: Every push is validated automatically. A new contributor can clone, build, and run tests in under 15 minutes.

### Feature 2.1: GitHub Actions Pipeline (✅ Mostly Complete)

- [x] **E2-F1-S1** CoreLogic build + test job on GitHub-hosted runners
- [x] **E2-F1-S2** `.csproj` build guard (full + changed-only for PRs)
- [x] **E2-F1-S3** Pester unit tests for build scripts (cross-platform matrix)
- [x] **E2-F1-S4** Pre-push git hook installer
- [ ] **E2-F1-S5** Activate Unity license on self-hosted runner for EditMode tests
  - **AC**: `unity-selfhost-tests.yml` runs EditMode tests and uploads results.
  - **Blocker**: Requires Unity license file on the runner machine.

- [ ] **E2-F1-S6** Add code coverage reporting for CoreLogic (coverlet + ReportGenerator)
  - **AC**: Coverage report uploaded as artifact on each PR. Target: 80%+.
  - **Files**: Update `CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj`, `.github/workflows/ci.yml`

### Feature 2.2: Developer Onboarding

- [x] **E2-F2-S1** `docs/DEV_SETUP.md` — environment setup guide
- [x] **E2-F2-S2** `docs/TESTING.md` — how to run tests locally and in CI
- [x] **E2-F2-S3** `CONTRIBUTING.md` — branching strategy, PR process, hooks
- [x] **E2-F2-S4** Add GitHub Issue templates (bug report, feature request, story)
  - **AC**: `gh issue create` prompts with a structured template.
  - **Files**: `.github/ISSUE_TEMPLATE/`

- [x] **E2-F2-S5** Add PR template with checklist
  - **AC**: Every new PR auto-populates a checklist (tests, docs, no warnings).
  - **Files**: `.github/PULL_REQUEST_TEMPLATE.md`

### Feature 2.3: Repository Hygiene

- [x] **E2-F3-S1** `.editorconfig` with C# naming, braces, var preferences
- [x] **E2-F3-S2** Add `CODEOWNERS` file for auto-review assignment
  - **AC**: PRs touching `Assets/Scripts/` auto-request review from the core team.
  - **Files**: `.github/CODEOWNERS`

- [x] **E2-F3-S3** Clean `.gitignore` — exclude `pester-results.xml`, `TestResults/`, `actions-runner/`
  - **AC**: `git status` is clean after a fresh test run.

---

## Epic 3: Spawner Architecture Refactor

> **Goal**: Replace tight coupling (`GameObject.Find`) with explicit dependency injection. Split HexSpawner's mixed responsibilities into focused services.

### Feature 3.1: Dependency Injection for Spawners

- [ ] **E3-F1-S1** Replace `GameObject.Find` in `GameSpawner.Awake` with `[SerializeField]` references
  - **AC**: `GameSpawner` works without any `GameObject.Find` calls. All references set in the prefab.
  - **Files**: `Assets/Scripts/GameSpawner.cs`
  - **Tests**: Verify in existing `MapLifecycleTests.cs`

- [ ] **E3-F1-S2** Create `ISpawner` interface and have all spawners implement it
  - **AC**: `GameSpawner` iterates over `IEnumerable<ISpawner>` instead of hard-coded fields.
  - **Files**: `Assets/Scripts/ISpawner.cs`, update `SpawnerBase.cs`

### Feature 3.2: HexSpawner Responsibility Split

- [ ] **E3-F2-S1** Extract `HexFactory` — hex prefab instantiation only
  - **AC**: `HexFactory.Create(col, row, type)` returns a configured `Hex` GameObject.
  - **Files**: `Assets/Scripts/Services/HexFactory.cs`

- [ ] **E3-F2-S2** Extract `HexRegistry` — tracking all live hex references
  - **AC**: `HexRegistry.GetHex(col, row)` returns the `Hex` at that position. O(1) lookup.
  - **Files**: `Assets/Scripts/Services/HexRegistry.cs`

- [ ] **E3-F2-S3** Extract `SelectionManager` — hex selection/deselection logic
  - **AC**: `SelectionManager.Select(hex)` / `.DeselectAll()` replace inline selection code.
  - **Files**: `Assets/Scripts/Services/SelectionManager.cs`, update `SelectLand.cs`

### Feature 3.3: Save/Load Service

- [ ] **E3-F3-S1** Create `SaveLoadService` to centralize persistence
  - **AC**: Single entry point for save/load. `GameSpawner` delegates to this service.
  - **Files**: `Assets/Scripts/Services/SaveLoadService.cs`

- [ ] **E3-F3-S2** Add async save with progress callback
  - **AC**: Large maps don't block the main thread during save. Progress bar in editor.
  - **Files**: Update `SaveLoadService.cs`

---

## Epic 4: Core Gameplay Mechanics

> **Goal**: Implement the actual game rules — resource production, building placement, trading, turn management.

### Feature 4.1: Turn Management

- [ ] **E4-F1-S1** Create `TurnManager` with player rotation and phase tracking
  - **AC**: `TurnManager.CurrentPlayer`, `.NextTurn()`, `.CurrentPhase` (Roll/Build/Trade/End).
  - **Files**: `Assets/Scripts/Gameplay/TurnManager.cs`
  - **Tests**: `Assets/Tests/Editor/TurnManagerTests.cs`

- [ ] **E4-F1-S2** Add dice roll mechanic with `Rng` integration
  - **AC**: `DiceRoller.Roll()` returns 2–12 with correct probability distribution.
  - **Tests**: Statistical test — 10,000 rolls should approximate expected distribution.

### Feature 4.2: Resource Production

- [ ] **E4-F2-S1** Create `ResourceManager` — tracks resources per player
  - **AC**: After a dice roll, hexes with matching numbers produce resources for adjacent settlements.
  - **Files**: `Assets/Scripts/Gameplay/ResourceManager.cs`

- [ ] **E4-F2-S2** Create `ProductionEngine` — resolves which hexes produce on a given roll
  - **AC**: Given roll=8, returns list of `(hex, player, resourceType)` tuples.
  - **Tests**: Unit test with known board layout.

### Feature 4.3: Building Placement

- [ ] **E4-F3-S1** Settlement placement rules — must be on empty corner, 2+ edges from other settlements
  - **AC**: `PlacementValidator.CanPlaceSettlement(corner)` returns true/false with reason.
  - **Tests**: Test adjacency rule, test on occupied corner, test on valid corner.

- [ ] **E4-F3-S2** Road placement rules — must extend from own settlement or road
  - **AC**: `PlacementValidator.CanPlaceRoad(edge)` returns true/false with reason.

- [ ] **E4-F3-S3** City upgrade — replace settlement with city on same corner
  - **AC**: `PlacementValidator.CanUpgradeToCity(corner)` checks ownership and resource cost.

### Feature 4.4: Win Condition

- [ ] **E4-F4-S1** Victory point tracking
  - **AC**: Settlements=1VP, Cities=2VP, longest road bonus, largest army bonus.
  - **Tests**: Known board state → expected VP count.

- [ ] **E4-F4-S2** Win detection — first to 10 VP
  - **AC**: `VictoryChecker.Check()` returns winning player or null.

---

## Epic 5: Multiplayer Networking

> **Goal**: Two or more players can play on separate machines with synchronized board state.

### Feature 5.1: Deterministic Simulation

- [ ] **E5-F1-S1** Seed-based RNG for reproducible games
  - **AC**: Same seed + same actions → identical board state on all clients.
  - **Files**: Update `Rng.cs` to accept seed parameter.

- [ ] **E5-F1-S2** Action log serialization (command pattern)
  - **AC**: All player actions are serializable. Replay from action log reproduces exact game state.

### Feature 5.2: State Synchronization

- [ ] **E5-F2-S1** Client-server architecture with authoritative server
  - **AC**: Server validates actions, broadcasts state updates. Clients cannot cheat.

- [ ] **E5-F2-S2** Lobby and matchmaking
  - **AC**: Players can create/join a game lobby. Game starts when all players ready.

---

## Epic 6: Performance & Polish

> **Goal**: The game runs at 60fps on mid-range hardware with 100+ hex boards.

### Feature 6.1: Object Pooling

- [ ] **E6-F1-S1** Implement hex object pool — reuse GameObjects instead of Instantiate/Destroy
  - **AC**: Board regeneration reuses existing hex objects. No GC spikes during rebuild.
  - **Files**: `Assets/Scripts/Services/HexPool.cs`

### Feature 6.2: Rendering Optimization

- [ ] **E6-F2-S1** Add frustum culling for off-screen hexes
- [ ] **E6-F2-S2** LOD system for distant hexes (simplified mesh)
- [ ] **E6-F2-S3** GPU instancing for identical hex types

---

## Epic 7: Security, Performance & Onboarding

> **Goal**: The repo has documented security practices, performance budgets, and a frictionless onboarding experience for new contributors.

### Feature 7.1: Security Documentation & Hardening (✅ Complete)

- [x] **E7-F1-S1** Create `docs/SECURITY.md` — threat model, trust boundaries, input validation patterns
- [x] **E7-F1-S2** Harden `GameSpawner.LoadState` — path traversal guard, file size limit, schema validation
- [x] **E7-F1-S3** Add `.github/dependabot.yml` for automated NuGet + GitHub Actions dependency scanning

### Feature 7.2: Performance Documentation (✅ Complete)

- [x] **E7-F2-S1** Create `docs/PERFORMANCE.md` — budgets, profiling guide, known hotspots, optimization patterns
- [ ] **E7-F2-S2** Add performance regression tests — measure spawn time for 7×7 and 19×19 grids
  - **AC**: Test fails if spawn time exceeds budget from PERFORMANCE.md.
  - **Files**: `Assets/Tests/Editor/PerformanceRegressionTests.cs`

### Feature 7.3: Developer Onboarding (✅ Complete)

- [x] **E7-F3-S1** Create `docs/ONBOARDING.md` — day-1 guide, exercises, cheat sheet
- [x] **E7-F3-S2** Consolidate root `CONTRIBUTING.md` with `docs/CONTRIBUTING.md`
- [x] **E7-F3-S3** Add `LICENSE` file (MIT)

### Feature 7.4: Architecture Decision Records (✅ Complete)

- [x] **E7-F4-S1** ADR-0001: CoreLogic library separation
- [x] **E7-F4-S2** ADR-0002: Backing state pattern
- [x] **E7-F4-S3** ADR-0003: Snapshot undo system
- [x] **E7-F4-S4** ADR-0004: Structured logging
- [ ] **E7-F4-S5** ADR-0005: Save file format versioning (when implemented)
  - **AC**: ADR documents chosen versioning scheme and migration strategy.

### Feature 7.5: Scaling & Future Architecture

- [ ] **E7-F5-S1** Create `docs/SCALING.md` — ECS migration path, async patterns, spatial indexing
  - **AC**: Document covers scaling from 7×7 to 100×100 with concrete patterns.
- [ ] **E7-F5-S2** Prototype spatial hash index for O(1) hex lookup by world position
  - **AC**: `SpatialIndex.GetHexAt(worldPos)` returns hex in O(1). Unit test with 10,000 hexes.
  - **Files**: `CoreLogic/src/CoreLogic/Grid/SpatialIndex.cs`

---

## Milestone Tracking

| Milestone | Target | Status |
|-----------|--------|--------|
| `v0.1.0` — Core Board | Done | ✅ Hex generation, save/load, map editor |
| `v0.2.0` — Map Editor v2 | Done | ✅ Validation, auto-repair, undo/redo |
| `v0.3.0` — Senior Uplift | Done | ✅ Code quality, CI, docs, security, perf, onboarding |
| `v0.4.0` — Edge & Corner | Planned | ⬜ E1-F2, E3-F2 |
| `v0.5.0` — Gameplay MVP | Planned | ⬜ E4 (turns, resources, building) |
| `v1.0.0` — Multiplayer | Planned | ⬜ E5 |
