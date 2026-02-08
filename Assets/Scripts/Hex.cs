using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Represents a single hexagonal tile on the game board.
/// Owns a <see cref="HexState"/> that stores persistent data (type, number, rotation, etc.).
/// </summary>
public class Hex : MonoBehaviour
{
    /// <summary>
    /// Lightweight data-transfer object used by <see cref="CoreLogicAdapter"/> and <see cref="Initialize"/>.
    /// </summary>
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

    /// <summary>
    /// Full hex state used by spawners, the editor, and serialization.
    /// </summary>
    [Serializable]
    public class HexState
    {
        [ShowInInspector, OdinSerialize] public int Col;
        [ShowInInspector, OdinSerialize] public int Row;
        [ShowInInspector, OdinSerialize] public string HexType;
        [ShowInInspector, OdinSerialize] public string HexSubType;
        [ShowInInspector, OdinSerialize] public int Rotation;
        [ShowInInspector, OdinSerialize] public int? HexNum;
        [ShowInInspector, OdinSerialize] public string GroupID;
        [ShowInInspector, OdinSerialize] public bool Selected;

        // Runtime-only — do NOT serialize GameObject references into saved state
        [System.NonSerialized]
        public Dictionary<GameObject, Color> originalMaterialColors = new Dictionary<GameObject, Color>();

        public HexState() { }

        /// <summary>Converts offset (col, row) to cube coordinates.</summary>
        public HexExtensions.HexExtensions.Hex CRToHex(int col, int row)
        {
            var b = new HexExtensions.HexExtensions.OffsetCoord(col, row);
            var c = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b);
            return c;
        }

        /// <summary>Extracts the column from a cube-coordinate hex.</summary>
        public int CFromHex(HexExtensions.HexExtensions.Hex h)
        {
            return HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).col;
        }

        /// <summary>Extracts the row from a cube-coordinate hex.</summary>
        public int RFromHex(HexExtensions.HexExtensions.Hex h)
        {
            return HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).row;
        }

        /// <summary>Returns the six canonical hex direction vectors.</summary>
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

    public void Select() => state.Selected = true;

    public void Deselect() => state.Selected = false;

    public void ToggleSelect() => state.Selected = !state.Selected;

    /// <summary>Stub — visual edge updates are handled by <see cref="EdgeSpawner"/>.</summary>
    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { }

    public int Col => state.Col;
    public int Row => state.Row;
}