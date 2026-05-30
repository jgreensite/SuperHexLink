using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Unit tests for RulesEngine functionality.
    /// Tests Lua scripting, rule registration, conflict resolution, and execution.
    /// </summary>
    [TestFixture]
    public class RulesEngineTests
    {
        private RulesEngine _rulesEngine;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings
            {
                IsEnabled = (category, severity) => true,
                LogLevel = ActionLogSeverity.Info
            };
            _rulesEngine = new RulesEngine(_logSettings);
        }

        [Test]
        public void Constructor_InitializesWithDefaultRules()
        {
            // Act
            var allRules = _rulesEngine.GetAllRules();

            // Assert
            Assert.IsTrue(allRules.Count > 0, "Should have default rules initialized");
            Assert.IsTrue(allRules.ContainsKey("default_production"), "Should have default_production rule");
            Assert.IsTrue(allRules.ContainsKey("settlement_bonus"), "Should have settlement_bonus rule");
            Assert.IsTrue(allRules.ContainsKey("city_bonus"), "Should have city_bonus rule");
        }

        [Test]
        public void RegisterRule_ValidRule_ReturnsTrue()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "test_rule",
                Name = "Test Rule",
                Description = "A test rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { result = 'test' }",
                Priority = 15
            };

            // Act
            var result = _rulesEngine.RegisterRule(rule);

            // Assert
            Assert.IsTrue(result, "Should successfully register valid rule");
            Assert.IsNotNull(_rulesEngine.GetRule("test_rule"), "Rule should be retrievable");
        }

        [Test]
        public void RegisterRule_InvalidConditionScript_ReturnsFalse()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "invalid_rule",
                Name = "Invalid Rule",
                Description = "A rule with invalid condition",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "invalid lua syntax here",
                ActionScript = "return { result = 'test' }"
            };

            // Act
            var result = _rulesEngine.RegisterRule(rule);

            // Assert
            Assert.IsFalse(result, "Should fail to register rule with invalid condition");
            Assert.IsNull(_rulesEngine.GetRule("invalid_rule"), "Invalid rule should not be registered");
        }

        [Test]
        public void RegisterRule_InvalidActionScript_ReturnsFalse()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "invalid_action_rule",
                Name = "Invalid Action Rule",
                Description = "A rule with invalid action",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "invalid lua syntax here"
            };

            // Act
            var result = _rulesEngine.RegisterRule(rule);

            // Assert
            Assert.IsFalse(result, "Should fail to register rule with invalid action");
            Assert.IsNull(_rulesEngine.GetRule("invalid_action_rule"), "Invalid rule should not be registered");
        }

        [Test]
        public void RegisterRule_DuplicateId_ReturnsFalse()
        {
            // Arrange
            var rule1 = new RulesEngine.RegisteredRule
            {
                Id = "duplicate_rule",
                Name = "First Rule",
                Description = "First rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { result = 'first' }"
            };

            var rule2 = new RulesEngine.RegisteredRule
            {
                Id = "duplicate_rule",
                Name = "Second Rule",
                Description = "Second rule with same ID",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { result = 'second' }"
            };

            // Act
            var result1 = _rulesEngine.RegisterRule(rule1);
            var result2 = _rulesEngine.RegisterRule(rule2);

            // Assert
            Assert.IsTrue(result1, "First registration should succeed");
            Assert.IsFalse(result2, "Duplicate registration should fail");
        }

        [Test]
        public void ExecuteRules_ApplicableRules_ExecutesSuccessfully()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "execution_test",
                Name = "Execution Test Rule",
                Description = "Test rule execution",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.resourceType == 'wood'",
                ActionScript = "return { amount = 5 }",
                Priority = 20
            };

            _rulesEngine.RegisterRule(rule);

            var parameters = new Dictionary<string, object>
            {
                ["resourceType"] = "wood",
                ["baseAmount"] = 1
            };

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            Assert.IsTrue(results.Count > 0, "Should execute rules");
            Assert.IsTrue(results.Any(r => r.RuleId == "execution_test"), "Should execute test rule");
            Assert.IsTrue(results.All(r => r.Success), "All rules should execute successfully");
        }

        [Test]
        public void ExecuteRules_NonApplicableRules_DoesNotExecute()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "non_applicable_rule",
                Name = "Non Applicable Rule",
                Description = "Rule that should not apply",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.resourceType == 'gold'",
                ActionScript = "return { amount = 10 }"
            };

            _rulesEngine.RegisterRule(rule);

            var parameters = new Dictionary<string, object>
            {
                ["resourceType"] = "wood"
            };

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            Assert.IsFalse(results.Any(r => r.RuleId == "non_applicable_rule"), 
                "Non-applicable rule should not execute");
        }

        [Test]
        public void ExecuteRules_MultipleRules_ExecutesInPriorityOrder()
        {
            // Arrange
            var lowPriorityRule = new RulesEngine.RegisteredRule
            {
                Id = "low_priority_rule",
                Name = "Low Priority Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 1 }",
                Priority = 5
            };

            var highPriorityRule = new RulesEngine.RegisteredRule
            {
                Id = "high_priority_rule",
                Name = "High Priority Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 10 }",
                Priority = 25
            };

            _rulesEngine.RegisterRule(lowPriorityRule);
            _rulesEngine.RegisterRule(highPriorityRule);

            var parameters = new Dictionary<string, object>();

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            Assert.AreEqual(2, results.Count, "Should execute both rules");
            
            // High priority rule should execute first (based on priority ordering)
            var highPriorityResult = results.FirstOrDefault(r => r.RuleId == "high_priority_rule");
            var lowPriorityResult = results.FirstOrDefault(r => r.RuleId == "low_priority_rule");
            
            Assert.IsNotNull(highPriorityResult, "High priority rule should execute");
            Assert.IsNotNull(lowPriorityResult, "Low priority rule should execute");
        }

        [Test]
        public void ExecuteRules_WithConflicts_ResolvesCorrectly()
        {
            // Arrange
            var rule1 = new RulesEngine.RegisteredRule
            {
                Id = "conflict_rule_1",
                Name = "Conflict Rule 1",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 5 }",
                Priority = 10,
                Scope = RuleConflictResolver.RuleScope.Global,
                Behavior = RuleConflictResolver.RuleBehavior.Override
            };

            var rule2 = new RulesEngine.RegisteredRule
            {
                Id = "conflict_rule_2",
                Name = "Conflict Rule 2",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 10 }",
                Priority = 20,
                Scope = RuleConflictResolver.RuleScope.PlayerSpecific,
                Behavior = RuleConflictResolver.RuleBehavior.Override
            };

            _rulesEngine.RegisterRule(rule1);
            _rulesEngine.RegisterRule(rule2);

            var parameters = new Dictionary<string, object>();

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            // More specific rule (player-specific) should win over global rule
            Assert.IsTrue(results.Any(r => r.RuleId == "conflict_rule_2"), 
                "More specific rule should be executed");
            Assert.IsFalse(results.Any(r => r.RuleId == "conflict_rule_1"), 
                "Less specific rule should be superseded");
        }

        [Test]
        public void SetRuleEnabled_DisabledRule_DoesNotExecute()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "disable_test_rule",
                Name = "Disable Test Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 5 }"
            };

            _rulesEngine.RegisterRule(rule);
            _rulesEngine.SetRuleEnabled("disable_test_rule", false);

            var parameters = new Dictionary<string, object>();

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            Assert.IsFalse(results.Any(r => r.RuleId == "disable_test_rule"), 
                "Disabled rule should not execute");
        }

        [Test]
        public void SetRuleEnabled_NonExistentRule_ReturnsFalse()
        {
            // Act
            var result = _rulesEngine.SetRuleEnabled("non_existent_rule", true);

            // Assert
            Assert.IsFalse(result, "Should return false for non-existent rule");
        }

        [Test]
        public void GetRule_ExistingRule_ReturnsRule()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "get_test_rule",
                Name = "Get Test Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 5 }"
            };

            _rulesEngine.RegisterRule(rule);

            // Act
            var retrievedRule = _rulesEngine.GetRule("get_test_rule");

            // Assert
            Assert.IsNotNull(retrievedRule, "Should retrieve existing rule");
            Assert.AreEqual("get_test_rule", retrievedRule.Id);
            Assert.AreEqual("Get Test Rule", retrievedRule.Name);
        }

        [Test]
        public void GetRule_NonExistentRule_ReturnsNull()
        {
            // Act
            var rule = _rulesEngine.GetRule("non_existent_rule");

            // Assert
            Assert.IsNull(rule, "Should return null for non-existent rule");
        }

        [Test]
        public void UnregisterRule_ExistingRule_RemovesRule()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "unregister_test_rule",
                Name = "Unregister Test Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 5 }"
            };

            _rulesEngine.RegisterRule(rule);

            // Act
            var result = _rulesEngine.UnregisterRule("unregister_test_rule");

            // Assert
            Assert.IsTrue(result, "Should successfully unregister rule");
            Assert.IsNull(_rulesEngine.GetRule("unregister_test_rule"), "Rule should be removed");
        }

        [Test]
        public void UnregisterRule_NonExistentRule_ReturnsFalse()
        {
            // Act
            var result = _rulesEngine.UnregisterRule("non_existent_rule");

            // Assert
            Assert.IsFalse(result, "Should return false for non-existent rule");
        }

        [Test]
        public void GetAllRules_ReturnsAllRegisteredRules()
        {
            // Arrange
            var initialCount = _rulesEngine.GetAllRules().Count;

            var rule1 = new RulesEngine.RegisteredRule
            {
                Id = "all_rules_test_1",
                Name = "All Rules Test 1",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 1 }"
            };

            var rule2 = new RulesEngine.RegisteredRule
            {
                Id = "all_rules_test_2",
                Name = "All Rules Test 2",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 2 }"
            };

            _rulesEngine.RegisterRule(rule1);
            _rulesEngine.RegisterRule(rule2);

            // Act
            var allRules = _rulesEngine.GetAllRules();

            // Assert
            Assert.AreEqual(initialCount + 2, allRules.Count, "Should have all registered rules");
            Assert.IsTrue(allRules.ContainsKey("all_rules_test_1"), "Should contain first test rule");
            Assert.IsTrue(allRules.ContainsKey("all_rules_test_2"), "Should contain second test rule");
        }

        [Test]
        public void GetStatistics_ReturnsCorrectStatistics()
        {
            // Arrange
            var rule1 = new RulesEngine.RegisteredRule
            {
                Id = "stats_test_1",
                Name = "Stats Test 1",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 1 }"
            };

            var rule2 = new RulesEngine.RegisteredRule
            {
                Id = "stats_test_2",
                Name = "Stats Test 2",
                TimingWindow = RuleTimingSystem.ON_BUILDING,
                ConditionScript = "return true",
                ActionScript = "return { amount = 2 }"
            };

            _rulesEngine.RegisterRule(rule1);
            _rulesEngine.RegisterRule(rule2);
            _rulesEngine.SetRuleEnabled("stats_test_2", false);

            // Act
            var stats = _rulesEngine.GetStatistics();

            // Assert
            Assert.IsTrue(stats.ContainsKey("totalRules"), "Should have total rules count");
            Assert.IsTrue(stats.ContainsKey("enabledRules"), "Should have enabled rules count");
            Assert.IsTrue(stats.ContainsKey("disabledRules"), "Should have disabled rules count");
            Assert.IsTrue(stats.ContainsKey("rulesByTimingWindow"), "Should have rules by timing window");
            Assert.IsTrue(stats.ContainsKey("rulesByScope"), "Should have rules by scope");
            Assert.IsTrue(stats.ContainsKey("rulesByBehavior"), "Should have rules by behavior");
        }

        [Test]
        public void ExecuteRules_EmptyParameters_HandlesGracefully()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "empty_params_rule",
                Name = "Empty Parameters Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "return { amount = 1 }"
            };

            _rulesEngine.RegisterRule(rule);
            var emptyParameters = new Dictionary<string, object>();

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, emptyParameters);

            // Assert
            Assert.IsTrue(results.Count > 0, "Should handle empty parameters gracefully");
        }

        [Test]
        public void ExecuteRules_ScriptError_ReturnsErrorResult()
        {
            // Arrange
            var rule = new RulesEngine.RegisteredRule
            {
                Id = "error_rule",
                Name = "Error Rule",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return true",
                ActionScript = "this will cause an error" // Invalid Lua
            };

            _rulesEngine.RegisterRule(rule);
            var parameters = new Dictionary<string, object>();

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            var errorResult = results.FirstOrDefault(r => r.RuleId == "error_rule");
            Assert.IsNotNull(errorResult, "Should have result for error rule");
            Assert.IsFalse(errorResult.Success, "Error rule should fail");
            Assert.IsNotNull(errorResult.ErrorMessage, "Should have error message");
        }

        [Test]
        public void ExecuteRules_ComplexScenario_HandlesMultipleInteractions()
        {
            // Arrange - Complex scenario with multiple rule types
            var productionRule = new RulesEngine.RegisteredRule
            {
                Id = "complex_production",
                Name = "Complex Production",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.resourceType == 'wood'",
                ActionScript = "return { amount = context.baseAmount or 1 }",
                Priority = 10,
                Scope = RuleConflictResolver.RuleScope.ResourceType,
                Behavior = RuleConflictResolver.RuleBehavior.Additive
            };

            var bonusRule = new RulesEngine.RegisteredRule
            {
                Id = "complex_bonus",
                Name = "Complex Bonus",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.buildingType == 'settlement'",
                ActionScript = "return { multiplier = 1.5 }",
                Priority = 20,
                Scope = RuleConflictResolver.RuleScope.BuildingType,
                Behavior = RuleConflictResolver.RuleBehavior.Multiplicative
            };

            _rulesEngine.RegisterRule(productionRule);
            _rulesEngine.RegisterRule(bonusRule);

            var parameters = new Dictionary<string, object>
            {
                ["resourceType"] = "wood",
                ["buildingType"] = "settlement",
                ["baseAmount"] = 2
            };

            // Act
            var results = _rulesEngine.ExecuteRules(RuleTimingSystem.ON_RESOURCE_GAIN, parameters);

            // Assert
            Assert.AreEqual(2, results.Count, "Should execute both applicable rules");
            Assert.IsTrue(results.All(r => r.Success), "Both rules should succeed");
            
            var productionResult = results.FirstOrDefault(r => r.RuleId == "complex_production");
            var bonusResult = results.FirstOrDefault(r => r.RuleId == "complex_bonus");
            
            Assert.IsNotNull(productionResult, "Production rule should execute");
            Assert.IsNotNull(bonusResult, "Bonus rule should execute");
        }
    }
}
