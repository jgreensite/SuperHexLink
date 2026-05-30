using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.CoreLogic;
using SuperHexLink.Logging;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Manages resource tracking and transactions for all players.
    /// Based on NotebookLM research for economic feedback control and liquid/frozen commodities.
    /// </summary>
    public class ResourceManager
    {
        private readonly Dictionary<int, PlayerResources> _playerResources;
        private readonly ActionLogSettings _logSettings;
        private readonly EconomicBalancer _economicBalancer;

        // Resource types based on Catan-style economy
        public enum ResourceType
        {
            Wood,       // Construction material
            Brick,      // Construction material  
            Sheep,      // Trade/Liquid commodity
            Wheat,      // Food/Construction
            Ore,        // Advanced building
            Gold        // Special currency
        }

        public ResourceManager(ActionLogSettings logSettings, EconomicBalancer economicBalancer = null)
        {
            _playerResources = new Dictionary<int, PlayerResources>();
            _logSettings = logSettings;
            _economicBalancer = economicBalancer;
        }

        /// <summary>
        /// Initialize resources for a new player.
        /// </summary>
        public void InitializePlayer(int playerId)
        {
            if (_playerResources.ContainsKey(playerId))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Player {0} already has resources initialized", playerId);
                return;
            }

            _playerResources[playerId] = new PlayerResources
            {
                PlayerId = playerId,
                Resources = new Dictionary<ResourceType, int>
                {
                    { ResourceType.Wood, 0 },
                    { ResourceType.Brick, 0 },
                    { ResourceType.Sheep, 0 },
                    { ResourceType.Wheat, 0 },
                    { ResourceType.Ore, 0 },
                    { ResourceType.Gold, 0 }
                }
            };

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Initialized resources for player {0}", playerId);
        }

        /// <summary>
        /// Add resources to a player's inventory.
        /// </summary>
        public bool AddResources(int playerId, ResourceType resourceType, int amount)
        {
            if (!_playerResources.ContainsKey(playerId))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Error,
                    "Cannot add resources to non-existent player {0}", playerId);
                return false;
            }

            if (amount <= 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Cannot add non-positive amount {0} of resources", amount);
                return false;
            }

            var playerResources = _playerResources[playerId];
            playerResources.Resources[resourceType] += amount;

            // Apply economic feedback control if available
            _economicBalancer?.ApplyHoardingPenalty(playerId, playerResources);

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Added {0} {1} to player {2} (new total: {3})", 
                amount, resourceType, playerId, playerResources.Resources[resourceType]);

            return true;
        }

        /// <summary>
        /// Remove resources from a player's inventory.
        /// </summary>
        public bool RemoveResources(int playerId, ResourceType resourceType, int amount)
        {
            if (!_playerResources.ContainsKey(playerId))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Error,
                    "Cannot remove resources from non-existent player {0}", playerId);
                return false;
            }

            if (amount <= 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Cannot remove non-positive amount {0} of resources", amount);
                return false;
            }

            var playerResources = _playerResources[playerId];
            
            if (playerResources.Resources[resourceType] < amount)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Player {0} insufficient {1} (has {2}, needs {3})", 
                    playerId, resourceType, playerResources.Resources[resourceType], amount);
                return false;
            }

            playerResources.Resources[resourceType] -= amount;

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Removed {0} {1} from player {2} (new total: {3})", 
                amount, resourceType, playerId, playerResources.Resources[resourceType]);

            return true;
        }

        /// <summary>
        /// Check if player has sufficient resources.
        /// </summary>
        public bool HasSufficientResources(int playerId, ResourceType resourceType, int amount)
        {
            if (!_playerResources.ContainsKey(playerId))
                return false;

            return _playerResources[playerId].Resources[resourceType] >= amount;
        }

        /// <summary>
        /// Get player's resource count for a specific type.
        /// </summary>
        public int GetResourceCount(int playerId, ResourceType resourceType)
        {
            if (!_playerResources.ContainsKey(playerId))
                return 0;

            return _playerResources[playerId].Resources[resourceType];
        }

        /// <summary>
        /// Get all resources for a player.
        /// </summary>
        public Dictionary<ResourceType, int> GetPlayerResources(int playerId)
        {
            if (!_playerResources.ContainsKey(playerId))
                return new Dictionary<ResourceType, int>();

            return new Dictionary<ResourceType, int>(_playerResources[playerId].Resources);
        }

        /// <summary>
        /// Execute a resource transaction between players.
        /// </summary>
        public bool ExecuteTransaction(Transaction transaction)
        {
            // Validate transaction
            if (!ValidateTransaction(transaction))
                return false;

            // Execute from player's side
            if (transaction.FromPlayerId.HasValue)
            {
                if (!RemoveResources(transaction.FromPlayerId.Value, transaction.ResourceType, transaction.Amount))
                    return false;
            }

            // Execute to player's side
            if (!AddResources(transaction.ToPlayerId, transaction.ResourceType, transaction.Amount))
            {
                // Rollback if to player failed
                if (transaction.FromPlayerId.HasValue)
                {
                    AddResources(transaction.FromPlayerId.Value, transaction.ResourceType, transaction.Amount);
                }
                return false;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Transaction completed: {0}", transaction);

            return true;
        }

        /// <summary>
        /// Validate if a transaction can be executed.
        /// </summary>
        private bool ValidateTransaction(Transaction transaction)
        {
            if (transaction == null)
                return false;

            if (transaction.Amount <= 0)
                return false;

            if (!_playerResources.ContainsKey(transaction.ToPlayerId))
                return false;

            // Check from player has sufficient resources
            if (transaction.FromPlayerId.HasValue)
            {
                if (!_playerResources.ContainsKey(transaction.FromPlayerId.Value))
                    return false;

                if (!HasSufficientResources(transaction.FromPlayerId.Value, transaction.ResourceType, transaction.Amount))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get total resource count for all players (for economic analysis).
        /// </summary>
        public Dictionary<ResourceType, int> GetTotalResources()
        {
            var totals = new Dictionary<ResourceType, int>
            {
                { ResourceType.Wood, 0 },
                { ResourceType.Brick, 0 },
                { ResourceType.Sheep, 0 },
                { ResourceType.Wheat, 0 },
                { ResourceType.Ore, 0 },
                { ResourceType.Gold, 0 }
            };

            foreach (var playerResource in _playerResources.Values)
            {
                foreach (var resource in playerResource.Resources)
                {
                    totals[resource.Key] += resource.Value;
                }
            }

            return totals;
        }

        /// <summary>
        /// Get resource count for economic analysis (liquid vs frozen commodities).
        /// </summary>
        public EconomicMetrics GetEconomicMetrics(int playerId)
        {
            if (!_playerResources.ContainsKey(playerId))
                return new EconomicMetrics();

            var resources = _playerResources[playerId].Resources;
            
            // Liquid commodities (easily traded)
            var liquidTotal = resources[ResourceType.Sheep] + resources[ResourceType.Gold];
            
            // Frozen commodities (construction materials)
            var frozenTotal = resources[ResourceType.Wood] + resources[ResourceType.Brick] + 
                            resources[ResourceType.Wheat] + resources[ResourceType.Ore];

            return new EconomicMetrics
            {
                PlayerId = playerId,
                LiquidCommodities = liquidTotal,
                FrozenCommodities = frozenTotal,
                TotalResources = liquidTotal + frozenTotal,
                ResourceDistribution = new Dictionary<ResourceType, int>(resources)
            };
        }
    }

    /// <summary>
    /// Represents a player's resource inventory.
    /// </summary>
    public class PlayerResources
    {
        public int PlayerId { get; set; }
        public Dictionary<ResourceManager.ResourceType, int> Resources { get; set; }
    }

    /// <summary>
    /// Represents a resource transaction between players or from the system.
    /// </summary>
    public class Transaction
    {
        public int? FromPlayerId { get; set; }  // null = system/bank
        public int ToPlayerId { get; set; }
        public ResourceManager.ResourceType ResourceType { get; set; }
        public int Amount { get; set; }
        public string Reason { get; set; }

        public override string ToString()
        {
            var from = FromPlayerId?.ToString() ?? "System";
            return $"{Amount} {ResourceType} from {FromPlayerId} to {ToPlayerId} ({Reason})";
        }
    }

    /// <summary>
    /// Economic metrics for analysis (based on NotebookLM liquid/frozen commodity research).
    /// </summary>
    public class EconomicMetrics
    {
        public int PlayerId { get; set; }
        public int LiquidCommodities { get; set; }  // Easily traded resources
        public int FrozenCommodities { get; set; }  // Construction materials
        public int TotalResources { get; set; }
        public Dictionary<ResourceManager.ResourceType, int> ResourceDistribution { get; set; }

        /// <summary>
        /// Get economic balance ratio (helps identify runaway leaders).
        /// </summary>
        public float GetBalanceRatio()
        {
            if (TotalResources == 0) return 0f;
            return (float)LiquidCommodities / TotalResources;
        }

        /// <summary>
        /// Check if player might be a runaway leader (based on resource dominance).
        /// </summary>
        public bool IsPotentialRunawayLeader(int totalPlayers)
        {
            // Simple heuristic: if player has >50% more resources than average
            var averageResources = TotalResources / totalPlayers;
            return TotalResources > averageResources * 1.5f;
        }
    }
}
