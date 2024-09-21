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

    [SerializeField, HideInInspector]
    
    //create a HexState passing in a reference to the HexSpawner's state
    //private HexState state;

    [ShowInInspector]
    //TODO - needs to be static in order for the HexSpawner to be referenced by the HexState nested class
    //However, is this an issue if we have more than one HexSpawner? Need to test, if it is then pass a reference into the constructor for Hex instead
    public HexSpawnerState hexSpawnerState;
    public HexSpawner hexSpawner;
    public HexState hexState{ get; set; }

    /*
    public Hex(HexSpawner hs)
    {

        hexSpawner = hs;
        hexSpawnerState = hs.State;
        hexState = new HexState(hexSpawnerState)
        {
            GroupID = "0",
            Col = 0,
            Row = 0
        };

    }
    */
    
    public Hex()
    {

    }
    
    public void Initialize(HexSpawner hs)
    {
        hexSpawner = hs;
        hexSpawnerState = hs.State;

        if (hexSpawnerState == null)
        {
            throw new InvalidOperationException("HexSpawnerState cannot be null.");
        }

        hexState = new HexState(hexSpawnerState);
    }
    
    //TODO - Work out what this does, it may be am more elegant way of doing what you have
    IEnumerable<(Hex neighbor, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in EnumArray<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(hexState.Col, hexState.Row, direction);
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


    private void Start()
    {

    }

    public void ToggleSelect()
    {
        Debug.Log("Toggling selection..." + gameObject.name + "...");
        if (hexState.Selected)
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
        hexState.Selected = true;
        ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
    }

    public void NotSelect()
    {
        Debug.Log("Not Selecting..." + gameObject.name + "...");
        hexState.Selected = true;
        ApplyGlow(GameConstants.NOT_SELECTED_HEX_COLOR);
    }

    public void Deselect()
    {
        Debug.Log("Deselecting..." + gameObject.name + "...");
        hexState.Selected = false;
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
                hexState.OriginalMaterialColors[renderer.gameObject] = renderer.material.color;
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
            if (hexState.OriginalMaterialColors.ContainsKey(renderer.gameObject))
            {
                Debug.Log("Restoring original material..." + renderer.gameObject.name + "...");
                renderer.material.color = hexState.OriginalMaterialColors[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }

        foreach (GameObject obj in objectsToRemove)
        {
            hexState.OriginalMaterialColors.Remove(obj);
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
            if(neighbor != null && neighbor.hexState.Selected)
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
        public int Col { get; set; } = 0;
        public int Row { get; set; } = 0;
        public string HexType { get; set; } = "none";
        public string HexSubType { get; set; } = "none";
        public int Rotation { get; set; } = 0;
        public int? HexNum { get; set; } = null;
        public string GroupID { get; set; } = "1";
        public bool Selected { get; set; } = false;
        //public Dictionary<GameObject, Color> OriginalMaterialColors { get; set; }
        //public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges { get; set; }
        //public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners { get; set; }
    }
 
    //TODO - START HERE NEED TO PASS IN hexSpawnerState in a constructor
    public class HexState : IHexState
    {

        private HexExtensions.HexExtensions.Hex _hex;
        private bool isHandlingEvent = false;
        private HexSpawnerState _hexSpawnerState;
        public delegate void HexUpdatedHandler(HexState hexState);
        public event HexUpdatedHandler onHexUpdated;
         public HexState(HexSpawnerState hexSpawnerState)
        {
            _hexSpawnerState = hexSpawnerState;
            _hexSpawnerState.OnHexesChanged += HandleHexesChanged;
        
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

        // This method will be called when the event is triggered
        private void HandleHexesChanged(List<List<Hex.BaseHexState>> hexes)
        {
            isHandlingEvent = true;
            // Loop through each list of hexes
            foreach (var hexList in hexes)
            {
                // Loop through each hex in the list
                foreach (var hex in hexList)
                {
                    // Check if the column and row match the current HexState instance
                    if (hex.Col == this.Col && hex.Row == this.Row)
                    {
                        // Update the HexState based on the new hex
                        this.HexType = hex.HexType;
                        this.HexSubType = hex.HexSubType;
                        this.Rotation = hex.Rotation;
                        this.HexNum = hex.HexNum;
                        this.GroupID = hex.GroupID;
                        this.Selected = hex.Selected;
                    }
                }
            }
            isHandlingEvent = false;
        }

        [ShowInInspector] public HexExtensions.HexExtensions.Hex PositionDataHex
        {
            get { return _hex; }
            set
            {
                _hex = value;
                Col = CFromHex(_hex);
                Row = RFromHex(_hex);
                if(isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            }
        }
        
        //public int col { get { return this.col; } set { this.col = value; hex = CRToHex(col, row); } }
        /*
        private int _col
        {
            get {return hexSpawnerState.hexes[_col][_row].Col;}
            set { hexSpawnerState.hexes[_col][_row].Col=value;}
        }
        */        
        [ShowInInspector] public int Col
        {
            get {return _hexSpawnerState.hexes[Col][Row].Col;}
            set
            {
                _hex = CRToHex(Col, Row);
                if(isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            }
        }
        //public int row { get { return this.row; } set { this.row = value; hex = CRToHex(col, row); } }
        /*
        private int _row
        {
            get {return hexSpawnerState.hexes[Col][_row].Row;}
            set { hexSpawnerState.hexes[_col][_row].Row=value;}
        }   
        */
        [ShowInInspector] public int Row
        {
            get {return _hexSpawnerState.hexes[Col][Row].Row;}
            set
            {
                _hex = CRToHex(Col, Row);;
                if(isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            }
        }

        public string HexType
        {
            get {return _hexSpawnerState.hexes[Col][Row].HexType;}
            set
            {
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }

            }
        }
        public string HexSubType
        {
            get { return _hexSpawnerState.hexes[Col][Row].HexSubType; }
            set
            {
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            
            }
        }
        public int Rotation
        {
            get { return _hexSpawnerState.hexes[Col][Row].Rotation;}
            set
            {
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            }
        }
        public int? HexNum
        {
            get { return _hexSpawnerState.hexes[Col][Row].HexNum; }
            set{
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            
            }
        }
        public string GroupID
        {
            get { return _hexSpawnerState.hexes[Col][Row].GroupID; }
            set {
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }

            }
        }
        public bool Selected
        {
            get { return _hexSpawnerState.hexes[Col][Row].Selected; }
            set {
                if (!isHandlingEvent)
                {
                    onHexUpdated?.Invoke(this);
                }
            }
        
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