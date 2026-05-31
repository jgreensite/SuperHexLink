using System;
using System.Collections.Generic;

namespace SuperHexLink.Simulation
{
    /// <summary>
    /// Interface for all game actions in the Monte Carlo simulation framework.
    /// Based on NotebookLM research: Command pattern for tree search algorithms and undo functionality.
    /// </summary>
    public interface IGameAction
    {
        /// <summary>
        /// Unique identifier for this action type.
        /// </summary>
        string ActionId { get; }

        /// <summary>
        /// Player performing this action.
        /// </summary>
        int PlayerId { get; }

        /// <summary>
        /// Type of action for categorization.
        /// </summary>
        ActionType Type { get; }

        /// <summary>
        /// Cost in resources (if any).
        /// </summary>
        Dictionary<ResourceType, int> ResourceCost { get; }

        /// <summary>
        /// Priority for AI decision making.
        /// </summary>
        float Priority { get; }

        /// <summary>
        /// Apply the action to modify game state.
        /// </summary>
        /// <param name="state">Current game state to modify</param>
        void Apply(ref HeadlessGameState state);

        /// <summary>
        /// Check if this action is valid for the given state.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>True if action can be executed</returns>
        bool IsValid(HeadlessGameState state);

        /// <summary>
        /// Get a human-readable description of the action.
        /// </summary>
        /// <returns>Description string</returns>
        string GetDescription();

        /// <summary>
        /// Create a deep copy of this action.
        /// </summary>
        /// <returns>Cloned action</returns>
        IGameAction Clone();

        /// <summary>
        /// Estimate the value of this action for AI evaluation.
        /// </summary>
        /// <param name="state">Current game state</param>
        /// <returns>Estimated value (higher is better)</returns>
        float EstimateValue(HeadlessGameState state);
    }

    #region Concrete Action Implementations

    /// <summary>
    /// Roll dice action for production phase.
    /// </summary>
    public class RollDiceAction : IGameAction
    {
        public string ActionId => "roll_dice";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.Roll;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>();
        public float Priority => 1.0f;

        public void Apply(ref HeadlessGameState state)
        {
            var random = new Random();
            var dice1 = random.Next(1, 7);
            var dice2 = random.Next(1, 7);
            var roll = dice1 + dice2;

            state.CurrentTurn.DiceRoll = roll;
            state.CurrentTurn.Phase = TurnPhase.Build;

            // Calculate production based on roll
            CalculateProduction(ref state, roll);
        }

        private void CalculateProduction(ref HeadlessGameState state, int roll)
        {
            var productionResults = new List<ProductionResult>();

            foreach (var playerKvp in state.Players)
            {
                var playerId = playerKvp.Key;
                var playerState = playerKvp.Value;

                foreach (var building in playerState.Buildings.Values)
                {
                    var adjacentHexes = state.HexGrid.GetAdjacentHexes(building.LocationId);
                    foreach (var hexId in adjacentHexes)
                    {
                        if (state.HexGrid.Hexes.ContainsKey(hexId))
                        {
                            var hex = state.HexGrid.Hexes[hexId];
                            if (hex.ProductionNumber == roll)
                            {
                                var amount = building.Type == BuildingType.City ? 2 : 1;
                                productionResults.Add(new ProductionResult
                                {
                                    PlayerId = playerId,
                                    ResourceType = hex.ResourceType,
                                    Amount = amount,
                                    SourceHexId = hexId
                                });
                            }
                        }
                    }
                }
            }

            // Apply production results
            foreach (var result in productionResults)
            {
                if (state.Players.ContainsKey(result.PlayerId))
                {
                    var playerResources = state.Players[result.PlayerId].Resources;
                    if (playerResources.ContainsKey(result.ResourceType))
                    {
                        playerResources[result.ResourceType] += result.Amount;
                        state.EconomicState.TotalResourcesProduced += result.Amount;
                    }
                }
            }
        }

        public bool IsValid(HeadlessGameState state)
        {
            return state.CurrentTurn.Phase == TurnPhase.Roll && 
                   state.CurrentTurn.CurrentPlayerId == PlayerId;
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} rolls dice";
        }

        public IGameAction Clone()
        {
            return new RollDiceAction { PlayerId = PlayerId };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // Dice roll has moderate value - enables production
            return 5.0f;
        }
    }

    /// <summary>
    /// Place settlement action.
    /// </summary>
    public class PlaceSettlementAction : IGameAction
    {
        public PlaceSettlementAction() { }
        public PlaceSettlementAction(int cornerId, int playerId) { CornerId = cornerId; PlayerId = playerId; }

        public string ActionId => "place_settlement";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.Build;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>
        {
            { ResourceType.Wood, 1 },
            { ResourceType.Brick, 1 },
            { ResourceType.Sheep, 1 },
            { ResourceType.Wheat, 1 }
        };
        public float Priority => 8.0f;

        public int CornerId { get; set; }

        public void Apply(ref HeadlessGameState state)
        {
            if (!state.Players.ContainsKey(PlayerId)) return;

            var playerState = state.Players[PlayerId];
            
            // Deduct resources
            foreach (var cost in ResourceCost)
            {
                if (playerState.Resources.ContainsKey(cost.Key))
                {
                    playerState.Resources[cost.Key] -= cost.Value;
                }
            }

            // Place settlement
            var buildingId = playerState.Buildings.Count + 1;
            playerState.Buildings[buildingId] = new BuildingState
            {
                BuildingId = buildingId,
                Type = BuildingType.Settlement,
                LocationId = CornerId,
                ConstructionTurn = state.CurrentTurn.TurnNumber
            };

            // Update corner ownership
            if (state.HexGrid.Corners.ContainsKey(CornerId))
            {
                var corner = state.HexGrid.Corners[CornerId];
                corner.OwnerId = PlayerId;
                corner.BuildingType = BuildingType.Settlement;
            }

            // Update victory points
            playerState.VictoryPoints += 1;
            state.EconomicState.TotalBuildingsConstructed += 1;
        }

        public bool IsValid(HeadlessGameState state)
        {
            if (state.CurrentTurn.Phase != TurnPhase.Build || 
                state.CurrentTurn.CurrentPlayerId != PlayerId)
                return false;

            if (!state.Players.ContainsKey(PlayerId))
                return false;

            var playerState = state.Players[PlayerId];
            
            // Check resource cost
            foreach (var cost in ResourceCost)
            {
                if (!playerState.Resources.ContainsKey(cost.Key) || 
                    playerState.Resources[cost.Key] < cost.Value)
                    return false;
            }

            // Check placement validity (simplified)
            if (!state.HexGrid.Corners.ContainsKey(CornerId))
                return false;

            var corner = state.HexGrid.Corners[CornerId];
            return corner.OwnerId == -1;  // Unoccupied
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} places settlement at corner {CornerId}";
        }

        public IGameAction Clone()
        {
            return new PlaceSettlementAction 
            { 
                PlayerId = PlayerId, 
                CornerId = CornerId 
            };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // Settlements have high value - provide VP and production
            return 15.0f;
        }
    }

    /// <summary>
    /// Place road action.
    /// </summary>
    public class PlaceRoadAction : IGameAction
    {
        public PlaceRoadAction() { }
        public PlaceRoadAction(int edgeId, int playerId) { EdgeId = edgeId; PlayerId = playerId; }

        public string ActionId => "place_road";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.Build;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>
        {
            { ResourceType.Wood, 1 },
            { ResourceType.Brick, 1 }
        };
        public float Priority => 6.0f;

        public int EdgeId { get; set; }

        public void Apply(ref HeadlessGameState state)
        {
            if (!state.Players.ContainsKey(PlayerId)) return;

            var playerState = state.Players[PlayerId];
            
            // Deduct resources
            foreach (var cost in ResourceCost)
            {
                if (playerState.Resources.ContainsKey(cost.Key))
                {
                    playerState.Resources[cost.Key] -= cost.Value;
                }
            }

            // Place road
            var roadId = playerState.Roads.Count + 1;
            playerState.Roads[roadId] = new RoadState
            {
                RoadId = roadId,
                LocationId = EdgeId,
                ConstructionTurn = state.CurrentTurn.TurnNumber
            };

            // Update edge ownership
            if (state.HexGrid.Edges.ContainsKey(EdgeId))
            {
                var edge = state.HexGrid.Edges[EdgeId];
                edge.OwnerId = PlayerId;
            }

            state.EconomicState.TotalBuildingsConstructed += 1;
        }

        public bool IsValid(HeadlessGameState state)
        {
            if (state.CurrentTurn.Phase != TurnPhase.Build || 
                state.CurrentTurn.CurrentPlayerId != PlayerId)
                return false;

            if (!state.Players.ContainsKey(PlayerId))
                return false;

            var playerState = state.Players[PlayerId];
            
            // Check resource cost
            foreach (var cost in ResourceCost)
            {
                if (!playerState.Resources.ContainsKey(cost.Key) || 
                    playerState.Resources[cost.Key] < cost.Value)
                    return false;
            }

            // Check placement validity (simplified)
            if (!state.HexGrid.Edges.ContainsKey(EdgeId))
                return false;

            var edge = state.HexGrid.Edges[EdgeId];
            return edge.OwnerId == -1;  // Unoccupied
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} places road at edge {EdgeId}";
        }

        public IGameAction Clone()
        {
            return new PlaceRoadAction 
            { 
                PlayerId = PlayerId, 
                EdgeId = EdgeId 
            };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // Roads have moderate value - enable expansion and network
            return 8.0f;
        }
    }

    /// <summary>
    /// Upgrade settlement to city action.
    /// </summary>
    public class UpgradeToCityAction : IGameAction
    {
        public UpgradeToCityAction() { }
        public UpgradeToCityAction(int cornerId, int playerId) { CornerId = cornerId; PlayerId = playerId; }

        public string ActionId => "upgrade_to_city";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.Build;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>
        {
            { ResourceType.Wheat, 2 },
            { ResourceType.Ore, 3 }
        };
        public float Priority => 10.0f;

        public int CornerId { get; set; }

        public void Apply(ref HeadlessGameState state)
        {
            if (!state.Players.ContainsKey(PlayerId)) return;

            var playerState = state.Players[PlayerId];
            
            // Deduct resources
            foreach (var cost in ResourceCost)
            {
                if (playerState.Resources.ContainsKey(cost.Key))
                {
                    playerState.Resources[cost.Key] -= cost.Value;
                }
            }

            // Find and upgrade settlement
            var building = playerState.Buildings.Values
                .FirstOrDefault(b => b.LocationId == CornerId && b.Type == BuildingType.Settlement);
            
            if (building != null)
            {
                building.Type = BuildingType.City;
                
                // Update corner
                if (state.HexGrid.Corners.ContainsKey(CornerId))
                {
                    var corner = state.HexGrid.Corners[CornerId];
                    corner.BuildingType = BuildingType.City;
                }

                // Update victory points (city = 2 VP, settlement = 1 VP, so +1 VP)
                playerState.VictoryPoints += 1;
            }
        }

        public bool IsValid(HeadlessGameState state)
        {
            if (state.CurrentTurn.Phase != TurnPhase.Build || 
                state.CurrentTurn.CurrentPlayerId != PlayerId)
                return false;

            if (!state.Players.ContainsKey(PlayerId))
                return false;

            var playerState = state.Players[PlayerId];
            
            // Check resource cost
            foreach (var cost in ResourceCost)
            {
                if (!playerState.Resources.ContainsKey(cost.Key) || 
                    playerState.Resources[cost.Key] < cost.Value)
                    return false;
            }

            // Check if settlement exists at corner
            var building = playerState.Buildings.Values
                .FirstOrDefault(b => b.LocationId == CornerId && b.Type == BuildingType.Settlement);
            
            return building != null;
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} upgrades settlement to city at corner {CornerId}";
        }

        public IGameAction Clone()
        {
            return new UpgradeToCityAction 
            { 
                PlayerId = PlayerId, 
                CornerId = CornerId 
            };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // City upgrades have very high value - double production and extra VP
            return 20.0f;
        }
    }

    /// <summary>
    /// Bank trade action.
    /// </summary>
    public class BankTradeAction : IGameAction
    {
        public BankTradeAction() { }
        public BankTradeAction(int playerId, ResourceType fromResource, ResourceType toResource, int giveAmount, int getAmount)
        {
            PlayerId = playerId;
            FromResource = fromResource;
            ToResource = toResource;
            GiveAmount = giveAmount;
            GetAmount = getAmount;
        }

        public string ActionId => "bank_trade";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.Trade;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>
        {
            { FromResource, GiveAmount }
        };
        public float Priority => 4.0f;

        public ResourceType FromResource { get; set; }
        public ResourceType ToResource { get; set; }
        public int GiveAmount { get; set; }
        public int GetAmount { get; set; }

        public void Apply(ref HeadlessGameState state)
        {
            if (!state.Players.ContainsKey(PlayerId)) return;

            var playerState = state.Players[PlayerId];
            
            // Deduct resources
            if (playerState.Resources.ContainsKey(FromResource))
            {
                playerState.Resources[FromResource] -= GiveAmount;
            }

            // Add resources
            if (playerState.Resources.ContainsKey(ToResource))
            {
                playerState.Resources[ToResource] += GetAmount;
            }
        }

        public bool IsValid(HeadlessGameState state)
        {
            if (state.CurrentTurn.Phase != TurnPhase.Trade || 
                state.CurrentTurn.CurrentPlayerId != PlayerId)
                return false;

            if (!state.Players.ContainsKey(PlayerId))
                return false;

            var playerState = state.Players[PlayerId];
            
            return playerState.Resources.ContainsKey(FromResource) && 
                   playerState.Resources[FromResource] >= GiveAmount;
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} trades {GiveAmount} {FromResource} for {GetAmount} {ToResource}";
        }

        public IGameAction Clone()
        {
            return new BankTradeAction 
            { 
                PlayerId = PlayerId,
                FromResource = FromResource,
                ToResource = ToResource,
                GiveAmount = GiveAmount,
                GetAmount = GetAmount
            };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // Trade value depends on current needs (simplified)
            return 3.0f;
        }
    }

    /// <summary>
    /// End turn action.
    /// </summary>
    public class EndTurnAction : IGameAction
    {
        public string ActionId => "end_turn";
        public int PlayerId { get; set; }
        public ActionType Type => ActionType.System;
        public Dictionary<ResourceType, int> ResourceCost => new Dictionary<ResourceType, int>();
        public float Priority => 0.5f;

        public void Apply(ref HeadlessGameState state)
        {
            // Move to next player
            var nextPlayerId = (PlayerId + 1) % state.Players.Count;
            state.CurrentTurn.CurrentPlayerId = nextPlayerId;
            state.CurrentTurn.TurnNumber++;
            state.CurrentTurn.Phase = TurnPhase.Roll;
            state.CurrentTurn.DiceRoll = 0;

            // Update game phase based on turn number
            if (state.CurrentTurn.TurnNumber <= 4)
                state.GamePhase = GamePhase.EarlyGame;
            else if (state.CurrentTurn.TurnNumber <= 12)
                state.GamePhase = GamePhase.MidGame;
            else
                state.GamePhase = GamePhase.LateGame;

            state.EconomicState.EconomicCycles++;
        }

        public bool IsValid(HeadlessGameState state)
        {
            return state.CurrentTurn.CurrentPlayerId == PlayerId;
        }

        public string GetDescription()
        {
            return $"Player {PlayerId} ends turn";
        }

        public IGameAction Clone()
        {
            return new EndTurnAction { PlayerId = PlayerId };
        }

        public float EstimateValue(HeadlessGameState state)
        {
            // End turn has low priority - only do when no better actions
            return 1.0f;
        }
    }

    #endregion

    #region Supporting Data Structures

    /// <summary>
    /// Production result from dice roll.
    /// </summary>
    public struct ProductionResult
    {
        public int PlayerId { get; set; }
        public ResourceType ResourceType { get; set; }
        public int Amount { get; set; }
        public int SourceHexId { get; set; }
    }

    /// <summary>
    /// Types of game actions.
    /// </summary>
    public enum ActionType
    {
        Roll,      // Dice roll for production
        Build,     // Place buildings/roads
        Trade,     // Resource trading
        System     // System actions (end turn, etc.)
    }

    #endregion
}

