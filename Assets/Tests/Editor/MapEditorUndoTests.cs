using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorUndoTests
{
    private GameObject _spawnerGo;
    private HexSpawner _spawner;
    private GameObject _hexGo;
    private Hex _hex;

    [SetUp]
    public void Setup()
    {
        _spawnerGo = new GameObject("HexSpawner");
        _spawner = _spawnerGo.AddComponent<HexSpawner>();
        _spawner.State = new HexSpawner.HexSpawnerState
        {
            hexes = new List<List<Hex.HexState>>()
        };
        _spawner.State.hexes.Add(new List<Hex.HexState>());
        _spawner.State.hexes[0].Add(new Hex.HexState
        {
            Col = 0,
            Row = 0,
            HexType = "Grass", // Start state
            Rotation = 0
        });

        _hexGo = new GameObject("Hex");
        _hexGo.transform.parent = _spawnerGo.transform;
        _hex = _hexGo.AddComponent<Hex>();
        _hex.hexState = new Hex.HexState
        {
            Col = 0,
            Row = 0,
            HexType = "Grass",
            Rotation = 0
        };
    }

    [TearDown]
    public void Teardown()
    {
        if (_spawnerGo != null) Object.DestroyImmediate(_spawnerGo);
        if (_hexGo != null) Object.DestroyImmediate(_hexGo);
    }

    [Test]
    public void HexTypeChange_WithUndo_RevertsState()
    {
        // 1. Snapshot initial state
        var initialType = "Grass";
        var newType = "Water";

        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType);

        // 2. Perform modification (simulating SelectLand logic)
        _spawner.CreateUndoSnapshot();
        Undo.RecordObject(_spawner, "Modify Hex State");
        Undo.RegisterCompleteObjectUndo(_spawner, "Modify Hex State Full"); // Matches our fix

        _spawner.State.hexes[0][0].HexType = newType;
        _hex.hexState.HexType = newType;
        EditorUtility.SetDirty(_spawner);

        Assert.AreEqual(newType, _spawner.State.hexes[0][0].HexType);

        // 3. Perform Undo
        Undo.PerformUndo();

        // 4. Verify Reversion
        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType, 
            "HexType should revert to original after Undo. If fails, Odin serialization might be bypassing Unity Undo.");
        
        // 5. Verify Visual Reversion
        // In a real scenario, RefreshHex would update the visual mesh/material.
        // Here we assert the visual Hex component's state which tracks the type.
        Assert.AreEqual(initialType, _hex.hexState.HexType, "Visual Hex component state should also revert.");
    }

    [Test]
    public void BatchModification_WithUndo_RevertsAll()
    {
        // 1. Snapshot initial state
        var initialType = "Grass";
        var batchType = "Desert";
        
        // Setup multiple hexes (already have [0][0], need [0][1])
        _spawner.State.hexes[0].Add(new Hex.HexState 
        { 
            Col = 0, 
            Row = 1, 
            HexType = initialType,
            Rotation = 0 
        });
        
        // Create Visual GameObject for Hex 1 so we can verify SyncAllVisualsToState affects it
        var hexGo2 = new GameObject("Hex_0_1");
        hexGo2.transform.parent = _spawner.transform;
        var hex2 = hexGo2.AddComponent<Hex>();
        hex2.hexState = new Hex.HexState { Col = 0, Row = 1, HexType = initialType, Rotation = 0 };

        // 2. Perform Batch Modification (simulating ApplyAllAutoFixes logic)
        _spawner.CreateUndoSnapshot();
        Undo.RegisterCompleteObjectUndo(_spawner, "Batch Fix");
        
        // Modify multiple
        _spawner.State.hexes[0][0].HexType = batchType;
        _spawner.State.hexes[0][1].HexType = batchType;
        
        // Simulate side-effects of tool: Mark Dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(_spawner.gameObject.scene);

        Assert.AreEqual(batchType, _spawner.State.hexes[0][0].HexType);
        Assert.AreEqual(batchType, _spawner.State.hexes[0][1].HexType);
        Assert.IsTrue(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty, "Scene should be dirty after modification");
        
        // 3. Perform Undo
        Undo.PerformUndo();
        
        // 4. Verify Master Reversion
        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType, "Hex 0 Master should revert");
        Assert.AreEqual(initialType, _spawner.State.hexes[0][1].HexType, "Hex 1 Master should revert");
        
        // 5. Verify Visual Reversion (Checks SyncAllVisualsToState)
        // Since we don't have a full visual refresh in this test isolation (usually), 
        // we might need to manually invoke the OnUndoRedo callback or mock it.
        // However, in an Integration Test, Unity would call it. 
        // For this Unit Test, if we want to test SyncAllVisualsToState, we can call it manually to verify logic:
        
        // Invoke the sync logic manually to test it works given the restored state
        var method = typeof(HexSpawner).GetMethod("OnUndoRedoPerformed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if(method != null) method.Invoke(_spawner, null); 

        // Verify Visual Reversion
        // We assert expected visual state (HexType) which proves SyncAllVisualsToState worked
        Assert.AreEqual(initialType, _hex.hexState.HexType, "Visual Hex 0 Component should revert");
        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType, "Visual Hex 0 should match Master");
        
        // Assert visual state of the added hex as well
        Assert.AreEqual(initialType, hex2.hexState.HexType, "Visual Hex 1 Component should revert");
        
        // Cleanup second hex
        Object.DestroyImmediate(hexGo2);
    }

    [Test]
    public void SnapshotFlow_IntegrityCheck()
    {
        // 1. Setup Service
        var service = new SuperHexLink.Utils.HexSnapshotService();
        _spawner.SetSnapshotService(service);

        // 2. Create Snapshot
        _spawner.CreateUndoSnapshot();

        // 3. Verify Snapshot Exists (via Reflection since field is private)
        var field = typeof(HexSpawner).GetField("_undoSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var snapshot = field.GetValue(_spawner) as byte[];
        
        Assert.IsNotNull(snapshot, "Snapshot byte array should be populated");
        Assert.Greater(snapshot.Length, 0, "Snapshot should contain data");
        
        // Log size for monitoring (avoids memory regressions)
        Debug.Log($"[Test Monitor] Snapshot Size: {snapshot.Length} bytes");
        
        // Assertion: Ensure snapshot is within reasonable limits (e.g., < 5MB)
        Assert.Less(snapshot.Length, 5 * 1024 * 1024, "Snapshot size exceeds 5MB threshold");

        // 4. Simulate Unity Undo Restoration (manually set field + trigger callback)
        // Change state first so we have something to revert TO
        _spawner.State.hexes[0][0].HexType = "Lava";
        Assert.AreEqual("Lava", _spawner.State.hexes[0][0].HexType);

        // Restore snapshot manually (simulate unity undo)
        // In a real scenario, proper Unity Undo would handle this assignment.
        // Here we just want to verify OnUndoRedoPerformed logic works if the data is there.
        // Note: field value is already the 'original' snapshot because we didn't update it after "Lava".
        
        // Invoke Callback
        InvokeUndoRedoCallback();

        // 5. Assert Restoration
        Assert.AreEqual("Grass", _spawner.State.hexes[0][0].HexType, "State should be restored from snapshot");
        
        // 6. Assert Visual Sync (since we called the callback which calls SyncAllVisualsToState)
        Assert.AreEqual("Grass", _hex.hexState.HexType, "Visuals should be synced to restored state");
    }

    [Test]
    public void Redo_RestoresState_Correctly()
    {
        // 1. Initial State
        var initialType = "Grass";
        var changedType = "Water";

        // 2. Perform Change and Snapshot
        _spawner.CreateUndoSnapshot(); // Snapshot "Grass"
        Undo.RecordObject(_spawner, "Change Type");
        Undo.RegisterCompleteObjectUndo(_spawner, "Change Type");
        
        // Simulating the transaction:
        _spawner.State.hexes[0][0].HexType = changedType;
        _hex.hexState.HexType = changedType; 
        
        // CRITICAL: Update snapshot to match new state so Redo works
        // This simulates a "complete" transaction logic we verified earlier.
        _spawner.CreateUndoSnapshot(); 

        // 3. Undo
        Undo.PerformUndo();
        InvokeUndoRedoCallback();
        
        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType, "State should match Initial after Undo");

        // 4. Redo
        Undo.PerformRedo();
        InvokeUndoRedoCallback();

        Assert.AreEqual(changedType, _spawner.State.hexes[0][0].HexType, "State should match Changed after Redo");
    }

    [Test]
    public void SnapshotService_Failure_HandledGracefully()
    {
        // 1. Setup Mock Service that fails
        var mockService = new MockFailingSnapshotService();
        _spawner.SetSnapshotService(mockService);

        // 2. Setup State
        _spawner.CreateUndoSnapshot(); // Should log error, not crash

        // 3. Verify Graceful Fail (no snapshot created)
        var field = typeof(HexSpawner).GetField("_undoSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var snapshot = field.GetValue(_spawner) as byte[];
        Assert.IsNull(snapshot, "Snapshot should be null if service fails");

        // 4. Verify Restore Graceful Fail
        field.SetValue(_spawner, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
        
        var method = typeof(HexSpawner).GetMethod("OnUndoRedoPerformed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.DoesNotThrow(() => method.Invoke(_spawner, null));
    }

    private class MockFailingSnapshotService : SuperHexLink.Utils.IHexSnapshotService
    {
        public byte[] CreateSnapshot<T>(T state)
        {
            Debug.LogError("Mock Service Failure");
            return null;
        }

        public T RestoreSnapshot<T>(byte[] snapshot)
        {
            Debug.LogError("Mock Service Restore Failure");
            return default;
        }
    }


    [Test]
    public void Redo_ChainedTransactions_RestoresCorrectly()
    {
        // Regression Guard: Verify Multi-step Undo/Redo (stack depth > 1)
        var typeA = "Grass";
        var typeB = "Water";
        var typeC = "Lava";

        // Step 1: Baseline (Grass)
        Assert.AreEqual(typeA, _spawner.State.hexes[0][0].HexType);

        // Step 2: Change to Water
        _spawner.CreateUndoSnapshot(); // Snapshot Grass
        LogSnapshotSize("Step A->B");
        
        Undo.RecordObject(_spawner, "Step B");
        Undo.RegisterCompleteObjectUndo(_spawner, "Step B");
        _spawner.State.hexes[0][0].HexType = typeB;
        _hex.hexState.HexType = typeB; // Manual visual sync for test setup
        
        _spawner.CreateUndoSnapshot(); // Update Snapshot to Water (for Redo)
        LogSnapshotSize("Step B (Post-Update)");

        // Step 3: Change to Lava
        _spawner.CreateUndoSnapshot(); // Snapshot Water
        LogSnapshotSize("Step B->C");
        
        Undo.RecordObject(_spawner, "Step C");
        Undo.RegisterCompleteObjectUndo(_spawner, "Step C");
        _spawner.State.hexes[0][0].HexType = typeC;
        _hex.hexState.HexType = typeC; // Manual visual sync
        
        _spawner.CreateUndoSnapshot(); // Update Snapshot to Lava (for Redo)
        LogSnapshotSize("Step C (Post-Update)");

        // Verify current
        Assert.AreEqual(typeC, _spawner.State.hexes[0][0].HexType);
        Assert.AreEqual(typeC, _hex.hexState.HexType);

        // --- Undo Sequence ---

        // Undo 1 (Lava -> Water)
        Undo.PerformUndo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(typeB, _spawner.State.hexes[0][0].HexType, "Undo 1 should revert Master to Water");
        Assert.AreEqual(typeB, _hex.hexState.HexType, "Undo 1 should revert Visuals to Water");

        // Undo 2 (Water -> Grass)
        Undo.PerformUndo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(typeA, _spawner.State.hexes[0][0].HexType, "Undo 2 should revert Master to Grass");
        Assert.AreEqual(typeA, _hex.hexState.HexType, "Undo 2 should revert Visuals to Grass");

        // --- Redo Sequence ---

        // Redo 1 (Grass -> Water)
        Undo.PerformRedo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(typeB, _spawner.State.hexes[0][0].HexType, "Redo 1 should restore Master to Water");
        Assert.AreEqual(typeB, _hex.hexState.HexType, "Redo 1 should restore Visuals to Water");
        
        // Redo 2 (Water -> Lava)
        Undo.PerformRedo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(typeC, _spawner.State.hexes[0][0].HexType, "Redo 2 should restore Master to Lava");
        Assert.AreEqual(typeC, _hex.hexState.HexType, "Redo 2 should restore Visuals to Lava");
    }

    [Test]
    public void Redo_RestoresReplacementType()
    {
        // Redo should be able to restore a change to 'none' (empty land)
        var initialType = _spawner.State.hexes[0][0].HexType;
        var newType = GameConstants.CAR_TYPE_NONE;

        // Snapshot before change
        _spawner.CreateUndoSnapshot();
        Undo.RecordObject(_spawner, "Replace to none");
        Undo.RegisterCompleteObjectUndo(_spawner, "Replace to none");

        // Apply change
        _spawner.State.hexes[0][0].HexType = newType;
        _hex.hexState.HexType = newType;
        _spawner.CreateUndoSnapshot(); // Ensure redo target exists

        // Undo -> back to initial
        Undo.PerformUndo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(initialType, _spawner.State.hexes[0][0].HexType, "Undo should restore original type");
        Assert.AreEqual(initialType, _hex.hexState.HexType, "Undo should restore visual type");

        // Redo -> back to newType
        Undo.PerformRedo();
        InvokeUndoRedoCallback();
        Assert.AreEqual(newType, _spawner.State.hexes[0][0].HexType, "Redo should restore replaced type");
        Assert.AreEqual(newType, _hex.hexState.HexType, "Redo should restore visual replaced type");
    }

    [Test]
    public void HoverAndSelected_CanCoexist_InternalStateAllowsBoth()
    {
        // Ensure MapEditorWindow allows hover on a selected hex (internal state test)
        var window = UnityEditor.EditorWindow.GetWindow<MapEditorWindow>(true, "_TestWindow", false);
        Assert.IsNotNull(window, "Could not get MapEditorWindow instance");

        // Set selected hex via private field
        var selField = typeof(MapEditorWindow).GetField("_selectedHex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        selField.SetValue(window, _hex);

        // Call HighlightHexForHover (private) and verify hovered field becomes set
        var hoverMethod = typeof(MapEditorWindow).GetMethod("HighlightHexForHover", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(hoverMethod, "Could not find HighlightHexForHover method");
        hoverMethod.Invoke(window, new object[] { _hex });

        var hoverField = typeof(MapEditorWindow).GetField("_hoveredHex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var hovered = hoverField.GetValue(window) as Hex;
        Assert.IsNotNull(hovered, "Hovered hex should be set after invoking HighlightHexForHover");
        Assert.AreEqual(_hex, hovered, "Hovered hex should match the same hex that is selected");
    }

    [Test]
    public void TypeChange_ResetsRotation_ForSea()
    {
        // Ensure replacing a harbour with sea resets rotation to 0
        // Set initial state to harbour-like with rotation
        _spawner.State.hexes[0][0].HexType = GameConstants.CAR_TYPE_HARBOUR;
        _spawner.State.hexes[0][0].Rotation = 60;
        _hex.hexState.HexType = GameConstants.CAR_TYPE_HARBOUR;
        _hex.hexState.Rotation = 60;

        // Apply type change to sea
        var seaType = GameConstants.CAR_TYPE_SEA;
        _spawner.State.hexes[0][0].HexType = seaType;
        // Simulate what production code would do (refresh visuals)
        _hex.hexState.HexType = seaType;
        _spawner.RefreshHex(_hex);

        // Expect rotation normalized to 0
        Assert.AreEqual(0, _spawner.State.hexes[0][0].Rotation, "Master state rotation should be reset for sea");
        Assert.AreEqual(0, _hex.hexState.Rotation, "Visual rotation should be reset for sea");
    }

    private void InvokeUndoRedoCallback()
    {
        var method = typeof(HexSpawner).GetMethod("OnUndoRedoPerformed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(method, "Could not find OnUndoRedoPerformed method via reflection");
        method.Invoke(_spawner, null);
    }

    private void LogSnapshotSize(string stepLabel)
    {
        var field = typeof(HexSpawner).GetField("_undoSnapshot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var snapshot = field.GetValue(_spawner) as byte[];
        if (snapshot != null)
        {
            Debug.Log($"[Test Monitor] {stepLabel} - Snapshot Size: {snapshot.Length} bytes");
        }
    }
}
