# TODO List

This document tracks all outstanding tasks, issues, and planned improvements for SuperHexLink.

## Critical Path

### 1. Complete Board State Serialization
**Priority**: High  
**Status**: In Progress (70% complete)

- [x] Extract CoreLogic library with grid math
- [x] Implement backing state pattern in Hex.cs
- [x] Add BaseHexState serialization
- [x] Create CoreLogicAdapter for Unity integration
- [x] Deploy CoreLogic.dll to Assets/Plugins/CoreLogic/
- [x] Add unit tests for serialization round-trips
- [ ] Implement EdgeSpawner reconstruction from saved state
- [ ] Implement CornerSpawner reconstruction from saved state
- [ ] Add end-to-end save/load validation tests
- [ ] Test deterministic board recreation (Load → Save → Compare hashes)

**Note**: After modifying CoreLogic, run `.\scripts\deploy-corelogic-to-unity.cmd` to update Unity.

**Blockers**: None  
**Files**: `Assets/Scripts/HexSpawner.cs`, `Assets/Scripts/EdgeSpawner.cs`, `Assets/Scripts/CornerSpawner.cs`

### 2. CI/CD Pipeline Stabilization
**Priority**: High  
**Status**: In Progress (80% complete)

- [x] Add CoreLogic dotnet tests to CI
- [x] Add csproj build guard
- [x] Set up pre-push hooks
- [x] Configure GitHub Actions workflows
- [x] Zero StyleCop warnings in CoreLogic
- [ ] Activate Unity license on self-hosted runner
- [ ] Run Unity EditMode tests in CI
- [ ] Add Unity PlayMode tests
- [ ] Configure test result artifacts
- [ ] Add code coverage reporting

**Blockers**: Unity license activation on runner  
**Files**: `.github/workflows/ci.yml`, `.github/workflows/unity-selfhost-tests.yml`

### 3. Refactor Spawner Architecture
**Priority**: Medium  
**Status**: Not Started

- [ ] Remove GameObject.Find usage in GameSpawner
- [ ] Implement dependency injection for spawners
- [ ] Split HexSpawner responsibilities:
  - [ ] HexFactory (instantiation)
  - [ ] HexRegistry (tracking)
  - [ ] SelectionManager (selection logic)
- [ ] Add object pooling for hex/edge/corner prefabs
- [ ] Create SaveLoadService to centralize persistence
- [ ] Add async save operations

**Blockers**: Save/load must be stable first  
**Files**: `Assets/Scripts/GameSpawner.cs`, `Assets/Scripts/HexSpawner.cs`

## Code Quality Issues

### Unity Scripts (Assets/Scripts/)

#### GameSpawner.cs
- **Line 79**: Externalize default value as constant
- **Line 116**: Simplify spawn logic (current implementation not elegant)
- **Issue**: Tight coupling via GameObject.Find
- **Issue**: No null safety checks

#### HexSpawner.cs
- **Line 114**: Remove redundant `GroupID = null` assignment
- **Line 427**: Simplify complex refresh logic
- **Issue**: Mixed responsibilities (factory + registry + selection)
- **Issue**: Serialization coupled to spawning

#### GameUIManager.cs
- **Line 85-86**: Refactor to use UQuery for cleaner element selection
- Reference: [Unity UQuery Documentation](https://docs.unity3d.com/Manual/UIE-UQuery.html)

#### HexLandModel.cs
- **Line 7**: Remove leftover old hex model code (technical debt)

#### HexExtensions.cs
- **Line 83**: Update tests after direction order change (clockwise for rotations)
- **Line 385**: Remove hardcoded Q-ODD system assumption

#### GameConstants.cs
- **Line 7**: Add private setters, public getters for all constants
- **Line 186**: Make server and port selectable (currently hardcoded)

#### Rng.cs
- **Line 17**: Review suspicious ASCII conversion logic: `Convert.ToDouble(randomNumber[0])`

## Testing Gaps

### Unit Tests Needed
- [ ] HexGrid distance calculations
- [ ] Hex neighbor finding (all 6 directions)
- [ ] Coordinate conversion edge cases (large coords, negative coords)
- [ ] Serialization with null/missing fields
- [ ] State validation (invalid hex types, out-of-bounds coords)

### Integration Tests Needed
- [ ] CoreLogicAdapter round-trip conversions
- [ ] Spawner state reconstruction
- [ ] Save/Load with complex board layouts
- [ ] Multi-player state synchronization prep

### End-to-End Tests Needed
- [ ] Generate → Save → Clear → Load → Verify identical board
- [ ] Load saved game from previous version (migration test)
- [ ] Performance: Load 1000+ hex board
- [ ] Stress test: Rapid save/load cycles

## Documentation Gaps

- [x] Main README with project overview
- [x] Development setup guide
- [x] Testing guide
- [x] Architecture documentation
- [x] Contributing guide
- [x] CoreLogic library README
- [ ] API documentation (XML comments)
- [ ] Tutorial: Adding new hex types
- [ ] Tutorial: Creating custom spawners
- [ ] Troubleshooting guide
- [ ] Performance optimization guide

## Technical Debt

### High Priority
1. **Remove GameObject.Find usage** - Tight coupling, breaks in complex scenes
2. **Add null safety checks** - Many spawner methods assume objects exist
3. **Implement proper error handling** - Crashes on malformed save files
4. **Add schema versioning** - No migration path for save file changes

### Medium Priority
1. **Refactor message system** - Mix of GameConstants + NodeCanvas events
2. **Add object pooling** - Instantiate/Destroy creates GC pressure
3. **Optimize serialization** - Current JSON serialization is synchronous on main thread
4. **Remove ParadoxNotion TODOs** - Vendor code comments should be tracked separately

### Low Priority
1. **Consistent naming conventions** - Mix of camelCase/PascalCase in places
2. **Reduce code duplication** - EdgeSpawner/CornerSpawner have similar patterns
3. **Add editor tools** - Custom inspectors for complex types
4. **Improve logging** - Add structured logging with levels

## Future Features

### Phase 1: Core Gameplay
- [ ] Implement Edge gameplay mechanics
- [ ] Implement Corner gameplay mechanics
- [ ] Add hex adjacency rules engine
- [ ] Add win condition detection
- [ ] Add player turn management

### Phase 2: Multiplayer
- [ ] Add deterministic simulation
- [ ] Implement state synchronization
- [ ] Add rollback/prediction networking
- [ ] Add replay system
- [ ] Add spectator mode

### Phase 3: Performance
- [ ] Evaluate ECS migration
- [ ] Add Burst compiler support
- [ ] Implement Job System parallelization
- [ ] Add asset streaming for large maps
- [ ] Optimize rendering (frustum culling, LOD)

### Phase 4: Content
- [ ] Map editor UI
- [ ] Save/Load UI improvements
- [ ] Campaign mode
- [ ] Procedural generation settings
- [ ] Custom game rules editor

## Build & Infrastructure

### Scripts Organization
- [x] Clean up scripts/ directory structure
- [ ] Remove actions-runner artifacts from repo (add to .gitignore)
- [ ] Document script usage in scripts/README.md
- [ ] Add script parameter validation
- [ ] Create unified build script

### CI/CD Improvements
- [ ] Add automated Unity builds
- [ ] Add platform-specific builds (Windows, macOS, Linux)
- [ ] Add automated deployment to itch.io or similar
- [ ] Add build artifact retention policy
- [ ] Add nightly test runs

## Notes

### Third-Party Code TODOs
ParadoxNotion (NodeCanvas) vendor code contains many TODO/FIXME markers. These should be:
- Tracked separately from project TODOs
- Reviewed before updating vendor code
- Reported upstream if appropriate
- Documented if we add workarounds

### Cleanup Actions
When cleaning up TODOs:
1. Create an issue on GitHub
2. Link issue number to TODO comment
3. Update this file to reference issue
4. Remove TODO comment after completion

### Priority Definitions
- **Critical**: Blocks other work or causes crashes
- **High**: Important for next release
- **Medium**: Improves quality but not urgent
- **Low**: Nice to have, can defer

---

**Last Updated**: November 22, 2024  
**Next Review**: After save/load stabilization is complete
