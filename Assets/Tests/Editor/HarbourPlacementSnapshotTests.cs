using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class HarbourPlacementSnapshotTests
    {
        private GameConstants constants;

        [SetUp]
        public void SetUp()
        {
            constants = ScriptableObject.CreateInstance<GameConstants>();
        }

        [TearDown]
        public void TearDown()
        {
            if (constants != null)
            {
                Object.DestroyImmediate(constants);
            }
        }

        [Test]
        public void TryGetHarbourFacingDirection_ReturnsTrue_WhenLandAdjacent()
        {
            var grid = new HexGridConfig { cols = 3, rows = 3 };
            var cells = new[]
            {
                (Col: 1, Row: 1, HexType: GameConstants.CAR_TYPE_SEA),
                (Col: 1, Row: 0, HexType: GameConstants.CAR_TYPE_FIELD)
            };

            var snapshot = new HarbourBoardSnapshot(grid, cells);
            var hasDirection = snapshot.TryGetHarbourFacingDirection(1, 1, constants, out var directionIndex);

            Assert.That(hasDirection, Is.True);
            Assert.That(directionIndex, Is.InRange(0, 5));
        }

        [Test]
        public void TryGetHarbourFacingDirection_ReturnsFalse_WhenNoLandNearby()
        {
            var grid = new HexGridConfig { cols = 3, rows = 3 };
            var cells = new[]
            {
                (Col: 1, Row: 1, HexType: GameConstants.CAR_TYPE_SEA),
                (Col: 0, Row: 1, HexType: GameConstants.CAR_TYPE_SEA),
                (Col: 1, Row: 0, HexType: GameConstants.CAR_TYPE_SEA)
            };

            var snapshot = new HarbourBoardSnapshot(grid, cells);
            var hasDirection = snapshot.TryGetHarbourFacingDirection(1, 1, constants, out _);

            Assert.That(hasDirection, Is.False);
        }

        [Test]
        public void UpdateCell_MarksHarbourPosition()
        {
            var grid = new HexGridConfig { cols = 1, rows = 1 };
            var cells = new[]
            {
                (Col: 0, Row: 0, HexType: GameConstants.CAR_TYPE_SEA)
            };

            var snapshot = new HarbourBoardSnapshot(grid, cells);
            snapshot.UpdateCell(0, 0, GameConstants.CAR_TYPE_HARBOUR);

            CollectionAssert.Contains(snapshot.GetHarbourPositions().ToList(), (0, 0));
        }
    }
}
