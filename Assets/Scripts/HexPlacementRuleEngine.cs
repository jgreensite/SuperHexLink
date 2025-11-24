using System;
using System.Collections.Generic;
using HexExtensions;

public delegate bool TryGetHexStateDelegate(int col, int row, out Hex.HexState state);
public delegate bool HexTypePredicate(string hexType);

public class HexPlacementRuleEngine
{
    private readonly TryGetHexStateDelegate stateLookup;
    private readonly HexTypePredicate isReplaceableLand;

    public HexPlacementRuleEngine(TryGetHexStateDelegate stateLookup, HexTypePredicate isReplaceableLand)
    {
        this.stateLookup = stateLookup ?? throw new ArgumentNullException(nameof(stateLookup));
        this.isReplaceableLand = isReplaceableLand ?? throw new ArgumentNullException(nameof(isReplaceableLand));
    }

    public bool AllowsPlacement(HexPlacementRuleConfig rule, Hex hex)
    {
        if (rule == null || rule.ruleTypes == null || rule.ruleTypes.Count == 0)
        {
            return true;
        }

        foreach (var ruleType in rule.ruleTypes)
        {
            switch (ruleType)
            {
                case HexPlacementRuleType.NeedsAdjacentLand:
                    if (!HasAdjacentReplaceableLand(hex))
                    {
                        return false;
                    }
                    break;
                default:
                    break;
            }
        }

        return true;
    }

    private bool HasAdjacentReplaceableLand(Hex hex)
    {
        if (hex == null || hex.hexState == null)
        {
            return false;
        }

        foreach (var direction in hex.hexState.Neighbours())
        {
            var offset = OffsetCoord.QoffsetFromCube(OffsetCoord.ODD, direction);
            if (stateLookup(offset.col, offset.row, out var neighborState) && isReplaceableLand(neighborState.HexType))
            {
                return true;
            }
        }

        return false;
    }
}
