using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexLandModel : MonoBehaviour
{
    private Renderer renderer;

    // Start is called before the first frame update
     private void Start()
    {

        renderer = GetComponent<Renderer>();
    }

   
    private void OnMouseEnter()
    {
	renderer.material.color = Color.red;
    }

    private void OnMouseExit()
    {
        renderer.material.color = Color.white;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
