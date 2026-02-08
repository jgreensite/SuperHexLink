using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Spawner responsible for corner elements (settlements, cities, etc.).
/// Currently a placeholder — implement when corner gameplay is needed.
/// </summary>
public class CornerSpawner : SpawnerBase
{
    [ShowInInspector, OdinSerialize]
    private CornerSpawnerState state;

    public CornerSpawnerState State
    {
        get => state;
        set => state = value;
    }

    public override void Spawn() => BuildMe(false);

    public override void BuildMe(bool isRefresh) { /* TODO: implement corner spawning */ }

    public override void Clear() { /* TODO: implement corner clearing */ }

    public override void Refresh() { /* TODO: implement corner refresh */ }

    [Serializable]
    public class CornerSpawnerState
    {
        [SerializeField] public string myState;
    }
}
