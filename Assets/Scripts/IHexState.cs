// Assets/Scripts/IHexState.cs
public interface IHexState
{
    int Col { get; set; }
    int Row { get; set; }
    string HexType { get; set; }
    string HexSubType { get; set; }
    int Rotation { get; set; }
    int? HexNum { get; set; }
    string GroupID { get; set; }
    bool Selected { get; set; }
}