// <copyright file="HexGridDistanceTests.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Tests
{
    using NUnit.Framework;
    using CoreLogic.Grid;

    public class HexGridDistanceTests
    {
        [Test]
        public void Distance_SameHex_IsZero()
        {
            var a = new AxialCoord(3, -2);
            Assert.AreEqual(0, HexGrid.Distance(a, a));
        }

        [Test]
        public void Distance_ImmediateNeighbor_IsOne()
        {
            var origin = new AxialCoord(0, 0);
            var neighbors = HexGrid.GetNeighbors(origin);
            foreach (var n in neighbors)
            {
                Assert.AreEqual(1, HexGrid.Distance(origin, n),
                    $"Distance to neighbor {n} should be 1");
            }
        }

        [Test]
        public void Distance_TwoStepsAway_IsTwo()
        {
            var a = new AxialCoord(0, 0);
            var b = new AxialCoord(2, 0);
            Assert.AreEqual(2, HexGrid.Distance(a, b));
        }

        [Test]
        public void Distance_IsSymmetric()
        {
            var a = new AxialCoord(3, -2);
            var b = new AxialCoord(-1, 4);
            Assert.AreEqual(HexGrid.Distance(a, b), HexGrid.Distance(b, a));
        }

        [Test]
        public void Distance_LargeCoords_DoesNotOverflow()
        {
            var a = new AxialCoord(10000, -5000);
            var b = new AxialCoord(-10000, 5000);
            int dist = HexGrid.Distance(a, b);
            Assert.Greater(dist, 0);
            Assert.AreEqual(20000, dist);
        }

        [Test]
        public void Distance_NegativeCoords_WorksCorrectly()
        {
            var a = new AxialCoord(-3, -3);
            var b = new AxialCoord(-1, -1);
            // cube(-3,-3)=(-3,6,-3), cube(-1,-1)=(-1,2,-1) → max(|2|,|4|,|2|) = 4
            Assert.AreEqual(4, HexGrid.Distance(a, b));
        }

        [Test]
        public void Distance_TriangleInequality_Holds()
        {
            // For any three points: dist(a,c) <= dist(a,b) + dist(b,c)
            var a = new AxialCoord(0, 0);
            var b = new AxialCoord(2, -1);
            var c = new AxialCoord(4, -3);

            int ab = HexGrid.Distance(a, b);
            int bc = HexGrid.Distance(b, c);
            int ac = HexGrid.Distance(a, c);

            Assert.LessOrEqual(ac, ab + bc, "Triangle inequality violated");
        }
    }
}
