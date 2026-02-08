# Architecture Overview

This document describes the technical architecture and design decisions for SuperHexLink.

## System Architecture

### Layer Separation

```
┌─────────────────────────────────────────┐
│         Unity Presentation Layer        │
│   (MonoBehaviours, UI, Rendering)       │
├─────────────────────────────────────────┤
│        Unity Game Logic Layer           │
│  (Spawners, State Management, Events)   │
├─────────────────────────────────────────┤
│         CoreLogic Adapter               │
│      (Unity ↔ CoreLogic bridge)         │
├─────────────────────────────────────────┤
│         CoreLogic Library               │
│  (Pure C# - Grid Math, Serialization)   │
└─────────────────────────────────────────┘
```

### Key Design Decisions

#### 1. CoreLogic Library Separation
**Decision**: Extract pure game logic into a standalone .NET Standard 2.0 library.

**Rationale**:
- Fast unit testing without Unity overhead
- Reusable logic across Unity/server/tools
- Clear separation of concerns
- Deterministic behavior validation

**Trade-offs**: Additional adapter layer needed, but benefits outweigh costs.

#### 2. Adapter Pattern for Unity Integration
**Decision**: Use static extension methods in `CoreLogicAdapter.cs` for type conversions.

**Rationale**:
- Keeps CoreLogic Unity-independent
- Minimal boilerplate
- Easy to maintain
- Clear conversion boundaries

#### 3. Backing State Pattern
**Decision**: `Hex.HexState` uses immutable `BaseHexState` backing data.

**Rationale**:
- Prevents recursive property access bugs
- Enables deterministic serialization
- Clear separation of runtime vs. persisted data
- Supports undo/redo and networking

**Implementation**: See `Assets/Scripts/Hex.cs` for details.

## Component Responsibilities

### GameSpawner
- Central coordinator for board initialization
- Manages spawner lifecycle
- Handles camera positioning
- Coordinates save/load operations

**Current Issues**: Tight coupling via GameObject.Find
**Planned**: Dependency injection pattern

### HexSpawner
- Hex instantiation and management
- Random board generation
- State persistence coordination
- Hex selection and highlighting

**Current Issues**: Mixed responsibilities
**Planned**: Split into HexFactory, HexRegistry, SelectionManager

### Harbour Replacement Pipeline
- Executed immediately after initial board generation to convert spare sea tiles into configured harbour counts.
- Candidates are derived from a snapshot of the current board and only allow harbours whose rotation faces one of the `GameConstants.harbourFacingLandTypes` entries.
- Orientation validation reruns after each pass so harbours that lose valid land neighbors revert back to sea and can be retried, which keeps replacement outcomes deterministic even when fallback attempts fire.
- `HexSpawner.RefreshHex(Hex)` is called whenever the pipeline changes a hex so the mesh/material/model combination is re-rendered without rebuilding the entire board.
- Unit tests exercise `HarbourBoardSnapshot` to ensure harbour direction lookups, updates, and tracking remain consistent.

### Hex (MonoBehaviour)
- Runtime hex representation
- Visual state management
- Event handling
- Backing state synchronization

**Architecture**: Uses backing state pattern with `BaseHexState`

### CoreLogic.Grid.HexGrid
- Pure coordinate math (axial, cube, world)
- Neighbor calculations
- Distance functions
- No Unity dependencies

### CoreLogic.Serialization
- JSON serialization/deserialization
- State snapshots
- Deterministic round-trip guarantees

## Data Flow

### Board Creation Flow
```
User Action
    ↓
GameSpawner.GenerateBoard()
    ↓
HexSpawner.RandomizeBoard()
    ↓
HexSpawner.BuildMe(fromState: false)
    ↓
Instantiate Hex prefabs
    ↓
Hex.Initialize(spawner, baseState)
    ↓
Hex visual setup
```

### Save Flow
```
User Save Request
    ↓
GameSpawner.Save()
    ↓
Collect all Hex.BaseHexState
    ↓
CoreLogic.Serialization.Serialize()
    ↓
Write JSON to data/maps/
```

### Load Flow
```
User Load Request
    ↓
GameSpawner.Load()
    ↓
Read JSON from data/maps/
    ↓
CoreLogic.Serialization.Deserialize()
    ↓
HexSpawner.BuildMe(fromState: true)
    ↓
Recreate hexes from BaseHexState
```

## Persistence Model

### Serialized State Structure
```json
{
  "hexes": [
    {
      "col": 0,
      "row": 0,
      "hexType": "Land",
      "hexSubType": "Grass",
      "rotation": 0,
      "hexNum": 5,
      "groupId": null,
      "selected": false
    }
  ]
}
```

### State Guarantees
1. **Deterministic**: Same input → same output
2. **Complete**: All gameplay-relevant data included
3. **Minimal**: No runtime-only data (materials, GameObjects)
4. **Versioned**: Schema version for future migrations

## Testing Strategy

### Unit Tests (CoreLogic)
- Grid math correctness
- Serialization round-trips
- Coordinate conversions
- Edge cases and boundary conditions

**Tool**: NUnit via `dotnet test`
**Speed**: < 100ms for full suite

### Integration Tests (Unity EditMode)
- Adapter conversions
- Spawner logic
- State management
- Prefab instantiation

**Tool**: Unity Test Runner (EditMode)

### End-to-End Tests (Unity PlayMode)
- Full save/load cycles
- Board generation
- User interactions
- Visual validation

**Tool**: Unity Test Runner (PlayMode)

## Build Pipeline

### CI/CD Flow
```
Push to GitHub
    ↓
├─ Fast Checks (< 30s)
│  ├─ CoreLogic build
│  ├─ CoreLogic tests
│  └─ All .csproj builds
│
├─ Unity Checks (self-hosted, minutes)
│  ├─ Unity compilation
│  └─ Unity EditMode tests
│
└─ Deployment (manual)
   └─ Build artifacts
```

### Local Development Flow
```
Code Change
    ↓
Pre-push Hook
    ├─ CoreLogic tests
    └─ .csproj build guard
    ↓
Push (if passing)
```

## Undo System & State Snapshots

### Problem
Odin Inspector's serialized structures (deeply nested `List<List<HexState>>`) are not reliably tracked by Unity's native `Undo.RecordObject`. Native Undo often misses changes to deep properties, leading to state desynchronization.

### Solution: Snapshotting
Reference: `Assets/Scripts/Utils/HexSnapshotService.cs`

1.  **Wrapper**: We use `HexSnapshotService` to serialize the *entire* `HexSpawner.State` into a `byte[]` blob.
2.  **Storage**: The blob is stored in `HexSpawner._undoSnapshot` (which is a primitive array, so Unity Undo tracks it perfectly).
3.  **Restore**: When Unity Undo reverts the `_undoSnapshot` field, `HexSpawner.OnUndoRedoPerformed` detects the change and deserializes the blob back into the live `HexSpawner.State` object graph.
4.  **Visual Sync**: `HexSpawner.SyncAllVisualsToState()` is called to force all `Hex` GameObjects to match the restored master state.

### Trade-offs
*   **Memory**: Storing full snapshots of large maps (10k+ hexes) in the Undo stack increases memory usage. If this becomes a bottleneck, clearing the undo stack or confirming edits (clearing `_undoSnapshot` logic) may be required.
*   **Redo**: This system supports Redo automatically (Unity restores the "future" snapshot, we rehydrate it). **Caution**: Clearing snapshots after Undo prevents Redo. Only clear if memory is critical and Redo is acceptable to lose.

## Third-Party Integration

### Odin Inspector (Sirenix)
- Custom inspector attributes
- Serialization overrides
- Editor tooling enhancements

**Location**: `Assets/Plugins/Sirenix/`
**Usage**: Property attributes on MonoBehaviours

### ParadoxNotion (NodeCanvas)
- Behavior trees
- Visual scripting
- AI logic

**Location**: `Assets/ParadoxNotion/`
**Usage**: Currently minimal, planned expansion

## Future Architecture Goals

### Phase 1: Refactor (Current)
- [x] Extract CoreLogic library
- [x] Implement backing state pattern
- [x] Add fast unit tests
- [x] Set up CI pipeline
- [ ] Complete EdgeSpawner/CornerSpawner

### Phase 2: Services
- [ ] Introduce dependency injection
- [ ] Create service layer (SaveLoadService, EventBus)
- [ ] Remove GameObject.Find usage
- [ ] Implement object pooling

### Phase 3: Networking
- [ ] Deterministic simulation
- [ ] State synchronization
- [ ] Rollback/prediction
- [ ] Replay system

### Phase 4: Performance
- [ ] ECS migration (if needed)
- [ ] Burst compiler integration
- [ ] Job system parallelization
- [ ] Asset streaming

## Migration Notes

### Recent Changes (Nov 2025)
1. Created CoreLogic library from Unity code
2. Added CoreLogicAdapter for type conversions
3. Refactored Hex.HexState to use backing pattern
4. Added comprehensive test coverage
5. Set up CI with fast-fail checks

### Breaking Changes
- `Hex.Initialize()` signature changed (now requires BaseHexState)
- Serialization moved from HexSpawner to CoreLogic
- Some public fields made private (use properties)

### Migration Guide
See commit history on `feature/hex-state-refactor` branch for step-by-step changes.
