using System;
using System.Collections;
using System.Collections.Generic;
using HexExtensions;
using SimpleHexExtensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
public class Corner : MonoBehaviour
{
    public readonly int q;
    public readonly int r;
    public readonly int s;
    public readonly int cornerIndex;

    public Corner(int q, int r, int s, int cornerIndex)
    {
        this.q = q;
        this.r = r;
        this.s = s;
        this.cornerIndex = cornerIndex;
    }

    // Override Equals and GetHashCode to ensure uniqueness in dictionary
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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

        // Create a Corner identifier (for simplicity, use the first hex in the list)
        return new Corner(sharedHexes[0].q, sharedHexes[0].r, sharedHexes[0].s, cornerIndex);
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
