using System;
using System.Collections.Generic;
using UnityEngine;

public class Helpers : MonoBehaviour
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
                if ((t.gameObject.name.StartsWith(name, StringComparison.CurrentCultureIgnoreCase)) || (name==""))
                {
                    if (matched) ret.Add(t.gameObject);
                }
                else if (!matched) ret.Add(t.gameObject);
            }
        }
        return ret;
    }

    public static void DestroyObjects(List<GameObject> ret)
    {
        foreach (GameObject g in ret)
        {
#if UNITY_EDITOR
            DestroyImmediate(g);
#elif !UNITY_EDITOR
            Destroy(g);
#endif
        }
    }
}
