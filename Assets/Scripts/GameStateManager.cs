// Assets/Scripts/GameStateManager.cs
using System;
using System.IO;
using Sirenix.Serialization;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public GameSpawnerState CurrentGameState { get; private set; }
    public event Action<GameSpawnerState> OnGameStateChanged;

    public void SaveState(string filePath)
    {
        byte[] bytes = SerializationUtility.SerializeValue(CurrentGameState, DataFormat.JSON);
        File.WriteAllBytes(filePath, bytes);
    }

    public void LoadState(string filePath)
    {
        if (!File.Exists(filePath)) return;

        byte[] bytes = File.ReadAllBytes(filePath);
        CurrentGameState = SerializationUtility.DeserializeValue<GameSpawnerState>(bytes, DataFormat.JSON);
        OnGameStateChanged?.Invoke(CurrentGameState);
    }
}