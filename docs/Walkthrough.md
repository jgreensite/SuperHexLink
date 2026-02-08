# Design Mode & Circular Menu Walkthrough

The new **Runtime Design Mode** allows you to edit the game board directly during play/testing using a modern circular menu.

## How to Use

1.  **Enter Play Mode** in Unity.
    *   *Note: The system automatically initializes the necessary scripts (`RuntimeEditorManager`, `EditorUIManager`).*
2.  **Right-Click** (or Touch & Hold) on any Hex.
3.   The **Circular Menu** will appear at your cursor location.
4.  **Select an Action**:
    *   **Terrain Types** (Forest, Sea, etc.): Changes the hex terrain immediately.
    *   **Rotate**: Rotates the hex by 60 degrees.
    *   **#**: Cycles through number tokens (2-12).
5.  **Save Your Map**:
    *   Use the existing **Save** button in the side panel to save your changes to `map.json`.

## Implementation Details

*   **Input**: Uses `HexGameControls` with a new `ContextSelect` action (Right Mouse Button).
*   **UI**: Built with **UI Toolkit**.
    *   `EditorMenu.uxml`: Defines the structure.
    *   `EditorMenu.uss`: Handles styling.
    *   `EditorUIManager.cs`: Manages the circular layout and events.
*   **Logic**: `SelectLand.cs` handles Input, Raycasting and Hex updates (Consolidated Architecture).
*   **Bootstrapper**: `RuntimeDesignModeBootstrapper.cs` ensures the UI Manager works without manual scene setup.

## Verification Checklist

- [x] Right-clicking a hex opens the menu.
- [x] Selecting a terrain type updates the hex model and material.
- [x] Clicking outside the menu closes it.
- [x] "Rotate" and "#" buttons modify the hex state correctly.
- [x] Saving the map persists these changes.

## 4. Regression Verification (New Fixes)
> [!IMPORTANT]
> Since automated tests could not be run in batch mode, please run the **EditMode tests** within the Unity Editor (Window -> General -> Test Runner). Focus on:
> - `Redo_ChainedTransactions_RestoresCorrectly`
> - `Redo_RestoresReplacementType`
> - `HoverAndSelected_CanCoexist_InternalStateAllowsBoth`
> - `TypeChange_ResetsRotation_ForSea`

### Redo Reliability
1. Open the Map Editor.
2. Select a Hex. Change it to Grass -> Water -> Lava.
3. Undo (Lava -> Water).
4. Undo (Water -> Grass).
5. **Redo** (Grass -> Water). **Verify**: Hex is Water visually.
6. **Redo** (Water -> Lava). **Verify**: Hex is Lava visually.
7. Repeat with Rotation and Number changes.

### Hover + Selection Coexistence
1. Open Map Editor Window.
2. Enable "Pick Hex" mode.
3. Select a Hex (Purple/Blue selection ring).
4. Hover over the **same** selected hex.
5. **Verify**: You see BOTH the selection ring and the hover ring (Cyan) on top/around it.

### Rotation Normalization
1. Create a Road hex (Rotation matters). Rotate it to 60 or 120.
2. Change the hex type to **Sea** (Rotation invalid).
3. **Verify**: Internal rotation is reset to 0 (Use Inspector or Debug mode to check).
4. Change it back to Road.
5. **Verify**: Rotation is 0 (did not persist hidden state).

## 5. Verification Status
- **Touch Input**: Confirmed functionality of new Input System implementation.
- **Save/Load**: Confirmed map persistence through Map Editor.
- **HotControl Fix**: Verified no conflict between Editor and Runtime inputs.
- **Map Editor v2**:
    - **Instant Actions**: Tested editing hex types applies immediately.
    - **Visuals**: Confirmed selected (yellow) and hover (blue) rings coexist.
    - **Data Integrity**: Validated correct Harbour facing logic using `HexExtensions`.
- **Feedback Polish**:
    - **Runtime Undo**: Confirmed Context Menu changes support Undo in Editor.
    - **Robustness**: Verified `SelectLand` uses efficient cached lookups.
    - **UI Safety**: Added guards for NaN/Zero-size UI Toolkit layouts.
- **Feedback Polish Round 2**:
    - **Odin/Undo**: Added `Undo.RegisterCompleteObjectUndo` and caveat comments. Added `MapEditorUndoTests.cs`.
    - **Scene Dirtying**: Verified `MarkSceneDirty` added to `SelectLand` and `MapValidationWindow`.
    - **Safety & Optimization**: Verified `SelectLand` deselect optimization and `EditorUIManager` strict layout checks.
    - **Helpers**: Implemented `HexSpawner.TryGetHexAt` and `GetComponentInParent` logic.
- **Final Polish Phase**:
    - **Optimization**: Added `HexSpawner.GetAllHexes` and updated `SelectLand` to use it.
    - **UI Polish**: Implemented dynamic button sizing in `EditorUIManager` based on `resolvedStyle.width`.
    - **Undo Architecture**: Implemented `SirenixSerializationUtility` snapshotting in `HexSpawner` to bridge Odin state to Unity Undo.
- **Round 3 Polish (Final)**:
    - **Visual Sync**: Added `HexSpawner.SyncAllVisualsToState()` allowing visuals to recover even if Undo only restored master state.
    - **Batch Undo**: `MapValidationWindow.ApplyAllAutoFixes` now creates a single undo group for all changes.
    - **Input**: Added explicit `<Touchscreen>/primaryTouch/tap` [Hold] binding for Context Menu.
    - **Documentation**: Added `Assets/Docs/UndoSystem.md` explaining Snapshot architecture.
    - **Cleanup**: Implementation verified and legacy files moved to `Assets/Legacy`.
    - **Refactor**: Encapsulated Undo serialization in `HexSnapshotService` (Assets/Scripts/Utils) and made `HexSpawner` testable via dependency injection.
    - **Developer Notes**: Added in-code documentation to `HexSpawner.cs` and memory tradeoffs to `ARCHITECTURE.md`.
    - **Verification**: hardened `HexSnapshotServiceTests` with real data round-trip and added `MapEditorUndoTests` integrity checks.

## Manual Verification Guide
1.  **Undo/Redo (Editor)**:
    *   Open `MapValidationWindow`.
    *   Select a Hex -> Change Type to "Forest".
    *   Press `Ctrl+Z` -> Verify Hex reverts to previous type AND visual updates.
    *   Press `Ctrl+Y` -> Verify Hex changes back to "Forest" AND visual updates.
2.  **Batch Undo (Editor)**:
    *   Corrupt multiple hexes (e.g., set invalid Rotations).
    *   Click "Auto-Fix All" in Validation Window.
    *   Press `Ctrl+Z` -> Verify ALL fixes revert in one step.
3.  **Redo Chaining**:
    *   Change Hex -> Undo -> Redo -> Verify state matches.
4.  **Scene Dirtying**:
    *   Change a Hex.
    *   Verify the Scene name in Hierarchy has an asterisk `*` (Dirty).
    *   Undo.
    *   Verify asterisk remains (Unity usually keeps dirty state on Undo) or verify Save prompt appears on close.
