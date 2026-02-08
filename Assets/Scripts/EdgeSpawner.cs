using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Spawner responsible for edge elements between hexes (roads, etc.).
/// Currently a placeholder — implement when edge gameplay is needed.
/// </summary>
public class EdgeSpawner : SpawnerBase
{
    [ShowInInspector, OdinSerialize]
    private EdgeSpawnerState state;

    public EdgeSpawnerState State
    {
        get => state;
        set => state = value;
    }

    public override void Spawn() => BuildMe(false);

    public override void BuildMe(bool isRefresh) { /* TODO: implement edge spawning */ }

    public override void Clear() { /* TODO: implement edge clearing */ }

    public override void Refresh() { /* TODO: implement edge refresh */ }

    [Serializable]
    public class EdgeSpawnerState
    {
        [SerializeField] public string myState;
    }
}
