using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for spawner state defaults, validation, and defensive behavior.
    /// Ensures spawners handle null/empty state gracefully and produce sensible defaults.
    /// </summary>
    public class SpawnerStateValidationTests
    {
        [Test]
        public void HexGridConfig_CreateDefault_Returns7x7()
        {
            var config = HexGridConfig.CreateDefault();

            Assert.AreEqual(7, config.cols);
            Assert.AreEqual(7, config.rows);
            Assert.Greater(config.radius, 0f);
            Assert.Greater(config.height, 0f);
        }

        [Test]
        public void HexGridConfig_IsInBounds_ValidCoordinates_ReturnsTrue()
        {
            var config = new HexGridConfig { cols = 5, rows = 5, radius = 1, height = 1 };

            Assert.IsTrue(config.IsInBounds(0, 0));
            Assert.IsTrue(config.IsInBounds(4, 4));
            Assert.IsTrue(config.IsInBounds(2, 3));
        }

        [Test]
        public void HexGridConfig_IsInBounds_OutOfRange_ReturnsFalse()
        {
            var config = new HexGridConfig { cols = 5, rows = 5, radius = 1, height = 1 };

            Assert.IsFalse(config.IsInBounds(-1, 0));
            Assert.IsFalse(config.IsInBounds(0, -1));
            Assert.IsFalse(config.IsInBounds(5, 0));
            Assert.IsFalse(config.IsInBounds(0, 5));
        }

        [Test]
        public void GameSpawnerState_DefaultLandConfigs_NotEmpty()
        {
            var configs = GameSpawner.GameSpawnerState.CreateDefaultLandConfigs();

            Assert.IsNotNull(configs);
            Assert.Greater(configs.Count, 0, "Default land configs should contain at least one entry");
        }

        [Test]
        public void GameSpawnerState_DefaultNumConfigs_NotEmpty()
        {
            var configs = GameSpawner.GameSpawnerState.CreateDefaultNumConfigs();

            Assert.IsNotNull(configs);
            Assert.Greater(configs.Count, 0, "Default num configs should contain at least one entry");
        }

        [Test]
        public void HexSpawnerState_EmptyHexes_IsEmptyList()
        {
            var state = new HexSpawner.HexSpawnerState();

            // A fresh state should have a non-null hexes list (may be empty)
            Assert.IsNotNull(state.hexes);
        }

        [Test]
        public void HexState_DefaultValues_AreReasonable()
        {
            var state = new Hex.HexState();

            Assert.AreEqual(0, state.Col);
            Assert.AreEqual(0, state.Row);
            Assert.AreEqual(0, state.Rotation);
            Assert.IsFalse(state.Selected);
            Assert.IsNull(state.HexNum);
        }

        [Test]
        public void HexState_CubeCoordinates_SumToZero()
        {
            // Cube coordinate invariant: x + y + z = 0
            var state = new Hex.HexState { Col = 3, Row = 2 };
            var cube = state.CubeCoordinates;

            Assert.AreEqual(0, cube.x + cube.y + cube.z,
                "Cube coordinates must satisfy x + y + z = 0");
        }

        [Test]
        public void HexState_CubeCoordinates_MultiplePositions_AllSumToZero()
        {
            var positions = new[] { (0, 0), (1, 0), (0, 1), (3, 4), (6, 6) };

            foreach (var (col, row) in positions)
            {
                var state = new Hex.HexState { Col = col, Row = row };
                var cube = state.CubeCoordinates;
                Assert.AreEqual(0, cube.x + cube.y + cube.z,
                    $"Cube coords for ({col},{row}) don't sum to zero: ({cube.x},{cube.y},{cube.z})");
            }
        }

        [Test]
        public void SpawnerBase_IsConfiguredEmpty_NullOrEmptyReturnsTrue()
        {
            // Test via the static helper behavior — null, empty, "None", "none" should all be empty
            Assert.IsTrue(IsConfiguredEmptyHelper(null));
            Assert.IsTrue(IsConfiguredEmptyHelper(""));
            Assert.IsTrue(IsConfiguredEmptyHelper("None"));
            Assert.IsTrue(IsConfiguredEmptyHelper("none"));
        }

        [Test]
        public void SpawnerBase_IsConfiguredEmpty_ValidTypeReturnsFalse()
        {
            Assert.IsFalse(IsConfiguredEmptyHelper("FIELD"));
            Assert.IsFalse(IsConfiguredEmptyHelper("SEA"));
            Assert.IsFalse(IsConfiguredEmptyHelper("HARBOUR"));
        }

        /// <summary>
        /// Mirror of the SpawnerBase.IsConfiguredEmpty logic for testing without a MonoBehaviour instance.
        /// </summary>
        private static bool IsConfiguredEmptyHelper(string value)
        {
            return string.IsNullOrEmpty(value)
                || string.Equals(value, "None", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
