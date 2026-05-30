# SuperHexLink — Roadmap

> **Single source of truth** for what gets built next.
> See [`BACKLOG.md`](../BACKLOG.md) for full Epic/Story detail and acceptance criteria.
> See [`TODO.md`](TODO.md) for immediate tactical work items and tech debt.

**Last Updated**: May 2026

---

## Current State

| Layer | Status |
|-------|--------|
| CoreLogic (grid, serialization) | ✅ Complete + tested |
| Board persistence (save/load round-trips) | ✅ Complete + tested |
| Spawner architecture (ISpawner, services, DI) | ✅ Complete + tested |
| Gameplay mechanics (turns, dice, resources, placement, VP) | ✅ Complete + tested |
| Rules Engine (core + conflict resolution + ontology) | ✅ Core done; **Lua execution is a stub** |
| Headless simulation (HeadlessGameState, IGameAction) | ✅ Complete; **missing tests** |
| GameCI / Unity EditMode CI | 🔄 Blocked on `.ulf` license secret |
| MoonSharp Lua integration | ❌ Not started |
| Unity ↔ CoreLogic bridge | ❌ Not started |

---

## v0.5.0 — Next Release

**Theme**: Wire the engine — connect Lua, bridge Unity↔CoreLogic, close test gaps.

### Priority 1 — MoonSharp Lua Integration (`E8-F2`)

The `RulesEngine.ExecuteScriptInternal` method is a documented stub. Real Lua execution must be wired.

**Tasks** (agent executes):
1. Add `<PackageReference Include="MoonSharp" Version="2.0.0" />` to `CoreLogic/src/CoreLogic/CoreLogic.csproj`
2. Replace stub in `Assets/Scripts/Rules/RulesEngine.cs` with `MoonSharp.Interpreter.Script.RunString()`
3. Add `RulesEngineIntegrationTests.cs` — integration test with real Lua (`return 2+2` → 4)
4. Verify: `dotnet test CoreLogic/tests/CoreLogic.Tests/CoreLogic.Tests.csproj`

**Definition of Done**: Integration test proves real Lua execution. No Unity license needed.

---

### Priority 2 — Missing Test Files (`E9-F3`)

Five classes have no test coverage at all.

**Tasks** (agent executes):
1. `Assets/Tests/Editor/RuleTimingSystemTests.cs`
2. `Assets/Tests/Editor/GameOntologyTests.cs`
3. `Assets/Tests/Editor/EconomicBalancerTests.cs`
4. `Assets/Tests/Editor/HeadlessGameStateTests.cs`
5. `Assets/Tests/Editor/IGameActionTests.cs`

**Definition of Done**: All 5 test files created, all tests pass in `dotnet test` (CoreLogic) or Unity EditMode.

---

### Priority 3 — Unity↔CoreLogic Bridge (`E9-F2`)

Without this bridge, the headless simulation is disconnected from the live Unity scene.

**Tasks** (agent executes):
1. Create `Assets/Scripts/Integration/UnityGameStateBridge.cs`
   - `ExtractFromScene(GameSpawner spawner) → HeadlessGameState`
   - `ApplyToScene(HeadlessGameState state, GameSpawner spawner)`
2. Add `Assets/Tests/Editor/UnityIntegrationTests.cs`

**Definition of Done**: Round-trip test passes — extract scene → modify state → apply → verify board matches.

---

### Priority 4 — GameCI Unity CI (`E2-F1-S5`)

**Blocked on user action**: needs `.ulf` license file from Unity Hub.

**User must do**:
```powershell
# Run once with real credentials — creates C:\ProgramData\Unity\Unity_lic.ulf
& "C:\Program Files\Unity\Hub\Editor\2021.3.18f1\Editor\Unity.exe" -batchmode -nographics -quit -username "YOUR_EMAIL" -password "YOUR_PASSWORD"
```

Then add GitHub secrets:
- `UNITY_LICENSE` — full contents of `C:\ProgramData\Unity\Unity_lic.ulf`
- `UNITY_EMAIL` — Unity account email
- `UNITY_PASSWORD` — Unity account password

**Agent then**:
1. Add `unity-test` job to `.github/workflows/ci.yml` using `game-ci/unity-test-runner@v4`
2. Add `unity-build-android` and `unity-build-windows` jobs using `game-ci/unity-builder@v4` (on `v*` tags only)
3. Upload build artifacts to GitHub Releases

---

## v1.0.0 — Multiplayer

**Theme**: Two players on separate machines with synchronized board state.

- Seed-based deterministic RNG (`E5-F1-S1`)
- Action log serialization (`E5-F1-S2`)
- Client-server with authoritative server (`E5-F2-S1`)
- Lobby and matchmaking (`E5-F2-S2`)

---

## Deferred / Future

| Item | Reason |
|------|--------|
| Object pooling (`E6-F1`) | Premature until profiling shows GC spikes |
| Frustum culling / LOD (`E6-F2`) | Premature until frame rate target is missed |
| Async save (`E3-F3-S2`) | Low priority; board is small |
| Performance regression tests (`E7-F2-S2`) | After CI is green |
| ADR-0005: Save file versioning (`E7-F4-S5`) | After save format is stable |
| `docs/SCALING.md` (`E7-F5-S1`) | Informational; no blocker |
