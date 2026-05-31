using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;

namespace SuperHexLink.Build
{
    /// <summary>
    /// Unity build automation for staging deployments.
    /// Provides automated build configuration for staging environment.
    /// </summary>
    public static class StagingBuild
    {
        /// <summary>
        /// Perform staging build with appropriate configuration.
        /// Called from CI/CD pipeline via Unity build method.
        /// </summary>
        public static void PerformBuild()
        {
            Debug.Log("=== Starting Staging Build ===");
            
            // Configure build settings for staging
            ConfigureStagingEnvironment();
            
            // Set build options
            BuildPlayerOptions buildOptions = new BuildPlayerOptions();
            buildOptions.scenes = GetScenes();
            buildOptions.locationPathName = GetBuildPath();
            buildOptions.target = BuildTarget.StandaloneWindows64;
            buildOptions.options = BuildOptions.None;

            // Add staging-specific build options
            buildOptions.options |= BuildOptions.StrictMode;
            buildOptions.options |= BuildOptions.CompressWithLz4;
            
            Debug.Log($"Building for target: {buildOptions.target}");
            Debug.Log($"Output path: {buildOptions.locationPathName}");
            Debug.Log($"Scenes to build: {buildOptions.scenes.Length}");

            // Perform build
            BuildReport buildReport = BuildPipeline.BuildPlayer(buildOptions);
            
            // Handle build result
            if (buildReport.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ Staging build successful!");
                Debug.Log($"Build size: {buildReport.summary.totalSize} bytes");
                Debug.Log($"Build time: {buildReport.summary.totalTime}");
                
                // Create staging configuration
                CreateStagingConfig();
                
                // Generate build manifest
                GenerateBuildManifest(buildReport);
                
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"❌ Staging build failed!");
                Debug.LogError($"Result: {buildReport.summary.result}");
                
                // Log any errors
                foreach (var step in buildReport.steps)
                {
                    if (step.messages.Length > 0)
                    {
                        Debug.LogError($"Build step '{step.name}' errors:");
                        foreach (var message in step.messages)
                        {
                            if (message.type == LogType.Error || message.type == LogType.Exception)
                            {
                                Debug.LogError($"  {message.content}");
                            }
                        }
                    }
                }
                
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Configure Unity environment for staging build.
        /// </summary>
        private static void ConfigureStagingEnvironment()
        {
            Debug.Log("Configuring staging environment...");
            
            // Set scripting define symbols for staging
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, 
                "STAGING;UNITY_ASSERTIONS"
            );
            
            // Configure player settings for staging
            PlayerSettings.companyName = "SuperHexLink Staging";
            PlayerSettings.productName = "SuperHexLink (Staging)";
            PlayerSettings.bundleVersion = GetStagingVersion();
            
            // Ensure development build is enabled for debugging
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            
            // Configure quality settings for staging
            QualitySettings.SetQualityLevel(2, true); // Good quality for testing
            
            Debug.Log($"Staging version: {PlayerSettings.bundleVersion}");
            Debug.Log($"Scripting defines: {PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone)}");
        }

        /// <summary>
        /// Get all scenes that should be included in the build.
        /// </summary>
        private static string[] GetScenes()
        {
            // Find all scenes in the project
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            string[] scenes = new string[sceneGuids.Length];
            
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                scenes[i] = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                Debug.Log($"Including scene: {scenes[i]}");
            }
            
            return scenes;
        }

        /// <summary>
        /// Get the output path for the staging build.
        /// </summary>
        private static string GetBuildPath()
        {
            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "build", "StandaloneWindows64");
            Directory.CreateDirectory(buildDir);
            
            string exeName = "SuperHexLink-Staging.exe";
            return Path.Combine(buildDir, exeName);
        }

        /// <summary>
        /// Get staging version string based on commit and timestamp.
        /// </summary>
        private static string GetStagingVersion()
        {
            string commitHash = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local";
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmm");
            return $"staging-{timestamp}-{commitHash.Substring(0, 7)}";
        }

        /// <summary>
        /// Create staging configuration files.
        /// </summary>
        private static void CreateStagingConfig()
        {
            Debug.Log("Creating staging configuration...");
            
            string configDir = Path.Combine(Directory.GetCurrentDirectory(), "build", "StandaloneWindows64", "config");
            Directory.CreateDirectory(configDir);
            
            // Create staging server config
            string serverConfigPath = Path.Combine(configDir, "server-config.json");
            string serverConfig = @"{
  ""serverHost"": ""staging.superhexlink.com"",
  ""serverPort"": 6321,
  ""connectionTimeoutMs"": 5000,
  ""maxRetries"": 3,
  ""enableLogging"": true,
  ""_comment"": ""Staging environment configuration""
}";
            
            File.WriteAllText(serverConfigPath, serverConfig);
            Debug.Log($"Created staging config: {serverConfigPath}");
        }

        /// <summary>
        /// Generate build manifest for deployment tracking.
        /// </summary>
        private static void GenerateBuildManifest(BuildReport buildReport)
        {
            Debug.Log("Generating build manifest...");
            
            var manifest = new
            {
                version = PlayerSettings.bundleVersion,
                buildTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                unityVersion = Application.unityVersion,
                targetPlatform = buildReport.summary.platform.ToString(),
                buildSize = buildReport.summary.totalSize,
                buildDuration = buildReport.summary.totalTime.ToString(),
                commitHash = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
                branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "local",
                environment = "staging",
                scenes = GetScenes(),
                buildOptions = new
                {
                    development = EditorUserBuildSettings.development,
                    scriptDebugging = EditorUserBuildSettings.allowDebugging,
                    compression = "LZ4"
                }
            };
            
            string manifestPath = Path.Combine(Path.GetDirectoryName(GetBuildPath()), "manifest.json");
            string manifestJson = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(manifestPath, manifestJson);
            
            Debug.Log($"Build manifest created: {manifestPath}");
        }

        /// <summary>
        /// Validate build prerequisites.
        /// </summary>
        [MenuItem("Build/Validate Staging Build")]
        public static void ValidateStagingBuild()
        {
            Debug.Log("=== Validating Staging Build Prerequisites ===");
            
            bool isValid = true;
            
            // Check if Unity license is available
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogWarning("⚠️ Unity license may not be properly configured");
            }
            
            // Check if required scenes exist
            string[] scenes = GetScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("❌ No scenes found in project");
                isValid = false;
            }
            
            // Check build output directory permissions
            string buildPath = GetBuildPath();
            string buildDir = Path.GetDirectoryName(buildPath);
            try
            {
                Directory.CreateDirectory(buildDir);
                string testFile = Path.Combine(buildDir, "test.txt");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                Debug.Log("✅ Build directory is writable");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Build directory is not writable: {ex.Message}");
                isValid = false;
            }
            
            // Check scripting defines
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            if (!currentDefines.Contains("STAGING"))
            {
                Debug.LogWarning("⚠️ STAGING define symbol not set");
            }
            
            if (isValid)
            {
                Debug.Log("✅ Staging build validation passed");
            }
            else
            {
                Debug.LogError("❌ Staging build validation failed");
            }
        }
    }
}
