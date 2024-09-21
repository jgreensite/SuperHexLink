using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[Serializable]
public class HexSpawnerState
{
    private bool isHandlingEvent = false;
    // Define a delegate for the event
    public delegate void HexesChangedHandler(List<List<Hex.BaseHexState>> hexes);
    public event HexesChangedHandler OnHexesChanged;

    [TableList(ShowIndexLabels = true)] [OdinSerialize] 
    private List<List<Hex.BaseHexState>> _hexes = new List<List<Hex.BaseHexState>>();

    public List<List<Hex.BaseHexState>> hexes
    {
        get { return _hexes; }
        set
        {
            _hexes = value;
            if (isHandlingEvent)
            {
                OnHexesChanged?.Invoke(_hexes);
            }
        }
    }
   public HexSpawnerState(Hex.HexState hexState)
    {
        if (hexState == null)
        {
            throw new ArgumentNullException(nameof(hexState), "Hex state cannot be null.");
        }
        hexState.onHexUpdated += HandleHexUpdated;
        Debug.Log("HexSpawnerState created and subscribed to hex updates.");
    }

    private void HandleHexUpdated(Hex.HexState hexState)
    {
        isHandlingEvent = true;
        hexes[hexState.Col][hexState.Row].HexType = hexState.HexType;
        // ... continue for other properties
        isHandlingEvent = false;
    }
}
