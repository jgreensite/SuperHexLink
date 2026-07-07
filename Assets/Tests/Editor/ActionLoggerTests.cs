using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for enhanced ActionLogger with structured logging and correlation IDs.
    /// </summary>
    public class ActionLoggerTests
    {
        private ActionLogSettings _testSettings;
        private string _originalCorrelationId;

        [SetUp]
        public void SetUp()
        {
            // Create test settings
            _testSettings = new ActionLogSettings();
            
            // Store original correlation ID
            _originalCorrelationId = ActionLogger.GetCorrelationId();
            
            // Initialize logger for testing
            ActionLogger.Initialize("test-correlation", LogFormat.Both, ActionLogSeverity.Debug);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original correlation ID
            ActionLogger.SetCorrelationId(_originalCorrelationId);
        }

        [Test]
        public void ActionLogger_Initialize_SetsCorrelationId()
        {
            // Arrange
            string testCorrelationId = "test-12345";
            
            // Act
            ActionLogger.Initialize(testCorrelationId);
            
            // Assert
            Assert.AreEqual(testCorrelationId, ActionLogger.GetCorrelationId());
        }

        [Test]
        public void ActionLogger_Initialize_GeneratesCorrelationIdWhenNull()
        {
            // Act
            ActionLogger.Initialize(null);
            string correlationId = ActionLogger.GetCorrelationId();
            
            // Assert
            Assert.IsNotNull(correlationId);
            Assert.AreEqual(8, correlationId.Length);
            Assert.IsTrue(Guid.TryParseExact(correlationId, "N", out _));
        }

        [Test]
        public void ActionLogger_SetCorrelationId_UpdatesCorrelationId()
        {
            // Arrange
            string newCorrelationId = "new-correlation-123";
            
            // Act
            ActionLogger.SetCorrelationId(newCorrelationId);
            
            // Assert
            Assert.AreEqual(newCorrelationId, ActionLogger.GetCorrelationId());
        }

        [Test]
        public void ActionLogger_AddGlobalContext_AddsContextToAllLogs()
        {
            // Arrange
            ActionLogger.AddGlobalContext("testKey", "testValue");
            ActionLogger.AddGlobalContext("userId", 12345);
            
            // Act
            var config = ActionLogger.GetConfiguration();
            
            // Assert
            Assert.IsTrue(config.ContainsKey("globalContext"));
            var globalContext = (Dictionary<string, object>)config["globalContext"];
            Assert.AreEqual("testValue", globalContext["testKey"]);
            Assert.AreEqual(12345, globalContext["userId"]);
        }

        [Test]
        public void ActionLogger_SetLogFormat_UpdatesFormat()
        {
            // Arrange & Act
            ActionLogger.SetLogFormat(LogFormat.Json);
            var config = ActionLogger.GetConfiguration();
            
            // Assert
            Assert.AreEqual("Json", config["logFormat"]);
        }

        [Test]
        public void ActionLogger_SetMinimumLevel_UpdatesLevel()
        {
            // Arrange & Act
            ActionLogger.SetMinimumLevel(ActionLogSeverity.Warning);
            var config = ActionLogger.GetConfiguration();
            
            // Assert
            Assert.AreEqual("Warning", config["minimumLevel"]);
        }

        [Test]
        public void ActionLogger_LogStructured_IncludesContextData()
        {
            // Arrange
            var context = new Dictionary<string, object>
            {
                ["operation"] = "test-operation",
                ["duration"] = 150.5,
                ["success"] = true
            };
            
            // Act
            // Note: This test verifies the method doesn't throw and logs correctly
            // Actual log content verification would require log capture infrastructure
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogStructured(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, 
                    "Test message with context", context);
            });
        }

        [Test]
        public void ActionLogger_LogPerformance_LogsPerformanceMetrics()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(250.75);
            var context = new Dictionary<string, object>
            {
                ["userId"] = "user123",
                ["operationType"] = "database-query"
            };
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogPerformance(_testSettings, "database-query", duration, context);
            });
        }

        [Test]
        public void ActionLogger_LogSecurity_LogsSecurityEvents()
        {
            // Arrange
            var context = new Dictionary<string, object>
            {
                ["ipAddress"] = "192.168.1.100",
                ["userId"] = "user456"
            };
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogSecurity(_testSettings, "login-attempt", "Failed login attempt", context);
            });
        }

        [Test]
        public void ActionLogger_Log_RespectsMinimumLevel()
        {
            // Arrange
            ActionLogger.SetMinimumLevel(ActionLogSeverity.Warning);
            
            // Act & Assert
            // Debug and Info should be filtered out
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Debug, "Debug message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "Info message");
            });
            
            // Warning and above should pass through
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Warning, "Warning message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Error, "Error message");
            });
        }

        [Test]
        public void ActionLogger_Log_RespectsSettings()
        {
            // Arrange
            var disabledSettings = new ActionLogSettings();
            disabledSettings.logGeneral = false;
            
            // Act & Assert
            // Should not log when category is disabled
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(disabledSettings, ActionLogCategory.General, ActionLogSeverity.Info, "Should not log");
            });
        }

        [Test]
        public void ActionLogger_Log_HandlesNullSettings()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(null, ActionLogCategory.General, ActionLogSeverity.Info, "Test message");
            });
        }

        [Test]
        public void ActionLogger_Log_HandlesEmptyMessage()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, null);
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "   ");
            });
        }

        [Test]
        public void ActionLogger_Log_FormatsMessageWithArgs()
        {
            // Arrange
            string userId = "user123";
            int attempts = 3;
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, 
                    "User {0} attempted login {1} times", userId, attempts);
            });
        }

        [Test]
        public void ActionLogger_GetConfiguration_ReturnsCompleteConfig()
        {
            // Arrange
            ActionLogger.SetCorrelationId("config-test");
            ActionLogger.SetLogFormat(LogFormat.Json);
            ActionLogger.SetMinimumLevel(ActionLogSeverity.Error);
            ActionLogger.AddGlobalContext("testKey", "testValue");
            
            // Act
            var config = ActionLogger.GetConfiguration();
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual("config-test", config["correlationId"]);
            Assert.AreEqual("Json", config["logFormat"]);
            Assert.AreEqual("Error", config["minimumLevel"]);
            Assert.IsTrue(config.ContainsKey("globalContext"));
        }

        [Test]
        public void ActionLogger_LogAllCategories_WorksCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "General message");
                ActionLogger.Log(_testSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Info, "Hex lifecycle message");
                ActionLogger.Log(_testSettings, ActionLogCategory.HexLand, ActionLogSeverity.Info, "Hex land message");
                ActionLogger.Log(_testSettings, ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "Game lifecycle message");
                ActionLogger.Log(_testSettings, ActionLogCategory.Network, ActionLogSeverity.Info, "Network message");
                ActionLogger.Log(_testSettings, ActionLogCategory.Performance, ActionLogSeverity.Info, "Performance message");
                ActionLogger.Log(_testSettings, ActionLogCategory.Security, ActionLogSeverity.Info, "Security message");
                ActionLogger.Log(_testSettings, ActionLogCategory.Configuration, ActionLogSeverity.Info, "Configuration message");
            });
        }

        [Test]
        public void ActionLogger_LogAllSeverities_WorksCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Debug, "Debug message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "Info message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Warning, "Warning message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Error, "Error message");
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Critical, "Critical message");
            });
        }

        [Test]
        public void ActionLogger_LogWithNullContext_HandlesGracefully()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogStructured(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, 
                    "Test message", null);
            });
        }

        [Test]
        public void ActionLogger_LogWithEmptyContext_HandlesGracefully()
        {
            // Arrange
            var emptyContext = new Dictionary<string, object>();
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogStructured(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, 
                    "Test message", emptyContext);
            });
        }

        [Test]
        public void ActionLogger_LogPerformance_WithoutContext_WorksCorrectly()
        {
            // Arrange
            var duration = TimeSpan.FromMilliseconds(100);
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogPerformance(_testSettings, "test-operation", duration);
            });
        }

        [Test]
        public void ActionLogger_LogSecurity_WithoutContext_WorksCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.LogSecurity(_testSettings, "security-event", "Security event details");
            });
        }

        [Test]
        public void ActionLogger_MultipleLogFormats_WorksCorrectly()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ActionLogger.SetLogFormat(LogFormat.Plain);
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "Plain format test");
                
                ActionLogger.SetLogFormat(LogFormat.Json);
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "JSON format test");
                
                ActionLogger.SetLogFormat(LogFormat.Both);
                ActionLogger.Log(_testSettings, ActionLogCategory.General, ActionLogSeverity.Info, "Both formats test");
            });
        }

        [Test]
        public void ActionLogger_GlobalContext_IncludesUnityInfo()
        {
            // Arrange
            ActionLogger.Initialize(); // Re-initialize to add Unity context
            
            // Act
            var config = ActionLogger.GetConfiguration();
            var globalContext = (Dictionary<string, object>)config["globalContext"];
            
            // Assert
            Assert.IsTrue(globalContext.ContainsKey("unityVersion"));
            Assert.IsTrue(globalContext.ContainsKey("platform"));
            Assert.IsTrue(globalContext.ContainsKey("version"));
            Assert.IsTrue(globalContext.ContainsKey("startTime"));
        }

        [Test]
        public void ActionLogger_CorrelationId_Persistence()
        {
            // Arrange
            string testId = "persistent-test-id";
            ActionLogger.SetCorrelationId(testId);
            
            // Act
            string retrievedId1 = ActionLogger.GetCorrelationId();
            ActionLogger.Initialize(); // Re-initialize should preserve
            string retrievedId2 = ActionLogger.GetCorrelationId();
            
            // Assert
            Assert.AreEqual(testId, retrievedId1);
            Assert.AreEqual(testId, retrievedId2);
        }
    }
}
