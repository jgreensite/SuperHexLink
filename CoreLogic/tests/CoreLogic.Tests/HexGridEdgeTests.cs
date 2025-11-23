// <copyright file="HexGridEdgeTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Grid;

    public class HexGridEdgeTests
    {
        [Test]
        public void Axial_World_Roundtrip_MultipleCoordsAndSizes()
        {
            var coords = new[] {
                new AxialCoord(0,0), new AxialCoord(5,-3), new AxialCoord(-7,2), new AxialCoord(10,10), new AxialCoord(-12,-4)
            };
            var sizes = new double[] { 1.0, 2.5, 10.0, 0.5 };

            foreach (var a in coords)
            {
                foreach (var s in sizes)
                {
                    var (x,y) = HexGrid.AxialToWorld(a, s);
                    var a2 = HexGrid.WorldToAxial(x, y, s);
                    Assert.AreEqual(a, a2, $"Roundtrip failed for {a} at size {s}");
                }
            }
        }

        [Test]
        public void AxialToCube_Rounding_Stability_LargeCoords()
        {
            var a = new AxialCoord(1000, -500);
            var cube = HexGrid.AxialToCube(a);
            var a2 = HexGrid.CubeToAxial(cube);
            Assert.AreEqual(a, a2);
        }
    }
}
