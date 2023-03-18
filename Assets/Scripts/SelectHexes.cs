using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEngine.InputSystem.InputAction;

public class SelectHexes : MonoBehaviour
{
    Mouse mouse => Mouse.current;
    Camera cam;

    public Material glowMaterial;
    //private Color glowColor = Color.yellow;
    //public float glowIntensity = 2.0f;
    //public float glowThreshold = 1.0f;

    //private PostProcessVolume postProcessVolume;

    private void Start()
    {
        /*
        postProcessVolume = FindObjectOfType<PostProcessVolume>();
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGetSettings(out bloom);
        }
        */
    }
    
    void Update()
    {
        // If the left mouse button was pressed this frame, we will
        // check if we clicked on a hex.
        if (Input.GetMouseButtonDown(0))
        {
            // Get the main camera.
            cam = Camera.main;
            // Get the mouse position.
            Vector3 mousePosition = mouse.position.ReadValue();
            // Create a ray from the mouse position.
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            // Check if the ray hit anything.

            if (Physics.Raycast(ray, out RaycastHit hit) &&
                (hit.collider.gameObject.CompareTag("Land")) &&
                (hit.collider.gameObject.layer == LayerMask.NameToLayer("Model")))
            {

                GameObject go = hit.collider.gameObject;

                Debug.Log("got a hit with mouse click");
                //get all children of the hit object and set their material to the glow material
                foreach (Transform child in go.transform)
                {
                    if(child.GetComponent<Renderer>() != null) {
                        Debug.Log("got a child of the hit object");
                        child.GetComponent<Renderer>().material = glowMaterial;
                    }
                }
            }
        }
    }
}
