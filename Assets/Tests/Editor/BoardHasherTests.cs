using NUnit.Framework;
using UnityEngine;
using System.Linq;
using SuperHexLink.Utils;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for BoardHasher deterministic board comparison functionality.
    /// Verifies that identical boards produce identical hashes and different boards produce different hashes.
    /// </summary>
    public class BoardHasherTests
    {
        private GameObject _hexSpawnerGo1;
        private GameObject _hexSpawnerGo2;
        private GameObject _edgeSpawnerGo1;
        private GameObject _edgeSpawnerGo2;
        private GameObject _cornerSpawnerGo1;
        private GameObject _cornerSpawnerGo2;
        
        private HexSpawner _hexSpawner1;
        private HexSpawner _hexSpawner2;
        private EdgeSpawner _edgeSpawner1;
        private EdgeSpawner _edgeSpawner2;
        private CornerSpawner _cornerSpawner1;
        private CornerSpawner _cornerSpawner2;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObjects
            _hexSpawnerGo1 = new GameObject("HexSpawner1");
            _hexSpawner1 = _hexSpawnerGo1.AddComponent<HexSpawner>();
            
            _hexSpawnerGo2 = new GameObject("HexSpawner2");
            _hexSpawner2 = _hexSpawnerGo2.AddComponent<HexSpawner>();
            
            _edgeSpawnerGo1 = new GameObject("EdgeSpawner1");
            _edgeSpawner1 = _edgeSpawnerGo1.AddComponent<EdgeSpawner>();
            
            _edgeSpawnerGo2 = new GameObject("EdgeSpawner2");
            _edgeSpawner2 = _edgeSpawnerGo2.AddComponent<EdgeSpawner>();
            
            _cornerSpawnerGo1 = new GameObject("CornerSpawner1");
            _cornerSpawner1 = _cornerSpawnerGo1.AddComponent<CornerSpawner>();
            
            _cornerSpawnerGo2 = new GameObject("CornerSpawner2");
            _cornerSpawner2 = _cornerSpawnerGo2.AddComponent<CornerSpawner>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test GameObjects
            Object.DestroyImmediate(_hexSpawnerGo1);
            Object.DestroyImmediate(_hexSpawnerGo2);
            Object.DestroyImmediate(_edgeSpawnerGo1);
            Object.DestroyImmediate(_edgeSpawnerGo2);
            Object.DestroyImmediate(_cornerSpawnerGo1);
            Object.DestroyImmediate(_cornerSpawnerGo2);

            // Clean up any spawned objects
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("Hex_") || go.name.StartsWith("Edge_") || go.name.StartsWith("Corner_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void BoardHasher_ComputeHash_EmptyBoards_ReturnsSameHash()
        {
            // Arrange
            InitializeEmptySpawners();

            // Act
            var hash1 = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);
            var hash2 = BoardHasher.ComputeHash(_hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.AreEqual(hash1, hash2);
            Assert.IsNotEmpty(hash1);
            Assert.AreEqual(64, hash1.Length); // SHA-256 hash length
        }

        [Test]
        public void BoardHasher_ComputeHash_IdenticalBoards_ReturnsSameHash()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act
            var hash1 = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);
            var hash2 = BoardHasher.ComputeHash(_hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void BoardHasher_ComputeHash_DifferentHexes_ReturnsDifferentHashes()
        {
            // Arrange
            CreateIdenticalBoards();
            // Modify one hex in the second board
            var hex = _hexSpawner2.GetAllHexes().First();
            hex.hexState.HexType = "mountain";

            // Act
            var hash1 = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);
            var hash2 = BoardHasher.ComputeHash(_hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void BoardHasher_ComputeHash_DifferentEdges_ReturnsDifferentHashes()
        {
            // Arrange
            CreateIdenticalBoards();
            // Add an extra edge to the second board
            var hexes = _hexSpawner2.GetAllHexes().ToList();
            _edgeSpawner2.AddEdge(hexes[0], hexes[1], "bridge");

            // Act
            var hash1 = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);
            var hash2 = BoardHasher.ComputeHash(_hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void BoardHasher_ComputeHash_DifferentCorners_ReturnsDifferentHashes()
        {
            // Arrange
            CreateIdenticalBoards();
            // Add an extra corner to the second board
            var hex = _hexSpawner2.GetAllHexes().First();
            _cornerSpawner2.AddCorner(hex, 1, "city");

            // Act
            var hash1 = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);
            var hash2 = BoardHasher.ComputeHash(_hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void BoardHasher_ComputeHexHash_HexesOnly_ReturnsCorrectHash()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act
            var hexHash1 = BoardHasher.ComputeHexHash(_hexSpawner1);
            var hexHash2 = BoardHasher.ComputeHexHash(_hexSpawner2);

            // Assert
            Assert.AreEqual(hexHash1, hexHash2);
            Assert.IsNotEmpty(hexHash1);
            Assert.AreEqual(64, hexHash1.Length);
        }

        [Test]
        public void BoardHasher_ComputeEdgeHash_EdgesOnly_ReturnsCorrectHash()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act
            var edgeHash1 = BoardHasher.ComputeEdgeHash(_edgeSpawner1);
            var edgeHash2 = BoardHasher.ComputeEdgeHash(_edgeSpawner2);

            // Assert
            Assert.AreEqual(edgeHash1, edgeHash2);
            Assert.IsNotEmpty(edgeHash1);
            Assert.AreEqual(64, edgeHash1.Length);
        }

        [Test]
        public void BoardHasher_ComputeCornerHash_CornersOnly_ReturnsCorrectHash()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act
            var cornerHash1 = BoardHasher.ComputeCornerHash(_cornerSpawner1);
            var cornerHash2 = BoardHasher.ComputeCornerHash(_cornerSpawner2);

            // Assert
            Assert.AreEqual(cornerHash1, cornerHash2);
            Assert.IsNotEmpty(cornerHash1);
            Assert.AreEqual(64, cornerHash1.Length);
        }

        [Test]
        public void BoardHasher_CompareBoards_IdenticalBoards_ReturnsIdentical()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act
            var comparison = BoardHasher.CompareBoards(_hexSpawner1, _edgeSpawner1, _cornerSpawner1,
                                                     _hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.IsTrue(comparison.AreIdentical);
            Assert.IsTrue(comparison.HexesMatch);
            Assert.IsTrue(comparison.EdgesMatch);
            Assert.IsTrue(comparison.CornersMatch);
            Assert.AreEqual(comparison.Board1Hash, comparison.Board2Hash);
            Assert.IsNull(comparison.Error);
        }

        [Test]
        public void BoardHasher_CompareBoards_DifferentBoards_ReturnsDifferences()
        {
            // Arrange
            CreateIdenticalBoards();
            // Modify hexes in second board
            var hex = _hexSpawner2.GetAllHexes().First();
            hex.hexState.HexType = "mountain";
            // Add extra edge to second board
            var hexes = _hexSpawner2.GetAllHexes().ToList();
            _edgeSpawner2.AddEdge(hexes[0], hexes[1], "bridge");

            // Act
            var comparison = BoardHasher.CompareBoards(_hexSpawner1, _edgeSpawner1, _cornerSpawner1,
                                                     _hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Assert
            Assert.IsFalse(comparison.AreIdentical);
            Assert.IsFalse(comparison.HexesMatch);
            Assert.IsFalse(comparison.EdgesMatch);
            Assert.IsTrue(comparison.CornersMatch); // Corners should still match
            Assert.AreNotEqual(comparison.Board1Hash, comparison.Board2Hash);
            Assert.IsNull(comparison.Error);
        }

        [Test]
        public void BoardHasher_ValidateBoardIntegrity_ValidHash_ReturnsTrue()
        {
            // Arrange
            CreateIdenticalBoards();
            var expectedHash = BoardHasher.ComputeHash(_hexSpawner1, _edgeSpawner1, _cornerSpawner1);

            // Act
            var isValid = BoardHasher.ValidateBoardIntegrity(_hexSpawner1, _edgeSpawner1, _cornerSpawner1, expectedHash);

            // Assert
            Assert.IsTrue(isValid);
        }

        [Test]
        public void BoardHasher_ValidateBoardIntegrity_InvalidHash_ReturnsFalse()
        {
            // Arrange
            CreateIdenticalBoards();
            var wrongHash = "wrong_hash_value";

            // Act
            var isValid = BoardHasher.ValidateBoardIntegrity(_hexSpawner1, _edgeSpawner1, _cornerSpawner1, wrongHash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [Test]
        public void BoardHasher_ComputeHash_NullHexSpawner_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => BoardHasher.ComputeHash(null));
        }

        [Test]
        public void BoardHasher_ComputeEdgeHash_NullEdgeSpawner_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => BoardHasher.ComputeEdgeHash(null));
        }

        [Test]
        public void BoardHasher_ComputeCornerHash_NullCornerSpawner_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => BoardHasher.ComputeCornerHash(null));
        }

        [Test]
        public void BoardHasher_ValidateBoardIntegrity_NullExpectedHash_ThrowsException()
        {
            // Arrange
            CreateIdenticalBoards();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                BoardHasher.ValidateBoardIntegrity(_hexSpawner1, _edgeSpawner1, _cornerSpawner1, null));
        }

        [Test]
        public void BoardHasher_CompareBoards_GetSummary_ReturnsDetailedInfo()
        {
            // Arrange
            CreateIdenticalBoards();
            var comparison = BoardHasher.CompareBoards(_hexSpawner1, _edgeSpawner1, _cornerSpawner1,
                                                     _hexSpawner2, _edgeSpawner2, _cornerSpawner2);

            // Act
            var summary = comparison.GetSummary();

            // Assert
            Assert.IsNotEmpty(summary);
            Assert.IsTrue(summary.Contains("IDENTICAL"));
            Assert.IsTrue(summary.Contains("Board1:"));
            Assert.IsTrue(summary.Contains("Board2:"));
            Assert.IsTrue(summary.Contains("Hexes: MATCH"));
            Assert.IsTrue(summary.Contains("Edges: MATCH"));
            Assert.IsTrue(summary.Contains("Corners: MATCH"));
        }

        [Test]
        public void BoardHasher_DeterministicOrdering_SameBoardOrder_ReturnsSameHash()
        {
            // Arrange - Create board with multiple hexes in different order
            CreateBoardWithMultipleHexes(_hexSpawner1);
            CreateBoardWithMultipleHexes(_hexSpawner2);

            // Act
            var hash1 = BoardHasher.ComputeHexHash(_hexSpawner1);
            var hash2 = BoardHasher.ComputeHexHash(_hexSpawner2);

            // Assert
            Assert.AreEqual(hash1, hash2, "Hashes should be identical regardless of internal ordering");
        }

        [Test]
        public void BoardHasher_EdgeNormalization_EdgeOrder_ReturnsSameHash()
        {
            // Arrange
            CreateBoardWithMultipleHexes(_hexSpawner1);
            CreateBoardWithMultipleHexes(_hexSpawner2);
            
            var hexes1 = _hexSpawner1.GetAllHexes().ToList();
            var hexes2 = _hexSpawner2.GetAllHexes().ToList();
            
            // Add edges in different orders
            _edgeSpawner1.AddEdge(hexes1[0], hexes1[1], "road");
            _edgeSpawner1.AddEdge(hexes1[1], hexes1[2], "bridge");
            
            _edgeSpawner2.AddEdge(hexes2[1], hexes2[2], "bridge");
            _edgeSpawner2.AddEdge(hexes2[0], hexes2[1], "road");

            // Act
            var hash1 = BoardHasher.ComputeEdgeHash(_edgeSpawner1);
            var hash2 = BoardHasher.ComputeEdgeHash(_edgeSpawner2);

            // Assert
            Assert.AreEqual(hash1, hash2, "Edge hashes should be identical regardless of edge order");
        }

        /// <summary>
        /// Helper method to initialize empty spawners.
        /// </summary>
        private void InitializeEmptySpawners()
        {
            _hexSpawner1.State = new HexSpawner.HexSpawnerState();
            _hexSpawner1.State.hexes = new List<Hex.HexState>();
            
            _hexSpawner2.State = new HexSpawner.HexSpawnerState();
            _hexSpawner2.State.hexes = new List<Hex.HexState>();
            
            _edgeSpawner1.State = new EdgeSpawner.EdgeSpawnerState();
            _edgeSpawner1.State.edges = new List<EdgeSpawner.EdgeState>();
            
            _edgeSpawner2.State = new EdgeSpawner.EdgeSpawnerState();
            _edgeSpawner2.State.edges = new List<EdgeSpawner.EdgeState>();
            
            _cornerSpawner1.State = new CornerSpawner.CornerSpawnerState();
            _cornerSpawner1.State.corners = new List<CornerSpawner.CornerState>();
            
            _cornerSpawner2.State = new CornerSpawner.CornerSpawnerState();
            _cornerSpawner2.State.corners = new List<CornerSpawner.CornerState>();
        }

        /// <summary>
        /// Helper method to create identical boards on both spawners.
        /// </summary>
        private void CreateIdenticalBoards()
        {
            InitializeEmptySpawners();
            
            // Create identical hex states
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < 2; col++)
                {
                    var hexState = new Hex.HexState
                    {
                        Col = col,
                        Row = row,
                        HexType = "grass",
                        HexSubType = "plain",
                        Rotation = 0,
                        HexNum = row * 2 + col + 1,
                        GroupID = $"group_{row}",
                        Selected = false
                    };
                    
                    _hexSpawner1.State.hexes.Add(hexState);
                    _hexSpawner2.State.hexes.Add(hexState);
                }
            }
            
            // Create identical edge states
            var edgeState = new EdgeSpawner.EdgeState
            {
                hex1Col = 0,
                hex1Row = 0,
                hex2Col = 1,
                hex2Row = 0,
                edgeType = "road"
            };
            
            _edgeSpawner1.State.edges.Add(edgeState);
            _edgeSpawner2.State.edges.Add(edgeState);
            
            // Create identical corner states
            var cornerState = new CornerSpawner.CornerState
            {
                hexCol = 0,
                hexRow = 0,
                vertexIndex = 0,
                cornerType = "settlement"
            };
            
            _cornerSpawner1.State.corners.Add(cornerState);
            _cornerSpawner2.State.corners.Add(cornerState);
        }

        /// <summary>
        /// Helper method to create a board with multiple hexes.
        /// </summary>
        private void CreateBoardWithMultipleHexes(HexSpawner spawner)
        {
            spawner.State = new HexSpawner.HexSpawnerState();
            spawner.State.hexes = new List<Hex.HexState>();
            
            // Create hexes in random order to test sorting
            var hexes = new[]
            {
                new Hex.HexState { Col = 2, Row = 1, HexType = "forest", HexSubType = "dense", Rotation = 90, HexNum = 5, GroupID = "group1", Selected = false },
                new Hex.HexState { Col = 0, Row = 0, HexType = "grass", HexSubType = "plain", Rotation = 0, HexNum = 1, GroupID = "group0", Selected = false },
                new Hex.HexState { Col = 1, Row = 1, HexType = "mountain", HexSubType = "high", Rotation = 180, HexNum = 4, GroupID = "group1", Selected = false },
                new Hex.HexState { Col = 1, Row = 0, HexType = "water", HexSubType = "deep", Rotation = 270, HexNum = 2, GroupID = "group0", Selected = false },
                new Hex.HexState { Col = 0, Row = 1, HexType = "desert", HexSubType = "dry", Rotation = 45, HexNum = 3, GroupID = "group1", Selected = false }
            };
            
            spawner.State.hexes.AddRange(hexes);
        }
    }
}
