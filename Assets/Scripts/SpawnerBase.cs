using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Abstract base class for all spawners (Hex, Edge, Corner).
/// Provides common references and lifecycle methods.
/// </summary>
public abstract class SpawnerBase : SerializedMonoBehaviour
{
    [Tooltip("Shared game constants asset — assign in the Inspector.")]
    public GameConstants CS;

    [Button("Spawn")]
    public abstract void Spawn();

    [Button("Refresh")]
    public abstract void Refresh();

    public abstract void BuildMe(bool isRefresh);

    [Button("Clear")]
    public abstract void Clear();

    /// <summary>
    /// Returns true when <paramref name="hexType"/> represents an empty / off-board cell.
    /// </summary>
    protected static bool IsConfiguredEmpty(string hexType)
    {
        return string.IsNullOrEmpty(hexType)
            || string.Equals(hexType, GameConstants.CAR_TYPE_WORD_NULL, StringComparison.Ordinal)
            || string.Equals(hexType, GameConstants.CAR_TYPE_NONE, StringComparison.Ordinal);
    }
}
