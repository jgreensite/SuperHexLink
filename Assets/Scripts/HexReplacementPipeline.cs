using System;
using System.Collections.Generic;
using System.Linq;
using FDL.Library.Numeric;
using HexExtensions;
using SuperHexLink.Logging;
using UnityEngine;

public interface IHexReplacementRule
{
    string Name { get; }
    void Execute(HexReplacementContext context);
}

public class HexReplacementPipeline
{
    private readonly List<IHexReplacementRule> rules = new();

    public HexReplacementPipeline(IEnumerable<IHexReplacementRule>? initialRules = null)
    {
        if (initialRules != null)
        {
            rules.AddRange(initialRules);
        }
    }

    public void AddRule(IHexReplacementRule rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));
        rules.Add(rule);
    }

    public void Run(HexReplacementContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (var rule in rules)
        {
            rule.Execute(context);
        }
    }
}

public class HexReplacementContext
{
    private readonly Dictionary<(int Col, int Row), Hex> hexLookup;

    public HexReplacementContext(HexSpawner hexSpawner, ActionLogSettings? logSettings)
    {
        HexSpawner = hexSpawner ?? throw new ArgumentNullException(nameof(hexSpawner));
        ActionLogSettings = logSettings;
        hexLookup = UnityEngine.Object.FindObjectsOfType<Hex>()
            .Where(h => h != null && h.hexState != null)
            .ToDictionary(h => (h.hexState.Col, h.hexState.Row));
    }

    public HexSpawner HexSpawner { get; }
    public ActionLogSettings? ActionLogSettings { get; }

    public IEnumerable<Hex> Hexes => hexLookup.Values;

    public bool TryGetHex(int col, int row, out Hex hex) => hexLookup.TryGetValue((col, row), out hex);

    public void Log(ActionLogCategory category, ActionLogSeverity severity, string message, params object[] args)
    {
        ActionLogger.Log(ActionLogSettings, category, severity, message, args);
    }
}

public readonly struct HexReplacementCandidate
{
    public Hex ReplacementHex { get; }
    public int DirectionIndex { get; }
    public float RotationDegrees => DirectionIndex * 60f;

    public HexReplacementCandidate(Hex replacementHex, int directionIndex)
    {
        ReplacementHex = replacementHex ?? throw new ArgumentNullException(nameof(replacementHex));
        DirectionIndex = directionIndex;
    }
}

public class HarbourReplacementRule : IHexReplacementRule
{
    private readonly GameConstants constants;
    private readonly HexPlacementRuleConfig? ruleConfig;
    private readonly int targetCount;

    public HarbourReplacementRule(GameConstants constants, HexPlacementRuleConfig? ruleConfig, int targetCount)
    {
        this.constants = constants ?? throw new ArgumentNullException(nameof(constants));
        this.ruleConfig = ruleConfig;
        this.targetCount = Math.Max(targetCount, 0);
    }

    public string Name => "Harbour Replacement";

    public void Execute(HexReplacementContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (targetCount <= 0) return;
        if (!context.HexSpawner.GridConfig.HasValue) return;

        var snapshot = HarbourBoardSnapshot.FromContext(context, context.HexSpawner.GridConfig.Value);
        int placed = 0;
        int consecutiveFailures = 0;
        int maxAttempts = Math.Max(ruleConfig?.maxAttemptsBeforeFallback ?? (targetCount * 5), 1);

        while (placed < targetCount && consecutiveFailures < maxAttempts)
        {
            var candidates = GatherCandidates(context, snapshot).ToList();
            if (candidates.Count == 0)
            {
                consecutiveFailures++;
                continue;
            }

            var index = RandomNumber.Between(0, candidates.Count - 1);
            var candidate = candidates[index];
            ApplyCandidate(candidate, context, snapshot);
            placed++;
            consecutiveFailures = 0;
        }

        ValidateHarbourOrientations(context, snapshot);

        var totalHarbours = snapshot.GetHarbourPositions().Count();
        if (totalHarbours < targetCount)
        {
            context.Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                "Harbour replacement placed {0}/{1} harbours via pipeline", totalHarbours, targetCount);
        }
        else
        {
            context.Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
                "Placed {0} harbours via replacement pass", totalHarbours);
        }
    }

    private IEnumerable<HexReplacementCandidate> GatherCandidates(HexReplacementContext context, HarbourBoardSnapshot snapshot)
    {
        var gridConfig = context.HexSpawner.GridConfig;
        if (!gridConfig.HasValue) yield break;

        var directions = HexExtensions.HexExtensions.Hex.directions;
        foreach (var hex in context.Hexes)
        {
            if (hex == null || hex.hexState == null) continue;
            if (!string.Equals(hex.hexState.HexType, GameConstants.CAR_TYPE_SEA, StringComparison.OrdinalIgnoreCase)) continue;
            if (!snapshot.TryGetHarbourFacingDirection(hex.hexState.Col, hex.hexState.Row, constants, out var dirIndex)) continue;
            yield return new HexReplacementCandidate(hex, dirIndex);
        }
    }

    private void ApplyCandidate(HexReplacementCandidate candidate, HexReplacementContext context, HarbourBoardSnapshot snapshot)
    {
        var hex = candidate.ReplacementHex;
        if (hex == null || hex.hexState == null) return;

        hex.hexState.HexType = GameConstants.CAR_TYPE_HARBOUR;
        hex.hexState.Rotation = Mathf.RoundToInt(candidate.RotationDegrees) % 360;
        snapshot.UpdateCell(hex.hexState.Col, hex.hexState.Row, GameConstants.CAR_TYPE_HARBOUR);
        context.HexSpawner.RefreshHex(hex);

        context.Log(ActionLogCategory.HexLand, ActionLogSeverity.Info,
            "Placed harbour at {0}_{1} facing direction {2}",
            hex.hexState.Col, hex.hexState.Row, candidate.DirectionIndex);
    }

    private void ValidateHarbourOrientations(HexReplacementContext context, HarbourBoardSnapshot snapshot)
    {
        foreach (var position in snapshot.GetHarbourPositions())
        {
            if (!context.TryGetHex(position.Col, position.Row, out var hex))
            {
                continue;
            }

            if (hex == null || hex.hexState == null) continue;

            if (!snapshot.TryGetHarbourFacingDirection(position.Col, position.Row, constants, out var direction))
            {
                context.Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                    "Harbour at {0}_{1} has no adjacent eligible land; reverting to sea", position.Col, position.Row);
                hex.hexState.HexType = GameConstants.CAR_TYPE_SEA;
                snapshot.UpdateCell(position.Col, position.Row, GameConstants.CAR_TYPE_SEA);
                context.HexSpawner.RefreshHex(hex);
                continue;
            }

            var desiredRotation = direction * 60;
            if (hex.hexState.Rotation != desiredRotation)
            {
                hex.hexState.Rotation = desiredRotation;
                context.HexSpawner.RefreshHex(hex);
            }
        }
    }
}

public class HarbourBoardSnapshot
{
    private readonly HexGridConfig gridConfig;
    private readonly Dictionary<(int Col, int Row), string> cells;

    public HarbourBoardSnapshot(HexGridConfig gridConfig, IEnumerable<(int Col, int Row, string HexType)> initialCells)
    {
        this.gridConfig = gridConfig;
        cells = new Dictionary<(int, int), string>();

        if (initialCells == null)
        {
            return;
        }

        foreach (var cell in initialCells)
        {
            cells[(cell.Col, cell.Row)] = cell.HexType ?? string.Empty;
        }
    }

    public static HarbourBoardSnapshot FromContext(HexReplacementContext context, HexGridConfig gridConfig)
    {
        var cells = context.Hexes
            .Where(h => h != null && h.hexState != null)
            .Select(h => (h.hexState.Col, h.hexState.Row, h.hexState.HexType ?? GameConstants.CAR_TYPE_WORD_NULL));
        return new HarbourBoardSnapshot(gridConfig, cells);
    }

    public bool TryGetHarbourFacingDirection(int col, int row, GameConstants constants, out int directionIndex)
    {
        if (constants == null) throw new ArgumentNullException(nameof(constants));
        directionIndex = -1;

        if (!TryGetNeighboringDirections(col, row, out var directions))
        {
            return false;
        }

        foreach (var (dirIndex, neighbor) in directions)
        {
            if (!TryGetCellType(neighbor.Col, neighbor.Row, out var neighborType))
            {
                continue;
            }

            if (!constants.IsHarbourFacingLandType(neighborType))
            {
                continue;
            }

            directionIndex = dirIndex;
            return true;
        }

        return false;
    }

    public bool TryGetCellType(int col, int row, out string hexType)
    {
        return cells.TryGetValue((col, row), out hexType);
    }

    public void UpdateCell(int col, int row, string hexType)
    {
        cells[(col, row)] = hexType ?? string.Empty;
    }

    public IEnumerable<(int Col, int Row)> GetHarbourPositions()
    {
        foreach (var kvp in cells)
        {
            if (string.Equals(kvp.Value, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase))
            {
                yield return kvp.Key;
            }
        }
    }

    private bool TryGetNeighboringDirections(int col, int row, out List<(int DirectionIndex, (int Col, int Row) Coordinates)> result)
    {
        result = new List<(int, (int, int))>();
        var directions = HexExtensions.HexExtensions.Hex.directions;
        for (var dirIndex = 0; dirIndex < directions.Count; dirIndex++)
        {
            var baseOffset = new HexExtensions.HexExtensions.OffsetCoord(col, row);
            var baseHex = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, baseOffset);
            var neighborHex = baseHex.Add(directions[dirIndex]);
            var neighborOffset = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, neighborHex);

            if (!gridConfig.IsInBounds(neighborOffset.row, neighborOffset.col))
            {
                continue;
            }

            result.Add((dirIndex, (neighborOffset.col, neighborOffset.row)));
        }

        return result.Count > 0;
    }
}
