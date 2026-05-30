using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SuperHexLink.Services;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for HexRegistry service.
    /// Verifies O(1) hex lookup functionality and registry management.
    /// </summary>
    public class HexRegistryTests
    {
        private HexRegistry _hexRegistry;
        private GameObject _testParent;

        [SetUp]
        public void SetUp()
        {
            _hexRegistry = new HexRegistry(new ActionLogSettings());
            _testParent = new GameObject("TestParent");
        }

        [TearDown]
        public void TearDown()
        {
            _hexRegistry.Clear();
            Object.DestroyImmediate(_testParent);
        }

        [Test]
        public void HexRegistry_GetHex_EmptyRegistry_ReturnsNull()
        {
            // Act
            Hex hex = _hexRegistry.GetHex(0, 0);

            // Assert
            Assert.IsNull(hex, "Should return null for empty registry");
        }

        [Test]
        public void HexRegistry_RegisterHex_ValidHex_ReturnsTrue()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };

            // Act
            bool result = _hexRegistry.RegisterHex(hex);

            // Assert
            Assert.IsTrue(result, "Should successfully register hex");
            Assert.AreEqual(1, _hexRegistry.GetHexCount(), "Should have 1 hex registered");
        }

        [Test]
        public void HexRegistry_RegisterHex_NullHex_ReturnsFalse()
        {
            // Act
            bool result = _hexRegistry.RegisterHex(null);

            // Assert
            Assert.IsFalse(result, "Should return false for null hex");
            Assert.AreEqual(0, _hexRegistry.GetHexCount(), "Should have 0 hexes registered");
        }

        [Test]
        public void HexRegistry_RegisterHex_NullState_ReturnsFalse()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = null;

            // Act
            bool result = _hexRegistry.RegisterHex(hex);

            // Assert
            Assert.IsFalse(result, "Should return false for hex with null state");
            Assert.AreEqual(0, _hexRegistry.GetHexCount(), "Should have 0 hexes registered");
        }

        [Test]
        public void HexRegistry_RegisterHex_DuplicateCoordinates_ReturnsFalse()
        {
            // Arrange
            GameObject hexGo1 = new GameObject("TestHex1");
            GameObject hexGo2 = new GameObject("TestHex2");
            Hex hex1 = hexGo1.AddComponent<Hex>();
            Hex hex2 = hexGo2.AddComponent<Hex>();
            
            hex1.hexState = new Hex.HexState { Col = 1, Row = 2 };
            hex2.hexState = new Hex.HexState { Col = 1, Row = 2 };

            // Act
            bool result1 = _hexRegistry.RegisterHex(hex1);
            bool result2 = _hexRegistry.RegisterHex(hex2);

            // Assert
            Assert.IsTrue(result1, "First registration should succeed");
            Assert.IsFalse(result2, "Duplicate registration should fail");
            Assert.AreEqual(1, _hexRegistry.GetHexCount(), "Should have 1 hex registered");
        }

        [Test]
        public void HexRegistry_GetHex_RegisteredHex_ReturnsCorrectHex()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 3, Row = 4 };
            _hexRegistry.RegisterHex(hex);

            // Act
            Hex retrievedHex = _hexRegistry.GetHex(3, 4);

            // Assert
            Assert.IsNotNull(retrievedHex, "Should return registered hex");
            Assert.AreSame(hex, retrievedHex, "Should return the same hex instance");
        }

        [Test]
        public void HexRegistry_GetHex_UnregisteredCoordinates_ReturnsNull()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            _hexRegistry.RegisterHex(hex);

            // Act
            Hex retrievedHex = _hexRegistry.GetHex(9, 9);

            // Assert
            Assert.IsNull(retrievedHex, "Should return null for unregistered coordinates");
        }

        [Test]
        public void HexRegistry_UnregisterHex_RegisteredHex_ReturnsTrue()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            _hexRegistry.RegisterHex(hex);

            // Act
            bool result = _hexRegistry.UnregisterHex(hex);

            // Assert
            Assert.IsTrue(result, "Should successfully unregister hex");
            Assert.AreEqual(0, _hexRegistry.GetHexCount(), "Should have 0 hexes after unregistration");
            Assert.IsNull(_hexRegistry.GetHex(1, 2), "Hex should be removed from lookup");
        }

        [Test]
        public void HexRegistry_UnregisterHex_UnregisteredHex_ReturnsFalse()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };

            // Act
            bool result = _hexRegistry.UnregisterHex(hex);

            // Assert
            Assert.IsFalse(result, "Should return false for unregistered hex");
        }

        [Test]
        public void HexRegistry_GetAllHexes_MultipleHexes_ReturnsAllHexes()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                GameObject hexGo = new GameObject($"TestHex{i}");
                Hex hex = hexGo.AddComponent<Hex>();
                hex.hexState = new Hex.HexState { Col = i, Row = i };
                _hexRegistry.RegisterHex(hex);
            }

            // Act
            System.Collections.Generic.IReadOnlyList<Hex> allHexes = _hexRegistry.GetAllHexes();

            // Assert
            Assert.AreEqual(3, allHexes.Count, "Should return all registered hexes");
            Assert.IsTrue(allHexes.All(h => h != null), "All hexes should be non-null");
        }

        [Test]
        public void HexRegistry_GetHexesInRegion_ValidRegion_ReturnsHexesInRegion()
        {
            // Arrange
            for (int col = 0; col < 5; col++)
            {
                for (int row = 0; row < 5; row++)
                {
                    GameObject hexGo = new GameObject($"TestHex_{col}_{row}");
                    Hex hex = hexGo.AddComponent<Hex>();
                    hex.hexState = new Hex.HexState { Col = col, Row = row };
                    _hexRegistry.RegisterHex(hex);
                }
            }

            // Act
            System.Collections.Generic.IEnumerable<Hex> hexesInRegion = _hexRegistry.GetHexesInRegion(1, 3, 1, 3);

            // Assert
            int count = hexesInRegion.Count();
            Assert.AreEqual(9, count, "Should return 9 hexes in 3x3 region");
        }

        [Test]
        public void HexRegistry_GetHexesInRange_ValidRange_ReturnsHexesInRange()
        {
            // Arrange
            GameObject centerHexGo = new GameObject("CenterHex");
            Hex centerHex = centerHexGo.AddComponent<Hex>();
            centerHex.hexState = new Hex.HexState { Col = 2, Row = 2 };
            _hexRegistry.RegisterHex(centerHex);

            // Add hexes at distance 1
            GameObject hexGo1 = new GameObject("Hex1");
            Hex hex1 = hexGo1.AddComponent<Hex>();
            hex1.hexState = new Hex.HexState { Col = 1, Row = 2 };
            _hexRegistry.RegisterHex(hex1);

            GameObject hexGo2 = new GameObject("Hex2");
            Hex hex2 = hexGo2.AddComponent<Hex>();
            hex2.hexState = new Hex.HexState { Col = 3, Row = 2 };
            _hexRegistry.RegisterHex(hex2);

            // Add hex at distance 2
            GameObject hexGo3 = new GameObject("Hex3");
            Hex hex3 = hexGo3.AddComponent<Hex>();
            hex3.hexState = new Hex.HexState { Col = 4, Row = 2 };
            _hexRegistry.RegisterHex(hex3);

            // Act
            System.Collections.Generic.IEnumerable<Hex> hexesInRange = _hexRegistry.GetHexesInRange(2, 2, 1);

            // Assert
            int count = hexesInRange.Count();
            Assert.AreEqual(3, count, "Should return 3 hexes within distance 1");
        }

        [Test]
        public void HexRegistry_HasHex_RegisteredHex_ReturnsTrue()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            _hexRegistry.RegisterHex(hex);

            // Act
            bool hasHex = _hexRegistry.HasHex(1, 2);

            // Assert
            Assert.IsTrue(hasHex, "Should return true for registered hex");
        }

        [Test]
        public void HexRegistry_HasHex_UnregisteredHex_ReturnsFalse()
        {
            // Act
            bool hasHex = _hexRegistry.HasHex(9, 9);

            // Assert
            Assert.IsFalse(hasHex, "Should return false for unregistered hex");
        }

        [Test]
        public void HexRegistry_Clear_MultipleHexes_ClearsAll()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                GameObject hexGo = new GameObject($"TestHex{i}");
                Hex hex = hexGo.AddComponent<Hex>();
                hex.hexState = new Hex.HexState { Col = i, Row = i };
                _hexRegistry.RegisterHex(hex);
            }

            // Act
            _hexRegistry.Clear();

            // Assert
            Assert.AreEqual(0, _hexRegistry.GetHexCount(), "Should have 0 hexes after clear");
            Assert.IsNull(_hexRegistry.GetHex(0, 0), "Should return null after clear");
        }

        [Test]
        public void HexRegistry_ValidateConsistency_ValidRegistry_ReturnsTrue()
        {
            // Arrange
            for (int i = 0; i < 3; i++)
            {
                GameObject hexGo = new GameObject($"TestHex{i}");
                Hex hex = hexGo.AddComponent<Hex>();
                hex.hexState = new Hex.HexState { Col = i, Row = i };
                _hexRegistry.RegisterHex(hex);
            }

            // Act
            bool isValid = _hexRegistry.ValidateConsistency();

            // Assert
            Assert.IsTrue(isValid, "Valid registry should pass consistency check");
        }

        [Test]
        public void HexRegistry_GetHexesByType_ValidType_ReturnsHexesOfType()
        {
            // Arrange
            GameObject hexGo1 = new GameObject("ForestHex");
            Hex forestHex = hexGo1.AddComponent<Hex>();
            forestHex.hexState = new Hex.HexState { Col = 1, Row = 1, HexType = "forest" };
            _hexRegistry.RegisterHex(forestHex);

            GameObject hexGo2 = new GameObject("WaterHex");
            Hex waterHex = hexGo2.AddComponent<Hex>();
            waterHex.hexState = new Hex.HexState { Col = 2, Row = 2, HexType = "water" };
            _hexRegistry.RegisterHex(waterHex);

            GameObject hexGo3 = new GameObject("ForestHex2");
            Hex forestHex2 = hexGo3.AddComponent<Hex>();
            forestHex2.hexState = new Hex.HexState { Col = 3, Row = 3, HexType = "forest" };
            _hexRegistry.RegisterHex(forestHex2);

            // Act
            System.Collections.Generic.IEnumerable<Hex> forestHexes = _hexRegistry.GetHexesByType("forest");

            // Assert
            int count = forestHexes.Count();
            Assert.AreEqual(2, count, "Should return 2 forest hexes");
        }

        [Test]
        public void HexRegistry_GetSelectedHexes_SelectedHexes_ReturnsSelectedHexes()
        {
            // Arrange
            GameObject hexGo1 = new GameObject("SelectedHex");
            Hex selectedHex = hexGo1.AddComponent<Hex>();
            selectedHex.hexState = new Hex.HexState { Col = 1, Row = 1, Selected = true };
            _hexRegistry.RegisterHex(selectedHex);

            GameObject hexGo2 = new GameObject("UnselectedHex");
            Hex unselectedHex = hexGo2.AddComponent<Hex>();
            unselectedHex.hexState = new Hex.HexState { Col = 2, Row = 2, Selected = false };
            _hexRegistry.RegisterHex(unselectedHex);

            // Act
            System.Collections.Generic.IEnumerable<Hex> selectedHexes = _hexRegistry.GetSelectedHexes();

            // Assert
            int count = selectedHexes.Count();
            Assert.AreEqual(1, count, "Should return 1 selected hex");
            Assert.AreSame(selectedHex, selectedHexes.First(), "Should return the selected hex");
        }

        [Test]
        public void HexRegistry_UpdateHexCoordinates_ValidHex_UpdatesCoordinates()
        {
            // Arrange
            GameObject hexGo = new GameObject("TestHex");
            Hex hex = hexGo.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            _hexRegistry.RegisterHex(hex);

            // Act
            bool result = _hexRegistry.UpdateHexCoordinates(hex, 5, 6);

            // Assert
            Assert.IsTrue(result, "Should successfully update coordinates");
            Assert.AreEqual(5, hex.hexState.Col, "Hex column should be updated");
            Assert.AreEqual(6, hex.hexState.Row, "Hex row should be updated");
            Assert.IsNull(_hexRegistry.GetHex(1, 2), "Old coordinates should be removed");
            Assert.AreSame(hex, _hexRegistry.GetHex(5, 6), "Hex should be found at new coordinates");
        }

        [Test]
        public void HexRegistry_UpdateHexCoordinates_NullHex_ReturnsFalse()
        {
            // Act
            bool result = _hexRegistry.UpdateHexCoordinates(null, 1, 2);

            // Assert
            Assert.IsFalse(result, "Should return false for null hex");
        }

        [Test]
        public void HexRegistry_Performance_LargeNumberOfHexes_OperatesEfficiently()
        {
            // Arrange - Register 1000 hexes
            for (int i = 0; i < 1000; i++)
            {
                GameObject hexGo = new GameObject($"TestHex{i}");
                Hex hex = hexGo.AddComponent<Hex>();
                hex.hexState = new Hex.HexState { Col = i % 50, Row = i / 50 };
                _hexRegistry.RegisterHex(hex);
            }

            // Act - Perform O(1) lookup operations
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                Hex hex = _hexRegistry.GetHex(i % 50, i / 50);
                Assert.IsNotNull(hex, $"Should find hex at ({i % 50}, {i / 50})");
            }
            
            stopwatch.Stop();

            // Assert - Should complete quickly (under 10ms for 100 lookups)
            Assert.Less(stopwatch.ElapsedMilliseconds, 10, "O(1) lookup should be very fast");
            Assert.AreEqual(1000, _hexRegistry.GetHexCount(), "Should have all 1000 hexes registered");
        }
    }
}
