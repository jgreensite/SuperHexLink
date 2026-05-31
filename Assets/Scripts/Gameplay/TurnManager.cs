using System;
using System.Collections.Generic;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Manages turn-based gameplay including player rotation and phase tracking.
    /// Provides the foundation for all core gameplay mechanics in Catan-like games.
    /// Implements production-grade turn management with comprehensive validation and logging.
    /// </summary>
    public class TurnManager
    {
        private readonly ActionLogSettings _logSettings;
        private readonly List<Player> _players;
        private int _currentPlayerIndex;
        private TurnPhase _currentPhase;
        private int _turnNumber;
        private Dictionary<TurnPhase, Action<TurnPhase>> _phaseEnterActions;
        private Dictionary<TurnPhase, Action<TurnPhase>> _phaseExitActions;

        /// <summary>
        /// Gets the current player whose turn it is.
        /// </summary>
        public Player CurrentPlayer => _players.Count > 0 ? _players[_currentPlayerIndex] : null;

        /// <summary>
        /// Gets the current phase of the turn.
        /// </summary>
        public TurnPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Gets the current turn number (1-based).
        /// </summary>
        public int TurnNumber => _turnNumber;

        /// <summary>
        /// Gets the total number of players in the game.
        /// </summary>
        public int PlayerCount => _players.Count;

        /// <summary>
        /// Event fired when the current player changes.
        /// </summary>
        public event Action<Player> CurrentPlayerChanged;

        /// <summary>
        /// Event fired when the current phase changes.
        /// </summary>
        public event Action<TurnPhase, TurnPhase> PhaseChanged;

        /// <summary>
        /// Event fired when a new turn begins.
        /// </summary>
        public event Action<int, Player> TurnStarted;

        /// <summary>
        /// Event fired when a turn ends.
        /// </summary>
        public event Action<int, Player> TurnEnded;

        public TurnManager(ActionLogSettings logSettings = null)
        {
            _logSettings = logSettings ?? new ActionLogSettings();
            _players = new List<Player>();
            _currentPlayerIndex = 0;
            _currentPhase = TurnPhase.Roll;
            _turnNumber = 0;
            
            InitializePhaseActions();
        }

        /// <summary>
        /// Initializes the game with the specified players.
        /// </summary>
        /// <param name="players">List of players participating in the game</param>
        /// <returns>True if initialization succeeded, false otherwise</returns>
        public bool InitializeGame(List<Player> players)
        {
            if (players == null || players.Count < 2)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "TurnManager: Cannot initialize game with less than 2 players");
                return false;
            }

            if (players.Count > 6)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                    $"TurnManager: Game initialized with {players.Count} players (recommended: 2-6)");
            }

            _players.Clear();
            _players.AddRange(players);
            _currentPlayerIndex = 0;
            _currentPhase = TurnPhase.Roll;
            _turnNumber = 1;

            // Assign player numbers
            for (int i = 0; i < _players.Count; i++)
            {
                _players[i].PlayerNumber = i + 1;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                $"TurnManager: Game initialized with {_players.Count} players. Player {_players[0].PlayerNumber} starts");

            // Start the first turn
            StartTurn();

            return true;
        }

        /// <summary>
        /// Advances to the next phase in the current turn.
        /// </summary>
        /// <returns>True if phase advanced successfully, false if at end phase</returns>
        public bool NextPhase()
        {
            if (_players.Count == 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "TurnManager: Cannot advance phase - no players initialized");
                return false;
            }

            TurnPhase previousPhase = _currentPhase;
            
            // Execute phase exit actions
            if (_phaseExitActions.TryGetValue(_currentPhase, out Action<TurnPhase> exitAction))
            {
                exitAction(previousPhase);
            }

            // Determine next phase
            switch (_currentPhase)
            {
                case TurnPhase.Roll:
                    _currentPhase = TurnPhase.Build;
                    break;
                case TurnPhase.Build:
                    _currentPhase = TurnPhase.Trade;
                    break;
                case TurnPhase.Trade:
                    _currentPhase = TurnPhase.End;
                    break;
                case TurnPhase.End:
                    // End of turn - move to next player
                    return NextTurn();
                default:
                    ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                        $"TurnManager: Unknown phase {_currentPhase}");
                    return false;
            }

            // Execute phase enter actions
            if (_phaseEnterActions.TryGetValue(_currentPhase, out Action<TurnPhase> enterAction))
            {
                enterAction(_currentPhase);
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} advanced from {previousPhase} to {_currentPhase}");

            PhaseChanged?.Invoke(previousPhase, _currentPhase);
            return true;
        }

        /// <summary>
        /// Advances to the next player's turn.
        /// </summary>
        /// <returns>True if turn advanced successfully</returns>
        public bool NextTurn()
        {
            if (_players.Count == 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "TurnManager: Cannot advance turn - no players initialized");
                return false;
            }

            Player previousPlayer = CurrentPlayer;
            
            // End current turn
            TurnEnded?.Invoke(_turnNumber, previousPlayer);

            // Move to next player
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
            
            // Increment turn number when we loop back to first player
            if (_currentPlayerIndex == 0)
            {
                _turnNumber++;
            }

            // Reset to roll phase
            TurnPhase previousPhase = _currentPhase;
            _currentPhase = TurnPhase.Roll;

            // Execute phase exit/enter actions
            if (_phaseExitActions.TryGetValue(previousPhase, out Action<TurnPhase> exitAction))
            {
                exitAction(previousPhase);
            }

            if (_phaseEnterActions.TryGetValue(_currentPhase, out Action<TurnPhase> enterAction))
            {
                enterAction(_currentPhase);
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                $"TurnManager: Turn {_turnNumber} - Player {CurrentPlayer?.PlayerNumber} begins (from Player {previousPlayer?.PlayerNumber})");

            CurrentPlayerChanged?.Invoke(CurrentPlayer);
            TurnStarted?.Invoke(_turnNumber, CurrentPlayer);
            PhaseChanged?.Invoke(previousPhase, _currentPhase);

            StartTurn();
            return true;
        }

        /// <summary>
        /// Skips to a specific phase (for debugging or special game rules).
        /// </summary>
        /// <param name="targetPhase">Target phase to skip to</param>
        /// <returns>True if skip succeeded</returns>
        public bool SkipToPhase(TurnPhase targetPhase)
        {
            if (_players.Count == 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "TurnManager: Cannot skip phase - no players initialized");
                return false;
            }

            if (!Enum.IsDefined(typeof(TurnPhase), targetPhase))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"TurnManager: Invalid target phase {targetPhase}");
                return false;
            }

            TurnPhase previousPhase = _currentPhase;

            // Execute phase exit actions
            if (_phaseExitActions.TryGetValue(_currentPhase, out Action<TurnPhase> exitAction))
            {
                exitAction(previousPhase);
            }

            _currentPhase = targetPhase;

            // Execute phase enter actions
            if (_phaseEnterActions.TryGetValue(_currentPhase, out Action<TurnPhase> enterAction))
            {
                enterAction(_currentPhase);
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} skipped from {previousPhase} to {targetPhase}");

            PhaseChanged?.Invoke(previousPhase, _currentPhase);
            return true;
        }

        /// <summary>
        /// Gets a player by their player number.
        /// </summary>
        /// <param name="playerNumber">Player number (1-based)</param>
        /// <returns>Player instance or null if not found</returns>
        public Player GetPlayer(int playerNumber)
        {
            return _players.Find(p => p.PlayerNumber == playerNumber);
        }

        /// <summary>
        /// Gets all players in turn order.
        /// </summary>
        /// <returns>Read-only list of players</returns>
        public IReadOnlyList<Player> GetAllPlayers()
        {
            return _players.AsReadOnly();
        }

        /// <summary>
        /// Checks if it's the specified player's turn.
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if it's the player's turn</returns>
        public bool IsPlayerTurn(Player player)
        {
            return CurrentPlayer == player;
        }

        /// <summary>
        /// Gets the current turn state as a string for debugging.
        /// </summary>
        /// <returns>Formatted turn state</returns>
        public string GetTurnState()
        {
            return $"Turn {_turnNumber}, Player {CurrentPlayer?.PlayerNumber ?? 0}, Phase: {_currentPhase}";
        }

        /// <summary>
        /// Resets the turn manager to initial state.
        /// </summary>
        public void Reset()
        {
            _currentPlayerIndex = 0;
            _currentPhase = TurnPhase.Roll;
            _turnNumber = 0;

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                "TurnManager: Reset to initial state");
        }

        /// <summary>
        /// Validates the current state of the turn manager.
        /// </summary>
        /// <returns>True if state is valid</returns>
        public bool ValidateState()
        {
            if (_players.Count == 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "TurnManager: No players initialized");
                return false;
            }

            if (_currentPlayerIndex < 0 || _currentPlayerIndex >= _players.Count)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"TurnManager: Invalid player index {_currentPlayerIndex} for {_players.Count} players");
                return false;
            }

            if (_turnNumber < 1)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    $"TurnManager: Invalid turn number {_turnNumber}");
                return false;
            }

            return true;
        }

        private void InitializePhaseActions()
        {
            _phaseEnterActions = new Dictionary<TurnPhase, Action<TurnPhase>>
            {
                [TurnPhase.Roll] = OnRollPhaseEnter,
                [TurnPhase.Build] = OnBuildPhaseEnter,
                [TurnPhase.Trade] = OnTradePhaseEnter,
                [TurnPhase.End] = OnEndPhaseEnter
            };

            _phaseExitActions = new Dictionary<TurnPhase, Action<TurnPhase>>
            {
                [TurnPhase.Roll] = OnRollPhaseExit,
                [TurnPhase.Build] = OnBuildPhaseExit,
                [TurnPhase.Trade] = OnTradePhaseExit,
                [TurnPhase.End] = OnEndPhaseExit
            };
        }

        private void StartTurn()
        {
            // Reset player's turn-specific state
            if (CurrentPlayer != null)
            {
                CurrentPlayer.ResetTurnState();
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Started turn {_turnNumber} for Player {CurrentPlayer?.PlayerNumber}");
        }

        private void OnRollPhaseEnter(TurnPhase fromPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} entered Roll phase");
        }

        private void OnRollPhaseExit(TurnPhase toPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} exited Roll phase");
        }

        private void OnBuildPhaseEnter(TurnPhase fromPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} entered Build phase");
        }

        private void OnBuildPhaseExit(TurnPhase toPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} exited Build phase");
        }

        private void OnTradePhaseEnter(TurnPhase fromPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} entered Trade phase");
        }

        private void OnTradePhaseExit(TurnPhase toPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} exited Trade phase");
        }

        private void OnEndPhaseEnter(TurnPhase fromPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} entered End phase");
        }

        private void OnEndPhaseExit(TurnPhase toPhase)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Debug,
                $"TurnManager: Player {CurrentPlayer?.PlayerNumber} exited End phase");
        }
    }

    /// <summary>
    /// Represents the different phases of a turn.
    /// </summary>
    public enum TurnPhase
    {
        Roll,    // Player rolls dice
        Build,   // Player builds structures
        Trade,   // Player trades resources
        End      // Turn end cleanup
    }

    /// <summary>
    /// Represents a player in the game.
    /// </summary>
    public class Player
    {
        public int PlayerNumber { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public bool IsAI { get; set; }
        public int VictoryPoints { get; set; }
        public Dictionary<ResourceType, int> Resources { get; private set; }

        public Player()
        {
            Resources = new Dictionary<ResourceType, int>();
            ResetTurnState();
        }

        public void ResetTurnState()
        {
            // Reset any turn-specific state here
            // This could include things like:
            // - Buildings built this turn
            // - Trades made this turn
            // - Development cards played this turn
        }
    }

    /// <summary>
    /// Resource types in the game.
    /// </summary>
    public enum ResourceType
    {
        Wood,
        Brick,
        Sheep,
        Wheat,
        Ore
    }
}
