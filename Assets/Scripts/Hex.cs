﻿using System;
using System.Collections;
using System.Collections.Generic;
using HexExtensions;
using SimpleHexExtensions;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class Hex : MonoBehaviour
{

    [SerializeField, HideInInspector]
    private HexState state = new HexState();

    [ShowInInspector]
    public HexState hexState

    {
        get { return this.state; }
        set { this.state = value; }
    }

    private HexSpawner hexSpawner;

    IEnumerable<(Hex neighbor, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in EnumArray<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(state.col, state.row, direction);
            yield return (neighbor, direction);
        }
    }


    private Renderer renderer;

    void Start()
    {
        renderer = GetComponent<Renderer>();

    }


    private void Awake()
    {
        hexSpawner = GameObject.FindObjectOfType<HexSpawner>();

    }

    public void ToggleSelect()
    {
        Debug.Log("Toggling selection..." + gameObject.name + "...");
        if (state.Selected)
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
        state.Selected = true;
        ApplyGlowMaterial();
    }

    private void Deselect()
    {
        Debug.Log("Deselecting..." + gameObject.name + "...");
        state.Selected = false;
        RestoreOriginalMaterials();
    }
     private void ApplyGlowMaterial()
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        foreach (Renderer renderer in renderers)
        {
            Debug.Log("storing materials");
            state.originalMaterials[renderer.gameObject] = renderer.material;
            renderer.material.color = Color.red; // Change the color to yellow or any other desired color
        }
    }

    private void RestoreOriginalMaterials()
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (Renderer renderer in renderers)
        {
            if (state.originalMaterials.ContainsKey(renderer.gameObject))
            {
                Debug.Log("restoring materials");
                renderer.material = state.originalMaterials[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            state.originalMaterials.Remove(obj);
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
        Debug.Log("found renderers..." + renderers); 
        return renderers;
    }

    public void UpdateNeighbors()
    {
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor != null && neighbor.state.Selected)
                neighbor.UpdateEdge(direction.Opposite());
    }

    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) {
        // Get the edge value from the material
        var edge = Mathf.Floor(Mathf.Abs(state.meshRenderer.material.GetFloat($"_Edge{(int)direction}") - 1));
        // Update the edge value in the material
        state.meshRenderer.material.SetFloat(
            name: $"_Edge{(int)direction}",
            value: edge
        );
    }
}

[System.Serializable]
public class HexState
{
    //public int col { get { return this.col; } set { this.col = value; hex = CRToHex(col, row); } }
    private int _col;
    public int col
    {
        get { return _col; }
        set
        {
            _col = value;
            //_hex = CRToHex(col, row);
        }
    }
    //public int row { get { return this.row; } set { this.row = value; hex = CRToHex(col, row); } }
    private int _row;
    public int row
    {
        get { return _row; }
        set
        {
            _row = value;
            Hex = CRToHex(col, row);
            Debug.Log("q:" + Hex.q + " " + "r:" + Hex.r + " " + "s:" + Hex.s + " ");
        }
    }
    public string HexType;
    public string HexSubType;
    public int Rotation = 0;
    public int? HexNum;
    public string GroupID;
    public bool Selected;
    public MeshRenderer meshRenderer;
    public Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    [ShowInInspector] public HexExtensions.HexExtensions.Hex Hex { get; set; }

//TODO - Tidy-up Extensions
//These are new methods that should probably be added to HexExtensions
    public HexExtensions.HexExtensions.Hex CRToHex(int col, int row)
    {
        HexExtensions.HexExtensions.OffsetCoord b = new HexExtensions.HexExtensions.OffsetCoord(col, row);
        HexExtensions.HexExtensions.Hex c = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b);
        return (c);
    }

    public int CFromHex(HexExtensions.HexExtensions.Hex h)
    {
        return (HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).col);
    }

    public int RFromHex(HexExtensions.HexExtensions.Hex h)
    {
        return (HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).row);
    }

    //Each Hex will have a specific set of neighbours, note that this will include hexes at co-ordinates outside the gameboard
    //use isOnBoardHex to check to see if a Hex is on the gameboard 
    public List<HexExtensions.HexExtensions.Hex> Neighbours()
    {
        int i = 0;
        List<HexExtensions.HexExtensions.Hex> neighbours = new();
        foreach (HexExtensions.HexExtensions.Hex h in HexExtensions.HexExtensions.Hex.directions)
        {
            var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h);
           
            if (((o.col > 0)) && (o.row > 0))
            {
            
                neighbours.Add(Hex.Neighbor(i));
            
            }
            else
            {
                //TODO - A bit of a hack in here to place a filler in the 
                var fill = new HexExtensions.HexExtensions.Hex();
                fill.Scale(0);
                neighbours.Add(fill);
            }
            
            i++;
        }
        return (neighbours);
    }
}