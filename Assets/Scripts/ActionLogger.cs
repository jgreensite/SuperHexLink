using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SuperHexLink.Logging
{
    public enum ActionLogSeverity
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public enum ActionLogCategory
    {
        General,
        HexLifecycle,
        HexLand,
        GameLifecycle,
        Network,
        Performance,
        Security,
        Configuration
    }

    public enum LogFormat
    {
        Plain,
        Json,
        Both
    }

    public static class ActionLogger
    {
        private static string _correlationId;
        private static readonly Dictionary<string, object> _globalContext = new Dictionary<string, object>();
        private static LogFormat _logFormat = LogFormat.Plain;
        private static ActionLogSeverity _minimumLevel = ActionLogSeverity.Debug;

        /// <summary>
        /// Initialize the ActionLogger with correlation ID and global context.
        /// </summary>
        public static void Initialize(string correlationId = null, LogFormat format = LogFormat.Plain, ActionLogSeverity minimumLevel = ActionLogSeverity.Debug)
        {
            _correlationId = correlationId ?? GenerateCorrelationId();
            _logFormat = format;
            _minimumLevel = minimumLevel;
            
            // Add global context
            _globalContext["unityVersion"] = Application.unityVersion;
            _globalContext["platform"] = Application.platform.ToString();
            _globalContext["version"] = Application.version;
            _globalContext["startTime"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            
            Debug.Log($"ActionLogger initialized with correlation ID: {_correlationId}");
        }

        /// <summary>
        /// Set the correlation ID for the current session.
        /// </summary>
        public static void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? GenerateCorrelationId();
        }

        /// <summary>
        /// Get the current correlation ID.
        /// </summary>
        public static string GetCorrelationId()
        {
            return _correlationId ?? GenerateCorrelationId();
        }

        /// <summary>
        /// Add global context that will be included in all log entries.
        /// </summary>
        public static void AddGlobalContext(string key, object value)
        {
            _globalContext[key] = value;
        }

        /// <summary>
        /// Set the log format (Plain, Json, or Both).
        /// </summary>
        public static void SetLogFormat(LogFormat format)
        {
            _logFormat = format;
        }

        /// <summary>
        /// Set the minimum log level.
        /// </summary>
        public static void SetMinimumLevel(ActionLogSeverity minimumLevel)
        {
            _minimumLevel = minimumLevel;
        }

        /// <summary>
        /// Log a message with structured data support.
        /// </summary>
        public static void Log(ActionLogSettings settings, ActionLogCategory category, ActionLogSeverity severity, string message, params object[] args)
        {
            LogStructured(settings, category, severity, message, null, args);
        }

        /// <summary>
        /// Log a structured message with additional context data.
        /// </summary>
        public static void LogStructured(ActionLogSettings settings, ActionLogCategory category, ActionLogSeverity severity, string message, Dictionary<string, object> context = null, params object[] args)
        {
            if (settings == null || string.IsNullOrWhiteSpace(message)) return;
            if (!settings.IsEnabled(category, severity)) return;
            if (severity < _minimumLevel) return;

            string formattedMessage = (args != null && args.Length > 0) ? string.Format(message, args) : message;
            
            // Create log entry
            Dictionary<string, object> logEntry = CreateLogEntry(category, severity, formattedMessage, context);

            // Output based on format preference
            switch (_logFormat)
            {
                case LogFormat.Json:
                    OutputJson(logEntry, severity);
                    break;
                case LogFormat.Plain:
                    OutputPlain(logEntry, severity);
                    break;
                case LogFormat.Both:
                    OutputPlain(logEntry, severity);
                    OutputJson(logEntry, severity);
                    break;
            }
        }

        /// <summary>
        /// Log performance metrics.
        /// </summary>
        public static void LogPerformance(ActionLogSettings settings, string operation, TimeSpan duration, Dictionary<string, object> context = null)
        {
            var perfContext = new Dictionary<string, object>
            {
                ["operation"] = operation,
                ["durationMs"] = duration.TotalMilliseconds,
                ["category"] = "Performance"
            };

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    perfContext[kvp.Key] = kvp.Value;
                }
            }

            LogStructured(settings, ActionLogCategory.Performance, ActionLogSeverity.Info, 
                $"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms", perfContext);
        }

        /// <summary>
        /// Log security events.
        /// </summary>
        public static void LogSecurity(ActionLogSettings settings, string securityEvent, string details, Dictionary<string, object> context = null)
        {
            var secContext = new Dictionary<string, object>
            {
                ["securityEvent"] = securityEvent,
                ["details"] = details,
                ["category"] = "Security"
            };

            if (context != null)
            {
                foreach (var kvp in context)
                {
                    secContext[kvp.Key] = kvp.Value;
                }
            }

            LogStructured(settings, ActionLogCategory.Security, ActionLogSeverity.Warning, 
                $"Security: {securityEvent} - {details}", secContext);
        }

        /// <summary>
        /// Create a structured log entry.
        /// </summary>
        private static Dictionary<string, object> CreateLogEntry(ActionLogCategory category, ActionLogSeverity severity, string message, Dictionary<string, object> context)
        {
            var entry = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                ["level"] = severity.ToString(),
                ["category"] = category.ToString(),
                ["message"] = message,
                ["correlationId"] = GetCorrelationId()
            };

            // Add global context
            foreach (var kvp in _globalContext)
            {
                entry[$"global.{kvp.Key}"] = kvp.Value;
            }

            // Add specific context
            if (context != null)
            {
                foreach (var kvp in context)
                {
                    entry[kvp.Key] = kvp.Value;
                }
            }

            return entry;
        }

        /// <summary>
        /// Output log entry in plain text format.
        /// </summary>
        private static void OutputPlain(Dictionary<string, object> logEntry, ActionLogSeverity severity)
        {
            string category = logEntry["category"].ToString();
            string message = logEntry["message"].ToString();
            string correlationId = logEntry["correlationId"].ToString();
            string timestamp = logEntry["timestamp"].ToString();

            string prefix = $"[ActionLog/{category}] [{correlationId}] [{timestamp}] {message}";

            switch (severity)
            {
                case ActionLogSeverity.Critical:
                case ActionLogSeverity.Error:
                    Debug.LogError(prefix);
                    break;
                case ActionLogSeverity.Warning:
                    Debug.LogWarning(prefix);
                    break;
                default:
                    Debug.Log(prefix);
                    break;
            }
        }

        /// <summary>
        /// Output log entry in JSON format.
        /// </summary>
        private static void OutputJson(Dictionary<string, object> logEntry, ActionLogSeverity severity)
        {
            try
            {
                string json = JsonUtility.ToJson(new SerializableDictionary(logEntry), true);
                string jsonPrefix = $"[ActionLog-JSON] {json}";

                switch (severity)
                {
                    case ActionLogSeverity.Critical:
                    case ActionLogSeverity.Error:
                        Debug.LogError(jsonPrefix);
                        break;
                    case ActionLogSeverity.Warning:
                        Debug.LogWarning(jsonPrefix);
                        break;
                    default:
                        Debug.Log(jsonPrefix);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionLog-JSON-Error] Failed to serialize log entry: {ex.Message}");
                // Fallback to plain format
                OutputPlain(logEntry, severity);
            }
        }

        /// <summary>
        /// Generate a new correlation ID.
        /// </summary>
        private static string GenerateCorrelationId()
        {
            return Guid.NewGuid().ToString("N")[..8];
        }

        /// <summary>
        /// Get current logging configuration.
        /// </summary>
        public static Dictionary<string, object> GetConfiguration()
        {
            return new Dictionary<string, object>
            {
                ["correlationId"] = GetCorrelationId(),
                ["logFormat"] = _logFormat.ToString(),
                ["minimumLevel"] = _minimumLevel.ToString(),
                ["globalContext"] = _globalContext
            };
        }
    }

    /// <summary>
    /// Helper class to make Dictionary serializable by Unity's JsonUtility.
    /// </summary>
    [Serializable]
    public class SerializableDictionary
    {
        public List<string> keys;
        public List<object> values;

        public SerializableDictionary(Dictionary<string, object> dict)
        {
            keys = new List<string>();
            values = new List<object>();

            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
    }
}
