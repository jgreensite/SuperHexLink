using UnityEngine;

public static class RuntimeDesignModeBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // 1. Ensure UI Manager is attached
        var gameUI = GameObject.Find("GameUIManager");
        if (gameUI != null)
        {
            if (gameUI.GetComponent<EditorUIManager>() == null)
            {
                gameUI.AddComponent<EditorUIManager>();
                Debug.Log("[Bootstrapper] Added EditorUIManager to GameUIManager");
            }
        }
        else
        {
            Debug.LogWarning("[Bootstrapper] Could not find GameUIManager to attach EditorUIManager");
        }
    }
}
