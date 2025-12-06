using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

// Restored Hex implementation with HexState API required across the codebase.
public class Hex : MonoBehaviour
{
    [Serializable]
    public class BaseHexState
    {
        public int Col;
        public int Row;
        public string HexType;
        public string HexSubType;
        public int Rotation;
        public int? HexNum;
        public string GroupID;
        public bool Selected;
    }

    [Serializable]
    public class HexState
    {
        // Position helpers (optional internal representation)
        private HexExtensions.HexExtensions.Hex _hex;

    // Public properties expected by spawners and UI
    [ShowInInspector, OdinSerialize] public int Col;
    [ShowInInspector, OdinSerialize] public int Row;
    [ShowInInspector, OdinSerialize] public string HexType;
    [ShowInInspector, OdinSerialize] public string HexSubType;
    [ShowInInspector, OdinSerialize] public int Rotation;
    [ShowInInspector, OdinSerialize] public int? HexNum;
    [ShowInInspector, OdinSerialize] public string GroupID;
    [ShowInInspector, OdinSerialize] public bool Selected;

        // runtime-only visuals - do NOT serialize GameObject references into saved state
    [System.NonSerialized]
    public Dictionary<GameObject, Color> originalMaterialColors = new Dictionary<GameObject, Color>();

        public HexState() { }

        // Convert column/row to HexExtensions hex
        public HexExtensions.HexExtensions.Hex CRToHex(int col, int row)
        {
            var b = new HexExtensions.HexExtensions.OffsetCoord(col, row);
            var c = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b);
            return c;
        }

        public int CFromHex(HexExtensions.HexExtensions.Hex h)
        {
            return HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).col;
        }

        public int RFromHex(HexExtensions.HexExtensions.Hex h)
        {
            return HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).row;
        }

        // Return the neighbors as HexExtensions hexes — used by some land placement logic
        public List<HexExtensions.HexExtensions.Hex> Neighbours()
        {
            var list = new List<HexExtensions.HexExtensions.Hex>();
            foreach (var dir in HexExtensions.HexExtensions.Hex.directions)
            {
                list.Add(dir);
            }
            return list;
        }
    }

    [SerializeField, HideInInspector]
    [OdinSerialize]
    private HexState state = new HexState();

    [ShowInInspector]
    public HexState hexState { get => state; set => state = value; }

    private HexSpawner hexSpawner;

    // Called by spawners during creation/load to bind a backing state
    public void Initialize(HexSpawner hs, BaseHexState backingState)
    {
        hexSpawner = hs;
        if (backingState != null)
        {
            state.Col = backingState.Col;
            state.Row = backingState.Row;
            state.HexType = backingState.HexType;
            state.HexSubType = backingState.HexSubType;
            state.Rotation = backingState.Rotation;
            state.HexNum = backingState.HexNum;
            state.GroupID = backingState.GroupID;
            state.Selected = backingState.Selected;
        }
    }

    private void Awake()
    {
        if (hexSpawner == null) hexSpawner = GameObject.FindObjectOfType<HexSpawner>();
    }

    public void Select()
    {
        state.Selected = true;
    }

    public void NotSelect()
    {
        state.Selected = false;
    }

    public void Deselect() => NotSelect();

    public void ToggleSelect() => state.Selected = !state.Selected;

    // Edge update stub (actual visual logic lives elsewhere)
    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { }

    public int Col => state.Col;
    public int Row => state.Row;
}