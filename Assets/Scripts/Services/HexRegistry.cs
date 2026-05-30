using System;
using System.Collections.Generic;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Services
{
    /// <summary>
    /// Registry service for O(1) hex lookup and management.
    /// Provides fast access to hexes by coordinates and maintains hex collections.
    /// Extracts hex lookup logic from HexSpawner for better performance and separation of concerns.
    /// </summary>
    public class HexRegistry
    {
        private readonly Dictionary<(int col, int row), Hex> _hexLookup;
        private readonly List<Hex> _allHexes;
        private readonly ActionLogSettings _logSettings;

        public HexRegistry(ActionLogSettings logSettings = null)
        {
            _hexLookup = new Dictionary<(int col, int row), Hex>();
            _allHexes = new List<Hex>();
            _logSettings = logSettings ?? new ActionLogSettings();
        }

        /// <summary>
        /// Gets a hex by its coordinates in O(1) time.
        /// </summary>
        /// <param name="col">Column coordinate</param>
        /// <param name="row">Row coordinate</param>
        /// <returns>Hex at the specified coordinates, or null if not found</returns>
        public Hex GetHex(int col, int row)
        {
            _hexLookup.TryGetValue((col, row), out Hex hex);
            return hex;
        }

        /// <summary>
        /// Registers a hex in the registry for fast lookup.
        /// </summary>
        /// <param name="hex">Hex to register</param>
        /// <returns>True if registration succeeded, false if hex was null or already registered</returns>
        public bool RegisterHex(Hex hex)
        {
            if (hex == null || hex.hexState == null)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                    "HexRegistry: Cannot register null hex or hex with null state");
                return false;
            }

            var key = (hex.hexState.Col, hex.hexState.Row);
            
            if (_hexLookup.ContainsKey(key))
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Warning,
                    $"HexRegistry: Hex at ({hex.hexState.Col}, {hex.hexState.Row}) is already registered");
                return false;
            }

            _hexLookup[key] = hex;
            _allHexes.Add(hex);

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Debug,
                $"HexRegistry: Registered hex at ({hex.hexState.Col}, {hex.hexState.Row})");

            return true;
        }

        /// <summary>
        /// Unregisters a hex from the registry.
        /// </summary>
        /// <param name="hex">Hex to unregister</param>
        /// <returns>True if unregistration succeeded, false if hex was not found</returns>
        public bool UnregisterHex(Hex hex)
        {
            if (hex == null || hex.hexState == null)
            {
                return false;
            }

            var key = (hex.hexState.Col, hex.hexState.Row);
            
            if (!_hexLookup.ContainsKey(key))
            {
                return false;
            }

            _hexLookup.Remove(key);
            _allHexes.Remove(hex);

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Debug,
                $"HexRegistry: Unregistered hex at ({hex.hexState.Col}, {hex.hexState.Row})");

            return true;
        }

        /// <summary>
        /// Gets all registered hexes.
        /// </summary>
        /// <returns>List of all registered hexes</returns>
        public IReadOnlyList<Hex> GetAllHexes()
        {
            return _allHexes.AsReadOnly();
        }

        /// <summary>
        /// Gets hexes in a rectangular region.
        /// </summary>
        /// <param name="minCol">Minimum column (inclusive)</param>
        /// <param name="maxCol">Maximum column (inclusive)</param>
        /// <param name="minRow">Minimum row (inclusive)</param>
        /// <param name="maxRow">Maximum row (inclusive)</param>
        /// <returns>Hexes within the specified region</returns>
        public IEnumerable<Hex> GetHexesInRegion(int minCol, int maxCol, int minRow, int maxRow)
        {
            for (int col = minCol; col <= maxCol; col++)
            {
                for (int row = minRow; row <= maxRow; row++)
                {
                    Hex hex = GetHex(col, row);
                    if (hex != null)
                    {
                        yield return hex;
                    }
                }
            }
        }

        /// <summary>
        /// Gets hexes within a certain distance from a center point.
        /// </summary>
        /// <param name="centerCol">Center column</param>
        /// <param name="centerRow">Center row</param>
        /// <param name="distance">Maximum distance (in hex units)</param>
        /// <returns>Hexes within the specified distance</returns>
        public IEnumerable<Hex> GetHexesInRange(int centerCol, int centerRow, int distance)
        {
            for (int col = centerCol - distance; col <= centerCol + distance; col++)
            {
                for (int row = centerRow - distance; row <= centerRow + distance; row++)
                {
                    // Simple distance check (can be optimized for hex grids)
                    int colDistance = Math.Abs(col - centerCol);
                    int rowDistance = Math.Abs(row - centerRow);
                    
                    if (colDistance + rowDistance <= distance)
                    {
                        Hex hex = GetHex(col, row);
                        if (hex != null)
                        {
                            yield return hex;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a hex exists at the specified coordinates.
        /// </summary>
        /// <param name="col">Column coordinate</param>
        /// <param name="row">Row coordinate</param>
        /// <returns>True if a hex exists at the coordinates</returns>
        public bool HasHex(int col, int row)
        {
            return _hexLookup.ContainsKey((col, row));
        }

        /// <summary>
        /// Gets the total count of registered hexes.
        /// </summary>
        /// <returns>Number of registered hexes</returns>
        public int GetHexCount()
        {
            return _allHexes.Count;
        }

        /// <summary>
        /// Clears all registered hexes.
        /// </summary>
        public void Clear()
        {
            int count = _allHexes.Count;
            _hexLookup.Clear();
            _allHexes.Clear();

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Info,
                $"HexRegistry: Cleared {count} hexes from registry");
        }

        /// <summary>
        /// Validates registry consistency.
        /// </summary>
        /// <returns>True if registry is consistent, false otherwise</returns>
        public bool ValidateConsistency()
        {
            if (_hexLookup.Count != _allHexes.Count)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Error,
                    $"HexRegistry: Inconsistent state - lookup count ({_hexLookup.Count}) != list count ({_allHexes.Count})");
                return false;
            }

            // Check that all hexes in the list are also in the lookup
            foreach (Hex hex in _allHexes)
            {
                if (hex == null || hex.hexState == null)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Error,
                        "HexRegistry: Found null hex or hex state in list");
                    return false;
                }

                var key = (hex.hexState.Col, hex.hexState.Row);
                if (!_hexLookup.ContainsKey(key))
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Error,
                        $"HexRegistry: Hex at ({hex.hexState.Col}, {hex.hexState.Row}) missing from lookup");
                    return false;
                }
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Debug,
                "HexRegistry: Registry consistency validation passed");

            return true;
        }

        /// <summary>
        /// Gets hexes by type.
        /// </summary>
        /// <param name="hexType">Type of hex to find</param>
        /// <returns>Hexes of the specified type</returns>
        public IEnumerable<Hex> GetHexesByType(string hexType)
        {
            foreach (Hex hex in _allHexes)
            {
                if (hex?.hexState?.HexType == hexType)
                {
                    yield return hex;
                }
            }
        }

        /// <summary>
        /// Gets selected hexes.
        /// </summary>
        /// <returns>Currently selected hexes</returns>
        public IEnumerable<Hex> GetSelectedHexes()
        {
            foreach (Hex hex in _allHexes)
            {
                if (hex?.hexState?.Selected == true)
                {
                    yield return hex;
                }
            }
        }

        /// <summary>
        /// Updates hex coordinates in the registry (useful for dynamic grid changes).
        /// </summary>
        /// <param name="hex">Hex to update</param>
        /// <param name="newCol">New column coordinate</param>
        /// <param name="newRow">New row coordinate</param>
        /// <returns>True if update succeeded</returns>
        public bool UpdateHexCoordinates(Hex hex, int newCol, int newRow)
        {
            if (hex == null || hex.hexState == null)
            {
                return false;
            }

            var oldKey = (hex.hexState.Col, hex.hexState.Row);
            var newKey = (newCol, newRow);

            // Remove old entry
            if (_hexLookup.ContainsKey(oldKey))
            {
                _hexLookup.Remove(oldKey);
            }

            // Add new entry
            _hexLookup[newKey] = hex;
            hex.hexState.Col = newCol;
            hex.hexState.Row = newRow;

            ActionLogger.Log(_logSettings, ActionLogCategory.HexLifecycle, ActionLogSeverity.Debug,
                $"HexRegistry: Updated hex coordinates from ({oldKey.Item1}, {oldKey.Item2}) to ({newCol}, {newRow})");

            return true;
        }
    }
}
