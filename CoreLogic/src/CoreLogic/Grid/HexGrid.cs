// <copyright file="HexGrid.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Grid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // HexGrid contains helpers; AxialCoord has been moved to its own file to satisfy SA1649.
    public static class HexGrid
    {
        // Axial directions: pointy-top layout
        private static readonly (int, int)[] Directions = new[]
        {
            (1, 0),
            (1, -1),
            (0, -1),
            (-1, 0),
            (-1, 1),
            (0, 1),
        };

        public static IEnumerable<AxialCoord> GetNeighbors(AxialCoord a)
        {
            foreach (var (dq, dr) in Directions)
            {
                yield return new AxialCoord(a.Q + dq, a.R + dr);
            }
        }

        public static (int X, int Y, int Z) AxialToCube(AxialCoord a)
        {
            var x = a.Q;
            var z = a.R;
            var y = -x - z;
            return (x, y, z);
        }

        public static AxialCoord CubeToAxial((int X, int Y, int Z) c)
        {
            var (x, y, z) = c;

            return new AxialCoord(x, z);
        }

        // Fractional cube rounding (for world -> axial conversions)
        // NOTE: implementation moved lower so public members appear before private members (SA1202).

        // World conversion for pointy-top hexes:
        // x = size * sqrt(3) * (q + r/2), y = size * 3/2 * r
        public static (double X, double Y) AxialToWorld(AxialCoord a, double size)
        {
            double x = size * Math.Sqrt(3) * (a.Q + (a.R / 2.0));
            double y = size * ((3.0 / 2.0) * a.R);

            return (x, y);
        }

        public static AxialCoord WorldToAxial(double x, double y, double size)
        {
            double a = Math.Sqrt(3) / 3.0;
            double b = 1.0 / 3.0;

            double numeratorQ = (a * x) - (b * y);
            double q = numeratorQ / size;

            double numeratorR = (2.0 / 3.0) * y;
            double r = numeratorR / size;

            double cx = q;
            double cz = r;
            double cy = -cx - cz;

            var (rx, ry, rz) = CubeRound(cx, cy, cz);

            return new AxialCoord(rx, rz);
        }

        // Fractional cube rounding (for world -> axial conversions)
        private static (int X, int Y, int Z) CubeRound(double x, double y, double z)
        {
            int rx = (int)Math.Round(x);
            int ry = (int)Math.Round(y);
            int rz = (int)Math.Round(z);

            double dx = Math.Abs(rx - x);
            double dy = Math.Abs(ry - y);
            double dz = Math.Abs(rz - z);

            if (dx > dy && dx > dz)
            {
                rx = -ry - rz;
            }
            else if (dy > dz)
            {
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }

            return (rx, ry, rz);
        }
    }
}
