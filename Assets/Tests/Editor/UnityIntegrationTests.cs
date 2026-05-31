using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Integration;
using SuperHexLink.Simulation;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Tests for UnityGameStateBridge — verifies scene state → HeadlessGameState
    /// and HeadlessGameState → scene state conversions.
    /// </summary>
    [TestFixture]
    public class UnityIntegrationTests
    {
        private GameSpawner.GameSpawnerState _gameState;
        private HexSpawner.HexSpawnerState _hexSpawnerState;

        [SetUp]
        public void SetUp()
        {
            _gameState = new GameSpawner.GameSpawnerState();
            _hexSpawnerState = new HexSpawner.HexSpawnerState();
        }

        // ── ExtractFromScene ──────────────────────────────────────────────────────

        [Test]
        public void ExtractFromScene_DefaultState_ReturnsNonNullState()
        {
            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);
            Assert.IsNotNull(state.Players);
        }

        [Test]
        public void ExtractFromScene_DefaultState_HasCorrectPlayerCount()
        {
            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 3);
            Assert.AreEqual(3, state.Players.Count);
        }

        [Test]
        public void ExtractFromScene_DefaultGridConfig_HasCorrectDimensions()
        {
            // Default HexGridConfig.CreateDefault() is 7x7
            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);
            // Grid should contain 7*7 = 49 hexes
            Assert.AreEqual(49, state.HexGrid.Hexes.Count,
                "7x7 grid should produce 49 hex entries");
        }

        [Test]
        public void ExtractFromScene_WithForestHex_MapsToWood()
        {
            // Set up a 2x1 grid so we can test a specific hex type
            _gameState.hexGridConfig = new HexGridConfig { cols = 2, rows = 1 };
            _hexSpawnerState.hexes = new List<List<Hex.HexState>>
            {
                new List<Hex.HexState>
                {
                    new Hex.HexState { Col = 0, Row = 0, HexType = "forest", HexNum = 6 }
                }
            };

            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);

            var hexId = 0 * 1 + 0; // col * rows + row
            Assert.IsTrue(state.HexGrid.Hexes.ContainsKey(hexId));
            Assert.AreEqual(ResourceType.Wood, state.HexGrid.Hexes[hexId].ResourceType);
        }

        [Test]
        public void ExtractFromScene_WithForestHex_MapsProductionNumber()
        {
            _gameState.hexGridConfig = new HexGridConfig { cols = 1, rows = 1 };
            _hexSpawnerState.hexes = new List<List<Hex.HexState>>
            {
                new List<Hex.HexState>
                {
                    new Hex.HexState { Col = 0, Row = 0, HexType = "mountain", HexNum = 10 }
                }
            };

            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);

            var hexId = 0;
            Assert.AreEqual(10, state.HexGrid.Hexes[hexId].ProductionNumber);
            Assert.AreEqual(ResourceType.Ore, state.HexGrid.Hexes[hexId].ResourceType);
        }

        [Test]
        public void ExtractFromScene_AllResourceTypes_MappedCorrectly()
        {
            _gameState.hexGridConfig = new HexGridConfig { cols = 6, rows = 1 };
            _hexSpawnerState.hexes = new List<List<Hex.HexState>>
            {
                new List<Hex.HexState>
                {
                    new Hex.HexState { Col = 0, Row = 0, HexType = "forest",   HexNum = 5 },
                    new Hex.HexState { Col = 1, Row = 0, HexType = "hill",     HexNum = 6 },
                    new Hex.HexState { Col = 2, Row = 0, HexType = "pasture",  HexNum = 8 },
                    new Hex.HexState { Col = 3, Row = 0, HexType = "field",    HexNum = 9 },
                    new Hex.HexState { Col = 4, Row = 0, HexType = "mountain", HexNum = 3 },
                    new Hex.HexState { Col = 5, Row = 0, HexType = "gold",     HexNum = 4 }
                }
            };

            var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);

            Assert.AreEqual(ResourceType.Wood,  state.HexGrid.Hexes[0].ResourceType, "forest→Wood");
            Assert.AreEqual(ResourceType.Brick, state.HexGrid.Hexes[1].ResourceType, "hill→Brick");
            Assert.AreEqual(ResourceType.Sheep, state.HexGrid.Hexes[2].ResourceType, "pasture→Sheep");
            Assert.AreEqual(ResourceType.Wheat, state.HexGrid.Hexes[3].ResourceType, "field→Wheat");
            Assert.AreEqual(ResourceType.Ore,   state.HexGrid.Hexes[4].ResourceType, "mountain→Ore");
            Assert.AreEqual(ResourceType.Gold,  state.HexGrid.Hexes[5].ResourceType, "gold→Gold");
        }

        [Test]
        public void ExtractFromScene_SeaHex_UsesFallbackResourceType()
        {
            _gameState.hexGridConfig = new HexGridConfig { cols = 1, rows = 1 };
            _hexSpawnerState.hexes = new List<List<Hex.HexState>>
            {
                new List<Hex.HexState>
                {
                    new Hex.HexState { Col = 0, Row = 0, HexType = "sea", HexNum = null }
                }
            };

            // Should not throw; fallback resource type is returned
            Assert.DoesNotThrow(() =>
            {
                var state = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);
                Assert.IsNotNull(state.HexGrid);
            });
        }

        [Test]
        public void ExtractFromScene_NullGameState_ThrowsArgumentNull()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                UnityGameStateBridge.ExtractFromScene(null, _hexSpawnerState, 2));
        }

        [Test]
        public void ExtractFromScene_NullHexSpawnerState_ThrowsArgumentNull()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                UnityGameStateBridge.ExtractFromScene(_gameState, null, 2));
        }

        // ── ApplyToScene ──────────────────────────────────────────────────────────

        [Test]
        public void ApplyToScene_UpdatesGridConfigDimensions()
        {
            // Create a 3x4 simulation state
            var simState = HeadlessGameState.CreateInitialState(2, 3, 4);
            var targetGameState = new GameSpawner.GameSpawnerState();

            UnityGameStateBridge.ApplyToScene(simState, targetGameState);

            Assert.AreEqual(3, targetGameState.hexGridConfig.cols);
            Assert.AreEqual(4, targetGameState.hexGridConfig.rows);
        }

        [Test]
        public void ApplyToScene_NullGameState_ThrowsArgumentNull()
        {
            var simState = HeadlessGameState.CreateInitialState(2, 3, 3);
            Assert.Throws<System.ArgumentNullException>(() =>
                UnityGameStateBridge.ApplyToScene(simState, null));
        }

        [Test]
        public void ApplyToScene_PopulatesLandConfigs()
        {
            var simState = HeadlessGameState.CreateInitialState(2, 3, 3);
            var targetGameState = new GameSpawner.GameSpawnerState();

            UnityGameStateBridge.ApplyToScene(simState, targetGameState);

            Assert.IsNotNull(targetGameState.landConfigs);
            Assert.Greater(targetGameState.landConfigs.Count, 0,
                "ApplyToScene should populate land configs from hex resource distribution");
        }

        // ── Round-trip ────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_ExtractThenApply_PreservesGridDimensions()
        {
            _gameState.hexGridConfig = new HexGridConfig { cols = 5, rows = 5 };
            _hexSpawnerState.hexes = new List<List<Hex.HexState>>();

            var extracted = UnityGameStateBridge.ExtractFromScene(_gameState, _hexSpawnerState, 2);

            var newGameState = new GameSpawner.GameSpawnerState();
            UnityGameStateBridge.ApplyToScene(extracted, newGameState);

            Assert.AreEqual(5, newGameState.hexGridConfig.cols);
            Assert.AreEqual(5, newGameState.hexGridConfig.rows);
        }
    }
}
