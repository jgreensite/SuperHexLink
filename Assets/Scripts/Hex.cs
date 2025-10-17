using System;
using System.Collections.Generic;
using HexExtensions;
using SimpleHexExtensions;
using Sirenix.OdinInspector;
using UnityEngine;

public class Hex : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private HexState state = new HexState();

    [ShowInInspector]
    public HexState hexState
    {
        get => state;
        set => state = value;
    }

    private HexSpawner hexSpawner;

    // Bind this MonoBehaviour to a backing state created during load
    public void Initialize(HexSpawner hs, BaseHexState backingState)
    {
        hexSpawner = hs;
        var hsState = hs != null ? hs.State : null;
        hexState = new HexState(hsState, backingState);
    }

    IEnumerable<(Hex neighbor, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in EnumArray<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(state.Col, state.Row, direction);
            yield return (neighbor, direction);
        }
    }

    private void Awake()
    {
        hexSpawner = GameObject.FindObjectOfType<HexSpawner>();
    }

    public void ToggleSelect() => (state.Selected ? Deselect : Select)();

    public void Select()
    {
        state.Selected = true;
        ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
    }

    public void Deselect()
    {
        state.Selected = false;
        RestoreOriginalMaterials();
    }

    private void ApplyGlow(Color color)
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                state.originalMaterialColors[renderer.gameObject] = renderer.material.color;
                renderer.material.color = color;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        List<GameObject> objectsToRemove = new List<GameObject>();
        foreach (Renderer renderer in renderers)
        {
            if (state.originalMaterialColors.ContainsKey(renderer.gameObject))
            {
                renderer.material.color = state.originalMaterialColors[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }
        foreach (GameObject obj in objectsToRemove) state.originalMaterialColors.Remove(obj);
    }

    private List<Renderer> GetAllRenderers(Transform parent)
    {
        List<Renderer> renderers = new List<Renderer>();
        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null) renderers.Add(renderer);
            renderers.AddRange(GetAllRenderers(child));
        }
        return renderers;
    }

    public void UpdateNeighbors()
    {
        foreach (var (neighbor, direction) in NeighborsWithDirection())
            if (neighbor != null && neighbor.state.Selected) neighbor.UpdateEdge(direction.Opposite());
    }

    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
    {
        var edge = Mathf.Floor(Mathf.Abs(GetComponent<MeshRenderer>().material.GetFloat($"_Edge{(int)direction}") - 1));
        GetComponent<MeshRenderer>().material.SetFloat($"_Edge{(int)direction}", edge);
    }

    [System.Serializable]
    public class HexState
    {
        private HexExtensions.HexExtensions.Hex _hex;
        public HexSpawner.HexSpawnerState hexSpawnerState;
        private readonly BaseHexState backingState;

        [ShowInInspector]
        public HexExtensions.HexExtensions.Hex PositionDataHex
        {
            get => _hex;
            set
            {
                _hex = value;
                if (backingState != null)
                {
                    backingState.Col = CFromHex(_hex);
                    backingState.Row = RFromHex(_hex);
                }
            }
        }

        private int _col;
        [ShowInInspector]
        public int Col
        {
            get => backingState != null ? backingState.Col : _col;
            set
            {
                if (backingState != null)
                {
                    backingState.Col = value;
                    _hex = CRToHex(backingState.Col, backingState.Row);
                }
                else
                {
                    _col = value;
                    _hex = CRToHex(_col, _row);
                }
            }
        }

        private int _row;
        [ShowInInspector]
        public int Row
        {
            get => backingState != null ? backingState.Row : _row;
            set
            {
                if (backingState != null)
                {
                    backingState.Row = value;
                    _hex = CRToHex(backingState.Col, backingState.Row);
                }
                else
                {
                    _row = value;
                    _hex = CRToHex(_col, _row);
                }
            }
        }

        public string HexType
        {
            get => backingState != null ? backingState.HexType : null;
            set { if (backingState != null) backingState.HexType = value; }
        }

        public string HexSubType
        {
            get => backingState != null ? backingState.HexSubType : null;
            set { if (backingState != null) backingState.HexSubType = value; }
        }

        public int Rotation
        {
            get => backingState != null ? backingState.Rotation : 0;
            set { if (backingState != null) backingState.Rotation = value; }
        }

        public int? HexNum
        {
            get => backingState != null ? backingState.HexNum : null;
            set { if (backingState != null) backingState.HexNum = value; }
        }

        public string GroupID
        {
            get => backingState != null ? backingState.GroupID : null;
            set { if (backingState != null) backingState.GroupID = value; }
        }

        public bool Selected
        {
            get => backingState != null ? backingState.Selected : false;
            set { if (backingState != null) backingState.Selected = value; }
        }

        public Dictionary<GameObject, Color> originalMaterialColors = new Dictionary<GameObject, Color>();
        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges;
        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners;

        // Legacy constructor
        public HexState()
        {
            this.backingState = new BaseHexState();
            Edges = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection))) RemoveEdgeStructure(direction);
            Corners = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection))) RemoveCornerStructure(direction);
        }

        // Backing-state constructor
        public HexState(HexSpawner.HexSpawnerState hexSpawnerState, BaseHexState backingState)
        {
            this.hexSpawnerState = hexSpawnerState;
            this.backingState = backingState ?? new BaseHexState();
            _hex = CRToHex(this.backingState.Col, this.backingState.Row);
            originalMaterialColors = new Dictionary<GameObject, Color>();
            Edges = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection))) RemoveEdgeStructure(direction);
            Corners = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection))) RemoveCornerStructure(direction);
        }

        public void AddEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { Edges[direction] = true; }
        public void RemoveEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { Edges[direction] = false; }
        public void AddCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction) { Corners[direction] = ""; }
        public void RemoveCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction) { Corners[direction] = ""; }

        public HexExtensions.HexExtensions.Hex CRToHex(int col, int row)
        {
            HexExtensions.HexExtensions.OffsetCoord b = new HexExtensions.HexExtensions.OffsetCoord(col, row);
            HexExtensions.HexExtensions.Hex c = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b);
            return c;
        }

        public int CFromHex(HexExtensions.HexExtensions.Hex h) => HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).col;
        public int RFromHex(HexExtensions.HexExtensions.Hex h) => HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).row;

        public List<HexExtensions.HexExtensions.Hex> Neighbours()
        {
            int i = 0;
            List<HexExtensions.HexExtensions.Hex> neighbours = new List<HexExtensions.HexExtensions.Hex>();
            foreach (HexExtensions.HexExtensions.Hex h in HexExtensions.HexExtensions.Hex.directions)
            {
                var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h);
                if (o.col > 0 && o.row > 0) neighbours.Add(PositionDataHex.Neighbor(i));
                else { var fill = new HexExtensions.HexExtensions.Hex(); fill.Scale(0); neighbours.Add(fill); }
                i++;
            }
            return neighbours;
        }
    }

    [System.Serializable]
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
}
using System;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using HexExtensions;
using SimpleHexExtensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
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

    // Called by spawners when creating a hex to bind it to its backing state container
    public void Initialize(HexSpawner hs, Hex.BaseHexState backingState)
    {
        hexSpawner = hs;
        var hsState = hs != null ? hs.State : null;
        hexState = new HexState(hsState, backingState);
    }

    IEnumerable<(Hex neighbor, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)> NeighborsWithDirection()
    {
        foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in EnumArray<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection>.Values)
        {
            Hex neighbor = hexSpawner.GetNeighborAt(state.Col, state.Row, direction);
            yield return (neighbor, direction);
        }
    }

    private void Awake()
    {
        hexSpawner = GameObject.FindObjectOfType<HexSpawner>();
    }

    public void ToggleSelect()
    {
        if (state.Selected) Deselect(); else Select();
    }

    public void Select()
    {
        state.Selected = true;
        ApplyGlow(GameConstants.SELECTED_HEX_COLOR);
    }

    public void Deselect()
    {
        state.Selected = false;
        RestoreOriginalMaterials();
    }

    private void ApplyGlow(Color color)
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        foreach (Renderer renderer in renderers)
        {
            if (renderer.material.HasProperty("_Color"))
            {
                state.originalMaterialColors[renderer.gameObject] = renderer.material.color;
                renderer.material.color = color;
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        List<Renderer> renderers = GetAllRenderers(transform);
        List<GameObject> objectsToRemove = new List<GameObject>();
        foreach (Renderer renderer in renderers)
        {
            if (state.originalMaterialColors.ContainsKey(renderer.gameObject))
            {
                renderer.material.color = state.originalMaterialColors[renderer.gameObject];
                objectsToRemove.Add(renderer.gameObject);
            }
        }
        foreach (GameObject obj in objectsToRemove) state.originalMaterialColors.Remove(obj);
    }

    private List<Renderer> GetAllRenderers(Transform parent)
    {
        List<Renderer> renderers = new List<Renderer>();
        foreach (Transform child in parent)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null) renderers.Add(renderer);
            renderers.AddRange(GetAllRenderers(child));
        }
        return renderers;
    }

    public void UpdateNeighbors()
    {
        foreach (var (neighbor, direction) in NeighborsWithDirection())
            if (neighbor != null && neighbor.state.Selected) neighbor.UpdateEdge(direction.Opposite());
    }

    public void UpdateEdge(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
    {
        var edge = Mathf.Floor(Mathf.Abs(GetComponent<MeshRenderer>().material.GetFloat($"_Edge{(int)direction}") - 1));
        GetComponent<MeshRenderer>().material.SetFloat(name: $"_Edge{(int)direction}", value: edge);
    }

    [System.Serializable]
    public class HexState
    {
        private HexExtensions.HexExtensions.Hex _hex;
        public HexSpawner.HexSpawnerState hexSpawnerState;
        private readonly Hex.BaseHexState backingState;

        [ShowInInspector] public HexExtensions.HexExtensions.Hex PositionDataHex
        {
            get { return _hex; }
            set
            {
                _hex = value;
                if (backingState != null)
                {
                    backingState.Col = CFromHex(_hex);
                    backingState.Row = RFromHex(_hex);
                }
            }
        }

        private int _col;
        [ShowInInspector] public int Col
        {
            get { return backingState != null ? backingState.Col : _col; }
            set
            {
                if (backingState != null)
                {
                    backingState.Col = value;
                    _hex = CRToHex(backingState.Col, backingState.Row);
                }
                else
                {
                    _col = value;
                    _hex = CRToHex(_col, _row);
                }
            }
        }

        private int _row;
        [ShowInInspector] public int Row
        {
            get { return backingState != null ? backingState.Row : _row; }
            set
            {
                if (backingState != null)
                {
                    backingState.Row = value;
                    _hex = CRToHex(backingState.Col, backingState.Row);
                }
                else
                {
                    _row = value;
                    _hex = CRToHex(_col, _row);
                }
            }
        }

        public string HexType
        {
            get { return backingState != null ? backingState.HexType : null; }
            set { if (backingState != null) backingState.HexType = value; }
        }
        public string HexSubType
        {
            get { return backingState != null ? backingState.HexSubType : null; }
            set { if (backingState != null) backingState.HexSubType = value; }
        }
        public int Rotation
        {
            get { return backingState != null ? backingState.Rotation : 0; }
            set { if (backingState != null) backingState.Rotation = value; }
        }
        public int? HexNum
        {
            get { return backingState != null ? backingState.HexNum : null; }
            set { if (backingState != null) backingState.HexNum = value; }
        }
        public string GroupID
        {
            get { return backingState != null ? backingState.GroupID : null; }
            set { if (backingState != null) backingState.GroupID = value; }
        }
        public bool Selected
        {
            get { return backingState != null ? backingState.Selected : false; }
            set { if (backingState != null) backingState.Selected = value; }
        }

        public Dictionary<GameObject, Color> originalMaterialColors = new Dictionary<GameObject, Color>();
        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool> Edges;
        [ShowInInspector] public Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string> Corners;

        // Legacy constructor
        public HexState()
        {
            this.backingState = new BaseHexState();
            Edges = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection))) RemoveEdgeStructure(direction);
            Corners = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection))) RemoveCornerStructure(direction);
        }

        // Backing-state constructor
        public HexState(HexSpawner.HexSpawnerState hexSpawnerState, Hex.BaseHexState backingState)
        {
            this.hexSpawnerState = hexSpawnerState;
            this.backingState = backingState ?? new Hex.BaseHexState();
            _hex = CRToHex(this.backingState.Col, this.backingState.Row);
            originalMaterialColors = new Dictionary<GameObject, Color>();
            Edges = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection, bool>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection))) RemoveEdgeStructure(direction);
            Corners = new Dictionary<SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection, string>();
            foreach (SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction in Enum.GetValues(typeof(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection))) RemoveCornerStructure(direction);
        }

        public void AddEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { Edges[direction] = true; }
        public void RemoveEdgeStructure(SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction) { Edges[direction] = false; }
        public void AddCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction) { Corners[direction] = ""; }
        public void RemoveCornerStructure(SimpleHexExtensions.SimpleHexExtensions.HexVertexDirection direction) { Corners[direction] = ""; }

        public HexExtensions.HexExtensions.Hex CRToHex(int col, int row)
        {
            HexExtensions.HexExtensions.OffsetCoord b = new HexExtensions.HexExtensions.OffsetCoord(col, row);
            HexExtensions.HexExtensions.Hex c = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, b);
            return (c);
        }
        public int CFromHex(HexExtensions.HexExtensions.Hex h) { return (HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).col); }
        public int RFromHex(HexExtensions.HexExtensions.Hex h) { return (HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h).row); }

        public List<HexExtensions.HexExtensions.Hex> Neighbours()
        {
            int i = 0;
            List<HexExtensions.HexExtensions.Hex> neighbours = new();
            foreach (HexExtensions.HexExtensions.Hex h in HexExtensions.HexExtensions.Hex.directions)
            {
                var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h);
                if (((o.col > 0)) && (o.row > 0)) neighbours.Add(PositionDataHex.Neighbor(i));
                else { var fill = new HexExtensions.HexExtensions.Hex(); fill.Scale(0); neighbours.Add(fill); }
                i++;
            }
            return (neighbours);
        }
    }

    [System.Serializable]
    public class BaseHexState { public int Col; public int Row; public string HexType; public string HexSubType; public int Rotation; public int? HexNum; public string GroupID; public bool Selected; }
}
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

        // Simple backing struct used for serialization of the minimal hex state
        [System.Serializable]
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

            // Simple backing struct used for serialization of the minimal hex state
            [System.Serializable]
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
        }