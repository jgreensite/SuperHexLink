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

/// <summary>
/// Central orchestrator for spawning and managing all game board elements.
/// Owns the master <see cref="GameSpawnerState"/> and coordinates hex, edge, and corner spawners.
/// </summary>
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

    [SerializeField]
    private HexSpawner hexSpawner;
    [SerializeField]
    private EdgeSpawner edgeSpawner;
    [SerializeField]
    private CornerSpawner cornerSpawner;
    [SerializeField]
    private ActionLogSettings actionLogSettings;

    [BoxGroup("Save Map")]
    [LabelText("Save to path")]
    public string saveMapPath = "./data/maps/map.json";

    [BoxGroup("Save Map")]
    [HorizontalGroup("Save Map/Buttons")]
    [Button("Browse...", ButtonSizes.Medium)]
    public void BrowseSaveMapPath()
    {
#if UNITY_EDITOR
        BrowseMapFile(ref saveMapPath, true);
#endif
    }

    [BoxGroup("Save Map")]
    [HorizontalGroup("Save Map/Buttons")]
    [Button("Save Map", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void SaveToConfiguredPath()
    {
        SaveHexes(saveMapPath);
        Debug.Log($"Map saved to: {saveMapPath}");
    }

    [BoxGroup("Load Map")]
    [LabelText("Load from path")]
    public string loadMapPath = "./data/maps/map.json";

    [BoxGroup("Load Map")]
    [HorizontalGroup("Load Map/Buttons")]
    [Button("Browse...", ButtonSizes.Medium)]
    public void BrowseLoadMapPath()
    {
#if UNITY_EDITOR
        BrowseMapFile(ref loadMapPath, false);
#endif
    }

    [BoxGroup("Load Map")]
    [HorizontalGroup("Load Map/Buttons")]
    [Button("Load Map", ButtonSizes.Medium)]
    [GUIColor(0.4f, 0.6f, 0.9f)]
    public void LoadFromConfiguredPath()
    {
        LoadState(loadMapPath);
    }

    public void Awake()
    {
        // Ensure State is initialized with valid defaults
        if (SerializedState == null)
        {
            SerializedState = new GameSpawnerState();
        }
        else if (SerializedState.hexGridConfig.cols <= 0 || SerializedState.hexGridConfig.rows <= 0)
        {
            // Existing state has invalid grid config - apply defaults
            SerializedState.hexGridConfig = HexGridConfig.CreateDefault();
        }
        hexSpawner = GameObject.Find("HexSpawner").GetComponent<HexSpawner>();
        edgeSpawner = GameObject.Find("EdgeSpawner").GetComponent<EdgeSpawner>();
        cornerSpawner = GameObject.Find("CornerSpawner").GetComponent<CornerSpawner>();
    }

    /// <summary>
    /// Ensures state has valid grid config and land configs. Call before any operation needing dimensions.
    /// </summary>
    private void EnsureValidState()
    {
        if (SerializedState == null)
        {
            SerializedState = new GameSpawnerState();
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "EnsureValidState: Created new GameSpawnerState");
        }

        if (SerializedState.hexGridConfig.cols <= 0 || SerializedState.hexGridConfig.rows <= 0)
        {
            SerializedState.hexGridConfig = HexGridConfig.CreateDefault();
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "EnsureValidState: Applied default grid config (7x7)");
        }
        
        // Ensure land configs are populated - required for Spawn to work
        if (SerializedState.landConfigs == null || SerializedState.landConfigs.Count == 0)
        {
            SerializedState.landConfigs = GameSpawnerState.CreateDefaultLandConfigs();
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "EnsureValidState: Applied default land configs ({0} types)", SerializedState.landConfigs.Count);
        }
        
        // Ensure num configs are populated - required for hex numbers
        if (SerializedState.numConfigs == null || SerializedState.numConfigs.Count == 0)
        {
            SerializedState.numConfigs = GameSpawnerState.CreateDefaultNumConfigs();
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "EnsureValidState: Applied default num configs ({0} types)", SerializedState.numConfigs.Count);
        }
    }
    
    public override void BuildMe(bool isRefresh)
    {
        EnsureValidState();
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
        EnsureValidState();
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
        EnsureValidState();
        hexSpawner.Refresh();
        edgeSpawner.Refresh();
        cornerSpawner.Refresh();
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "GameSpawner Refresh executed");
    }

    /// <summary>
    /// Saves the current board state to JSON files at the specified (or configured) path.
    /// Also writes individual state files for each spawner for debugging.
    /// </summary>
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

    /// <summary>
    /// Loads a saved map from disk, handling both current and legacy formats.
    /// Deserializes, repairs, and rebuilds the full board.
    /// </summary>
    public void LoadState(string filePath = null)
    {
        string targetMapPath = ResolveMapPath(filePath, loadMapPath);
        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "LoadState: loading from {0}", targetMapPath);

        if (!IsPathSafe(targetMapPath))
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                "LoadState: path rejected (directory traversal detected): {0}", targetMapPath);
            return;
        }

        if (!File.Exists(targetMapPath))
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error, "LoadState: file not found: {0}", targetMapPath);
            return;
        }

        long fileSize = new System.IO.FileInfo(targetMapPath).Length;
        if (fileSize > MaxMapFileSizeBytes)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                "LoadState: file too large ({0:N0} bytes, max {1:N0}): {2}", fileSize, MaxMapFileSizeBytes, targetMapPath);
            return;
        }

        byte[] bytes = File.ReadAllBytes(targetMapPath);
        string jsonContent = System.Text.Encoding.UTF8.GetString(bytes);
        CombinedSpawnerState spawnerStates;

        bool isLegacy = LegacyMapConverter.IsLegacyFormat(jsonContent);

        if (isLegacy)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "LoadState: detected legacy format in {0}, attempting conversion", targetMapPath);

            var conversionResult = LegacyMapConverter.TryConvertLegacyJson(jsonContent);
            if (!conversionResult.Success)
            {
                Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "LoadState: legacy conversion failed for {0}: {1}", targetMapPath, conversionResult.ErrorMessage);
                return;
            }

            spawnerStates = conversionResult.ConvertedState;
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
                "Legacy conversion complete: {0}", conversionResult.Report);

            foreach (string warning in conversionResult.Report.Warnings)
            {
                Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning, "Legacy conversion warning: {0}", warning);
            }
        }
        else
        {
            try
            {
                spawnerStates = SirenixSerializationUtility.DeserializeValue<CombinedSpawnerState>(bytes, DataFormat.JSON);
            }
            catch (Exception ex)
            {
                Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "LoadState: deserialization failed for {0}: {1}", targetMapPath, ex.Message);
                return;
            }

            if (spawnerStates == null)
            {
                Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "LoadState: deserialized null from {0}", targetMapPath);
                return;
            }

            if (spawnerStates.HexState?.hexes == null)
            {
                Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Error,
                    "LoadState: no hex data in {0} — file may be corrupted", targetMapPath);
                return;
            }
        }

        // Apply state and validate grid config
        State = spawnerStates.GameState ?? new GameSpawnerState();

        if (State.hexGridConfig.cols <= 0 || State.hexGridConfig.rows <= 0)
        {
            Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Warning,
                "LoadState: invalid grid config in file, applying defaults (7x7)");
            State.hexGridConfig = HexGridConfig.CreateDefault();
        }

        // Repair any inconsistencies in the loaded hex data
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

        // Assign state to spawners
        hexSpawner.State = spawnerStates.HexState ?? new HexSpawner.HexSpawnerState();
        edgeSpawner.State = spawnerStates.EdgeState ?? new EdgeSpawner.EdgeSpawnerState();
        cornerSpawner.State = spawnerStates.CornerState ?? new CornerSpawner.CornerSpawnerState();

        Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info,
            "LoadState: rebuilding board from {0} ({1}x{2})",
            targetMapPath, gridConfig.cols, gridConfig.rows);

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
            landConfigs = CreateDefaultLandConfigs();
            numConfigs = CreateDefaultNumConfigs();
        }
        
        /// <summary>
        /// Creates default land configurations for a standard 4-player Catan board.
        /// </summary>
        public static List<LandConfig> CreateDefaultLandConfigs()
        {
            return new List<LandConfig>
            {
                new LandConfig { landGroupID = "1", landCnt = 13, landType = "sea" },
                new LandConfig { landGroupID = "1", landCnt = 4, landType = "forest" },
                new LandConfig { landGroupID = "1", landCnt = 4, landType = "pasture" },
                new LandConfig { landGroupID = "1", landCnt = 4, landType = "field" },
                new LandConfig { landGroupID = "1", landCnt = 3, landType = "hill" },
                new LandConfig { landGroupID = "1", landCnt = 3, landType = "mountain" },
                new LandConfig { landGroupID = "1", landCnt = 1, landType = "desert" },
                new LandConfig { landGroupID = "1", landCnt = 5, landType = "harbour" },
            };
        }
        
        /// <summary>
        /// Creates default number configurations for a standard 4-player Catan board.
        /// </summary>
        public static List<NumConfig> CreateDefaultNumConfigs()
        {
            return new List<NumConfig>
            {
                new NumConfig { numGroupID = "1", numCnt = 1, numType = 2 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 3 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 4 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 5 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 6 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 8 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 9 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 10 },
                new NumConfig { numGroupID = "1", numCnt = 2, numType = 11 },
                new NumConfig { numGroupID = "1", numCnt = 1, numType = 12 },
            };
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

    /// <summary>Maximum file size (10 MB) accepted by LoadState to prevent memory exhaustion.</summary>
    private const long MaxMapFileSizeBytes = 10 * 1024 * 1024;

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

    /// <summary>
    /// Validates that a resolved map path does not escape the project directory via traversal.
    /// Returns true if the path is safe, false if it contains suspicious traversal patterns.
    /// </summary>
    private static bool IsPathSafe(string resolvedPath)
    {
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return false;
        }

        // Normalise and check for directory traversal
        string fullPath = Path.GetFullPath(resolvedPath);
        string projectRoot = Path.GetFullPath(".");
        return fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase);
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
