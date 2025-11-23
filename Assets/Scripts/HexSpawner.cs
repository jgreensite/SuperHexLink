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
using UnityEngine.Animations;
using SuperHexLink.Logging;

public class HexSpawner : SpawnerBase
{
   
    //Hex Materials
    /*
    public Material forestMaterial;
    public Material pastureMaterial;
    public Material fieldMaterial;
    public Material hillMaterial;
    public Material mountainMaterial;
    public Material desertMaterial;
    public Material mineMaterial;
    public Material seaMaterial;
    public Material goldMaterial;
    */
    //Hex Prefab Types

    [ShowInInspector,OdinSerialize]
    private HexSpawnerState state;

    public HexSpawnerState State
    {
        get { return state; }
        set { state = value; }
    }
    
    public Hex hexPrefab;

    //Hex Model Types
    public HexLandModel hexLandPrefab;
              
    //Hex Text Prefab Types
    public HexText hexTextPrefab;

    [SerializeField]
    private ActionLogSettings actionLogSettings;

    //State of parent object
    [SerializeField]
    private GameSpawner gameSpawner;

    //game constants
    //public GameConstants CS;

    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<GameSpawner.LandConfig> landTypes = new();
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<GameSpawner.NumConfig> numTypes = new();

    private void Awake()
    {
        gameSpawner = GameObject.Find("GameSpawner").GetComponent<GameSpawner>();
        // Ensure state is initialized to avoid null reference exceptions when BuildMe/Clear are called
        if (state == null) state = new HexSpawnerState();

        var grid = gameSpawner?.State?.hexGridConfig;
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
            "HexSpawner awake; GameSpawner={0}, grid={1}x{2}",
            gameSpawner?.name ?? "<missing>", grid?.cols ?? 0, grid?.rows ?? 0);
    }
   
    [Button("Spawn Hexes")]
    public override void Spawn()
    {
        BuildMe(false);
    }
    public override void BuildMe(bool isRefresh)
    // builds the 
    // "odd-q" vertical layout shoves odd columns down
    // see https://www.redblobgames.com/grids/hexagons/ for more information
    {
    // Build the list of available lands and numbers we can choose from
    BuildTypes();

    // Preserve the loaded hex state when refreshing so we don't drop the saved layout.
    List<List<Hex.HexState>> preservedHexes = null;
    if (isRefresh && state != null && state.hexes != null)
    {
        preservedHexes = state.hexes;
    }

    // Always clear any existing hex GameObjects before (re)building.
    // This prevents duplicate/stacked hexes when BuildMe is called multiple times (e.g. Load -> BuildMe(true)).
    Clear();
    
    if (isRefresh)
    {
        state.hexes = preservedHexes ?? new List<List<Hex.HexState>>();
    }
    else
    {
        state.hexes = new System.Collections.Generic.List<System.Collections.Generic.List<Hex.HexState>>();
    }

        // Now based on the dimensions of the gameboard which may have changed since the last time we called this
        // build the hex and text associated with the hex GameObjects
        // Note that only if we are not refreshing do we assign a land type and the text to the hex
        for (int col = 0; col < gameSpawner.State.hexGridConfig.cols; col++)
        {
            // When refreshing (loading), don't create a new column list - the loaded state already has it
            if (!isRefresh)
            {
                state.hexes.Add(new List<Hex.HexState>());
            }
            
            for (int row = 0; row < gameSpawner.State.hexGridConfig.rows; row++)
            {
                Hex newHex = Instantiate(
                    original: hexPrefab,
                    position: new Vector3(
                        y: UnityEngine.Random.Range(gameSpawner.State.hexGridConfig.minHeight, gameSpawner.State.hexGridConfig.maxHeight),
                        z: -row * gameSpawner.State.hexGridConfig.Apothem * 2 + Get_Z_Offset(col),
                        x: (float)(col * gameSpawner.State.hexGridConfig.radius * 1.5)

                    ),
                    rotation: Quaternion.identity,
                    parent: transform
                );

                newHex.transform.localScale = new Vector3(
                    x: newHex.transform.localScale.x * gameSpawner.State.hexGridConfig.radius,
                    y: newHex.transform.localScale.y * gameSpawner.State.hexGridConfig.height,
                    z: newHex.transform.localScale.z * gameSpawner.State.hexGridConfig.radius
                );
                newHex.name = String.Concat("hex ", col, "_", row);
                newHex.gameObject.layer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEBOARD);

                // When refreshing (loading), use the loaded state directly.
                // When spawning new, initialize the hex's state with position info.
                if (isRefresh)
                {
                    // Verify the loaded state has matching dimensions before accessing
                    if (col < state.hexes.Count && row < state.hexes[col].Count)
                    {
                        // Assign the loaded state from the state array to this hex GameObject
                        newHex.hexState = state.hexes[col][row];
                    }
                    else
                    {
                        Debug.LogWarning($"BuildMe: Loaded state dimensions mismatch at col={col}, row={row}. Expected {gameSpawner.State.hexGridConfig.cols}x{gameSpawner.State.hexGridConfig.rows}, got {state.hexes.Count}x{(col < state.hexes.Count ? state.hexes[col].Count : 0)}");
                        // Initialize with default values if dimensions don't match
                        newHex.hexState.Col = col;
                        newHex.hexState.Row = row;
                        newHex.hexState.HexType = "none";
                    }
                }
                else
                {
                    //set default hex
                    newHex.hexState.GroupID = null;//todo - need to remove this

                    //makes newHex index this object
                    newHex.hexState.Col = col;
                    newHex.hexState.Row = row;

                    // add to the 2 dimensional list of hexes
                    state.hexes[col].Add(newHex.hexState);
                }

                //create a Hexnumber make it a child of the hex just spawned
                HexText newTextHex = Instantiate(
                    original: hexTextPrefab,
                    parent: newHex.transform
                    );
                newTextHex.name = String.Concat("text ", col, "_", row);
                newTextHex.transform.localPosition = new Vector3(
                    x: (float)(newHex.GetComponent<Renderer>().bounds.size.x/2 * -0.25),
                    y: (float)(newHex.GetComponent<Renderer>().bounds.size.y * 1.2),
                    z: (float)(newHex.GetComponent<Renderer>().bounds.size.z/2 * -0.4)
                    );
                newTextHex.gameObject.layer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMETEXT); 

                //When you load a saved game you already know the hex state; only randomize for fresh boards
                if (!isRefresh)
                {
                    RandomizeLand(newHex, false);
                }
                SetLand(newHex);
            }
        }
    }

    private void BuildTypes()
    {
        landTypes.Clear();
        landTypes = gameSpawner.State.landConfigs.Clone();

        numTypes.Clear();
        numTypes = gameSpawner.State.numConfigs.Clone();
    }

    [Button("Update Hexes")]
    //used to update hexes based on what has changed in the HexSpawnerState
    public void UpdateHexes()
    {
        //get all the Hex GameObjects that are children of this HexSpawner
        Hex[] unorderedHexes = FindObjectsOfType<Hex>();
        
        // Filter out hexes with null hexState
        var validHexes = unorderedHexes.Where(h => h != null && h.hexState != null).ToArray();
        if (validHexes.Length == 0)
        {
            Debug.LogWarning("UpdateHexes: No valid hexes found in scene. Spawn hexes first.");
            return;
        }
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "UpdateHexes start with {0} total hexes, {1} valid", unorderedHexes.Length, validHexes.Length);
        
        // Group by Col and order each group by Row
        List<List<Hex>> orderedHexes = validHexes
            .GroupBy(h => h.hexState.Col)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderBy(h => h.hexState.Row).ToList())
            .ToList();

        foreach (Hex h in unorderedHexes)
        {
            List<GameObject> ret = Helpers.GetObjectsInLayer(h.gameObject, LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEMODEL));
            //get all the Hex GameObjects that are children of this Hex
            foreach (GameObject g in ret)
            {
#if UNITY_EDITOR
                DestroyImmediate(g);
#elif !UNITY_EDITOR
                Destroy(g);
#endif
            }
                SetLand(h);
        }
        //Change the camera called "Game Camera" position to the centre of the map pointed at the middle of the hexes and zoom out so all game objects are visible using the number of rows and columns

        //get the  x and z coordinates of the first hex in the list
        float x_start = orderedHexes.First().First().transform.position.x;
        float z_start = orderedHexes.First().First().transform.position.z;
        float x_end = orderedHexes.Last().Last().transform.position.x;
        float z_end = orderedHexes.Last().Last().transform.position.z;
        float x_mid = (x_start + x_end) / 2;
        float z_mid = (z_start + z_end) / 2;
        //make y_mid 2/3 the maximum of x_mid or z_mid, whichever is greater
        float y_mid = 1.1f * Math.Max(Math.Abs(x_mid), Math.Abs(z_mid));
        //log x_mid, y_mid and z_mid to the console
        Debug.Log("x_mid = " + x_mid);
        Debug.Log("y_mid = " + y_mid);
        Debug.Log("z_mid = " + z_mid);

        //reposition the camera to get the whole board in frame
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "Camera target position ({0}, {1}, {2})", x_mid, y_mid, z_mid);
        Camera.allCameras[0].transform.position = new Vector3(x_mid, y_mid, z_mid);
    }

    private void SetLand(Hex h)
    //Sets the land type and number of the hex based on its hexState, used when creating a new hex or refreshing an existing one
    {
        // Null check for hex state first
        if (h == null || h.hexState == null)
        {
            Debug.LogWarning("SetLand: Hex or hex state is null");
            return;
        }
        
        // Verify bounds before copying state from array
        if (h.hexState.Col < state.hexes.Count && h.hexState.Row < state.hexes[h.hexState.Col].Count)
        {
            //copy the hexState to the hex from the master state array
            h.hexState = state.hexes[h.hexState.Col][h.hexState.Row];
        }
        else
        {
            Debug.LogWarning($"SetLand: Hex {h.name} has Col={h.hexState.Col}, Row={h.hexState.Row} which is out of bounds for state.hexes ({state.hexes.Count}x{(h.hexState.Col < state.hexes.Count ? state.hexes[h.hexState.Col].Count : 0)})");
            return;
        }
        
        // Get the HexType value from the hex state
        string hexType = h.hexState.HexType;
        Log(ActionLogCategory.HexLand, ActionLogSeverity.Info,
            "SetLand start {0} type={1} at {2}_{3}",
            h.name, hexType, h.hexState.Col, h.hexState.Row);
        
        // Safely resolve the material from the IoC container based on the HexType value
        Material material = null;
        if (!string.IsNullOrEmpty(hexType) && CS.materialMap.ContainsKey(hexType))
        {
            material = CS.materialMap[hexType];
        }
        
        // Set the material and visibility of the mesh renderer based on the material and HexType values
        if (material != null)
        {
            h.GetComponent<Renderer>().material = material;
            h.GetComponent<MeshRenderer>().enabled = true;

            Log(ActionLogCategory.HexLand, ActionLogSeverity.Info,
                "Applied material for {0}", hexType);

            if (hexType == GameConstants.CAR_TYPE_SEA || hexType == GameConstants.CAR_TYPE_HARBOUR)
            {
                // Make land a little lower for sea and harbour hex types
                double yNew = hexPrefab.transform.localScale.y * gameSpawner.State.hexGridConfig.height * 0.95;
                h.transform.localScale = new Vector3(h.transform.localScale.x, (float)yNew, h.transform.localScale.z);
            }
            
            else{
                // Make land a standard height for all other hex types
                double yNew = hexPrefab.transform.localScale.y * gameSpawner.State.hexGridConfig.height * 1.0;
                h.transform.localScale = new Vector3(h.transform.localScale.x, (float)yNew, h.transform.localScale.z);
            }            
        }
        else
        {
            // Hide the mesh renderer if no material is found for the HexType value
            h.GetComponent<Renderer>().material = null;
            h.GetComponent<MeshRenderer>().enabled = false;
            Log(ActionLogCategory.HexLand, ActionLogSeverity.Warning,
                "{0} @ Col: {1} Row: {2} has HexType '{3}' with no material mapping",
                h.name, h.hexState.Col, h.hexState.Row, hexType);
            Debug.LogWarning(string.Format("{0} @ Col: {1} Row: {2} has HexType '{3}' with no material mapping", h.name, h.hexState.Col, h.hexState.Row, hexType));
        }

        //Now if the hex should have a land model ontop of it and a number render them
        if (isRenderedType(h.hexState.HexType))
        {
            CleanUpOldLandChildren(h);
            //reset the rotation of the hex
            h.transform.rotation = Quaternion.identity;
            //rotate the hex to the correct rotation
            HexLandModel instantiatedLand = Instantiate(original: hexLandPrefab, parent: h.transform);
            LandModelPosition(instantiatedLand, h);
            FilterLandModelMeshes(instantiatedLand, hexType);
            //set the text of the hex
            SetText(h);
            Log(ActionLogCategory.HexLand, ActionLogSeverity.Info,
                "Rendered model and text for {0}", h.name);
        }
        else
        {
            //if not remove any text that may be there
            var t = h.gameObject.GetComponentInChildren<TextMeshPro>();
            t.text = null;
            t.GetComponent<MeshRenderer>().enabled = false;
            Log(ActionLogCategory.HexLand, ActionLogSeverity.Debug,
                "Skipped model render for {0}", h.name);
        }
    }

    private void LandModelPosition(HexLandModel newHexLandModel, Hex h)
    {
        newHexLandModel.name = String.Concat("landModel ", h.hexState.HexType);
        newHexLandModel.transform.localPosition = (new Vector3(0, 0, 0) + Vector3.Scale(h.GetComponent<MeshFilter>().sharedMesh.bounds.size, Vector3.up));

        //Harbours can only point towards certain land types
        if (h.hexState.HexType != GameConstants.CAR_TYPE_HARBOUR)
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
                        if (isReplaceableLandType(state.hexes[o.col][o.row].HexType))
                        {
                            newHexLandModel.transform.Rotate(Vector3.up, r * inc);
                        foundSuitable = true;
                        }
                    }
                r++;
            }
            if (foundSuitable == false) { Debug.Log(h.hexState.Col + "_" + h.hexState.Col + " Cannot find a suitable rotation"); }
        }
        newHexLandModel.gameObject.layer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEMODEL);
    }

    private void CleanUpOldLandChildren(Hex h)
    {
        List<GameObject> ret = Helpers.GetChildObjectsByName(h.gameObject, h.hexState.HexType, false);
        List<GameObject> sub = Helpers.GetChildObjectsByName(h.gameObject, h.hexState.HexType + "_" + GameConstants.CAR_TYPE_SUB_KEYWORD, true);
        var s = h.hexState.HexType + "_" + GameConstants.CAR_TYPE_SUB_KEYWORD + "_" + h.hexState.HexSubType;
        List<GameObject> lit = Helpers.GetChilObjectLights(h.gameObject);
        ret.RemoveAll((go) => lit.Contains(go));
        sub.RemoveAll((go) => go.name == s);
        ret.RemoveAll((go) => go.GetComponent<TextMeshPro>() != null);
        ret.AddRange(sub);
        Helpers.DestroyObjects(ret);
    }

    private void FilterLandModelMeshes(HexLandModel landModel, string hexType)
    {
        if (landModel == null || string.IsNullOrEmpty(hexType)) return;
        string normalizedType = hexType.ToLowerInvariant();
        foreach (Transform child in landModel.GetComponentsInChildren<Transform>(true))
        {
            if (child == landModel.transform) continue;
            string childName = child.gameObject.name.ToLowerInvariant();
            bool isMatch = childName.StartsWith(normalizedType, StringComparison.OrdinalIgnoreCase);
            child.gameObject.SetActive(isMatch);
        }
    }

    private void SetText(Hex h)
    {
        //assign the number
        if (h == null || h.hexState == null)
        {
            Debug.LogWarning("SetText: Hex or hex state is null");
            return;
        }
        
        var t = h.gameObject.GetComponentInChildren<TextMeshPro>();
        if (t == null)
        {
            // TextMeshPro component should be created when land model is instantiated
            // If missing, the hex prefab or land model may not be properly configured
            if (isNumberedLandType(h.hexState.HexType))
            {
                Debug.LogWarning($"SetText: TextMeshPro component not found on hex {h.name} (Type: {h.hexState.HexType}). Hex prefab may need hexTextPrefab configured.");
                Log(ActionLogCategory.HexLand, ActionLogSeverity.Warning,
                    "Missing TextMeshPro on {0} of Type {1}", h.name, h.hexState.HexType);
            }
            return;
        }
        
        if (isNumberedLandType(h.hexState.HexType))
        {
            t.text = h.hexState.HexNum.ToString();
            t.GetComponent<MeshRenderer>().enabled = true;
            Log(ActionLogCategory.HexLand, ActionLogSeverity.Info,
                "SetText assigned number {0} for {1}", h.hexState.HexNum, h.name);
            
            if ((h.hexState.HexNum == 8) || (h.hexState.HexNum == 6))
            {
                t.color = CS.HIGHEST_PROBABILITY_COLOR;
            } else {
                t.color = CS.LOWEST_PROBABILITY_COLOR;
            }
            
        }
        else
        {
            t.text = null;
            t.GetComponent<MeshRenderer>().enabled = false;
            Log(ActionLogCategory.HexLand, ActionLogSeverity.Debug,
                "SetText cleared visuals for {0}", h.name);
        }
    }

    [Button("Refresh Hexes")]
    public override void Refresh()
    {
        BuildTypes();
        //get all the Hex GameObjects that are children of this HexSpawner
        Hex[] Hexes = FindObjectsOfType<Hex>();
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "Refresh called with {0} scene hexes", Hexes.Length);
        
        if (Hexes.Length == 0)
        {
            Debug.LogWarning("Refresh: No hexes found in scene. Spawn hexes first.");
            return;
        }
        
        foreach (Hex h in Hexes)
        {
            if (h == null || h.hexState == null)
            {
                Debug.LogWarning("Refresh: Skipping hex with null state");
                continue;
            }
            
        //do not randomize if supposed to skip
            if (isReplaceableLandType(h.hexState.HexType))
            {
                RandomizeLand(h, true);
                SetLand(h);
            }
        }
        UpdateHexes();
    }

    [Button("Clear Hexes")]
    public override void Clear()
    {
    List<GameObject> ret = Helpers.GetChildObjectsByName(this.gameObject, true);
    Helpers.DestroyObjects(ret);
    if (state == null) state = new HexSpawnerState();
    state.hexes = new System.Collections.Generic.List<System.Collections.Generic.List<Hex.HexState>>();
    }

    private void Log(ActionLogCategory category, ActionLogSeverity severity, string message, params object[] args)
    {
        ActionLogger.Log(actionLogSettings, category, severity, message, args);
    }


    private bool isConfiguredEmpty(String t)
    {
        if (
            (t == "") || (t == null) ||
            (t == GameConstants.CAR_TYPE_WORD_NULL) || (t == GameConstants.CAR_TYPE_NONE)
            )
        {
            return true;
        }
        else { return false; }
    }

    private bool isReplaceableLandType(String t)
    {
        if (
            (isConfiguredEmpty(t)) || (t == GameConstants.CAR_TYPE_SEA) || (t == GameConstants.CAR_TYPE_HARBOUR)
            )
        {
            return false;
        }
        else { return true; }
    }

    private bool isNumberedLandType(String t)
    {
        if (
            (isConfiguredEmpty(t)) || (t == GameConstants.CAR_TYPE_SEA) || (t == GameConstants.CAR_TYPE_DESERT) || (t == GameConstants.CAR_TYPE_HARBOUR)
            )
        {
            return false;
        }
        else { return true; }
    }

    private bool isRenderedType(String t)
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
        IEnumerable<GameSpawner.LandConfig> types = new List<GameSpawner.LandConfig>();
        List<String> typesAll = new List<String>();
        IEnumerable<GameSpawner.NumConfig> nums = new List<GameSpawner.NumConfig>();
        List<int> numsAll = new List<int>();

        //TODO - This works but is a bit complex, consider simplification
        // if this is a refresh then only "replaceable" lands can be placed
        if (isRefresh)
        {
            //set the land type and remove the land from the list of available lands
            //first make sure to find a random land in the same group
            //TO DO - THIS IS NOT WORKING
            // YOU NEED TO MAKE SURE THAT THE LANDS ARE NOT ALL USED UP

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

        foreach (GameSpawner.LandConfig t in types)
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
            List<GameSpawner.LandConfig> landRemove =
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
            randomLand = GameConstants.CAR_TYPE_WORD_NULL;
            h.hexState.GroupID = "1";
            Debug.Log(string.Format("{0} @ Col: {1} Row: {2} has been assigned a default land as none remain to give to it", h.name, h.hexState.Col, h.hexState.Row));
        }

        //now finally set the hex type
        h.hexState.HexType = randomLand;
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "RandomizeLand assigned {0} (group {1}) at {2}_{3}",
            randomLand, h.hexState.GroupID, h.hexState.Col, h.hexState.Row);
        
        //update the text associated with the hex
        if (isNumberedLandType(h.hexState.HexType))
        {
            //set the land number and remove the number from the list of available numbers
            nums =
                (from n in numTypes
                where ((n.numGroupID == h.hexState.GroupID) &&
                (n.numCnt > 0))
                select n).ToList();

            foreach (GameSpawner.NumConfig n in nums)
            {
                for (int cnt = 0; cnt < n.numCnt; cnt++)
                {
                    numsAll.Add(n.numType);
                }
            }
            indexToRemove = RandomNumber.Between(0, numsAll.Count() - 1);
            if (numsAll.Count() > 0)
            {
                List<GameSpawner.NumConfig> numRemove =
                    (from nt in nums
                    where (
                    (nt.numType == numsAll[indexToRemove]))
                    select nt).ToList();
                numRemove[0].numCnt = numRemove[0].numCnt - 1;
                randomNum = numsAll[indexToRemove];

                h.hexState.HexNum = randomNum;
                Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
                    "RandomizeLand picked number {0} for {1}_{2}",
                    randomNum, h.hexState.Col, h.hexState.Row);
            }
        }
        else { h.hexState.HexNum = null; }
    }

    //private float Get_X_Offset(int row) => row % 2 == 0 ? hexGrid.radius * 1.5f : 0f;
    private float Get_Z_Offset(int col) => col % 2 == 0 ? gameSpawner.State.hexGridConfig.Apothem * 1.0f : 0f;

    //checks to see if the Hex is on the board
    public bool isOnBoardHex(HexExtensions.HexExtensions.Hex h)
    {

        var o = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, h);
        if ((o.col > -1) && (o.col < gameSpawner.State.hexGridConfig.cols) && (o.row > -1) && (o.row < gameSpawner.State.hexGridConfig.rows))
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

    private Hex GetHexIfInBounds(int row, int col)
    {
        //get all the Hex GameObjects that are children of this HexSpawner
        Hex[] Hexes = FindObjectsOfType<Hex>();
        int idx = col * gameSpawner.State.hexGridConfig.rows + row;
        return gameSpawner.State.hexGridConfig.IsInBounds(row, col) ? Hexes[idx] : null;
    }
  
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

    [Serializable]
    public class HexSpawnerState
    {
        [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<List<Hex.HexState>> hexes = new List<List<Hex.HexState>>();
    }
}