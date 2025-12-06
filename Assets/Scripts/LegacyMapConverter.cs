using System;
using System.Collections.Generic;
using System.Linq;
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

            // Parse hex states - match entire state blocks and extract fields individually
            // This handles various formats: "HexSubType": "value", "HexSubType": null, etc.
            var stateBlockPattern = new Regex(@"""state"":\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}", RegexOptions.Singleline);
            var stateMatches = stateBlockPattern.Matches(jsonContent);

            var flatHexes = new List<Hex.HexState>();
            bool anyHexHasColRow = false;

            foreach (Match stateMatch in stateMatches)
            {
                string stateContent = stateMatch.Groups[1].Value;

                // Extract HexType (required)
                var hexTypeMatch = Regex.Match(stateContent, @"""HexType"":\s*""([^""]*)""", RegexOptions.IgnoreCase);
                string hexType = hexTypeMatch.Success ? hexTypeMatch.Groups[1].Value : "";

                // Extract HexSubType (can be string or null)
                var hexSubTypeMatch = Regex.Match(stateContent, @"""HexSubType"":\s*(?:""([^""]*)""|null)", RegexOptions.IgnoreCase);
                string hexSubType = hexSubTypeMatch.Success ? (hexSubTypeMatch.Groups[1].Value ?? "") : "";

                // Extract Rotation
                var rotationMatch = Regex.Match(stateContent, @"""Rotation"":\s*(\d+)", RegexOptions.IgnoreCase);
                int rotation = rotationMatch.Success ? int.Parse(rotationMatch.Groups[1].Value) : 0;

                // Extract col/row if present (some formats have lowercase)
                var colMatch = Regex.Match(stateContent, @"""col"":\s*(\d+)", RegexOptions.IgnoreCase);
                var rowMatch = Regex.Match(stateContent, @"""row"":\s*(\d+)", RegexOptions.IgnoreCase);
                int? col = colMatch.Success ? int.Parse(colMatch.Groups[1].Value) : (int?)null;
                int? row = rowMatch.Success ? int.Parse(rowMatch.Groups[1].Value) : (int?)null;
                
                // Track whether ANY hex has col/row data in file (not just non-zero values)
                if (colMatch.Success && rowMatch.Success)
                {
                    anyHexHasColRow = true;
                }

                var hexData = new Hex.HexState
                {
                    HexType = hexType,
                    HexSubType = hexSubType,
                    Rotation = rotation,
                    Col = col ?? 0,
                    Row = row ?? 0
                };

                // Track if we didn't find col/row in file
                if (!col.HasValue || !row.HasValue)
                {
                    result.Report.HexesMissingCoords++;
                }

                // Only normalize truly empty/missing types to sea
                // Keep "null" as-is since it represents intentional empty/off-board hexes
                if (string.IsNullOrWhiteSpace(hexData.HexType))
                {
                    UnityEngine.Debug.Log($"LegacyMapConverter: Converting empty HexType to 'sea'");
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

            // Create the hex state structure
            var hexState = new HexSpawner.HexSpawnerState
            {
                hexes = new List<List<Hex.HexState>>()
            };

            // Use col/row data if the file contained those fields (even if values are 0)
            bool hasColRowData = anyHexHasColRow;

            if (hasColRowData)
            {
                // Organize by col/row from the data itself
                var hexesByCol = flatHexes.GroupBy(h => h.Col).OrderBy(g => g.Key);
                foreach (var colGroup in hexesByCol)
                {
                    var column = colGroup.OrderBy(h => h.Row).ToList();
                    hexState.hexes.Add(column);
                }

                // Update grid dimensions based on actual data
                if (hexState.hexes.Count > 0)
                {
                    result.Report.GridCols = hexState.hexes.Count;
                    result.Report.GridRows = hexState.hexes.Max(c => c.Count);
                }
            }
            else
            {
                // Organize into 2D grid based on position in array (column-major)
                for (int c = 0; c < result.Report.GridCols; c++)
                {
                    var column = new List<Hex.HexState>();
                    for (int r = 0; r < result.Report.GridRows; r++)
                    {
                        int flatIndex = c * result.Report.GridRows + r;
                        if (flatIndex < flatHexes.Count)
                        {
                            var hex = flatHexes[flatIndex];
                            hex.Col = c;
                            hex.Row = r;
                            column.Add(hex);
                        }
                        else
                        {
                            // Fill missing with default sea hex
                            column.Add(new Hex.HexState
                            {
                                Col = c,
                                Row = r,
                                HexType = GameConstants.CAR_TYPE_SEA,
                                HexSubType = "",
                                Rotation = 0
                            });
                        }
                    }
                    hexState.hexes.Add(column);
                }
            }

            // Create game state with grid config
            var gameState = new GameSpawner.GameSpawnerState();
            gameState.hexGridConfig = new HexGridConfig
            {
                cols = result.Report.GridCols,
                rows = result.Report.GridRows,
                radius = 1,
                height = 1
            };

            // Parse landConfigs from legacy format
            gameState.landConfigs = ParseLegacyLandConfigs(jsonContent);
            if (gameState.landConfigs == null || gameState.landConfigs.Count == 0)
            {
                result.Report.Warnings.Add("No landConfigs found in legacy file, using defaults");
                gameState.landConfigs = GameSpawner.GameSpawnerState.CreateDefaultLandConfigs();
            }

            // Parse numConfigs from legacy format
            gameState.numConfigs = ParseLegacyNumConfigs(jsonContent);
            if (gameState.numConfigs == null || gameState.numConfigs.Count == 0)
            {
                result.Report.Warnings.Add("No numConfigs found in legacy file, using defaults");
                gameState.numConfigs = GameSpawner.GameSpawnerState.CreateDefaultNumConfigs();
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

            int totalExpected = result.Report.GridCols * result.Report.GridRows;
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

        // Legacy format has HexSpawnerState as root type (with or without type ID prefix)
        return jsonContent.Contains("\"$type\": \"HexSpawnerState") ||
               jsonContent.Contains("\"$type\": \"0|HexSpawnerState");
    }

    /// <summary>
    /// Parses landConfigs from legacy JSON format.
    /// </summary>
    private static List<GameSpawner.LandConfig> ParseLegacyLandConfigs(string jsonContent)
    {
        var configs = new List<GameSpawner.LandConfig>();

        try
        {
            // Find the landConfigs section and extract entries
            // Pattern matches: "landGroupID": "1", "landCnt": 5, "landType": "hill"
            var landConfigPattern = new Regex(
                @"\{\s*" +
                @"(?:[^{}]*""landGroupID"":\s*""([^""]*)"")?" +
                @"[^{}]*""landCnt"":\s*(\d+)" +
                @"[^{}]*""landType"":\s*""([^""]*)""" +
                @"[^{}]*\}",
                RegexOptions.Singleline);

            // Also try alternate field order
            var landConfigPatternAlt = new Regex(
                @"\{\s*" +
                @"[^{}]*""landType"":\s*""([^""]*)""" +
                @"[^{}]*""landCnt"":\s*(\d+)" +
                @"(?:[^{}]*""landGroupID"":\s*""([^""]*)"")?" +
                @"[^{}]*\}",
                RegexOptions.Singleline);

            // Find the landConfigs array section
            var landConfigsSection = Regex.Match(jsonContent, @"""landConfigs"":\s*\{[^}]*""\$rcontent"":\s*\[(.*?)\]\s*\}", RegexOptions.Singleline);
            if (landConfigsSection.Success)
            {
                string content = landConfigsSection.Groups[1].Value;

                var matches = landConfigPattern.Matches(content);
                foreach (Match match in matches)
                {
                    configs.Add(new GameSpawner.LandConfig
                    {
                        landGroupID = string.IsNullOrEmpty(match.Groups[1].Value) ? "1" : match.Groups[1].Value,
                        landCnt = int.Parse(match.Groups[2].Value),
                        landType = match.Groups[3].Value
                    });
                }

                // If no matches, try alternate pattern
                if (configs.Count == 0)
                {
                    matches = landConfigPatternAlt.Matches(content);
                    foreach (Match match in matches)
                    {
                        configs.Add(new GameSpawner.LandConfig
                        {
                            landType = match.Groups[1].Value,
                            landCnt = int.Parse(match.Groups[2].Value),
                            landGroupID = string.IsNullOrEmpty(match.Groups[3].Value) ? "1" : match.Groups[3].Value
                        });
                    }
                }
            }

            UnityEngine.Debug.Log($"LegacyMapConverter: Parsed {configs.Count} landConfigs");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"LegacyMapConverter: Failed to parse landConfigs: {ex.Message}");
        }

        return configs;
    }

    /// <summary>
    /// Parses numConfigs from legacy JSON format.
    /// </summary>
    private static List<GameSpawner.NumConfig> ParseLegacyNumConfigs(string jsonContent)
    {
        var configs = new List<GameSpawner.NumConfig>();

        try
        {
            // Find the numConfigs array section
            var numConfigsSection = Regex.Match(jsonContent, @"""numConfigs"":\s*\{[^}]*""\$rcontent"":\s*\[(.*?)\]\s*\}", RegexOptions.Singleline);
            if (numConfigsSection.Success)
            {
                string content = numConfigsSection.Groups[1].Value;

                // Pattern: "numGroupID": "1", "numCnt": 2, "numType": 2
                var numConfigPattern = new Regex(
                    @"\{\s*" +
                    @"(?:[^{}]*""numGroupID"":\s*""([^""]*)"")?" +
                    @"[^{}]*""numCnt"":\s*(\d+)" +
                    @"[^{}]*""numType"":\s*(\d+)" +
                    @"[^{}]*\}",
                    RegexOptions.Singleline);

                var matches = numConfigPattern.Matches(content);
                foreach (Match match in matches)
                {
                    configs.Add(new GameSpawner.NumConfig
                    {
                        numGroupID = string.IsNullOrEmpty(match.Groups[1].Value) ? "1" : match.Groups[1].Value,
                        numCnt = int.Parse(match.Groups[2].Value),
                        numType = int.Parse(match.Groups[3].Value)
                    });
                }
            }

            UnityEngine.Debug.Log($"LegacyMapConverter: Parsed {configs.Count} numConfigs");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"LegacyMapConverter: Failed to parse numConfigs: {ex.Message}");
        }

        return configs;
    }
}
