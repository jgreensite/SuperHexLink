using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SuperHexLink
{
    /// <summary>
    /// Runtime server configuration loaded from StreamingAssets with environment variable overrides.
    /// Provides secure, environment-specific configuration without hardcoded values.
    /// </summary>
    [Serializable]
    public class ServerConfig
    {
        private string serverHost = "127.0.0.1";
        private int serverPort = 6321;
        private int connectionTimeoutMs = 5000;
        private int maxRetries = 3;
        private bool enableLogging = true;

        public string ServerHost => serverHost;
        public int ServerPort => serverPort;
        public int ConnectionTimeoutMs => connectionTimeoutMs;
        public int MaxRetries => maxRetries;
        public bool EnableLogging => enableLogging;

        /// <summary>
        /// Load configuration from StreamingAssets/server-config.json with environment variable overrides.
        /// Falls back to localhost defaults if config file is missing or invalid.
        /// </summary>
        public static ServerConfig Load()
        {
            var config = new ServerConfig();
            
            // Try to load from StreamingAssets first
            try
            {
                string configPath = Path.Combine(Application.streamingAssetsPath, "server-config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonUtility.FromJson<ServerConfig>(json);
                    Debug.Log($"ServerConfig: Loaded from {configPath}");
                }
                else
                {
                    Debug.LogWarning($"ServerConfig: Config file not found at {configPath}, using defaults");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ServerConfig: Failed to load config file, using defaults. Error: {ex.Message}");
            }

            // Environment variable overrides (highest priority)
            bool hasEnvOverrides = false;
            
            // Server host override
            string hostEnv = GetEnvVar("SUPERHEX_SERVER_HOST", null);
            if (!string.IsNullOrEmpty(hostEnv))
            {
                config.serverHost = hostEnv.Trim();
                hasEnvOverrides = true;
                Debug.Log($"ServerConfig: Environment override - Host = {config.serverHost}");
            }
            
            // Server port override
            string portEnv = GetEnvVar("SUPERHEX_SERVER_PORT", null);
            if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int portOverride))
            {
                config.serverPort = portOverride;
                hasEnvOverrides = true;
                Debug.Log($"ServerConfig: Environment override - Port = {config.serverPort}");
            }
            else if (!string.IsNullOrEmpty(portEnv))
            {
                Debug.LogWarning($"ServerConfig: Invalid SUPERHEX_SERVER_PORT value '{portEnv}', using config file value");
            }

            // Connection timeout override
            string timeoutEnv = GetEnvVar("SUPERHEX_CONNECTION_TIMEOUT", null);
            if (!string.IsNullOrEmpty(timeoutEnv) && int.TryParse(timeoutEnv, out int timeoutOverride))
            {
                config.connectionTimeoutMs = timeoutOverride;
                hasEnvOverrides = true;
                Debug.Log($"ServerConfig: Environment override - Timeout = {config.connectionTimeoutMs}ms");
            }
            else if (!string.IsNullOrEmpty(timeoutEnv))
            {
                Debug.LogWarning($"ServerConfig: Invalid SUPERHEX_CONNECTION_TIMEOUT value '{timeoutEnv}', using config file value");
            }

            // Max retries override
            string retriesEnv = GetEnvVar("SUPERHEX_MAX_RETRIES", null);
            if (!string.IsNullOrEmpty(retriesEnv) && int.TryParse(retriesEnv, out int retriesOverride))
            {
                config.maxRetries = retriesOverride;
                hasEnvOverrides = true;
                Debug.Log($"ServerConfig: Environment override - MaxRetries = {config.maxRetries}");
            }
            else if (!string.IsNullOrEmpty(retriesEnv))
            {
                Debug.LogWarning($"ServerConfig: Invalid SUPERHEX_MAX_RETRIES value '{retriesEnv}', using config file value");
            }

            // Logging enable override
            string loggingEnv = GetEnvVar("SUPERHEX_ENABLE_LOGGING", null);
            if (!string.IsNullOrEmpty(loggingEnv) && bool.TryParse(loggingEnv, out bool loggingOverride))
            {
                config.enableLogging = loggingOverride;
                hasEnvOverrides = true;
                Debug.Log($"ServerConfig: Environment override - EnableLogging = {config.enableLogging}");
            }
            else if (!string.IsNullOrEmpty(loggingEnv))
            {
                Debug.LogWarning($"ServerConfig: Invalid SUPERHEX_ENABLE_LOGGING value '{loggingEnv}', using config file value");
            }

            if (hasEnvOverrides)
            {
                Debug.Log("ServerConfig: Environment variable overrides applied");
            }

            // Validate configuration
            config.Validate();

            Debug.Log($"ServerConfig: Final config - Host: {config.serverHost}, Port: {config.serverPort}");
            return config;
        }

        /// <summary>
        /// Get environment variable value with fallback to default.
        /// </summary>
        private static string GetEnvVar(string key, string defaultValue)
        {
            string value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        /// <summary>
        /// Get all available SuperHexLink environment variables for debugging.
        /// Returns a dictionary of variable names to their values (or "not set").
        /// </summary>
        public static System.Collections.Generic.Dictionary<string, string> GetEnvironmentVariables()
        {
            var envVars = new System.Collections.Generic.Dictionary<string, string>();
            
            envVars["SUPERHEX_SERVER_HOST"] = GetEnvVar("SUPERHEX_SERVER_HOST", "not set");
            envVars["SUPERHEX_SERVER_PORT"] = GetEnvVar("SUPERHEX_SERVER_PORT", "not set");
            envVars["SUPERHEX_CONNECTION_TIMEOUT"] = GetEnvVar("SUPERHEX_CONNECTION_TIMEOUT", "not set");
            envVars["SUPERHEX_MAX_RETRIES"] = GetEnvVar("SUPERHEX_MAX_RETRIES", "not set");
            envVars["SUPERHEX_ENABLE_LOGGING"] = GetEnvVar("SUPERHEX_ENABLE_LOGGING", "not set");
            
            return envVars;
        }

        /// <summary>
        /// Log all available environment variables for debugging purposes.
        /// </summary>
        public static void LogEnvironmentVariables()
        {
            Debug.Log("=== SuperHexLink Environment Variables ===");
            var envVars = GetEnvironmentVariables();
            foreach (var kvp in envVars)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
            Debug.Log("=========================================");
        }

        /// <summary>
        /// Validate configuration values and apply sensible defaults for invalid values.
        /// </summary>
        public void Validate()
        {
            // Validate host
            if (string.IsNullOrEmpty(serverHost))
            {
                Debug.LogWarning("ServerConfig: Invalid server host, using localhost");
                serverHost = "127.0.0.1";
            }

            // Validate port
            if (serverPort is < 1 or > 65535)
            {
                Debug.LogWarning($"ServerConfig: Invalid port {serverPort}, using 6321");
                serverPort = 6321;
            }

            // Validate timeout
            if (connectionTimeoutMs is < 100 or > 30000)
            {
                Debug.LogWarning($"ServerConfig: Invalid timeout {connectionTimeoutMs}ms, using 5000ms");
                connectionTimeoutMs = 5000;
            }

            // Validate retries
            if (maxRetries is < 0 or > 10)
            {
                Debug.LogWarning($"ServerConfig: Invalid max retries {maxRetries}, using 3");
                maxRetries = 3;
            }
        }

        /// <summary>
        /// Create a default configuration for development/testing.
        /// </summary>
        public static ServerConfig CreateDefault()
        {
            return new ServerConfig
            {
                serverHost = "127.0.0.1",
                serverPort = 6321,
                connectionTimeoutMs = 5000,
                maxRetries = 3,
                enableLogging = true
            };
        }
    }
}
