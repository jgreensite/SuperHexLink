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
                // Clicked on a Circular Menu Item
                else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("CircularMenuLayer"))
                {
                    HandleMenuItemClick(hit.collider.gameObject);
                }
            }
            // Clicked elsewhere
            else
            {
                DestroyCircularMenu();
            }
        }
    }

    private void HandleHexClick(GameObject hex)
    {
        DestroyCircularMenu();
        ShowCircularMenuAtHex(hex);
    }

    private void ShowCircularMenuAtHex(GameObject hex)
    {
        currentMenuInstance = Instantiate(circularMenuPrefab, hex.transform.position, Quaternion.identity);
        CircularMenu circularMenu = currentMenuInstance.GetComponent<CircularMenu>();
        circularMenu.ShowMenu();
    }

    private void HandleMenuItemClick(GameObject menuItem)
    {
        // Implement the logic for when a menu item is clicked.
        // This might involve calling a method on a script attached to the menuItem.
        Debug.Log("Clicked on menu item: " + menuItem.name);
    }

    private void DestroyCircularMenu()
    {
        if (currentMenuInstance != null)
        {
            Destroy(currentMenuInstance);
            currentMenuInstance = null;
        }
    }
}
