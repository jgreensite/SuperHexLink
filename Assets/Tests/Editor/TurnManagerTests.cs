using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SuperHexLink.Gameplay;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for TurnManager.
    /// Verifies turn-based gameplay mechanics, player rotation, and phase tracking.
    /// </summary>
    public class TurnManagerTests
    {
        private TurnManager _turnManager;
        private List<Player> _testPlayers;

        [SetUp]
        public void SetUp()
        {
            _turnManager = new TurnManager(new ActionLogSettings());
            _testPlayers = new List<Player>
            {
                new Player { Name = "Player 1", Color = Color.red },
                new Player { Name = "Player 2", Color = Color.blue },
                new Player { Name = "Player 3", Color = Color.green }
            };
        }

        [Test]
        public void TurnManager_InitializeGame_ValidPlayers_ReturnsTrue()
        {
            // Act
            bool result = _turnManager.InitializeGame(_testPlayers);

            // Assert
            Assert.IsTrue(result, "Initialization should succeed with valid players");
            Assert.AreEqual(3, _turnManager.PlayerCount, "Should have 3 players");
            Assert.AreEqual(1, _turnManager.TurnNumber, "Should start at turn 1");
            Assert.AreEqual(TurnPhase.Roll, _turnManager.CurrentPhase, "Should start in Roll phase");
            Assert.AreEqual(_testPlayers[0], _turnManager.CurrentPlayer, "First player should start");
        }

        [Test]
        public void TurnManager_InitializeGame_NullPlayers_ReturnsFalse()
        {
            // Act
            bool result = _turnManager.InitializeGame(null);

            // Assert
            Assert.IsFalse(result, "Initialization should fail with null players");
            Assert.AreEqual(0, _turnManager.PlayerCount, "Should have 0 players");
        }

        [Test]
        public void TurnManager_InitializeGame_TooFewPlayers_ReturnsFalse()
        {
            // Arrange
            List<Player> singlePlayer = new List<Player> { _testPlayers[0] };

            // Act
            bool result = _turnManager.InitializeGame(singlePlayer);

            // Assert
            Assert.IsFalse(result, "Initialization should fail with less than 2 players");
        }

        [Test]
        public void TurnManager_InitializeGame_TooManyPlayers_LogsWarning()
        {
            // Arrange
            List<Player> manyPlayers = new List<Player>();
            for (int i = 0; i < 8; i++)
            {
                manyPlayers.Add(new Player { Name = $"Player {i + 1}" });
            }

            // Act
            bool result = _turnManager.InitializeGame(manyPlayers);

            // Assert
            Assert.IsTrue(result, "Initialization should succeed but log warning");
            Assert.AreEqual(8, _turnManager.PlayerCount, "Should have all 8 players");
        }

        [Test]
        public void TurnManager_NextPhase_ValidProgression_AdvancesCorrectly()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act & Assert - Roll to Build
            bool result1 = _turnManager.NextPhase();
            Assert.IsTrue(result1, "Should advance from Roll to Build");
            Assert.AreEqual(TurnPhase.Build, _turnManager.CurrentPhase, "Should be in Build phase");

            // Act & Assert - Build to Trade
            bool result2 = _turnManager.NextPhase();
            Assert.IsTrue(result2, "Should advance from Build to Trade");
            Assert.AreEqual(TurnPhase.Trade, _turnManager.CurrentPhase, "Should be in Trade phase");

            // Act & Assert - Trade to End
            bool result3 = _turnManager.NextPhase();
            Assert.IsTrue(result3, "Should advance from Trade to End");
            Assert.AreEqual(TurnPhase.End, _turnManager.CurrentPhase, "Should be in End phase");
        }

        [Test]
        public void TurnManager_NextPhase_FromEndPhase_AdvancesToNextPlayer()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            
            // Advance to End phase
            while (_turnManager.CurrentPhase != TurnPhase.End)
            {
                _turnManager.NextPhase();
            }

            Player firstPlayer = _turnManager.CurrentPlayer;

            // Act
            bool result = _turnManager.NextPhase();

            // Assert
            Assert.IsTrue(result, "Should advance to next player from End phase");
            Assert.AreEqual(TurnPhase.Roll, _turnManager.CurrentPhase, "Should reset to Roll phase");
            Assert.AreNotEqual(firstPlayer, _turnManager.CurrentPlayer, "Should be next player");
        }

        [Test]
        public void TurnManager_NextTurn_ValidPlayers_AdvancesCorrectly()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            Player firstPlayer = _turnManager.CurrentPlayer;
            int initialTurnNumber = _turnManager.TurnNumber;

            // Act
            bool result = _turnManager.NextTurn();

            // Assert
            Assert.IsTrue(result, "Next turn should succeed");
            Assert.AreEqual(TurnPhase.Roll, _turnManager.CurrentPhase, "Should reset to Roll phase");
            Assert.AreNotEqual(firstPlayer, _turnManager.CurrentPlayer, "Should be next player");
            Assert.AreEqual(initialTurnNumber, _turnManager.TurnNumber, "Turn number should not increment yet");
        }

        [Test]
        public void TurnManager_NextTurn_CompletesFullCircle_IncrementsTurnNumber()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            int initialTurnNumber = _turnManager.TurnNumber;

            // Act - Advance through all players
            for (int i = 0; i < _testPlayers.Count; i++)
            {
                _turnManager.NextTurn();
            }

            // Assert
            Assert.AreEqual(initialTurnNumber + 1, _turnManager.TurnNumber, "Turn number should increment after full circle");
            Assert.AreEqual(_testPlayers[0], _turnManager.CurrentPlayer, "Should be back to first player");
        }

        [Test]
        public void TurnManager_SkipToPhase_ValidPhase_SkipsCorrectly()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            bool result = _turnManager.SkipToPhase(TurnPhase.Trade);

            // Assert
            Assert.IsTrue(result, "Skip to phase should succeed");
            Assert.AreEqual(TurnPhase.Trade, _turnManager.CurrentPhase, "Should be in Trade phase");
        }

        [Test]
        public void TurnManager_SkipToPhase_InvalidPhase_ReturnsFalse()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            bool result = _turnManager.SkipToPhase((TurnPhase)999);

            // Assert
            Assert.IsFalse(result, "Skip to invalid phase should fail");
        }

        [Test]
        public void TurnManager_GetPlayer_ValidNumber_ReturnsPlayer()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            Player player = _turnManager.GetPlayer(2);

            // Assert
            Assert.IsNotNull(player, "Should find player");
            Assert.AreEqual(2, player.PlayerNumber, "Should return correct player");
        }

        [Test]
        public void TurnManager_GetPlayer_InvalidNumber_ReturnsNull()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            Player player = _turnManager.GetPlayer(99);

            // Assert
            Assert.IsNull(player, "Should return null for invalid player number");
        }

        [Test]
        public void TurnManager_GetAllPlayers_ReturnsAllPlayers()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            IReadOnlyList<Player> players = _turnManager.GetAllPlayers();

            // Assert
            Assert.AreEqual(_testPlayers.Count, players.Count, "Should return all players");
            for (int i = 0; i < _testPlayers.Count; i++)
            {
                Assert.AreEqual(_testPlayers[i].PlayerNumber, players[i].PlayerNumber, $"Player {i + 1} should match");
            }
        }

        [Test]
        public void TurnManager_IsPlayerTurn_CurrentPlayer_ReturnsTrue()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            Player currentPlayer = _turnManager.CurrentPlayer;

            // Act
            bool isCurrentTurn = _turnManager.IsPlayerTurn(currentPlayer);

            // Assert
            Assert.IsTrue(isCurrentTurn, "Current player should be in turn");
        }

        [Test]
        public void TurnManager_IsPlayerTurn_OtherPlayer_ReturnsFalse()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            Player otherPlayer = _testPlayers[1]; // Not current player (assuming 3+ players)

            // Act
            bool isCurrentTurn = _turnManager.IsPlayerTurn(otherPlayer);

            // Assert
            Assert.IsFalse(isCurrentTurn, "Other player should not be in turn");
        }

        [Test]
        public void TurnManager_GetTurnState_ReturnsFormattedState()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            string turnState = _turnManager.GetTurnState();

            // Assert
            Assert.IsTrue(turnState.Contains("Turn 1"), "Should contain turn number");
            Assert.IsTrue(turnState.Contains("Player 1"), "Should contain current player");
            Assert.IsTrue(turnState.Contains("Roll"), "Should contain current phase");
        }

        [Test]
        public void TurnManager_Reset_ResetsToInitialState()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            _turnManager.NextPhase(); // Change state

            // Act
            _turnManager.Reset();

            // Assert
            Assert.AreEqual(0, _turnManager.PlayerCount, "Should have 0 players after reset");
            Assert.AreEqual(0, _turnManager.TurnNumber, "Should have turn number 0 after reset");
            Assert.AreEqual(TurnPhase.Roll, _turnManager.CurrentPhase, "Should be in Roll phase after reset");
        }

        [Test]
        public void TurnManager_ValidateState_ValidState_ReturnsTrue()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            bool isValid = _turnManager.ValidateState();

            // Assert
            Assert.IsTrue(isValid, "Valid state should pass validation");
        }

        [Test]
        public void TurnManager_ValidateState_NoPlayers_ReturnsFalse()
        {
            // Arrange
            _turnManager.Reset(); // No players initialized

            // Act
            bool isValid = _turnManager.ValidateState();

            // Assert
            Assert.IsFalse(isValid, "Invalid state (no players) should fail validation");
        }

        [Test]
        public void TurnManager_Events_FireCorrectly()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);
            
            bool currentPlayerChangedFired = false;
            bool phaseChangedFired = false;
            bool turnStartedFired = false;
            bool turnEndedFired = false;

            _turnManager.CurrentPlayerChanged += (player) => currentPlayerChangedFired = true;
            _turnManager.PhaseChanged += (from, to) => phaseChangedFired = true;
            _turnManager.TurnStarted += (turn, player) => turnStartedFired = true;
            _turnManager.TurnEnded += (turn, player) => turnEndedFired = true;

            // Act
            _turnManager.NextPhase(); // Should trigger PhaseChanged
            _turnManager.NextTurn();  // Should trigger all events

            // Assert
            Assert.IsTrue(phaseChangedFired, "PhaseChanged should fire");
            Assert.IsTrue(currentPlayerChangedFired, "CurrentPlayerChanged should fire");
            Assert.IsTrue(turnStartedFired, "TurnStarted should fire");
            Assert.IsTrue(turnEndedFired, "TurnEnded should fire");
        }

        [Test]
        public void TurnManager_NextPhase_NoPlayers_ReturnsFalse()
        {
            // Arrange
            _turnManager.Reset(); // No players

            // Act
            bool result = _turnManager.NextPhase();

            // Assert
            Assert.IsFalse(result, "Next phase should fail with no players");
        }

        [Test]
        public void TurnManager_NextTurn_NoPlayers_ReturnsFalse()
        {
            // Arrange
            _turnManager.Reset(); // No players

            // Act
            bool result = _turnManager.NextTurn();

            // Assert
            Assert.IsFalse(result, "Next turn should fail with no players");
        }

        [Test]
        public void TurnManager_SkipToPhase_NoPlayers_ReturnsFalse()
        {
            // Arrange
            _turnManager.Reset(); // No players

            // Act
            bool result = _turnManager.SkipToPhase(TurnPhase.Build);

            // Assert
            Assert.IsFalse(result, "Skip to phase should fail with no players");
        }

        [Test]
        public void TurnManager_PlayerNumbers_AssignedCorrectly()
        {
            // Arrange & Act
            _turnManager.InitializeGame(_testPlayers);

            // Assert
            for (int i = 0; i < _testPlayers.Count; i++)
            {
                Assert.AreEqual(i + 1, _testPlayers[i].PlayerNumber, $"Player {i + 1} should have correct number");
            }
        }

        [Test]
        public void TurnManager_Performance_ManyTurns_OperatesEfficiently()
        {
            // Arrange
            _turnManager.InitializeGame(_testPlayers);

            // Act
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                _turnManager.NextPhase(); // Through all phases
                _turnManager.NextPhase();
                _turnManager.NextPhase();
                _turnManager.NextPhase(); // Should trigger NextTurn
            }
            
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "1000 turns should complete quickly");
        }

        [Test]
        public void Player_ResetTurnState_ResetsState()
        {
            // Arrange
            Player player = new Player();
            player.VictoryPoints = 5;
            player.Resources[ResourceType.Wood] = 3;

            // Act
            player.ResetTurnState();

            // Assert
            Assert.AreEqual(5, player.VictoryPoints, "Victory points should persist");
            Assert.AreEqual(3, player.Resources[ResourceType.Wood], "Resources should persist");
            // Turn-specific state would be reset (implementation dependent)
        }
    }
}
