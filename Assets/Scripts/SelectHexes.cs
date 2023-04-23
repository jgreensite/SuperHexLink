using UnityEngine;
using UnityEngine.InputSystem;

public class SelectHexes : MonoBehaviour, HexGameControls.IMoveActions
{
    private HexGameControls inputs;
    private Camera cam;

    public CircularMenu ClickMenu;

    private void Awake()
    {
        cam = Camera.main;
        inputs = new HexGameControls();
        inputs.Move.SetCallbacks(this);
    }

    private void OnEnable() =>  inputs.Move.Enable();
    private void OnDisable() => inputs.Move.Disable();
    public void OnSelectHex(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                break;
            case InputActionPhase.Performed:
            {
                Debug.Log("Performed...");
                if(cam != null){
                    Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

                    if (Physics.Raycast(ray, out RaycastHit hit) &&
                        hit.collider.gameObject.CompareTag("Land") &&
                        hit.collider.gameObject.layer == LayerMask.NameToLayer("Model"))
                    {
                        Hex currentHex = hit.collider.gameObject.transform.parent.GetComponent<Hex>();
                        currentHex.ToggleSelect();
                    }
                ClickMenu.ShowCircularMenu();
                }
            }
                break;
            case InputActionPhase.Canceled:
                break;
        }
    }

     public void OnStart(InputAction.CallbackContext context){}
}