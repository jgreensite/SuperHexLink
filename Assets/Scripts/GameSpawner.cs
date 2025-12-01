using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SirenixSerializationUtility = Sirenix.Serialization.SerializationUtility;
using UnityEngine;
using SuperHexLink.Logging;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameSpawner : SpawnerBase
{

    [ShowInInspector]
    [OdinSerialize]
    private GameSpawnerState SerializedState { get; set; }

    public GameSpawnerState State
    {
        get => SerializedState;
        set => SerializedState = value;
    }

    //public override GameSpawnerState State { set => throw new NotImplementedException(); }

    [SerializeField]
    private HexSpawner hexSpawner;
    [SerializeField]
    private EdgeSpawner edgeSpawner;
    [SerializeField]
    private CornerSpawner cornerSpawner;
    [SerializeField]
    private ActionLogSettings actionLogSettings;

    [BoxGroup("Save Map")]
    [HorizontalGroup("Save Map/Path", 0.75f)]
    [LabelText("Save map file")]
    [Sirenix.OdinInspector.FilePath(ParentFolder = "data/maps", Extensions = "json")]
    public string saveMapPath = "./data/maps/map.json";

    [BoxGroup("Save Map")]
    [HorizontalGroup("Save Map/Path", 0.25f)]
    [HideLabel]
    [Button("Browse", ButtonSizes.Small)]
    public void BrowseSaveMapPath()
    {
#if UNITY_EDITOR
        BrowseMapFile(ref saveMapPath, true);
#endif
    }

    [BoxGroup("Save Map")]
    [Button("Save to selected path", ButtonSizes.Large)]
    public void SaveToConfiguredPath()
    {
        SaveHexes(saveMapPath);
    }

    [BoxGroup("Load Map")]
    [HorizontalGroup("Load Map/Path", 0.75f)]
    [LabelText("Load map file")]
    [Sirenix.OdinInspector.FilePath(ParentFolder = "data/maps", Extensions = "json")]
    public string loadMapPath = "./data/maps/map.json";

    [BoxGroup("Load Map")]
    [HorizontalGroup("Load Map/Path", 0.25f)]
    [HideLabel]
        [Button("Browse", ButtonSizes.Small)]
    public void BrowseLoadMapPath()
        {
    #if UNITY_EDITOR
            BrowseMapFile(ref loadMapPath, false);
    #endif
        }

    [BoxGroup("Load Map")]
    [Button("Load from selected path", ButtonSizes.Large)]
    public void LoadFromConfiguredPath()
        {
            LoadState(loadMapPath);
        }

    public void Awake()
    {
        // Ensure State is initialized to avoid null reference exceptions
        if (SerializedState == null)
        {
            SerializedState = new GameSpawnerState();
        }
        hexSpawner = GameObject.Find("HexSpawner").GetComponent<HexSpawner>();
        edgeSpawner = GameObject.Find("EdgeSpawner").GetComponent<EdgeSpawner>();
        cornerSpawner = GameObject.Find("CornerSpawner").GetComponent<CornerSpawner>();
    }
    
    public override void BuildMe(bool isRefresh)
    {
        hexSpawner.BuildMe(isRefresh);
        edgeSpawner.BuildMe(isRefresh);
        cornerSpawner.BuildMe(isRefresh);

        AssociateElements();
        AdjustCameraPosition();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "GameSpawner BuildMe isRefresh={0}", isRefresh);
    }

    [Button("Spawn All Game Elements")]
    public override void Spawn()
    {
        hexSpawner.Spawn();
        edgeSpawner.Spawn();
        cornerSpawner.Spawn();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "GameSpawner Spawn triggered");
    }

    [Button("Clear All Game Elements")]
    public override void Clear()
    {
        hexSpawner.Clear();
        edgeSpawner.Clear();
        cornerSpawner.Clear();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "GameSpawner Clear executed");
    }

    [Button("Refresh All Game Elements")]
    public override void Refresh()
    {
        hexSpawner.Refresh();
        edgeSpawner.Refresh();
        cornerSpawner.Refresh();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "GameSpawner Refresh executed");
    }

    public void SaveHexes(string filePath = null)
    {
        string targetMapPath = ResolveMapPath(filePath, saveMapPath);
        string directory = Path.GetDirectoryName(targetMapPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = ".";
        }

        _ = Directory.CreateDirectory(directory);

        CombinedSpawnerState spawnerStates = new()
        {
            GameState = State,
            HexState = hexSpawner.State,
            EdgeState = edgeSpawner.State,
            CornerState = cornerSpawner.State
        };

        byte[] bytes0 = SirenixSerializationUtility.SerializeValue(spawnerStates, DataFormat.JSON);
        File.WriteAllBytes(targetMapPath, bytes0);
        byte[] bytes1 = SirenixSerializationUtility.SerializeValue(State, DataFormat.JSON);
        File.WriteAllBytes(Path.Combine(directory, "0_gameSpawnerState.json"), bytes1);
        byte[] bytes2 = SirenixSerializationUtility.SerializeValue(hexSpawner.State, DataFormat.JSON);
        File.WriteAllBytes(Path.Combine(directory, "1_hexSpawnerState.json"), bytes2);
        byte[] bytes3 = SirenixSerializationUtility.SerializeValue(edgeSpawner.State, DataFormat.JSON);
        File.WriteAllBytes(Path.Combine(directory, "2_edgeSpawnerState.json"), bytes3);
        byte[] bytes4 = SirenixSerializationUtility.SerializeValue(cornerSpawner.State, DataFormat.JSON);
        File.WriteAllBytes(Path.Combine(directory, "3_cornerSpawnerState.json"), bytes4);
    }

    public void LoadState(string filePath = null)
    {
        string targetMapPath = ResolveMapPath(filePath, loadMapPath);
        if (!File.Exists(targetMapPath))
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning, "LoadState could not find file {0}", targetMapPath);
            return;
        }

        byte[] bytes = File.ReadAllBytes(targetMapPath);

        // Check for legacy format (old files have "HexSpawnerState" as root, new have "CombinedSpawnerState")
        string jsonPreview = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 500));
        if (jsonPreview.Contains("\"$type\": \"HexSpawnerState") || jsonPreview.Contains("\"$type\": \"0|HexSpawnerState"))
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                "LoadState failed: File {0} uses legacy format (HexSpawnerState). Please re-save using the current version.",
                targetMapPath);
            return;
        }

        CombinedSpawnerState spawnerStates;
        try
        {
            spawnerStates = SirenixSerializationUtility.DeserializeValue<CombinedSpawnerState>(bytes, DataFormat.JSON);
        }
        catch (Exception ex)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                "LoadState failed to deserialize {0}: {1}", targetMapPath, ex.Message);
            return;
        }

        if (spawnerStates == null)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning, "LoadState could not deserialize state from {0}", targetMapPath);
            return;
        }

        // Validate that hex state has usable data
        if (spawnerStates.HexState?.hexes == null)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                "LoadState failed: File {0} has no valid hex data. The file may be corrupted or in an incompatible format.",
                targetMapPath);
            return;
        }

        State = spawnerStates.GameState ?? new GameSpawnerState();
        HexGridConfig gridConfig = State.hexGridConfig;
        HexStateRepairReport repairReport = hexSpawner.RepairLoadedState(gridConfig, spawnerStates.HexState, CS);
        if (repairReport.HasChanges)
        {
            Log(ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                "LoadState repaired hex data (created {0}, coords {1}, defaults {2}, rotation {3}, harbours removed {4}, harbour rotation {5})",
                repairReport.CellsCreated,
                repairReport.CoordinatesFixed,
                repairReport.HexTypesDefaulted,
                repairReport.RotationNormalized,
                repairReport.HarboursRemoved,
                repairReport.HarbourRotationsFixed);
        }

        hexSpawner.State = spawnerStates.HexState ?? new HexSpawner.HexSpawnerState();
        edgeSpawner.State = spawnerStates.EdgeState ?? new EdgeSpawner.EdgeSpawnerState();
        cornerSpawner.State = spawnerStates.CornerState ?? new CornerSpawner.CornerSpawnerState();

        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "LoadState applying saved data from {0}", targetMapPath);
        BuildMe(true);
        hexSpawner.UpdateHexes();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "LoadState finished");
    }


    private void AssociateElements()
    {
        // Implement logic to associate Hexes with their respective Edges and Corners
    }

    private void AdjustCameraPosition()
    {
        // Logic to adjust the camera to fit the game board
    }

    [Serializable]
    public class GameSpawnerState
    {
        [SerializeField] public HexGridConfig hexGridConfig;
        [TableList(ShowIndexLabels = true)]
        [OdinSerialize]
        public List<LandConfig> landConfigs = new();
        [TableList(ShowIndexLabels = true)]
        [OdinSerialize]
        public List<NumConfig> numConfigs = new();

        public GameSpawnerState()
        {
            hexGridConfig = HexGridConfig.CreateDefault();
        }
    }

    public class LandConfig
    {
        public string landGroupID;
        public int landCnt;
        public string landType;
    }

    public class NumConfig
    {
        public string numGroupID;
        public int numCnt;
        public int numType;
    }

    private void Log(ActionLogCategory category, ActionLogSeverity severity, string message, params object[] args)
    {
        ActionLogger.Log(actionLogSettings, category, severity, message, args);
    }

    private static string ResolveMapPath(string providedPath, string fallbackPath)
    {
        string pathToUse = string.IsNullOrWhiteSpace(providedPath) ? fallbackPath : providedPath;
        if (string.IsNullOrWhiteSpace(pathToUse))
        {
            return pathToUse;
        }

        // If already an absolute path or starts with ./, return as-is
        if (Path.IsPathRooted(pathToUse) || pathToUse.StartsWith("./") || pathToUse.StartsWith(".\\"))
        {
            return pathToUse;
        }

        // Otherwise, assume it's relative to data/maps (Odin FilePath ParentFolder behavior)
        return Path.Combine(".", "data", "maps", pathToUse);
    }

#if UNITY_EDITOR
    private static string GetExistingDirectory(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string fallback = Path.Combine(projectRoot, "data", "maps");
        return Directory.Exists(fallback) ? fallback : projectRoot;
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
        {
            string relative = absolutePath[projectRoot.Length..];
            relative = relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(".", relative);
        }

        return absolutePath;
    }

    private static void BrowseMapFile(ref string path, bool useSaveDialog)
    {
        string currentDirectory = GetExistingDirectory(path);
        string initialFileName = Path.GetFileName(path) ?? "map.json";
        string selectedPath = useSaveDialog
            ? EditorUtility.SaveFilePanel("Choose save location", currentDirectory, initialFileName, "json")
            : EditorUtility.OpenFilePanel("Choose map to load", currentDirectory, "json");

        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            path = ToProjectRelativePath(selectedPath);
        }
    }
#endif

    [Serializable]
    public class CombinedSpawnerState
    {
        public GameSpawnerState GameState;
        public HexSpawner.HexSpawnerState HexState;
        public EdgeSpawner.EdgeSpawnerState EdgeState;
        public CornerSpawner.CornerSpawnerState CornerState;
    }
}
