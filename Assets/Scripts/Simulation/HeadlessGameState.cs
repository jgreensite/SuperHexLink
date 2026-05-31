using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperHexLink.Simulation
{
    /// <summary>
    /// Unity-free game state representation for Monte Carlo simulation.
    /// Based on NotebookLM research: pure C# simulation for AI rule testing performance.
    /// </summary>
    public struct HeadlessGameState : IEquatable<HeadlessGameState>
    {
        #region Core Game State

        /// <summary>
        /// Hex grid data - pure C# representation without Unity GameObjects.
        /// </summary>
        public HexGridData HexGrid { get; set; }

        /// <summary>
        /// Player data including resources and buildings.
        /// </summary>
        public Dictionary<int, PlayerState> Players { get; set; }

        /// <summary>
        /// Current turn information.
        /// </summary>
        public TurnState CurrentTurn { get; set; }

        /// <summary>
        /// Game phase and status.
        /// </summary>
        public GamePhase GamePhase { get; set; }

        /// <summary>
        /// Victory conditions and win tracking.
        /// </summary>
        public VictoryState VictoryState { get; set; }

        /// <summary>
        /// Economic state for balancing analysis.
        /// </summary>
        public EconomicState EconomicState { get; set; }

        #endregion

        #region Constructors and Initialization

        /// <summary>
        /// Create initial game state for specified number of players.
        /// </summary>
        public static HeadlessGameState CreateInitialState(int playerCount, int gridWidth, int gridHeight)
        {
            var state = new HeadlessGameState
            {
                HexGrid = HexGridData.CreateRandomGrid(gridWidth, gridHeight),
                Players = CreateInitialPlayers(playerCount),
                CurrentTurn = new TurnState
                {
                    CurrentPlayerId = 0,
                    TurnNumber = 1,
                    Phase = TurnPhase.Roll,
                    DiceRoll = 0
                },
                GamePhase = GamePhase.EarlyGame,
                VictoryState = new VictoryState
                {
                    VictoryThreshold = 10,
                    Winners = new List<int>()
                },
                EconomicState = new EconomicState
                {
                    TotalResourcesProduced = 0,
                    TotalBuildingsConstructed = 0,
                    EconomicCycles = 0
                }
            };

            return state;
        }

        /// <summary>
        /// Create initial player states.
        /// </summary>
        private static Dictionary<int, PlayerState> CreateInitialPlayers(int playerCount)
        {
            var players = new Dictionary<int, PlayerState>();
            
            for (int i = 0; i < playerCount; i++)
            {
                players[i] = new PlayerState
                {
                    PlayerId = i,
                    Resources = CreateInitialResources(),
                    Buildings = new Dictionary<int, BuildingState>(),
                    Roads = new Dictionary<int, RoadState>(),
                    VictoryPoints = 0,
                    ArmySize = 0
                };
            }

            return players;
        }

        /// <summary>
        /// Create initial resource allocation.
        /// </summary>
        private static Dictionary<ResourceType, int> CreateInitialResources()
        {
            return new Dictionary<ResourceType, int>
            {
                { ResourceType.Wood, 0 },
                { ResourceType.Brick, 0 },
                { ResourceType.Sheep, 0 },
                { ResourceType.Wheat, 0 },
                { ResourceType.Ore, 0 },
                { ResourceType.Gold, 0 }
            };
        }

        #endregion

        #region Game State Operations

        /// <summary>
        /// Apply a game action to the state.
        /// </summary>
        public HeadlessGameState ApplyAction(IGameAction action)
        {
            var newState = this;  // Struct copy
            action.Apply(ref newState);
            return newState;
        }

        /// <summary>
        /// Get valid moves for the current player.
        /// </summary>
        public List<IGameAction> GetValidMoves()
        {
            var moves = new List<IGameAction>();
            var currentPlayerId = CurrentTurn.CurrentPlayerId;
            var playerState = Players[currentPlayerId];

            switch (CurrentTurn.Phase)
            {
                case TurnPhase.Roll:
                    moves.Add(new RollDiceAction());
                    break;

                case TurnPhase.Build:
                    moves.AddRange(GetValidBuildingMoves(currentPlayerId, playerState));
                    moves.AddRange(GetValidRoadMoves(currentPlayerId, playerState));
                    break;

                case TurnPhase.Trade:
                    moves.AddRange(GetValidTradeMoves(currentPlayerId, playerState));
                    break;

                case TurnPhase.End:
                    moves.Add(new EndTurnAction());
                    break;
            }

            return moves;
        }

        /// <summary>
        /// Get valid building moves for a player.
        /// </summary>
        private List<IGameAction> GetValidBuildingMoves(int playerId, PlayerState playerState)
        {
            var moves = new List<IGameAction>();

            // Check settlement placements
            foreach (var corner in HexGrid.Corners.Values.Where(c => c.OwnerId == -1))
            {
                if (CanPlaceSettlement(corner.CornerId, playerId))
                {
                    moves.Add(new PlaceSettlementAction(corner.CornerId, playerId));
                }
            }

            // Check city upgrades
            foreach (var settlement in playerState.Buildings.Values.Where(b => b.Type == BuildingType.Settlement))
            {
                if (CanUpgradeToCity(settlement.LocationId, playerId))
                {
                    moves.Add(new UpgradeToCityAction(settlement.LocationId, playerId));
                }
            }

            return moves;
        }

        /// <summary>
        /// Get valid road moves for a player.
        /// </summary>
        private List<IGameAction> GetValidRoadMoves(int playerId, PlayerState playerState)
        {
            var moves = new List<IGameAction>();

            foreach (var edge in HexGrid.Edges.Values.Where(e => e.OwnerId == -1))
            {
                if (CanPlaceRoad(edge.EdgeId, playerId))
                {
                    moves.Add(new PlaceRoadAction(edge.EdgeId, playerId));
                }
            }

            return moves;
        }

        /// <summary>
        /// Get valid trade moves for a player.
        /// </summary>
        private List<IGameAction> GetValidTradeMoves(int playerId, PlayerState playerState)
        {
            var moves = new List<IGameAction>();

            // Bank trades (4:1)
            foreach (var resource in playerState.Resources.Where(r => r.Value >= 4))
            {
                foreach (var targetResource in (ResourceType[])Enum.GetValues(typeof(ResourceType)))
                {
                    if (targetResource != resource.Key)
                    {
                        moves.Add(new BankTradeAction(playerId, resource.Key, targetResource, 4, 1));
                    }
                }
            }

            // Player trades (would need other players in multiplayer)
            // For now, just bank trades

            return moves;
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Check if settlement can be placed at corner.
        /// </summary>
        private bool CanPlaceSettlement(int cornerId, int playerId)
        {
            if (!HexGrid.Corners.ContainsKey(cornerId))
                return false;

            var corner = HexGrid.Corners[cornerId];
            if (corner.OwnerId != -1)
                return false;

            // Check distance rule (2+ edges from other settlements)
            var adjacentCorners = HexGrid.GetAdjacentCorners(cornerId);
            foreach (var adjacentCornerId in adjacentCorners)
            {
                if (HexGrid.Corners.ContainsKey(adjacentCornerId))
                {
                    var adjacentCorner = HexGrid.Corners[adjacentCornerId];
                    if (adjacentCorner.OwnerId != -1)
                        return false;
                }
            }

            // Check road connectivity (not for initial placement)
            if (CurrentTurn.TurnNumber > 2)
            {
                var connectedEdges = HexGrid.Edges.Values
                    .Where(e => (e.Corner1Id == cornerId || e.Corner2Id == cornerId) && e.OwnerId == playerId)
                    .ToList();

                if (!connectedEdges.Any())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check if city can be upgraded at corner.
        /// </summary>
        private bool CanUpgradeToCity(int cornerId, int playerId)
        {
            if (!HexGrid.Corners.ContainsKey(cornerId))
                return false;

            var corner = HexGrid.Corners[cornerId];
            if (corner.OwnerId != playerId)
                return false;

            var playerState = Players[playerId];
            var building = playerState.Buildings.GetValueOrDefault(cornerId);
            
            return building?.Type == BuildingType.Settlement;
        }

        /// <summary>
        /// Check if road can be placed at edge.
        /// </summary>
        private bool CanPlaceRoad(int edgeId, int playerId)
        {
            if (!HexGrid.Edges.ContainsKey(edgeId))
                return false;

            var edge = HexGrid.Edges[edgeId];
            if (edge.OwnerId != -1)
                return false;

            // Check connectivity to player's network
            var corner1Connected = HexGrid.Corners.ContainsKey(edge.Corner1Id) && 
                                 HexGrid.Corners[edge.Corner1Id].OwnerId == playerId;
            var corner2Connected = HexGrid.Corners.ContainsKey(edge.Corner2Id) && 
                                 HexGrid.Corners[edge.Corner2Id].OwnerId == playerId;

            // Check road connectivity
            var road1Connected = HexGrid.Edges.Values
                .Any(e => e.OwnerId == playerId && 
                         (e.Corner1Id == edge.Corner1Id || e.Corner2Id == edge.Corner1Id));
            var road2Connected = HexGrid.Edges.Values
                .Any(e => e.OwnerId == playerId && 
                         (e.Corner1Id == edge.Corner2Id || e.Corner2Id == edge.Corner2Id));

            return corner1Connected || corner2Connected || road1Connected || road2Connected;
        }

        #endregion

        #region Evaluation Methods

        /// <summary>
        /// Evaluate board state for AI decision making.
        /// </summary>
        public float EvaluateBoardState(int playerId)
        {
            var score = 0f;
            var playerState = Players[playerId];

            // Victory points (most important)
            score += playerState.VictoryPoints * 100f;

            // Resource value
            var resourceValue = CalculateResourceValue(playerState.Resources);
            score += resourceValue * 10f;

            // Building value
            var buildingValue = CalculateBuildingValue(playerState.Buildings);
            score += buildingValue * 20f;

            // Network value (roads and connectivity)
            var networkValue = CalculateNetworkValue(playerState);
            score += networkValue * 15f;

            // Production potential
            var productionValue = CalculateProductionPotential(playerId);
            score += productionValue * 25f;

            // Positional advantage
            var positionalValue = CalculatePositionalAdvantage(playerId);
            score += positionalValue * 5f;

            return score;
        }

        /// <summary>
        /// Calculate the value of player's resources.
        /// </summary>
        private float CalculateResourceValue(Dictionary<ResourceType, int> resources)
        {
            var values = new Dictionary<ResourceType, float>
            {
                { ResourceType.Wood, 1.0f },
                { ResourceType.Brick, 1.0f },
                { ResourceType.Sheep, 1.2f },  // More liquid, good for trading
                { ResourceType.Wheat, 1.3f },  // Food, maintenance
                { ResourceType.Ore, 1.5f },     // Advanced building
                { ResourceType.Gold, 2.0f }     // Special currency
            };

            return resources.Sum(r => r.Value * values.GetValueOrDefault(r.Key, 1.0f));
        }

        /// <summary>
        /// Calculate the value of player's buildings.
        /// </summary>
        private float CalculateBuildingValue(Dictionary<int, BuildingState> buildings)
        {
            return buildings.Sum(b => b.Value.Type switch
            {
                BuildingType.Settlement => 10f,
                BuildingType.City => 25f,
                _ => 0f
            });
        }

        /// <summary>
        /// Calculate the value of player's road network.
        /// </summary>
        private float CalculateNetworkValue(PlayerState playerState)
        {
            var roadCount = playerState.Roads.Count;
            var longestRoad = CalculateLongestRoad(playerState);
            
            return roadCount * 2f + longestRoad * 5f;
        }

        /// <summary>
        /// Calculate longest continuous road for a player.
        /// </summary>
        private int CalculateLongestRoad(PlayerState playerState)
        {
            // Simplified longest road calculation
            // In a full implementation, this would use graph traversal
            return playerState.Roads.Count;
        }

        /// <summary>
        /// Calculate production potential for a player.
        /// </summary>
        private float CalculateProductionPotential(int playerId)
        {
            var potential = 0f;
            var playerState = Players[playerId];

            foreach (var building in playerState.Buildings.Values)
            {
                var adjacentHexes = HexGrid.GetAdjacentHexes(building.LocationId);
                foreach (var hexId in adjacentHexes)
                {
                    if (HexGrid.Hexes.ContainsKey(hexId))
                    {
                        var hex = HexGrid.Hexes[hexId];
                        var productionValue = hex.ResourceType switch
                        {
                            ResourceType.Wood => 1.0f,
                            ResourceType.Brick => 1.0f,
                            ResourceType.Sheep => 1.2f,
                            ResourceType.Wheat => 1.3f,
                            ResourceType.Ore => 1.5f,
                            _ => 0f
                        };

                        // Weight by dice probability (6 and 8 are best)
                        var diceProbability = GetDiceProbability(hex.ProductionNumber);
                        potential += productionValue * diceProbability * (building.Type == BuildingType.City ? 2f : 1f);
                    }
                }
            }

            return potential;
        }

        /// <summary>
        /// Get dice roll probability.
        /// </summary>
        private float GetDiceProbability(int number)
        {
            var probabilities = new Dictionary<int, float>
            {
                {2, 1/36f}, {3, 2/36f}, {4, 3/36f}, {5, 4/36f}, {6, 5/36f}, {7, 6/36f},
                {8, 5/36f}, {9, 4/36f}, {10, 3/36f}, {11, 2/36f}, {12, 1/36f}
            };

            return probabilities.GetValueOrDefault(number, 0f);
        }

        /// <summary>
        /// Calculate positional advantage.
        /// </summary>
        private float CalculatePositionalAdvantage(int playerId)
        {
            var advantage = 0f;
            var playerState = Players[playerId];

            // Check access to high-production numbers
            foreach (var building in playerState.Buildings.Values)
            {
                var adjacentHexes = HexGrid.GetAdjacentHexes(building.LocationId);
                foreach (var hexId in adjacentHexes)
                {
                    if (HexGrid.Hexes.ContainsKey(hexId))
                    {
                        var hex = HexGrid.Hexes[hexId];
                        if (hex.ProductionNumber == 6 || hex.ProductionNumber == 8)
                        {
                            advantage += 2f;  // Bonus for high-probability numbers
                        }
                    }
                }
            }

            return advantage;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clone the game state (for simulation branching).
        /// </summary>
        public HeadlessGameState Clone()
        {
            return new HeadlessGameState
            {
                HexGrid = HexGrid.Clone(),
                Players = new Dictionary<int, PlayerState>(Players),
                CurrentTurn = new TurnState
                {
                    CurrentPlayerId = CurrentTurn.CurrentPlayerId,
                    TurnNumber = CurrentTurn.TurnNumber,
                    Phase = CurrentTurn.Phase,
                    DiceRoll = CurrentTurn.DiceRoll
                },
                GamePhase = GamePhase,
                VictoryState = new VictoryState
                {
                    VictoryThreshold = VictoryState.VictoryThreshold,
                    Winners = new List<int>(VictoryState.Winners)
                },
                EconomicState = new EconomicState
                {
                    TotalResourcesProduced = EconomicState.TotalResourcesProduced,
                    TotalBuildingsConstructed = EconomicState.TotalBuildingsConstructed,
                    EconomicCycles = EconomicState.EconomicCycles
                }
            };
        }

        /// <summary>
        /// Check if game is over.
        /// </summary>
        public bool IsGameOver()
        {
            return VictoryState.Winners.Any() || CurrentTurn.TurnNumber > 1000;  // Prevent infinite games
        }

        /// <summary>
        /// Get winner (if any).
        /// </summary>
        public int? GetWinner()
        {
            return VictoryState.Winners.FirstOrDefault();
        }

        #endregion

        #region Equality and Hashing

        public bool Equals(HeadlessGameState other)
        {
            var players = Players;
            return HexGrid.Equals(other.HexGrid) &&
                   players.Count == other.Players.Count &&
                   players.All(kvp => other.Players.ContainsKey(kvp.Key) && 
                                     players[kvp.Key].Equals(other.Players[kvp.Key])) &&
                   CurrentTurn.Equals(other.CurrentTurn) &&
                   GamePhase == other.GamePhase;
        }

        public override bool Equals(object obj)
        {
            return obj is HeadlessGameState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HexGrid, Players, CurrentTurn, GamePhase);
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Hex grid data without Unity dependencies.
    /// </summary>
    public struct HexGridData
    {
        public Dictionary<int, HexData> Hexes { get; set; }
        public Dictionary<int, CornerData> Corners { get; set; }
        public Dictionary<int, EdgeData> Edges { get; set; }

        public static HexGridData CreateRandomGrid(int width, int height)
        {
            var grid = new HexGridData
            {
                Hexes = new Dictionary<int, HexData>(),
                Corners = new Dictionary<int, CornerData>(),
                Edges = new Dictionary<int, EdgeData>()
            };

            // Generate hex grid
            var random = new Random();
            var resourceTypes = (ResourceType[])Enum.GetValues(typeof(ResourceType));
            
            for (int q = 0; q < width; q++)
            {
                for (int r = 0; r < height; r++)
                {
                    var hexId = q * height + r;
                    grid.Hexes[hexId] = new HexData
                    {
                        HexId = hexId,
                        Q = q,
                        R = r,
                        ResourceType = resourceTypes[random.Next(resourceTypes.Length)],
                        ProductionNumber = random.Next(2, 13),  // 2-12 dice range
                        ExhaustionLevel = 0
                    };
                }
            }

            // Generate corners and edges (simplified)
            // In a full implementation, this would properly calculate hex adjacency

            return grid;
        }

        public HexGridData Clone()
        {
            return new HexGridData
            {
                Hexes = new Dictionary<int, HexData>(Hexes),
                Corners = new Dictionary<int, CornerData>(Corners),
                Edges = new Dictionary<int, EdgeData>(Edges)
            };
        }

        public List<int> GetAdjacentHexes(int cornerId)
        {
            // Simplified adjacency calculation
            // In a full implementation, this would use proper hex coordinate math
            return new List<int>();
        }

        public List<int> GetAdjacentCorners(int cornerId)
        {
            // Simplified adjacency calculation
            return new List<int>();
        }

        public bool Equals(HexGridData other)
        {
            var hexes = Hexes;
            return hexes.Count == other.Hexes.Count &&
                   hexes.All(kvp => other.Hexes.ContainsKey(kvp.Key) && 
                                    hexes[kvp.Key].Equals(other.Hexes[kvp.Key]));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hexes, Corners, Edges);
        }
    }

    /// <summary>
    /// Individual hex data.
    /// </summary>
    public struct HexData
    {
        public int HexId { get; set; }
        public int Q { get; set; }  // Cube coordinate Q
        public int R { get; set; }  // Cube coordinate R
        public ResourceType ResourceType { get; set; }
        public int ProductionNumber { get; set; }
        public int ExhaustionLevel { get; set; }

        public bool Equals(HexData other)
        {
            return HexId == other.HexId && 
                   ResourceType == other.ResourceType && 
                   ProductionNumber == other.ProductionNumber;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HexId, ResourceType, ProductionNumber);
        }
    }

    /// <summary>
    /// Corner/vertex data.
    /// </summary>
    public struct CornerData
    {
        public int CornerId { get; set; }
        public int OwnerId { get; set; }  // -1 = unowned
        public BuildingType BuildingType { get; set; }

        public bool Equals(CornerData other)
        {
            return CornerId == other.CornerId && 
                   OwnerId == other.OwnerId && 
                   BuildingType == other.BuildingType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CornerId, OwnerId, BuildingType);
        }
    }

    /// <summary>
    /// Edge data for roads.
    /// </summary>
    public struct EdgeData
    {
        public int EdgeId { get; set; }
        public int OwnerId { get; set; }  // -1 = unowned
        public int Corner1Id { get; set; }
        public int Corner2Id { get; set; }

        public bool Equals(EdgeData other)
        {
            return EdgeId == other.EdgeId && 
                   OwnerId == other.OwnerId &&
                   Corner1Id == other.Corner1Id && 
                   Corner2Id == other.Corner2Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EdgeId, OwnerId, Corner1Id, Corner2Id);
        }
    }

    /// <summary>
    /// Player state data.
    /// </summary>
    public class PlayerState
    {
        public int PlayerId { get; set; }
        public Dictionary<ResourceType, int> Resources { get; set; } = new Dictionary<ResourceType, int>();
        public Dictionary<int, BuildingState> Buildings { get; set; } = new Dictionary<int, BuildingState>();
        public Dictionary<int, RoadState> Roads { get; set; } = new Dictionary<int, RoadState>();
        public int VictoryPoints { get; set; }
        public int ArmySize { get; set; }

        public bool Equals(PlayerState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return PlayerId == other.PlayerId &&
                   Resources.Count == other.Resources.Count &&
                   Resources.All(kvp => other.Resources.ContainsKey(kvp.Key) && 
                                       Resources[kvp.Key] == other.Resources[kvp.Key]) &&
                   Buildings.Count == other.Buildings.Count &&
                   VictoryPoints == other.VictoryPoints &&
                   ArmySize == other.ArmySize;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerId, Resources, Buildings, VictoryPoints, ArmySize);
        }
    }

    /// <summary>
    /// Building state data.
    /// </summary>
    public class BuildingState
    {
        public int BuildingId { get; set; }
        public BuildingType Type { get; set; }
        public int LocationId { get; set; }  // Corner ID
        public int ConstructionTurn { get; set; }

        public bool Equals(BuildingState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return BuildingId == other.BuildingId &&
                   Type == other.Type &&
                   LocationId == other.LocationId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BuildingId, Type, LocationId);
        }
    }

    /// <summary>
    /// Road state data.
    /// </summary>
    public class RoadState
    {
        public int RoadId { get; set; }
        public int LocationId { get; set; }  // Edge ID
        public int ConstructionTurn { get; set; }

        public bool Equals(RoadState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return RoadId == other.RoadId && LocationId == other.LocationId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoadId, LocationId);
        }
    }

    /// <summary>
    /// Turn state data.
    /// </summary>
    public struct TurnState
    {
        public int CurrentPlayerId { get; set; }
        public int TurnNumber { get; set; }
        public TurnPhase Phase { get; set; }
        public int DiceRoll { get; set; }

        public bool Equals(TurnState other)
        {
            return CurrentPlayerId == other.CurrentPlayerId &&
                   TurnNumber == other.TurnNumber &&
                   Phase == other.Phase &&
                   DiceRoll == other.DiceRoll;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(CurrentPlayerId, TurnNumber, Phase, DiceRoll);
        }
    }

    /// <summary>
    /// Victory state data.
    /// </summary>
    public class VictoryState
    {
        public int VictoryThreshold { get; set; }
        public List<int> Winners { get; set; } = new List<int>();

        public bool Equals(VictoryState other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            
            return VictoryThreshold == other.VictoryThreshold &&
                   Winners.Count == other.Winners.Count &&
                   Winners.All(winner => other.Winners.Contains(winner));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(VictoryThreshold, Winners);
        }
    }

    /// <summary>
    /// Economic state data.
    /// </summary>
    public struct EconomicState
    {
        public int TotalResourcesProduced { get; set; }
        public int TotalBuildingsConstructed { get; set; }
        public int EconomicCycles { get; set; }

        public bool Equals(EconomicState other)
        {
            return TotalResourcesProduced == other.TotalResourcesProduced &&
                   TotalBuildingsConstructed == other.TotalBuildingsConstructed &&
                   EconomicCycles == other.EconomicCycles;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TotalResourcesProduced, TotalBuildingsConstructed, EconomicCycles);
        }
    }

    #endregion

    #region Enums

    public enum ResourceType
    {
        Wood, Brick, Sheep, Wheat, Ore, Gold
    }

    public enum BuildingType
    {
        None, Settlement, City
    }

    public enum TurnPhase
    {
        Roll, Build, Trade, End
    }

    public enum GamePhase
    {
        EarlyGame, MidGame, LateGame
    }

    #endregion
}
