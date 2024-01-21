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

public class Hex : MonoBehaviour
{

    [SerializeField, HideInInspector]
    
    //create a HexState passing in a reference to the HexSpawner's state
    private HexState state;

    [ShowInInspector]
    public HexState hexState

    {
        get { return this.state; }
        set { this.state = value; }
    }

    //TODO - needs to be static in order for the HexSpawner to be referenced by the HexState nested class
    //However, is this an issue if we have more than one HexSpawner? Need to test, if it is then pass a reference into the constructor for Hex instead
    public HexSpawner hexSpawner;

    //TODO - Work out what this does, it may be am more elegant way of doing what you have
    IEnumerable<(Hex neighbor, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in EnumArray<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(state.Col, state.Row, direction);
            yield return (neighbor, direction);
        }
    }


    //private Renderer renderer;

    /*
    void Start()
    {
        renderer = GetComponent<Renderer>();

    }
    */


    private void Awake()
    {
        hexSpawner = GameObject.FindObjectOfType<HexSpawner>();
        state = new HexState(hexSpawner.GetComponent<HexSpawner>().State);
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
        ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
    }

    public void NotSelect()
    {
        Debug.Log("Not Selecting..." + gameObject.name + "...");
        state.Selected = true;
        ApplyGlow(GameConstants.NOT_SELECTED_HEX_COLOR);
    }

    public void Deselect()
    {
        Debug.Log("Deselecting..." + gameObject.name + "...");
        state.Selected = false;
        RestoreOriginalMaterials();
    }
    private void ApplyGlow(Color color)
    {
        Debug.Log("Applying glow material..." + gameObject.name + "...");
        List<Renderer> renderers = GetAllRenderers(transform);
        foreach (Renderer renderer in renderers)
        {
            //if the gameobject has a renderer with a material that has a property of color
            if(renderer.material.HasProperty("_Color"))
            {
                //store the original color of the material
                state.OriginalMaterialColors[renderer.gameObject] = renderer.material.color;
                //change the color of the material to yellow
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
            if (state.OriginalMaterialColors.ContainsKey(renderer.gameObject))
            {
                Debug.Log("Restoring original material..." + renderer.gameObject.name + "...");
                renderer.material.color = state.OriginalMaterialColors[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            state.OriginalMaterialColors.Remove(obj);
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

    public void UpdateNeighbors()
    {
        foreach(var (neighbor, direction) in NeighborsWithDirection())
            if(neighbor != null && neighbor.state.Selected)
                neighbor.UpdateEdge(direction.Opposite());
    }

    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) {
        // Get the edge value from the material
        var edge = Mathf.Floor(Mathf.Abs(GetComponent<MeshRenderer>().material.GetFloat($"_Edge{(int)direction}") - 1));
        // Update the edge value in the material
        GetComponent<MeshRenderer>().material.SetFloat(
            name: $"_Edge{(int)direction}",
            value: edge
        );
    }

public interface IHexState
{
    //HexExtensions.HexExtensions.Hex PositionDataHex { get; set; }
    int Col { get; set; }
    int Row { get; set; }
    string HexType { get; set; }
    string HexSubType { get; set; }
    int Rotation { get; set; }
    int? HexNum { get; set; }
    string GroupID { get; set; }
    bool Selected { get; set; }
    //Dictionary<GameObject, Color> OriginalMaterialColors { get; set; }
    //Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges { get; set; }
    //Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners { get; set; }
}

[System.Serializable]
public class BaseHexState : IHexState
{
    //protected HexExtensions.HexExtensions.Hex _hex;
    //protected int _col;
    //protected int _row;
    //public HexExtensions.HexExtensions.Hex PositionDataHex { get; set; }
    public int Col { get; set; }
    public int Row { get; set; }
    public string HexType { get; set; }
    public string HexSubType { get; set; }
    public int Rotation { get; set; }
    public int? HexNum { get; set; }
    public string GroupID { get; set; }
    public bool Selected { get; set; }
    //public Dictionary<GameObject, Color> OriginalMaterialColors { get; set; }
    //public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges { get; set; }
    //public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners { get; set; }
}
 
    public class HexState : IHexState
    {
        private HexExtensions.HexExtensions.Hex _hex;

        public HexSpawner.HexSpawnerState hexSpawnerState;
        
        [ShowInInspector] public HexExtensions.HexExtensions.Hex PositionDataHex
        {
            get { return _hex; }
            set
            {
                _hex = value;
                _col = CFromHex(_hex);
                _row = RFromHex(_hex);
            }
        }
        
        //public int col { get { return this.col; } set { this.col = value; hex = CRToHex(col, row); } }
        private int _col;
        [ShowInInspector] public int Col
        {
            get { return _col; }
            set
            {
                _col = value;
                _hex = CRToHex(_col, _row);
            }
        }
        //public int row { get { return this.row; } set { this.row = value; hex = CRToHex(col, row); } }
        private int _row;
        [ShowInInspector] public int Row
        {
            get { return _row; }
            set
            {
                _row = value;
                _hex = CRToHex(_col, _row);;
            }
        }

        public string HexType
        {
            get {return hexSpawnerState.hexes[_col][_row].HexType;}
            set { hexSpawnerState.hexes[_col][_row].HexType=value;}
        }
        public string HexSubType
        {
            get { return hexSpawnerState.hexes[_col][_row].HexSubType; }
            set { hexSpawnerState.hexes[_col][_row].HexSubType=value; }
        }
        public int Rotation
        {
            get { return hexSpawnerState.hexes[_col][_row].Rotation;}
            set { hexSpawnerState.hexes[_col][_row].Rotation=value; }
        } 
        public int? HexNum
        {
            get { return hexSpawnerState.hexes[_col][_row].HexNum; }
            set { hexSpawnerState.hexes[_col][_row].HexNum=value; }
        }
        public string GroupID
        {
            get { return hexSpawnerState.hexes[_col][_row].GroupID; }
            set { hexSpawnerState.hexes[_col][_row].GroupID=value; }

        }
        public bool Selected
        {
            get { return hexSpawnerState.hexes[_col][_row].Selected; }
            set { hexSpawnerState.hexes[_col][_row].Selected=value; }
        
        }
        //[System.NonSerialized] public MeshRenderer meshRenderer;
    
        public Dictionary<GameObject, Color> OriginalMaterialColors; 
        /*{
            get {return hexSpawner.State.hexes[_col][_row].OriginalMaterialColors;}
            set { hexSpawner.State.hexes[_col][_row].OriginalMaterialColors=value;}
        }*/

        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges;
        /*{
            get { return hexSpawner.State.hexes[_col][_row].Edges;}
            set {hexSpawner.State.hexes[_col][_row].Edges= value;}
        }*/

        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners;
        /*
        {
            get { return hexSpawner.State.hexes[_col][_row].Corners;}
            set {hexSpawner.State.hexes[_col][_row].Corners = value;}
        }
        */

        public HexState(HexSpawner.HexSpawnerState hexSpawnerState)
        {
            this.hexSpawnerState = hexSpawnerState;
            
            // Initialize the Edges dictionary in the constructor
            Edges = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection)))
            {
                RemoveEdgeStructure(direction);
            }

            // Initialize the Corners dictionary in the constructor
            Corners = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection)))
            {
                RemoveCornerStructure(direction);
            }
        }

        public void AddEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
        {
            Edges[direction] = true;
        }

        public void RemoveEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
        {
            Edges[direction] = false;
        }

        public void AddCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction)
        {
            Corners[direction] = "";
        }

        public void RemoveCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction)
        {
            Corners[direction] = "";
        }

        /*
        If you want to use the HexExtensions library you need to convert the Hex to a Point
        public List<HexExtensions.HexExtensions.Point> GetCorners()
        {
            List<HexExtensions.HexExtensions.Point> corners = new List<HexExtensions.HexExtensions.Point>();
        
            /*HexExtensions.HexExtensions.Layout myLayout =
            new HexExtensions.HexExtensions.Layout(HexExtensions.HexExtensions.Layout.pointy,
            new HexExtensions.HexExtensions.Point(sizeX, sizeY),
            new HexExtensions.HexExtensions.Point(originX, originY));

            corners = HexExtensions.HexExtensions.HexCorners(Hex, 1);
            return corners;
        }
        */

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
                
                    neighbours.Add(PositionDataHex.Neighbor(i));
                
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
}