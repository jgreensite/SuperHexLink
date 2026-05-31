using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using SuperHexLink.Logging;

/// <summary>
/// Spawner responsible for edge elements between hexes (roads, etc.).
/// Implements save/load reconstruction with proper edge state management.
/// </summary>
public class EdgeSpawner : SpawnerBase
{
    [ShowInInspector, OdinSerialize]
    private EdgeSpawnerState state;

    [SerializeField]
    private List<EdgeInstance> edgeInstances = new List<EdgeInstance>();

    public EdgeSpawnerState State
    {
        get => state;
        set => state = value;
    }

    public override void Spawn() => BuildMe(false);

    public override void BuildMe(bool isRefresh)
    {
        if (state == null)
        {
            state = new EdgeSpawnerState();
            return;
        }

        if (isRefresh)
        {
            Refresh();
            return;
        }

        // Clear existing edges before rebuilding
        Clear();

        // Rebuild edges from saved state
        RebuildEdgesFromState();

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, $"EdgeSpawner built {edgeInstances.Count} edges from state");
    }

    public override void Clear()
    {
        // Destroy all edge GameObjects
        foreach (var edgeInstance in edgeInstances)
        {
            if (edgeInstance?.gameObject != null)
            {
                DestroyImmediate(edgeInstance.gameObject);
            }
        }

        edgeInstances.Clear();

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, "EdgeSpawner cleared all edges");
    }

    public override void Refresh()
    {
        // Refresh existing edges - update their state based on current hex configuration
        foreach (var edgeInstance in edgeInstances)
        {
            if (edgeInstance?.gameObject != null)
            {
                UpdateEdgeVisual(edgeInstance);
            }
        }

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, $"EdgeSpawner refreshed {edgeInstances.Count} edges");
    }

    /// <summary>
    /// Rebuilds all edges from the saved state.
    /// </summary>
    private void RebuildEdgesFromState()
    {
        if (state?.edges == null) return;

        var hexSpawner = FindObjectOfType<HexSpawner>();
        if (hexSpawner == null)
        {
            Debug.LogWarning("EdgeSpawner: Cannot find HexSpawner for edge reconstruction");
            return;
        }

        foreach (var edgeState in state.edges)
        {
            var edgeInstance = CreateEdgeFromState(edgeState, hexSpawner);
            if (edgeInstance != null)
            {
                edgeInstances.Add(edgeInstance);
            }
        }
    }

    /// <summary>
    /// Creates an edge GameObject from saved edge state.
    /// </summary>
    private EdgeInstance CreateEdgeFromState(EdgeState edgeState, HexSpawner hexSpawner)
    {
        // Find the two hexes that this edge connects
        var hex1 = hexSpawner.GetAllHexes().FirstOrDefault(h => 
            h.hexState != null && h.hexState.Col == edgeState.hex1Col && h.hexState.Row == edgeState.hex1Row);
        
        var hex2 = hexSpawner.GetAllHexes().FirstOrDefault(h => 
            h.hexState != null && h.hexState.Col == edgeState.hex2Col && h.hexState.Row == edgeState.hex2Row);

        if (hex1 == null || hex2 == null)
        {
            Debug.LogWarning($"EdgeSpawner: Cannot find hexes for edge ({edgeState.hex1Col},{edgeState.hex1Row})-({edgeState.hex2Col},{edgeState.hex2Row})");
            return null;
        }

        // Calculate edge position between the two hexes
        var edgePosition = (hex1.transform.position + hex2.transform.position) / 2f;
        var edgeDirection = (hex2.transform.position - hex1.transform.position).normalized;

        // Create edge GameObject
        var edgeGo = new GameObject($"Edge_{edgeState.edgeType}_{edgeState.hex1Col}_{edgeState.hex1Row}_{edgeState.hex2Col}_{edgeState.hex2Row}");
        edgeGo.transform.position = edgePosition;
        edgeGo.transform.LookAt(edgePosition + edgeDirection);

        // Add visual representation (simple cylinder for roads)
        var edgeRenderer = edgeGo.AddComponent<MeshRenderer>();
        var edgeFilter = edgeGo.AddComponent<MeshFilter>();
        
        // Create a simple cylinder mesh for the edge
        var edgeMesh = CreateEdgeMesh(edgeState.edgeType);
        edgeFilter.mesh = edgeMesh;
        
        // Set material based on edge type
        edgeRenderer.material = GetEdgeMaterial(edgeState.edgeType);

        // Create EdgeInstance to track the edge
        var edgeInstance = new EdgeInstance
        {
            gameObject = edgeGo,
            edgeState = edgeState,
            hex1 = hex1,
            hex2 = hex2
        };

        return edgeInstance;
    }

    /// <summary>
    /// Creates a mesh for the edge based on its type.
    /// </summary>
    private Mesh CreateEdgeMesh(string edgeType)
    {
        var mesh = new Mesh();
        
        // Create a simple cylinder/road segment
        float radius = 0.1f;
        float height = 1.0f;
        int segments = 8;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Create cylinder vertices
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            
            vertices.Add(new Vector3(x, -height/2, z));
            vertices.Add(new Vector3(x, height/2, z));
        }

        // Create cylinder triangles
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            
            // Side triangles
            triangles.Add(i * 2);
            triangles.Add(next * 2);
            triangles.Add(i * 2 + 1);
            
            triangles.Add(next * 2);
            triangles.Add(next * 2 + 1);
            triangles.Add(i * 2 + 1);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// Gets the appropriate material for the edge type.
    /// </summary>
    private Material GetEdgeMaterial(string edgeType)
    {
        // Return different materials based on edge type
        switch (edgeType?.ToLower())
        {
            case "road":
                return new Material(Shader.Find("Standard")) { color = new Color(0.6f, 0.3f, 0f) };
            case "bridge":
                return new Material(Shader.Find("Standard")) { color = Color.gray };
            default:
                return new Material(Shader.Find("Standard")) { color = Color.white };
        }
    }

    /// <summary>
    /// Updates the visual appearance of an edge based on current game state.
    /// </summary>
    private void UpdateEdgeVisual(EdgeInstance edgeInstance)
    {
        if (edgeInstance?.gameObject == null || edgeInstance.edgeState == null) return;

        // Update material or appearance based on current game state
        var renderer = edgeInstance.gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = GetEdgeMaterial(edgeInstance.edgeState.edgeType);
        }
    }

    /// <summary>
    /// Adds a new edge between two hexes.
    /// </summary>
    public void AddEdge(Hex hex1, Hex hex2, string edgeType)
    {
        if (state == null) state = new EdgeSpawnerState();
        if (state.edges == null) state.edges = new List<EdgeState>();

        var edgeState = new EdgeState
        {
            hex1Col = hex1.hexState.Col,
            hex1Row = hex1.hexState.Row,
            hex2Col = hex2.hexState.Col,
            hex2Row = hex2.hexState.Row,
            edgeType = edgeType
        };

        state.edges.Add(edgeState);

        // Create the edge GameObject
        var hexSpawner = FindObjectOfType<HexSpawner>();
        var edgeInstance = CreateEdgeFromState(edgeState, hexSpawner);
        if (edgeInstance != null)
        {
            edgeInstances.Add(edgeInstance);
        }
    }

    /// <summary>
    /// Gets all current edge instances.
    /// </summary>
    public List<EdgeInstance> GetEdgeInstances() => edgeInstances;

    [Serializable]
    public class EdgeSpawnerState
    {
        [SerializeField, OdinSerialize]
        public List<EdgeState> edges = new List<EdgeState>();
    }

    [Serializable]
    public class EdgeState
    {
        [SerializeField] public int hex1Col;
        [SerializeField] public int hex1Row;
        [SerializeField] public int hex2Col;
        [SerializeField] public int hex2Row;
        [SerializeField] public string edgeType;
    }

    [Serializable]
    public class EdgeInstance
    {
        public GameObject gameObject;
        public EdgeState edgeState;
        public Hex hex1;
        public Hex hex2;
    }
}
