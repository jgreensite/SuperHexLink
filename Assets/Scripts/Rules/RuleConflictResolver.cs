using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.Logging;

namespace SuperHexLink.Rules
{
    /// <summary>
    /// Implements rule conflict resolution with specific-beats-general override logic.
    /// Based on NotebookLM research: "Automated rule contradiction handling with specific-beats-general"
    /// </summary>
    public class RuleConflictResolver
    {
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<string, RuleScope> _ruleScopes;
        private readonly Dictionary<string, RuleBehavior> _ruleBehaviors;
        private readonly Dictionary<string, int> _rulePriorities;

        /// <summary>
        /// Rule scope determines specificity level for conflict resolution.
        /// More specific scopes override more general scopes.
        /// </summary>
        public enum RuleScope
        {
            Global = 1,      // Most general - applies to all game elements
            ResourceType = 2, // Specific to resource types
            BuildingType = 3, // Specific to building types  
            PlayerSpecific = 4, // Specific to individual players
            HexSpecific = 5,   // Specific to individual hexes
            InstanceSpecific = 6 // Most specific - applies to single instance
        }

        /// <summary>
        /// Rule behavior determines how rules interact when conflicts occur.
        /// </summary>
        public enum RuleBehavior
        {
            Additive,      // Effects stack (e.g., multiple production bonuses)
            Multiplicative, // Effects multiply (e.g., multiple multipliers)
            Absolute,      // Highest absolute value wins
            Override,      // More specific rule completely overrides general
            Conditional    // Rules apply based on conditions
        }

        /// <summary>
        /// Represents a rule conflict that needs resolution.
        /// </summary>
        public class RuleConflict
        {
            public string RuleId { get; set; }
            public string ConflictReason { get; set; }
            public RuleScope Scope { get; set; }
            public RuleBehavior Behavior { get; set; }
            public int Priority { get; set; }
            public object Context { get; set; }

            public RuleConflict(string ruleId, RuleScope scope, RuleBehavior behavior, int priority, object context = null)
            {
                RuleId = ruleId;
                Scope = scope;
                Behavior = behavior;
                Priority = priority;
                Context = context;
                ConflictReason = DetermineConflictReason();
            }

            private string DetermineConflictReason()
            {
                return $"Rule {RuleId} (Scope: {Scope}, Behavior: {Behavior}, Priority: {Priority}) conflicts with existing rules";
            }
        }

        /// <summary>
        /// Result of conflict resolution process.
        /// </summary>
        public class ConflictResolution
        {
            public bool HasConflicts { get; set; }
            public List<RuleConflict> Conflicts { get; set; }
            public List<string> ResolvedRules { get; set; }
            public List<string> SupersededRules { get; set; }
            public string ResolutionStrategy { get; set; }

            public ConflictResolution()
            {
                Conflicts = new List<RuleConflict>();
                ResolvedRules = new List<string>();
                SupersededRules = new List<string>();
            }
        }

        public RuleConflictResolver(ActionLogSettings logSettings)
        {
            _logSettings = logSettings;
            _ruleScopes = new Dictionary<string, RuleScope>();
            _ruleBehaviors = new Dictionary<string, RuleBehavior>();
            _rulePriorities = new Dictionary<string, int>();

            InitializeDefaultRules();
        }

        /// <summary>
        /// Initialize default rule scopes and behaviors for common game elements.
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Default resource rules are general
            _ruleScopes["resource_production"] = RuleScope.ResourceType;
            _ruleBehaviors["resource_production"] = RuleBehavior.Additive;
            _rulePriorities["resource_production"] = 10;

            // Building rules are more specific
            _ruleScopes["settlement_production"] = RuleScope.BuildingType;
            _ruleBehaviors["settlement_production"] = RuleBehavior.Multiplicative;
            _rulePriorities["settlement_production"] = 20;

            // Player-specific rules are very specific
            _ruleScopes["player_bonus"] = RuleScope.PlayerSpecific;
            _ruleBehaviors["player_bonus"] = RuleBehavior.Override;
            _rulePriorities["player_bonus"] = 30;

            // Hex-specific rules are most specific
            _ruleScopes["hex_modifier"] = RuleScope.HexSpecific;
            _ruleBehaviors["hex_modifier"] = RuleBehavior.Absolute;
            _rulePriorities["hex_modifier"] = 40;
        }

        /// <summary>
        /// Register a rule with its scope, behavior, and priority.
        /// </summary>
        public void RegisterRule(string ruleId, RuleScope scope, RuleBehavior behavior, int priority = 10)
        {
            _ruleScopes[ruleId] = scope;
            _ruleBehaviors[ruleId] = behavior;
            _rulePriorities[ruleId] = priority;

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Registered rule {0} with scope {1}, behavior {2}, priority {3}", 
                ruleId, scope, behavior, priority);
        }

        /// <summary>
        /// Detect conflicts between multiple rules.
        /// </summary>
        public ConflictResolution DetectConflicts(IEnumerable<string> ruleIds, object context = null)
        {
            var resolution = new ConflictResolution();
            var ruleList = ruleIds.ToList();

            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Detecting conflicts for {0} rules", ruleList.Count);

            // Check for conflicts between each pair of rules
            for (int i = 0; i < ruleList.Count; i++)
            {
                for (int j = i + 1; j < ruleList.Count; j++)
                {
                    var rule1 = ruleList[i];
                    var rule2 = ruleList[j];

                    if (AreRulesConflicting(rule1, rule2))
                    {
                        var conflict1 = new RuleConflict(rule1, 
                            _ruleScopes.GetValueOrDefault(rule1, RuleScope.Global),
                            _ruleBehaviors.GetValueOrDefault(rule1, RuleBehavior.Additive),
                            _rulePriorities.GetValueOrDefault(rule1, 10),
                            context);

                        var conflict2 = new RuleConflict(rule2,
                            _ruleScopes.GetValueOrDefault(rule2, RuleScope.Global),
                            _ruleBehaviors.GetValueOrDefault(rule2, RuleBehavior.Additive),
                            _rulePriorities.GetValueOrDefault(rule2, 10),
                            context);

                        resolution.Conflicts.Add(conflict1);
                        resolution.Conflicts.Add(conflict2);
                        resolution.HasConflicts = true;
                    }
                }
            }

            if (resolution.HasConflicts)
            {
                resolution = ResolveConflicts(resolution);
            }

            return resolution;
        }

        /// <summary>
        /// Check if two rules conflict with each other.
        /// </summary>
        private bool AreRulesConflicting(string rule1Id, string rule2Id)
        {
            var scope1 = _ruleScopes.GetValueOrDefault(rule1Id, RuleScope.Global);
            var scope2 = _ruleScopes.GetValueOrDefault(rule2Id, RuleScope.Global);
            var behavior1 = _ruleBehaviors.GetValueOrDefault(rule1Id, RuleBehavior.Additive);
            var behavior2 = _ruleBehaviors.GetValueOrDefault(rule2Id, RuleBehavior.Additive);

            // Rules with different behaviors don't conflict
            if (behavior1 != behavior2)
                return false;

            // Rules with significantly different priorities don't conflict
            var priority1 = _rulePriorities.GetValueOrDefault(rule1Id, 10);
            var priority2 = _rulePriorities.GetValueOrDefault(rule2Id, 10);
            if (Math.Abs(priority1 - priority2) > 20)
                return false;

            // Rules with same behavior and similar priorities may conflict
            // unless they're at different specificity levels
            if (scope1 == scope2 || (scope1 == RuleScope.Global && scope2 != RuleScope.Global))
                return true;

            return false;
        }

        /// <summary>
        /// Resolve detected conflicts using specific-beats-general logic.
        /// </summary>
        private ConflictResolution ResolveConflicts(ConflictResolution resolution)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Resolving {0} rule conflicts", resolution.Conflicts.Count);

            resolution.ResolutionStrategy = "specific-beats-general";

            // Group conflicts by behavior type
            var conflictsByBehavior = resolution.Conflicts
                .GroupBy(c => c.Behavior)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kvp in conflictsByBehavior)
            {
                var behavior = kvp.Key;
                var conflicts = kvp.Value;

                switch (behavior)
                {
                    case RuleBehavior.Override:
                        ResolveOverrideConflicts(conflicts, resolution);
                        break;
                    case RuleBehavior.Absolute:
                        ResolveAbsoluteConflicts(conflicts, resolution);
                        break;
                    case RuleBehavior.Additive:
                        ResolveAdditiveConflicts(conflicts, resolution);
                        break;
                    case RuleBehavior.Multiplicative:
                        ResolveMultiplicativeConflicts(conflicts, resolution);
                        break;
                    case RuleBehavior.Conditional:
                        ResolveConditionalConflicts(conflicts, resolution);
                        break;
                }
            }

            LogResolutionResults(resolution);
            return resolution;
        }

        /// <summary>
        /// Resolve override conflicts - most specific rule wins completely.
        /// </summary>
        private void ResolveOverrideConflicts(List<RuleConflict> conflicts, ConflictResolution resolution)
        {
            // Sort by scope specificity (more specific first), then by priority
            var sortedConflicts = conflicts.OrderByDescending(c => c.Scope)
                                          .ThenByDescending(c => c.Priority)
                                          .ToList();

            // The most specific rule wins, others are superseded
            if (sortedConflicts.Count > 0)
            {
                var winner = sortedConflicts.First();
                resolution.ResolvedRules.Add(winner.RuleId);

                for (int i = 1; i < sortedConflicts.Count; i++)
                {
                    resolution.SupersededRules.Add(sortedConflicts[i].RuleId);
                }
            }
        }

        /// <summary>
        /// Resolve absolute conflicts - highest absolute value wins.
        /// </summary>
        private void ResolveAbsoluteConflicts(List<RuleConflict> conflicts, ConflictResolution resolution)
        {
            // For absolute conflicts, we need to compare actual values
            // This is a simplified implementation - in practice, you'd need to evaluate the rule effects
            var sortedConflicts = conflicts.OrderByDescending(c => c.Priority)
                                          .ThenByDescending(c => c.Scope)
                                          .ToList();

            if (sortedConflicts.Count > 0)
            {
                var winner = sortedConflicts.First();
                resolution.ResolvedRules.Add(winner.RuleId);

                for (int i = 1; i < sortedConflicts.Count; i++)
                {
                    resolution.SupersededRules.Add(sortedConflicts[i].RuleId);
                }
            }
        }

        /// <summary>
        /// Resolve additive conflicts - all rules apply but may be combined.
        /// </summary>
        private void ResolveAdditiveConflicts(List<RuleConflict> conflicts, ConflictResolution resolution)
        {
            // For additive conflicts, all rules can apply - they just stack
            foreach (var conflict in conflicts)
            {
                resolution.ResolvedRules.Add(conflict.RuleId);
            }
        }

        /// <summary>
        /// Resolve multiplicative conflicts - all multipliers apply.
        /// </summary>
        private void ResolveMultiplicativeConflicts(List<RuleConflict> conflicts, ConflictResolution resolution)
        {
            // For multiplicative conflicts, all multipliers apply
            foreach (var conflict in conflicts)
            {
                resolution.ResolvedRules.Add(conflict.RuleId);
            }
        }

        /// <summary>
        /// Resolve conditional conflicts - evaluate conditions to determine active rules.
        /// </summary>
        private void ResolveConditionalConflicts(List<RuleConflict> conflicts, ConflictResolution resolution)
        {
            // For conditional conflicts, evaluate conditions to determine which rules apply
            // This is a simplified implementation - in practice, you'd need to evaluate actual conditions
            var sortedConflicts = conflicts.OrderByDescending(c => c.Priority)
                                          .ThenByDescending(c => c.Scope)
                                          .ToList();

            foreach (var conflict in sortedConflicts)
            {
                // Simplified: assume all conditions are met
                resolution.ResolvedRules.Add(conflict.RuleId);
            }
        }

        /// <summary>
        /// Get the effective rules after conflict resolution.
        /// </summary>
        public List<string> GetEffectiveRules(IEnumerable<string> ruleIds, object context = null)
        {
            var resolution = DetectConflicts(ruleIds, context);
            return resolution.ResolvedRules;
        }

        /// <summary>
        /// Check if a rule would conflict with existing rules.
        /// </summary>
        public bool WouldConflict(string ruleId, IEnumerable<string> existingRuleIds)
        {
            var testRules = existingRuleIds.Append(ruleId);
            var resolution = DetectConflicts(testRules);
            return resolution.HasConflicts;
        }

        /// <summary>
        /// Get rule information for debugging and analysis.
        /// </summary>
        public Dictionary<string, object> GetRuleInfo(string ruleId)
        {
            return new Dictionary<string, object>
            {
                ["ruleId"] = ruleId,
                ["scope"] = _ruleScopes.GetValueOrDefault(ruleId, RuleScope.Global),
                ["behavior"] = _ruleBehaviors.GetValueOrDefault(ruleId, RuleBehavior.Additive),
                ["priority"] = _rulePriorities.GetValueOrDefault(ruleId, 10)
            };
        }

        /// <summary>
        /// Get all registered rules for analysis.
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> GetAllRules()
        {
            var allRules = new Dictionary<string, Dictionary<string, object>>();
            
            foreach (var ruleId in _ruleScopes.Keys)
            {
                allRules[ruleId] = GetRuleInfo(ruleId);
            }

            return allRules;
        }

        /// <summary>
        /// Log conflict resolution results for debugging.
        /// </summary>
        private void LogResolutionResults(ConflictResolution resolution)
        {
            ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Info,
                "Conflict resolution complete: {0} resolved, {1} superseded",
                resolution.ResolvedRules.Count, resolution.SupersededRules.Count);

            if (resolution.ResolvedRules.Count > 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Debug,
                    "Resolved rules: {0}", string.Join(", ", resolution.ResolvedRules));
            }

            if (resolution.SupersededRules.Count > 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.General, ActionLogSeverity.Debug,
                    "Superseded rules: {0}", string.Join(", ", resolution.SupersededRules));
            }
        }
    }
}
