using NUnit.Framework;
using UnityEngine;
using System.Linq;
using Sirenix.Serialization;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for CornerSpawner save/load reconstruction functionality.
    /// Verifies that corners can be saved and reconstructed correctly.
    /// </summary>
    public class CornerSpawnerStateTests
    {
        private GameObject _testGameObject;
        private CornerSpawner _cornerSpawner;
        private GameObject _hexSpawnerGo;
        private HexSpawner _hexSpawner;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObjects
            _testGameObject = new GameObject("TestCornerSpawner");
            _cornerSpawner = _testGameObject.AddComponent<CornerSpawner>();

            _hexSpawnerGo = new GameObject("TestHexSpawner");
            _hexSpawner = _hexSpawnerGo.AddComponent<HexSpawner>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test GameObjects
            if (_testGameObject != null) Object.DestroyImmediate(_testGameObject);
            if (_hexSpawnerGo != null) Object.DestroyImmediate(_hexSpawnerGo);

            // Clean up any spawned corners
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("Corner_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void CornerSpawner_InitialState_IsEmpty()
        {
            // Arrange & Act
            _cornerSpawner.BuildMe(false);

            // Assert
            Assert.IsNotNull(_cornerSpawner.State);
            Assert.IsNotNull(_cornerSpawner.State.corners);
            Assert.AreEqual(0, _cornerSpawner.State.corners.Count);
            Assert.AreEqual(0, _cornerSpawner.GetCornerInstances().Count);
        }

        [Test]
        public void CornerSpawner_AddCorner_CreatesCornerInstance()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Act
            _cornerSpawner.AddCorner(hex, 0, "settlement");

            // Assert
            Assert.AreEqual(1, _cornerSpawner.State.corners.Count);
            Assert.AreEqual(1, _cornerSpawner.GetCornerInstances().Count);

            var cornerState = _cornerSpawner.State.corners.First();
            Assert.AreEqual(hex.hexState.Col, cornerState.hexCol);
            Assert.AreEqual(hex.hexState.Row, cornerState.hexRow);
            Assert.AreEqual(0, cornerState.vertexIndex);
            Assert.AreEqual("settlement", cornerState.cornerType);
        }

        [Test]
        public void CornerSpawner_BuildMe_ReconstructsCornersFromState()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Add a corner to create state
            _cornerSpawner.AddCorner(hex, 0, "settlement");
            var originalState = _cornerSpawner.State;

            // Clear and rebuild
            _cornerSpawner.Clear();
            Assert.AreEqual(0, _cornerSpawner.GetCornerInstances().Count);

            // Act
            _cornerSpawner.BuildMe(false);

            // Assert
            Assert.AreEqual(1, _cornerSpawner.GetCornerInstances().Count);
            Assert.AreEqual(originalState.corners.Count, _cornerSpawner.State.corners.Count);
            
            var reconstructedCorner = _cornerSpawner.GetCornerInstances().First();
            Assert.IsNotNull(reconstructedCorner.gameObject);
            Assert.IsTrue(reconstructedCorner.gameObject.name.StartsWith("Corner_settlement_"));
        }

        [Test]
        public void CornerSpawner_BuildMeWithRefresh_UpdatesExistingCorners()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            _cornerSpawner.AddCorner(hex, 0, "settlement");
            var cornerInstance = _cornerSpawner.GetCornerInstances().First();

            // Act
            _cornerSpawner.BuildMe(true); // Refresh mode

            // Assert
            Assert.AreEqual(1, _cornerSpawner.GetCornerInstances().Count);
            Assert.AreSame(cornerInstance, _cornerSpawner.GetCornerInstances().First());
        }

        [Test]
        public void CornerSpawner_Clear_RemovesAllCorners()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            _cornerSpawner.AddCorner(hex, 0, "settlement");
            _cornerSpawner.AddCorner(hex, 1, "city");

            Assert.AreEqual(2, _cornerSpawner.GetCornerInstances().Count);

            // Act
            _cornerSpawner.Clear();

            // Assert
            Assert.AreEqual(0, _cornerSpawner.GetCornerInstances().Count);
            // State is preserved
            Assert.AreEqual(2, _cornerSpawner.State.corners.Count);
        }

        [Test]
        public void CornerSpawner_AddMultipleCorners_HandlesCorrectly()
        {
            // Arrange
            CreateTestHexes();
            var hexes = _hexSpawner.GetAllHexes().Take(2).ToList();

            // Act
            _cornerSpawner.AddCorner(hexes[0], 0, "settlement");
            _cornerSpawner.AddCorner(hexes[0], 1, "city");
            _cornerSpawner.AddCorner(hexes[1], 2, "settlement");

            // Assert
            Assert.AreEqual(3, _cornerSpawner.State.corners.Count);
            Assert.AreEqual(3, _cornerSpawner.GetCornerInstances().Count);

            var settlementCorners = _cornerSpawner.State.corners.Where(c => c.cornerType == "settlement").ToList();
            var cityCorners = _cornerSpawner.State.corners.Where(c => c.cornerType == "city").ToList();

            Assert.AreEqual(2, settlementCorners.Count);
            Assert.AreEqual(1, cityCorners.Count);
        }

        [Test]
        public void CornerSpawner_CornerState_SerializationWorks()
        {
            // Arrange
            var cornerState = new CornerSpawner.CornerState
            {
                hexCol = 1,
                hexRow = 2,
                vertexIndex = 3,
                cornerType = "settlement"
            };

            // Act - Simulate serialization
            var serialized = SerializationUtility.SerializeValue(cornerState, DataFormat.Binary);
            var deserialized = SerializationUtility.DeserializeValue<CornerSpawner.CornerState>(serialized, DataFormat.Binary);

            // Assert
            Assert.AreEqual(cornerState.hexCol, deserialized.hexCol);
            Assert.AreEqual(cornerState.hexRow, deserialized.hexRow);
            Assert.AreEqual(cornerState.vertexIndex, deserialized.vertexIndex);
            Assert.AreEqual(cornerState.cornerType, deserialized.cornerType);
        }

        [Test]
        public void CornerSpawner_BuildMeWithoutHexSpawner_LogsWarning()
        {
            // Arrange - No HexSpawner in scene
            Object.DestroyImmediate(_hexSpawnerGo);

            // Add some state to try to reconstruct
            _cornerSpawner.State = new CornerSpawner.CornerSpawnerState();
            _cornerSpawner.State.corners.Add(new CornerSpawner.CornerState
            {
                hexCol = 0,
                hexRow = 0,
                vertexIndex = 0,
                cornerType = "settlement"
            });

            // Act & Assert - Should not crash, just log warning
            Assert.DoesNotThrow(() => _cornerSpawner.BuildMe(false));
            Assert.AreEqual(0, _cornerSpawner.GetCornerInstances().Count);
        }

        [Test]
        public void CornerSpawner_CornerInstance_HasCorrectReferences()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Act
            _cornerSpawner.AddCorner(hex, 0, "settlement");
            var cornerInstance = _cornerSpawner.GetCornerInstances().First();

            // Assert
            Assert.IsNotNull(cornerInstance.gameObject);
            Assert.IsNotNull(cornerInstance.cornerState);
            Assert.AreSame(hex, cornerInstance.hex);
            Assert.AreEqual("settlement", cornerInstance.cornerState.cornerType);
            Assert.AreEqual(0, cornerInstance.cornerState.vertexIndex);
        }

        [Test]
        public void CornerSpawner_DifferentCornerTypes_HaveDifferentMaterials()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Act
            _cornerSpawner.AddCorner(hex, 0, "settlement");
            _cornerSpawner.AddCorner(hex, 1, "city");
            _cornerSpawner.AddCorner(hex, 2, "unknown");

            var cornerInstances = _cornerSpawner.GetCornerInstances();

            // Assert
            var settlementCorner = cornerInstances.First(c => c.cornerState.cornerType == "settlement");
            var cityCorner = cornerInstances.First(c => c.cornerState.cornerType == "city");
            var unknownCorner = cornerInstances.First(c => c.cornerState.cornerType == "unknown");

            Assert.AreEqual(Color.green, settlementCorner.gameObject.GetComponent<MeshRenderer>().material.color);
            Assert.AreEqual(Color.blue, cityCorner.gameObject.GetComponent<MeshRenderer>().material.color);
            Assert.AreEqual(Color.gray, unknownCorner.gameObject.GetComponent<MeshRenderer>().material.color);
        }

        [Test]
        public void CornerSpawner_VertexIndex_CalculatesCorrectPositions()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();
            hex.transform.position = Vector3.zero; // Center at origin for predictable positions

            // Act
            _cornerSpawner.AddCorner(hex, 0, "settlement"); // top-right
            _cornerSpawner.AddCorner(hex, 3, "settlement"); // bottom-left

            var cornerInstances = _cornerSpawner.GetCornerInstances();

            // Assert
            var topRightCorner = cornerInstances.First(c => c.cornerState.vertexIndex == 0);
            var bottomLeftCorner = cornerInstances.First(c => c.cornerState.vertexIndex == 3);

            // Top-right should have positive x and z
            Assert.Greater(topRightCorner.gameObject.transform.position.x, 0);
            Assert.Greater(topRightCorner.gameObject.transform.position.z, 0);

            // Bottom-left should have negative x and z
            Assert.Less(bottomLeftCorner.gameObject.transform.position.x, 0);
            Assert.Less(bottomLeftCorner.gameObject.transform.position.z, 0);
        }

        [Test]
        public void CornerSpawner_InvalidVertexIndex_UsesDefault()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Act - Use invalid vertex index
            _cornerSpawner.AddCorner(hex, 10, "settlement"); // Invalid index

            var cornerInstance = _cornerSpawner.GetCornerInstances().First();

            // Assert - Should not crash and should create corner at default position
            Assert.IsNotNull(cornerInstance.gameObject);
            Assert.AreEqual(0, cornerInstance.cornerState.vertexIndex); // Should default to 0
        }

        [Test]
        public void CornerSpawner_MeshGeneration_CreatesCorrectMeshes()
        {
            // Arrange
            CreateTestHexes();
            var hex = _hexSpawner.GetAllHexes().First();

            // Act
            _cornerSpawner.AddCorner(hex, 0, "settlement");
            _cornerSpawner.AddCorner(hex, 1, "city");

            var cornerInstances = _cornerSpawner.GetCornerInstances();

            // Assert
            var settlementCorner = cornerInstances.First(c => c.cornerState.cornerType == "settlement");
            var cityCorner = cornerInstances.First(c => c.cornerState.cornerType == "city");

            var settlementMesh = settlementCorner.gameObject.GetComponent<MeshFilter>().mesh;
            var cityMesh = cityCorner.gameObject.GetComponent<MeshFilter>().mesh;

            Assert.IsNotNull(settlementMesh);
            Assert.IsNotNull(cityMesh);
            Assert.IsTrue(settlementMesh.vertices.Length > 0);
            Assert.IsTrue(cityMesh.vertices.Length > 0);

            // Settlement should have fewer vertices (pyramid) than city (cube)
            Assert.Less(settlementMesh.vertices.Length, cityMesh.vertices.Length);
        }

        /// <summary>
        /// Helper method to create test hexes for corner testing.
        /// </summary>
        private void CreateTestHexes()
        {
            // Create a simple 2x2 hex grid for testing
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    var hexGo = new GameObject($"Hex_{col}_{row}");
                    var hex = hexGo.AddComponent<Hex>();
                    
                    hex.hexState = new Hex.HexState
                    {
                        Col = col,
                        Row = row,
                        HexType = "grass",
                        Rotation = 0
                    };

                    // Position hexes in a grid pattern
                    float xOffset = col * 1.0f;
                    float zOffset = row * 1.0f;
                    if (row % 2 == 1) xOffset += 0.5f; // Offset for odd rows
                    
                    hex.transform.position = new Vector3(xOffset, 0, zOffset);
                }
            }
        }
    }
}
