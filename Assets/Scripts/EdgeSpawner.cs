using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using AnyClone;
using FDL.Library.Numeric;
//using Script;
using TMPro;
using SimpleHexExtensions;
using HexExtensions;
public class EdgeSpawner : SpawnerBase
{
    [ShowInInspector,OdinSerialize]
    private EdgeSpawnerState state;

    public EdgeSpawnerState State
    {
        get { return state; }
        set { state = value; }
    }
    
    /*
    [SerializeField]
    private Edge edgePrefab;

    [SerializeField, TableList]
    private List<Edge> edges = new List<Edge>();

    public override void ClearItems()
    {
        foreach (var edge in edges)
        {
            if (edge != null)
            {
                DestroyImmediate(edge.gameObject);
            }
        }
        edges.Clear();
    }

    public override void InitializeOrRefreshItems()
    {
        // Assuming each edge is placed based on two adjacent hexes
        foreach (var hexRow in hexes)
        {
            foreach (var hex in hexRow)
            {
                foreach (var direction in Enum.GetValues(typeof(HexDirection)))
                {
                    Vector3 edgePosition = CalculateEdgePosition(hex, (HexDirection)direction);
                    Edge newEdge = Instantiate(edgePrefab, edgePosition, Quaternion.identity, this.transform);
                    newEdge.Initialize(hex, hex.GetNeighbor((HexDirection)direction)); // Assuming a method to get neighboring hexes
                    edges.Add(newEdge);
                }
            }
        }
    }

    private Vector3 CalculateEdgePosition(Hex hex, HexDirection direction)
    {
        // Implement your logic to determine the position of an edge based on a hex and a direction
        return new Vector3(); // Placeholder
    }
    */

    public override void Spawn()
    {
        BuildMe(false);
    }

    public override void BuildMe(bool isRefresh)
    {
        //throw new NotImplementedException();
    }

    public override void Clear()
    {
        //throw new NotImplementedException();
    }

    public override void Refresh()
    {
        //throw new NotImplementedException();
    }

    [System.Serializable]
    public class EdgeSpawnerState
    {
        [SerializeField] public string myState;
    }
}
