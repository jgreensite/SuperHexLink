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

    // Snapshot service for robust Undo
    private SuperHexLink.Utils.IHexSnapshotService _snapshotService;

    // Undo Snapshot (Serialized JSON)
    // DEV NOTE: We store a full serialized snapshot of the state because Odin's deep-nested 
    // serialization structures (List<List<HexState>>) are not reliably tracked by Unity's native Undo.
    // We snapshot BEFORE edits, and Unity restores this field. OnUndoRedoPerformed then rehydrates the Odin state.
    [SerializeField, HideInInspector]
    private byte[] _undoSnapshot;

    /// <summary>
    /// For testing purposes only. Allows injection of a mock snapshot service.
    /// </summary>
    public void SetSnapshotService(SuperHexLink.Utils.IHexSnapshotService service)
    {
        _snapshotService = service;
    }

    public void CreateUndoSnapshot()
    {
        // Snapshot the current Odin state into a Unity-serialized byte array
        // This allows Unity's native Undo system to capture the deep state
        if (_snapshotService == null) _snapshotService = new SuperHexLink.Utils.HexSnapshotService(); // Fallback for editor mode
        
        if (state != null)
        {
            _undoSnapshot = _snapshotService.CreateSnapshot(state);
            Debug.Log($"[Undo] Created snapshot: {(_undoSnapshot?.Length ?? 0)} bytes");
        }
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.undoRedoPerformed += OnUndoRedoPerformed;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.undoRedoPerformed -= OnUndoRedoPerformed;
#endif
    }

#if UNITY_EDITOR
    private void OnUndoRedoPerformed()
    {
        // If we have a snapshot (restored by Unity Undo), restore the Odin state from it
        if (_undoSnapshot != null && _undoSnapshot.Length > 0)
        {
            if (_snapshotService == null) _snapshotService = new SuperHexLink.Utils.HexSnapshotService();

            var restoredState = _snapshotService.RestoreSnapshot<HexSpawnerState>(_undoSnapshot);
            if (restoredState != null)
            {
                state = restoredState;
                // Unity Undo restores the Visual GameObject state (mesh/materials) to what it was
                // BUT it might not perfectly match the restored master state if references were broken.
                // To be safe and deterministic, we explicitly sync visuals to the restored master state.
                SyncAllVisualsToState();
            }
        }
    }

    private void SyncAllVisualsToState()
    {
        if (state == null || state.hexes == null) return;

        var allHexes = GetAllHexes();
        foreach (var hex in allHexes)
        {
            if (hex == null || hex.hexState == null) continue;

            int col = hex.hexState.Col;
            int row = hex.hexState.Row;

            // Find authoritative state
            if (state.hexes.Count > col && state.hexes[col] != null && 
                state.hexes[col].Count > row)
            {
                var masterHexState = state.hexes[col][row];
                if (masterHexState != null)
                {
                    // Update visual component's state to match restored master state
                    // We copy values to ensure the visual uses the data from the snapshot
                    hex.hexState.HexType = masterHexState.HexType;
                    hex.hexState.HexNum = masterHexState.HexNum;
                    hex.hexState.Rotation = masterHexState.Rotation;
                    hex.hexState.GroupID = masterHexState.GroupID;
                    hex.hexState.Selected = masterHexState.Selected;
                    
                    // Refresh visual appearance (mesh, material, text)
                    RefreshHex(hex);
                }
            }
        }
    }
#endif
    
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

    public HexGridConfig? GridConfig => gameSpawner?.State?.hexGridConfig;

    private HexPlacementRuleEngine placementRuleEngine;

    private HexPlacementRuleEngine GetPlacementRuleEngine()
    {
        if (placementRuleEngine == null)
        {
            placementRuleEngine = new HexPlacementRuleEngine(TryGetHexState, isReplaceableLandType);
        }

        return placementRuleEngine;
    }

    //game constants
    //public GameConstants CS;

    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<GameSpawner.LandConfig> landTypes = new();
    [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<GameSpawner.NumConfig> numTypes = new();
    private List<GameSpawner.LandConfig> harbourConfigs = new();

    private void Awake()
    {
        gameSpawner = GameObject.Find("GameSpawner").GetComponent<GameSpawner>();
        
        // Initialize helpers
        _snapshotService = new SuperHexLink.Utils.HexSnapshotService();

        // Ensure state is initialized to avoid null reference exceptions when BuildMe/Clear are called
        if (state == null) state = new HexSpawnerState();

        // Defensive: Ensure gameSpawner has initialized state with valid grid config
        EnsureValidGameSpawnerState();

        var grid = gameSpawner?.State?.hexGridConfig;
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
            "HexSpawner awake; GameSpawner={0}, grid={1}x{2}",
            gameSpawner?.name ?? "<missing>", grid?.cols ?? 0, grid?.rows ?? 0);

        placementRuleEngine = new HexPlacementRuleEngine(TryGetHexState, isReplaceableLandType);
        
        // Run comprehensive diagnostics on startup
        RunStartupDiagnostics();
    }
    
    /// <summary>
    /// Runs comprehensive diagnostics to help identify setup issues.
    /// </summary>
    private void RunStartupDiagnostics()
    {
        Debug.Log("=== HexSpawner STARTUP DIAGNOSTICS ===");
        
        // Check GameSpawner
        if (gameSpawner == null)
        {
            Debug.LogError("DIAGNOSTIC: GameSpawner is NULL - cannot spawn hexes");
        }
        else
        {
            Debug.Log($"DIAGNOSTIC: GameSpawner found: {gameSpawner.name}");
            if (gameSpawner.State == null)
            {
                Debug.LogError("DIAGNOSTIC: GameSpawner.State is NULL");
            }
            else
            {
                var cfg = gameSpawner.State.hexGridConfig;
                Debug.Log($"DIAGNOSTIC: Grid config: cols={cfg.cols}, rows={cfg.rows}, radius={cfg.radius}, height={cfg.height}");
                if (cfg.cols <= 0 || cfg.rows <= 0)
                {
                    Debug.LogWarning("DIAGNOSTIC: Grid has invalid dimensions (cols or rows <= 0)");
                }
            }
        }
        
        // Check CS (GameConstants)
        if (CS == null)
        {
            Debug.LogError("DIAGNOSTIC: CS (GameConstants) is NULL - materials won't work. Assign it in the Inspector!");
        }
        else
        {
            Debug.Log($"DIAGNOSTIC: GameConstants found: {CS.name}");
            if (CS.materialMap == null)
            {
                Debug.LogWarning("DIAGNOSTIC: CS.materialMap is NULL (might not be initialized yet - OnEnable runs later)");
            }
            else
            {
                Debug.Log($"DIAGNOSTIC: materialMap has {CS.materialMap.Count} entries");
            }
        }
        
        // Check local state
        if (state == null)
        {
            Debug.LogWarning("DIAGNOSTIC: HexSpawner.state is NULL");
        }
        else
        {
            Debug.Log($"DIAGNOSTIC: HexSpawner.state.hexes has {state.hexes?.Count ?? 0} columns");
            if (state.hexes != null && state.hexes.Count > 0)
            {
                int totalHexes = state.hexes.Sum(col => col?.Count ?? 0);
                Debug.Log($"DIAGNOSTIC: Total hexes in state: {totalHexes}");
            }
        }
        
        Debug.Log("=== END DIAGNOSTICS ===");
    }

    /// <summary>
    /// Ensures GameSpawner has a valid state with proper grid config.
    /// Call this before any operation that needs grid dimensions.
    /// </summary>
    private void EnsureValidGameSpawnerState()
    {
        if (gameSpawner == null)
        {
            gameSpawner = GameObject.Find("GameSpawner")?.GetComponent<GameSpawner>();
            if (gameSpawner == null)
            {
                Debug.LogError("HexSpawner: Cannot find GameSpawner in scene!");
                return;
            }
        }

        // Ensure State exists
        if (gameSpawner.State == null)
        {
            gameSpawner.State = new GameSpawner.GameSpawnerState();
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                "EnsureValidGameSpawnerState: Created new GameSpawnerState with defaults");
        }

        // Ensure grid config is valid
        if (gameSpawner.State.hexGridConfig.cols <= 0 || gameSpawner.State.hexGridConfig.rows <= 0)
        {
            gameSpawner.State.hexGridConfig = HexGridConfig.CreateDefault();
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                "EnsureValidGameSpawnerState: Applied default grid config (7x7)");
        }

        // Ensure land configs exist (critical for RandomizeLand)
        if (gameSpawner.State.landConfigs == null || gameSpawner.State.landConfigs.Count == 0)
        {
            gameSpawner.State.landConfigs = GameSpawner.GameSpawnerState.CreateDefaultLandConfigs();
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                $"EnsureValidGameSpawnerState: Applied default land configs ({gameSpawner.State.landConfigs.Count} types)");
        }

        // Ensure num configs exist (critical for RandomizeNum)
        if (gameSpawner.State.numConfigs == null || gameSpawner.State.numConfigs.Count == 0)
        {
            gameSpawner.State.numConfigs = GameSpawner.GameSpawnerState.CreateDefaultNumConfigs();
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                $"EnsureValidGameSpawnerState: Applied default num configs ({gameSpawner.State.numConfigs.Count} numbers)");
        }
    }
   
    [Button("Spawn Hexes")]
    public override void Spawn()
    {
        Debug.Log("=== SPAWN CALLED ===");
        BuildMe(false);
    }
    public override void BuildMe(bool isRefresh)
    // builds the 
    // "odd-q" vertical layout shoves odd columns down
    // see https://www.redblobgames.com/grids/hexagons/ for more information
    {
        Debug.Log($"=== BUILDME START (isRefresh={isRefresh}) ===");
        
        // Defensive: Always ensure valid state before proceeding
        EnsureValidGameSpawnerState();
        
        // Final check after attempting to fix - if still invalid, abort with clear message
        if (gameSpawner?.State?.hexGridConfig.cols <= 0 || gameSpawner?.State?.hexGridConfig.rows <= 0)
        {
            Debug.LogError($"BuildMe FAILED: Could not establish valid grid config. cols={gameSpawner?.State?.hexGridConfig.cols ?? -1}, rows={gameSpawner?.State?.hexGridConfig.rows ?? -1}");
            return;
        }
        
        int cols = gameSpawner.State.hexGridConfig.cols;
        int rows = gameSpawner.State.hexGridConfig.rows;
        Debug.Log($"BuildMe: Grid dimensions validated: {cols}x{rows}");

        // Build the list of available lands and numbers we can choose from
        BuildTypes();

        // Preserve the loaded hex state when refreshing so we don't drop the saved layout.
        List<List<Hex.HexState>> preservedHexes = null;
        if (isRefresh && state != null && state.hexes != null)
        {
            preservedHexes = state.hexes;
            Debug.Log($"BuildMe: Preserving {state.hexes.Count} columns of hex state for refresh");
            if (state.hexes.Count > 0 && state.hexes[0].Count > 0)
            {
                Debug.Log($"BuildMe: First hex in preserved state: HexType='{state.hexes[0][0]?.HexType ?? "NULL"}'");
            }
        }

        // Always clear any existing hex GameObjects before (re)building.
        // This prevents duplicate/stacked hexes when BuildMe is called multiple times (e.g. Load -> BuildMe(true)).
        Clear();
        Debug.Log($"BuildMe: After Clear(), state.hexes has {state?.hexes?.Count ?? 0} columns");
        
        if (isRefresh)
        {
            state.hexes = preservedHexes ?? new List<List<Hex.HexState>>();
            Debug.Log($"BuildMe: Restored preserved hexes - now {state.hexes.Count} columns");
            if (state.hexes.Count > 0 && state.hexes[0].Count > 0)
            {
                Debug.Log($"BuildMe: After restore, first hex HexType='{state.hexes[0][0]?.HexType ?? "NULL"}'");
            }
        }
        else
        {
            state.hexes = new System.Collections.Generic.List<System.Collections.Generic.List<Hex.HexState>>();
            Debug.Log("BuildMe: Created fresh empty hexes list for new spawn");
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
        if (!isRefresh)
        {
            RunReplacementPipeline();
        }
    }

    private void BuildTypes()
    {
        landTypes.Clear();
        landTypes = gameSpawner.State.landConfigs.Clone();

        harbourConfigs = landTypes
            .Where(cfg => string.Equals(cfg.landType, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase))
            .ToList();
        landTypes.RemoveAll(cfg => string.Equals(cfg.landType, GameConstants.CAR_TYPE_HARBOUR, StringComparison.OrdinalIgnoreCase));

        numTypes.Clear();
        numTypes = gameSpawner.State.numConfigs.Clone();
    }

    private int GetHarbourReplacementTarget()
    {
        return harbourConfigs?.Sum(cfg => Math.Max(cfg?.landCnt ?? 0, 0)) ?? 0;
    }

    private void RunReplacementPipeline()
    {
        if (CS == null || gameSpawner == null) return;
        int targetHarbours = GetHarbourReplacementTarget();
        if (targetHarbours <= 0) return;

        var context = new HexReplacementContext(this, actionLogSettings);
        var pipeline = new HexReplacementPipeline();
        pipeline.AddRule(new HarbourReplacementRule(CS, CS.GetPlacementRule(GameConstants.CAR_TYPE_HARBOUR), targetHarbours));
        pipeline.Run(context);
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

    public void RefreshHex(Hex hex)
    {
        if (hex == null)
        {
            return;
        }

        CleanUpOldLandChildren(hex);
        SetLand(hex);
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
        
        // Null check for CS (GameConstants)
        if (CS == null)
        {
            Debug.LogError($"SetLand: CS (GameConstants) is NULL for hex {h.name} - cannot apply materials. Assign GameConstants in the Inspector!");
            return;
        }
        
        // Null check for materialMap (may not be initialized if OnEnable hasn't run)
        if (CS.materialMap == null)
        {
            Debug.LogError($"SetLand: CS.materialMap is NULL for hex {h.name} - GameConstants.OnEnable may not have run yet");
            return;
        }
        
        Debug.Log($"SetLand BEFORE: {h.name} Col={h.hexState.Col} Row={h.hexState.Row} HexType='{h.hexState.HexType}'");
        
        // Verify state array is valid
        if (state == null || state.hexes == null)
        {
            Debug.LogError($"SetLand: state or state.hexes is NULL for hex {h.name}");
            return;
        }
        
        // Verify bounds before copying state from array
        if (h.hexState.Col < state.hexes.Count && h.hexState.Row < state.hexes[h.hexState.Col].Count)
        {
            //copy the hexState to the hex from the master state array
            h.hexState = state.hexes[h.hexState.Col][h.hexState.Row];
            Debug.Log($"SetLand AFTER COPY: {h.name} HexType='{h.hexState.HexType}'");
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

            var directions = HexExtensions.HexExtensions.Hex.directions;
            int targetIndex = Mathf.FloorToInt(h.hexState.Rotation / 60f);
            if (directions.Count > 0)
            {
                targetIndex = ((targetIndex % directions.Count) + directions.Count) % directions.Count;
                if (HasValidHarbourDirection(h, targetIndex))
                {
                    newHexLandModel.transform.Rotate(Vector3.up, targetIndex * 60);
                    foundSuitable = true;
                    h.hexState.Rotation = targetIndex * 60;
                }
                else
                {
                    for (int dirIndex = 0; dirIndex < directions.Count && !foundSuitable; dirIndex++)
                    {
                        if (HasValidHarbourDirection(h, dirIndex))
                        {
                            newHexLandModel.transform.Rotate(Vector3.up, dirIndex * 60);
                            foundSuitable = true;
                            h.hexState.Rotation = dirIndex * 60;
                        }
                    }
                }
            }

            if (!foundSuitable)
            {
                float fallbackRotation = Mathf.Repeat(h.hexState.Rotation, 360f);
                newHexLandModel.transform.Rotate(Vector3.up, fallbackRotation);
                Debug.Log(h.hexState.Col + "_" + h.hexState.Row + " Cannot find a suitable rotation; using stored rotation " + fallbackRotation);
            }
        }
        newHexLandModel.gameObject.layer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEMODEL);
    }

    public bool TryGetHexState(int col, int row, out Hex.HexState hexState)
    {
        hexState = null;
        if (state?.hexes == null) return false;
        if (col < 0 || row < 0) return false;
        if (col >= state.hexes.Count) return false;
        var column = state.hexes[col];
        if (column == null || row >= column.Count) return false;
        hexState = column[row];
        return hexState != null;
    }

    private bool HasValidHarbourDirection(Hex h, int directionIndex)
    {
        if (h == null || h.hexState == null) return false;
        if (gameSpawner?.State?.hexGridConfig == null) return false;

        var directions = HexExtensions.HexExtensions.Hex.directions;
        if (directionIndex < 0 || directionIndex >= directions.Count) return false;

        var baseOffset = new HexExtensions.HexExtensions.OffsetCoord(h.hexState.Col, h.hexState.Row);
        var baseHex = HexExtensions.HexExtensions.OffsetCoord.QoffsetToCube(HexExtensions.HexExtensions.OffsetCoord.ODD, baseOffset);
        var neighborHex = baseHex.Add(directions[directionIndex]);
        var neighborOffset = HexExtensions.HexExtensions.OffsetCoord.QoffsetFromCube(HexExtensions.HexExtensions.OffsetCoord.ODD, neighborHex);

        if (!gameSpawner.State.hexGridConfig.IsInBounds(neighborOffset.row, neighborOffset.col))
        {
            return false;
        }

        if (TryGetHexState(neighborOffset.col, neighborOffset.row, out var neighborState))
        {
            return IsHarbourFacingNeighbor(neighborState.HexType);
        }

        return false;
    }

    private bool IsHarbourFacingNeighbor(string neighborType)
    {
        if (CS == null)
        {
            return false;
        }

        return CS.IsHarbourFacingLandType(neighborType);
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
            
            // Fix Z-height (Y-axis in Unity) to prevent z-fighting with the land model
            // Reset local position first then apply offset, or just adjust Y? 
            // Assuming default local pos (0,y,0), we'll bump Y slightly.
            Vector3 currentPos = t.transform.localPosition;
            t.transform.localPosition = new Vector3(currentPos.x, 0.15f, currentPos.z); 

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
        Debug.Log("=== REFRESH START ===");
        
        // Defensive: Ensure valid state before proceeding
        EnsureValidGameSpawnerState();
        
        BuildTypes();
        
        // Debug: Log what landTypes contains
        Debug.Log($"Refresh: After BuildTypes, landTypes has {landTypes?.Count ?? 0} entries:");
        if (landTypes != null)
        {
            foreach (var lt in landTypes)
            {
                Debug.Log($"  - {lt?.landType ?? "NULL"}: count={lt?.landCnt ?? -1}, groupID={lt?.landGroupID ?? "NULL"}");
            }
        }
        Debug.Log($"Refresh: gameSpawner.State.landConfigs has {gameSpawner?.State?.landConfigs?.Count ?? 0} entries");
        
        //get all the Hex GameObjects that are children of this HexSpawner
        Hex[] Hexes = FindObjectsOfType<Hex>();
        Debug.Log($"Refresh: Found {Hexes.Length} hex GameObjects in scene");
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "Refresh called with {0} scene hexes", Hexes.Length);
        
        if (Hexes.Length == 0)
        {
            Debug.LogWarning("Refresh: No hexes found in scene. Spawn hexes first.");
            return;
        }
        
        // Log state status
        Debug.Log($"Refresh: state.hexes has {state?.hexes?.Count ?? 0} columns");
        
        int randomized = 0;
        int skipped = 0;
        foreach (Hex h in Hexes)
        {
            if (h == null || h.hexState == null)
            {
                Debug.LogWarning("Refresh: Skipping hex with null state");
                skipped++;
                continue;
            }
            
            //do not randomize if supposed to skip
            if (isReplaceableLandType(h.hexState.HexType))
            {
                RandomizeLand(h, true);
                SetLand(h);
                randomized++;
            }
            else
            {
                skipped++;
            }
        }
        Debug.Log($"Refresh: Randomized {randomized} hexes, skipped {skipped}");
        
        Debug.Log("Refresh: Calling UpdateHexes...");
        UpdateHexes();
        Debug.Log("=== REFRESH END ===");
    }

    [Button("Clear Hexes")]
    public override void Clear()
    {
        List<GameObject> ret = Helpers.GetChildObjectsByName(this.gameObject, true);
        Helpers.DestroyObjects(ret);
        if (state == null)
        {
            state = new HexSpawnerState();
        }
        state.hexes = new List<List<Hex.HexState>>();
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
        string randomLand = GameConstants.CAR_TYPE_WORD_NULL;
        int randomNum;
        IEnumerable<GameSpawner.LandConfig> types = new List<GameSpawner.LandConfig>();
        List<string> typesAll = new List<string>();
        IEnumerable<GameSpawner.NumConfig> nums = new List<GameSpawner.NumConfig>();
        List<int> numsAll = new();

        // Default GroupID to "1" if null (common case for loaded maps)
        string effectiveGroupID = h.hexState.GroupID ?? "1";

        if (isRefresh)
        {
            types = landTypes
                .Where(t => t.landGroupID == effectiveGroupID && isReplaceableLandType(t.landType) && t.landCnt > 0)
                .ToList();
        }
        else
        {
            types = landTypes.Where(t => t.landCnt > 0).ToList();
        }

        foreach (GameSpawner.LandConfig t in types)
        {
            for (int cnt = 0; cnt < t.landCnt; cnt++)
            {
                typesAll.Add(t.landType);
            }
        }

        var (selectedHexType, selectionRule, usedCandidate, usedFallback, selectionAttempts) = SelectLandType(h, typesAll);
        if (string.IsNullOrEmpty(selectedHexType))
        {
            Debug.LogWarning("RandomizeLand: No land candidates available for " + h.name);
        }

        if (usedFallback)
        {
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                "RandomizeLand falling back to {0} for {1} after {2} attempts ({3})",
                selectedHexType, h.name, selectionAttempts, selectionRule?.ruleName ?? "<rule>");
        }

        randomLand = string.IsNullOrEmpty(selectedHexType) ? GameConstants.CAR_TYPE_WORD_NULL : selectedHexType;

        if (usedCandidate || usedFallback)
        {
            var landEntry = types.FirstOrDefault(lt => string.Equals(lt.landType, randomLand, StringComparison.OrdinalIgnoreCase));
            if (landEntry != null)
            {
                landEntry.landCnt--;
                h.hexState.GroupID = landEntry.landGroupID;
            }
            else if (usedCandidate)
            {
                Debug.LogWarning($"RandomizeLand: missing LandConfig for {randomLand}");
            }
        }
        else
        {
            h.hexState.GroupID = h.hexState.GroupID ?? "1";
        }

        h.hexState.HexType = randomLand;
        Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
            "RandomizeLand assigned {0} (group {1}) at {2}_{3}",
            randomLand, h.hexState.GroupID, h.hexState.Col, h.hexState.Row);

        if (isNumberedLandType(h.hexState.HexType))
        {
            nums = numTypes
                .Where(n => string.Equals(n.numGroupID, h.hexState.GroupID) && n.numCnt > 0)
                .ToList();

            foreach (GameSpawner.NumConfig n in nums)
            {
                for (int cnt = 0; cnt < n.numCnt; cnt++)
                {
                    numsAll.Add(n.numType);
                }
            }

            if (numsAll.Count > 0)
            {
                int indexRemoval = RandomNumber.Between(0, numsAll.Count - 1);
                List<GameSpawner.NumConfig> numRemove = nums
                    .Where(nt => nt.numType == numsAll[indexRemoval])
                    .ToList();
                if (numRemove.Count > 0)
                {
                    numRemove[0].numCnt--;
                    randomNum = numsAll[indexRemoval];

                    h.hexState.HexNum = randomNum;
                    Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
                        "RandomizeLand picked number {0} for {1}_{2}",
                        randomNum, h.hexState.Col, h.hexState.Row);
                }
            }
        }
        else
        {
            h.hexState.HexNum = null;
        }
    }

    private (string hexType, HexPlacementRuleConfig rule, bool usedCandidate, bool usedFallback, int attempts) SelectLandType(Hex h, List<string> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return (GameConstants.CAR_TYPE_WORD_NULL, null, false, false, 0);
        }

        var ruleAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int iteration = 0;
        int maxIterations = Math.Max(candidates.Count * 2, 80);

        while (iteration < maxIterations)
        {
            iteration++;
            int index = RandomNumber.Between(0, candidates.Count - 1);
            string candidate = candidates[index];
            var rule = CS?.GetPlacementRule(candidate);
            if (rule == null)
            {
                return (candidate, null, true, false, iteration);
            }

            if (!ruleAttempts.TryGetValue(candidate, out int attemptCount))
            {
                attemptCount = 0;
            }
            attemptCount++;
            ruleAttempts[candidate] = attemptCount;

            if (GetPlacementRuleEngine().AllowsPlacement(rule, h))
            {
                return (candidate, rule, true, false, attemptCount);
            }

            if (attemptCount >= Math.Max(rule.maxAttemptsBeforeFallback, 1))
            {
                string fallback = string.IsNullOrEmpty(rule.fallbackHexType) ? GameConstants.CAR_TYPE_SEA : rule.fallbackHexType;
                return (fallback, rule, false, true, attemptCount);
            }
        }

        string fallbackType = candidates[iteration % candidates.Count];
        return (fallbackType, CS?.GetPlacementRule(fallbackType), true, false, iteration);
    }

    //private float Get_X_Offset(int row) => row % 2 == 0 ? hexGrid.radius * 1.5f : 0f;
    private float Get_Z_Offset(int col) => col % 2 == 0 ? gameSpawner.State.hexGridConfig.Apothem * 1.0f : 0f;

    public List<Hex> GetAllHexes()
    {
        var hexes = new List<Hex>();
        foreach (Transform child in transform)
        {
            var hex = child.GetComponent<Hex>();
            if (hex != null)
            {
                hexes.Add(hex);
            }
        }
        return hexes;
    }

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

    public Hex TryGetHexAt(int col, int row)
    {
        // Iterate through child transforms which is safer/faster than global FindObjectsOfType if hexes are children
        foreach (Transform child in transform)
        {
            var hex = child.GetComponent<Hex>();
            // Null checks for safety
            if (hex != null && hex.hexState != null && 
                hex.hexState.Col == col && hex.hexState.Row == row)
            {
                return hex;
            }
        }
        return null;
    }

    public HexStateRepairReport RepairLoadedState(HexGridConfig gridConfig, HexSpawnerState loadedState, GameConstants constants)
    {
        return HexStateRepairer.Repair(loadedState, gridConfig, constants);
    }

    [Serializable]
    public class HexSpawnerState
    {
        [TableList(ShowIndexLabels = true)]
        [OdinSerialize]
        public List<List<Hex.HexState>> hexes = new();
    }
}