// <copyright file="AxialCoord.cs" company="SuperHexLink">
// Copyright (c) SuperHexLink. All rights reserved.
// </copyright>
namespace CoreLogic.Grid
{
    using System;

    /// <summary>
    /// Simple axial coordinate representation (q, r).
    /// Kept as a separate type so file name matches the first type (SA1649).
    /// </summary>
    public readonly struct AxialCoord : IEquatable<AxialCoord>
    {
        public AxialCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q { get; }

        public int R { get; }

        public bool Equals(AxialCoord other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is AxialCoord a && Equals(a);

        public override int GetHashCode() => (Q, R).GetHashCode();

        public override string ToString() => $"({Q},{R})";
    }
}
