using NUnit.Framework;
using UnityEngine;
using System.Linq;
using Sirenix.Serialization;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for EdgeSpawner save/load reconstruction functionality.
    /// Verifies that edges can be saved and reconstructed correctly.
    /// </summary>
    public class EdgeSpawnerStateTests
    {
        private GameObject _testGameObject;
        private EdgeSpawner _edgeSpawner;
        private GameObject _hexSpawnerGo;
        private HexSpawner _hexSpawner;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObjects
            _testGameObject = new GameObject("TestEdgeSpawner");
            _edgeSpawner = _testGameObject.AddComponent<EdgeSpawner>();

            _hexSpawnerGo = new GameObject("TestHexSpawner");
            _hexSpawner = _hexSpawnerGo.AddComponent<HexSpawner>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test GameObjects
            if (_testGameObject != null) Object.DestroyImmediate(_testGameObject);
            if (_hexSpawnerGo != null) Object.DestroyImmediate(_hexSpawnerGo);

            // Clean up any spawned edges
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("Edge_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void EdgeSpawner_InitialState_IsEmpty()
        {
            // Arrange & Act
            _edgeSpawner.BuildMe(false);

            // Assert
            Assert.IsNotNull(_edgeSpawner.State);
            Assert.IsNotNull(_edgeSpawner.State.edges);
            Assert.AreEqual(0, _edgeSpawner.State.edges.Count);
            Assert.AreEqual(0, _edgeSpawner.GetEdgeInstances().Count);
        }

        [Test]
        public void EdgeSpawner_AddEdge_CreatesEdgeInstance()
        {
            // Arrange
            CreateTestHexes();

            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            // Act
            _edgeSpawner.AddEdge(hex1, hex2, "road");

            // Assert
            Assert.AreEqual(1, _edgeSpawner.State.edges.Count);
            Assert.AreEqual(1, _edgeSpawner.GetEdgeInstances().Count);

            var edgeState = _edgeSpawner.State.edges.First();
            Assert.AreEqual(hex1.hexState.Col, edgeState.hex1Col);
            Assert.AreEqual(hex1.hexState.Row, edgeState.hex1Row);
            Assert.AreEqual(hex2.hexState.Col, edgeState.hex2Col);
            Assert.AreEqual(hex2.hexState.Row, edgeState.hex2Row);
            Assert.AreEqual("road", edgeState.edgeType);
        }

        [Test]
        public void EdgeSpawner_BuildMe_ReconstructsEdgesFromState()
        {
            // Arrange
            CreateTestHexes();
            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            // Add an edge to create state
            _edgeSpawner.AddEdge(hex1, hex2, "road");
            var originalState = _edgeSpawner.State;

            // Clear and rebuild
            _edgeSpawner.Clear();
            Assert.AreEqual(0, _edgeSpawner.GetEdgeInstances().Count);

            // Act
            _edgeSpawner.BuildMe(false);

            // Assert
            Assert.AreEqual(1, _edgeSpawner.GetEdgeInstances().Count);
            Assert.AreEqual(originalState.edges.Count, _edgeSpawner.State.edges.Count);
            
            var reconstructedEdge = _edgeSpawner.GetEdgeInstances().First();
            Assert.IsNotNull(reconstructedEdge.gameObject);
            Assert.IsTrue(reconstructedEdge.gameObject.name.StartsWith("Edge_road_"));
        }

        [Test]
        public void EdgeSpawner_BuildMeWithRefresh_UpdatesExistingEdges()
        {
            // Arrange
            CreateTestHexes();
            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            _edgeSpawner.AddEdge(hex1, hex2, "road");
            var edgeInstance = _edgeSpawner.GetEdgeInstances().First();

            // Act
            _edgeSpawner.BuildMe(true); // Refresh mode

            // Assert
            Assert.AreEqual(1, _edgeSpawner.GetEdgeInstances().Count);
            Assert.AreSame(edgeInstance, _edgeSpawner.GetEdgeInstances().First());
        }

        [Test]
        public void EdgeSpawner_Clear_RemovesAllEdges()
        {
            // Arrange
            CreateTestHexes();
            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            _edgeSpawner.AddEdge(hex1, hex2, "road");
            _edgeSpawner.AddEdge(hex1, hex2, "bridge");

            Assert.AreEqual(2, _edgeSpawner.GetEdgeInstances().Count);

            // Act
            _edgeSpawner.Clear();

            // Assert
            Assert.AreEqual(0, _edgeSpawner.GetEdgeInstances().Count);
            // State is preserved
            Assert.AreEqual(2, _edgeSpawner.State.edges.Count);
        }

        [Test]
        public void EdgeSpawner_AddMultipleEdges_HandlesCorrectly()
        {
            // Arrange
            CreateTestHexes();
            var hexes = _hexSpawner.GetAllHexes().Take(3).ToList();

            // Act
            _edgeSpawner.AddEdge(hexes[0], hexes[1], "road");
            _edgeSpawner.AddEdge(hexes[1], hexes[2], "bridge");
            _edgeSpawner.AddEdge(hexes[0], hexes[2], "road");

            // Assert
            Assert.AreEqual(3, _edgeSpawner.State.edges.Count);
            Assert.AreEqual(3, _edgeSpawner.GetEdgeInstances().Count);

            var roadEdges = _edgeSpawner.State.edges.Where(e => e.edgeType == "road").ToList();
            var bridgeEdges = _edgeSpawner.State.edges.Where(e => e.edgeType == "bridge").ToList();

            Assert.AreEqual(2, roadEdges.Count);
            Assert.AreEqual(1, bridgeEdges.Count);
        }

        [Test]
        public void EdgeSpawner_EdgeState_SerializationWorks()
        {
            // Arrange
            var edgeState = new EdgeSpawner.EdgeState
            {
                hex1Col = 1,
                hex1Row = 2,
                hex2Col = 3,
                hex2Row = 4,
                edgeType = "road"
            };

            // Act - Simulate serialization
            var serialized = SerializationUtility.SerializeValue(edgeState, DataFormat.Binary);
            var deserialized = SerializationUtility.DeserializeValue<EdgeSpawner.EdgeState>(serialized, DataFormat.Binary);

            // Assert
            Assert.AreEqual(edgeState.hex1Col, deserialized.hex1Col);
            Assert.AreEqual(edgeState.hex1Row, deserialized.hex1Row);
            Assert.AreEqual(edgeState.hex2Col, deserialized.hex2Col);
            Assert.AreEqual(edgeState.hex2Row, deserialized.hex2Row);
            Assert.AreEqual(edgeState.edgeType, deserialized.edgeType);
        }

        [Test]
        public void EdgeSpawner_BuildMeWithoutHexSpawner_LogsWarning()
        {
            // Arrange - No HexSpawner in scene
            Object.DestroyImmediate(_hexSpawnerGo);

            // Add some state to try to reconstruct
            _edgeSpawner.State = new EdgeSpawner.EdgeSpawnerState();
            _edgeSpawner.State.edges.Add(new EdgeSpawner.EdgeState
            {
                hex1Col = 0,
                hex1Row = 0,
                hex2Col = 1,
                hex2Row = 0,
                edgeType = "road"
            });

            // Act & Assert - Should not crash, just log warning
            Assert.DoesNotThrow(() => _edgeSpawner.BuildMe(false));
            Assert.AreEqual(0, _edgeSpawner.GetEdgeInstances().Count);
        }

        [Test]
        public void EdgeSpawner_EdgeInstance_HasCorrectReferences()
        {
            // Arrange
            CreateTestHexes();
            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            // Act
            _edgeSpawner.AddEdge(hex1, hex2, "road");
            var edgeInstance = _edgeSpawner.GetEdgeInstances().First();

            // Assert
            Assert.IsNotNull(edgeInstance.gameObject);
            Assert.IsNotNull(edgeInstance.edgeState);
            Assert.AreSame(hex1, edgeInstance.hex1);
            Assert.AreSame(hex2, edgeInstance.hex2);
            Assert.AreEqual("road", edgeInstance.edgeState.edgeType);
        }

        [Test]
        public void EdgeSpawner_DifferentEdgeTypes_HaveDifferentMaterials()
        {
            // Arrange
            CreateTestHexes();
            var hex1 = _hexSpawner.GetAllHexes().First();
            var hex2 = _hexSpawner.GetAllHexes().Skip(1).First();

            // Act
            _edgeSpawner.AddEdge(hex1, hex2, "road");
            _edgeSpawner.AddEdge(hex1, hex2, "bridge");
            _edgeSpawner.AddEdge(hex1, hex2, "unknown");

            var edgeInstances = _edgeSpawner.GetEdgeInstances();

            // Assert
            var roadEdge = edgeInstances.First(e => e.edgeState.edgeType == "road");
            var bridgeEdge = edgeInstances.First(e => e.edgeState.edgeType == "bridge");
            var unknownEdge = edgeInstances.First(e => e.edgeState.edgeType == "unknown");

            Assert.AreEqual(Color.brown, roadEdge.gameObject.GetComponent<MeshRenderer>().material.color);
            Assert.AreEqual(Color.gray, bridgeEdge.gameObject.GetComponent<MeshRenderer>().material.color);
            Assert.AreEqual(Color.white, unknownEdge.gameObject.GetComponent<MeshRenderer>().material.color);
        }

        /// <summary>
        /// Helper method to create test hexes for edge testing.
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
