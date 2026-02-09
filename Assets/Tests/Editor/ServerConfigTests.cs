using NUnit.Framework;
using UnityEngine;
using System.IO;
using System;
using SuperHexLink;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for ServerConfig loading, validation, and environment variable overrides.
    /// Validates the complete configuration management pipeline.
    /// </summary>
    public class ServerConfigTests
    {
        private const string TempConfigDir = "./Assets/StreamingAssets/";
        private const string TempConfigPath = TempConfigDir + "server-config.json";
        private string originalServerHost;
        private string originalServerPort;
        private string originalTimeout;
        private string originalRetries;
        private string originalLogging;

        [SetUp]
        public void SetUp()
        {
            // Backup original environment variables
            originalServerHost = Environment.GetEnvironmentVariable("SUPERHEX_SERVER_HOST");
            originalServerPort = Environment.GetEnvironmentVariable("SUPERHEX_SERVER_PORT");
            originalTimeout = Environment.GetEnvironmentVariable("SUPERHEX_CONNECTION_TIMEOUT");
            originalRetries = Environment.GetEnvironmentVariable("SUPERHEX_MAX_RETRIES");
            originalLogging = Environment.GetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING");

            // Clear environment variables for clean testing
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", null);
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", null);
            Environment.SetEnvironmentVariable("SUPERHEX_CONNECTION_TIMEOUT", null);
            Environment.SetEnvironmentVariable("SUPERHEX_MAX_RETRIES", null);
            Environment.SetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING", null);

            // Ensure StreamingAssets directory exists
            if (!Directory.Exists(TempConfigDir))
            {
                Directory.CreateDirectory(TempConfigDir);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Restore original environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", originalServerHost);
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", originalServerPort);
            Environment.SetEnvironmentVariable("SUPERHEX_CONNECTION_TIMEOUT", originalTimeout);
            Environment.SetEnvironmentVariable("SUPERHEX_MAX_RETRIES", originalRetries);
            Environment.SetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING", originalLogging);

            // Clean up test config file
            if (File.Exists(TempConfigPath))
            {
                File.Delete(TempConfigPath);
            }
        }

        [Test]
        public void Load_WithMissingConfigFile_ReturnsDefaults()
        {
            // Arrange: Ensure config file doesn't exist
            if (File.Exists(TempConfigPath))
            {
                File.Delete(TempConfigPath);
            }

            // Act
            var config = ServerConfig.Load();

            // Assert: Should return default values
            Assert.AreEqual("127.0.0.1", config.ServerHost, "Default host should be localhost");
            Assert.AreEqual(6321, config.ServerPort, "Default port should be 6321");
            Assert.AreEqual(5000, config.ConnectionTimeoutMs, "Default timeout should be 5000ms");
            Assert.AreEqual(3, config.MaxRetries, "Default max retries should be 3");
            Assert.IsTrue(config.EnableLogging, "Default logging should be enabled");
        }

        [Test]
        public void Load_WithValidConfigFile_LoadsConfiguration()
        {
            // Arrange: Create a valid config file
            string configJson = @"{
                ""serverHost"": ""192.168.1.100"",
                ""serverPort"": 8080,
                ""connectionTimeoutMs"": 10000,
                ""maxRetries"": 5,
                ""enableLogging"": false
            }";
            File.WriteAllText(TempConfigPath, configJson);

            // Act
            var config = ServerConfig.Load();

            // Assert: Should load values from config file
            Assert.AreEqual("192.168.1.100", config.ServerHost, "Should load host from config");
            Assert.AreEqual(8080, config.ServerPort, "Should load port from config");
            Assert.AreEqual(10000, config.ConnectionTimeoutMs, "Should load timeout from config");
            Assert.AreEqual(5, config.MaxRetries, "Should load max retries from config");
            Assert.IsFalse(config.EnableLogging, "Should load logging setting from config");
        }

        [Test]
        public void Load_WithEnvironmentVariables_OverridesConfigFile()
        {
            // Arrange: Create config file and set environment variables
            string configJson = @"{
                ""serverHost"": ""192.168.1.100"",
                ""serverPort"": 8080,
                ""connectionTimeoutMs"": 10000,
                ""maxRetries"": 5,
                ""enableLogging"": false
            }";
            File.WriteAllText(TempConfigPath, configJson);

            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "env.example.com");
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "9999");
            Environment.SetEnvironmentVariable("SUPERHEX_CONNECTION_TIMEOUT", "15000");
            Environment.SetEnvironmentVariable("SUPERHEX_MAX_RETRIES", "7");
            Environment.SetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING", "true");

            // Act
            var config = ServerConfig.Load();

            // Assert: Environment variables should override config file
            Assert.AreEqual("env.example.com", config.ServerHost, "Environment host should override config");
            Assert.AreEqual(9999, config.ServerPort, "Environment port should override config");
            Assert.AreEqual(15000, config.ConnectionTimeoutMs, "Environment timeout should override config");
            Assert.AreEqual(7, config.MaxRetries, "Environment retries should override config");
            Assert.IsTrue(config.EnableLogging, "Environment logging should override config");
        }

        [Test]
        public void Load_WithInvalidPort_ValidatesToDefault()
        {
            // Arrange: Config with invalid port
            string configJson = @"{
                ""serverHost"": ""127.0.0.1"",
                ""serverPort"": 70000,
                ""connectionTimeoutMs"": 5000,
                ""maxRetries"": 3,
                ""enableLogging"": true
            }";
            File.WriteAllText(TempConfigPath, configJson);

            // Act
            var config = ServerConfig.Load();

            // Assert: Invalid port should be corrected to default
            Assert.AreEqual(6321, config.ServerPort, "Invalid port should be corrected to default");
        }

        [Test]
        public void Load_WithInvalidTimeout_ValidatesToDefault()
        {
            // Arrange: Config with invalid timeout
            string configJson = @"{
                ""serverHost"": ""127.0.0.1"",
                ""serverPort"": 6321,
                ""connectionTimeoutMs"": 50000,
                ""maxRetries"": 3,
                ""enableLogging"": true
            }";
            File.WriteAllText(TempConfigPath, configJson);

            // Act
            var config = ServerConfig.Load();

            // Assert: Invalid timeout should be corrected to default
            Assert.AreEqual(5000, config.ConnectionTimeoutMs, "Invalid timeout should be corrected to default");
        }

        [Test]
        public void Load_WithInvalidHost_ValidatesToDefault()
        {
            // Arrange: Config with empty host
            string configJson = @"{
                ""serverHost"": """",
                ""serverPort"": 6321,
                ""connectionTimeoutMs"": 5000,
                ""maxRetries"": 3,
                ""enableLogging"": true
            }";
            File.WriteAllText(TempConfigPath, configJson);

            // Act
            var config = ServerConfig.Load();

            // Assert: Empty host should be corrected to default
            Assert.AreEqual("127.0.0.1", config.ServerHost, "Empty host should be corrected to default");
        }

        [Test]
        public void Load_WithMalformedJson_FallsBackToDefaults()
        {
            // Arrange: Malformed JSON
            string malformedJson = @"{ ""serverHost"": ""127.0.0.1"", ""serverPort"": }";
            File.WriteAllText(TempConfigPath, malformedJson);

            // Act
            var config = ServerConfig.Load();

            // Assert: Should fall back to defaults
            Assert.AreEqual("127.0.0.1", config.ServerHost, "Malformed JSON should fall back to defaults");
            Assert.AreEqual(6321, config.ServerPort, "Malformed JSON should fall back to defaults");
        }

        [Test]
        public void CreateDefault_ReturnsValidConfiguration()
        {
            // Act
            var config = ServerConfig.CreateDefault();

            // Assert: Should return valid default configuration
            Assert.IsNotNull(config, "CreateDefault should not return null");
            Assert.AreEqual("127.0.0.1", config.ServerHost, "Default host should be localhost");
            Assert.AreEqual(6321, config.ServerPort, "Default port should be 6321");
            Assert.AreEqual(5000, config.ConnectionTimeoutMs, "Default timeout should be 5000ms");
            Assert.AreEqual(3, config.MaxRetries, "Default max retries should be 3");
            Assert.IsTrue(config.EnableLogging, "Default logging should be enabled");
        }

        [Test]
        public void GameConstants_GetServerConfig_ReturnsLoadedConfiguration()
        {
            // Arrange: Set up environment variable
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "test.example.com");

            // Act
            var config = GameConstants.GetServerConfig();

            // Assert: Should return loaded configuration
            Assert.IsNotNull(config, "GetServerConfig should not return null");
            Assert.AreEqual("test.example.com", config.ServerHost, "Should return environment override");
        }

        [Test]
        public void GetEnvironmentVariables_ReturnsAllVariables()
        {
            // Arrange: Set some environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "env.test.com");
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "9999");

            // Act
            var envVars = ServerConfig.GetEnvironmentVariables();

            // Assert: Should return all environment variables
            Assert.AreEqual(5, envVars.Count, "Should return 5 environment variables");
            Assert.AreEqual("env.test.com", envVars["SUPERHEX_SERVER_HOST"], "Should return host value");
            Assert.AreEqual("9999", envVars["SUPERHEX_SERVER_PORT"], "Should return port value");
            Assert.AreEqual("not set", envVars["SUPERHEX_CONNECTION_TIMEOUT"], "Should show 'not set' for missing vars");
        }

        [Test]
        public void LogEnvironmentVariables_DoesNotThrow()
        {
            // Arrange: Set up environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "log.test.com");

            // Act & Assert: Should not throw exception
            Assert.DoesNotThrow(() => ServerConfig.LogEnvironmentVariables(), 
                "LogEnvironmentVariables should not throw exception");
        }

        [Test]
        public void Load_WithInvalidEnvironmentValues_LogsWarningsAndUsesDefaults()
        {
            // Arrange: Set invalid environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "invalid_port");
            Environment.SetEnvironmentVariable("SUPERHEX_CONNECTION_TIMEOUT", "not_a_number");
            Environment.SetEnvironmentVariable("SUPERHEX_MAX_RETRIES", "abc");
            Environment.SetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING", "maybe");

            // Act
            var config = ServerConfig.Load();

            // Assert: Should use config file or default values for invalid env vars
            Assert.AreEqual(6321, config.ServerPort, "Should use default port for invalid env var");
            Assert.AreEqual(5000, config.ConnectionTimeoutMs, "Should use default timeout for invalid env var");
            Assert.AreEqual(3, config.MaxRetries, "Should use default retries for invalid env var");
            Assert.IsTrue(config.EnableLogging, "Should use default logging for invalid env var");
        }

        [Test]
        public void Load_WithPartialEnvironmentVariables_AppliesOnlyValidOnes()
        {
            // Arrange: Create config file and set partial environment variables
            string configJson = @"{
                ""serverHost"": ""config.example.com"",
                ""serverPort"": 8080,
                ""connectionTimeoutMs"": 10000,
                ""maxRetries"": 5,
                ""enableLogging"": false
            }";
            File.WriteAllText(TempConfigPath, configJson);

            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "env.example.com");
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "9999");
            // Leave other env vars unset

            // Act
            var config = ServerConfig.Load();

            // Assert: Should apply only valid environment variable overrides
            Assert.AreEqual("env.example.com", config.ServerHost, "Should apply host override");
            Assert.AreEqual(9999, config.ServerPort, "Should apply port override");
            Assert.AreEqual(10000, config.ConnectionTimeoutMs, "Should keep config timeout when env not set");
            Assert.AreEqual(5, config.MaxRetries, "Should keep config retries when env not set");
            Assert.IsFalse(config.EnableLogging, "Should keep config logging when env not set");
        }

        [Test]
        public void Load_WithWhitespaceInEnvironmentVariable_TrimsValue()
        {
            // Arrange: Set environment variable with whitespace
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "  trimmed.example.com  ");

            // Act
            var config = ServerConfig.Load();

            // Assert: Should trim whitespace from environment variable
            Assert.AreEqual("trimmed.example.com", config.ServerHost, "Should trim whitespace from host");
        }
    }
}
