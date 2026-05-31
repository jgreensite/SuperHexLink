using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Utils
{
    /// <summary>
    /// Utility for computing deterministic hashes of game board states.
    /// Enables board comparison and validation for save/load integrity.
    /// </summary>
    public static class BoardHasher
    {
        private static readonly ActionLogSettings _logSettings = new ActionLogSettings();

        /// <summary>
        /// Computes a deterministic hash of the complete board state.
        /// Includes hexes, edges, corners, and all their properties.
        /// </summary>
        /// <param name="hexSpawner">The hex spawner containing board state</param>
        /// <param name="edgeSpawner">The edge spawner containing edge state</param>
        /// <param name="cornerSpawner">The corner spawner containing corner state</param>
        /// <returns>Deterministic SHA-256 hash of the board state</returns>
        public static string ComputeHash(HexSpawner hexSpawner, EdgeSpawner edgeSpawner = null, CornerSpawner cornerSpawner = null)
        {
            if (hexSpawner == null)
            {
                throw new ArgumentNullException(nameof(hexSpawner));
            }

            try
            {
                var hashInput = new StringBuilder();
                
                // Add hex state
                AddHexState(hashInput, hexSpawner);
                
                // Add edge state if available
                if (edgeSpawner != null)
                {
                    AddEdgeState(hashInput, edgeSpawner);
                }
                
                // Add corner state if available
                if (cornerSpawner != null)
                {
                    AddCornerState(hashInput, cornerSpawner);
                }

                // Compute SHA-256 hash
                using (var sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(hashInput.ToString());
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Error, 
                    $"Error computing board hash: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Computes a hash of only the hex state (ignores edges and corners).
        /// </summary>
        public static string ComputeHexHash(HexSpawner hexSpawner)
        {
            return ComputeHash(hexSpawner, null, null);
        }

        /// <summary>
        /// Computes a hash of only the edge state.
        /// </summary>
        public static string ComputeEdgeHash(EdgeSpawner edgeSpawner)
        {
            if (edgeSpawner == null)
            {
                throw new ArgumentNullException(nameof(edgeSpawner));
            }

            try
            {
                var hashInput = new StringBuilder();
                AddEdgeState(hashInput, edgeSpawner);

                using (var sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(hashInput.ToString());
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Error, 
                    $"Error computing edge hash: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Computes a hash of only the corner state.
        /// </summary>
        public static string ComputeCornerHash(CornerSpawner cornerSpawner)
        {
            if (cornerSpawner == null)
            {
                throw new ArgumentNullException(nameof(cornerSpawner));
            }

            try
            {
                var hashInput = new StringBuilder();
                AddCornerState(hashInput, cornerSpawner);

                using (var sha256 = SHA256.Create())
                {
                    var inputBytes = Encoding.UTF8.GetBytes(hashInput.ToString());
                    var hashBytes = sha256.ComputeHash(inputBytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Error, 
                    $"Error computing corner hash: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Compares two board states and returns detailed comparison results.
        /// </summary>
        public static BoardHashComparison CompareBoards(HexSpawner hex1, EdgeSpawner edge1, CornerSpawner corner1,
                                                      HexSpawner hex2, EdgeSpawner edge2, CornerSpawner corner2)
        {
            var comparison = new BoardHashComparison();

            try
            {
                // Compute hashes for both boards
                comparison.Board1Hash = ComputeHash(hex1, edge1, corner1);
                comparison.Board2Hash = ComputeHash(hex2, edge2, corner2);
                comparison.AreIdentical = comparison.Board1Hash == comparison.Board2Hash;

                // Detailed component comparison
                comparison.HexHash1 = ComputeHexHash(hex1);
                comparison.HexHash2 = ComputeHexHash(hex2);
                comparison.HexesMatch = comparison.HexHash1 == comparison.HexHash2;

                if (edge1 != null && edge2 != null)
                {
                    comparison.EdgeHash1 = ComputeEdgeHash(edge1);
                    comparison.EdgeHash2 = ComputeEdgeHash(edge2);
                    comparison.EdgesMatch = comparison.EdgeHash1 == comparison.EdgeHash2;
                }

                if (corner1 != null && corner2 != null)
                {
                    comparison.CornerHash1 = ComputeCornerHash(corner1);
                    comparison.CornerHash2 = ComputeCornerHash(corner2);
                    comparison.CornersMatch = comparison.CornerHash1 == comparison.CornerHash2;
                }

                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Info, 
                    $"Board comparison completed: Identical={comparison.AreIdentical}, " +
                    $"Hexes={comparison.HexesMatch}, Edges={comparison.EdgesMatch}, Corners={comparison.CornersMatch}");
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Error, 
                    $"Error comparing boards: {ex.Message}");
                comparison.Error = ex.Message;
            }

            return comparison;
        }

        /// <summary>
        /// Validates board state integrity by comparing with expected hash.
        /// </summary>
        public static bool ValidateBoardIntegrity(HexSpawner hexSpawner, EdgeSpawner edgeSpawner, CornerSpawner cornerSpawner, string expectedHash)
        {
            if (string.IsNullOrEmpty(expectedHash))
            {
                throw new ArgumentException("Expected hash cannot be null or empty", nameof(expectedHash));
            }

            try
            {
                var actualHash = ComputeHash(hexSpawner, edgeSpawner, cornerSpawner);
                var isValid = actualHash == expectedHash;

                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, 
                    isValid ? ActionLogSeverity.Info : ActionLogSeverity.Warning,
                    $"Board integrity validation: Expected={expectedHash}, Actual={actualHash}, Valid={isValid}");

                return isValid;
            }
            catch (Exception ex)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Configuration, ActionLogSeverity.Error, 
                    $"Error validating board integrity: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Adds hex state information to the hash input.
        /// </summary>
        private static void AddHexState(StringBuilder hashInput, HexSpawner hexSpawner)
        {
            if (hexSpawner?.State?.hexes == null) return;

            // Sort hexes by coordinates for deterministic ordering
            var sortedHexes = hexSpawner.State.hexes
                .Where(row => row != null)
                .SelectMany(row => row)
                .Where(h => h != null)
                .OrderBy(h => h.Col)
                .ThenBy(h => h.Row)
                .ToList();

            foreach (var hex in sortedHexes)
            {
                hashInput.AppendLine($"HEX:{hex.Col}:{hex.Row}:{hex.HexType}:{hex.HexSubType}:{hex.Rotation}:{hex.HexNum}:{hex.GroupID}:{hex.Selected}");
            }
        }

        /// <summary>
        /// Adds edge state information to the hash input.
        /// </summary>
        private static void AddEdgeState(StringBuilder hashInput, EdgeSpawner edgeSpawner)
        {
            if (edgeSpawner?.State?.edges == null) return;

            // Sort edges for deterministic ordering
            var sortedEdges = edgeSpawner.State.edges
                .Where(e => e != null)
                .OrderBy(e => Math.Min(e.hex1Col, e.hex2Col))
                .ThenBy(e => Math.Min(e.hex1Row, e.hex2Row))
                .ThenBy(e => Math.Max(e.hex1Col, e.hex2Col))
                .ThenBy(e => Math.Max(e.hex1Row, e.hex2Row))
                .ThenBy(e => e.edgeType)
                .ToList();

            foreach (var edge in sortedEdges)
            {
                // Normalize edge representation (always store smaller coordinates first)
                int minCol = Math.Min(edge.hex1Col, edge.hex2Col);
                int minRow = Math.Min(edge.hex1Row, edge.hex2Row);
                int maxCol = Math.Max(edge.hex1Col, edge.hex2Col);
                int maxRow = Math.Max(edge.hex1Row, edge.hex2Row);

                hashInput.AppendLine($"EDGE:{minCol}:{minRow}:{maxCol}:{maxRow}:{edge.edgeType}");
            }
        }

        /// <summary>
        /// Adds corner state information to the hash input.
        /// </summary>
        private static void AddCornerState(StringBuilder hashInput, CornerSpawner cornerSpawner)
        {
            if (cornerSpawner?.State?.corners == null) return;

            // Sort corners for deterministic ordering
            var sortedCorners = cornerSpawner.State.corners
                .Where(c => c != null)
                .OrderBy(c => c.hexCol)
                .ThenBy(c => c.hexRow)
                .ThenBy(c => c.vertexIndex)
                .ThenBy(c => c.cornerType)
                .ToList();

            foreach (var corner in sortedCorners)
            {
                hashInput.AppendLine($"CORNER:{corner.hexCol}:{corner.hexRow}:{corner.vertexIndex}:{corner.cornerType}");
            }
        }
    }

    /// <summary>
    /// Result of board hash comparison operation.
    /// </summary>
    [Serializable]
    public class BoardHashComparison
    {
        public string Board1Hash { get; set; }
        public string Board2Hash { get; set; }
        public bool AreIdentical { get; set; }

        public string HexHash1 { get; set; }
        public string HexHash2 { get; set; }
        public bool HexesMatch { get; set; }

        public string EdgeHash1 { get; set; }
        public string EdgeHash2 { get; set; }
        public bool EdgesMatch { get; set; }

        public string CornerHash1 { get; set; }
        public string CornerHash2 { get; set; }
        public bool CornersMatch { get; set; }

        public string Error { get; set; }

        /// <summary>
        /// Gets a summary of the comparison results.
        /// </summary>
        public string GetSummary()
        {
            if (!string.IsNullOrEmpty(Error))
            {
                return $"Error: {Error}";
            }

            var summary = new StringBuilder();
            summary.AppendLine($"Overall: {(AreIdentical ? "IDENTICAL" : "DIFFERENT")}");
            summary.AppendLine($"  Board1: {Board1Hash}");
            summary.AppendLine($"  Board2: {Board2Hash}");
            summary.AppendLine($"  Hexes: {(HexesMatch ? "MATCH" : "DIFFER")}");
            summary.AppendLine($"  Edges: {(EdgesMatch ? "MATCH" : "DIFFER")}");
            summary.AppendLine($"  Corners: {(CornersMatch ? "MATCH" : "DIFFER")}");

            return summary.ToString();
        }
    }
}
