using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Logging;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Validates building placement rules according to Catan-style game mechanics.
    /// Based on NotebookLM research for placement rules and spatial constraints.
    /// </summary>
    public class PlacementValidator
    {
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<int, CornerData> _cornerData;
        private readonly Dictionary<int, EdgeData> _edgeData;
        private readonly Dictionary<int, List<RoadConnection>> _roadNetwork;

        public PlacementValidator(ActionLogSettings logSettings)
        {
            _logSettings = logSettings;
            _cornerData = new Dictionary<int, CornerData>();
            _edgeData = new Dictionary<int, EdgeData>();
            _roadNetwork = new Dictionary<int, List<RoadConnection>>();
        }

        #region Settlement Placement Validation (E4-F3-S1)

        /// <summary>
        /// Validate settlement placement according to rules: empty corner, 2+ edges from other settlements.
        /// </summary>
        public PlacementValidationResult CanPlaceSettlement(int cornerId, int playerId, bool isInitialPlacement = false)
        {
            var result = new PlacementValidationResult { IsValid = true };

            // Check if corner exists
            if (!_cornerData.ContainsKey(cornerId))
            {
                result.IsValid = false;
                result.Reason = $"Corner {cornerId} does not exist";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Error,
                    "Settlement placement failed: {0}", result.Reason);
                return result;
            }

            var corner = _cornerData[cornerId];

            // Check if corner is empty
            if (!IsCornerEmpty(cornerId))
            {
                result.IsValid = false;
                result.Reason = $"Corner {cornerId} is already occupied by {corner.BuildingType}";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                    "Settlement placement failed: {0}", result.Reason);
                return result;
            }

            // During initial placement, skip distance rules
            if (!isInitialPlacement)
            {
                // Check distance rule: must be 2+ edges from other settlements
                var distanceValidation = ValidateSettlementDistance(cornerId);
                if (!distanceValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Reason = distanceValidation.Reason;
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                        "Settlement placement failed: {0}", result.Reason);
                    return result;
                }

                // Check if connected to player's road network
                if (!IsConnectedToPlayerNetwork(cornerId, playerId))
                {
                    result.IsValid = false;
                    result.Reason = $"Corner {cornerId} is not connected to player {playerId}'s road network";
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                        "Settlement placement failed: {0}", result.Reason);
                    return result;
                }
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Settlement placement validated for player {0} at corner {1}", playerId, cornerId);

            return result;
        }

        /// <summary>
        /// Validate settlement distance rule (2+ edges from other settlements).
        /// </summary>
        private PlacementValidationResult ValidateSettlementDistance(int cornerId)
        {
            var adjacentCorners = GetAdjacentCorners(cornerId);
            
            foreach (var adjacentCornerId in adjacentCorners)
            {
                if (_cornerData.ContainsKey(adjacentCornerId))
                {
                    var adjacentCorner = _cornerData[adjacentCornerId];
                    if (adjacentCorner.BuildingType != BuildingType.None)
                    {
                        return new PlacementValidationResult
                        {
                            IsValid = false,
                            Reason = $"Too close to existing {adjacentCorner.BuildingType} at corner {adjacentCornerId}"
                        };
                    }
                }
            }

            // Check corners two edges away
            var cornersTwoEdgesAway = GetCornersTwoEdgesAway(cornerId);
            foreach (var distantCornerId in cornersTwoEdgesAway)
            {
                if (_cornerData.ContainsKey(distantCornerId))
                {
                    var distantCorner = _cornerData[distantCornerId];
                    if (distantCorner.BuildingType != BuildingType.None)
                    {
                        return new PlacementValidationResult
                        {
                            IsValid = false,
                            Reason = $"Too close to existing {distantCorner.BuildingType} at corner {distantCornerId} (2 edges away)"
                        };
                    }
                }
            }

            return new PlacementValidationResult { IsValid = true };
        }

        /// <summary>
        /// Check if a corner is empty (no building present).
        /// </summary>
        private bool IsCornerEmpty(int cornerId)
        {
            return !_cornerData.ContainsKey(cornerId) || 
                   _cornerData[cornerId].BuildingType == BuildingType.None;
        }

        /// <summary>
        /// Get corners adjacent to the given corner (1 edge away).
        /// </summary>
        private List<int> GetAdjacentCorners(int cornerId)
        {
            var adjacentCorners = new List<int>();
            
            // Find all edges connected to this corner
            var connectedEdges = _edgeData.Values
                .Where(edge => edge.Corner1Id == cornerId || edge.Corner2Id == cornerId)
                .ToList();

            foreach (var edge in connectedEdges)
            {
                var adjacentCornerId = edge.Corner1Id == cornerId ? edge.Corner2Id : edge.Corner1Id;
                adjacentCorners.Add(adjacentCornerId);
            }

            return adjacentCorners;
        }

        /// <summary>
        /// Get corners that are two edges away from the given corner.
        /// </summary>
        private List<int> GetCornersTwoEdgesAway(int cornerId)
        {
            var cornersTwoEdgesAway = new List<int>();
            var adjacentCorners = GetAdjacentCorners(cornerId);

            foreach (var adjacentCornerId in adjacentCorners)
            {
                var secondLevelCorners = GetAdjacentCorners(adjacentCornerId);
                foreach (var secondLevelCornerId in secondLevelCorners)
                {
                    if (secondLevelCornerId != cornerId && !cornersTwoEdgesAway.Contains(secondLevelCornerId))
                    {
                        cornersTwoEdgesAway.Add(secondLevelCornerId);
                    }
                }
            }

            return cornersTwoEdgesAway;
        }

        #endregion

        #region Road Placement Validation (E4-F3-S2)

        /// <summary>
        /// Validate road placement according to rules: must extend from own settlement or road.
        /// </summary>
        public PlacementValidationResult CanPlaceRoad(int edgeId, int playerId, bool isInitialPlacement = false)
        {
            var result = new PlacementValidationResult { IsValid = true };

            // Check if edge exists
            if (!_edgeData.ContainsKey(edgeId))
            {
                result.IsValid = false;
                result.Reason = $"Edge {edgeId} does not exist";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Error,
                    "Road placement failed: {0}", result.Reason);
                return result;
            }

            var edge = _edgeData[edgeId];

            // Check if edge is empty
            if (edge.HasRoad)
            {
                result.IsValid = false;
                result.Reason = $"Edge {edgeId} already has a road";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                    "Road placement failed: {0}", result.Reason);
                return result;
            }

            // During initial placement, check if adjacent to settlement
            if (isInitialPlacement)
            {
                var adjacentSettlement = GetAdjacentSettlement(edgeId, playerId);
                if (adjacentSettlement == null)
                {
                    result.IsValid = false;
                    result.Reason = $"Initial road must be placed adjacent to player's settlement";
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                        "Road placement failed: {0}", result.Reason);
                    return result;
                }
            }
            else
            {
                // Check if connected to player's existing road network or settlement
                if (!IsConnectedToPlayerRoadNetwork(edgeId, playerId))
                {
                    result.IsValid = false;
                    result.Reason = $"Edge {edgeId} is not connected to player {playerId}'s road network or settlement";
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                        "Road placement failed: {0}", result.Reason);
                    return result;
                }
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Road placement validated for player {0} at edge {1}", playerId, edgeId);

            return result;
        }

        /// <summary>
        /// Check if road placement is connected to player's road network.
        /// </summary>
        private bool IsConnectedToPlayerRoadNetwork(int edgeId, int playerId)
        {
            var edge = _edgeData[edgeId];

            // Check if either corner has player's settlement
            if (HasPlayerSettlement(edge.Corner1Id, playerId) || HasPlayerSettlement(edge.Corner2Id, playerId))
            {
                return true;
            }

            // Check if either corner is connected to player's road network
            if (IsCornerConnectedToRoadNetwork(edge.Corner1Id, playerId) ||
                IsCornerConnectedToRoadNetwork(edge.Corner2Id, playerId))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a corner has a settlement belonging to the player.
        /// </summary>
        private bool HasPlayerSettlement(int cornerId, int playerId)
        {
            return _cornerData.ContainsKey(cornerId) &&
                   _cornerData[cornerId].PlayerId == playerId &&
                   (_cornerData[cornerId].BuildingType == BuildingType.Settlement ||
                    _cornerData[cornerId].BuildingType == BuildingType.City);
        }

        /// <summary>
        /// Check if a corner is connected to player's road network.
        /// </summary>
        private bool IsCornerConnectedToRoadNetwork(int cornerId, int playerId)
        {
            return _roadNetwork.ContainsKey(cornerId) &&
                   _roadNetwork[cornerId].Any(road => road.PlayerId == playerId);
        }

        /// <summary>
        /// Get adjacent settlement for initial placement.
        /// </summary>
        private BuildingData GetAdjacentSettlement(int edgeId, int playerId)
        {
            var edge = _edgeData[edgeId];

            if (HasPlayerSettlement(edge.Corner1Id, playerId))
            {
                return new BuildingData
                {
                    CornerId = edge.Corner1Id,
                    PlayerId = playerId,
                    BuildingType = _cornerData[edge.Corner1Id].BuildingType
                };
            }

            if (HasPlayerSettlement(edge.Corner2Id, playerId))
            {
                return new BuildingData
                {
                    CornerId = edge.Corner2Id,
                    PlayerId = playerId,
                    BuildingType = _cornerData[edge.Corner2Id].BuildingType
                };
            }

            return null;
        }

        #endregion

        #region City Upgrade Validation (E4-F3-S3)

        /// <summary>
        /// Validate city upgrade: replace settlement with city on same corner.
        /// </summary>
        public PlacementValidationResult CanUpgradeToCity(int cornerId, int playerId)
        {
            var result = new PlacementValidationResult { IsValid = true };

            // Check if corner exists
            if (!_cornerData.ContainsKey(cornerId))
            {
                result.IsValid = false;
                result.Reason = $"Corner {cornerId} does not exist";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Error,
                    "City upgrade failed: {0}", result.Reason);
                return result;
            }

            var corner = _cornerData[cornerId];

            // Check if corner has player's settlement
            if (corner.BuildingType != BuildingType.Settlement || corner.PlayerId != playerId)
            {
                result.IsValid = false;
                result.Reason = $"Corner {cornerId} does not have player {playerId}'s settlement (has {corner.BuildingType})";
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                    "City upgrade failed: {0}", result.Reason);
                return result;
            }

            // Check resource cost (this would be validated by ResourceManager)
            // For now, just validate the placement rules

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "City upgrade validated for player {0} at corner {1}", playerId, cornerId);

            return result;
        }

        #endregion

        #region Network Connectivity

        /// <summary>
        /// Check if a corner is connected to player's road network.
        /// </summary>
        private bool IsConnectedToPlayerNetwork(int cornerId, int playerId)
        {
            // Check if corner has player's building
            if (HasPlayerSettlement(cornerId, playerId))
            {
                return true;
            }

            // Check if corner is connected to player's road network
            return IsCornerConnectedToRoadNetwork(cornerId, playerId);
        }

        #endregion

        #region Data Management

        /// <summary>
        /// Register a corner for tracking.
        /// </summary>
        public void RegisterCorner(int cornerId, CornerData cornerData)
        {
            _cornerData[cornerId] = cornerData;
        }

        /// <summary>
        /// Register an edge for tracking.
        /// </summary>
        public void RegisterEdge(int edgeId, EdgeData edgeData)
        {
            _edgeData[edgeId] = edgeData;
        }

        /// <summary>
        /// Update corner with new building.
        /// </summary>
        public void PlaceBuilding(int cornerId, BuildingType buildingType, int playerId)
        {
            if (_cornerData.ContainsKey(cornerId))
            {
                _cornerData[cornerId].BuildingType = buildingType;
                _cornerData[cornerId].PlayerId = playerId;
                
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                    "Placed {0} for player {1} at corner {2}", buildingType, playerId, cornerId);
            }
        }

        /// <summary>
        /// Update edge with new road.
        /// </summary>
        public void PlaceRoad(int edgeId, int playerId)
        {
            if (_edgeData.ContainsKey(edgeId))
            {
                _edgeData[edgeId].HasRoad = true;
                _edgeData[edgeId].PlayerId = playerId;

                // Update road network
                UpdateRoadNetwork(edgeId, playerId);
                
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                    "Placed road for player {0} at edge {1}", playerId, edgeId);
            }
        }

        /// <summary>
        /// Update road network connectivity.
        /// </summary>
        private void UpdateRoadNetwork(int edgeId, int playerId)
        {
            var edge = _edgeData[edgeId];

            // Add road to both corners' network
            if (!_roadNetwork.ContainsKey(edge.Corner1Id))
            {
                _roadNetwork[edge.Corner1Id] = new List<RoadConnection>();
            }
            _roadNetwork[edge.Corner1Id].Add(new RoadConnection { EdgeId = edgeId, PlayerId = playerId });

            if (!_roadNetwork.ContainsKey(edge.Corner2Id))
            {
                _roadNetwork[edge.Corner2Id] = new List<RoadConnection>();
            }
            _roadNetwork[edge.Corner2Id].Add(new RoadConnection { EdgeId = edgeId, PlayerId = playerId });
        }

        /// <summary>
        /// Get all player buildings for analysis.
        /// </summary>
        public List<BuildingData> GetPlayerBuildings(int playerId)
        {
            return _cornerData.Values
                .Where(corner => corner.PlayerId == playerId && corner.BuildingType != BuildingType.None)
                .Select(corner => new BuildingData
                {
                    CornerId = corner.CornerId,
                    PlayerId = corner.PlayerId,
                    BuildingType = corner.BuildingType
                })
                .ToList();
        }

        /// <summary>
        /// Get all player roads for analysis.
        /// </summary>
        public List<RoadData> GetPlayerRoads(int playerId)
        {
            return _edgeData.Values
                .Where(edge => edge.HasRoad && edge.PlayerId == playerId)
                .Select(edge => new RoadData
                {
                    EdgeId = edge.EdgeId,
                    PlayerId = edge.PlayerId,
                    Corner1Id = edge.Corner1Id,
                    Corner2Id = edge.Corner2Id
                })
                .ToList();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Result of placement validation.
    /// </summary>
    public class PlacementValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
    }

    /// <summary>
    /// Data for a corner position.
    /// </summary>
    public class CornerData
    {
        public int CornerId { get; set; }
        public BuildingType BuildingType { get; set; } = BuildingType.None;
        public int PlayerId { get; set; }
        public List<int> AdjacentEdgeIds { get; set; } = new List<int>();
        public List<int> AdjacentCornerIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Data for an edge position.
    /// </summary>
    public class EdgeData
    {
        public int EdgeId { get; set; }
        public bool HasRoad { get; set; } = false;
        public int PlayerId { get; set; }
        public int Corner1Id { get; set; }
        public int Corner2Id { get; set; }
        public List<int> AdjacentHexIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// Road connection for network tracking.
    /// </summary>
    public class RoadConnection
    {
        public int EdgeId { get; set; }
        public int PlayerId { get; set; }
    }

    /// <summary>
    /// Building data for validation results.
    /// </summary>
    public class BuildingData
    {
        public int CornerId { get; set; }
        public int PlayerId { get; set; }
        public BuildingType BuildingType { get; set; }
    }

    /// <summary>
    /// Road data for analysis.
    /// </summary>
    public class RoadData
    {
        public int EdgeId { get; set; }
        public int PlayerId { get; set; }
        public int Corner1Id { get; set; }
        public int Corner2Id { get; set; }
    }

    #endregion
}
