using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexLandModel : MonoBehaviour
{
    //TODO - this is left over code for the old hex model.  It should be removed once the new model is working. I don't see the need for the ability to change the color of the hexes like this as we have another system for that now.
    
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

