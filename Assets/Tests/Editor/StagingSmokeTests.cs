using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using SuperHexLink;

namespace Tests.Editor
{
    /// <summary>
    /// Smoke tests for staging deployment validation.
    /// Tests critical functionality after staging deployment.
    /// </summary>
    public class StagingSmokeTests
    {
        private const string StagingConfigPath = "./build/StandaloneWindows64/config/server-config.json";
        private const string StagingManifestPath = "./build/StandaloneWindows64/manifest.json";

        [SetUp]
        public void SetUp()
        {
            Debug.Log("=== Staging Smoke Tests Setup ===");
        }

        [Test]
        public void Staging_ConfigurationFile_ExistsAndValid()
        {
            // Arrange: Check if staging config file exists
            bool configExists = File.Exists(StagingConfigPath);
            
            // Assert: Configuration file should exist
            Assert.IsTrue(configExists, $"Staging configuration file should exist at {StagingConfigPath}");
            
            if (configExists)
            {
                string configContent = File.ReadAllText(StagingConfigPath);
                Assert.IsNotEmpty(configContent, "Configuration file should not be empty");
                Assert.IsTrue(configContent.Contains("staging.superhexlink.com"), "Config should contain staging host");
                Assert.IsTrue(configContent.Contains("6321"), "Config should contain staging port");
                Debug.Log("✅ Staging configuration file is valid");
            }
        }

        [Test]
        public void Staging_ManifestFile_ExistsAndValid()
        {
            // Arrange: Check if staging manifest file exists
            bool manifestExists = File.Exists(StagingManifestPath);
            
            // Assert: Manifest file should exist
            Assert.IsTrue(manifestExists, $"Staging manifest file should exist at {StagingManifestPath}");
            
            if (manifestExists)
            {
                string manifestContent = File.ReadAllText(StagingManifestPath);
                Assert.IsNotEmpty(manifestContent, "Manifest file should not be empty");
                Assert.IsTrue(manifestContent.Contains("\"environment\""), "Manifest should contain environment field");
                Assert.IsTrue(manifestContent.Contains("\"version\""), "Manifest should contain version field");
                Assert.IsTrue(manifestContent.Contains("staging"), "Manifest should indicate staging environment");
                Debug.Log("✅ Staging manifest file is valid");
            }
        }

        [Test]
        public void ServerConfig_LoadsStagingConfiguration()
        {
            // Arrange: Set up staging environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "staging.superhexlink.com");
            
            // Act: Load configuration
            var config = ServerConfig.Load();
            
            // Assert: Should load staging configuration
            Assert.IsNotNull(config, "ServerConfig should not be null");
            Assert.AreEqual("staging.superhexlink.com", config.ServerHost, "Should load staging host");
            Assert.AreEqual(6321, config.ServerPort, "Should load staging port");
            Assert.IsTrue(config.EnableLogging, "Should enable logging in staging");
            
            Debug.Log("✅ ServerConfig loads staging configuration correctly");
        }

        [Test]
        public void GameConstants_ReturnsStagingConfiguration()
        {
            // Arrange: Set up staging environment
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "staging.superhexlink.com");
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "6321");
            
            // Act: Get configuration through GameConstants
            var config = GameConstants.GetServerConfig();
            
            // Assert: Should return staging configuration
            Assert.IsNotNull(config, "GameConstants should return valid config");
            Assert.AreEqual("staging.superhexlink.com", config.ServerHost, "Should return staging host");
            Assert.AreEqual(6321, config.ServerPort, "Should return staging port");
            
            Debug.Log("✅ GameConstants returns staging configuration correctly");
        }

        [Test]
        public void Staging_BuildArtifacts_Present()
        {
            // Arrange: Check for build artifacts
            string buildDir = "./build/StandaloneWindows64";
            bool buildDirExists = Directory.Exists(buildDir);
            
            // Assert: Build directory should exist
            Assert.IsTrue(buildDirExists, $"Build directory should exist at {buildDir}");
            
            if (buildDirExists)
            {
                var files = Directory.GetFiles(buildDir, "*.exe");
                Assert.IsTrue(files.Length > 0, "Should have at least one executable file");
                
                foreach (var file in files)
                {
                    Debug.Log($"Found build artifact: {file}");
                }
                
                Debug.Log("✅ Staging build artifacts are present");
            }
        }

        [Test]
        public void Staging_ScriptingDefines_Configured()
        {
            // Arrange: Check for staging scripting defines
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            
            // Assert: Should include STAGING define
            Assert.IsTrue(currentDefines.Contains("STAGING"), "Should include STAGING scripting define");
            
            Debug.Log($"✅ Scripting defines configured: {currentDefines}");
        }

        [Test]
        public void Staging_PlayerSettings_Configured()
        {
            // Arrange: Check player settings
            string productName = PlayerSettings.productName;
            string companyName = PlayerSettings.companyName;
            string bundleVersion = PlayerSettings.bundleVersion;
            
            // Assert: Should have staging-specific settings
            Assert.IsTrue(productName.Contains("Staging"), $"Product name should indicate staging: {productName}");
            Assert.IsTrue(companyName.Contains("Staging"), $"Company name should indicate staging: {companyName}");
            Assert.IsNotEmpty(bundleVersion, "Bundle version should be set");
            
            Debug.Log($"✅ Player settings configured - Product: {productName}, Version: {bundleVersion}");
        }

        [Test, Ignore("Requires CoreLogic.Models - CoreLogic.State namespace does not exist")]
        public void Staging_CoreLogic_Functionality()
        {
            // Arrange: Test CoreLogic functionality
            try
            {
                var hexState = new CoreLogic.Models.HexState();
                hexState.Col = 1;
                hexState.Row = 2;

                // Basic sanity: values assigned
                Assert.AreEqual(1, hexState.Col);
                Assert.AreEqual(2, hexState.Row);
                Debug.Log("✅ CoreLogic functionality works correctly");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"CoreLogic functionality failed: {ex.Message}");
            }
        }

        [Test, Ignore("CoreLogic.Serialization.GameStateSerializer does not exist in the DLL")]
        public void Staging_SaveLoad_Functionality()
        {
            // Skipped: GameStateSerializer is not part of the CoreLogic DLL.
            Assert.Ignore("GameStateSerializer not available");
        }

        [Test]
        public void Staging_EnvironmentVariables_Work()
        {
            // Arrange: Set test environment variables
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_HOST", "test-staging.example.com");
            Environment.SetEnvironmentVariable("SUPERHEX_SERVER_PORT", "9999");
            Environment.SetEnvironmentVariable("SUPERHEX_ENABLE_LOGGING", "false");
            
            // Act: Load configuration
            var config = ServerConfig.Load();
            
            // Assert: Environment variables should work
            Assert.AreEqual("test-staging.example.com", config.ServerHost, "Should use env host");
            Assert.AreEqual(9999, config.ServerPort, "Should use env port");
            Assert.IsFalse(config.EnableLogging, "Should use env logging setting");
            
            Debug.Log("✅ Environment variables work correctly");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.Log("=== Staging Smoke Tests Completed ===");
        }
    }
}
