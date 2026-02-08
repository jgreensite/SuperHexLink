# Map Editor Undo System

## Overview
The Map Editor v2 uses a hybrid Undo strategy to ensure reliability between Odin Inspector's serialized data (`[OdinSerialize]`) and Unity's native Undo system.

## The Problem
Odin serialization structures (nested `List<List<HexState>>`) are not always reliably tracked by `Undo.RecordObject`. Changes to deep properties might be missed by Unity's native dirty tracking, leading to desync where pressing Undo reverts the scene dirty flag but not the actual data.

## The Solution: Snapshotting
Reference: `HexSpawner.cs`

1.  **Snapshot**: Before any modification, we modify `byte[] _undoSnapshot` in `HexSpawner`.
2.  **Serialize**: We use `SirenixSerializationUtility.SerializeValue` to store the *entire* `HexSpawner.State` into this byte array.
3.  **Record**: We call `Undo.RegisterCompleteObjectUndo`. Unity records the state of `_undoSnapshot`.
4.  **Restore**: When the user presses Undo, Unity restores the `_undoSnapshot` byte array to its previous state.
5.  **Rehydrate**: We hook into `Undo.undoRedoPerformed`. When triggered, we verify if `_undoSnapshot` exists and immediate deserialize it back into `HexSpawner.State`.
6.  **Sync Visuals**: Finally, we call `SyncAllVisualsToState()` to force every `Hex` GameObject to update its component data (`HexType`, `HexNum`, `Rotation`) to match the restored Master State.

## Best Practices
*   **Always Snapshot**: Call `_hexSpawner.CreateUndoSnapshot()` before `Undo.RecordObject`.
*   **Batching**: For bulk operations (like "Fix All"), snapshot ONCE before the loop. This creates a single Undo step.
*   **Memory**: Snapshots store keyframe modifications. For massive maps (10k+ hexes), this could consume memory. Optimizations (clearing old snapshots or diffing) may be needed if memory pressure increases.

## Operational Guidance
*   **Snapshot Size**: We enforce a test threshold of 5MB per snapshot. If exceeded, consider compressing the snapshot or clearing the undo history.
*   **Clearing Undo**: To free memory, use `Undo.ClearAll()`. **Warning**: This disables Redo. Only do this if memory is critical.
*   **Redo Logic**: Valid Redo requires the system to restore a "future" snapshot. To support this, ensure `_undoSnapshot` captures the validation state *after* operations if you want Redo to re-apply it perfectly (though our current implementation prioritizes safe Undo).
