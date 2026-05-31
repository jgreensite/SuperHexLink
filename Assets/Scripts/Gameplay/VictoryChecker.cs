using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Logging;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Tracks victory points and detects win conditions.
    /// Based on Catan-style victory conditions with 10 VP threshold.
    /// </summary>
    public class VictoryChecker
    {
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<int, VictoryPoints> _playerVictoryPoints;
        private readonly Dictionary<int, SpecialAchievement> _specialAchievements;

        // Victory point thresholds
        private const int VICTORY_THRESHOLD = 10;
        private const int LONGEST_ROAD_THRESHOLD = 5;
        private const int LARGEST_ARMY_THRESHOLD = 3;

        public VictoryChecker(ActionLogSettings logSettings)
        {
            _logSettings = logSettings;
            _playerVictoryPoints = new Dictionary<int, VictoryPoints>();
            _specialAchievements = new Dictionary<int, SpecialAchievement>();
        }

        #region Victory Point Tracking (E4-F4-S1)

        /// <summary>
        /// Calculate victory points for a player based on buildings and achievements.
        /// </summary>
        public VictoryPoints CalculateVictoryPoints(int playerId, List<BuildingData> buildings, List<RoadData> roads, int armySize)
        {
            var victoryPoints = new VictoryPoints { PlayerId = playerId };

            // Points from settlements (1 VP each)
            victoryPoints.SettlementPoints = buildings.Count(b => b.BuildingType == BuildingType.Settlement);

            // Points from cities (2 VP each)
            victoryPoints.CityPoints = buildings.Count(b => b.BuildingType == BuildingType.City) * 2;

            // Points from longest road (2 VP)
            victoryPoints.LongestRoadPoints = HasLongestRoad(playerId, roads) ? 2 : 0;

            // Points from largest army (2 VP)
            victoryPoints.LargestArmyPoints = HasLargestArmy(playerId, armySize) ? 2 : 0;

            // Calculate total
            victoryPoints.TotalPoints = victoryPoints.SettlementPoints + 
                                       victoryPoints.CityPoints + 
                                       (victoryPoints.LongestRoadPoints ?? 0) + 
                                       (victoryPoints.LargestArmyPoints ?? 0);

            // Store for tracking
            _playerVictoryPoints[playerId] = victoryPoints;

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Victory points for player {0}: {1} total (Settlements: {2}, Cities: {3}, Longest Road: {4}, Largest Army: {5})",
                playerId, victoryPoints.TotalPoints, victoryPoints.SettlementPoints, 
                victoryPoints.CityPoints, victoryPoints.LongestRoadPoints, victoryPoints.LargestArmyPoints);

            return victoryPoints;
        }

        /// <summary>
        /// Check if player has the longest road.
        /// </summary>
        private bool HasLongestRoad(int playerId, List<RoadData> roads)
        {
            var playerRoads = roads.Where(r => r.PlayerId == playerId).ToList();
            var longestRoadLength = CalculateLongestRoadLength(playerRoads);

            // Check if other players have longer roads
            foreach (var otherPlayerId in _playerVictoryPoints.Keys.Where(id => id != playerId))
            {
                var otherPlayerRoads = roads.Where(r => r.PlayerId == otherPlayerId).ToList();
                var otherLongestRoad = CalculateLongestRoadLength(otherPlayerRoads);
                
                if (otherLongestRoad >= longestRoadLength)
                {
                    return false;  // Another player has equal or longer road
                }
            }

            return longestRoadLength >= LONGEST_ROAD_THRESHOLD;
        }

        /// <summary>
        /// Calculate the longest continuous road length for a player.
        /// </summary>
        private int CalculateLongestRoadLength(List<RoadData> roads)
        {
            if (!roads.Any()) return 0;

            var maxLength = 0;
            var visitedRoads = new HashSet<int>();
            var roadNetwork = BuildRoadNetwork(roads);

            foreach (var road in roads)
            {
                if (!visitedRoads.Contains(road.EdgeId))
                {
                    var length = ExploreRoadNetwork(road.EdgeId, roadNetwork, visitedRoads);
                    maxLength = Math.Max(maxLength, length);
                }
            }

            return maxLength;
        }

        /// <summary>
        /// Build road network adjacency list.
        /// </summary>
        private Dictionary<int, List<int>> BuildRoadNetwork(List<RoadData> roads)
        {
            var network = new Dictionary<int, List<int>>();

            foreach (var road in roads)
            {
                if (!network.ContainsKey(road.EdgeId))
                {
                    network[road.EdgeId] = new List<int>();
                }

                // Find connected roads (sharing corners)
                var connectedRoads = roads.Where(r => r.EdgeId != road.EdgeId &&
                    (r.Corner1Id == road.Corner1Id || r.Corner1Id == road.Corner2Id ||
                     r.Corner2Id == road.Corner1Id || r.Corner2Id == road.Corner2Id)).ToList();

                foreach (var connectedRoad in connectedRoads)
                {
                    network[road.EdgeId].Add(connectedRoad.EdgeId);
                }
            }

            return network;
        }

        /// <summary>
        /// Explore road network to find longest path.
        /// </summary>
        private int ExploreRoadNetwork(int startEdgeId, Dictionary<int, List<int>> network, HashSet<int> visited)
        {
            var stack = new Stack<(int EdgeId, int Length)>();
            stack.Push((startEdgeId, 1));
            var maxLength = 0;

            while (stack.Count > 0)
            {
                var (currentEdgeId, length) = stack.Pop();
                
                if (visited.Contains(currentEdgeId))
                {
                    continue;
                }

                visited.Add(currentEdgeId);
                maxLength = Math.Max(maxLength, length);

                if (network.ContainsKey(currentEdgeId))
                {
                    foreach (var connectedEdgeId in network[currentEdgeId])
                    {
                        if (!visited.Contains(connectedEdgeId))
                        {
                            stack.Push((connectedEdgeId, length + 1));
                        }
                    }
                }
            }

            return maxLength;
        }

        /// <summary>
        /// Check if player has the largest army.
        /// </summary>
        private bool HasLargestArmy(int playerId, int armySize)
        {
            if (armySize < LARGEST_ARMY_THRESHOLD) return false;

            // Check if other players have larger armies
            foreach (var otherPlayerId in _playerVictoryPoints.Keys.Where(id => id != playerId))
            {
                var otherArmySize = GetPlayerArmySize(otherPlayerId);
                if (otherArmySize >= armySize)
                {
                    return false;  // Another player has equal or larger army
                }
            }

            return true;
        }

        /// <summary>
        /// Get player's army size (would be tracked elsewhere).
        /// </summary>
        private int GetPlayerArmySize(int playerId)
        {
            // This would be tracked by a military/combat system
            // For now, return 0 as placeholder
            return 0;
        }

        /// <summary>
        /// Update special achievements (longest road, largest army).
        /// </summary>
        public void UpdateSpecialAchievements(int playerId, SpecialAchievementType type, bool hasAchievement)
        {
            if (!_specialAchievements.ContainsKey(playerId))
            {
                _specialAchievements[playerId] = new SpecialAchievement { PlayerId = playerId };
            }

            var achievement = _specialAchievements[playerId];

            switch (type)
            {
                case SpecialAchievementType.LongestRoad:
                    achievement.HasLongestRoad = hasAchievement;
                    break;
                case SpecialAchievementType.LargestArmy:
                    achievement.HasLargestArmy = hasAchievement;
                    break;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Updated special achievement for player {0}: {1} = {2}", 
                playerId, type, hasAchievement);
        }

        #endregion

        #region Win Detection (E4-F4-S2)

        /// <summary>
        /// Check if any player has reached the victory condition.
        /// </summary>
        public WinDetectionResult CheckForWinner(Dictionary<int, List<BuildingData>> allBuildings, 
                                               Dictionary<int, List<RoadData>> allRoads, 
                                               Dictionary<int, int> armySizes)
        {
            var winners = new List<int>();

            foreach (var playerId in allBuildings.Keys)
            {
                var buildings = allBuildings[playerId];
                var roads = allRoads.GetValueOrDefault(playerId, new List<RoadData>());
                var armySize = armySizes.GetValueOrDefault(playerId, 0);

                var victoryPoints = CalculateVictoryPoints(playerId, buildings, roads, armySize);

                if (victoryPoints.TotalPoints >= VICTORY_THRESHOLD)
                {
                    winners.Add(playerId);
                    
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                        "Player {0} has reached victory with {1} points!", playerId, victoryPoints.TotalPoints);
                }
            }

            var result = new WinDetectionResult
            {
                HasWinner = winners.Count > 0,
                Winners = winners,
                VictoryThreshold = VICTORY_THRESHOLD
            };

            if (result.HasWinner)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                    "Game victory detected! Winners: {0}", string.Join(", ", result.Winners));
            }

            return result;
        }

        /// <summary>
        /// Get detailed victory status for all players.
        /// </summary>
        public Dictionary<int, VictoryStatus> GetVictoryStatus(Dictionary<int, List<BuildingData>> allBuildings,
                                                            Dictionary<int, List<RoadData>> allRoads,
                                                            Dictionary<int, int> armySizes)
        {
            var status = new Dictionary<int, VictoryStatus>();

            foreach (var playerId in allBuildings.Keys)
            {
                var buildings = allBuildings[playerId];
                var roads = allRoads.GetValueOrDefault(playerId, new List<RoadData>());
                var armySize = armySizes.GetValueOrDefault(playerId, 0);

                var victoryPoints = CalculateVictoryPoints(playerId, buildings, roads, armySize);

                status[playerId] = new VictoryStatus
                {
                    PlayerId = playerId,
                    VictoryPoints = victoryPoints,
                    IsWinner = victoryPoints.TotalPoints >= VICTORY_THRESHOLD,
                    PointsToVictory = Math.Max(0, VICTORY_THRESHOLD - victoryPoints.TotalPoints),
                    VictoryBreakdown = GetVictoryBreakdown(victoryPoints)
                };
            }

            return status;
        }

        /// <summary>
        /// Get breakdown of victory sources.
        /// </summary>
        private string GetVictoryBreakdown(VictoryPoints victoryPoints)
        {
            var breakdown = new List<string>();

            if (victoryPoints.SettlementPoints > 0)
                breakdown.Add($"{victoryPoints.SettlementPoints} from settlements");
            
            if (victoryPoints.CityPoints > 0)
                breakdown.Add($"{victoryPoints.CityPoints} from cities");
            
            if (victoryPoints.LongestRoadPoints > 0)
                breakdown.Add($"{victoryPoints.LongestRoadPoints} from longest road");
            
            if (victoryPoints.LargestArmyPoints > 0)
                breakdown.Add($"{victoryPoints.LargestArmyPoints} from largest army");

            return string.Join(", ", breakdown);
        }

        #endregion

        #region Victory Analysis

        /// <summary>
        /// Get victory progress analysis for strategic planning.
        /// </summary>
        public VictoryAnalysis GetVictoryAnalysis(int playerId, Dictionary<int, List<BuildingData>> allBuildings,
                                               Dictionary<int, List<RoadData>> allRoads,
                                               Dictionary<int, int> armySizes)
        {
            var buildings = allBuildings.GetValueOrDefault(playerId, new List<BuildingData>());
            var roads = allRoads.GetValueOrDefault(playerId, new List<RoadData>());
            var armySize = armySizes.GetValueOrDefault(playerId, 0);

            var victoryPoints = CalculateVictoryPoints(playerId, buildings, roads, armySize);
            var analysis = new VictoryAnalysis
            {
                PlayerId = playerId,
                CurrentPoints = victoryPoints.TotalPoints,
                PointsNeeded = VICTORY_THRESHOLD - victoryPoints.TotalPoints,
                VictoryPaths = GetVictoryPaths(victoryPoints, buildings, roads, armySize)
            };

            return analysis;
        }

        /// <summary>
        /// Get possible paths to victory.
        /// </summary>
        private List<VictoryPath> GetVictoryPaths(VictoryPoints currentPoints, List<BuildingData> buildings, 
                                               List<RoadData> roads, int armySize)
        {
            var paths = new List<VictoryPath>();
            var pointsNeeded = VICTORY_THRESHOLD - currentPoints.TotalPoints;

            if (pointsNeeded <= 0) return paths;

            // Path through settlements
            var settlementPath = new VictoryPath
            {
                Type = VictoryPathType.SettlementExpansion,
                Description = "Build more settlements",
                PointsPossible = pointsNeeded,
                BuildingsNeeded = Math.Min(pointsNeeded, 5),  // Max 5 settlements
                Feasibility = CalculateSettlementFeasibility(buildings.Count)
            };
            paths.Add(settlementPath);

            // Path through city upgrades
            var settlementsToUpgrade = buildings.Count(b => b.BuildingType == BuildingType.Settlement);
            if (settlementsToUpgrade > 0)
            {
                var cityPath = new VictoryPath
                {
                    Type = VictoryPathType.CityUpgrades,
                    Description = "Upgrade settlements to cities",
                    PointsPossible = settlementsToUpgrade,  // 1 point per upgrade
                    BuildingsNeeded = Math.Min(settlementsToUpgrade, pointsNeeded),
                    Feasibility = CalculateCityFeasibility(settlementsToUpgrade)
                };
                paths.Add(cityPath);
            }

            // Path through longest road
            if (!currentPoints.LongestRoadPoints.HasValue || currentPoints.LongestRoadPoints == 0)
            {
                var roadPath = new VictoryPath
                {
                    Type = VictoryPathType.LongestRoad,
                    Description = "Achieve longest road",
                    PointsPossible = 2,
                    RoadsNeeded = LONGEST_ROAD_THRESHOLD,
                    Feasibility = CalculateRoadFeasibility(roads.Count)
                };
                paths.Add(roadPath);
            }

            // Path through largest army
            if (!currentPoints.LargestArmyPoints.HasValue || currentPoints.LargestArmyPoints == 0)
            {
                var armyPath = new VictoryPath
                {
                    Type = VictoryPathType.LargestArmy,
                    Description = "Achieve largest army",
                    PointsPossible = 2,
                    ArmySizeNeeded = LARGEST_ARMY_THRESHOLD,
                    Feasibility = CalculateArmyFeasibility(armySize)
                };
                paths.Add(armyPath);
            }

            return paths.OrderByDescending(p => p.Feasibility).ToList();
        }

        private float CalculateSettlementFeasibility(int currentSettlements)
        {
            if (currentSettlements >= 5) return 0f;  // Max settlements
            return 1f - (currentSettlements * 0.15f);  // Decreasing feasibility
        }

        private float CalculateCityFeasibility(int settlementsToUpgrade)
        {
            return settlementsToUpgrade > 0 ? 0.8f : 0f;
        }

        private float CalculateRoadFeasibility(int currentRoads)
        {
            return currentRoads >= LONGEST_ROAD_THRESHOLD ? 1f : 0.6f;
        }

        private float CalculateArmyFeasibility(int currentArmy)
        {
            return currentArmy >= LARGEST_ARMY_THRESHOLD ? 1f : 0.4f;
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Get current victory points for a player.
        /// </summary>
        public VictoryPoints GetPlayerVictoryPoints(int playerId)
        {
            return _playerVictoryPoints.GetValueOrDefault(playerId, new VictoryPoints { PlayerId = playerId });
        }

        /// <summary>
        /// Get all players' victory points for leaderboard.
        /// </summary>
        public List<VictoryPoints> GetLeaderboard()
        {
            return _playerVictoryPoints.Values.OrderByDescending(vp => vp.TotalPoints).ToList();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Victory points breakdown for a player.
    /// </summary>
    public class VictoryPoints
    {
        public int PlayerId { get; set; }
        public int SettlementPoints { get; set; }
        public int CityPoints { get; set; }
        public int? LongestRoadPoints { get; set; }
        public int? LargestArmyPoints { get; set; }
        public int TotalPoints { get; set; }
    }

    /// <summary>
    /// Special achievement tracking.
    /// </summary>
    public class SpecialAchievement
    {
        public int PlayerId { get; set; }
        public bool HasLongestRoad { get; set; }
        public bool HasLargestArmy { get; set; }
    }

    /// <summary>
    /// Result of win detection.
    /// </summary>
    public class WinDetectionResult
    {
        public bool HasWinner { get; set; }
        public List<int> Winners { get; set; } = new List<int>();
        public int VictoryThreshold { get; set; }
    }

    /// <summary>
    /// Victory status for a player.
    /// </summary>
    public class VictoryStatus
    {
        public int PlayerId { get; set; }
        public VictoryPoints VictoryPoints { get; set; }
        public bool IsWinner { get; set; }
        public int PointsToVictory { get; set; }
        public string VictoryBreakdown { get; set; }
    }

    /// <summary>
    /// Victory analysis for strategic planning.
    /// </summary>
    public class VictoryAnalysis
    {
        public int PlayerId { get; set; }
        public int CurrentPoints { get; set; }
        public int PointsNeeded { get; set; }
        public List<VictoryPath> VictoryPaths { get; set; } = new List<VictoryPath>();
    }

    /// <summary>
    /// Possible path to victory.
    /// </summary>
    public class VictoryPath
    {
        public VictoryPathType Type { get; set; }
        public string Description { get; set; }
        public int PointsPossible { get; set; }
        public int BuildingsNeeded { get; set; }
        public int RoadsNeeded { get; set; }
        public int ArmySizeNeeded { get; set; }
        public float Feasibility { get; set; }  // 0-1 scale
    }

    public enum SpecialAchievementType
    {
        LongestRoad,
        LargestArmy
    }

    public enum VictoryPathType
    {
        SettlementExpansion,
        CityUpgrades,
        LongestRoad,
        LargestArmy
    }

    #endregion
}
