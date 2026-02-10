using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using SuperHexLink.Logging;

/// <summary>
/// Spawner responsible for corner elements (settlements, cities, etc.).
/// Implements save/load reconstruction with proper corner state management.
/// </summary>
public class CornerSpawner : SpawnerBase
{
    [ShowInInspector, OdinSerialize]
    private CornerSpawnerState state;

    [SerializeField]
    private List<CornerInstance> cornerInstances = new List<CornerInstance>();

    public CornerSpawnerState State
    {
        get => state;
        set => state = value;
    }

    public override void Spawn() => BuildMe(false);

    public override void BuildMe(bool isRefresh)
    {
        if (state == null)
        {
            state = new CornerSpawnerState();
            return;
        }

        if (isRefresh)
        {
            Refresh();
            return;
        }

        // Clear existing corners before rebuilding
        Clear();

        // Rebuild corners from saved state
        RebuildCornersFromState();

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, $"CornerSpawner built {cornerInstances.Count} corners from state");
    }

    public override void Clear()
    {
        // Destroy all corner GameObjects
        foreach (var cornerInstance in cornerInstances)
        {
            if (cornerInstance?.gameObject != null)
            {
                DestroyImmediate(cornerInstance.gameObject);
            }
        }

        cornerInstances.Clear();

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, "CornerSpawner cleared all corners");
    }

    public override void Refresh()
    {
        // Refresh existing corners - update their state based on current hex configuration
        foreach (var cornerInstance in cornerInstances)
        {
            if (cornerInstance?.gameObject != null)
            {
                UpdateCornerVisual(cornerInstance);
            }
        }

        ActionLogger.Log(new ActionLogSettings(), ActionLogCategory.HexLifecycle, 
            ActionLogSeverity.Info, $"CornerSpawner refreshed {cornerInstances.Count} corners");
    }

    /// <summary>
    /// Rebuilds all corners from the saved state.
    /// </summary>
    private void RebuildCornersFromState()
    {
        if (state?.corners == null) return;

        var hexSpawner = FindObjectOfType<HexSpawner>();
        if (hexSpawner == null)
        {
            Debug.LogWarning("CornerSpawner: Cannot find HexSpawner for corner reconstruction");
            return;
        }

        foreach (var cornerState in state.corners)
        {
            var cornerInstance = CreateCornerFromState(cornerState, hexSpawner);
            if (cornerInstance != null)
            {
                cornerInstances.Add(cornerInstance);
            }
        }
    }

    /// <summary>
    /// Creates a corner GameObject from saved corner state.
    /// </summary>
    private CornerInstance CreateCornerFromState(CornerState cornerState, HexSpawner hexSpawner)
    {
        // Find the hex that contains this corner
        var hex = hexSpawner.GetAllHexes().FirstOrDefault(h => 
            h.hexState != null && h.hexState.Col == cornerState.hexCol && h.hexState.Row == cornerState.hexRow);

        if (hex == null)
        {
            Debug.LogWarning($"CornerSpawner: Cannot find hex for corner ({cornerState.hexCol},{cornerState.hexRow}) at vertex {cornerState.vertexIndex}");
            return null;
        }

        // Calculate corner position based on hex position and vertex index
        var cornerPosition = CalculateCornerPosition(hex, cornerState.vertexIndex);

        // Create corner GameObject
        var cornerGo = new GameObject($"Corner_{cornerState.cornerType}_{cornerState.hexCol}_{cornerState.hexRow}_v{cornerState.vertexIndex}");
        cornerGo.transform.position = cornerPosition;

        // Add visual representation (simple cube for settlements/cities)
        var cornerRenderer = cornerGo.AddComponent<MeshRenderer>();
        var cornerFilter = cornerGo.AddComponent<MeshFilter>();
        
        // Create a simple mesh for the corner
        var cornerMesh = CreateCornerMesh(cornerState.cornerType);
        cornerFilter.mesh = cornerMesh;
        
        // Set material based on corner type
        cornerRenderer.material = GetCornerMaterial(cornerState.cornerType);

        // Create CornerInstance to track the corner
        var cornerInstance = new CornerInstance
        {
            gameObject = cornerGo,
            cornerState = cornerState,
            hex = hex
        };

        return cornerInstance;
    }

    /// <summary>
    /// Calculates the world position of a corner vertex on a hex.
    /// </summary>
    private Vector3 CalculateCornerPosition(Hex hex, int vertexIndex)
    {
        // Hex vertices are arranged clockwise starting from the top-right
        // Vertex 0: top-right, 1: right, 2: bottom-right, 3: bottom-left, 4: left, 5: top-left
        float hexRadius = 0.5f; // Assuming hex radius of 0.5 units
        
        var angles = new float[]
        {
            30f,   // top-right
            90f,   // right
            150f,  // bottom-right
            210f,  // bottom-left
            270f,  // left
            330f   // top-left
        };

        if (vertexIndex < 0 || vertexIndex >= angles.Length)
        {
            vertexIndex = 0; // Default to top-right if invalid
        }

        float angleRad = angles[vertexIndex] * Mathf.Deg2Rad;
        float localX = hexRadius * Mathf.Cos(angleRad);
        float localZ = hexRadius * Mathf.Sin(angleRad);

        return hex.transform.position + new Vector3(localX, 0, localZ);
    }

    /// <summary>
    /// Creates a mesh for the corner based on its type.
    /// </summary>
    private Mesh CreateCornerMesh(string cornerType)
    {
        var mesh = new Mesh();
        
        // Create different shapes based on corner type
        switch (cornerType?.ToLower())
        {
            case "settlement":
                return CreateSettlementMesh();
            case "city":
                return CreateCityMesh();
            default:
                return CreateDefaultCornerMesh();
        }
    }

    /// <summary>
    /// Creates a pyramid mesh for settlements.
    /// </summary>
    private Mesh CreateSettlementMesh()
    {
        var mesh = new Mesh();
        float size = 0.2f;
        float height = 0.3f;

        var vertices = new Vector3[]
        {
            // Base vertices
            new Vector3(-size, 0, -size),
            new Vector3(size, 0, -size),
            new Vector3(size, 0, size),
            new Vector3(-size, 0, size),
            // Apex
            new Vector3(0, height, 0)
        };

        var triangles = new int[]
        {
            // Base
            0, 2, 1,
            0, 3, 2,
            // Sides
            0, 1, 4,
            1, 2, 4,
            2, 3, 4,
            3, 0, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// Creates a cube mesh for cities.
    /// </summary>
    private Mesh CreateCityMesh()
    {
        var mesh = new Mesh();
        float size = 0.25f;
        float height = 0.5f;

        var vertices = new Vector3[]
        {
            // Bottom face
            new Vector3(-size, 0, -size),
            new Vector3(size, 0, -size),
            new Vector3(size, 0, size),
            new Vector3(-size, 0, size),
            // Top face
            new Vector3(-size, height, -size),
            new Vector3(size, height, -size),
            new Vector3(size, height, size),
            new Vector3(-size, height, size)
        };

        var triangles = new int[]
        {
            // Bottom
            0, 2, 1,
            0, 3, 2,
            // Top
            4, 5, 6,
            4, 6, 7,
            // Front
            0, 5, 1,
            0, 4, 5,
            // Back
            2, 7, 3,
            2, 6, 7,
            // Left
            0, 7, 4,
            0, 3, 7,
            // Right
            1, 6, 5,
            1, 2, 6
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// Creates a default sphere mesh for unknown corner types.
    /// </summary>
    private Mesh CreateDefaultCornerMesh()
    {
        var mesh = new Mesh();
        float radius = 0.15f;
        int segments = 8;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        // Create sphere vertices
        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = (float)lat / segments * Mathf.PI;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = (float)lon / segments * 2 * Mathf.PI;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float x = cosPhi * sinTheta * radius;
                float y = cosTheta * radius;
                float z = sinPhi * sinTheta * radius;

                vertices.Add(new Vector3(x, y, z));
            }
        }

        // Create sphere triangles
        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int current = lat * (segments + 1) + lon;
                int next = current + segments + 1;

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(current + 1);

                triangles.Add(current + 1);
                triangles.Add(next);
                triangles.Add(next + 1);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    /// <summary>
    /// Gets the appropriate material for the corner type.
    /// </summary>
    private Material GetCornerMaterial(string cornerType)
    {
        // Return different materials based on corner type
        switch (cornerType?.ToLower())
        {
            case "settlement":
                return new Material(Shader.Find("Standard")) { color = Color.green };
            case "city":
                return new Material(Shader.Find("Standard")) { color = Color.blue };
            default:
                return new Material(Shader.Find("Standard")) { color = Color.gray };
        }
    }

    /// <summary>
    /// Updates the visual appearance of a corner based on current game state.
    /// </summary>
    private void UpdateCornerVisual(CornerInstance cornerInstance)
    {
        if (cornerInstance?.gameObject == null || cornerInstance.cornerState == null) return;

        // Update material or appearance based on current game state
        var renderer = cornerInstance.gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = GetCornerMaterial(cornerInstance.cornerState.cornerType);
        }
    }

    /// <summary>
    /// Adds a new corner at a specific vertex of a hex.
    /// </summary>
    public void AddCorner(Hex hex, int vertexIndex, string cornerType)
    {
        if (state == null) state = new CornerSpawnerState();
        if (state.corners == null) state.corners = new List<CornerState>();

        var cornerState = new CornerState
        {
            hexCol = hex.hexState.Col,
            hexRow = hex.hexState.Row,
            vertexIndex = vertexIndex,
            cornerType = cornerType
        };

        state.corners.Add(cornerState);

        // Create the corner GameObject
        var hexSpawner = FindObjectOfType<HexSpawner>();
        var cornerInstance = CreateCornerFromState(cornerState, hexSpawner);
        if (cornerInstance != null)
        {
            cornerInstances.Add(cornerInstance);
        }
    }

    /// <summary>
    /// Gets all current corner instances.
    /// </summary>
    public List<CornerInstance> GetCornerInstances() => cornerInstances;

    [Serializable]
    public class CornerSpawnerState
    {
        [SerializeField, OdinSerialize]
        public List<CornerState> corners = new List<CornerState>();
    }

    [Serializable]
    public class CornerState
    {
        [SerializeField] public int hexCol;
        [SerializeField] public int hexRow;
        [SerializeField] public int vertexIndex;
        [SerializeField] public string cornerType;
    }

    [Serializable]
    public class CornerInstance
    {
        public GameObject gameObject;
        public CornerState cornerState;
        public Hex hex;
    }
}
