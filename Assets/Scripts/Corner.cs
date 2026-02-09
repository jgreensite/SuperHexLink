using System;
using System.Collections.Generic;
using HexExtensions;
using UnityEngine;

/// <summary>
/// Represents a corner (vertex) shared by up to three hexes on the board.
/// Identified by cube coordinates (q, r, s) and a corner index (0–5).
/// </summary>
public class Corner : MonoBehaviour
{
    public int q;
    public int r;
    public int s;
    public int cornerIndex;

    /// <summary>
    /// Initialise the corner identifier after instantiation.
    /// Unity MonoBehaviours must not use constructors.
    /// </summary>
    public void Init(int q, int r, int s, int cornerIndex)
    {
        this.q = q;
        this.r = r;
        this.s = s;
        this.cornerIndex = cornerIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is Corner corner &&
               q == corner.q &&
               r == corner.r &&
               s == corner.s &&
               cornerIndex == corner.cornerIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(q, r, s, cornerIndex);
    }
}

public class CornerData
{
    public string BuildingType { get; set; }
    // Add more properties as needed

    public CornerData(string buildingType)
    {
        BuildingType = buildingType;
    }
}

public static class CornerManager
{
    private static Dictionary<Corner, CornerData> corners = new Dictionary<Corner, CornerData>();

    // Method to get the Corner identifier for a given hex and corner index
    public static Corner GetCornerIdentifier(HexExtensions.HexExtensions.Hex hex, int cornerIndex, int numColumns, int numRows)
    {
        // Use the GetHexesSharingCorner method to find all the hexes that share this corner
        List<HexExtensions.HexExtensions.Hex> sharedHexes = HexExtensions.HexExtensions.GetHexesSharingCorner(hex, cornerIndex, numColumns, numRows);

        // Create a temporary Corner identifier for dictionary lookups.
        // NOTE: This creates a standalone object not attached to a GameObject — acceptable
        // because it is only used as a dictionary key via Equals/GetHashCode.
        var corner = new GameObject("_cornerKey").AddComponent<Corner>();
        corner.Init(sharedHexes[0].q, sharedHexes[0].r, sharedHexes[0].s, cornerIndex);
        return corner;
    }

    // Method to set corner data
    public static void SetCornerData(HexExtensions.HexExtensions.Hex hex, int cornerIndex, CornerData data, int numColumns, int numRows)
    {
        Corner cornerId = GetCornerIdentifier(hex, cornerIndex, numColumns, numRows);
        corners[cornerId] = data;
    }

    // Method to get corner data
    public static CornerData GetCornerData(HexExtensions.HexExtensions.Hex hex, int cornerIndex, int numColumns, int numRows)
    {
        Corner cornerId = GetCornerIdentifier(hex, cornerIndex, numColumns, numRows);
        if (corners.TryGetValue(cornerId, out CornerData data))
        {
            return data;
        }
        else
        {
            return null; // or a default value
        }
    }
}
