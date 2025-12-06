using System;
using System.Collections.Generic;
using UnityEngine;

public static class HexStateRepairer
{
    public static HexStateRepairReport Repair(HexSpawner.HexSpawnerState state, HexGridConfig gridConfig, GameConstants constants)
    {
        var report = new HexStateRepairReport();
        if (gridConfig.cols <= 0 || gridConfig.rows <= 0)
        {
            return report;
        }

        if (state == null)
        {
            return report;
        }

        if (state.hexes == null)
        {
            state.hexes = new List<List<Hex.HexState>>();
        }

        AdjustColumnCount(state.hexes, gridConfig.cols);

        for (int col = 0; col < gridConfig.cols; col++)
        {
            var column = state.hexes[col];
            if (column == null)
            {
                column = new List<Hex.HexState>();
                state.hexes[col] = column;
            }

            report.CellsCreated += FillColumnToSize(column, col, gridConfig.rows);
            TrimColumn(column, gridConfig.rows);

            for (int row = 0; row < gridConfig.rows; row++)
            {
                var hex = column[row];
                if (hex == null)
                {
                    hex = CreateDefaultHexState(col, row);
                    column[row] = hex;
                    report.CellsCreated++;
                }

                if (EnsureCoordinates(hex, col, row))
                {
                    report.CoordinatesFixed++;
                }

                // Normalize hex type - handle empty, whitespace, and "null" string
                if (string.IsNullOrWhiteSpace(hex.HexType) || 
                    string.Equals(hex.HexType, GameConstants.CAR_TYPE_WORD_NULL, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(hex.HexType, GameConstants.CAR_TYPE_NONE, StringComparison.OrdinalIgnoreCase))
                {
                    hex.HexType = GameConstants.CAR_TYPE_SEA;
                    report.HexTypesDefaulted++;
                }
                else
                {
                    var trimmed = hex.HexType.Trim();
                    if (!string.Equals(trimmed, hex.HexType, StringComparison.Ordinal))
                    {
                        hex.HexType = trimmed;
                    }
                }

                var normalizedRotation = ((hex.Rotation % 360) + 360) % 360;
                if (hex.Rotation != normalizedRotation)
                {
                    hex.Rotation = normalizedRotation;
                    report.RotationNormalized++;
                }
            }
        }

        var snapshot = HarbourBoardSnapshot.FromState(gridConfig, state);
        ApplyHarbourFixes(state, gridConfig, constants, snapshot, ref report);

        return report;
    }

    private static void AdjustColumnCount(List<List<Hex.HexState>> columns, int targetCols)
    {
        while (columns.Count < targetCols)
        {
            columns.Add(new List<Hex.HexState>());
        }

        if (columns.Count > targetCols)
        {
            columns.RemoveRange(targetCols, columns.Count - targetCols);
        }
    }

    private static int FillColumnToSize(List<Hex.HexState> column, int colIndex, int targetRows)
    {
        int created = 0;
        while (column.Count < targetRows)
        {
            column.Add(CreateDefaultHexState(colIndex, column.Count));
            created++;
        }

        return created;
    }

    private static void TrimColumn(List<Hex.HexState> column, int targetRows)
    {
        if (column.Count <= targetRows) return;
        column.RemoveRange(targetRows, column.Count - targetRows);
    }

    private static bool EnsureCoordinates(Hex.HexState hex, int col, int row)
    {
        var changed = false;
        if (hex.Col != col)
        {
            hex.Col = col;
            changed = true;
        }

        if (hex.Row != row)
        {
            hex.Row = row;
            changed = true;
        }

        return changed;
    }

    private static void ApplyHarbourFixes(HexSpawner.HexSpawnerState state, HexGridConfig gridConfig, GameConstants constants, HarbourBoardSnapshot snapshot, ref HexStateRepairReport report)
    {
        if (state == null || state.hexes == null) return;
        if (constants == null)
        {
            return;
        }

        for (int col = 0; col < state.hexes.Count && col < gridConfig.cols; col++)
        {
            var column = state.hexes[col];
            if (column == null) continue;

            for (int row = 0; row < column.Count && row < gridConfig.rows; row++)
            {
                var hex = column[row];
                if (hex == null) continue;

                if (!string.Equals(hex.HexType, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!snapshot.TryGetHarbourFacingDirection(col, row, constants, out var direction))
                {
                    if (!string.Equals(hex.HexType, GameConstants.CAR_TYPE_SEA, StringComparison.OrdinalIgnoreCase))
                    {
                        hex.HexType = GameConstants.CAR_TYPE_SEA;
                        hex.Rotation = 0;
                        report.HarboursRemoved++;
                        snapshot.UpdateCell(col, row, hex.HexType);
                    }

                    continue;
                }

                var desiredRotation = direction * 60;
                if (hex.Rotation != desiredRotation)
                {
                    hex.Rotation = desiredRotation;
                    report.HarbourRotationsFixed++;
                }
            }
        }
    }

    private static Hex.HexState CreateDefaultHexState(int col, int row)
    {
        return new Hex.HexState
        {
            Col = col,
            Row = row,
            HexType = GameConstants.CAR_TYPE_SEA,
            HexSubType = string.Empty,
            Rotation = 0
        };
    }
}

public struct HexStateRepairReport
{
    public int CellsCreated;
    public int CoordinatesFixed;
    public int HexTypesDefaulted;
    public int RotationNormalized;
    public int HarboursRemoved;
    public int HarbourRotationsFixed;

    public bool HasChanges => CellsCreated > 0 || CoordinatesFixed > 0 || HexTypesDefaulted > 0 || RotationNormalized > 0 || HarboursRemoved > 0 || HarbourRotationsFixed > 0;
}
