using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexLandModel : MonoBehaviour
{
    private Renderer myRenderer;

    // Start is called before the first frame update
    private void Start()
    {
        myRenderer = GetComponent<Renderer>();

        // Check if Renderer component is found
        if (myRenderer == null)
        {
            //Debug.LogError("Renderer component not found on " + gameObject.name);
        }
    }

    private void OnMouseEnter()
    {
        // Ensure renderer is not null before accessing it
        if (myRenderer != null)
        {
            myRenderer.material.color = Color.red;
        }
    }

    private void OnMouseExit()
    {
        // Ensure renderer is not null before accessing it
        if (myRenderer != null)
        {
            myRenderer.material.color = Color.white;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

