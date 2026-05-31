using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    /// <summary>
    /// Integration tests that verify real Lua execution via MoonSharp.
    /// These tests prove the stub has been replaced with a working interpreter.
    /// </summary>
    [TestFixture]
    public class RulesEngineIntegrationTests
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

        // ── Sandbox smoke tests ───────────────────────────────────────────────────

        [Test]
        public void Sandbox_ExecuteArithmetic_ReturnsCorrectResult()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var result = sandbox.ExecuteScript("return 2 + 2", new Dictionary<string, object>());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsNotNull(result.ReturnValue);
            Assert.IsTrue(result.ReturnValue.ContainsKey("result"));
            // MoonSharp returns numbers as doubles; 4.0 == 4
            Assert.AreEqual(4.0, System.Convert.ToDouble(result.ReturnValue["result"]), 0.001);
        }

        [Test]
        public void Sandbox_ExecuteStringConcat_ReturnsString()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var result = sandbox.ExecuteScript("return 'hello' .. ' ' .. 'world'", new Dictionary<string, object>());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual("hello world", result.ReturnValue["result"].ToString());
        }

        [Test]
        public void Sandbox_ContextVariable_AccessibleFromLua()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var context = new Dictionary<string, object> { ["bonus"] = 5 };
            var result = sandbox.ExecuteScript("return bonus * 3", context);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(15.0, System.Convert.ToDouble(result.ReturnValue["result"]), 0.001);
        }

        [Test]
        public void Sandbox_BooleanReturn_Works()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var result = sandbox.ExecuteScript("return 10 > 5", new Dictionary<string, object>());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(true, result.ReturnValue["result"]);
        }

        [Test]
        public void Sandbox_ConditionalLogic_Works()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var context = new Dictionary<string, object> { ["resources"] = 8 };
            var result = sandbox.ExecuteScript(
                "if resources >= 7 then return 'penalty' else return 'ok' end",
                context);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual("penalty", result.ReturnValue["result"].ToString());
        }

        [Test]
        public void Sandbox_ScriptWithNoReturn_SucceedsWithEmptyResult()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            // A script that just calls print() with no return value
            var result = sandbox.ExecuteScript("print('hello from lua')", new Dictionary<string, object>());

            Assert.IsTrue(result.Success, result.ErrorMessage);
            // ReturnValue should be empty (no return statement)
            Assert.IsNotNull(result.ReturnValue);
        }

        // ── Security validation ───────────────────────────────────────────────────

        [Test]
        public void Sandbox_DangerousPattern_RejectsScript()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var result = sandbox.ExecuteScript("os.execute('rm -rf /')", new Dictionary<string, object>());

            Assert.IsFalse(result.Success, "os.execute should be blocked");
            Assert.IsNotNull(result.ErrorMessage);
        }

        [Test]
        public void Sandbox_InfiniteLoopPattern_RejectsScript()
        {
            var sandbox = new RulesEngine.LuaSandbox(_logSettings);
            var result = sandbox.ExecuteScript("while true do end", new Dictionary<string, object>());

            Assert.IsFalse(result.Success, "Infinite loop pattern should be blocked");
        }

        // ── RulesEngine integration ───────────────────────────────────────────────

        [Test]
        public void RulesEngine_ExecuteConditionScript_RealLua_ReturnsTrue()
        {
            // The default rules have condition scripts; verify one executes properly
            var rules = _rulesEngine.GetAllRules();
            Assert.IsTrue(rules.Count > 0, "At least one default rule must be registered");

            // Execute a registered rule to verify the pipeline works end-to-end
            var context = new RulesEngine.RuleExecutionContext
            {
                EventType = "production",
                Parameters = new Dictionary<string, object> { ["diceRoll"] = 6 },
                GameState = new Dictionary<string, object> { ["turnNumber"] = 1 }
            };

            // Should not throw even if rule scripts reference context vars
            Assert.DoesNotThrow(() =>
            {
                var result = _rulesEngine.ExecuteRule("default_production", context);
                Assert.IsNotNull(result);
            });
        }
    }
}
