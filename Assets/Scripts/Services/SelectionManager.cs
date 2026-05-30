using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Services
{
    /// <summary>
    /// Manager service for hex selection and placement rules.
    /// Extracts selection logic from HexSpawner for better separation of concerns.
    /// Handles hex type selection, placement validation, and selection statistics.
    /// </summary>
    public class SelectionManager
    {
        private readonly GameConstants _gameConstants;
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<string, int> _selectionAttempts;
        private readonly System.Random _random;

        public SelectionManager(GameConstants gameConstants, ActionLogSettings logSettings = null)
        {
            _gameConstants = gameConstants ?? throw new ArgumentNullException(nameof(gameConstants));
            _logSettings = logSettings ?? new ActionLogSettings();
            _selectionAttempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _random = new System.Random();
        }

        /// <summary>
        /// Selects a hex type from the available candidates based on placement rules.
        /// </summary>
        /// <param name="hex">The hex to place</param>
        /// <param name="candidates">List of candidate hex types</param>
        /// <returns>Selection result with chosen type, rule, and metadata</returns>
        public SelectionResult SelectHexType(Hex hex, List<string> candidates)
        {
            if (hex == null)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Error,
                    "SelectionManager: Cannot select type for null hex");
                return new SelectionResult(GameConstants.CAR_TYPE_WORD_NULL, null, false, false, 0);
            }

            if (candidates == null || candidates.Count == 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Warning,
                    $"SelectionManager: No candidates available for hex at ({hex.hexState?.Col ?? -1}, {hex.hexState?.Row ?? -1})");
                return new SelectionResult(GameConstants.CAR_TYPE_WORD_NULL, null, false, false, 0);
            }

            _selectionAttempts.Clear();
            int iteration = 0;
            int maxIterations = Math.Max(candidates.Count * 2, 80);

            while (iteration < maxIterations)
            {
                iteration++;
                int index = _random.Next(0, candidates.Count);
                string candidate = candidates[index];
                
                var rule = _gameConstants?.GetPlacementRule(candidate);
                if (rule == null)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                        $"SelectionManager: No placement rule for '{candidate}', using as candidate");
                    return new SelectionResult(candidate, null, true, false, iteration);
                }

                // Track attempts for this candidate
                if (!_selectionAttempts.TryGetValue(candidate, out int attemptCount))
                {
                    attemptCount = 0;
                }
                attemptCount++;
                _selectionAttempts[candidate] = attemptCount;

                // Check if placement is allowed
                if (AllowsPlacement(rule, hex))
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                        $"SelectionManager: Selected '{candidate}' for hex at ({hex.hexState.Col}, {hex.hexState.Row}) after {attemptCount} attempts");
                    return new SelectionResult(candidate, rule, true, false, attemptCount);
                }

                // Check if we should use fallback
                if (attemptCount >= Math.Max(rule.maxAttemptsBeforeFallback, 1))
                {
                    string fallback = string.IsNullOrEmpty(rule.fallbackHexType) ? GameConstants.CAR_TYPE_SEA : rule.fallbackHexType;
                    ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Warning,
                        $"SelectionManager: Using fallback '{fallback}' for hex at ({hex.hexState.Col}, {hex.hexState.Row}) after {attemptCount} attempts with rule '{rule.ruleName}'");
                    return new SelectionResult(fallback, rule, false, true, attemptCount);
                }
            }

            // Fallback to cycling through candidates
            string fallbackType = candidates[iteration % candidates.Count];
            var fallbackRule = _gameConstants?.GetPlacementRule(fallbackType);
            
            ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Warning,
                $"SelectionManager: Exhausted {maxIterations} iterations, falling back to '{fallbackType}' for hex at ({hex.hexState.Col}, {hex.hexState.Row})");
            
            return new SelectionResult(fallbackType, fallbackRule, true, false, iteration);
        }

        /// <summary>
        /// Gets the current selection statistics for debugging.
        /// </summary>
        /// <returns>Dictionary of candidate types and their attempt counts</returns>
        public IReadOnlyDictionary<string, int> GetSelectionStatistics()
        {
            return _selectionAttempts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        /// <summary>
        /// Clears selection statistics.
        /// </summary>
        public void ClearStatistics()
        {
            _selectionAttempts.Clear();
            ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                "SelectionManager: Cleared selection statistics");
        }

        /// <summary>
        /// Validates if a hex type can be placed at the specified location.
        /// </summary>
        /// <param name="hexType">Type of hex to validate</param>
        /// <param name="hex">Target hex location</param>
        /// <returns>True if placement is allowed</returns>
        public bool ValidatePlacement(string hexType, Hex hex)
        {
            if (string.IsNullOrEmpty(hexType) || hex == null)
            {
                return false;
            }

            var rule = _gameConstants?.GetPlacementRule(hexType);
            if (rule == null)
            {
                // No rule means placement is allowed
                return true;
            }

            return AllowsPlacement(rule, hex);
        }

        /// <summary>
        /// Gets all valid placement candidates for a hex location.
        /// </summary>
        /// <param name="hex">Target hex location</param>
        /// <param name="allCandidates">All possible candidates</param>
        /// <returns>List of candidates that can be placed at this location</returns>
        public List<string> GetValidCandidates(Hex hex, List<string> allCandidates)
        {
            if (hex == null || allCandidates == null)
            {
                return new List<string>();
            }

            var validCandidates = new List<string>();
            
            foreach (string candidate in allCandidates)
            {
                if (ValidatePlacement(candidate, hex))
                {
                    validCandidates.Add(candidate);
                }
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                $"SelectionManager: Found {validCandidates.Count} valid candidates out of {allCandidates.Count} for hex at ({hex.hexState.Col}, {hex.hexState.Row})");

            return validCandidates;
        }

        /// <summary>
        /// Gets placement rule information for a hex type.
        /// </summary>
        /// <param name="hexType">Hex type to get rule for</param>
        /// <returns>Placement rule or null if not found</returns>
        public HexPlacementRuleConfig GetPlacementRule(string hexType)
        {
            return _gameConstants?.GetPlacementRule(hexType);
        }

        /// <summary>
        /// Checks if placement is allowed according to the rule.
        /// </summary>
        /// <param name="rule">Placement rule to check</param>
        /// <param name="hex">Target hex</param>
        /// <returns>True if placement is allowed</returns>
        private bool AllowsPlacement(HexPlacementRuleConfig rule, Hex hex)
        {
            if (rule == null || hex == null || hex.hexState == null)
            {
                return false;
            }

            // This would contain the actual placement rule logic
            // For now, this is a placeholder that would need to be implemented
            // based on the specific placement rule engine used in the project
            
            // Example implementation (would need to be replaced with actual rule engine):
            return true; // Placeholder - implement actual rule logic
        }

        /// <summary>
        /// Sets the random seed for deterministic testing.
        /// </summary>
        /// <param name="seed">Random seed</param>
        public void SetRandomSeed(int seed)
        {
            _random = new System.Random(seed);
            ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                $"SelectionManager: Set random seed to {seed}");
        }

        /// <summary>
        /// Gets a random candidate from the list (for simple random selection without rules).
        /// </summary>
        /// <param name="candidates">List of candidates</param>
        /// <returns>Random candidate or null if list is empty</returns>
        public string GetRandomCandidate(List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            int index = _random.Next(0, candidates.Count);
            return candidates[index];
        }

        /// <summary>
        /// Filters candidates based on simple criteria (e.g., excluding certain types).
        /// </summary>
        /// <param name="candidates">Original candidates</param>
        /// <param name="excludeTypes">Types to exclude</param>
        /// <returns>Filtered candidates</returns>
        public List<string> FilterCandidates(List<string> candidates, List<string> excludeTypes)
        {
            if (candidates == null)
            {
                return new List<string>();
            }

            if (excludeTypes == null || excludeTypes.Count == 0)
            {
                return new List<string>(candidates);
            }

            var filtered = candidates.Where(candidate => 
                !excludeTypes.Contains(candidate, StringComparer.OrdinalIgnoreCase)).ToList();

            ActionLogger.Log(_logSettings, ActionLogCategory.HexSelection, ActionLogSeverity.Debug,
                $"SelectionManager: Filtered {candidates.Count} candidates to {filtered.Count} after excluding {excludeTypes.Count} types");

            return filtered;
        }
    }

    /// <summary>
    /// Result of a hex type selection operation.
    /// </summary>
    public class SelectionResult
    {
        public string SelectedType { get; }
        public HexPlacementRuleConfig Rule { get; }
        public bool UsedCandidate { get; }
        public bool UsedFallback { get; }
        public int Attempts { get; }

        public SelectionResult(string selectedType, HexPlacementRuleConfig rule, bool usedCandidate, bool usedFallback, int attempts)
        {
            SelectedType = selectedType;
            Rule = rule;
            UsedCandidate = usedCandidate;
            UsedFallback = usedFallback;
            Attempts = attempts;
        }

        public override string ToString()
        {
            return $"Selected: {SelectedType}, Rule: {Rule?.ruleName ?? "None"}, Candidate: {UsedCandidate}, Fallback: {UsedFallback}, Attempts: {Attempts}";
        }
    }
}
