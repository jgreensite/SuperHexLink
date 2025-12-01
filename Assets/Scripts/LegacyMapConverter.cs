using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Converts legacy HexSpawnerState format to current CombinedSpawnerState format.
/// Legacy format stored List&lt;List&lt;Hex&gt;&gt; (MonoBehaviour references) instead of List&lt;List&lt;Hex.HexState&gt;&gt;.
/// </summary>
public static class LegacyMapConverter
{
    /// <summary>
    /// Result of attempting to convert a legacy map file.
    /// </summary>
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public GameSpawner.CombinedSpawnerState ConvertedState { get; set; }
        public ConversionReport Report { get; set; } = new ConversionReport();
    }

    /// <summary>
    /// Report of what was converted and what issues were found.
    /// </summary>
    public class ConversionReport
    {
        public int HexesConverted { get; set; }
        public int HexesMissingType { get; set; }
        public int HexesMissingCoords { get; set; }
        public int RotationsNormalized { get; set; }
        public int GridCols { get; set; }
        public int GridRows { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasWarnings => Warnings.Count > 0;

        public override string ToString()
        {
            return $"Converted {HexesConverted} hexes ({GridCols}x{GridRows} grid). " +
                   $"Missing types: {HexesMissingType}, Missing coords: {HexesMissingCoords}, " +
                   $"Rotations fixed: {RotationsNormalized}, Warnings: {Warnings.Count}";
        }
    }

    /// <summary>
    /// Attempts to convert legacy JSON to current format by parsing the JSON structure directly.
    /// </summary>
    public static ConversionResult TryConvertLegacyJson(string jsonContent)
    {
        var result = new ConversionResult();

        try
        {
            // Parse grid dimensions from hexGrid section
            var gridMatch = Regex.Match(jsonContent, @"""hexGrid"":\s*\{[^}]*""cols"":\s*(\d+)[^}]*""rows"":\s*(\d+)", RegexOptions.Singleline);
            if (!gridMatch.Success)
            {
                // Try alternate order
                gridMatch = Regex.Match(jsonContent, @"""hexGrid"":\s*\{[^}]*""rows"":\s*(\d+)[^}]*""cols"":\s*(\d+)", RegexOptions.Singleline);
                if (gridMatch.Success)
                {
                    result.Report.GridRows = int.Parse(gridMatch.Groups[1].Value);
                    result.Report.GridCols = int.Parse(gridMatch.Groups[2].Value);
                }
            }
            else
            {
                result.Report.GridCols = int.Parse(gridMatch.Groups[1].Value);
                result.Report.GridRows = int.Parse(gridMatch.Groups[2].Value);
            }

            if (result.Report.GridCols <= 0 || result.Report.GridRows <= 0)
            {
                result.Report.Warnings.Add("Could not determine grid dimensions, using defaults");
                result.Report.GridCols = 7;
                result.Report.GridRows = 7;
            }

            // Create the converted state
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = new HexGridConfig
            {
                cols = result.Report.GridCols,
                rows = result.Report.GridRows,
                radius = 1,
                height = 1
            };

            var hexState = new HexSpawner.HexSpawnerState
            {
                hexes = new List<List<Hex.HexState>>()
            };

            // Parse hex states from the nested structure
            // Pattern: "state": { ... "HexType": "xxx", "HexSubType": "xxx", "Rotation": N, ... }
            var hexStatePattern = new Regex(
                @"""state"":\s*\{[^{}]*?" +
                @"""HexType"":\s*""([^""]*)""\s*," +
                @"[^{}]*?""HexSubType"":\s*""([^""]*)""\s*," +
                @"[^{}]*?""Rotation"":\s*(\d+)",
                RegexOptions.Singleline);

            var matches = hexStatePattern.Matches(jsonContent);

            // Organize into grid based on expected dimensions
            int totalExpected = result.Report.GridCols * result.Report.GridRows;
            var flatHexes = new List<Hex.HexState>();

            foreach (Match match in matches)
            {
                var hexData = new Hex.HexState
                {
                    HexType = match.Groups[1].Value,
                    HexSubType = match.Groups[2].Value,
                    Rotation = int.Parse(match.Groups[3].Value)
                };

                // Normalize "null" string to sea
                if (string.Equals(hexData.HexType, "null", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(hexData.HexType))
                {
                    hexData.HexType = GameConstants.CAR_TYPE_SEA;
                    result.Report.HexesMissingType++;
                }

                // Normalize rotation to 0-359
                var normalizedRotation = ((hexData.Rotation % 360) + 360) % 360;
                if (normalizedRotation != hexData.Rotation)
                {
                    hexData.Rotation = normalizedRotation;
                    result.Report.RotationsNormalized++;
                }

                flatHexes.Add(hexData);
                result.Report.HexesConverted++;
            }

            // Organize into 2D grid (column-major as expected by current format)
            for (int col = 0; col < result.Report.GridCols; col++)
            {
                var column = new List<Hex.HexState>();
                for (int row = 0; row < result.Report.GridRows; row++)
                {
                    int flatIndex = col * result.Report.GridRows + row;
                    if (flatIndex < flatHexes.Count)
                    {
                        var hex = flatHexes[flatIndex];
                        hex.Col = col;
                        hex.Row = row;
                        column.Add(hex);
                    }
                    else
                    {
                        // Fill missing with default sea hex
                        column.Add(new Hex.HexState
                        {
                            Col = col,
                            Row = row,
                            HexType = GameConstants.CAR_TYPE_SEA,
                            HexSubType = "",
                            Rotation = 0
                        });
                        result.Report.HexesMissingCoords++;
                    }
                }
                hexState.hexes.Add(column);
            }

            // Build final state
            result.ConvertedState = new GameSpawner.CombinedSpawnerState
            {
                GameState = gameState,
                HexState = hexState,
                EdgeState = new EdgeSpawner.EdgeSpawnerState(),
                CornerState = new CornerSpawner.CornerSpawnerState()
            };

            if (result.Report.HexesConverted == 0)
            {
                result.Success = false;
                result.ErrorMessage = "No hex data found in legacy file";
                return result;
            }

            if (result.Report.HexesConverted != totalExpected)
            {
                result.Report.Warnings.Add($"Found {result.Report.HexesConverted} hexes but expected {totalExpected} for {result.Report.GridCols}x{result.Report.GridRows} grid");
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Failed to parse legacy format: {ex.Message}";
            return result;
        }
    }

    /// <summary>
    /// Checks if the JSON content appears to be in legacy format.
    /// </summary>
    public static bool IsLegacyFormat(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return false;

        // Legacy format has HexSpawnerState as root type
        return jsonContent.Contains("\"$type\": \"HexSpawnerState") ||
               jsonContent.Contains("\"$type\": \"0|HexSpawnerState");
    }
}
