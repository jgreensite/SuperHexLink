using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;
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

    IEnumerable<(Hex neighbor, HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach(HexNeighborDirection direction in EnumArray<HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(state.col, state.row, direction);
            yield return (neighbor, direction);
        }
    }
    

    private void Awake()
    {
        hexSpawner = GameObject.FindObjectOfType<HexSpawner>();

    }

    private void Start()
    {
        /*
        Instantiate(hexNumberPrefab);
        hexNumberPrefab.transform.parent = transform;
        hexNumberPrefab.transform.localPosition = Vector3.zero;
        */
    }

    public void ToggleSelect() => (state.Selected ? (Action)Deselect : (Action)Select)();

    private void Select()
    {
        state.Selected = true;

        // Update edges of this hex based on the direction to each neighbor
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor == null || !neighbor.state.Selected)
                UpdateEdge(direction);

        UpdateNeighbors();
    }


    private void Deselect()
    {
        state.Selected = false;

        // Clear self
        for(int i = 0; i < 6; i++)
            state.meshRenderer.material.SetFloat($"_Edge{i}", 0f);

        UpdateNeighbors();
    }

    public void UpdateNeighbors()
    {
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor != null && neighbor.state.Selected)
                neighbor.UpdateEdge(direction.Opposite());
    }

    public void UpdateEdge(HexNeighborDirection direction) => 
        state.meshRenderer.material.SetFloat(
            name: $"_Edge{(int)direction}",
            value: Mathf.Abs(state.meshRenderer.material.GetFloat($"_Edge{(int)direction}") - 1).Floor()
        );
}

[System.Serializable]
public class HexState
{ 
    public int col;
    public int row;
    public string HexType;
    public int? HexNum;
    public string GroupID;
    public bool Selected;
    public MeshRenderer meshRenderer;
}