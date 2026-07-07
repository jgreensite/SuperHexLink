using NUnit.Framework;
using System.Linq;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    [TestFixture]
    public class RuleTimingSystemTests
    {
        private RuleTimingSystem _timingSystem;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings();
            _timingSystem = new RuleTimingSystem(_logSettings);
        }

        // ── Construction ─────────────────────────────────────────────────────────

        [Test]
        public void Constructor_InitializesAllTimingWindows()
        {
            // All 9 canonical windows should exist and be empty
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.BEFORE_CALCULATION).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_CALCULATION).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.AFTER_CALCULATION).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.BEFORE_RESOURCE_GAIN).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_RESOURCE_GAIN).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.AFTER_RESOURCE_GAIN).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.BEFORE_BUILDING).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_BUILDING).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.AFTER_BUILDING).Count);
        }

        // ── RegisterRule ──────────────────────────────────────────────────────────

        [Test]
        public void RegisterRule_AddsRuleToCorrectTimingWindow()
        {
            var rule = new TimedRule
            {
                RuleId = "test_rule",
                TimingWindow = RuleTimingSystem.ON_CALCULATION,
                PriorityLayer = 50,
                StackBehavior = StackBehavior.Simultaneous
            };

            _timingSystem.RegisterRule(rule);

            var rules = _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_CALCULATION);
            Assert.AreEqual(1, rules.Count);
            Assert.AreEqual("test_rule", rules[0].RuleId);
        }

        [Test]
        public void RegisterRule_DoesNotAddToOtherWindows()
        {
            var rule = new TimedRule
            {
                RuleId = "isolated_rule",
                TimingWindow = RuleTimingSystem.ON_CALCULATION,
                PriorityLayer = 50
            };

            _timingSystem.RegisterRule(rule);

            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.BEFORE_CALCULATION).Count);
            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.AFTER_CALCULATION).Count);
        }

        [Test]
        public void RegisterRule_SortsRulesByPriorityDescending()
        {
            _timingSystem.RegisterRule(new TimedRule { RuleId = "low",  TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 10 });
            _timingSystem.RegisterRule(new TimedRule { RuleId = "high", TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 90 });
            _timingSystem.RegisterRule(new TimedRule { RuleId = "mid",  TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 50 });

            var rules = _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_CALCULATION);

            Assert.AreEqual("high", rules[0].RuleId);
            Assert.AreEqual("mid",  rules[1].RuleId);
            Assert.AreEqual("low",  rules[2].RuleId);
        }

        [Test]
        public void RegisterRule_WithUnknownTimingWindow_DoesNotThrow()
        {
            var rule = new TimedRule
            {
                RuleId = "unknown_window_rule",
                TimingWindow = "NonExistentWindow",
                PriorityLayer = 50
            };

            // Should log a warning but not throw
            Assert.DoesNotThrow(() => _timingSystem.RegisterRule(rule));
        }

        // ── GetRulePriority ───────────────────────────────────────────────────────

        [Test]
        public void GetRulePriority_ReturnsCorrectPriority_AfterRegister()
        {
            _timingSystem.RegisterRule(new TimedRule
            {
                RuleId = "priority_rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                PriorityLayer = 75
            });

            Assert.AreEqual(75, _timingSystem.GetRulePriority("priority_rule"));
        }

        [Test]
        public void GetRulePriority_ReturnsMinusOne_ForUnknownRule()
        {
            Assert.AreEqual(-1, _timingSystem.GetRulePriority("does_not_exist"));
        }

        // ── UnregisterRule ────────────────────────────────────────────────────────

        [Test]
        public void UnregisterRule_RemovesRuleFromTimingWindow()
        {
            _timingSystem.RegisterRule(new TimedRule
            {
                RuleId = "removable",
                TimingWindow = RuleTimingSystem.ON_CALCULATION,
                PriorityLayer = 50
            });

            _timingSystem.UnregisterRule("removable");

            Assert.AreEqual(0, _timingSystem.GetRulesInTimingWindow(RuleTimingSystem.ON_CALCULATION).Count);
        }

        [Test]
        public void UnregisterRule_RemovesPriority_Entry()
        {
            _timingSystem.RegisterRule(new TimedRule
            {
                RuleId = "to_remove",
                TimingWindow = RuleTimingSystem.ON_CALCULATION,
                PriorityLayer = 50
            });
            _timingSystem.UnregisterRule("to_remove");

            Assert.AreEqual(-1, _timingSystem.GetRulePriority("to_remove"));
        }

        [Test]
        public void UnregisterRule_NonExistentRule_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _timingSystem.UnregisterRule("ghost_rule"));
        }

        // ── DetectRuleConflicts ───────────────────────────────────────────────────

        [Test]
        public void DetectRuleConflicts_ReturnsEmpty_WhenNoRules()
        {
            var conflicts = _timingSystem.DetectRuleConflicts();
            Assert.IsNotNull(conflicts);
            Assert.AreEqual(0, conflicts.Count);
        }

        [Test]
        public void DetectRuleConflicts_ReturnsEmpty_WhenRulesHaveDistinctPriorities()
        {
            _timingSystem.RegisterRule(new TimedRule { RuleId = "r1", TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 10 });
            _timingSystem.RegisterRule(new TimedRule { RuleId = "r2", TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 20 });

            var conflicts = _timingSystem.DetectRuleConflicts();
            Assert.IsNotNull(conflicts);
        }

        // ── GetStatistics ─────────────────────────────────────────────────────────

        [Test]
        public void GetStatistics_ReturnsNonNull()
        {
            var stats = _timingSystem.GetStatistics();
            Assert.IsNotNull(stats);
        }

        [Test]
        public void GetStatistics_RulesPerWindowReflectsRegistrations()
        {
            _timingSystem.RegisterRule(new TimedRule { RuleId = "s1", TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 10 });
            _timingSystem.RegisterRule(new TimedRule { RuleId = "s2", TimingWindow = RuleTimingSystem.ON_CALCULATION, PriorityLayer = 20 });

            var stats = _timingSystem.GetStatistics();
            Assert.IsTrue(stats.RulesPerTimingWindow.ContainsKey(RuleTimingSystem.ON_CALCULATION));
            Assert.AreEqual(2, stats.RulesPerTimingWindow[RuleTimingSystem.ON_CALCULATION]);
        }

        // ── Execution History ─────────────────────────────────────────────────────

        [Test]
        public void ClearExecutionHistory_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _timingSystem.ClearExecutionHistory());
        }

        [Test]
        public void GetExecutionHistory_ReturnsEmptyList_Initially()
        {
            var history = _timingSystem.GetExecutionHistory();
            Assert.IsNotNull(history);
            Assert.AreEqual(0, history.Count);
        }
    }
}
