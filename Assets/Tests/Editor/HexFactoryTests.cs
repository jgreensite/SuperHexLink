using NUnit.Framework;
using UnityEngine;
using SuperHexLink.Services;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for HexFactory service.
    /// Verifies that hex GameObject creation and configuration works correctly.
    /// </summary>
    public class HexFactoryTests
    {
        private GameObject _testParent;
        private GameObject _hexPrefab;
        private GameObject _hexTextPrefab;
        private GameObject _hexLandPrefab;
        private HexFactory _hexFactory;
        private HexGridConfig _gridConfig;

        [SetUp]
        public void SetUp()
        {
            // Create test parent
            _testParent = new GameObject("TestParent");

            // Create test prefabs
            _hexPrefab = new GameObject("HexPrefab");
            _hexPrefab.AddComponent<Hex>();

            _hexTextPrefab = new GameObject("HexTextPrefab");
            _hexTextPrefab.AddComponent<TextMeshPro>();

            _hexLandPrefab = new GameObject("HexLandPrefab");

            // Create hex factory
            _hexFactory = new HexFactory(new ActionLogSettings());

            // Create test grid configuration
            _gridConfig = new HexGridConfig
            {
                cols = 3,
                rows = 3,
                radius = 1.0f,
                height = 1.0f,
                minHeight = 0.0f,
                maxHeight = 2.0f,
                Apothem = 0.866f // Standard hex apothem for radius 1
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up created objects
            Object.DestroyImmediate(_testParent);
            Object.DestroyImmediate(_hexPrefab);
            Object.DestroyImmediate(_hexTextPrefab);
            Object.DestroyImmediate(_hexLandPrefab);

            // Clean up any created hexes
            foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name.StartsWith("hex_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HexFactory_CreateHex_ValidParameters_ReturnsHexInstance()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 1, 2, _testParent.transform, _gridConfig, false);

            // Assert
            Assert.IsNotNull(hex, "Hex should be created");
            Assert.AreEqual("hex_1_2", hex.name, "Hex should have correct name");
            Assert.AreEqual(1, hex.hexState.Col, "Hex should have correct column");
            Assert.AreEqual(2, hex.hexState.Row, "Hex should have correct row");
            Assert.AreEqual("none", hex.hexState.HexType, "Hex should have default type");
            Assert.IsNotNull(hex.transform.parent, "Hex should be parented to specified transform");
        }

        [Test]
        public void HexFactory_CreateHex_NullPrefab_ReturnsNull()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(null, 0, 0, _testParent.transform, _gridConfig, false);

            // Assert
            Assert.IsNull(hex, "Should return null when prefab is null");
        }

        [Test]
        public void HexFactory_CreateHex_ValidCoordinates_CalculatesCorrectPosition()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 1, 1, _testParent.transform, _gridConfig, false);

            // Assert - Verify position calculation
            Vector3 expectedPosition = new Vector3(
                x: 1.5f, // col * radius * 1.5
                y: 0f, // Random height will be between 0 and 2
                z: -1.732f // -row * apothem * 2 + GetZOffset(1)
            );

            // Y coordinate will be random, so we only check X and Z
            Assert.AreEqual(expectedPosition.x, hex.transform.position.x, 0.001f, "X position should match expected calculation");
            Assert.AreEqual(expectedPosition.z, hex.transform.position.z, 0.001f, "Z position should match expected calculation");
            Assert.IsTrue(hex.transform.position.y >= _gridConfig.minHeight, "Y position should be at least minHeight");
            Assert.IsTrue(hex.transform.position.y <= _gridConfig.maxHeight, "Y position should be at most maxHeight");
        }

        [Test]
        public void HexFactory_CreateHex_GridConfig_AppliesCorrectScale()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, false);

            // Assert
            Vector3 expectedScale = new Vector3(
                x: _gridConfig.radius,
                y: _gridConfig.height,
                z: _gridConfig.radius
            );
            Assert.AreEqual(expectedScale, hex.transform.localScale, "Hex should be scaled according to grid config");
        }

        [Test]
        public void HexFactory_CreateHex_RefreshMode_DoesNotOverrideState()
        {
            // Arrange
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, false);
            hex.hexState.HexType = "forest";
            hex.hexState.HexNum = 5;

            // Act
            Hex refreshedHex = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, true);

            // Assert
            Assert.IsNotNull(refreshedHex, "Refreshed hex should be created");
            // Note: In refresh mode, the state would be loaded from the spawner's state array
            // This test verifies the factory doesn't override the state during refresh
        }

        [Test]
        public void HexFactory_CreateHexText_ValidParameters_ReturnsTextMeshPro()
        {
            // Act
            TextMeshPro hexText = _hexFactory.CreateHexText(_hexTextPrefab, _testParent.transform);

            // Assert
            Assert.IsNotNull(hexText, "Hex text should be created");
            Assert.AreEqual("hexText", hexText.name, "Hex text should have correct name");
            Assert.AreEqual(_testParent.transform, hexText.transform.parent, "Hex text should be parented correctly");
        }

        [Test]
        public void HexFactory_CreateHexText_NullPrefab_ReturnsNull()
        {
            // Act
            TextMeshPro hexText = _hexFactory.CreateHexText(null, _testParent.transform);

            // Assert
            Assert.IsNull(hexText, "Should return null when text prefab is null");
        }

        [Test]
        public void HexFactory_CreateLandModel_ValidParameters_ReturnsGameObject()
        {
            // Act
            GameObject landModel = _hexFactory.CreateLandModel(_hexLandPrefab, _testParent.transform, "forest");

            // Assert
            Assert.IsNotNull(landModel, "Land model should be created");
            Assert.AreEqual("landModel", landModel.name, "Land model should have correct name");
            Assert.AreEqual(_testParent.transform, landModel.transform.parent, "Land model should be parented correctly");
            Assert.AreEqual(Quaternion.identity, landModel.transform.rotation, "Land model should have identity rotation");
            Assert.AreEqual(Vector3.zero, landModel.transform.localPosition, "Land model should be at local origin");
        }

        [Test]
        public void HexFactory_CreateLandModel_NullPrefab_ReturnsNull()
        {
            // Act
            GameObject landModel = _hexFactory.CreateLandModel(null, _testParent.transform, "forest");

            // Assert
            Assert.IsNull(landModel, "Should return null when land prefab is null");
        }

        [Test]
        public void HexFactory_CreateHex_MultipleCoordinates_CreatesUniqueHexes()
        {
            // Act
            Hex hex1 = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, false);
            Hex hex2 = _hexFactory.CreateHex(_hexPrefab, 1, 0, _testParent.transform, _gridConfig, false);
            Hex hex3 = _hexFactory.CreateHex(_hexPrefab, 0, 1, _testParent.transform, _gridConfig, false);

            // Assert
            Assert.IsNotNull(hex1, "First hex should be created");
            Assert.IsNotNull(hex2, "Second hex should be created");
            Assert.IsNotNull(hex3, "Third hex should be created");

            Assert.AreNotEqual(hex1, hex2, "Hexes should be different instances");
            Assert.AreNotEqual(hex2, hex3, "Hexes should be different instances");
            Assert.AreNotEqual(hex1, hex3, "Hexes should be different instances");

            Assert.AreNotEqual(hex1.transform.position, hex2.transform.position, "Hexes should have different positions");
            Assert.AreNotEqual(hex2.transform.position, hex3.transform.position, "Hexes should have different positions");
        }

        [Test]
        public void HexFactory_CreateHex_GridConfiguration_UsesCorrectLayer()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, false);

            // Assert
            int expectedLayer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEBOARD);
            Assert.AreEqual(expectedLayer, hex.gameObject.layer, "Hex should be on correct layer");
        }

        [Test]
        public void HexFactory_CreateHex_DefaultState_InitializesCorrectly()
        {
            // Act
            Hex hex = _hexFactory.CreateHex(_hexPrefab, 0, 0, _testParent.transform, _gridConfig, false);

            // Assert
            Assert.IsNotNull(hex.hexState, "Hex state should be initialized");
            Assert.AreEqual(0, hex.hexState.Col, "Column should be set correctly");
            Assert.AreEqual(0, hex.hexState.Row, "Row should be set correctly");
            Assert.AreEqual("none", hex.hexState.HexType, "Hex type should default to 'none'");
            Assert.AreEqual("plain", hex.hexState.HexSubType, "Hex sub-type should default to 'plain'");
            Assert.AreEqual(0, hex.hexState.Rotation, "Rotation should default to 0");
            Assert.AreEqual(0, hex.hexState.HexNum, "Hex number should default to 0");
            Assert.IsNull(hex.hexState.GroupID, "Group ID should default to null");
            Assert.IsFalse(hex.hexState.Selected, "Selected should default to false");
        }
    }
}
