using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexExtensions;

namespace SimpleHexExtensions
{
    public static class SimpleHexExtensions 
    {
        public enum HexNeighborDirection { Up, UpRight, DownRight, Down, DownLeft, UpLeft }
        public enum HexVertexDirection { Veertex0, Vertex1, Vertex2, Vertex3, Vertex4, Vertex5 }
        public static int Floor(this float val) => Mathf.FloorToInt(val);
        public static HexNeighborDirection Opposite(this HexNeighborDirection direction) =>
            (HexNeighborDirection)(((int)direction + 3) % 6);
    
    }
}