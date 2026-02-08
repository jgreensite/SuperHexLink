# TODO — Quick Reference

> **For the full product roadmap** (Epics, Features, Stories), see [`BACKLOG.md`](../BACKLOG.md).
> This file tracks immediate tactical work items and known technical debt.

**Last Updated**: February 2026

---

## Recently Completed (v0.3.0 Uplift)

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

## Immediate Next Steps

See `BACKLOG.md` for the detailed breakdown. Summary:

| Priority | Item | Backlog ID |
|----------|------|------------|
| **High** | Implement EdgeSpawner.BuildMe from saved state | `E1-F2-S1` |
| **High** | Implement CornerSpawner.BuildMe from saved state | `E1-F2-S2` |
| **High** | End-to-end save/load round-trip test | `E1-F2-S3` |
| **High** | Activate Unity license on CI runner | `E2-F1-S5` |
| **Medium** | Replace `GameObject.Find` in GameSpawner | `E3-F1-S1` |
| **Medium** | Add code coverage reporting | `E2-F1-S6` |

## Known Technical Debt

| Severity | Issue | Backlog Ref |
|----------|-------|-------------|
| High | `GameObject.Find` coupling in `GameSpawner` | `E3-F1-S1` |
| High | Mixed responsibilities in `HexSpawner` (factory + registry + selection) | `E3-F2` |
| Medium | No object pooling — `Instantiate`/`Destroy` causes GC pressure | `E6-F1-S1` |
| Medium | Synchronous JSON save on main thread | `E3-F3-S2` |
| Medium | Hardcoded server address/port in `GameConstants` | — |
| Low | `GameUIManager` should use UQuery for element selection | — |
| Low | `HexLandModel` contains leftover old hex model code | — |

## Testing Gaps

| Area | What's Missing | Backlog Ref |
|------|----------------|-------------|
| CoreLogic | Distance calculations, neighbor edge cases | — |
| Unity | EdgeSpawner/CornerSpawner state reconstruction | `E1-F2` |
| Unity | Deterministic board recreation (hash comparison) | `E1-F3` |
| E2E | Generate → Save → Clear → Load → Verify identical | `E1-F2-S3` |
| Perf | Load 1000+ hex board benchmark | — |

---

**Next review**: After Edge & Corner reconstruction is complete (v0.4.0).
