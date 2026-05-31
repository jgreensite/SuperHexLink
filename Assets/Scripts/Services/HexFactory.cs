using TMPro;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Services
{
    /// <summary>
    /// Factory service responsible for creating and configuring hex GameObject instances.
    /// Extracts prefab instantiation logic from HexSpawner for better separation of concerns.
    /// </summary>
    public class HexFactory
    {
        private readonly ActionLogSettings _logSettings;

        public HexFactory(ActionLogSettings logSettings = null)
        {
            _logSettings = logSettings ?? new ActionLogSettings();
        }

        /// <summary>
        /// Creates a new hex GameObject with the specified configuration.
        /// Handles positioning, scaling, naming, and layer assignment.
        /// </summary>
        /// <param name="hexPrefab">The hex prefab to instantiate</param>
        /// <param name="col">Column coordinate</param>
        /// <param name="row">Row coordinate</param>
        /// <param name="parent">Parent transform for the hex</param>
        /// <param name="gridConfig">Grid configuration for positioning and scaling</param>
        /// <param name="isRefresh">Whether this is a refresh operation (uses existing state)</param>
        /// <returns>Created hex GameObject</returns>
        public Hex CreateHex(GameObject hexPrefab, int col, int row, Transform parent, HexGridConfig gridConfig, bool isRefresh = false)
        {
            if (hexPrefab == null)
            {
                Debug.LogError("HexFactory: Hex prefab is null");
                return null;
            }

            // Calculate position and instantiate
            Vector3 position = CalculateHexPosition(col, row, gridConfig);
            Hex newHex = Object.Instantiate(hexPrefab, position, Quaternion.identity, parent);

            // Configure scale
            Vector3 scale = new Vector3(
                x: newHex.transform.localScale.x * gridConfig.radius,
                y: newHex.transform.localScale.y * gridConfig.height,
                z: newHex.transform.localScale.z * gridConfig.radius
            );
            newHex.transform.localScale = scale;

            // Set name and layer
            newHex.name = $"hex_{col}_{row}";
            newHex.gameObject.layer = LayerMask.NameToLayer(GameConstants.OBJ_LOCATION_LAYER_GAMEBOARD);

            // Initialize state
            InitializeHexState(newHex, col, row, isRefresh);

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
                $"HexFactory: Created hex at ({col}, {row})");

            return newHex;
        }

        /// <summary>
        /// Creates a hex text GameObject for displaying numbers on hexes.
        /// </summary>
        /// <param name="hexTextPrefab">The text prefab to instantiate</param>
        /// <param name="parent">Parent transform (typically the hex)</param>
        /// <returns>Created hex text GameObject</returns>
        public TextMeshPro CreateHexText(GameObject hexTextPrefab, Transform parent)
        {
            if (hexTextPrefab == null)
            {
                Debug.LogError("HexFactory: Hex text prefab is null");
                return null;
            }

            TextMeshPro hexText = Object.Instantiate(hexTextPrefab, parent);
            hexText.name = "hexText";

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Debug,
                "HexFactory: Created hex text");

            return hexText;
        }

        /// <summary>
        /// Creates a land model GameObject for hex visual representation.
        /// </summary>
        /// <param name="hexLandPrefab">The land model prefab to instantiate</param>
        /// <param name="parent">Parent transform (typically the hex)</param>
        /// <param name="hexType">Type of hex for mesh filtering</param>
        /// <returns>Created land model GameObject</returns>
        public GameObject CreateLandModel(GameObject hexLandPrefab, Transform parent, string hexType)
        {
            if (hexLandPrefab == null)
            {
                Debug.LogError("HexFactory: Hex land prefab is null");
                return null;
            }

            GameObject landModel = Object.Instantiate(hexLandPrefab, parent);
            landModel.name = "landModel";

            // Position and configure the land model
            PositionLandModel(landModel);
            FilterLandModelMeshes(landModel, hexType);

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLand, ActionLogSeverity.Debug,
                $"HexFactory: Created land model for hex type '{hexType}'");

            return landModel;
        }

        /// <summary>
        /// Calculates the world position for a hex at the given coordinates.
        /// </summary>
        private Vector3 CalculateHexPosition(int col, int row, HexGridConfig gridConfig)
        {
            return new Vector3(
                x: (float)(col * gridConfig.radius * 1.5),
                y: UnityEngine.Random.Range(gridConfig.minHeight, gridConfig.maxHeight),
                z: -row * gridConfig.Apothem * 2 + GetZOffset(col, gridConfig)
            );
        }

        /// <summary>
        /// Calculates the Z offset for hex positioning to create the hex grid pattern.
        /// </summary>
        private float GetZOffset(int col, HexGridConfig gridConfig)
        {
            return col % 2 == 0 ? 0f : -gridConfig.Apothem;
        }

        /// <summary>
        /// Initializes the hex state with default values.
        /// </summary>
        private void InitializeHexState(Hex hex, int col, int row, bool isRefresh)
        {
            if (hex.hexState == null)
            {
                hex.hexState = new Hex.HexState();
            }

            if (!isRefresh)
            {
                // Initialize with default values for new hexes
                hex.hexState.Col = col;
                hex.hexState.Row = row;
                hex.hexState.HexType = "none";
                hex.hexState.HexSubType = "plain";
                hex.hexState.Rotation = 0;
                hex.hexState.HexNum = 0;
                hex.hexState.GroupID = null;
                hex.hexState.Selected = false;
            }
        }

        /// <summary>
        /// Positions the land model correctly on the hex.
        /// </summary>
        private void PositionLandModel(GameObject landModel)
        {
            // Reset rotation and position
            landModel.transform.rotation = Quaternion.identity;
            landModel.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Filters the land model meshes based on hex type.
        /// </summary>
        private void FilterLandModelMeshes(GameObject landModel, string hexType)
        {
            // This would contain logic to enable/disable specific meshes based on hex type
            // Implementation depends on the specific land model structure
            // For now, this is a placeholder for the mesh filtering logic
        }
    }
}
