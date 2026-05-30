using NUnit.Framework;
using UnityEngine;
using SuperHexLink;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for ISpawner interface implementation across all spawner types.
    /// Verifies that all spawners properly implement the ISpawner contract.
    /// </summary>
    public class ISpawnerTests
    {
        private GameObject gameSpawnerGo;
        private GameObject hexSpawnerGo;
        private GameObject edgeSpawnerGo;
        private GameObject cornerSpawnerGo;
        
        private GameSpawner gameSpawner;
        private HexSpawner hexSpawner;
        private EdgeSpawner edgeSpawner;
        private CornerSpawner cornerSpawner;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObjects
            gameSpawnerGo = new GameObject("GameSpawner");
            gameSpawner = gameSpawnerGo.AddComponent<GameSpawner>();
            
            hexSpawnerGo = new GameObject("HexSpawner");
            hexSpawner = hexSpawnerGo.AddComponent<HexSpawner>();
            
            edgeSpawnerGo = new GameObject("EdgeSpawner");
            edgeSpawner = edgeSpawnerGo.AddComponent<EdgeSpawner>();
            
            cornerSpawnerGo = new GameObject("CornerSpawner");
            cornerSpawner = cornerSpawnerGo.AddComponent<CornerSpawner>();

            // Assign references via reflection (simulating Unity Inspector assignment)
            System.Reflection.FieldInfo hexSpawnerField = typeof(GameSpawner).GetField("hexSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo edgeSpawnerField = typeof(GameSpawner).GetField("edgeSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo cornerSpawnerField = typeof(GameSpawner).GetField("cornerSpawner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Reflection.FieldInfo logSettingsField = typeof(GameSpawner).GetField("actionLogSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            hexSpawnerField.SetValue(gameSpawner, hexSpawner);
            edgeSpawnerField.SetValue(gameSpawner, edgeSpawner);
            cornerSpawnerField.SetValue(gameSpawner, cornerSpawner);
            logSettingsField.SetValue(gameSpawner, new SuperHexLink.Logging.ActionLogSettings());
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test GameObjects
            Object.DestroyImmediate(gameSpawnerGo);
            Object.DestroyImmediate(hexSpawnerGo);
            Object.DestroyImmediate(edgeSpawnerGo);
            Object.DestroyImmediate(cornerSpawnerGo);

            // Clean up any spawned objects
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("Hex_") || go.name.StartsWith("Edge_") || go.name.StartsWith("Corner_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HexSpawner_ImplementsISpawner_ReturnsTrue()
        {
            // Act
            ISpawner spawner = hexSpawner as ISpawner;

            // Assert
            Assert.IsNotNull(spawner, "HexSpawner should implement ISpawner interface");
        }

        [Test]
        public void EdgeSpawner_ImplementsISpawner_ReturnsTrue()
        {
            // Act
            ISpawner spawner = edgeSpawner as ISpawner;

            // Assert
            Assert.IsNotNull(spawner, "EdgeSpawner should implement ISpawner interface");
        }

        [Test]
        public void CornerSpawner_ImplementsISpawner_ReturnsTrue()
        {
            // Act
            ISpawner spawner = cornerSpawner as ISpawner;

            // Assert
            Assert.IsNotNull(spawner, "CornerSpawner should implement ISpawner interface");
        }

        [Test]
        public void GameSpawner_AllSpawnersCollection_ReturnsCorrectCount()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references

            // Act
            var allSpawnersField = typeof(GameSpawner).GetField("AllSpawners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Collections.Generic.IEnumerable<ISpawner> allSpawners = allSpawnersField.GetValue(gameSpawner) as System.Collections.Generic.IEnumerable<ISpawner>;

            // Assert
            Assert.IsNotNull(allSpawners, "AllSpawners collection should not be null");
            Assert.AreEqual(3, allSpawners.Count(), "Should have exactly 3 spawners (hex, edge, corner)");
        }

        [Test]
        public void GameSpawner_AllSpawnersCollection_ContainsAllSpawnerTypes()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references

            // Act
            var allSpawnersField = typeof(GameSpawner).GetField("AllSpawners", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            System.Collections.Generic.IEnumerable<ISpawner> allSpawners = allSpawnersField.GetValue(gameSpawner) as System.Collections.Generic.IEnumerable<ISpawner>;

            // Assert
            Assert.IsTrue(allSpawners.Any(s => s is HexSpawner), "Should contain HexSpawner");
            Assert.IsTrue(allSpawners.Any(s => s is EdgeSpawner), "Should contain EdgeSpawner");
            Assert.IsTrue(allSpawners.Any(s => s is CornerSpawner), "Should contain CornerSpawner");
        }

        [Test]
        public void GameSpawner_BuildMe_UsesAllSpawnersCollection_DoesNotThrow()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references
            gameSpawner.State = new GameSpawner.GameSpawnerState();

            // Act & Assert - Should not throw when using AllSpawners collection
            Assert.DoesNotThrow(() => gameSpawner.BuildMe(false), "BuildMe should not throw when using AllSpawners collection");
        }

        [Test]
        public void GameSpawner_Spawn_UsesAllSpawnersCollection_DoesNotThrow()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references
            gameSpawner.State = new GameSpawner.GameSpawnerState();

            // Act & Assert - Should not throw when using AllSpawners collection
            Assert.DoesNotThrow(() => gameSpawner.Spawn(), "Spawn should not throw when using AllSpawners collection");
        }

        [Test]
        public void GameSpawner_Clear_UsesAllSpawnersCollection_DoesNotThrow()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references

            // Act & Assert - Should not throw when using AllSpawners collection
            Assert.DoesNotThrow(() => gameSpawner.Clear(), "Clear should not throw when using AllSpawners collection");
        }

        [Test]
        public void GameSpawner_Refresh_UsesAllSpawnersCollection_DoesNotThrow()
        {
            // Arrange
            gameSpawner.Awake(); // Initialize spawner references
            gameSpawner.State = new GameSpawner.GameSpawnerState();

            // Act & Assert - Should not throw when using AllSpawners collection
            Assert.DoesNotThrow(() => gameSpawner.Refresh(), "Refresh should not throw when using AllSpawners collection");
        }

        [Test]
        public void ISpawner_InterfaceMethods_AllSpawnersHaveRequiredMethods()
        {
            // Arrange & Act
            ISpawner hexSpawnerInterface = hexSpawner;
            ISpawner edgeSpawnerInterface = edgeSpawner;
            ISpawner cornerSpawnerInterface = cornerSpawner;

            // Assert - All spawners should have the required ISpawner methods
            Assert.IsNotNull(hexSpawnerInterface, "HexSpawner should implement ISpawner");
            Assert.IsNotNull(edgeSpawnerInterface, "EdgeSpawner should implement ISpawner");
            Assert.IsNotNull(cornerSpawnerInterface, "CornerSpawner should implement ISpawner");

            // Verify methods exist (they should since they're defined in the interface)
            Assert.DoesNotThrow(() => hexSpawnerInterface.Spawn(), "HexSpawner Spawn method should be callable");
            Assert.DoesNotThrow(() => hexSpawnerInterface.BuildMe(false), "HexSpawner BuildMe method should be callable");
            Assert.DoesNotThrow(() => hexSpawnerInterface.Clear(), "HexSpawner Clear method should be callable");
            Assert.DoesNotThrow(() => hexSpawnerInterface.Refresh(), "HexSpawner Refresh method should be callable");

            Assert.DoesNotThrow(() => edgeSpawnerInterface.Spawn(), "EdgeSpawner Spawn method should be callable");
            Assert.DoesNotThrow(() => edgeSpawnerInterface.BuildMe(false), "EdgeSpawner BuildMe method should be callable");
            Assert.DoesNotThrow(() => edgeSpawnerInterface.Clear(), "EdgeSpawner Clear method should be callable");
            Assert.DoesNotThrow(() => edgeSpawnerInterface.Refresh(), "EdgeSpawner Refresh method should be callable");

            Assert.DoesNotThrow(() => cornerSpawnerInterface.Spawn(), "CornerSpawner Spawn method should be callable");
            Assert.DoesNotThrow(() => cornerSpawnerInterface.BuildMe(false), "CornerSpawner BuildMe method should be callable");
            Assert.DoesNotThrow(() => cornerSpawnerInterface.Clear(), "CornerSpawner Clear method should be callable");
            Assert.DoesNotThrow(() => cornerSpawnerInterface.Refresh(), "CornerSpawner Refresh method should be callable");
        }
    }
}
