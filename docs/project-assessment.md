# SuperHexLink Repository Assessment (Historical)

**Note**: This document is archived for historical reference. See [ARCHITECTURE.md](ARCHITECTURE.md) for current architecture and [TODO.md](TODO.md) for current tasks.

---

# Original Assessment - SuperHexLink Repository

## Overview
SuperHexLink is a Unity-based hex–grid strategy prototype. The repository mixes runtime gameplay scripts in `Assets/Scripts/`, third-party tooling under `Assets/ParadoxNotion/` and `Assets/Plugins/`, and various serialization helpers within `data/`. The project targets Unity `2021.3.18f1` per `ProjectSettings/ProjectVersion.txt`. The assessment below captures the current state of core systems (spawning, persistence, messaging), documents the actions completed so far, and enumerates the detailed roadmap required to stabilize the project on the new workstation.

## Codebase Assessment

### Board Generation & Spawners (`Assets/Scripts/GameSpawner.cs`, `Assets/Scripts/HexSpawner.cs`, `Assets/Scripts/EdgeSpawner.cs`, `Assets/Scripts/CornerSpawner.cs`)
- **Central coordination in `GameSpawner`** relies on `GameObject.Find`/`FindObjectsOfType`, tightly coupling scene lookup to object names without null safeguarding. This hinders deterministic instantiation during save/load, especially when spawner hierarchies are rebuilt from serialized state.
- **`HexSpawner` responsibilities** span random board creation, state persistence, and runtime refresh logic. It mutates shared state (`hexSpawnerState.hexes`) while simultaneously depending on scene queries (`FindObjectsOfType<Hex>()`) for ordering, making serialized collections diverge from actual scene object ordering.
- **`EdgeSpawner`/`CornerSpawner` placeholders** currently return early or leave stubbed methods. As a result, saved states never rebuild edge or corner entities, and adjacency metadata stored inside hex states points to nonexistent objects.
- **Camera & selection integration** inside `GameSpawner.AssociateElements()`/`AdjustCameraPosition()` assumes spawned objects already exist. Missing edges/corners and asynchronous rebuilds lead to unmatched references and `NullReferenceException`s during refresh.

### Hex Runtime Model (`Assets/Scripts/Hex.cs`)
- Prior to intervention, `Hex.HexState` exposed `Col`/`Row` setters that re-indexed into `hexSpawnerState.hexes[Col][Row]`, producing recursive property access and non-deterministic lookups during deserialization.
- `Hex.Initialize(HexSpawner hs)` created a new `HexState` with default `GroupID`, `Col`, and `Row`, disregarding any existing serialized backing data, resetting metadata whenever `BuildMe()` executes.
- `HexState` maintained dictionaries (`Edges`, `Corners`, `OriginalMaterialColors`) without binding them to serialized DTOs, causing runtime-only data to leak and making deterministic save/load impossible.

### Persistence Layer (`data/` JSON helpers, Odin serialization usage)
- Save routines scatter state across multiple JSON files, lack directory existence checks, and rely on synchronous IO on the main thread. Failures during write leave partially updated files with no rollback.
- The load pipeline re-randomizes the board before applying saved data, ensuring mismatches when comparing snapshots.
- No versioning or schema documentation exists, making forward compatibility fragile.

### Messaging & Event Flow (`Assets/Scripts/GameConstants.cs`, NodeCanvas assets)
- The project mixes a custom `GameConstants` message enumeration with NodeCanvas behavior trees and Odin-driven inspector automation. No consolidated event bus exists, and message names collide between gameplay and UI logic.
- Several scripts read constants that are never raised, implying incomplete migration from an earlier custom messaging system to NodeCanvas.

### Tooling, Tests, and CI
- Only lightweight math/unit helper tests live in `Tests.cs`; there are no Unity Test Runner assemblies (`Assets/Tests/EditMode`, `Assets/Tests/PlayMode`).
- Continuous integration is absent. There are no pipelines verifying builds, tests, or serialization compatibility.
- The repository lacks onboarding documentation describing required Unity modules, command-line invocations, or manual installation paths.

## Work Completed to Date

### Hex Runtime Refactor
- **Updated `Hex.Initialize()`** to accept both a `HexSpawner` and a serialized `Hex.BaseHexState`. The constructor now binds the runtime `HexState` to immutable backing data rather than inventing defaults.
- **Refactored `Hex.HexState`**:
  - Added a `backingState` reference storing persisted fields (`Col`, `Row`, `HexType`, etc.).
  - Redirected property accessors to manipulate `backingState` values, eliminating recursive state lookups and enabling deterministic serialization/deserialization.
  - Seeded `PositionDataHex` by deriving axial coordinates from the persisted `Col` and `Row` on construction.
  - Initialized `OriginalMaterialColors`, `Edges`, and `Corners` dictionaries explicitly without touching runtime-only state in `hexSpawnerState`.
- These changes live entirely within `Assets/Scripts/Hex.cs` and provide the basis for deterministic board reconstruction.

### Unity Environment Investigation (Superseded by Manual Install)
- Documented Ubuntu prerequisites (Mesa, GTK, AppIndicator, portal packages) and multiple iterations attempting Unity Hub GUI/headless login.
- Established manual installation instructions culminating in downloading `Unity-2021.3.18f1.tar.xz` and extracting to `~/Unity/Editor/2021.3.18f1/`. Although Hub authentication failed, the editor binaries are ready for license activation on the alternate machine.

## Pending Work & Detailed Roadmap

### 1. Stabilize Data & Serialization Model
- **Finalize DTO definitions** within `HexSpawner.HexSpawnerState`, ensuring each hex entry stores immutable identifiers (column, row, GUID) and references to edge/corner data via explicit keys, not scene indices.
- **Introduce schema versioning** (e.g., `int Version` field) and migration utilities housed under `Assets/Scripts/Persistence/` to evolve save files safely.
- **Refactor state containers** so runtime components (`Hex`, `Edge`, `Corner`) consume immutable DTO snapshots, with mutation funneled through dedicated services (e.g., `BoardStateService`).

### 2. Rebuild Spawner Pipelines
- **`HexSpawner.BuildMe(bool fromState)`** should instantiate hex prefabs deterministically from serialized DTOs, reusing or pooling objects instead of destroying/recreating randomly.
- **Implement `EdgeSpawner` / `CornerSpawner`** to materialize edges and corners based on hex adjacency rules (using helpers in `HexExtensions.HexExtensions`). Saved state must reconstruct these objects exactly once and link them back to owning hexes.
- **Camera/selection assignments** need to be hooked after all spawners complete to avoid referencing null lists.

### 3. Persistence & Save/Load Workflow
- **Centralize persistence** in a new service (`SaveLoadService.cs` under `Assets/Scripts/Services/`):
  - Validate directories, use atomic file writes (temporary file + rename), and support async save operations.
  - Provide `LoadBoard(string path)` variant returning DTOs without mutating the scene, enabling pre-validation before applying to spawners.
- **Add snapshot comparisons** to test deterministic reloads (hashes of serialized DTO arrays).

### 4. Messaging Architecture Cleanup
- **Inventory all message usages** across gameplay/UI scripts, NodeCanvas graphs, and Odin attributes.
- **Adopt a single event system**, e.g., ScriptableObject channels or C# events behind an `EventBus` service, deprecating unused constants in `GameConstants.cs`.
- **Document integration** for external systems (multiplayer, UI) so additions follow the same mechanism.

### 5. Testing & Tooling
- **Create Unity Test Runner assemblies**:
  - `Assets/Tests/EditMode/EditModeTests.asmdef` targeting `Assembly-CSharp` for serialization/messaging unit tests.
  - `Assets/Tests/PlayMode/PlayModeTests.asmdef` for spawn/load/playback verification.
- **Author critical tests**:
  - Hex serialization round-trip ensuring property parity between DTO and runtime `HexState`.
  - End-to-end play mode scenario: spawn → save → clear scene → load → verify board equality.
- **Introduce CI** (GitHub Actions or equivalent) to run headless edit/play mode tests using Unity’s command-line runner.

### 6. Documentation & Onboarding
- **Produce developer setup guide** covering manual editor installation, license activation via `-noHub`, and instructions for the alternate machine to replicate the environment.
- **Add architecture notes** to `docs/` describing the refactored flow for spawners, persistence, and messaging once implemented.

## Immediate Next Steps on the Alternate Machine

1. **Activate Unity Editor** using the newly installed binaries (`~/Unity/Editor/2021.3.18f1/Editor/Unity -noHub`). Sign in online (Hub is no longer required once external browser login succeeds) and export the license (`Unity_lic.ulf`) for archival.
2. **Sync repository** and verify the `Hex.cs` refactor compiles within the Unity Editor, resolving any serialized field migrations triggered by the new constructor signature.
3. **Implement DTO restructuring** for `HexSpawnerState` and commit companion updates to `HexSpawner.cs` and `GameSpawner.cs` to consume the new deterministic state model.
4. **Extend spawner coverage** by implementing `EdgeSpawner` and `CornerSpawner` logic, referencing `HexExtensions.HexExtensions.GetHexesSharingCorner()` and neighbor direction utilities.
5. **Establish `SaveLoadService`** and migrate existing ad-hoc persistence calls to the centralized API.
6. **Set up Unity Test Runner assemblies** and seed serialization regression tests before further refactors.

## Appendix: Manual Unity Installation Summary
- Download archive: `wget -O ~/Unity/Unity-2021.3.18f1.tar.xz https://download.unity3d.com/download_unity/3129e69bc0c7/LinuxEditorInstaller/Unity.tar.xz`
- Extract: `tar -xf ~/Unity/Unity-2021.3.18f1.tar.xz -C ~/Unity/Editor/2021.3.18f1`
- Launch editor (bypass Hub): `~/Unity/Editor/2021.3.18f1/Editor/Unity -noHub -projectPath ~/dev/SuperHexLink`
- Force external browser (if needed): `UNITY_BROWSER_FORCE_EXTERNAL=1 ~/Unity/Editor/2021.3.18f1/Editor/Unity -noHub ...`
- Apply manual license (if obtained from another machine): `~/Unity/Editor/2021.3.18f1/Editor/Unity -noHub -manualLicenseFile /path/to/Unity_lic.ulf`

This document provides the authoritative reference for the new workstation to continue stabilization work without digging back through prior session history.
