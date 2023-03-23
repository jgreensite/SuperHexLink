using UnityEngine;
using UnityEngine.InputSystem;

public class SelectHexes : MonoBehaviour
{
    public InputActionAsset inputActions;
    private InputAction selectHexAction;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        var actionMap = inputActions.FindActionMap("New action map");
        selectHexAction = actionMap.FindAction("SelectHex");
    }

    private void OnEnable()
    {
        selectHexAction.performed += OnSelectHex;
        selectHexAction.Enable();
    }

    private void OnDisable()
    {
        selectHexAction.performed -= OnSelectHex;
        selectHexAction.Disable();
    }

    public void OnSelectHex(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit) &&
                hit.collider.gameObject.CompareTag("Land") &&
                hit.collider.gameObject.layer == LayerMask.NameToLayer("Model"))
            {
                Hex currentHex = hit.collider.gameObject.transform.parent.GetComponent<Hex>();
                currentHex.ToggleSelect();
            }
        }
    }
}