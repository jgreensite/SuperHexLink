// Assets/Scripts/GameSpawnerState.cs
using System;
using System.Collections.Generic;

[Serializable]
public class GameSpawnerState
{
    public List<List<HexState>> HexStates = new List<List<HexState>>();
    // Add other game state properties as needed
}

[Serializable]
public class HexState
{
    public int Col;
    public int Row;
    public string HexType;
    public bool Selected;
    // Add other properties as needed
}