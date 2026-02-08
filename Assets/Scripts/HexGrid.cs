using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Configuration for the hexagonal grid layout.
/// Uses the "odd-q" vertical offset coordinate system.
/// See https://www.redblobgames.com/grids/hexagons/ for reference.
/// </summary>
[Serializable]
public struct HexGridConfig
{
    public int cols;
    [ShowInInspector, ReadOnly] public readonly int maxCol => cols - 1;

    public int rows;
    [ShowInInspector, ReadOnly] public readonly int maxRow => rows - 1;

    public int radius;
    public float height;

    [SerializeField, MinMaxSlider(-64, 64, true)]
    private Vector2 hexHeightVariance;

    [ShowInInspector, ReadOnly] public readonly float minHeight => hexHeightVariance.x;
    [ShowInInspector, ReadOnly] public readonly float maxHeight => hexHeightVariance.y;

    /// <summary>Distance from centre to the midpoint of any side (flat-top hex).</summary>
    public readonly float Apothem =>
        Mathf.Sqrt(Mathf.Pow(radius, 2f) - Mathf.Pow(radius * 0.5f, 2f));

    /// <summary>Returns true when (<paramref name="row"/>, <paramref name="col"/>) is inside the grid.</summary>
    public readonly bool IsInBounds(int row, int col) =>
        row >= 0 && row <= maxRow && col >= 0 && col <= maxCol;

    /// <summary>Creates a sensible 7×7 default grid.</summary>
    public static HexGridConfig CreateDefault() => new HexGridConfig
    {
        cols = 7,
        rows = 7,
        radius = 1,
        height = 1
    };
}