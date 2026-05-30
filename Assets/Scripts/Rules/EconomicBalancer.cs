using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.CoreLogic;
using SuperHexLink.Logging;
using SuperHexLink.Gameplay;

namespace SuperHexLink.Rules
{
    /// <summary>
    /// Implements economic feedback control to prevent runaway leader problems.
    /// Based on NotebookLM research: liquid/frozen commodities, hoarding penalties, maintenance costs.
    /// </summary>
    public class EconomicBalancer
    {
        private readonly ActionLogSettings _logSettings;
        private readonly EconomicBalanceConfig _config;

        // Economic feedback thresholds
        private const int HOARDING_THRESHOLD = 7;  // 7+ card penalty from Catan research
        private const int MAINTENANCE_PER_BUILDING = 1;  // Resource cost per building
        private const int EXHAUSTION_THRESHOLD = 10;  // Hex production reduction point

        public EconomicBalancer(ActionLogSettings logSettings, EconomicBalanceConfig config = null)
        {
            _logSettings = logSettings;
            _config = config ?? new EconomicBalanceConfig();
        }

        /// <summary>
        /// Apply hoarding penalties to prevent resource stockpiling.
        /// </summary>
        public void ApplyHoardingPenalty(int playerId, PlayerResources playerResources)
        {
            var totalResources = playerResources.Resources.Values.Sum();
            
            if (totalResources >= HOARDING_THRESHOLD)
            {
                var penaltyAmount = totalResources / 2;  // Lose half (Catan rule)
                var resourcesLost = new Dictionary<ResourceManager.ResourceType, int>();

                // Apply penalty starting from most abundant resources
                var sortedResources = playerResources.Resources
                    .Where(kvp => kvp.Value > 0)
                    .OrderByDescending(kvp => kvp.Value)
                    .ToList();

                var remainingPenalty = penaltyAmount;
                foreach (var resource in sortedResources)
                {
                    if (remainingPenalty <= 0) break;

                    var toRemove = Math.Min(resource.Value, remainingPenalty);
                    resource.Value -= toRemove;
                    remainingPenalty -= toRemove;
                    resourcesLost[resource.Key] = toRemove;
                }

                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Applied hoarding penalty to player {0}: lost {1} total resources", 
                    playerId, penaltyAmount);

                foreach (var lost in resourcesLost)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                        "  - Lost {0} {1}", lost.Value, lost.Key);
                }
            }
        }

        /// <summary>
        /// Apply maintenance costs for buildings (negative feedback loop).
        /// </summary>
        public bool ApplyMaintenanceCosts(int playerId, ResourceManager resourceManager, int buildingCount)
        {
            var totalMaintenance = buildingCount * MAINTENANCE_PER_BUILDING;
            
            // Try to deduct from wheat first (food resource), then other resources
            var maintenancePaid = 0;
            var resourcesUsed = new Dictionary<ResourceManager.ResourceType, int>();

            // Priority order for maintenance payment
            var paymentPriority = new[]
            {
                ResourceManager.ResourceType.Wheat,
                ResourceManager.ResourceType.Sheep,
                ResourceManager.ResourceType.Wood,
                ResourceManager.ResourceType.Brick,
                ResourceManager.ResourceType.Ore,
                ResourceManager.ResourceType.Gold
            };

            foreach (var resourceType in paymentPriority)
            {
                if (maintenancePaid >= totalMaintenance) break;

                var available = resourceManager.GetResourceCount(playerId, resourceType);
                var needed = totalMaintenance - maintenancePaid;
                var toUse = Math.Min(available, needed);

                if (toUse > 0 && resourceManager.RemoveResources(playerId, resourceType, toUse))
                {
                    maintenancePaid += toUse;
                    resourcesUsed[resourceType] = toUse;
                }
            }

            if (maintenancePaid < totalMaintenance)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Player {0} could not pay full maintenance: paid {1}/{2}", 
                    playerId, maintenancePaid, totalMaintenance);
                return false;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Player {0} paid maintenance: {2} resources for {1} buildings", 
                playerId, buildingCount, maintenancePaid);

            return true;
        }

        /// <summary>
        /// Calculate resource production with exhaustion (diminishing returns).
        /// </summary>
        public int CalculateExhaustedProduction(int baseProduction, int hexExhaustionLevel)
        {
            if (hexExhaustionLevel <= 0) return baseProduction;
            
            // Diminishing returns: each exhaustion level reduces production by 10%
            var reductionPercentage = hexExhaustionLevel * 0.1f;
            var reductionAmount = (int)(baseProduction * reductionPercentage);
            var exhaustedProduction = Math.Max(1, baseProduction - reductionAmount);  // Minimum 1 resource

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Production exhausted: {0} -> {1} (exhaustion level: {2})", 
                baseProduction, exhaustedProduction, hexExhaustionLevel);

            return exhaustedProduction;
        }

        /// <summary>
        /// Apply targeted debilitation (robber-style mechanic) to leader.
        /// </summary>
        public void ApplyTargetedDebilitation(int leaderPlayerId, ResourceManager resourceManager, int targetPlayerId)
        {
            // This implements the "robber" mechanic from Catan research
            // where players can target the leader to prevent runaway victory
            
            var leaderResources = resourceManager.GetPlayerResources(leaderPlayerId);
            var targetResources = resourceManager.GetPlayerResources(targetPlayerId);

            // Find leader's most abundant resource
            var leaderMostAbundant = leaderResources
                .Where(kvp => kvp.Value > 0)
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();

            if (leaderMostAbundant.Key != null)
            {
                var stealAmount = Math.Min(1, leaderMostAbundant.Value);  // Steal 1 resource
                
                if (resourceManager.RemoveResources(leaderPlayerId, leaderMostAbundant.Key, stealAmount))
                {
                    resourceManager.AddResources(targetPlayerId, leaderMostAbundant.Key, stealAmount);
                    
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                        "Targeted debilitation: Player {0} stole {1} {2} from leader {3}", 
                        targetPlayerId, stealAmount, leaderMostAbundant.Key, leaderPlayerId);
                }
            }
        }

        /// <summary>
        /// Check if player should be targeted for economic balancing.
        /// </summary>
        public bool ShouldBalancePlayer(int playerId, EconomicMetrics metrics, int totalPlayers)
        {
            return metrics.IsPotentialRunawayLeader(totalPlayers) || 
                   metrics.GetBalanceRatio() > 0.8f;  // Too many liquid commodities
        }

        /// <summary>
        /// Apply spatial saturation limits (encirclement mechanics).
        /// </summary>
        public bool CanExpandTerritory(int playerId, int currentTerritoryCount, int maxTerritory)
        {
            // Implement spatial saturation from research: limit expansion to prevent snowballing
            var saturationRatio = (float)currentTerritoryCount / maxTerritory;
            
            if (saturationRatio > 0.7f)  // 70% saturation threshold
            {
                // Apply increasing difficulty for expansion
                var expansionDifficulty = (int)(saturationRatio * 10);  // Higher cost when more saturated
                
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                    "Player {0} territory expansion difficulty: {1} (saturation: {2:P})", 
                    playerId, expansionDifficulty, saturationRatio);
                
                return false;  // For now, block expansion when highly saturated
            }

            return true;
        }

        /// <summary>
        /// Get economic balance recommendations for AI players.
        /// </summary>
        public List<EconomicRecommendation> GetEconomicRecommendations(int playerId, EconomicMetrics metrics)
        {
            var recommendations = new List<EconomicRecommendation>();

            // Recommend converting liquid to frozen commodities
            if (metrics.GetBalanceRatio() > 0.6f)
            {
                recommendations.Add(new EconomicRecommendation
                {
                    Type = RecommendationType.ConvertLiquidToFrozen,
                    Priority = Priority.High,
                    Description = "Convert liquid commodities to frozen commodities (buildings)",
                    Reason = "Too many liquid commodities, need infrastructure"
                });
            }

            // Recommend diversification if too concentrated in one resource
            var maxResource = metrics.ResourceDistribution.Values.Max();
            var totalResources = metrics.ResourceDistribution.Values.Sum();
            
            if (maxResource > totalResources * 0.5f)
            {
                recommendations.Add(new EconomicRecommendation
                {
                    Type = RecommendationType.DiversifyResources,
                    Priority = Priority.Medium,
                    Description = "Diversify resource portfolio through trading",
                    Reason = "Over-concentrated in single resource type"
                });
            }

            // Recommend maintenance planning if many buildings
            if (metrics.FrozenCommodities > 15)
            {
                recommendations.Add(new EconomicRecommendation
                {
                    Type = RecommendationType.PlanMaintenance,
                    Priority = Priority.Medium,
                    Description = "Ensure sufficient wheat/food for maintenance costs",
                    Reason = "High building count requires maintenance planning"
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Apply economic feedback based on game phase.
        /// </summary>
        public void ApplyPhaseBasedBalancing(GamePhase phase, Dictionary<int, EconomicMetrics> allMetrics)
        {
            switch (phase)
            {
                case GamePhase.EarlyGame:
                    // Encourage expansion in early game
                    ApplyEarlyGameBalancing(allMetrics);
                    break;
                    
                case GamePhase.MidGame:
                    // Apply standard balancing in mid game
                    ApplyMidGameBalancing(allMetrics);
                    break;
                    
                case GamePhase.LateGame:
                    // Prevent runaway leaders in late game
                    ApplyLateGameBalancing(allMetrics);
                    break;
            }
        }

        private void ApplyEarlyGameBalancing(Dictionary<int, EconomicMetrics> allMetrics)
        {
            // Early game: encourage balanced growth
            foreach (var metrics in allMetrics.Values)
            {
                if (metrics.TotalResources < 10)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                        "Early game boost for player {0}: low resources, encouraging growth", metrics.PlayerId);
                    // Could implement small resource bonuses for struggling players
                }
            }
        }

        private void ApplyMidGameBalancing(Dictionary<int, EconomicMetrics> allMetrics)
        {
            // Mid game: standard balancing
            var totalPlayers = allMetrics.Count;
            foreach (var metrics in allMetrics.Values)
            {
                if (ShouldBalancePlayer(metrics.PlayerId, metrics, totalPlayers))
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                        "Mid game balancing triggered for player {0}: potential runaway leader", metrics.PlayerId);
                }
            }
        }

        private void ApplyLateGameBalancing(Dictionary<int, EconomicMetrics> allMetrics)
        {
            // Late game: aggressive balancing to prevent runaway victory
            var leaderMetrics = allMetrics.Values.OrderByDescending(m => m.TotalResources).FirstOrDefault();
            if (leaderMetrics != null)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Late game: player {0} is dominant, applying aggressive balancing", leaderMetrics.PlayerId);
                // Could implement stronger penalties or mechanics to help trailing players
            }
        }
    }

    /// <summary>
    /// Configuration for economic balancing parameters.
    /// </summary>
    public class EconomicBalanceConfig
    {
        public int HoardingThreshold { get; set; } = HOARDING_THRESHOLD;
        public int MaintenancePerBuilding { get; set; } = MAINTENANCE_PER_BUILDING;
        public float ExhaustionRate { get; set; } = 0.1f;  // 10% reduction per exhaustion level
        public float SaturationThreshold { get; set; } = 0.7f;  // 70% territory saturation
    }

    /// <summary>
    /// Economic recommendation for AI players.
    /// </summary>
    public class EconomicRecommendation
    {
        public RecommendationType Type { get; set; }
        public Priority Priority { get; set; }
        public string Description { get; set; }
        public string Reason { get; set; }
    }

    public enum RecommendationType
    {
        ConvertLiquidToFrozen,
        DiversifyResources,
        PlanMaintenance,
        TargetLeader,
        ExpandTerritory
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum GamePhase
    {
        EarlyGame,
        MidGame,
        LateGame
    }
}
