using UnityEngine;

namespace SuperHexLink.Logging
{
    [CreateAssetMenu(fileName = "ActionLogSettings", menuName = "Logging/Action Log Settings")]
    public class ActionLogSettings : ScriptableObject
    {
        public bool enabled = true;
        public ActionLogSeverity minSeverity = ActionLogSeverity.Info;
        public bool logGeneral = true;
        public bool logHexLifecycle = true;
        public bool logHexLand = true;
        public bool logGameLifecycle = true;

        public bool IsEnabled(ActionLogCategory category, ActionLogSeverity severity)
        {
            if (!enabled) return false;
            if (severity < minSeverity) return false;
            return category switch
            {
                ActionLogCategory.HexLifecycle => logHexLifecycle,
                ActionLogCategory.HexLand => logHexLand,
                ActionLogCategory.GameLifecycle => logGameLifecycle,
                _ => logGeneral
            };
        }
    }
}
