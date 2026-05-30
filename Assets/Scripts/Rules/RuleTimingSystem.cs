using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.CoreLogic;
using SuperHexLink.Logging;

namespace SuperHexLink.Rules
{
    /// <summary>
    /// Implements priority/timing stack system for rule resolution.
    /// Based on NotebookLM research: prevents rule trigger ambiguity with LIFO/FIFO stack and priority layers.
    /// </summary>
    public class RuleTimingSystem
    {
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<string, List<TimedRule>> _timingWindows;
        private readonly Stack<RuleExecution> _executionStack;
        private readonly Dictionary<string, int> _rulePriorities;

        // Timing window definitions
        public const string BEFORE_CALCULATION = "BeforeCalculation";
        public const string ON_CALCULATION = "OnCalculation";
        public const string AFTER_CALCULATION = "AfterCalculation";
        public const string BEFORE_RESOURCE_GAIN = "BeforeResourceGain";
        public const string ON_RESOURCE_GAIN = "OnResourceGain";
        public const string AFTER_RESOURCE_GAIN = "AfterResourceGain";
        public const string BEFORE_BUILDING = "BeforeBuilding";
        public const string ON_BUILDING = "OnBuilding";
        public const string AFTER_BUILDING = "AfterBuilding";

        public RuleTimingSystem(ActionLogSettings logSettings)
        {
            _logSettings = logSettings;
            _timingWindows = new Dictionary<string, List<TimedRule>>();
            _executionStack = new Stack<RuleExecution>();
            _rulePriorities = new Dictionary<string, int>();

            InitializeTimingWindows();
        }

        #region Timing Window Management

        /// <summary>
        /// Initialize all timing windows.
        /// </summary>
        private void InitializeTimingWindows()
        {
            var timingWindows = new[]
            {
                BEFORE_CALCULATION, ON_CALCULATION, AFTER_CALCULATION,
                BEFORE_RESOURCE_GAIN, ON_RESOURCE_GAIN, AFTER_RESOURCE_GAIN,
                BEFORE_BUILDING, ON_BUILDING, AFTER_BUILDING
            };

            foreach (var window in timingWindows)
            {
                _timingWindows[window] = new List<TimedRule>();
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Initialized {0} timing windows", timingWindows.Length);
        }

        /// <summary>
        /// Register a rule with timing and priority information.
        /// </summary>
        public void RegisterRule(TimedRule rule)
        {
            if (!_timingWindows.ContainsKey(rule.TimingWindow))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Unknown timing window: {0}", rule.TimingWindow);
                return;
            }

            _timingWindows[rule.TimingWindow].Add(rule);
            _rulePriorities[rule.RuleId] = rule.PriorityLayer;

            // Sort the timing window by priority (higher priority first)
            _timingWindows[rule.TimingWindow].Sort((a, b) => b.PriorityLayer.CompareTo(a.PriorityLayer));

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Registered rule {0} in timing window {1} with priority {2} (behavior: {3})",
                rule.RuleId, rule.TimingWindow, rule.PriorityLayer, rule.StackBehavior);
        }

        /// <summary>
        /// Unregister a rule from the timing system.
        /// </summary>
        public void UnregisterRule(string ruleId)
        {
            foreach (var timingWindow in _timingWindows.Values)
            {
                var rule = timingWindow.FirstOrDefault(r => r.RuleId == ruleId);
                if (rule != null)
                {
                    timingWindow.Remove(rule);
                    _rulePriorities.Remove(ruleId);
                    
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                        "Unregistered rule {0}", ruleId);
                    return;
                }
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                "Rule {0} not found for unregistration", ruleId);
        }

        #endregion

        #region Rule Execution

        /// <summary>
        /// Execute all rules in a specific timing window.
        /// </summary>
        public List<RuleExecutionResult> ExecuteTimingWindow(string timingWindow, RuleContext context)
        {
            var results = new List<RuleExecutionResult>();

            if (!_timingWindows.ContainsKey(timingWindow))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "Unknown timing window: {0}", timingWindow);
                return results;
            }

            var rules = _timingWindows[timingWindow];
            if (!rules.Any())
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                    "No rules registered for timing window: {0}", timingWindow);
                return results;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Executing {0} rules in timing window: {1}", rules.Count, timingWindow);

            switch (GetStackBehavior(rules))
            {
                case StackBehavior.LIFO:
                    results.AddRange(ExecuteLIFO(rules, context));
                    break;
                case StackBehavior.FIFO:
                    results.AddRange(ExecuteFIFO(rules, context));
                    break;
                case StackBehavior.Simultaneous:
                    results.AddRange(ExecuteSimultaneous(rules, context));
                    break;
            }

            return results;
        }

        /// <summary>
        /// Get the stack behavior for a set of rules.
        /// </summary>
        private StackBehavior GetStackBehavior(List<TimedRule> rules)
        {
            if (!rules.Any()) return StackBehavior.Simultaneous;

            // Use the highest priority rule's behavior
            var highestPriorityRule = rules.OrderByDescending(r => r.PriorityLayer).First();
            return highestPriorityRule.StackBehavior;
        }

        /// <summary>
        /// Execute rules using LIFO (Last-In, First-Out) stack behavior.
        /// </summary>
        private List<RuleExecutionResult> ExecuteLIFO(List<TimedRule> rules, RuleContext context)
        {
            var results = new List<RuleExecutionResult>();
            var stack = new Stack<TimedRule>(rules.OrderByDescending(r => r.PriorityLayer));

            while (stack.Count > 0)
            {
                var rule = stack.Pop();
                var result = ExecuteRule(rule, context);
                results.Add(result);

                // Check if this rule creates interrupts (adds more rules to the stack)
                if (result.CreatedInterrupts.Any())
                {
                    foreach (var interruptRule in result.CreatedInterrupts.OrderByDescending(r => r.PriorityLayer))
                    {
                        stack.Push(interruptRule);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Execute rules using FIFO (First-In, First-Out) queue behavior.
        /// </summary>
        private List<RuleExecutionResult> ExecuteFIFO(List<TimedRule> rules, RuleContext context)
        {
            var results = new List<RuleExecutionResult>();
            var queue = new Queue<TimedRule>(rules.OrderByDescending(r => r.PriorityLayer));

            while (queue.Count > 0)
            {
                var rule = queue.Dequeue();
                var result = ExecuteRule(rule, context);
                results.Add(result);

                // Add any new rules to the end of the queue
                foreach (var newRule in result.CreatedInterrupts.OrderByDescending(r => r.PriorityLayer))
                {
                    queue.Enqueue(newRule);
                }
            }

            return results;
        }

        /// <summary>
        /// Execute rules simultaneously (all at once).
        /// </summary>
        private List<RuleExecutionResult> ExecuteSimultaneous(List<TimedRule> rules, RuleContext context)
        {
            var results = new List<RuleExecutionResult>();
            var simultaneousContext = context.Clone();

            // Execute all rules with the same context
            foreach (var rule in rules)
            {
                var result = ExecuteRule(rule, simultaneousContext);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Execute a single rule.
        /// </summary>
        private RuleExecutionResult ExecuteRule(TimedRule rule, RuleContext context)
        {
            var execution = new RuleExecution
            {
                RuleId = rule.RuleId,
                TimingWindow = rule.TimingWindow,
                PriorityLayer = rule.PriorityLayer,
                Context = context,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Check if rule condition is met
                if (!EvaluateCondition(rule.ConditionScript, context))
                {
                    execution.Result = new RuleExecutionResult
                    {
                        RuleId = rule.RuleId,
                        Success = false,
                        Reason = "Condition not met",
                        ExecutionTime = DateTime.UtcNow - execution.StartTime
                    };

                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                        "Rule {0} skipped: condition not met", rule.RuleId);
                    return execution.Result;
                }

                // Execute rule action
                var actionResult = ExecuteAction(rule.ActionScript, context);
                
                execution.Result = new RuleExecutionResult
                {
                    RuleId = rule.RuleId,
                    Success = actionResult.Success,
                    Reason = actionResult.Reason,
                    ModifiedContext = actionResult.ModifiedContext,
                    CreatedInterrupts = actionResult.CreatedInterrupts,
                    ExecutionTime = DateTime.UtcNow - execution.StartTime
                };

                // Add to execution stack for tracking
                _executionStack.Push(execution);

                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                    "Rule {0} executed: {1} (took {2}ms)", 
                    rule.RuleId, execution.Result.Success ? "Success" : "Failed", 
                    execution.Result.ExecutionTime.TotalMilliseconds);

                return execution.Result;
            }
            catch (Exception ex)
            {
                execution.Result = new RuleExecutionResult
                {
                    RuleId = rule.RuleId,
                    Success = false,
                    Reason = $"Exception: {ex.Message}",
                    ExecutionTime = DateTime.UtcNow - execution.StartTime
                };

                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Error,
                    "Rule {0} failed with exception: {1}", rule.RuleId, ex.Message);

                return execution.Result;
            }
        }

        /// <summary>
        /// Evaluate rule condition (simplified for now).
        /// </summary>
        private bool EvaluateCondition(string conditionScript, RuleContext context)
        {
            // This would integrate with Lua scripting system
            // For now, return true as placeholder
            return true;
        }

        /// <summary>
        /// Execute rule action (simplified for now).
        /// </summary>
        private RuleActionResult ExecuteAction(string actionScript, RuleContext context)
        {
            // This would integrate with Lua scripting system
            // For now, return success as placeholder
            return new RuleActionResult
            {
                Success = true,
                Reason = "Action executed successfully",
                ModifiedContext = context,
                CreatedInterrupts = new List<TimedRule>()
            };
        }

        #endregion

        #region Timing Window Triggers

        /// <summary>
        /// Trigger resource gain timing windows.
        /// </summary>
        public List<RuleExecutionResult> TriggerResourceGain(ResourceGainContext resourceContext)
        {
            var context = new RuleContext
            {
                EventType = "ResourceGain",
                ResourceContext = resourceContext,
                Timestamp = DateTime.UtcNow
            };

            var allResults = new List<RuleExecutionResult>();

            // Execute in order: Before -> On -> After
            allResults.AddRange(ExecuteTimingWindow(BEFORE_RESOURCE_GAIN, context));
            allResults.AddRange(ExecuteTimingWindow(ON_RESOURCE_GAIN, context));
            allResults.AddRange(ExecuteTimingWindow(AFTER_RESOURCE_GAIN, context));

            return allResults;
        }

        /// <summary>
        /// Trigger building timing windows.
        /// </summary>
        public List<RuleExecutionResult> TriggerBuilding(BuildingContext buildingContext)
        {
            var context = new RuleContext
            {
                EventType = "Building",
                BuildingContext = buildingContext,
                Timestamp = DateTime.UtcNow
            };

            var allResults = new List<RuleExecutionResult>();

            // Execute in order: Before -> On -> After
            allResults.AddRange(ExecuteTimingWindow(BEFORE_BUILDING, context));
            allResults.AddRange(ExecuteTimingWindow(ON_BUILDING, context));
            allResults.AddRange(ExecuteTimingWindow(AFTER_BUILDING, context));

            return allResults;
        }

        /// <summary>
        /// Trigger calculation timing windows.
        /// </summary>
        public List<RuleExecutionResult> TriggerCalculation(CalculationContext calculationContext)
        {
            var context = new RuleContext
            {
                EventType = "Calculation",
                CalculationContext = calculationContext,
                Timestamp = DateTime.UtcNow
            };

            var allResults = new List<RuleExecutionResult>();

            // Execute in order: Before -> On -> After
            allResults.AddRange(ExecuteTimingWindow(BEFORE_CALCULATION, context));
            allResults.AddRange(ExecuteTimingWindow(ON_CALCULATION, context));
            allResults.AddRange(ExecuteTimingWindow(AFTER_CALCULATION, context));

            return allResults;
        }

        #endregion

        #region Rule Management

        /// <summary>
        /// Get all rules in a timing window.
        /// </summary>
        public List<TimedRule> GetRulesInTimingWindow(string timingWindow)
        {
            return _timingWindows.GetValueOrDefault(timingWindow, new List<TimedRule>());
        }

        /// <summary>
        /// Get rule priority.
        /// </summary>
        public int GetRulePriority(string ruleId)
        {
            return _rulePriorities.GetValueOrDefault(ruleId, 0);
        }

        /// <summary>
        /// Check for rule conflicts (same priority, same timing window).
        /// </summary>
        public List<RuleConflict> DetectRuleConflicts()
        {
            var conflicts = new List<RuleConflict>();

            foreach (var timingWindow in _timingWindows)
            {
                var rules = timingWindow.Value;
                var priorityGroups = rules.GroupBy(r => r.PriorityLayer);

                foreach (var group in priorityGroups.Where(g => g.Count() > 1))
                {
                    conflicts.Add(new RuleConflict
                    {
                        TimingWindow = timingWindow.Key,
                        PriorityLayer = group.Key,
                        ConflictingRules = group.Select(r => r.RuleId).ToList(),
                        ConflictType = ConflictType.SamePriority
                    });
                }
            }

            return conflicts;
        }

        /// <summary>
        /// Get execution history for debugging.
        /// </summary>
        public List<RuleExecution> GetExecutionHistory(int maxEntries = 50)
        {
            return _executionStack.Take(maxEntries).ToList();
        }

        /// <summary>
        /// Clear execution history.
        /// </summary>
        public void ClearExecutionHistory()
        {
            while (_executionStack.Count > 0)
            {
                _executionStack.Pop();
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Cleared execution history");
        }

        #endregion

        #region Statistics and Analysis

        /// <summary>
        /// Get timing system statistics.
        /// </summary>
        public RuleTimingStatistics GetStatistics()
        {
            var stats = new RuleTimingStatistics
            {
                TotalRules = _rulePriorities.Count,
                RulesPerTimingWindow = _timingWindows.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.Count),
                ExecutionHistorySize = _executionStack.Count,
                Conflicts = DetectRuleConflicts().Count
            };

            return stats;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A rule with timing and priority information.
    /// </summary>
    public class TimedRule
    {
        public string RuleId { get; set; }
        public string TimingWindow { get; set; }
        public int PriorityLayer { get; set; }  // 0-100, higher = more priority
        public StackBehavior StackBehavior { get; set; } = StackBehavior.Simultaneous;
        public string ConditionScript { get; set; }  // Lua script for condition
        public string ActionScript { get; set; }     // Lua script for action
        public string Expiration { get; set; } = "end_of_trigger";
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Context for rule execution.
    /// </summary>
    public class RuleContext
    {
        public string EventType { get; set; }
        public ResourceGainContext ResourceContext { get; set; }
        public BuildingContext BuildingContext { get; set; }
        public CalculationContext CalculationContext { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; }

        public RuleContext Clone()
        {
            return new RuleContext
            {
                EventType = EventType,
                ResourceContext = ResourceContext,
                BuildingContext = BuildingContext,
                CalculationContext = CalculationContext,
                CustomData = new Dictionary<string, object>(CustomData),
                Timestamp = Timestamp
            };
        }
    }

    /// <summary>
    /// Rule execution tracking.
    /// </summary>
    public class RuleExecution
    {
        public string RuleId { get; set; }
        public string TimingWindow { get; set; }
        public int PriorityLayer { get; set; }
        public RuleContext Context { get; set; }
        public RuleExecutionResult Result { get; set; }
        public DateTime StartTime { get; set; }
    }

    /// <summary>
    /// Result of rule execution.
    /// </summary>
    public class RuleExecutionResult
    {
        public string RuleId { get; set; }
        public bool Success { get; set; }
        public string Reason { get; set; }
        public RuleContext ModifiedContext { get; set; }
        public List<TimedRule> CreatedInterrupts { get; set; } = new List<TimedRule>();
        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Result of rule action execution.
    /// </summary>
    public class RuleActionResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; }
        public RuleContext ModifiedContext { get; set; }
        public List<TimedRule> CreatedInterrupts { get; set; } = new List<TimedRule>();
    }

    /// <summary>
    /// Context for resource gain events.
    /// </summary>
    public class ResourceGainContext
    {
        public int PlayerId { get; set; }
        public string ResourceType { get; set; }
        public int Amount { get; set; }
        public int SourceHexId { get; set; }
        public string SourceType { get; set; }  // "production", "trade", "bonus"
    }

    /// <summary>
    /// Context for building events.
    /// </summary>
    public class BuildingContext
    {
        public int PlayerId { get; set; }
        public string BuildingType { get; set; }
        public int LocationId { get; set; }
        public string LocationType { get; set; }  // "corner", "edge"
        public Dictionary<string, int> ResourceCost { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Context for calculation events.
    /// </summary>
    public class CalculationContext
    {
        public string CalculationType { get; set; }  // "victory_points", "resource_production", "trade_value"
        public Dictionary<string, object> InputParameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> OutputResults { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Rule conflict information.
    /// </summary>
    public class RuleConflict
    {
        public string TimingWindow { get; set; }
        public int PriorityLayer { get; set; }
        public List<string> ConflictingRules { get; set; } = new List<string>();
        public ConflictType ConflictType { get; set; }
    }

    /// <summary>
    /// Timing system statistics.
    /// </summary>
    public class RuleTimingStatistics
    {
        public int TotalRules { get; set; }
        public Dictionary<string, int> RulesPerTimingWindow { get; set; } = new Dictionary<string, int>();
        public int ExecutionHistorySize { get; set; }
        public int Conflicts { get; set; }
    }

    #endregion

    #region Enums

    public enum StackBehavior
    {
        LIFO,           // Last-In, First-Out (Interrupts/Instants)
        FIFO,           // First-In, First-Out (Queued triggers)
        Simultaneous    // Resolve at same time as other rules
    }

    public enum ConflictType
    {
        SamePriority,
        ContradictoryConditions,
        CircularDependency
    }

    #endregion
}
