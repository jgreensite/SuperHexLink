using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using HexExtensions;
using SimpleHexExtensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;

public class Hex : MonoBehaviour
{

 
    [ShowInInspector]
    //TODO - needs to be static in order for the HexSpawner to be referenced by the HexState nested class
    //However, is this an issue if we have more than one HexSpawner? Need to test, if it is then pass a reference into the constructor for Hex instead

    public HexState State{ get; private set; }
   
    
    // Method to initialize the hex with its state
    public void Initialize(HexState state)
    {
        State = state;
        // Update the visual representation of the hex based on the state
        UpdateVisuals();
    }
    
    // Method to update the hex with a new state
    public void UpdateHex(HexState newState)
    {
        State = newState;
        // Update the visual representation of the hex
        UpdateVisuals();
    }

    // Method to update the visual representation of the hex
    private void UpdateVisuals()
    {
        // Implement logic to update the hex's appearance based on its state
        if (State.Selected)
        {
            // Apply selected visual effect
            ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
        }
        else
        {
            // Restore original appearance
            RestoreOriginalMaterials();
        }
    }

    public void ToggleSelect()
    {
        Debug.Log("Toggling selection..." + gameObject.name + "...");
        if (State.Selected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    public void Select()
    {
        Debug.Log("Selecting..." + gameObject.name + "...");
        State.Selected = true;
        ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
    }

    public void NotSelect()
    {
        Debug.Log("Not Selecting..." + gameObject.name + "...");
        State.Selected = true;
        ApplyGlow(GameConstants.NOT_SELECTED_HEX_COLOR);
    }

    public void Deselect()
    {
        Debug.Log("Deselecting..." + gameObject.name + "...");
        State.Selected = false;
        RestoreOriginalMaterials();
    }
    // Method to apply a glow effect to the hex
    private void ApplyGlow(Color color)
    {
        Debug.Log("Applying glow material..." + gameObject.name + "...");
        List<Renderer> renderers = GetAllRenderers(transform);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                // Store the original color of the material
                State.OriginalMaterialColors[renderer.gameObject] = renderer.material.color;
                // Change the color of the material
                renderer.material.color = color;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        Debug.Log("Restoring original materials..." + gameObject.name + "...");
        List<Renderer> renderers = GetAllRenderers(transform);
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (Renderer renderer in renderers)
        {
            if (State.OriginalMaterialColors.ContainsKey(renderer.gameObject))
            {
                Debug.Log("Restoring original material..." + renderer.gameObject.name + "...");
                renderer.material.color = State.OriginalMaterialColors[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            State.OriginalMaterialColors.Remove(obj);
        }
    }

    private List<Renderer> GetAllRenderers(Transform parent)
    {
        List<Renderer> renderers = new List<Renderer>();

        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
            renderers.AddRange(GetAllRenderers(child));
        }
        return renderers;
    } 
}