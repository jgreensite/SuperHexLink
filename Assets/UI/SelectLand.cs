using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectLand : MonoBehaviour, HexGameControls.IMoveActions
{
    private HexGameControls inputs;
    private Camera cam;

    public GameObject circularMenuPrefab;
    private GameObject currentMenuInstance;
    
    // Cached references
    private HexSpawner _hexSpawner;
    private EditorUIManager _editorUiManager;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new HexGameControls();
        inputs.Move.SetCallbacks(this);
    }

    private void Start()
    {
        _hexSpawner = FindObjectOfType<HexSpawner>();
        _editorUiManager = FindObjectOfType<EditorUIManager>();
    }

    private void OnEnable() => inputs.Move.Enable();
    private void OnDisable() => inputs.Move.Disable();

    public void OnSelectHex(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Clicked on a Hex
                if (hit.collider.gameObject.CompareTag("Land") &&
                    hit.collider.gameObject.layer == LayerMask.NameToLayer("Model"))
                {
                    HandleHexClick(hit.collider.gameObject);
                }
            }
        }
    }

    private void HandleHexClick(GameObject go)
    {
        // Close any open menu using cached reference
        if (_editorUiManager == null) _editorUiManager = FindObjectOfType<EditorUIManager>();
        _editorUiManager?.HideMenu();

        // Get the parent that holds the part of the hex that has been clicked on
        var clickedHex = go.GetComponentInParent<Hex>();
        if (clickedHex == null)
        {
            Debug.LogError("Clicked object does not have a Hex component in parent hierarchy.");
            return;
        }

        // Toggle selection on the clicked hex
        clickedHex.ToggleSelect();

        // Efficiently deselect others using HexSpawner helper
        if (_hexSpawner != null)
        {
            var allHexes = _hexSpawner.GetAllHexes();
            foreach (var otherHex in allHexes)
            {
                if (otherHex != null && otherHex != clickedHex && otherHex.hexState != null && otherHex.hexState.Selected)
                {
                    otherHex.NotSelect();
                }
            }
        }
        else
        {
            // Fallback: FindObjectsOfType is better than FindGameObjectsWithTag
            var allHexes = FindObjectsOfType<Hex>();
            foreach (var otherHex in allHexes)
            {
                 if (otherHex != null && otherHex != clickedHex && otherHex.hexState != null && otherHex.hexState.Selected)
                 {
                     otherHex.NotSelect();
                 }
            }
        }
    }

    public void OnContextSelect(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            HandleContextClick();
        }
    }

    private void HandleContextClick()
    {
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check for Land/Model hit
            if (hit.collider.gameObject.CompareTag("Land") &&
                hit.collider.gameObject.layer == LayerMask.NameToLayer("Model"))
            {
                 var hex = hit.collider.GetComponentInParent<Hex>();
                 if (hex != null)
                 {
                     // Debug logging reduced for production cleanliness
                     // Debug.Log($"[SelectLand] Context Click on {hex.name}");
                     
                     if (_editorUiManager == null) _editorUiManager = FindObjectOfType<EditorUIManager>();
                     
                     if (_editorUiManager != null)
                     {
                         // Show Menu
                         _editorUiManager.ShowMenu(Mouse.current.position.ReadValue(), (cmd) => 
                         {
                             if (cmd == "CMD_ROTATE") RotateHex(hex);
                             else if (cmd == "CMD_NUMBER") CycleHexNumber(hex);
                             else UpdateHexType(hex, cmd);
                         });
                     }
                 }
            }
        }
    }

    private void UpdateHexType(Hex hex, string newType)
    {
        if (hex == null || hex.hexState == null) return;
        if (_hexSpawner == null) _hexSpawner = FindObjectOfType<HexSpawner>();

#if UNITY_EDITOR
        _hexSpawner.CreateUndoSnapshot();
        Undo.RecordObject(hex, "Change Hex Type");
        if (_hexSpawner != null) 
        {
            Undo.RegisterCompleteObjectUndo(_hexSpawner, "Change Hex Type");
        }
#endif

        hex.hexState.HexType = newType;
        
        // Normalize rotation for types that don't support it (e.g. Sea)
        // This prevents "hidden" rotation state from persisting when replacing back and forth
        if (newType == GameConstants.CAR_TYPE_SEA)
        {
            hex.hexState.Rotation = 0;
        }

        _hexSpawner?.RefreshHex(hex);

#if UNITY_EDITOR
        EditorUtility.SetDirty(hex);
        if (_hexSpawner != null) 
        {
            EditorUtility.SetDirty(_hexSpawner);
            // CRITICAL: Update snapshot to capture the "Future" state for Redo
            _hexSpawner.CreateUndoSnapshot();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hex.gameObject.scene);
#endif
    }

    private void RotateHex(Hex hex)
    {
        if (hex == null || hex.hexState == null) return;
        if (_hexSpawner == null) _hexSpawner = FindObjectOfType<HexSpawner>();

#if UNITY_EDITOR
        _hexSpawner.CreateUndoSnapshot();
        Undo.RecordObject(hex, "Rotate Hex");
        if (_hexSpawner != null)
        {
             Undo.RegisterCompleteObjectUndo(_hexSpawner, "Rotate Hex");
        }
#endif

        hex.hexState.Rotation = (hex.hexState.Rotation + 60) % 360;
        _hexSpawner?.RefreshHex(hex);

#if UNITY_EDITOR
        EditorUtility.SetDirty(hex);
        if (_hexSpawner != null) 
        {
            EditorUtility.SetDirty(_hexSpawner);
            // CRITICAL: Update snapshot to capture the "Future" state for Redo
            _hexSpawner.CreateUndoSnapshot();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hex.gameObject.scene);
#endif
    }
    
    private void CycleHexNumber(Hex hex)
    {
        if (hex == null || hex.hexState == null) return;
        if (_hexSpawner == null) _hexSpawner = FindObjectOfType<HexSpawner>();

#if UNITY_EDITOR
        _hexSpawner.CreateUndoSnapshot();
        Undo.RecordObject(hex, "Cycle Hex Number");
        if (_hexSpawner != null)
        {
             Undo.RegisterCompleteObjectUndo(_hexSpawner, "Cycle Hex Number");
        }
#endif

        int[] nums = { 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 };
        int current = hex.hexState.HexNum ?? 2;
        int nextIndex = 0;
        for(int i=0; i<nums.Length; i++)
        {
            if (nums[i] == current)
            {
                nextIndex = (i + 1) % nums.Length;
                break;
            }
        }
        hex.hexState.HexNum = nums[nextIndex];
        _hexSpawner?.RefreshHex(hex);

#if UNITY_EDITOR
        EditorUtility.SetDirty(hex);
        if (_hexSpawner != null) 
        {
            EditorUtility.SetDirty(_hexSpawner);
            // CRITICAL: Update snapshot to capture the "Future" state for Redo
            _hexSpawner.CreateUndoSnapshot();
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hex.gameObject.scene);
#endif
    }
}
