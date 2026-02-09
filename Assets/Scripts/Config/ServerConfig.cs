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
            config.serverHost = GetEnvVar("SUPERHEX_SERVER_HOST", config.serverHost);
            
            string portEnv = GetEnvVar("SUPERHEX_SERVER_PORT", config.serverPort.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(portEnv, out int portOverride))
            {
                config.serverPort = portOverride;
            }

            string timeoutEnv = GetEnvVar("SUPERHEX_CONNECTION_TIMEOUT", config.connectionTimeoutMs.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(timeoutEnv, out int timeoutOverride))
            {
                config.connectionTimeoutMs = timeoutOverride;
            }

            string retriesEnv = GetEnvVar("SUPERHEX_MAX_RETRIES", config.maxRetries.ToString(CultureInfo.InvariantCulture));
            if (int.TryParse(retriesEnv, out int retriesOverride))
            {
                config.maxRetries = retriesOverride;
            }

            string loggingEnv = GetEnvVar("SUPERHEX_ENABLE_LOGGING", config.enableLogging.ToString());
            if (bool.TryParse(loggingEnv, out bool loggingOverride))
            {
                config.enableLogging = loggingOverride;
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
