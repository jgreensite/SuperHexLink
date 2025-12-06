using System;
using UnityEngine;

namespace SuperHexLink.Logging
{
    public enum ActionLogSeverity
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public enum ActionLogCategory
    {
        General,
        HexLifecycle,
        HexLand,
        GameLifecycle
    }

    public static class ActionLogger
    {
        public static void Log(ActionLogSettings settings, ActionLogCategory category, ActionLogSeverity severity, string message, params object[] args)
        {
            if (settings == null || string.IsNullOrWhiteSpace(message)) return;
            if (!settings.IsEnabled(category, severity)) return;

            string formatted = (args != null && args.Length > 0) ? string.Format(message, args) : message;
            string prefix = $"[ActionLog/{category}] ";
            string payload = prefix + formatted;

            switch (severity)
            {
                case ActionLogSeverity.Warning:
                    Debug.LogWarning(payload);
                    break;
                case ActionLogSeverity.Error:
                    Debug.LogError(payload);
                    break;
                default:
                    Debug.Log(payload);
                    break;
            }
        }
    }
}
