using UnityEngine;
using UnityEngine.InputSystem;

public class SelectLand : MonoBehaviour, HexGameControls.IMoveActions
{
    private HexGameControls inputs;
    private Camera cam;

    public GameObject circularMenuPrefab;
    private GameObject currentMenuInstance;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new HexGameControls();
        inputs.Move.SetCallbacks(this);
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
        // Close any open menu
        FindObjectOfType<EditorUIManager>()?.HideMenu();

        // Get the parent that holds the part of the hex that has been clicked on and toggle the selection
        go.transform.parent.GetComponent<Hex>().ToggleSelect();

        // Grey out all other hexes
        foreach (GameObject otherHex in GameObject.FindGameObjectsWithTag("Land"))
        {
            if (go.transform.parent == null)
            {
                Debug.LogError("Parent is null");
            }
            else if (go.transform.parent.GetComponent<Hex>() == null)
            {
                Debug.LogError("Hex component on parent is null");
            }
            else if (otherHex == null)
            {
                Debug.LogError("otherHex is null");
            }
            else        
            {
                if ((otherHex.transform.parent.GetComponent<Hex>() != go.transform.parent.GetComponent<Hex>())
                && (otherHex.layer == LayerMask.NameToLayer("Model")))
                {
                    //call not selected method
                    otherHex.transform.parent.GetComponent<Hex>().NotSelect();
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
                 var hex = hit.collider.transform.parent.GetComponent<Hex>();
                 if (hex != null)
                 {
                     Debug.Log($"[SelectLand] Context Click on {hex.name}");
                     
                     var uiManager = FindObjectOfType<EditorUIManager>();
                     if (uiManager != null)
                     {
                         // Show Menu
                         uiManager.ShowMenu(Mouse.current.position.ReadValue(), (cmd) => 
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
        hex.hexState.HexType = newType;
        FindObjectOfType<HexSpawner>()?.RefreshHex(hex);
    }

    private void RotateHex(Hex hex)
    {
        hex.hexState.Rotation = (hex.hexState.Rotation + 60) % 360;
        FindObjectOfType<HexSpawner>()?.RefreshHex(hex);
    }
    
    private void CycleHexNumber(Hex hex)
    {
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
        FindObjectOfType<HexSpawner>()?.RefreshHex(hex);
    }
}
