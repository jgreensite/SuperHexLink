# TODO — Quick Reference

> **For the full product roadmap** (Epics, Features, Stories), see [`BACKLOG.md`](../BACKLOG.md).
> This file tracks immediate tactical work items and known technical debt.

**Last Updated**: May 2026

---

## Recently Completed (v0.4.0 Gameplay MVP)

- [x] `EdgeSpawner.BuildMe` — implemented with Clear/Refresh logic (`E1-F2-S1`)
- [x] `CornerSpawner.BuildMe` — implemented with Clear/Refresh logic (`E1-F2-S2`)
- [x] `BoardHasher` — deterministic board state hash utility (`E1-F3-S1`)
- [x] `GameSpawner` — replaced all `GameObject.Find` with `[SerializeField]` (`E3-F1-S1`)
- [x] `ISpawner` interface + all spawners implement it; `AllSpawners` as `IEnumerable<ISpawner>` (`E3-F1-S2`)
- [x] `HexFactory`, `HexRegistry`, `SelectionManager` — extracted from `HexSpawner` (`E3-F2`)
- [x] `SaveLoadService` — centralized persistence service (`E3-F3-S1`)
- [x] `TurnManager`, `DiceRoller` — turn management and dice mechanic (`E4-F1`)
- [x] `ResourceManager`, `ProductionEngine` — resource production on dice roll (`E4-F2`)
- [x] `PlacementValidator` — settlement, road, city placement rules (`E4-F3`)
- [x] `VictoryChecker` — 10 VP win detection (`E4-F4`)
- [x] `RulesEngine`, `RuleConflictResolver`, `RuleTimingSystem`, `GameOntology`, `EconomicBalancer` (`E8-F1`)
- [x] `HeadlessGameState`, `IGameAction` — Monte Carlo simulation framework (`E9-F1`)
- [x] Tests: `DiceRollerTests`, `HexFactoryTests`, `HexRegistryTests`, `ISpawnerTests`, `ResourceManagerTests`, `RuleConflictResolverTests`, `RulesEngineTests`, `SaveLoadServiceTests`, `SelectionManagerTests`, `TurnManagerTests`

- [x] `GameConstants` — `static` → `const`, `materialMap` → `MaterialMap`, removed empty `Start()`
- [x] `SpawnerBase` — promoted `IsConfiguredEmpty` to base, removed unused imports
- [x] `HexSpawner` — PascalCase methods, structured logging, removed commented-out code, restored `EnsureValidGameSpawnerState`
- [x] `GameSpawner` — replaced ~40 `Debug.Log` calls with `ActionLogger`, XML docs
- [x] `Hex` — fixed indentation, consolidated `NotSelect()` → `Deselect()`, XML docs
- [x] `Corner` — removed constructor (MonoBehaviour), added `Init()`, removed empty lifecycle methods
- [x] `EdgeSpawner` / `CornerSpawner` — removed commented-out code, cleaned imports
- [x] `HexGrid` — `[Serializable]`, XML docs, robust `IsInBounds`
- [x] `Helpers` — `MonoBehaviour` → `static class`, fixed typo, `StringComparison.OrdinalIgnoreCase`
- [x] `Extensions` — fixed `Veertex0` → `Vertex0`
- [x] `Rng` — replaced deprecated `RNGCryptoServiceProvider`
- [x] `LegacyMapConverter` — `int.Parse` → `int.TryParse` for defensive parsing
- [x] `MapValidationWindow` — removed dead code, fixed orphaned block
- [x] `SelectLand` — updated `NotSelect()` → `Deselect()` references
- [x] `.editorconfig` — added C# naming, var, brace conventions
- [x] `.gitignore` — deduplicated, added `pester-results.xml`
- [x] Removed `test-log.txt`
- [x] Added `BACKLOG.md`, `CHANGELOG.md`, issue/PR templates, `CODEOWNERS`
- [x] Rewrote `README.md` with badges, architecture diagram, tables
- [x] `docs/SECURITY.md` — threat model, input validation, secrets management
- [x] `docs/PERFORMANCE.md` — budgets, profiling guide, optimization patterns
- [x] `docs/ONBOARDING.md` — day-1 guide, exercises, cheat sheet
- [x] `docs/adr/` — 4 Architecture Decision Records
- [x] `HexGrid.Distance()` + 7 distance tests + 10 boundary tests (27 CoreLogic tests total)
- [x] `.github/dependabot.yml` — NuGet + GitHub Actions scanning
- [x] `LICENSE` (MIT) + badge in README
- [x] `global.json` — pinned .NET SDK 8.0.x
- [x] Consolidated `CONTRIBUTING.md` with `docs/CONTRIBUTING.md`
- [x] Removed `Prompts/` dev scratch from tracking
- [x] Fixed GameConstants.cs lint warnings + hardcoded IP warning comment

## Immediate Next Steps

See `BACKLOG.md` and `ROADMAP.md` for the full breakdown. Summary of what's actually missing:

| Priority | Item | Backlog ID |
|----------|------|------------|
| **Critical** | Add MoonSharp NuGet + wire real Lua in `RulesEngine.ExecuteScriptInternal` | `E8-F2-S1` |
| **Critical** | Create `UnityGameStateBridge` (`ExtractFromScene` / `ApplyToScene`) | `E9-F2-S1` |
| **High** | Add 5 missing test files: `RuleTimingSystemTests`, `GameOntologyTests`, `EconomicBalancerTests`, `HeadlessGameStateTests`, `IGameActionTests` | `E9-F3` |
| **High** | Activate Unity Personal license (`.ulf`) and add to GitHub secrets for GameCI | `E2-F1-S5` |
| **Medium** | Add code coverage reporting for CoreLogic (coverlet + ReportGenerator) | `E2-F1-S6` |
| **Low** | Async save with progress callback | `E3-F3-S2` |

## Known Technical Debt

| Severity | Issue | Backlog Ref |
|----------|-------|-------------|
| High | `RulesEngine.ExecuteScriptInternal` is a stub — MoonSharp not yet wired | `E8-F2-S1` |
| High | `UnityGameStateBridge` not yet created — Unity scene ↔ HeadlessGameState roundtrip missing | `E9-F2-S1` |
| High | 5 test files missing: `RuleTimingSystem`, `GameOntology`, `EconomicBalancer`, `HeadlessGameState`, `IGameAction` | `E9-F3` |
| High | Unity EditMode CI not running — no `.ulf` license secret in GitHub | `E2-F1-S5` |
| Medium | No object pooling — `Instantiate`/`Destroy` causes GC pressure | `E6-F1-S1` |
| Medium | Synchronous JSON save on main thread | `E3-F3-S2` |
| Low | `GameUIManager` should use UQuery for element selection | — |
| Low | `HexLandModel` contains leftover old hex model code | — |

## Testing Gaps

| Area | What's Missing | Backlog Ref |
|------|----------------|-------------|
| Rules | `RuleTimingSystemTests.cs` | `E9-F3-S1` |
| Rules | `GameOntologyTests.cs` | `E9-F3-S2` |
| Rules | `EconomicBalancerTests.cs` | `E9-F3-S3` |
| Simulation | `HeadlessGameStateTests.cs` | `E9-F3-S4` |
| Simulation | `IGameActionTests.cs` | `E9-F3-S5` |
| Rules (integration) | `RulesEngineIntegrationTests.cs` — real Lua execution via MoonSharp | `E8-F2-S1` |
| Integration | `UnityIntegrationTests.cs` — round-trip via `UnityGameStateBridge` | `E9-F2-S1` |

---

**Next review**: After MoonSharp + UnityGameStateBridge complete (v0.5.0).

> **v0.4.0 Gameplay MVP is complete.** See `BACKLOG.md` Epics 1–4, 8-F1, 9-F1 for the full list of delivered work.
