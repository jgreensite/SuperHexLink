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
using SuperHexLink.Logging;

public class GameSpawner : SpawnerBase
{

    [ShowInInspector,OdinSerialize]
    private GameSpawnerState state;

    public GameSpawnerState State
    {
        get { return state; }
        set { state = value; }
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

    public void Awake()
    {
        //state = new GameSpawnerState();
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

    [Button("Save Map")]
    //todo - externalise defauilt value as a constant
    public void SaveHexes(string filePath)
    {
        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/"; //default value
        }

        //build a List to hold the hexSpawner state and the edgeSpawner state and the cornerSpawner state

        CombinedSpawnerState spawnerStates = new()
        {
            GameState = State,
            HexState = hexSpawner.State,
            EdgeState = edgeSpawner.State,
            CornerState = cornerSpawner.State
        };

        //write a single save state comprised of the hexSpawner state and the edgeSpawner state and the cornerSpawner state

        byte[] bytes0 = SerializationUtility.SerializeValue(spawnerStates, DataFormat.JSON);
            File.WriteAllBytes(filePath + "map.json", bytes0);
        byte[] bytes1 = SerializationUtility.SerializeValue(State, DataFormat.JSON);
            File.WriteAllBytes(filePath + "0_gameSpawnerState.json", bytes1);
        byte[] bytes2 = SerializationUtility.SerializeValue(hexSpawner.State, DataFormat.JSON);
            File.WriteAllBytes(filePath + "1_hexSpawnerState.json", bytes2);
        byte[] bytes3 = SerializationUtility.SerializeValue(edgeSpawner.State, DataFormat.JSON);
            File.WriteAllBytes(filePath + "2_edgeSpawnerState.json", bytes3);
        byte[] bytes4 = SerializationUtility.SerializeValue(cornerSpawner.State, DataFormat.JSON);
            File.WriteAllBytes(filePath + "3_cornerSpawnerState.json", bytes4);


    }

    [Button("Load Map")]
    public void LoadState(string filePath)
    {
        //TODO - this is not very elegant
        //it would be better if we didn't have to call update hexes and that an event fired automatically

        //load the hex data
        CombinedSpawnerState spawnerStates = new();

        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/map.json"; //default value
        }

        if (!File.Exists(filePath)) return; // No state to load

        byte[] bytes = File.ReadAllBytes(filePath);
        spawnerStates = SerializationUtility.DeserializeValue<CombinedSpawnerState>(bytes, DataFormat.JSON);

        //copy accross loaded configuration data for the game
        State = spawnerStates.GameState;

    Log(ActionLogCategory.GameLifecycle, ActionLogSeverity.Info, "LoadState applying saved data from {0}", filePath);
    // Assign spawner states BEFORE creating GameObjects so BuildMe doesn't overwrite loaded state
    hexSpawner.State = spawnerStates.HexState;
    edgeSpawner.State = spawnerStates.EdgeState;
    cornerSpawner.State = spawnerStates.CornerState;

        //create new gameobjects attached to the loaded hex state without randomizing the board
        BuildMe(true);

        //reapply visuals (materials/models/camera) once the objects exist
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
        [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<LandConfig> landConfigs = new();
        [TableList(ShowIndexLabels = true)] [OdinSerialize] public List<NumConfig> numConfigs = new();

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

    [Serializable]
    public class CombinedSpawnerState
    {
        public GameSpawnerState GameState;
        public HexSpawner.HexSpawnerState HexState;
        public EdgeSpawner.EdgeSpawnerState EdgeState;
        public CornerSpawner.CornerSpawnerState CornerState;
    }
}
