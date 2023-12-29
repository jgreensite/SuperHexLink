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

public abstract class SpawnerBase : SerializedMonoBehaviour
{
    // Define common fields that are used by all spawners, if any

    public GameConstants CS;

    [Button("Spawn")]
    public abstract void Spawn();

    [Button("Refresh")]
    public abstract void Refresh();
  
    public abstract void BuildMe(bool isRefresh);

    [Button("Clear")]
    public abstract void Clear();

    bool isConfiguredEmpty(String t)
    {
        if (
            (t == "") || (t == null) ||
            (t == GameConstants.CAR_TYPE_WORD_NULL) || (t == GameConstants.CAR_TYPE_NONE)
            )
        {
            return true;
        }
        else { return false; }
    }
}
