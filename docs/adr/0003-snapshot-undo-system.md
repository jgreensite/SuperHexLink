# ADR-0003: Snapshot-Based Undo System

**Status**: Accepted
**Date**: 2025-12-01
**Deciders**: @jgreensite

## Context

Unity's `Undo.RecordObject` does not reliably track deep changes in Odin-serialized structures like `List<List<HexState>>`. Edits made in the Map Editor were lost on undo.

## Decision

Serialize the entire `HexSpawner.State` into a `byte[]` snapshot before each edit. Store the snapshot in a Unity-tracked field (`_undoSnapshot`). On undo, detect the change and rehydrate the state from the snapshot.

## Consequences

**Positive**:
- Undo/Redo works perfectly for all state changes
- Unity's native undo UI (Ctrl+Z) works as expected
- Supports both single-hex edits and batch operations

**Negative**:
- Memory: each snapshot stores the full state (~50 KB for a 7×7 board)
- Large boards (1000+ hexes) may stress the undo stack

**Mitigations**:
- Memory is acceptable for current board sizes (< 50×50)
- Can clear undo stack on scene change if needed
- Snapshot is compressed by Sirenix serialization

## References
- Implementation: `Assets/Scripts/Utils/HexSnapshotService.cs`
- Tests: `Assets/Tests/Editor/HexSnapshotServiceTests.cs`, `MapEditorUndoTests.cs`
