using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Simulation;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    [TestFixture]
    public class HeadlessGameStateTests
    {
        // ── CreateInitialState ────────────────────────────────────────────────────

        [Test]
        public void CreateInitialState_WithTwoPlayers_HasTwoPlayers()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.AreEqual(2, state.Players.Count);
        }

        [Test]
        public void CreateInitialState_HasTurnNumberOne()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.AreEqual(1, state.CurrentTurn.TurnNumber);
        }

        [Test]
        public void CreateInitialState_StartsWithRollPhase()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.AreEqual(TurnPhase.Roll, state.CurrentTurn.Phase);
        }

        [Test]
        public void CreateInitialState_CurrentPlayerIsZero()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.AreEqual(0, state.CurrentTurn.CurrentPlayerId);
        }

        [Test]
        public void CreateInitialState_PlayersHaveZeroVictoryPoints()
        {
            var state = HeadlessGameState.CreateInitialState(4, 5, 5);
            foreach (var player in state.Players.Values)
            {
                Assert.AreEqual(0, player.VictoryPoints);
            }
        }

        [Test]
        public void CreateInitialState_PlayersHaveZeroResources()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            foreach (var resource in state.Players[0].Resources.Values)
            {
                Assert.AreEqual(0, resource);
            }
        }

        [Test]
        public void CreateInitialState_HexGridIsPopulated()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.IsNotNull(state.HexGrid.Hexes);
            Assert.Greater(state.HexGrid.Hexes.Count, 0);
        }

        // ── IsGameOver ────────────────────────────────────────────────────────────

        [Test]
        public void IsGameOver_ReturnsFalse_OnInitialState()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            Assert.IsFalse(state.IsGameOver());
        }

        // ── GetValidMoves ─────────────────────────────────────────────────────────

        [Test]
        public void GetValidMoves_InRollPhase_ReturnsRollDiceAction()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            // Initial phase is Roll
            var moves = state.GetValidMoves();
            Assert.IsNotNull(moves);
            Assert.AreEqual(1, moves.Count);
            Assert.IsInstanceOf<RollDiceAction>(moves[0]);
        }

        // ── Clone ─────────────────────────────────────────────────────────────────

        [Test]
        public void Clone_ReturnsCopyWithSameTurnNumber()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var clone = state.Clone();
            Assert.AreEqual(state.CurrentTurn.TurnNumber, clone.CurrentTurn.TurnNumber);
        }

        [Test]
        public void Clone_WinnersListIsIndependent()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var clone = state.Clone();

            // Mutating clone's winners list must not affect original
            clone.VictoryState.Winners.Add(99);

            Assert.IsFalse(state.VictoryState.Winners.Contains(99));
        }

        // ── ApplyAction ───────────────────────────────────────────────────────────

        [Test]
        public void ApplyAction_RollDice_ChangesPhaseFromRollToBuild()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var rollAction = new RollDiceAction { PlayerId = 0 };

            var newState = state.ApplyAction(rollAction);

            Assert.AreEqual(TurnPhase.Build, newState.CurrentTurn.Phase);
        }

        [Test]
        public void ApplyAction_RollDice_SetsNonZeroDiceRoll()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var rollAction = new RollDiceAction { PlayerId = 0 };

            var newState = state.ApplyAction(rollAction);

            // Dice roll must be between 2 and 12
            Assert.GreaterOrEqual(newState.CurrentTurn.DiceRoll, 2);
            Assert.LessOrEqual(newState.CurrentTurn.DiceRoll, 12);
        }

        [Test]
        public void ApplyAction_DoesNotMutateOriginalState()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var rollAction = new RollDiceAction { PlayerId = 0 };

            _ = state.ApplyAction(rollAction);

            // Original state must remain unchanged
            Assert.AreEqual(TurnPhase.Roll, state.CurrentTurn.Phase);
        }

        // ── EvaluateBoardState ────────────────────────────────────────────────────

        [Test]
        public void EvaluateBoardState_ReturnsFloat_ForValidPlayer()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            // Should not throw and return a finite float
            var score = state.EvaluateBoardState(0);
            Assert.IsTrue(float.IsFinite(score), "Board evaluation must return a finite float");
        }
    }
}
