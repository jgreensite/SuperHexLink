using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SuperHexLink.Validation;

/// <summary>
/// Validates map state for referential integrity and configuration correctness.
/// </summary>
public static class MapValidator
{
    /// <summary>
    /// Valid land types that can be assigned to hexes.
    /// </summary>
    private static readonly HashSet<string> ValidLandTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        GameConstants.CAR_TYPE_FOREST,
        GameConstants.CAR_TYPE_PASTURE,
        GameConstants.CAR_TYPE_FIELD,
        GameConstants.CAR_TYPE_HILL,
        GameConstants.CAR_TYPE_MOUNTAIN,
        GameConstants.CAR_TYPE_MINE,
        GameConstants.CAR_TYPE_GOLD,
        GameConstants.CAR_TYPE_SEA,
        GameConstants.CAR_TYPE_HARBOUR,
        GameConstants.CAR_TYPE_DESERT,
        GameConstants.CAR_TYPE_NONE,
        GameConstants.CAR_TYPE_WORD_NULL
    };

    /// <summary>
    /// Land types that should have a number token.
    /// </summary>
    private static readonly HashSet<string> NumberedLandTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        GameConstants.CAR_TYPE_FOREST,
        GameConstants.CAR_TYPE_PASTURE,
        GameConstants.CAR_TYPE_FIELD,
        GameConstants.CAR_TYPE_HILL,
        GameConstants.CAR_TYPE_MOUNTAIN,
        GameConstants.CAR_TYPE_MINE,
        GameConstants.CAR_TYPE_GOLD
    };

    /// <summary>
    /// Land types that should NOT have a number token.
    /// </summary>
    private static readonly HashSet<string> NonNumberedLandTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        GameConstants.CAR_TYPE_SEA,
        GameConstants.CAR_TYPE_HARBOUR,
        GameConstants.CAR_TYPE_DESERT,
        GameConstants.CAR_TYPE_NONE,
        GameConstants.CAR_TYPE_WORD_NULL
    };

    /// <summary>
    /// Performs comprehensive validation of the current map state.
    /// </summary>
    public static MapValidationResult ValidateMap(
        GameSpawner.GameSpawnerState gameState,
        HexSpawner.HexSpawnerState hexState,
        EdgeSpawner.EdgeSpawnerState edgeState = null,
        CornerSpawner.CornerSpawnerState cornerState = null,
        GameConstants constants = null)
    {
        var result = new MapValidationResult();

        // Validate game state exists
        if (gameState == null)
        {
            result.AddError(ValidationCategory.GridStructure, "GameSpawnerState is null - no game configuration loaded");
            return result;
        }

        // Validate grid configuration
        ValidateGridConfig(gameState.hexGridConfig, result);

        // Validate land configs
        ValidateLandConfigs(gameState.landConfigs, result);

        // Validate num configs
        ValidateNumConfigs(gameState.numConfigs, result);

        // Validate hex state
        if (hexState == null)
        {
            result.AddError(ValidationCategory.HexState, "HexSpawnerState is null - no hex data loaded");
        }
        else
        {
            ValidateHexState(hexState, gameState.hexGridConfig, gameState.landConfigs, gameState.numConfigs, result, constants);
        }

        // Validate edge state if provided
        if (edgeState != null)
        {
            ValidateEdgeState(edgeState, gameState.hexGridConfig, result);
        }

        // Validate corner state if provided
        if (cornerState != null)
        {
            ValidateCornerState(cornerState, gameState.hexGridConfig, result);
        }

        return result;
    }

    /// <summary>
    /// Validates the grid configuration.
    /// </summary>
    private static void ValidateGridConfig(HexGridConfig config, MapValidationResult result)
    {
        if (config.cols <= 0)
        {
            result.AddError(ValidationCategory.GridStructure, $"Invalid grid columns: {config.cols} (must be > 0)");
        }
        else if (config.cols > 50)
        {
            result.AddWarning(ValidationCategory.GridStructure, $"Very large grid width: {config.cols} columns (may cause performance issues)");
        }

        if (config.rows <= 0)
        {
            result.AddError(ValidationCategory.GridStructure, $"Invalid grid rows: {config.rows} (must be > 0)");
        }
        else if (config.rows > 50)
        {
            result.AddWarning(ValidationCategory.GridStructure, $"Very large grid height: {config.rows} rows (may cause performance issues)");
        }

        if (config.radius <= 0)
        {
            result.AddWarning(ValidationCategory.GridStructure, $"Invalid hex radius: {config.radius} (should be > 0)");
        }

        if (config.height < 0)
        {
            result.AddWarning(ValidationCategory.GridStructure, $"Negative hex height: {config.height}");
        }

        result.AddInfo(ValidationCategory.GridStructure, $"Grid size: {config.cols}x{config.rows} ({config.cols * config.rows} cells)");
    }

    /// <summary>
    /// Validates land configurations.
    /// </summary>
    private static void ValidateLandConfigs(List<GameSpawner.LandConfig> landConfigs, MapValidationResult result)
    {
        if (landConfigs == null || landConfigs.Count == 0)
        {
            result.AddError(ValidationCategory.LandConfig, "No land configurations defined - spawning will fail");
            return;
        }

        var groupedByGroup = landConfigs.GroupBy(c => c.landGroupID ?? "").ToList();
        
        foreach (var group in groupedByGroup)
        {
            var groupId = string.IsNullOrEmpty(group.Key) ? "(empty)" : group.Key;
            var totalCount = group.Sum(c => c.landCnt);
            
            result.AddInfo(ValidationCategory.LandConfig, $"GroupID '{groupId}': {group.Count()} types, {totalCount} total hexes");

            foreach (var config in group)
            {
                if (string.IsNullOrWhiteSpace(config.landType))
                {
                    result.AddError(ValidationCategory.LandConfig, $"Land config in group '{groupId}' has empty landType");
                }
                else if (!ValidLandTypes.Contains(config.landType))
                {
                    result.AddWarning(ValidationCategory.LandConfig, $"Unknown land type '{config.landType}' in group '{groupId}'");
                }

                if (config.landCnt < 0)
                {
                    result.AddError(ValidationCategory.LandConfig, $"Negative count ({config.landCnt}) for land type '{config.landType}' in group '{groupId}'");
                }
                else if (config.landCnt == 0)
                {
                    result.AddInfo(ValidationCategory.LandConfig, $"Zero count for land type '{config.landType}' in group '{groupId}'");
                }
            }
        }
    }

    /// <summary>
    /// Validates number configurations.
    /// </summary>
    private static void ValidateNumConfigs(List<GameSpawner.NumConfig> numConfigs, MapValidationResult result)
    {
        if (numConfigs == null || numConfigs.Count == 0)
        {
            result.AddWarning(ValidationCategory.NumConfig, "No number configurations defined - hexes won't have numbers");
            return;
        }

        var groupedByGroup = numConfigs.GroupBy(c => c.numGroupID ?? "").ToList();
        
        foreach (var group in groupedByGroup)
        {
            var groupId = string.IsNullOrEmpty(group.Key) ? "(empty)" : group.Key;
            var totalCount = group.Sum(c => c.numCnt);
            
            result.AddInfo(ValidationCategory.NumConfig, $"Num GroupID '{groupId}': {group.Count()} types, {totalCount} total numbers");

            foreach (var config in group)
            {
                if (config.numType < 2 || config.numType > 12 || config.numType == 7)
                {
                    result.AddWarning(ValidationCategory.NumConfig, $"Unusual number type {config.numType} in group '{groupId}' (valid: 2-6, 8-12)");
                }

                if (config.numCnt < 0)
                {
                    result.AddError(ValidationCategory.NumConfig, $"Negative count ({config.numCnt}) for number {config.numType} in group '{groupId}'");
                }
            }
        }
    }

    /// <summary>
    /// Validates hex state array and individual hexes.
    /// </summary>
    private static void ValidateHexState(
        HexSpawner.HexSpawnerState hexState,
        HexGridConfig gridConfig,
        List<GameSpawner.LandConfig> landConfigs,
        List<GameSpawner.NumConfig> numConfigs,
        MapValidationResult result,
        GameConstants constants)
    {
        if (hexState.hexes == null)
        {
            result.AddError(ValidationCategory.HexState, "Hex array is null");
            return;
        }

        // Check column count matches grid
        if (hexState.hexes.Count != gridConfig.cols)
        {
            result.AddError(ValidationCategory.GridStructure, 
                $"Hex column count ({hexState.hexes.Count}) doesn't match grid config ({gridConfig.cols})");
        }

        // Build a set of GroupIDs from landConfigs for validation
        var validGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (landConfigs != null)
        {
            foreach (var lc in landConfigs)
            {
                if (!string.IsNullOrEmpty(lc.landGroupID))
                {
                    validGroupIds.Add(lc.landGroupID);
                }
            }
        }

        // Track land type usage for config validation
        var landTypeUsage = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var numUsage = new Dictionary<string, Dictionary<int, int>>(StringComparer.OrdinalIgnoreCase);

        // Validate each hex
        for (int col = 0; col < hexState.hexes.Count; col++)
        {
            var column = hexState.hexes[col];
            
            if (column == null)
            {
                result.AddError(ValidationCategory.GridStructure, $"Column {col} is null");
                continue;
            }

            if (column.Count != gridConfig.rows)
            {
                result.AddError(ValidationCategory.GridStructure, 
                    $"Column {col} has {column.Count} rows, expected {gridConfig.rows}");
            }

            for (int row = 0; row < column.Count; row++)
            {
                var hex = column[row];
                
                if (hex == null)
                {
                    result.AddError(ValidationCategory.HexState, $"Hex is null", col, row);
                    continue;
                }

                // Validate coordinates match position in array
                if (hex.Col != col)
                {
                    result.AddWarning(ValidationCategory.HexState, 
                        $"Hex.Col ({hex.Col}) doesn't match array position ({col})", col, row);
                }
                if (hex.Row != row)
                {
                    result.AddWarning(ValidationCategory.HexState, 
                        $"Hex.Row ({hex.Row}) doesn't match array position ({row})", col, row);
                }

                // Validate hex type
                ValidateHexType(hex, col, row, result);

                // Validate GroupID
                ValidateGroupID(hex, col, row, validGroupIds, result);

                // Validate rotation
                ValidateRotation(hex, col, row, result);

                // Validate number
                ValidateHexNumber(hex, col, row, result);

                // Track usage for later config validation
                TrackHexUsage(hex, landTypeUsage, numUsage);

                // Special validation for harbours
                if (string.Equals(hex.HexType, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase))
                {
                    ValidateHarbour(hex, col, row, hexState, gridConfig, constants, result);
                }
            }
        }

        // Validate land type counts against configs
        ValidateLandTypeUsage(landTypeUsage, landConfigs, result);

        // Validate number usage against configs
        ValidateNumUsage(numUsage, numConfigs, result);
    }

    private static void ValidateHexType(Hex.HexState hex, int col, int row, MapValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(hex.HexType))
        {
            result.AddError(ValidationCategory.HexState, "HexType is empty or null", col, row);
            return;
        }

        if (!ValidLandTypes.Contains(hex.HexType))
        {
            result.AddWarning(ValidationCategory.HexState, 
                $"Unknown HexType '{hex.HexType}'", col, row);
        }

        // Check for literal "null" string
        if (string.Equals(hex.HexType, GameConstants.CAR_TYPE_WORD_NULL, StringComparison.OrdinalIgnoreCase))
        {
            result.AddInfo(ValidationCategory.HexState, 
                "HexType is 'null' string (off-board space)", col, row);
        }
    }

    private static void ValidateGroupID(Hex.HexState hex, int col, int row, HashSet<string> validGroupIds, MapValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(hex.GroupID))
        {
            // Only warn if this is a land hex that should have been assigned from configs
            if (!IsOffBoardType(hex.HexType))
            {
                var issue = ValidationIssue.Warning(ValidationCategory.GroupID, 
                    $"GroupID is empty (will default to '1' during refresh)", col, row);
                issue.FixType = FixType.AutoFix;
                issue.FixValue = "1";
                issue.SuggestedFix = "Set GroupID to '1'";
                issue.FieldName = "GroupID";
                issue.CurrentValue = "(empty)";
                result.AddIssue(issue);
            }
            return;
        }

        if (validGroupIds.Count > 0 && !validGroupIds.Contains(hex.GroupID))
        {
            var issue = ValidationIssue.Warning(ValidationCategory.GroupID, 
                $"GroupID '{hex.GroupID}' not found in landConfigs", col, row);
            issue.FixType = FixType.ManualEdit;
            issue.FieldName = "GroupID";
            issue.CurrentValue = hex.GroupID;
            issue.SuggestedFix = $"Change to one of: {string.Join(", ", validGroupIds)}";
            result.AddIssue(issue);
        }
    }

    private static void ValidateRotation(Hex.HexState hex, int col, int row, MapValidationResult result)
    {
        // Rotation should be 0-359, and typically a multiple of 60 for hex grids
        if (hex.Rotation < 0 || hex.Rotation >= 360)
        {
            int normalizedRotation = ((hex.Rotation % 360) + 360) % 360;
            var issue = ValidationIssue.Warning(ValidationCategory.HexState, 
                $"Rotation {hex.Rotation} is out of normal range (0-359)", col, row);
            issue.FixType = FixType.AutoFix;
            issue.FixValue = normalizedRotation.ToString();
            issue.FieldName = "Rotation";
            issue.CurrentValue = hex.Rotation.ToString();
            issue.SuggestedFix = $"Normalize to {normalizedRotation}°";
            result.AddIssue(issue);
        }
        else if (hex.Rotation % 60 != 0)
        {
            // Non-standard rotation - only warn for types where rotation matters
            if (string.Equals(hex.HexType, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase))
            {
                int snappedRotation = Mathf.RoundToInt(hex.Rotation / 60f) * 60;
                var issue = ValidationIssue.Warning(ValidationCategory.HexState, 
                    $"Harbour rotation {hex.Rotation} is not a multiple of 60°", col, row);
                issue.FixType = FixType.SaveAndReload;
                issue.FieldName = "Rotation";
                issue.CurrentValue = hex.Rotation.ToString();
                issue.SuggestedFix = $"Save & Reload will auto-fix harbour rotation to face valid land";
                result.AddIssue(issue);
            }
        }
    }

    private static void ValidateHexNumber(Hex.HexState hex, int col, int row, MapValidationResult result)
    {
        bool shouldHaveNumber = NumberedLandTypes.Contains(hex.HexType);
        bool hasNumber = hex.HexNum.HasValue && hex.HexNum.Value > 0;

        if (shouldHaveNumber && !hasNumber)
        {
            var issue = ValidationIssue.Warning(ValidationCategory.HexState, 
                $"Land type '{hex.HexType}' should have a number but doesn't", col, row);
            issue.FixType = FixType.SaveAndReload;
            issue.FieldName = "HexNum";
            issue.CurrentValue = "(none)";
            issue.SuggestedFix = "Save & Reload will assign a number from numConfigs";
            result.AddIssue(issue);
        }
        else if (!shouldHaveNumber && hasNumber)
        {
            var issue = ValidationIssue.Info(ValidationCategory.HexState, 
                $"Land type '{hex.HexType}' has number {hex.HexNum} but shouldn't", col, row);
            issue.FixType = FixType.AutoFix;
            issue.FixValue = null; // Clear the number
            issue.FieldName = "HexNum";
            issue.CurrentValue = hex.HexNum.ToString();
            issue.SuggestedFix = "Remove number token";
            result.AddIssue(issue);
        }

        if (hasNumber)
        {
            if (hex.HexNum < 2 || hex.HexNum > 12 || hex.HexNum == 7)
            {
                result.AddWarning(ValidationCategory.HexState, 
                    $"Invalid number token value: {hex.HexNum}", col, row);
            }
        }
    }

    private static void ValidateHarbour(Hex.HexState hex, int col, int row, 
        HexSpawner.HexSpawnerState hexState, HexGridConfig gridConfig, 
        GameConstants constants, MapValidationResult result)
    {
        // Check if harbour has valid subtype
        if (string.IsNullOrWhiteSpace(hex.HexSubType))
        {
            result.AddInfo(ValidationCategory.Harbour, 
                "Harbour has no subtype (generic harbour)", col, row);
        }

        // Check if harbour is on the edge of the board (typical requirement)
        bool isEdge = col == 0 || col == gridConfig.cols - 1 || 
                      row == 0 || row == gridConfig.rows - 1;
        
        if (!isEdge)
        {
            result.AddInfo(ValidationCategory.Harbour, 
                "Harbour is not on board edge", col, row);
        }

        // Check if harbour faces a valid land type
        if (constants != null && hexState != null)
        {
            bool facesLand = CheckHarbourFacesLand(col, row, hex.Rotation, hexState, gridConfig, constants);
            if (!facesLand)
            {
                result.AddWarning(ValidationCategory.Harbour, 
                    $"Harbour at rotation {hex.Rotation}° doesn't face valid land", col, row);
            }
        }
    }

    private static bool CheckHarbourFacesLand(int col, int row, int rotation, 
        HexSpawner.HexSpawnerState hexState, HexGridConfig gridConfig, GameConstants constants)
    {
        // Direction index based on rotation (0=E, 60=SE, 120=SW, 180=W, 240=NW, 300=NE)
        int directionIndex = (rotation / 60) % 6;
        
        // Get neighbor coordinates based on direction (odd-q layout)
        var (neighborCol, neighborRow) = GetNeighborCoordinates(col, row, directionIndex);
        
        // Check bounds
        if (neighborCol < 0 || neighborCol >= gridConfig.cols || 
            neighborRow < 0 || neighborRow >= gridConfig.rows)
        {
            return false;
        }

        // Check if neighbor is valid land
        if (hexState.hexes == null || neighborCol >= hexState.hexes.Count)
            return false;
        
        var neighborColumn = hexState.hexes[neighborCol];
        if (neighborColumn == null || neighborRow >= neighborColumn.Count)
            return false;

        var neighbor = neighborColumn[neighborRow];
        if (neighbor == null)
            return false;

        // Check if neighbor is a land type that harbours can face
        return constants.harbourFacingLandTypes?.Contains(neighbor.HexType) ?? false;
    }

    private static (int col, int row) GetNeighborCoordinates(int col, int row, int direction)
    {
        // Odd-q offset coordinates - odd columns are shifted down
        bool isOddCol = col % 2 == 1;
        
        // Direction offsets for odd-q layout
        // 0=E, 1=SE, 2=SW, 3=W, 4=NW, 5=NE
        var evenColOffsets = new (int dc, int dr)[]
        {
            (1, 0),   // E
            (1, 1),   // SE
            (0, 1),   // SW (actually S for even cols)
            (-1, 0),  // W
            (0, -1),  // NW (actually N for even cols)
            (1, -1)   // NE
        };
        
        var oddColOffsets = new (int dc, int dr)[]
        {
            (1, 0),   // E
            (1, 0),   // SE (shifted)
            (0, 1),   // SW
            (-1, 0),  // W
            (-1, -1), // NW
            (0, -1)   // NE
        };

        var offsets = isOddCol ? oddColOffsets : evenColOffsets;
        var offset = offsets[direction % 6];
        
        return (col + offset.dc, row + offset.dr);
    }

    private static void TrackHexUsage(Hex.HexState hex, 
        Dictionary<string, Dictionary<string, int>> landTypeUsage,
        Dictionary<string, Dictionary<int, int>> numUsage)
    {
        if (hex == null) return;

        var groupId = hex.GroupID ?? "1"; // Default to "1" as the code does
        
        // Track land type
        if (!string.IsNullOrWhiteSpace(hex.HexType) && !IsOffBoardType(hex.HexType))
        {
            if (!landTypeUsage.ContainsKey(groupId))
                landTypeUsage[groupId] = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            
            if (!landTypeUsage[groupId].ContainsKey(hex.HexType))
                landTypeUsage[groupId][hex.HexType] = 0;
            
            landTypeUsage[groupId][hex.HexType]++;
        }

        // Track number
        if (hex.HexNum.HasValue && hex.HexNum > 0)
        {
            if (!numUsage.ContainsKey(groupId))
                numUsage[groupId] = new Dictionary<int, int>();
            
            if (!numUsage[groupId].ContainsKey(hex.HexNum.Value))
                numUsage[groupId][hex.HexNum.Value] = 0;
            
            numUsage[groupId][hex.HexNum.Value]++;
        }
    }

    private static void ValidateLandTypeUsage(
        Dictionary<string, Dictionary<string, int>> usage,
        List<GameSpawner.LandConfig> configs,
        MapValidationResult result)
    {
        if (configs == null || configs.Count == 0) return;

        var configsByGroup = configs.GroupBy(c => c.landGroupID ?? "").ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in usage)
        {
            var groupId = kvp.Key;
            var typeCounts = kvp.Value;

            if (!configsByGroup.TryGetValue(groupId, out var groupConfigs))
            {
                result.AddInfo(ValidationCategory.LandConfig, 
                    $"Hexes use GroupID '{groupId}' which has no landConfig entries");
                continue;
            }

            foreach (var typeKvp in typeCounts)
            {
                var landType = typeKvp.Key;
                var actualCount = typeKvp.Value;
                
                var config = groupConfigs.FirstOrDefault(c => 
                    string.Equals(c.landType, landType, StringComparison.OrdinalIgnoreCase));
                
                if (config == null)
                {
                    result.AddInfo(ValidationCategory.LandConfig, 
                        $"Land type '{landType}' used {actualCount} times in group '{groupId}' but not in config");
                }
                else if (actualCount != config.landCnt)
                {
                    result.AddInfo(ValidationCategory.LandConfig, 
                        $"Land type '{landType}' in group '{groupId}': used {actualCount}, config expects {config.landCnt}");
                }
            }
        }
    }

    private static void ValidateNumUsage(
        Dictionary<string, Dictionary<int, int>> usage,
        List<GameSpawner.NumConfig> configs,
        MapValidationResult result)
    {
        if (configs == null || configs.Count == 0) return;

        var configsByGroup = configs.GroupBy(c => c.numGroupID ?? "").ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in usage)
        {
            var groupId = kvp.Key;
            var numCounts = kvp.Value;

            if (!configsByGroup.TryGetValue(groupId, out var groupConfigs))
            {
                result.AddInfo(ValidationCategory.NumConfig, 
                    $"Hexes use numGroupID '{groupId}' which has no numConfig entries");
                continue;
            }

            foreach (var numKvp in numCounts)
            {
                var numType = numKvp.Key;
                var actualCount = numKvp.Value;
                
                var config = groupConfigs.FirstOrDefault(c => c.numType == numType);
                
                if (config == null)
                {
                    result.AddInfo(ValidationCategory.NumConfig, 
                        $"Number {numType} used {actualCount} times in group '{groupId}' but not in config");
                }
                else if (actualCount != config.numCnt)
                {
                    result.AddInfo(ValidationCategory.NumConfig, 
                        $"Number {numType} in group '{groupId}': used {actualCount}, config expects {config.numCnt}");
                }
            }
        }
    }

    private static void ValidateEdgeState(EdgeSpawner.EdgeSpawnerState edgeState, HexGridConfig gridConfig, MapValidationResult result)
    {
        // Basic edge state validation
        if (edgeState == null)
        {
            result.AddInfo(ValidationCategory.Reference, "EdgeSpawnerState is null");
            return;
        }

        // Add edge-specific validations as needed
        result.AddInfo(ValidationCategory.Reference, "Edge state present");
    }

    private static void ValidateCornerState(CornerSpawner.CornerSpawnerState cornerState, HexGridConfig gridConfig, MapValidationResult result)
    {
        // Basic corner state validation
        if (cornerState == null)
        {
            result.AddInfo(ValidationCategory.Reference, "CornerSpawnerState is null");
            return;
        }

        // Add corner-specific validations as needed
        result.AddInfo(ValidationCategory.Reference, "Corner state present");
    }

    private static bool IsOffBoardType(string hexType)
    {
        return string.IsNullOrWhiteSpace(hexType) ||
               string.Equals(hexType, GameConstants.CAR_TYPE_NONE, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(hexType, GameConstants.CAR_TYPE_WORD_NULL, StringComparison.OrdinalIgnoreCase);
    }
}
