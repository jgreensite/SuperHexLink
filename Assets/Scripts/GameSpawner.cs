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

public class GameSpawner : SpawnerBase
{
    [SerializeField]
    private HexSpawner hexSpawner;
    [SerializeField]
    private EdgeSpawner edgeSpawner;
    [SerializeField]
    private CornerSpawner cornerSpawner;

    public void Awake()
    {
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
    }

    [Button("Spawn All Game Elements")]
    public override void Spawn()
    {
        hexSpawner.Spawn();
        edgeSpawner.Spawn();
        cornerSpawner.Spawn();
    }

    [Button("Clear All Game Elements")]
    public override void Clear()
    {
        hexSpawner.Clear();
        edgeSpawner.Clear();
        cornerSpawner.Clear();
    }

    [Button("Refresh All Game Elements")]
    public override void Refresh()
    {
        hexSpawner.Refresh();
        edgeSpawner.Refresh();
        cornerSpawner.Refresh();
    }

    [Button("Save Map")]
    //todo - externalise defauilt value as a constant
    public void SaveHexes(string filePath)
    {
        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/map.json"; //default value
        }
            
            //build a List to hold the hexSpawner state and the edgeSpawner state and the cornerSpawner state
            
            List<SpawnerState> spawnerStates = new List<SpawnerState>();
            spawnerStates.Add(hexSpawner.state);
            spawnerStates.Add(edgeSpawner.state);
            spawnerStates.Add(cornerSpawner.state);

            //write a single save state comprised of the hexSpawner state and the edgeSpawner state and the cornerSpawner state
            
            byte[] bytes = SerializationUtility.SerializeValue(spawnerStates, DataFormat.JSON);
            File.WriteAllBytes(filePath, bytes);            
    }

    [Button("Load Map")]
    public void LoadState(string filePath)
    {
        //TODO - this is not very elegant
        //it would be better if we didn't have to call update hexes and that an event fired automatically
        
        //load the hex data
        SpawnerState loadedSpawner = new SpawnerState();
        List<SpawnerState> spawnerStates = new List<SpawnerState>();
        if ((filePath == null) || (filePath.Length == 0))
        {
            filePath = "./data/maps/map.json"; //default value
        }
        if (!File.Exists(filePath)) return; // No state to load

        byte[] bytes = File.ReadAllBytes(filePath);
        spawnerStates = SerializationUtility.DeserializeValue<List<SpawnerState>>(bytes, DataFormat.JSON);
 
        //copy accross loaded configuration data
        hexSpawner.state.hexGridConfig = loadedSpawner.hexGridConfig;
        hexSpawner.state.landConfigs = loadedSpawner.landConfigs;
        hexSpawner.state.numConfigs = loadedSpawner.numConfigs;

        //create new gameobjects attached to new hexes based on loaded values
        //remember it is not possible to serialise Unity gameobjects so we need to create new ones
        BuildMe(true);

        //Copy across hexState from loaded objects to new gameobjects
        //ToDo - I don't think it's possible to simply copy across the hexes as Unity gameonjects are not serialisable so we'll end up with junk, but I need to check this
        for (int col = 0; col < hexSpawner.state.hexGridConfig.cols; col++)
        {
            for (int row = 0; row < hexSpawner.state.hexGridConfig.rows; row++)
            {
                hexSpawner.state.hexes[col][row].hexState = loadedSpawner.hexes[col][row].hexState;
            }
        }

        //update the newly created gameobjects according to each ones hexState
        hexSpawner.UpdateHexes();
    }


    private void AssociateElements()
    {
        // Implement logic to associate Hexes with their respective Edges and Corners
    }

    private void AdjustCameraPosition()
    {
        // Logic to adjust the camera to fit the game board
    }
}
