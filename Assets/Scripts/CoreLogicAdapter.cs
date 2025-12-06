using System;
using CoreLogic.Models;

// Simple adapter helpers to convert between Unity's Hex.BaseHexState and CoreLogic.BaseHexState
// This approach avoids introducing assembly references in Unity and keeps conversion local.
public static class CoreLogicAdapter
{
    public static CoreLogic.Models.BaseHexState ToCore(this Hex.BaseHexState b)
    {
        if (b == null) return null;
        return new CoreLogic.Models.BaseHexState
        {
            Col = b.Col,
            Row = b.Row,
            HexType = b.HexType,
            HexSubType = b.HexSubType,
            Rotation = b.Rotation,
            HexNum = b.HexNum,
            GroupID = b.GroupID,
            Selected = b.Selected
        };
    }

    public static Hex.BaseHexState FromCore(this CoreLogic.Models.BaseHexState b)
    {
        if (b == null) return null;
        var ub = new Hex.BaseHexState();
        ub.Col = b.Col;
        ub.Row = b.Row;
        ub.HexType = b.HexType;
        ub.HexSubType = b.HexSubType;
        ub.Rotation = b.Rotation;
        ub.HexNum = b.HexNum;
        ub.GroupID = b.GroupID;
        ub.Selected = b.Selected;
        return ub;
    }
}
