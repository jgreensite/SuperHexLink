using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility methods for querying and destroying GameObjects by layer/name.
/// </summary>
public static class Helpers
{
    public static List<GameObject> GetObjectsInLayer(GameObject root, int layer)
    {
        List<GameObject> ret = new List<GameObject>();
        foreach (Transform t in root.transform.GetComponentsInChildren(typeof(Transform), true))
        {
            if (t.gameObject.layer == layer)
            {
                ret.Add(t.gameObject);
            }
        }
        return ret;
    }

    public static List<GameObject> GetChildObjectsByName(GameObject root, bool matched)
    {
        return GetChildObjectsByName(root, "", matched);
    }

    public static List<GameObject> GetChildObjectsByName(GameObject root, string name, bool matched)
    {
        List<GameObject> ret = new List<GameObject>();
        foreach (Transform t in root.transform.GetComponentsInChildren(typeof(Transform), true))
        {
            if (t.gameObject.name != root.name)
            {
                if (t.gameObject.name.StartsWith(name, StringComparison.OrdinalIgnoreCase) || name == "")
                {
                    if (matched) ret.Add(t.gameObject);
                }
                else if (!matched) ret.Add(t.gameObject);
            }
        }
        return ret;
    }

    public static List<GameObject> GetChildObjectLights(GameObject root)
    {
        List<GameObject> ret = new List<GameObject>();
        foreach (Transform t in root.transform.GetComponentsInChildren(typeof(Transform), true))
        {
            if (t.gameObject.name != root.name)
            {
                if (t.gameObject.GetComponent<Light>() != null)
                {
                    ret.Add(t.gameObject);
                }
            }
        }
        return ret;
    }

    public static void DestroyObjects(List<GameObject> ret)
    {
        foreach (GameObject g in ret)
        {
            if (Application.isEditor)
            {
                UnityEngine.Object.DestroyImmediate(g);
            }
            else
            {
                UnityEngine.Object.Destroy(g);
            }
        }
    }
}
