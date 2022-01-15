using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using AnyClone;
using FDL.Library.Numeric;
using Script;
using TMPro;
using SimpleHexExtensions;
using HexExtensions;

public class HexSpawner : SerializedMonoBehaviour
{

    [SerializeField, HideInInspector]
    public HexSpawnerState state = new HexSpawnerState();

    [ShowInInspector]
    public HexSpawnerState hexState

    {
        get { return this.state; }
        set { this.state = value; }
    }

    //Hex Materials
    public Material forestMaterial;
    public Material pastureMaterial;
    public Material fieldMaterial;
    public Material hillMaterial;
    public Material mountainMaterial;
    public Material desertMaterial;
    public Material mineMaterial;
    public Material seaMaterial;
    public Material goldMaterial;

    //Hex Prefab Types
    public Hex hexPrefab;

    //Hex Model Types
    public HexLandModel hexLandPrefab;
    /*
    public HexLandModel hexPasturePrefab;
    public HexLandModel hexFieldPrefab;
    public HexLandModel hexHillPrefab;
    public HexLandModel hexMountainPrefab;
    public HexLandModel hexDesertPrefab;
    public HexLandModel hexMinePrefab;
    public HexLandModel hexSeaPrefab;
    */
              
    //Hex Text Prefab Types
    public HexText hexTextPrefab;

    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<landConfig> landTypes = new List<landConfig>();
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<numConfig> numTypes = new List<numConfig>();

    [Button("Spawn Hexes")]
    private void SpawnHexes()
    {
        BuildHexes(false);
    }

    private void BuildHexes(bool isRefresh)
    {
        //Build the list of lands
        BuildTypes();

        // “odd-q” vertical layout shoves odd columns down
        // see https://www.redblobgames.com/grids/hexagons/ for more information
        if (state.hexes.Count > 0)
            ClearHexes();

        for (int col = 0; col < state.hexGrid.cols; col++)
        {
            state.hexes.Add(new List<Hex>());
            for (int row = 0; row < state.hexGrid.rows; row++)
            {
                Hex newHex = Instantiate(
                    original: hexPrefab,
                    position: new Vector3(
                        y: UnityEngine.Random.Range(state.hexGrid.minHeight, state.hexGrid.maxHeight),
                        z: -row * state.hexGrid.Apothem * 2 + Get_Z_Offset(col),
                        x: (float)(col * state.hexGrid.radius * 1.5)

                    ),
                    rotation: Quaternion.identity,
                    parent: transform
                );

                newHex.transform.localScale = new Vector3(
                    x: newHex.transform.localScale.x * state.hexGrid.radius,
                    y: newHex.transform.localScale.y * state.hexGrid.height,
                    z: newHex.transform.localScale.z * state.hexGrid.radius
                );
                newHex.name = String.Concat("hex ", col, "_", row);
                newHex.gameObject.layer = LayerMask.NameToLayer(CS.OBJ_LOCATION_LAYER_GAMEBOARD);

                //set default hex
                newHex.hexState.GroupID = null;//todo - need to remove this

                //makes newHex index this object
                newHex.hexState.col = col;
                newHex.hexState.row = row;

                //create a Hexnumber make it a child of the hex just spawned
                HexText newTextHex = Instantiate(
                    original: hexTextPrefab,
                    parent: newHex.transform
                    );
                newTextHex.name = String.Concat("text ", col, "_", row);
                newTextHex.transform.localPosition = new Vector3(
                    x: (float)(newHex.GetComponent<Renderer>().bounds.size.x/2 * -0.25),
                    y: (float)(newHex.GetComponent<Renderer>().bounds.size.y * 1.1),
                    z: (float)(newHex.GetComponent<Renderer>().bounds.size.z/2 * -0.4)
                    );
                newTextHex.gameObject.layer = LayerMask.NameToLayer(CS.OBJ_LOCATION_LAYER_GAMETEXT); 

                // add to the 2 dimensional list of hexes
                state.hexes[col].Add(newHex);

                if (!isRefresh)
                {
                    //make the hex a piece in the game
                    RandomizeLand(newHex, false);
                    SetLand(newHex);
                }
            }
        }
    }

    private void BuildTypes()
    {
        landTypes.Clear();
        landTypes = state.landConfigs.Clone();

        numTypes.Clear();
        numTypes = state.numConfigs.Clone();
    }

    [Button("Update Hexes")]
    //used to update hexes based on what has been edited in the inspector
    private void UpdateHexes()
    {
        foreach (List<Hex> c in state.hexes)
        {
            foreach (Hex r in c)
            {
                List<GameObject> ret = Helpers.GetObjectsInLayer(r.gameObject, LayerMask.NameToLayer(CS.OBJ_LOCATION_LAYER_GAMEMODEL));
                foreach (GameObject g in ret)
                {
#if UNITY_EDITOR
                    DestroyImmediate(g);
#elif !UNITY_EDITOR
                    Destroy(g);
#endif
                }
                SetLand(r);
            }
        }
    }

    private void SetLand(Hex h)
    {
        switch (h.hexState.HexType)
        {
            case CS.CAR_TYPE_FOREST:
                h.GetComponent<Renderer>().material = forestMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_PASTURE:
                h.GetComponent<Renderer>().material = pastureMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_FIELD:
                h.GetComponent<Renderer>().material = fieldMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_HILL:
                h.GetComponent<Renderer>().material = hillMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_MOUNTAIN:
                h.GetComponent<Renderer>().material = mountainMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_MINE:
                h.GetComponent<Renderer>().material = mineMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_SEA:
                h.GetComponent<Renderer>().material = seaMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                //make land a little lower
                double yNewS = hexPrefab.transform.localScale.y * state.hexGrid.height * 0.95;
                h.transform.localScale = new Vector3(h.transform.localScale.x, (float)yNewS, h.transform.localScale.z);
                break;
            case CS.CAR_TYPE_HARBOUR:
                h.GetComponent<Renderer>().material = seaMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                //make land a little lower
                double yNewH = hexPrefab.transform.localScale.y * state.hexGrid.height * 0.95;
                h.transform.localScale = new Vector3(h.transform.localScale.x, (float)yNewH, h.transform.localScale.z);
                break;
            case CS.CAR_TYPE_DESERT:
                h.GetComponent<Renderer>().material = desertMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_GOLD:
                h.GetComponent<Renderer>().material = goldMaterial;
                h.GetComponent<MeshRenderer>().enabled = true;
                break;
            case CS.CAR_TYPE_NONE:
                h.GetComponent<Renderer>().material = null;
                h.GetComponent<MeshRenderer>().enabled = false;
                break;
            case CS.CAR_TYPE_WORD_NULL:
                h.GetComponent<Renderer>().material = null;
                h.GetComponent<MeshRenderer>().enabled = false;
                break;
            default:
                h.GetComponent<Renderer>().material = null;
                h.GetComponent<MeshRenderer>().enabled = false;
                Debug.Log(string.Format("{0} @ Col: {1} Row: {2} has unknown material", h.name, h.hexState.col, h.hexState.row));
                break;
        }

        //Now if the hex should have a land model ontop of it and a number render them
        if (isRenderedLandType(h.hexState.HexType))
        {
            LandModelPosition(Instantiate(original: hexLandPrefab, parent: h.transform), h);
            SetText(h);
        }
    }

    private void LandModelPosition(HexLandModel newHexLandModel, Hex h)
    {
        newHexLandModel.name = String.Concat("landModel ", h.hexState.HexType);
        newHexLandModel.transform.localPosition = (new Vector3(0, 0, 0) + Vector3.Scale(h.GetComponent<MeshFilter>().sharedMesh.bounds.size, Vector3.up));

        //Harbours can only point towards certain land types
        if (h.hexState.HexType != CS.CAR_TYPE_HARBOUR)
        {
            newHexLandModel.transform.Rotate(Vector3.up, h.hexState.Rotation);
        }
        else
        {
            bool foundSuitable = false;
            int r = h.hexState.Rotation/60;
            var neigh = h.hexState.Neighbours();
            int inc = 360 / neigh.Count;

            //first check to see if the rotation given is valid
            if (isOnBoardHex(neigh[r]))
            {
                newHexLandModel.transform.Rotate(Vector3.up, h.hexState.Rotation);
                foundSuitable = true;
            }
            //if not set a new rotation based on first valid point
            r = 0;    
            while ((r < neigh.Count) && (foundSuitable == false))
            {
                var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, neigh[r]);
                if (isOnBoardHex(neigh[r]))
                    {
                        if (isReplaceableLandType(state.hexes[o.col][o.row].hexState.HexType))
                        {
                            newHexLandModel.transform.Rotate(Vector3.up, r * inc);
                        foundSuitable = true;
                        }
                    }
                r++;
            }
            if (foundSuitable == false) { Debug.Log(h.hexState.col + "_" + h.hexState.col + " Cannot find a suitable rotation"); }
        }

        newHexLandModel.gameObject.layer = LayerMask.NameToLayer(CS.OBJ_LOCATION_LAYER_GAMEMODEL);
        //get list of all objects that are not of this type, they all need to be removed
        List<GameObject> ret = Helpers.GetChildObjectsByName(newHexLandModel.gameObject, h.hexState.HexType, false);
        //get list of all subtypes of this object, all not mentioned subtypes need to be removed  
        List<GameObject> sub = Helpers.GetChildObjectsByName(newHexLandModel.gameObject, h.hexState.HexType + "_" + CS.CAR_TYPE_SUB_KEYWORD, true);
        var s = h.hexState.HexType + "_" + CS.CAR_TYPE_SUB_KEYWORD + "_" + h.hexState.HexSubType;
        //Debug.Log(s);
        sub.RemoveAll((go) => go.name == s);
        //merge lists and destroy
        ret.AddRange(sub);
        Helpers.DestroyObjects(ret);
    }

    private void SetText(Hex h)
    {
        //assign the number
        var t = h.gameObject.GetComponentInChildren<TextMeshPro>();
        if (isNumberedLandType(h.hexState.HexType))
        {
            t.text = h.hexState.HexNum.ToString();
            t.GetComponent<MeshRenderer>().enabled = true;
        }
        else
        {
            t.text = null;
            t.GetComponent<MeshRenderer>().enabled = false;
        }
    }

    [Button("Refresh Map")]
    private void RefeshHexes()
    {
        BuildTypes();
        foreach (List<Hex> hc in state.hexes)
        {
            foreach (Hex h in hc)
            {
                RefreshLand(h);
            }
        }
        UpdateHexes();
    }

    [Button("Clear Map")]
    private void ClearHexes()
    {
        List<GameObject> ret = Helpers.GetChildObjectsByName(this.gameObject, true);
        Helpers.DestroyObjects(ret);
        state.hexes = new List<List<Hex>>();
    }

    [Button("Save Map")]
    //todo - externalise defauilt value as a constant
    private void SaveHexes(string filePath)
    {
        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/map.json"; //default value
        }
            byte[] bytes = SerializationUtility.SerializeValue(state, DataFormat.JSON);
            File.WriteAllBytes(filePath, bytes);
    }

    [Button("Load Map")]
    public void LoadState(string filePath)
    {
        //TODO - this is not very elegant
        //it would be better if we didn't have to call update hexes and that an event fired automatically
        
        //load the hex data
        HexSpawnerState loadedHexSpawner = new HexSpawnerState();
        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/map.json"; //default value
        }
        if (!File.Exists(filePath)) return; // No state to load

        byte[] bytes = File.ReadAllBytes(filePath);
        loadedHexSpawner = SerializationUtility.DeserializeValue<HexSpawnerState>(bytes, DataFormat.JSON);
        //copy accross hexGrid and landconfig
        state.hexGrid = loadedHexSpawner.hexGrid;
        state.landConfigs = loadedHexSpawner.landConfigs;
        state.numConfigs = loadedHexSpawner.numConfigs;

        //create new gameobjects attached to new hexes based on loaded value, remember not possible to serialise Unity gameobjects
        BuildHexes(true);

        //Copy across hexState from loaded objects to new objects
        for (int col = 0; col < state.hexGrid.cols; col++)
        {
            for (int row = 0; row < state.hexGrid.rows; row++)
            {
                state.hexes[col][row].hexState = loadedHexSpawner.hexes[col][row].hexState;
            }
        }

        //update the hexstate as a result
        UpdateHexes();
    }

    private void RefreshLand(Hex h)
    {
        //do not randomize if supposed to skip
        if (isReplaceableLandType(h.hexState.HexType))
        {
            RandomizeLand(h, true);
        }
    }

    bool isConfiguredEmpty(String t)
    {
        if (
            (t == "") || (t == null) ||
            (t == CS.CAR_TYPE_WORD_NULL) || (t == CS.CAR_TYPE_NONE)
            )
        {
            return true;
        }
        else { return false; }
    }

    bool isReplaceableLandType(String t)
    {
        if (
            (isConfiguredEmpty(t)) || (t == CS.CAR_TYPE_SEA) || (t == CS.CAR_TYPE_HARBOUR)
            )
        {
            return false;
        }
        else { return true; }
    }

    bool isNumberedLandType(String t)
    {
        if (
            (isConfiguredEmpty(t)) || (t == CS.CAR_TYPE_SEA) || (t == CS.CAR_TYPE_DESERT) || (t == CS.CAR_TYPE_HARBOUR)
            )
        {
            return false;
        }
        else { return true; }
    }

    bool isRenderedLandType(String t)
    {
        if (isConfiguredEmpty(t))
        {
            return false;
        }
        else { return true; }
    }

    private void RandomizeLand(Hex h, bool isRefresh)
    {
        int indexToRemove;
        string randomLand;
        int randomNum;
        IEnumerable<landConfig> types = new List<landConfig>();
        List<String> typesAll = new List<String>();
        IEnumerable<numConfig> nums = new List<numConfig>();
        List<int> numsAll = new List<int>();

        //TODO - This works but is a bit complex, consider simplification
        // if this is a refresh then only "replaceable" lands can be placed
        if (isRefresh)
        {
            //set the land type and remove the land from the list of available lands
            //first make sure to find a random land in the same group

            types =
                (from t in landTypes
                where (
                (t.landGroupID == h.hexState.GroupID) &&
                (isReplaceableLandType(t.landType) && (t.landCnt > 0)))
                select t).ToList();
        }
        else {
            types =
                (from t in landTypes
                 where (
                 (t.landCnt > 0))
                 select t).ToList();
        }

        foreach (landConfig t in types)
        {
            for (int cnt = 0; cnt < t.landCnt; cnt++)
            {
                typesAll.Add(t.landType);
            }
        }
        indexToRemove = RandomNumber.Between(0, typesAll.Count() - 1);

        //now having matched on group remove the random land from the list of lands
        if ((typesAll.Count() > 0))
        {
            List<landConfig> landRemove =
                (from lt in types
                 where(
                 (lt.landType == typesAll[indexToRemove]))
                 select lt).ToList();
            landRemove[0].landCnt = landRemove[0].landCnt - 1;
            randomLand = typesAll[indexToRemove];
            //strictly only needed when not refreshing
            h.hexState.GroupID = landRemove[0].landGroupID;
        }
        //if there are no more lands left then make the land a default type
        else {
            randomLand = CS.CAR_TYPE_WORD_NULL;
            h.hexState.GroupID = "1";
            Debug.Log(string.Format("{0} @ Col: {1} Row: {2} has been assigned a default land as none remain to give to it", h.name, h.hexState.col, h.hexState.row));
        }

        //now finally set the hex type
        h.hexState.HexType = randomLand;
        
        //update the text associated with the hex
        if (isNumberedLandType(h.hexState.HexType))
        {
            //set the land number and remove the number from the list of available numbers
            nums =
                (from n in numTypes
                where ((n.numGroupID == h.hexState.GroupID) &&
                (n.numCnt > 0))
                select n).ToList();

            foreach (numConfig n in nums)
            {
                for (int cnt = 0; cnt < n.numCnt; cnt++)
                {
                    numsAll.Add(n.numType);
                }
            }
            indexToRemove = RandomNumber.Between(0, numsAll.Count() - 1);
            if (numsAll.Count() > 0)
            {
                List<numConfig> numRemove =
                    (from nt in nums
                    where (
                    (nt.numType == numsAll[indexToRemove]))
                    select nt).ToList();
                numRemove[0].numCnt = numRemove[0].numCnt - 1;
                randomNum = numsAll[indexToRemove];

                h.hexState.HexNum = randomNum;
            }
        }
        else { h.hexState.HexNum = null; }
    }

    //private float Get_X_Offset(int row) => row % 2 == 0 ? hexGrid.radius * 1.5f : 0f;
    private float Get_Z_Offset(int col) => col % 2 == 0 ? state.hexGrid.Apothem * 1.0f : 0f;

    //checks to see if the Hex is on thee board
    public bool isOnBoardHex(HexExtensions.HexExtensions.Hex h)
    {

        var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h);
        if ((o.col > -1) && (o.col < state.hexGrid.cols) && (o.row > -1) && (o.row < state.hexGrid.rows))
        {
            return (true);
        }
        else
        {
            return (false);
        }
    }

    public Hex GetNeighborAt(int col, int row, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
    {
        (int row, int col) offsets = GetOffsetInDirection(row % 2 == 0, direction);
        return GetHexIfInBounds(row + offsets.row, col + offsets.col);
    }

    private Hex GetHexIfInBounds(int row, int col) =>
        state.hexGrid.IsInBounds(row, col) ? state.hexes[row][col] : null;

    private (int row, int col) GetOffsetInDirection(bool isEven, SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection direction)
    {
        switch(direction)
        {
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.Up:
                return (2, 0);
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.UpRight:
                return isEven ? (1, 1) : (1, 0);
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.DownRight:
                return isEven ? (-1, 1) : (-1, 0);
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.Down:
                return (-2, 0);
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.DownLeft:
                return isEven ? (-1, 0) : (-1, -1);
            case SimpleHexExtensions.SimpleHexExtensions.HexNeighborDirection.UpLeft:
                return isEven ? (1, 0) : (1, -1);
        }
        return (0, 0);
    }
}

[System.Serializable]
public class HexSpawnerState
{
    [SerializeField] public HexGrid hexGrid;
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<List<Hex>> hexes = new List<List<Hex>>();
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<landConfig> landConfigs = new List<landConfig>();
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<numConfig> numConfigs = new List<numConfig>();
}

public class landConfig
{
    public string landGroupID;
    public int landCnt;
    public string landType;
}

public class numConfig
{
    public string numGroupID;
    public int numCnt;
    public int numType;
}