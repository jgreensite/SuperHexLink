// Assets/Scripts/BaseHexState.cs
using System;

[Serializable]
public class BaseHexState : IHexState // Implement the interface
{
    public int Col { get; set; } = 0;
    public int Row { get; set; } = 0;
    public string HexType { get; set; } = "none";
    public string HexSubType { get; set; } = "none";
    public int Rotation { get; set; } = 0;
    public int? HexNum { get; set; } = null;
    public string GroupID { get; set; } = "1";
    public bool Selected { get; set; } = false;

    // You can add any additional properties or methods as needed
}