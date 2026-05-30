using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Unit tests for RuleConflictResolver functionality.
    /// Tests specific-beats-general override logic and scope-based priority resolution.
    /// </summary>
    [TestFixture]
    public class RuleConflictResolverTests
    {
        private RuleConflictResolver _resolver;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings
            {
                IsEnabled = (category, severity) => true,
                LogLevel = ActionLogSeverity.Info
            };
            _resolver = new RuleConflictResolver(_logSettings);
        }

        [Test]
        public void Constructor_InitializesWithDefaultRules()
        {
            // Act
            var allRules = _resolver.GetAllRules();

            // Assert
            Assert.IsTrue(allRules.Count > 0, "Should have default rules initialized");
            Assert.IsTrue(allRules.ContainsKey("resource_production"), "Should have resource_production rule");
            Assert.IsTrue(allRules.ContainsKey("settlement_production"), "Should have settlement_production rule");
        }

        [Test]
        public void RegisterRule_AddsRuleWithCorrectProperties()
        {
            // Arrange
            const string ruleId = "test_rule";
            var scope = RuleConflictResolver.RuleScope.PlayerSpecific;
            var behavior = RuleConflictResolver.RuleBehavior.Override;
            const int priority = 25;

            // Act
            _resolver.RegisterRule(ruleId, scope, behavior, priority);
            var ruleInfo = _resolver.GetRuleInfo(ruleId);

            // Assert
            Assert.AreEqual(ruleId, ruleInfo["ruleId"]);
            Assert.AreEqual(scope, ruleInfo["scope"]);
            Assert.AreEqual(behavior, ruleInfo["behavior"]);
            Assert.AreEqual(priority, ruleInfo["priority"]);
        }

        [Test]
        public void DetectConflicts_NoConflicts_ReturnsEmptyResolution()
        {
            // Arrange
            var ruleIds = new[] { "resource_production", "settlement_production" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsFalse(resolution.HasConflicts, "Should not have conflicts");
            Assert.AreEqual(0, resolution.Conflicts.Count, "Should have no conflicts");
            Assert.AreEqual("specific-beats-general", resolution.ResolutionStrategy);
        }

        [Test]
        public void DetectConflicts_OverrideConflict_ResolvesWithSpecificBeatsGeneral()
        {
            // Arrange
            _resolver.RegisterRule("global_bonus", RuleConflictResolver.RuleScope.Global, RuleConflictResolver.RuleBehavior.Override, 10);
            _resolver.RegisterRule("player_bonus", RuleConflictResolver.RuleScope.PlayerSpecific, RuleConflictResolver.RuleBehavior.Override, 20);
            var ruleIds = new[] { "global_bonus", "player_bonus" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsTrue(resolution.HasConflicts, "Should detect conflicts");
            Assert.IsTrue(resolution.ResolvedRules.Contains("player_bonus"), "More specific rule should win");
            Assert.IsTrue(resolution.SupersededRules.Contains("global_bonus"), "Less specific rule should be superseded");
        }

        [Test]
        public void DetectConflicts_AdditiveBehavior_AllRulesApply()
        {
            // Arrange
            _resolver.RegisterRule("bonus1", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 10);
            _resolver.RegisterRule("bonus2", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 15);
            var ruleIds = new[] { "bonus1", "bonus2" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsTrue(resolution.HasConflicts, "Should detect conflicts");
            Assert.AreEqual(2, resolution.ResolvedRules.Count, "Both additive rules should apply");
            Assert.IsTrue(resolution.ResolvedRules.Contains("bonus1"), "First bonus should apply");
            Assert.IsTrue(resolution.ResolvedRules.Contains("bonus2"), "Second bonus should apply");
        }

        [Test]
        public void DetectConflicts_MultiplicativeBehavior_AllRulesApply()
        {
            // Arrange
            _resolver.RegisterRule("multiplier1", RuleConflictResolver.RuleScope.BuildingType, RuleConflictResolver.RuleBehavior.Multiplicative, 10);
            _resolver.RegisterRule("multiplier2", RuleConflictResolver.RuleScope.BuildingType, RuleConflictResolver.RuleBehavior.Multiplicative, 15);
            var ruleIds = new[] { "multiplier1", "multiplier2" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsTrue(resolution.HasConflicts, "Should detect conflicts");
            Assert.AreEqual(2, resolution.ResolvedRules.Count, "Both multiplicative rules should apply");
        }

        [Test]
        public void DetectConflicts_AbsoluteBehavior_HighestPriorityWins()
        {
            // Arrange
            _resolver.RegisterRule("absolute1", RuleConflictResolver.RuleScope.HexSpecific, RuleConflictResolver.RuleBehavior.Absolute, 10);
            _resolver.RegisterRule("absolute2", RuleConflictResolver.RuleScope.HexSpecific, RuleConflictResolver.RuleBehavior.Absolute, 20);
            var ruleIds = new[] { "absolute1", "absolute2" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsTrue(resolution.HasConflicts, "Should detect conflicts");
            Assert.AreEqual(1, resolution.ResolvedRules.Count, "Only one absolute rule should win");
            Assert.AreEqual(1, resolution.SupersededRules.Count, "One rule should be superseded");
        }

        [Test]
        public void DetectConflicts_DifferentBehaviors_NoConflict()
        {
            // Arrange
            _resolver.RegisterRule("additive_rule", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 10);
            _resolver.RegisterRule("override_rule", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Override, 15);
            var ruleIds = new[] { "additive_rule", "override_rule" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsFalse(resolution.HasConflicts, "Different behaviors should not conflict");
        }

        [Test]
        public void DetectConflicts_SignificantPriorityDifference_NoConflict()
        {
            // Arrange
            _resolver.RegisterRule("low_priority", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 10);
            _resolver.RegisterRule("high_priority", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 35);
            var ruleIds = new[] { "low_priority", "high_priority" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsFalse(resolution.HasConflicts, "Significant priority difference should prevent conflict");
        }

        [Test]
        public void GetEffectiveRules_ReturnsOnlyResolvedRules()
        {
            // Arrange
            _resolver.RegisterRule("global_rule", RuleConflictResolver.RuleScope.Global, RuleConflictResolver.RuleBehavior.Override, 10);
            _resolver.RegisterRule("specific_rule", RuleConflictResolver.RuleScope.PlayerSpecific, RuleConflictResolver.RuleBehavior.Override, 20);
            var ruleIds = new[] { "global_rule", "specific_rule" };

            // Act
            var effectiveRules = _resolver.GetEffectiveRules(ruleIds);

            // Assert
            Assert.AreEqual(1, effectiveRules.Count, "Should return only effective rules");
            Assert.AreEqual("specific_rule", effectiveRules[0], "More specific rule should be effective");
        }

        [Test]
        public void WouldConflict_WithConflictingRules_ReturnsTrue()
        {
            // Arrange
            _resolver.RegisterRule("existing_rule", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 10);
            var existingRules = new[] { "existing_rule" };

            // Act
            var wouldConflict = _resolver.WouldConflict("new_rule", existingRules);

            // Assert
            Assert.IsTrue(wouldConflict, "Should detect potential conflict");
        }

        [Test]
        public void WouldConflict_WithNonConflictingRules_ReturnsFalse()
        {
            // Arrange
            _resolver.RegisterRule("existing_rule", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 10);
            var existingRules = new[] { "existing_rule" };

            // Act
            var wouldConflict = _resolver.WouldConflict("different_behavior_rule", existingRules);

            // Assert
            Assert.IsFalse(wouldConflict, "Should not detect conflict with different behavior");
        }

        [Test]
        public void GetRuleInfo_NonExistentRule_ReturnsDefaults()
        {
            // Arrange
            const string nonExistentRule = "non_existent_rule";

            // Act
            var ruleInfo = _resolver.GetRuleInfo(nonExistentRule);

            // Assert
            Assert.AreEqual(nonExistentRule, ruleInfo["ruleId"]);
            Assert.AreEqual(RuleConflictResolver.RuleScope.Global, ruleInfo["scope"]);
            Assert.AreEqual(RuleConflictResolver.RuleBehavior.Additive, ruleInfo["behavior"]);
            Assert.AreEqual(10, ruleInfo["priority"]);
        }

        [Test]
        public void GetAllRules_ReturnsAllRegisteredRules()
        {
            // Arrange
            _resolver.RegisterRule("test_rule1", RuleConflictResolver.RuleScope.Global, RuleConflictResolver.RuleBehavior.Additive, 10);
            _resolver.RegisterRule("test_rule2", RuleConflictResolver.RuleScope.PlayerSpecific, RuleConflictResolver.RuleBehavior.Override, 20);

            // Act
            var allRules = _resolver.GetAllRules();

            // Assert
            Assert.IsTrue(allRules.Count >= 2, "Should have at least 2 rules");
            Assert.IsTrue(allRules.ContainsKey("test_rule1"), "Should contain test_rule1");
            Assert.IsTrue(allRules.ContainsKey("test_rule2"), "Should contain test_rule2");
        }

        [Test]
        public void ConflictResolution_ComplexScenario_ResolvesCorrectly()
        {
            // Arrange - Complex scenario with multiple conflicts
            _resolver.RegisterRule("global_production", RuleConflictResolver.RuleScope.Global, RuleConflictResolver.RuleBehavior.Additive, 5);
            _resolver.RegisterRule("resource_production", RuleConflictResolver.RuleScope.ResourceType, RuleConflictResolver.RuleBehavior.Additive, 15);
            _resolver.RegisterRule("building_production", RuleConflictResolver.RuleScope.BuildingType, RuleConflictResolver.RuleBehavior.Multiplicative, 25);
            _resolver.RegisterRule("player_bonus", RuleConflictResolver.RuleScope.PlayerSpecific, RuleConflictResolver.RuleBehavior.Override, 35);
            _resolver.RegisterRule("hex_modifier", RuleConflictResolver.RuleScope.HexSpecific, RuleConflictResolver.RuleBehavior.Absolute, 45);

            var ruleIds = new[] { "global_production", "resource_production", "building_production", "player_bonus", "hex_modifier" };

            // Act
            var resolution = _resolver.DetectConflicts(ruleIds);

            // Assert
            Assert.IsTrue(resolution.HasConflicts, "Should detect conflicts in complex scenario");
            
            // Override conflicts should be resolved by specificity
            Assert.IsTrue(resolution.ResolvedRules.Contains("hex_modifier"), "Most specific rule should win");
            Assert.IsTrue(resolution.ResolvedRules.Contains("player_bonus"), "Player-specific rule should win");
            
            // Additive and multiplicative should all apply
            Assert.IsTrue(resolution.ResolvedRules.Contains("global_production"), "Global additive should apply");
            Assert.IsTrue(resolution.ResolvedRules.Contains("resource_production"), "Resource additive should apply");
            Assert.IsTrue(resolution.ResolvedRules.Contains("building_production"), "Building multiplicative should apply");
        }

        [Test]
        public void ConflictResolution_EmptyRuleList_ReturnsEmptyResolution()
        {
            // Arrange
            var emptyRules = new string[0];

            // Act
            var resolution = _resolver.DetectConflicts(emptyRules);

            // Assert
            Assert.IsFalse(resolution.HasConflicts, "Empty list should have no conflicts");
            Assert.AreEqual(0, resolution.ResolvedRules.Count, "Empty list should have no resolved rules");
        }

        [Test]
        public void ConflictResolution_SingleRule_ReturnsSingleResolution()
        {
            // Arrange
            var singleRule = new[] { "resource_production" };

            // Act
            var resolution = _resolver.DetectConflicts(singleRule);

            // Assert
            Assert.IsFalse(resolution.HasConflicts, "Single rule should have no conflicts");
            Assert.AreEqual(0, resolution.ResolvedRules.Count, "Single rule should not be in resolved list");
        }

        [Test]
        public void RuleScope_Comparison_WorksCorrectly()
        {
            // Arrange
            var globalScope = RuleConflictResolver.RuleScope.Global;
            var playerScope = RuleConflictResolver.RuleScope.PlayerSpecific;
            var hexScope = RuleConflictResolver.RuleScope.HexSpecific;

            // Act & Assert
            Assert.IsTrue(hexScope > playerScope, "HexSpecific should be more specific than PlayerSpecific");
            Assert.IsTrue(playerScope > globalScope, "PlayerSpecific should be more specific than Global");
            Assert.IsTrue(hexScope > globalScope, "HexSpecific should be more specific than Global");
        }

        [Test]
        public void RuleBehavior_AllValues_AreValid()
        {
            // Arrange & Act
            var behaviors = Enum.GetValues(typeof(RuleConflictResolver.RuleBehavior));

            // Assert
            Assert.AreEqual(5, behaviors.Length, "Should have 5 rule behaviors");
            Assert.IsTrue(System.Enum.IsDefined(typeof(RuleConflictResolver.RuleBehavior), RuleConflictResolver.RuleBehavior.Additive));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RuleConflictResolver.RuleBehavior), RuleConflictResolver.RuleBehavior.Multiplicative));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RuleConflictResolver.RuleBehavior), RuleConflictResolver.RuleBehavior.Absolute));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RuleConflictResolver.RuleBehavior), RuleConflictResolver.RuleBehavior.Override));
            Assert.IsTrue(System.Enum.IsDefined(typeof(RuleConflictResolver.RuleBehavior), RuleConflictResolver.RuleBehavior.Conditional));
        }
    }
}
