using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Logging;
using SuperHexLink.Rules;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Resolves which hexes produce resources on dice rolls.
    /// Based on NotebookLM research for dice-based production with variance management.
    /// </summary>
    public class ProductionEngine
    {
        private readonly ActionLogSettings _logSettings;
        private readonly EconomicBalancer _economicBalancer;
        private readonly Dictionary<int, HexProductionData> _hexProductionMap;

        public ProductionEngine(ActionLogSettings logSettings, EconomicBalancer economicBalancer = null)
        {
            _logSettings = logSettings;
            _economicBalancer = economicBalancer;
            _hexProductionMap = new Dictionary<int, HexProductionData>();
        }

        /// <summary>
        /// Register a hex for production tracking.
        /// </summary>
        public void RegisterHex(int hexId, HexProductionData productionData)
        {
            _hexProductionMap[hexId] = productionData;
            
            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Registered hex {0} for production: {1} on roll {2}", 
                hexId, productionData.ResourceType, productionData.ProductionNumber);
        }

        /// <summary>
        /// Calculate resource production for a dice roll.
        /// </summary>
        public List<ProductionResult> CalculateProduction(int diceRoll, Dictionary<int, List<ProductionBuildingData>> buildingData)
        {
            var results = new List<ProductionResult>();
            
            // Find all hexes that produce on this roll
            var producingHexes = _hexProductionMap
                .Where(kvp => kvp.Value.ProductionNumber == diceRoll)
                .ToList();

            if (!producingHexes.Any())
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                    "No hexes produce on roll {0}", diceRoll);
                return results;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Roll {0}: {1} hexes producing", diceRoll, producingHexes.Count);

            foreach (var hexKvp in producingHexes)
            {
                var hexId = hexKvp.Key;
                var productionData = hexKvp.Value;

                if (!buildingData.ContainsKey(hexId))
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                        "No building data for hex {0}", hexId);
                    continue;
                }

                var buildings = buildingData[hexId];
                foreach (var building in buildings)
                {
                    var production = CalculateHexProduction(productionData, building);
                    if (production.Amount > 0)
                    {
                        results.Add(production);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Calculate production from a specific hex-building combination.
        /// </summary>
        private ProductionResult CalculateHexProduction(HexProductionData hexData, ProductionBuildingData building)
        {
            var baseAmount = building.BuildingType switch
            {
                BuildingType.Settlement => 1,
                BuildingType.City => 2,
                _ => 0
            };

            // Apply economic balancing (exhaustion, etc.)
            var adjustedAmount = _economicBalancer?.CalculateExhaustedProduction(baseAmount, hexData.ExhaustionLevel) ?? baseAmount;

            return new ProductionResult
            {
                PlayerId = building.PlayerId,
                ResourceType = hexData.ResourceType,
                Amount = adjustedAmount,
                HexId = building.HexId,
                BuildingType = building.BuildingType,
                ProductionNumber = hexData.ProductionNumber
            };
        }

        /// <summary>
        /// Get production statistics for analysis.
        /// </summary>
        public ProductionStatistics GetProductionStatistics()
        {
            var stats = new ProductionStatistics();
            
            foreach (var hexData in _hexProductionMap.Values)
            {
                var resourceType = hexData.ResourceType;
                if (!stats.ResourceDistribution.ContainsKey(resourceType))
                {
                    stats.ResourceDistribution[resourceType] = 0;
                }
                stats.ResourceDistribution[resourceType]++;
                
                stats.TotalHexes++;
            }

            // Calculate production probability for each dice roll
            for (int roll = 2; roll <= 12; roll++)
            {
                var hexCount = _hexProductionMap.Values.Count(h => h.ProductionNumber == roll);
                stats.ProductionProbability[roll] = (float)hexCount / stats.TotalHexes;
            }

            return stats;
        }

        /// <summary>
        /// Apply hex exhaustion (resource depletion over time).
        /// </summary>
        public void ApplyHexExhaustion(int hexId, int exhaustionAmount = 1)
        {
            if (!_hexProductionMap.ContainsKey(hexId))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Warning,
                    "Cannot apply exhaustion to non-existent hex {0}", hexId);
                return;
            }

            var hexData = _hexProductionMap[hexId];
            hexData.ExhaustionLevel += exhaustionAmount;

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Applied exhaustion to hex {0}: new level {1}", hexId, hexData.ExhaustionLevel);
        }

        /// <summary>
        /// Reset hex exhaustion (for testing or special events).
        /// </summary>
        public void ResetHexExhaustion(int hexId)
        {
            if (_hexProductionMap.ContainsKey(hexId))
            {
                _hexProductionMap[hexId].ExhaustionLevel = 0;
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                    "Reset exhaustion for hex {0}", hexId);
            }
        }

        /// <analysis>
        /// Get production variance analysis for dice roll distribution.
        /// This helps identify if the resource distribution is balanced.
        /// </analysis>
        public VarianceAnalysis GetVarianceAnalysis()
        {
            var analysis = new VarianceAnalysis();
            var diceProbabilities = GetDiceProbabilities();

            foreach (var hexData in _hexProductionMap.Values)
            {
                var roll = hexData.ProductionNumber;
                var expectedProbability = diceProbabilities[roll];
                
                analysis.RollDistribution[roll] = analysis.RollDistribution.GetValueOrDefault(roll, 0) + 1;
            }

            // Calculate variance from expected dice distribution
            var totalHexes = _hexProductionMap.Count;
            foreach (var kvp in diceProbabilities)
            {
                var expectedCount = kvp.Value * totalHexes;
                var actualCount = analysis.RollDistribution.GetValueOrDefault(kvp.Key, 0);
                var variance = Math.Abs(actualCount - expectedCount);
                
                analysis.VarianceByRoll[kvp.Key] = variance;
            }

            analysis.TotalVariance = analysis.VarianceByRoll.Values.Sum();
            analysis.IsBalanced = analysis.TotalVariance < totalHexes * 0.3f;  // 30% tolerance

            return analysis;
        }

        /// <summary>
        /// Get standard dice roll probabilities (2-12).
        /// </summary>
        private Dictionary<int, float> GetDiceProbabilities()
        {
            return new Dictionary<int, float>
            {
                {2, 1/36f}, {3, 2/36f}, {4, 3/36f}, {5, 4/36f}, {6, 5/36f}, {7, 6/36f},
                {8, 5/36f}, {9, 4/36f}, {10, 3/36f}, {11, 2/36f}, {12, 1/36f}
            };
        }

        /// <summary>
        /// Get production recommendations for board setup.
        /// </summary>
        public List<ProductionRecommendation> GetProductionRecommendations()
        {
            var recommendations = new List<ProductionRecommendation>();
            var variance = GetVarianceAnalysis();
            var stats = GetProductionStatistics();

            // Check for resource balance
            var resourceCounts = stats.ResourceDistribution;
            var maxResource = resourceCounts.Values.Max();
            var minResource = resourceCounts.Values.Min();
            
            if (maxResource > minResource * 2)
            {
                var overrepresentedResource = resourceCounts.First(kvp => kvp.Value == maxResource).Key;
                var underrepresentedResource = resourceCounts.First(kvp => kvp.Value == minResource).Key;
                
                recommendations.Add(new ProductionRecommendation
                {
                    Type = RecommendationType.BalanceResources,
                    Priority = Priority.High,
                    Description = $"Balance resource distribution: {overrepresentedResource} is overrepresented, {underrepresentedResource} is underrepresented",
                    ResourceFocus = underrepresentedResource
                });
            }

            // Check for dice roll balance
            if (!variance.IsBalanced)
            {
                recommendations.Add(new ProductionRecommendation
                {
                    Type = RecommendationType.BalanceDiceRolls,
                    Priority = Priority.Medium,
                    Description = "Balance dice roll distribution to reduce variance",
                    Details = variance.VarianceByRoll.Where(kvp => kvp.Value > 2).ToList()
                });
            }

            // Check for critical numbers (6, 8 are most valuable)
            var criticalHexes = _hexProductionMap.Values.Count(h => h.ProductionNumber == 6 || h.ProductionNumber == 8);
            if (criticalHexes < stats.TotalHexes * 0.2f)  // Less than 20% on critical numbers
            {
                recommendations.Add(new ProductionRecommendation
                {
                    Type = RecommendationType.OptimizeCriticalNumbers,
                    Priority = Priority.Medium,
                    Description = "Consider placing more resources on numbers 6 and 8 for better production balance"
                });
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Data for hex production capabilities.
    /// </summary>
    public class HexProductionData
    {
        public int HexId { get; set; }
        public ResourceManager.ResourceType ResourceType { get; set; }
        public int ProductionNumber { get; set; }  // Dice roll that triggers production
        public int ExhaustionLevel { get; set; } = 0;  // Current exhaustion/depletion level
    }

    /// <summary>
    /// Data for buildings on hexes, used by production calculations.
    /// </summary>
    public class ProductionBuildingData
    {
        public int BuildingId { get; set; }
        public int PlayerId { get; set; }
        public int HexId { get; set; }
        public BuildingType BuildingType { get; set; }
        public int CornerPosition { get; set; }  // Which corner of the hex
    }

    /// <summary>
    /// Result of production calculation.
    /// </summary>
    public class ProductionResult
    {
        public int PlayerId { get; set; }
        public ResourceManager.ResourceType ResourceType { get; set; }
        public int Amount { get; set; }
        public int HexId { get; set; }
        public BuildingType BuildingType { get; set; }
        public int ProductionNumber { get; set; }

        public override string ToString()
        {
            return $"{Amount} {ResourceType} to Player {PlayerId} from {BuildingType} on hex {HexId} (roll {ProductionNumber})";
        }
    }

    /// <summary>
    /// Statistics about resource production.
    /// </summary>
    public class ProductionStatistics
    {
        public int TotalHexes { get; set; }
        public Dictionary<ResourceManager.ResourceType, int> ResourceDistribution { get; set; } = new();
        public Dictionary<int, float> ProductionProbability { get; set; } = new();  // Roll -> probability
    }

    /// <summary>
    /// Analysis of production variance.
    /// </summary>
    public class VarianceAnalysis
    {
        public Dictionary<int, int> RollDistribution { get; set; } = new();
        public Dictionary<int, double> VarianceByRoll { get; set; } = new();
        public double TotalVariance { get; set; }
        public bool IsBalanced { get; set; }
    }

    /// <summary>
    /// Recommendation for production improvements.
    /// </summary>
    public class ProductionRecommendation
    {
        public RecommendationType Type { get; set; }
        public Priority Priority { get; set; }
        public string Description { get; set; }
        public ResourceManager.ResourceType? ResourceFocus { get; set; }
        public List<KeyValuePair<int, double>> Details { get; set; } = new();
    }

    public enum BuildingType
    {
        None = 0,
        Settlement,
        City,
        Road
    }

    public enum RecommendationType
    {
        BalanceResources,
        BalanceDiceRolls,
        OptimizeCriticalNumbers,
        ReduceVariance,
        IncreaseDiversity
    }
}
