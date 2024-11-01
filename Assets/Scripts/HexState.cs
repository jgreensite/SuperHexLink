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


[Serializable]
public class HexState : BaseHexState // Inherit from BaseHexState
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
        
            // Property to hold original material colors
            //TODO - This is a hack to get the original material colors, need to find a better way to do this as it is not serializable
            OriginalMaterialColors = new Dictionary<GameObject, Color>();
               
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
        private void HandleHexesChanged(List<List<IHexState>> hexes)
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