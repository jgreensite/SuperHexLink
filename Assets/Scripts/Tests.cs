using System;
using System.Collections.Generic;
using HexExtensions;

// Tests

namespace Tests
{
    struct Tests

    {

        static public void EqualHex(String name, HexExtensions.HexExtensions.Hex a, HexExtensions.HexExtensions.Hex b)
        {
            if (!(a.q == b.q && a.s == b.s && a.r == b.r))
            {
                Tests.Complain(name);
            }
        }


        static public void EqualOffsetcoord(String name, HexExtensions.HexExtensions.OffsetCoord a, HexExtensions.HexExtensions.OffsetCoord b)
        {
            if (!(a.col == b.col && a.row == b.row))
            {
                Tests.Complain(name);
            }
        }


        static public void EqualDoubledcoord(String name, HexExtensions.HexExtensions.DoubledCoord a, HexExtensions.HexExtensions.DoubledCoord b)
        {
            if (!(a.col == b.col && a.row == b.row))
            {
                Tests.Complain(name);
            }
        }


        static public void EqualInt(String name, int a, int b)
        {
            if (!(a == b))
            {
                Tests.Complain(name);
            }
        }


        static public void EqualHexArray(String name, List<HexExtensions.HexExtensions.Hex> a, List <HexExtensions.HexExtensions.Hex> b)
        {
            Tests.EqualInt(name, a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Tests.EqualHex(name, a[i], b[i]);
            }
        }


        static public void TestHexArithmetic()
        {
            Tests.EqualHex("hex_add", new HexExtensions.HexExtensions.Hex(4, -10, 6), new HexExtensions.HexExtensions.Hex (1, -3, 2).Add(new HexExtensions.HexExtensions.Hex (3, -7, 4)));
            Tests.EqualHex("hex_subtract", new HexExtensions.HexExtensions.Hex(-2, 4, -2), new HexExtensions.HexExtensions.Hex(1, -3, 2).Subtract(new HexExtensions.HexExtensions.Hex(3, -7, 4)));
        }


        static public void TestHexDirection()
        {
            Tests.EqualHex("hex_direction", new HexExtensions.HexExtensions.Hex(0, -1, 1), HexExtensions.HexExtensions.Hex.Direction(2));
        }


        static public void TestHexNeighbor()
        {
            Tests.EqualHex("hex_neighbor", new HexExtensions.HexExtensions.Hex(1, -3, 2), new HexExtensions.HexExtensions.Hex(1, -2, 1).Neighbor(2));
        }


        static public void TestHexDiagonal()
        {
            Tests.EqualHex("hex_diagonal", new HexExtensions.HexExtensions.Hex(-1, -1, 2), new HexExtensions.HexExtensions.Hex(1, -2, 1).DiagonalNeighbor(3));
        }


        static public void TestHexDistance()
        {
            Tests.EqualInt("hex_distance", 7, new HexExtensions.HexExtensions.Hex(3, -7, 4).Distance(new HexExtensions.HexExtensions.Hex(0, 0, 0)));
        }


        static public void TestHexRotateRight()
        {
            Tests.EqualHex("hex_rotate_right", new HexExtensions.HexExtensions.Hex(1, -3, 2).RotateRight(), new HexExtensions.HexExtensions.Hex(3, -2, -1));
        }


        static public void TestHexRotateLeft()
        {
            Tests.EqualHex("hex_rotate_left", new HexExtensions.HexExtensions.Hex(1, -3, 2).RotateLeft(), new HexExtensions.HexExtensions.Hex(-2, -1, 3));
        }


        static public void TestHexRound()
        {
            HexExtensions.HexExtensions.FractionalHex a = new HexExtensions.HexExtensions.FractionalHex(0.0, 0.0, 0.0);
            HexExtensions.HexExtensions.FractionalHex b = new HexExtensions.HexExtensions.FractionalHex(1.0, -1.0, 0.0);
            HexExtensions.HexExtensions.FractionalHex c = new HexExtensions.HexExtensions.FractionalHex(0.0, -1.0, 1.0);
            Tests.EqualHex("hex_round 1", new HexExtensions.HexExtensions.Hex(5, -10, 5), new HexExtensions.HexExtensions.FractionalHex(0.0, 0.0, 0.0).HexLerp(new HexExtensions.HexExtensions.FractionalHex(10.0, -20.0, 10.0), 0.5).HexRound());
            Tests.EqualHex("hex_round 2", a.HexRound(), a.HexLerp(b, 0.499).HexRound());
            Tests.EqualHex("hex_round 3", b.HexRound(), a.HexLerp(b, 0.501).HexRound());
            Tests.EqualHex("hex_round 4", a.HexRound(), new HexExtensions.HexExtensions.FractionalHex(a.q * 0.4 + b.q * 0.3 + c.q * 0.3, a.r * 0.4 + b.r * 0.3 + c.r * 0.3, a.s * 0.4 + b.s * 0.3 + c.s * 0.3).HexRound());
            Tests.EqualHex("hex_round 5", c.HexRound(), new HexExtensions.HexExtensions.FractionalHex(a.q * 0.3 + b.q * 0.3 + c.q * 0.4, a.r * 0.3 + b.r * 0.3 + c.r * 0.4, a.s * 0.3 + b.s * 0.3 + c.s * 0.4).HexRound());
        }


        static public void TestHexLinedraw()
        {
            Tests.EqualHexArray("hex_linedraw", new List<HexExtensions.HexExtensions.Hex> { new HexExtensions.HexExtensions.Hex(0, 0, 0), new HexExtensions.HexExtensions.Hex(0, -1, 1), new HexExtensions.HexExtensions.Hex(0, -2, 2), new HexExtensions.HexExtensions.Hex(1, -3, 2), new HexExtensions.HexExtensions.Hex(1, -4, 3), new HexExtensions.HexExtensions.Hex(1, -5, 4) }, HexExtensions.HexExtensions.FractionalHex.HexLinedraw(new HexExtensions.HexExtensions.Hex(0, 0, 0), new HexExtensions.HexExtensions.Hex(1, -5, 4)));
        }


        static public void TestLayout()
        {
            HexExtensions.HexExtensions.Hex h = new HexExtensions.HexExtensions.Hex(3, 4, -7);
            HexExtensions.HexExtensions.Layout flat = new HexExtensions.HexExtensions.Layout(HexExtensions.HexExtensions.Layout.flat, new HexExtensions.HexExtensions.Point(10.0, 15.0), new HexExtensions.HexExtensions.Point(35.0, 71.0));
            Tests.EqualHex("layout", h, flat.PixelToHex(flat.HexToPixel(h)).HexRound());
            HexExtensions.HexExtensions.Layout pointy = new HexExtensions.HexExtensions.Layout(HexExtensions.HexExtensions.Layout.pointy, new HexExtensions.HexExtensions.Point(10.0, 15.0), new HexExtensions.HexExtensions.Point(35.0, 71.0));
            Tests.EqualHex("layout", h, pointy.PixelToHex(pointy.HexToPixel(h)).HexRound());
        }


        static public void TestOffsetRoundtrip()
        {
            HexExtensions.HexExtensions.Hex a = new HexExtensions.HexExtensions.Hex(3, 4, -7);
            HexExtensions.HexExtensions.OffsetCoord b = new HexExtensions.HexExtensions.OffsetCoord(1, -3);
            Tests.EqualHex("conversion_roundtrip even-q", a, HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, a)));
            Tests.EqualOffsetcoord("conversion_roundtrip even-q", b, HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, b)));
            Tests.EqualHex("conversion_roundtrip odd-q", a, HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, a)));
            Tests.EqualOffsetcoord("conversion_roundtrip odd-q", b, HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b)));
            Tests.EqualHex("conversion_roundtrip even-r", a, HexExtensions.HexExtensions.OffsetCoord.RoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, HexExtensions.HexExtensions.OffsetCoord.RoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, a)));
            Tests.EqualOffsetcoord("conversion_roundtrip even-r", b, HexExtensions.HexExtensions.OffsetCoord.RoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, HexExtensions.HexExtensions.OffsetCoord.RoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, b)));
            Tests.EqualHex("conversion_roundtrip odd-r", a, HexExtensions.HexExtensions.OffsetCoord.RoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, HexExtensions.HexExtensions.OffsetCoord.RoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, a)));
            Tests.EqualOffsetcoord("conversion_roundtrip odd-r", b, HexExtensions.HexExtensions.OffsetCoord.RoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, HexExtensions.HexExtensions.OffsetCoord.RoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b)));
        }


        static public void TestOffsetFromCube()
        {
            Tests.EqualOffsetcoord("offset_from_cube even-q", new HexExtensions.HexExtensions.OffsetCoord(1, 3), HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, new HexExtensions.HexExtensions.Hex(1, 2, -3)));
            Tests.EqualOffsetcoord("offset_from_cube odd-q", new HexExtensions.HexExtensions.OffsetCoord(1, 2), HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, new HexExtensions.HexExtensions.Hex(1, 2, -3)));
        }


        static public void TestOffsetToCube()
        {
            Tests.EqualHex("offset_to_cube even-", new HexExtensions.HexExtensions.Hex(1, 2, -3), HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.EVEN, new HexExtensions.HexExtensions.OffsetCoord(1, 3)));
            Tests.EqualHex("offset_to_cube odd-q", new HexExtensions.HexExtensions.Hex(1, 2, -3), HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, new HexExtensions.HexExtensions.OffsetCoord(1, 2)));
        }


        static public void TestDoubledRoundtrip()
        {
            HexExtensions.HexExtensions.Hex a = new HexExtensions.HexExtensions.Hex(3, 4, -7);
            HexExtensions.HexExtensions.DoubledCoord b = new HexExtensions.HexExtensions.DoubledCoord(1, -3);
            Tests.EqualHex("conversion_roundtrip doubled-q", a, HexExtensions.HexExtensions.DoubledCoord.QdoubledFromCube(a).QdoubledToCube());
            Tests.EqualDoubledcoord("conversion_roundtrip doubled-q", b, HexExtensions.HexExtensions.DoubledCoord.QdoubledFromCube(b.QdoubledToCube()));
            Tests.EqualHex("conversion_roundtrip doubled-r", a, HexExtensions.HexExtensions.DoubledCoord.RdoubledFromCube(a).RdoubledToCube());
            Tests.EqualDoubledcoord("conversion_roundtrip doubled-r", b, HexExtensions.HexExtensions.DoubledCoord.RdoubledFromCube(b.RdoubledToCube()));
        }


        static public void TestDoubledFromCube()
        {
            Tests.EqualDoubledcoord("doubled_from_cube doubled-q", new HexExtensions.HexExtensions.DoubledCoord(1, 5), HexExtensions.HexExtensions.DoubledCoord.QdoubledFromCube(new HexExtensions.HexExtensions.Hex(1, 2, -3)));
            Tests.EqualDoubledcoord("doubled_from_cube doubled-r", new HexExtensions.HexExtensions.DoubledCoord(4, 2), HexExtensions.HexExtensions.DoubledCoord.RdoubledFromCube(new HexExtensions.HexExtensions.Hex(1, 2, -3)));
        }


        static public void TestDoubledToCube()
        {
            Tests.EqualHex("doubled_to_cube doubled-q", new HexExtensions.HexExtensions.Hex(1, 2, -3), new HexExtensions.HexExtensions.DoubledCoord(1, 5).QdoubledToCube());
            Tests.EqualHex("doubled_to_cube doubled-r", new HexExtensions.HexExtensions.Hex(1, 2, -3), new HexExtensions.HexExtensions.DoubledCoord(4, 2).RdoubledToCube());
        }


        static public void TestAll()
        {
            Tests.TestHexArithmetic();
            Tests.TestHexDirection();
            Tests.TestHexNeighbor();
            Tests.TestHexDiagonal();
            Tests.TestHexDistance();
            Tests.TestHexRotateRight();
            Tests.TestHexRotateLeft();
            Tests.TestHexRound();
            Tests.TestHexLinedraw();
            Tests.TestLayout();
            Tests.TestOffsetRoundtrip();
            Tests.TestOffsetFromCube();
            Tests.TestOffsetToCube();
            Tests.TestDoubledRoundtrip();
            Tests.TestDoubledFromCube();
            Tests.TestDoubledToCube();
        }


        static public void Main()
        {
            Tests.TestAll();
        }


        static public void Complain(String name)
        {
            Console.WriteLine("FAIL " + name);
        }
    }
}
