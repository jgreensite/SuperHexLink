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
public class CornerSpawner : SpawnerBase<CornerSpawner.CornerSpawnerState>
{
    private CornerSpawnerState state;

    public override CornerSpawnerState State
    {
        get { return state; }
        set { state = value; }
    }
    /*
    [SerializeField]
    private Corner cornerPrefab;

    [SerializeField, TableList]
    private List<Corner> corners = new List<Corner>();

    public override void ClearItems()
    {
        foreach (var corner in corners)
        {
            if (corner != null)
            {
                DestroyImmediate(corner.gameObject);
            }
        }
        corners.Clear();
    }

    public override void InitializeOrRefreshItems()
    {
        // Assuming each corner is placed at the intersection of three hexes
        foreach (var hexRow in hexes)
        {
            foreach (var hex in hexRow)
            {
                foreach (var cornerIndex in Enumerable.Range(0, 6)) // Assuming 6 corners for a hex
                {
                    Vector3 cornerPosition = CalculateCornerPosition(hex, cornerIndex);
                    Corner newCorner = Instantiate(cornerPrefab, cornerPosition, Quaternion.identity, this.transform);
                    newCorner.Initialize(hex, cornerIndex); // Assuming an Initialize method in Corner class
                    corners.Add(newCorner);
                }
            }
        }
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
    public class CornerSpawnerState
    {
        [SerializeField] public string myState;
    }

    private Vector3 CalculateCornerPosition(Hex hex, int cornerIndex)
    {
        // Implement your logic to determine the position of a corner based on a hex and a corner index
        return new Vector3(); // Placeholder
    }
}
