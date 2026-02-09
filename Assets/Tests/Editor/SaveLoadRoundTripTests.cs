using NUnit.Framework;
using UnityEngine;
using System.IO;
using System.Linq;
using Sirenix.Serialization;

namespace Tests.Editor
{
    /// <summary>
    /// End-to-end save/load validation tests.
    /// Verifies that a generated board can be saved, cleared, and loaded back
    /// with identical hex/edge/corner counts and types.
    /// </summary>
    public class SaveLoadRoundTripTests
    {
        private const string TempMapDir = "./data/maps/roundtrip_test/";
        private const string TempMapPath = TempMapDir + "map.json";

        [SetUp]
        public void SetUp()
        {
            // Ensure clean test environment
            if (Directory.Exists(TempMapDir)) Directory.Delete(TempMapDir, true);
            Directory.CreateDirectory(TempMapDir);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any created GameObjects
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("hex") || go.name.StartsWith("HexSpawner") || 
                    go.name.StartsWith("GameSpawner") || go.name.StartsWith("text_") || 
                    go.name.StartsWith("landModel") || go.name.StartsWith("hex_prefab") ||
                    go.name.StartsWith("Edge") || go.name.StartsWith("Corner"))
                {
                    Object.DestroyImmediate(go);
                }
            }
            // Remove temp map dir
            if (Directory.Exists(TempMapDir)) Directory.Delete(TempMapDir, true);
        }

        [Test]
        public void Generate_Save_Clear_Load_RoundTrip_MaintainsBoardState()
        {
            // Arrange: Generate a standard board
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();
            
            // Use standard 4-player configuration for reproducibility
            gameSpawner.State = GameSpawner.GameSpawnerState.CreateStandard4Player();
            gameSpawner.saveMapPath = TempMapPath;

            // Generate initial board
            gameSpawner.GenerateBoard();

            // Capture initial state
            var initialHexCount = Object.FindObjectsOfType<Hex>().Length;
            var initialHexTypes = Object.FindObjectsOfType<Hex>()
                .Select(h => h.HexState.HexType)
                .OrderBy(t => t)
                .ToArray();

            // Edge and corner spawners are currently stubbed, but we can still validate their state
            var initialEdgeState = gameSpawner.edgeSpawner.State;
            var initialCornerState = gameSpawner.cornerSpawner.State;

            // Act: Save the board
            gameSpawner.SaveState();

            // Clear the board
            gameSpawner.ClearBoard();

            // Verify board is cleared
            Assert.AreEqual(0, Object.FindObjectsOfType<Hex>().Length, 
                "Board should be empty after ClearBoard");

            // Load the board back
            gameSpawner.LoadState();

            // Assert: Verify round-trip integrity
            var loadedHexCount = Object.FindObjectsOfType<Hex>().Length;
            var loadedHexTypes = Object.FindObjectsOfType<Hex>()
                .Select(h => h.HexState.HexType)
                .OrderBy(t => t)
                .ToArray();

            // Hex count should match
            Assert.AreEqual(initialHexCount, loadedHexCount, 
                $"Hex count mismatch: expected {initialHexCount}, got {loadedHexCount}");

            // Hex types should match
            Assert.AreEqual(initialHexTypes.Length, loadedHexTypes.Length, 
                "Hex type array length mismatch");
            for (int i = 0; i < initialHexTypes.Length; i++)
            {
                Assert.AreEqual(initialHexTypes[i], loadedHexTypes[i], 
                    $"Hex type mismatch at index {i}: expected {initialHexTypes[i]}, got {loadedHexTypes[i]}");
            }

            // Edge and corner state should be preserved (even if currently empty)
            Assert.AreEqual(initialEdgeState, gameSpawner.edgeSpawner.State, 
                "EdgeSpawner state should be preserved through save/load");
            Assert.AreEqual(initialCornerState, gameSpawner.cornerSpawner.State, 
                "CornerSpawner state should be preserved through save/load");

            // Verify all save files exist
            Assert.IsTrue(File.Exists(TempMapPath), "Main map file should exist");
            Assert.IsTrue(File.Exists(Path.Combine(TempMapDir, "0_gameSpawnerState.json")), 
                "GameSpawner state file should exist");
            Assert.IsTrue(File.Exists(Path.Combine(TempMapDir, "1_hexSpawnerState.json")), 
                "HexSpawner state file should exist");
            Assert.IsTrue(File.Exists(Path.Combine(TempMapDir, "2_edgeSpawnerState.json")), 
                "EdgeSpawner state file should exist");
            Assert.IsTrue(File.Exists(Path.Combine(TempMapDir, "3_cornerSpawnerState.json")), 
                "CornerSpawner state file should exist");
        }

        [Test]
        public void SaveLoad_RoundTrip_WithModifiedHexState_PreservesChanges()
        {
            // Arrange: Generate board and modify a hex
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();
            gameSpawner.State = GameSpawner.GameSpawnerState.CreateStandard4Player();
            gameSpawner.saveMapPath = TempMapPath;
            gameSpawner.GenerateBoard();

            // Find a specific hex and modify it
            var hexes = Object.FindObjectsOfType<Hex>();
            Assert.IsTrue(hexes.Length > 0, "Should have at least one hex");
            
            var targetHex = hexes[0];
            var originalType = targetHex.HexState.HexType;
            var originalSelected = targetHex.HexState.Selected;
            
            // Modify the hex state
            targetHex.HexState.HexType = "MODIFIED_TYPE";
            targetHex.HexState.Selected = true;

            // Act: Save, clear, and load
            gameSpawner.SaveState();
            gameSpawner.ClearBoard();
            gameSpawner.LoadState();

            // Assert: Verify modifications are preserved
            var loadedHexes = Object.FindObjectsOfType<Hex>();
            var loadedHex = loadedHexes.FirstOrDefault(h => 
                h.HexState.Col == targetHex.HexState.Col && 
                h.HexState.Row == targetHex.HexState.Row);
            
            Assert.IsNotNull(loadedHex, "Should find the modified hex after load");
            Assert.AreEqual("MODIFIED_TYPE", loadedHex.HexState.HexType, 
                "Modified hex type should be preserved");
            Assert.IsTrue(loadedHex.HexState.Selected, 
                "Modified selected state should be preserved");
            Assert.AreNotEqual(originalType, loadedHex.HexState.HexType, 
                "Hex type should be different from original");
            Assert.AreNotEqual(originalSelected, loadedHex.HexState.Selected, 
                "Selected state should be different from original");
        }

        [Test]
        public void SaveLoad_RoundTrip_WithEmptyBoard_HandlesGracefully()
        {
            // Arrange: Create GameSpawner with empty state
            var gsGo = new GameObject("GameSpawner");
            var gameSpawner = gsGo.AddComponent<GameSpawner>();
            gameSpawner.State = new GameSpawner.GameSpawnerState();
            gameSpawner.saveMapPath = TempMapPath;

            // Act: Save empty state, clear (no-op), and load
            Assert.DoesNotThrow(() => gameSpawner.SaveState(), 
                "SaveState should not throw with empty board");
            Assert.DoesNotThrow(() => gameSpawner.ClearBoard(), 
                "ClearBoard should not throw with empty board");
            Assert.DoesNotThrow(() => gameSpawner.LoadState(), 
                "LoadState should not throw with empty board");

            // Assert: Verify empty state is maintained
            Assert.AreEqual(0, Object.FindObjectsOfType<Hex>().Length, 
                "Should have no hexes after loading empty state");
            Assert.IsTrue(File.Exists(TempMapPath), 
                "Map file should still be created for empty board");
        }
    }
}
