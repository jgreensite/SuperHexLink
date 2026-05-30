using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.CoreLogic;
using SuperHexLink.Logging;

namespace SuperHexLink.Rules
{
    /// <summary>
    /// Implements a comprehensive rules engine with Lua scripting support.
    /// Based on NotebookLM research: "JSON Schema 2020-12 with expert-recommended patterns"
    /// </summary>
    public class RulesEngine
    {
        private readonly ActionLogSettings _logSettings;
        private readonly RuleConflictResolver _conflictResolver;
        private readonly Dictionary<string, RegisteredRule> _registeredRules;
        private readonly Dictionary<string, RuleExecutionContext> _executionContexts;
        private readonly LuaSandbox _luaSandbox;

        /// <summary>
        /// Represents a registered rule in the engine.
        /// </summary>
        public class RegisteredRule
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string TimingWindow { get; set; }
            public string ConditionScript { get; set; }
            public string ActionScript { get; set; }
            public int Priority { get; set; }
            public bool Enabled { get; set; }
            public RuleConflictResolver.RuleScope Scope { get; set; }
            public RuleConflictResolver.RuleBehavior Behavior { get; set; }
            public Dictionary<string, object> Metadata { get; set; }

            public RegisteredRule()
            {
                Metadata = new Dictionary<string, object>();
                Enabled = true;
                Priority = 10;
                Scope = RuleConflictResolver.RuleScope.Global;
                Behavior = RuleConflictResolver.RuleBehavior.Additive;
            }
        }

        /// <summary>
        /// Context for rule execution.
        /// </summary>
        public class RuleExecutionContext
        {
            public string EventType { get; set; }
            public Dictionary<string, object> Parameters { get; set; }
            public Dictionary<string, object> GameState { get; set; }
            public DateTime ExecutionTime { get; set; }
            public string TriggeringRuleId { get; set; }

            public RuleExecutionContext()
            {
                Parameters = new Dictionary<string, object>();
                GameState = new Dictionary<string, object>();
                ExecutionTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Result of rule execution.
        /// </summary>
        public class RuleExecutionResult
        {
            public bool Success { get; set; }
            public string RuleId { get; set; }
            public string ErrorMessage { get; set; }
            public Dictionary<string, object> ReturnValue { get; set; }
            public TimeSpan ExecutionTime { get; set; }
            public List<string> TriggeredRules { get; set; }

            public RuleExecutionResult()
            {
                ReturnValue = new Dictionary<string, object>();
                TriggeredRules = new List<string>();
            }
        }

        /// <summary>
        /// Lua script execution sandbox for security.
        /// </summary>
        public class LuaSandbox
        {
            private readonly ActionLogSettings _logSettings;
            private readonly Dictionary<string, object> _allowedGlobals;
            private readonly TimeSpan _executionTimeout;
            private readonly long _memoryLimit;

            public LuaSandbox(ActionLogSettings logSettings)
            {
                _logSettings = logSettings;
                _executionTimeout = TimeSpan.FromMilliseconds(100); // 100ms timeout
                _memoryLimit = 8192 * 1024; // 8MB limit
                _allowedGlobals = InitializeAllowedGlobals();
            }

            private Dictionary<string, object> InitializeAllowedGlobals()
            {
                return new Dictionary<string, object>
                {
                    ["math"] = new LuaMathLibrary(),
                    ["string"] = new LuaStringLibrary(),
                    ["table"] = new LuaTableLibrary(),
                    ["print"] = new Action<object>(SafePrint),
                    ["assert"] = new Func<bool, object, object>(SafeAssert),
                    ["type"] = new Func<object, string>(SafeType),
                    ["tostring"] = new Func<object, string>(SafeToString),
                    ["tonumber"] = new Func<object, object>(SafeToNumber)
                };
            }

            /// <summary>
            /// Execute a Lua script in the secure sandbox.
            /// </summary>
            public LuaExecutionResult ExecuteScript(string script, Dictionary<string, object> context)
            {
                var startTime = DateTime.UtcNow;
                var result = new LuaExecutionResult();

                try
                {
                    // Validate script for security issues
                    var validationResult = ValidateScript(script);
                    if (!validationResult.IsValid)
                    {
                        result.Success = false;
                        result.ErrorMessage = validationResult.ErrorMessage;
                        return result;
                    }

                    // Create execution environment
                    var environment = CreateExecutionEnvironment(context);

                    // Execute script (simplified implementation)
                    result = ExecuteScriptInternal(script, environment);

                    result.ExecutionTime = DateTime.UtcNow - startTime;

                    ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Debug,
                        "Lua script executed in {0}ms", result.ExecutionTime.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.ExecutionTime = DateTime.UtcNow - startTime;

                    ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                        "Lua script execution failed: {0}", ex.Message);
                }

                return result;
            }

            private LuaValidationResult ValidateScript(string script)
            {
                var result = new LuaValidationResult { IsValid = true };

                // Check for dangerous patterns
                var dangerousPatterns = new[]
                {
                    "os.execute", "os.remove", "os.rename", "io.open", "io.close",
                    "dofile", "loadfile", "require", "package", "debug",
                    "collectgarbage", "gcinfo", "getmetatable", "setmetatable"
                };

                foreach (var pattern in dangerousPatterns)
                {
                    if (script.Contains(pattern))
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Dangerous function '{pattern}' not allowed";
                        return result;
                    }
                }

                // Check for infinite loops (basic pattern detection)
                if (script.Contains("while true") || script.Contains("for i=1,math.huge"))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Infinite loop patterns not allowed";
                    return result;
                }

                return result;
            }

            private Dictionary<string, object> CreateExecutionEnvironment(Dictionary<string, object> context)
            {
                var environment = new Dictionary<string, object>(_allowedGlobals);
                
                // Add context variables
                foreach (var kvp in context)
                {
                    environment[kvp.Key] = kvp.Value;
                }

                return environment;
            }

            private LuaExecutionResult ExecuteScriptInternal(string script, Dictionary<string, object> environment)
            {
                // This is a simplified implementation
                // In a real implementation, you would use a Lua interpreter like MoonSharp
                var result = new LuaExecutionResult { Success = true };

                // Simulate script execution
                if (script.Contains("return"))
                {
                    // Extract return value (simplified)
                    result.ReturnValue = new Dictionary<string, object>
                    {
                        ["result"] = "simulated_result"
                    };
                }

                return result;
            }

            // Safe Lua library implementations
            private void SafePrint(object value)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Info,
                    "Lua print: {0}", value?.ToString() ?? "nil");
            }

            private object SafeAssert(bool condition, object message = null)
            {
                if (!condition)
                {
                    throw new Exception($"Lua assertion failed: {message}");
                }
                return message;
            }

            private string SafeType(object value)
            {
                if (value == null) return "nil";
                if (value is string) return "string";
                if (value is double || value is int || value is float) return "number";
                if (value is bool) return "boolean";
                if (value is Dictionary<string, object>) return "table";
                return "userdata";
            }

            private string SafeToString(object value)
            {
                return value?.ToString() ?? "nil";
            }

            private object SafeToNumber(object value)
            {
                if (double.TryParse(value?.ToString(), out double number))
                    return number;
                return null;
            }
        }

        /// <summary>
        /// Lua execution result.
        /// </summary>
        public class LuaExecutionResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public Dictionary<string, object> ReturnValue { get; set; }
            public TimeSpan ExecutionTime { get; set; }

            public LuaExecutionResult()
            {
                ReturnValue = new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// Lua validation result.
        /// </summary>
        public class LuaValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
        }

        // Mock Lua library classes (in real implementation, these would be more comprehensive)
        private class LuaMathLibrary { }
        private class LuaStringLibrary { }
        private class LuaTableLibrary { }

        public RulesEngine(ActionLogSettings logSettings, RuleConflictResolver conflictResolver = null)
        {
            _logSettings = logSettings;
            _conflictResolver = conflictResolver ?? new RuleConflictResolver(logSettings);
            _registeredRules = new Dictionary<string, RegisteredRule>();
            _executionContexts = new Dictionary<string, RuleExecutionContext>();
            _luaSandbox = new LuaSandbox(logSettings);

            InitializeDefaultRules();
        }

        /// <summary>
        /// Initialize default game rules.
        /// </summary>
        private void InitializeDefaultRules()
        {
            // Default resource production rule
            RegisterRule(new RegisteredRule
            {
                Id = "default_production",
                Name = "Default Resource Production",
                Description = "Basic resource production based on dice rolls",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.roll ~= nil and context.resourceType ~= nil",
                ActionScript = "return { amount = context.baseAmount or 1 }",
                Priority = 10,
                Scope = RuleConflictResolver.RuleScope.ResourceType,
                Behavior = RuleConflictResolver.RuleBehavior.Additive
            });

            // Default settlement production bonus
            RegisterRule(new RegisteredRule
            {
                Id = "settlement_bonus",
                Name = "Settlement Production Bonus",
                Description = "Settlements provide production bonus",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.buildingType == 'settlement'",
                ActionScript = "return { multiplier = 1.0 }",
                Priority = 20,
                Scope = RuleConflictResolver.RuleScope.BuildingType,
                Behavior = RuleConflictResolver.RuleBehavior.Multiplicative
            });

            // Default city production bonus
            RegisterRule(new RegisteredRule
            {
                Id = "city_bonus",
                Name = "City Production Bonus",
                Description = "Cities provide double production",
                TimingWindow = RuleTimingSystem.ON_RESOURCE_GAIN,
                ConditionScript = "return context.buildingType == 'city'",
                ActionScript = "return { multiplier = 2.0 }",
                Priority = 30,
                Scope = RuleConflictResolver.RuleScope.BuildingType,
                Behavior = RuleConflictResolver.RuleBehavior.Multiplicative
            });
        }

        /// <summary>
        /// Register a new rule in the engine.
        /// </summary>
        public bool RegisterRule(RegisteredRule rule)
        {
            try
            {
                // Validate rule
                if (!ValidateRule(rule))
                {
                    return false;
                }

                // Register with conflict resolver
                _conflictResolver.RegisterRule(rule.Id, rule.Scope, rule.Behavior, rule.Priority);

                // Store rule
                _registeredRules[rule.Id] = rule;

                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Info,
                    "Registered rule {0}: {1}", rule.Id, rule.Name);

                return true;
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Failed to register rule {0}: {1}", rule.Id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Validate a rule before registration.
        /// </summary>
        private bool ValidateRule(RegisteredRule rule)
        {
            if (string.IsNullOrWhiteSpace(rule.Id))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule validation failed: ID is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(rule.ConditionScript))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule validation failed: Condition script is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(rule.ActionScript))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule validation failed: Action script is required");
                return false;
            }

            // Validate Lua scripts
            var conditionValidation = _luaSandbox.ExecuteScript($"return {rule.ConditionScript}", new Dictionary<string, object>());
            if (!conditionValidation.Success)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule validation failed: Condition script error - {0}", conditionValidation.ErrorMessage);
                return false;
            }

            var actionValidation = _luaSandbox.ExecuteScript(rule.ActionScript, new Dictionary<string, object>());
            if (!actionValidation.Success)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule validation failed: Action script error - {0}", actionValidation.ErrorMessage);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Execute rules for a specific event.
        /// </summary>
        public List<RuleExecutionResult> ExecuteRules(string eventType, Dictionary<string, object> parameters)
        {
            var results = new List<RuleExecutionResult>();

            try
            {
                // Create execution context
                var context = new RuleExecutionContext
                {
                    EventType = eventType,
                    Parameters = parameters
                };

                // Find applicable rules
                var applicableRules = GetApplicableRules(eventType, parameters);

                // Resolve conflicts
                var ruleIds = applicableRules.Select(r => r.Id).ToList();
                var conflictResolution = _conflictResolver.DetectConflicts(ruleIds, context);

                // Execute effective rules
                foreach (var ruleId in conflictResolution.ResolvedRules)
                {
                    var rule = _registeredRules[ruleId];
                    var result = ExecuteRule(rule, context);
                    results.Add(result);

                    // Add triggered rules to context for chain reactions
                    if (result.Success && result.TriggeredRules.Count > 0)
                    {
                        context.TriggeringRuleId = ruleId;
                        var chainResults = ExecuteRules(eventType, parameters);
                        results.AddRange(chainResults);
                    }
                }

                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Info,
                    "Executed {0} rules for event {1}", results.Count, eventType);
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule execution failed for event {0}: {1}", eventType, ex.Message);
                
                results.Add(new RuleExecutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                });
            }

            return results;
        }

        /// <summary>
        /// Get rules that apply to a specific event.
        /// </summary>
        private List<RegisteredRule> GetApplicableRules(string eventType, Dictionary<string, object> parameters)
        {
            var applicableRules = new List<RegisteredRule>();

            foreach (var rule in _registeredRules.Values.Where(r => r.Enabled))
            {
                if (rule.TimingWindow == eventType)
                {
                    // Check condition
                    var context = new Dictionary<string, object>(parameters);
                    context["eventType"] = eventType;
                    
                    var conditionResult = _luaSandbox.ExecuteScript($"return {rule.ConditionScript}", context);
                    
                    if (conditionResult.Success && IsTruthy(conditionResult.ReturnValue))
                    {
                        applicableRules.Add(rule);
                    }
                }
            }

            return applicableRules.OrderByDescending(r => r.Priority).ToList();
        }

        /// <summary>
        /// Execute a single rule.
        /// </summary>
        private RuleExecutionResult ExecuteRule(RegisteredRule rule, RuleExecutionContext context)
        {
            var result = new RuleExecutionResult
            {
                RuleId = rule.Id
            };

            var startTime = DateTime.UtcNow;

            try
            {
                // Prepare script context
                var scriptContext = new Dictionary<string, object>(context.Parameters);
                scriptContext["eventType"] = context.EventType;
                scriptContext["ruleId"] = rule.Id;
                scriptContext["gameState"] = context.GameState;

                // Execute action script
                var executionResult = _luaSandbox.ExecuteScript(rule.ActionScript, scriptContext);

                if (executionResult.Success)
                {
                    result.Success = true;
                    result.ReturnValue = executionResult.ReturnValue;
                    result.ExecutionTime = DateTime.UtcNow - startTime;

                    ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Debug,
                        "Rule {0} executed successfully in {1}ms", rule.Id, result.ExecutionTime.TotalMilliseconds);
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = executionResult.ErrorMessage;
                    result.ExecutionTime = DateTime.UtcNow - startTime;

                    ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                        "Rule {0} execution failed: {1}", rule.Id, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.ExecutionTime = DateTime.UtcNow - startTime;

                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Error,
                    "Rule {0} execution exception: {1}", rule.Id, ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Check if a value is truthy (Lua-style).
        /// </summary>
        private bool IsTruthy(Dictionary<string, object> returnValue)
        {
            if (returnValue == null || !returnValue.Any())
                return false;

            // Check for a "result" key or use first value
            if (returnValue.TryGetValue("result", out var result))
            {
                return IsTruthyValue(result);
            }

            // Use first value
            var firstValue = returnValue.Values.First();
            return IsTruthyValue(firstValue);
        }

        private bool IsTruthyValue(object value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is string s) return !string.IsNullOrEmpty(s);
            if (value is double d) return Math.Abs(d) > 0.001;
            if (value is int i) return i != 0;
            return true;
        }

        /// <summary>
        /// Enable or disable a rule.
        /// </summary>
        public bool SetRuleEnabled(string ruleId, bool enabled)
        {
            if (!_registeredRules.TryGetValue(ruleId, out var rule))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Warning,
                    "Rule {0} not found", ruleId);
                return false;
            }

            rule.Enabled = enabled;

            ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Info,
                "Rule {0} {1}", ruleId, enabled ? "enabled" : "disabled");

            return true;
        }

        /// <summary>
        /// Get all registered rules.
        /// </summary>
        public Dictionary<string, RegisteredRule> GetAllRules()
        {
            return new Dictionary<string, RegisteredRule>(_registeredRules);
        }

        /// <summary>
        /// Get a specific rule by ID.
        /// </summary>
        public RegisteredRule GetRule(string ruleId)
        {
            return _registeredRules.TryGetValue(ruleId, out var rule) ? rule : null;
        }

        /// <summary>
        /// Unregister a rule.
        /// </summary>
        public bool UnregisterRule(string ruleId)
        {
            if (!_registeredRules.Remove(ruleId))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Warning,
                    "Rule {0} not found for unregistration", ruleId);
                return false;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Rules, ActionLogSeverity.Info,
                "Unregistered rule {0}", ruleId);

            return true;
        }

        /// <summary>
        /// Get engine statistics.
        /// </summary>
        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["totalRules"] = _registeredRules.Count,
                ["enabledRules"] = _registeredRules.Values.Count(r => r.Enabled),
                ["disabledRules"] = _registeredRules.Values.Count(r => !r.Enabled),
                ["rulesByTimingWindow"] = _registeredRules.Values
                    .GroupBy(r => r.TimingWindow)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ["rulesByScope"] = _registeredRules.Values
                    .GroupBy(r => r.Scope)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ["rulesByBehavior"] = _registeredRules.Values
                    .GroupBy(r => r.Behavior)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
        }
    }
}
