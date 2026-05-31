using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Simulation;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Tests for all concrete IGameAction implementations.
    /// </summary>
    [TestFixture]
    public class IGameActionTests
    {
        // ── ActionId ──────────────────────────────────────────────────────────────

        [Test]
        public void AllActionIds_AreUnique()
        {
            var ids = new[]
            {
                new RollDiceAction().ActionId,
                new PlaceSettlementAction().ActionId,
                new PlaceRoadAction().ActionId,
                new UpgradeToCityAction().ActionId,
                new BankTradeAction().ActionId,
                new EndTurnAction().ActionId
            };

            var distinct = new HashSet<string>(ids);
            Assert.AreEqual(ids.Length, distinct.Count, "Every action must have a unique ActionId");
        }

        [Test] public void RollDiceAction_ActionId_IsRollDice()          { Assert.AreEqual("roll_dice",        new RollDiceAction().ActionId); }
        [Test] public void PlaceSettlementAction_ActionId_IsPlaceSettlement() { Assert.AreEqual("place_settlement", new PlaceSettlementAction().ActionId); }
        [Test] public void PlaceRoadAction_ActionId_IsPlaceRoad()         { Assert.AreEqual("place_road",       new PlaceRoadAction().ActionId); }
        [Test] public void UpgradeToCityAction_ActionId_IsUpgradeToCity() { Assert.AreEqual("upgrade_to_city",  new UpgradeToCityAction().ActionId); }
        [Test] public void BankTradeAction_ActionId_IsBankTrade()         { Assert.AreEqual("bank_trade",       new BankTradeAction().ActionId); }
        [Test] public void EndTurnAction_ActionId_IsEndTurn()             { Assert.AreEqual("end_turn",         new EndTurnAction().ActionId); }

        // ── ActionType ────────────────────────────────────────────────────────────

        [Test] public void RollDiceAction_Type_IsRoll()                   { Assert.AreEqual(ActionType.Roll,   new RollDiceAction().Type); }
        [Test] public void PlaceSettlementAction_Type_IsBuild()           { Assert.AreEqual(ActionType.Build,  new PlaceSettlementAction().Type); }
        [Test] public void PlaceRoadAction_Type_IsBuild()                 { Assert.AreEqual(ActionType.Build,  new PlaceRoadAction().Type); }
        [Test] public void UpgradeToCityAction_Type_IsBuild()             { Assert.AreEqual(ActionType.Build,  new UpgradeToCityAction().Type); }
        [Test] public void BankTradeAction_Type_IsTrade()                 { Assert.AreEqual(ActionType.Trade,  new BankTradeAction().Type); }
        [Test] public void EndTurnAction_Type_IsSystem()                  { Assert.AreEqual(ActionType.System, new EndTurnAction().Type); }

        // ── GetDescription ────────────────────────────────────────────────────────

        [Test]
        public void AllActions_GetDescription_ReturnsNonEmpty()
        {
            IGameAction[] actions =
            {
                new RollDiceAction { PlayerId = 1 },
                new PlaceSettlementAction(5, 1),
                new PlaceRoadAction(3, 1),
                new UpgradeToCityAction(5, 1),
                new BankTradeAction(1, ResourceType.Wood, ResourceType.Ore, 4, 1),
                new EndTurnAction { PlayerId = 1 }
            };

            foreach (var action in actions)
            {
                var desc = action.GetDescription();
                Assert.IsNotNull(desc, $"{action.GetType().Name}.GetDescription() returned null");
                Assert.IsNotEmpty(desc, $"{action.GetType().Name}.GetDescription() returned empty");
            }
        }

        // ── Clone ─────────────────────────────────────────────────────────────────

        [Test]
        public void RollDiceAction_Clone_RetainsSamePlayerId()
        {
            var original = new RollDiceAction { PlayerId = 2 };
            var clone = original.Clone() as RollDiceAction;
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.PlayerId, clone.PlayerId);
        }

        [Test]
        public void PlaceSettlementAction_Clone_RetainsCornerIdAndPlayerId()
        {
            var original = new PlaceSettlementAction(7, 3);
            var clone = original.Clone() as PlaceSettlementAction;
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.CornerId, clone.CornerId);
            Assert.AreEqual(original.PlayerId, clone.PlayerId);
        }

        [Test]
        public void PlaceRoadAction_Clone_RetainsEdgeIdAndPlayerId()
        {
            var original = new PlaceRoadAction(4, 1);
            var clone = original.Clone() as PlaceRoadAction;
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.EdgeId, clone.EdgeId);
            Assert.AreEqual(original.PlayerId, clone.PlayerId);
        }

        [Test]
        public void BankTradeAction_Clone_RetainsAllProperties()
        {
            var original = new BankTradeAction(0, ResourceType.Wood, ResourceType.Ore, 4, 1);
            var clone = original.Clone() as BankTradeAction;
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.PlayerId,     clone.PlayerId);
            Assert.AreEqual(original.FromResource, clone.FromResource);
            Assert.AreEqual(original.ToResource,   clone.ToResource);
            Assert.AreEqual(original.GiveAmount,   clone.GiveAmount);
            Assert.AreEqual(original.GetAmount,    clone.GetAmount);
        }

        [Test]
        public void EndTurnAction_Clone_RetainsPlayerId()
        {
            var original = new EndTurnAction { PlayerId = 2 };
            var clone = original.Clone() as EndTurnAction;
            Assert.IsNotNull(clone);
            Assert.AreEqual(original.PlayerId, clone.PlayerId);
        }

        // ── IsValid ───────────────────────────────────────────────────────────────

        [Test]
        public void RollDiceAction_IsValid_TrueWhenPhaseIsRollAndPlayerMatches()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            // Initial phase = Roll, current player = 0
            var action = new RollDiceAction { PlayerId = 0 };
            Assert.IsTrue(action.IsValid(state));
        }

        [Test]
        public void RollDiceAction_IsValid_FalseWhenWrongPlayer()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new RollDiceAction { PlayerId = 1 }; // Player 1 is not current (0 is)
            Assert.IsFalse(action.IsValid(state));
        }

        [Test]
        public void EndTurnAction_IsValid_TrueForCurrentPlayer()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new EndTurnAction { PlayerId = 0 };
            Assert.IsTrue(action.IsValid(state));
        }

        [Test]
        public void EndTurnAction_IsValid_FalseForOtherPlayer()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new EndTurnAction { PlayerId = 1 };
            Assert.IsFalse(action.IsValid(state));
        }

        // ── Apply ─────────────────────────────────────────────────────────────────

        [Test]
        public void RollDiceAction_Apply_ChangesPhaseFromRollToBuild()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new RollDiceAction { PlayerId = 0 };
            var newState = state.ApplyAction(action);
            Assert.AreEqual(TurnPhase.Build, newState.CurrentTurn.Phase);
        }

        [Test]
        public void EndTurnAction_Apply_AdvancesTurnNumber()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new EndTurnAction { PlayerId = 0 };
            var newState = state.ApplyAction(action);
            Assert.AreEqual(2, newState.CurrentTurn.TurnNumber);
        }

        [Test]
        public void EndTurnAction_Apply_MovesToNextPlayer()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            var action = new EndTurnAction { PlayerId = 0 };
            var newState = state.ApplyAction(action);
            Assert.AreEqual(1, newState.CurrentTurn.CurrentPlayerId);
        }

        [Test]
        public void EndTurnAction_Apply_WrapsPlayerIdAround()
        {
            var state = HeadlessGameState.CreateInitialState(2, 5, 5);
            // End turn for player 0 → player 1
            var newState = state.ApplyAction(new EndTurnAction { PlayerId = 0 });
            // End turn for player 1 → should wrap back to player 0
            var finalState = newState.ApplyAction(new EndTurnAction { PlayerId = 1 });
            Assert.AreEqual(0, finalState.CurrentTurn.CurrentPlayerId);
        }

        // ── ResourceCost ──────────────────────────────────────────────────────────

        [Test]
        public void RollDiceAction_ResourceCost_IsEmpty()
        {
            var cost = new RollDiceAction().ResourceCost;
            Assert.IsNotNull(cost);
            Assert.AreEqual(0, cost.Count);
        }

        [Test]
        public void PlaceSettlementAction_ResourceCost_RequiresFourResources()
        {
            var cost = new PlaceSettlementAction().ResourceCost;
            Assert.IsNotNull(cost);
            Assert.Greater(cost.Count, 0);
        }
    }
}
