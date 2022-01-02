using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleHexExtensions
{
    public static class SimpleHexExtensions 
    {
        public enum HexNeighborDirection { Up, UpRight, DownRight, Down, DownLeft, UpLeft }
        public static int Floor(this float val) => Mathf.FloorToInt(val);
        public static HexNeighborDirection Opposite(this HexNeighborDirection direction) =>
            (HexNeighborDirection)(((int)direction + 3) % 6);
    }
}