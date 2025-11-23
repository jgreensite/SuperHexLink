// <copyright file="HexGridTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Grid;
    using System.Linq;

    public class HexGridTests
    {
        [Test]
        public void Neighbors_AreCorrectCountAndOffsets()
        {
            var origin = new AxialCoord(0, 0);
            var neighbors = HexGrid.GetNeighbors(origin).ToArray();
            Assert.AreEqual(6, neighbors.Length);

            var expected = new[] {
                new AxialCoord(1,0), new AxialCoord(1,-1), new AxialCoord(0,-1),
                new AxialCoord(-1,0), new AxialCoord(-1,1), new AxialCoord(0,1)
            };
            CollectionAssert.AreEquivalent(expected, neighbors);
        }

        [Test]
        public void Axial_Cube_Roundtrip()
        {
            var a = new AxialCoord(3, -2);
            var cube = HexGrid.AxialToCube(a);
            var a2 = HexGrid.CubeToAxial(cube);
            Assert.AreEqual(a, a2);
        }

        [Test]
        public void World_Axial_Roundtrip()
        {
            var a = new AxialCoord(2, -1);
            var size = 10.0;
            var (x, y) = HexGrid.AxialToWorld(a, size);
            var a2 = HexGrid.WorldToAxial(x, y, size);
            Assert.AreEqual(a, a2);
        }
    }
}
