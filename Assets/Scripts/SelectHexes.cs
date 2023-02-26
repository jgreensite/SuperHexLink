using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class SelectHexes : MonoBehaviour
{
    Mouse mouse => Mouse.current;
    Camera cam;

    void Update()
    {
        // If the left mouse button was pressed this frame, we will
        // check if we clicked on a hex.
        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Get the main camera.
            cam = Camera.main;
            // Get the mouse position.
            Vector3 mousePosition = mouse.position.ReadValue();
            Debug.Log(mousePosition);
            // Create a ray from the mouse position.
            Ray ray = cam.ScreenPointToRay(mousePosition);
            // Check if the ray hit anything.
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Get the hex component from the hit object.
                Hex hex = hit.collider.GetComponent<Hex>();
                // If the hex component is not null, we know we hit a hex.
                // Toggle the selection of the hex.
                hex?.ToggleSelect();
            }
        }
    }
}
