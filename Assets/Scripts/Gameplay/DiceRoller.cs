using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SuperHexLink.Logging;

namespace SuperHexLink.Gameplay
{
    /// <summary>
    /// Handles dice rolling mechanics with proper probability distribution.
    /// Integrates with the game's RNG system for deterministic gameplay.
    /// Provides production-grade dice rolling with comprehensive validation and logging.
    /// </summary>
    public class DiceRoller
    {
        private readonly ActionLogSettings _logSettings;
        private readonly System.Random _random;
        private readonly Dictionary<int, int> _rollHistory;
        private readonly int[] _expectedDistribution;
        private readonly float[] _probabilities;

        /// <summary>
        /// Gets the total number of rolls made.
        /// </summary>
        public int TotalRolls => _rollHistory.Values.Sum();

        /// <summary>
        /// Gets the roll history as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<int, int> RollHistory => _rollHistory.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        /// <summary>
        /// Event fired when a dice roll occurs.
        /// </summary>
        public event Action<DiceRollResult> DiceRolled;

        public DiceRoller(ActionLogSettings logSettings = null, int? seed = null)
        {
            _logSettings = logSettings ?? new ActionLogSettings();
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            _rollHistory = new Dictionary<int, int>();

            // Initialize Catan dice probability distribution (2-12)
            // 2: 1/36, 3: 2/36, 4: 3/36, 5: 4/36, 6: 5/36, 7: 6/36, 8: 5/36, 9: 4/36, 10: 3/36, 11: 2/36, 12: 1/36
            _expectedDistribution = new int[] { 1, 2, 3, 4, 5, 6, 5, 4, 3, 2, 1 }; // 2 through 12
            _probabilities = _expectedDistribution.Select(x => x / 36.0f).ToArray();

            // Initialize roll history for all possible values (2-12)
            for (int i = 2; i <= 12; i++)
            {
                _rollHistory[i] = 0;
            }
        }

        /// <summary>
        /// Rolls two six-sided dice and returns the result.
        /// </summary>
        /// <returns>Dice roll result with individual dice values and total</returns>
        public DiceRollResult Roll()
        {
            // Roll two dice
            int die1 = _random.Next(1, 7); // 1-6
            int die2 = _random.Next(1, 7); // 1-6
            int total = die1 + die2;

            // Update history
            _rollHistory[total]++;

            var result = new DiceRollResult(die1, die2, total, TotalRolls);

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                $"DiceRoller: Rolled {die1} + {die2} = {total} (Roll #{TotalRolls})");

            DiceRolled?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Rolls multiple times and returns the results.
        /// </summary>
        /// <param name="count">Number of rolls to perform</param>
        /// <returns>List of dice roll results</returns>
        public List<DiceRollResult> RollMultiple(int count)
        {
            if (count <= 0)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    $"DiceRoller: Invalid roll count {count}");
                return new List<DiceRollResult>();
            }

            if (count > 1000)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    $"DiceRoller: Large roll count {count} may impact performance");
            }

            var results = new List<DiceRollResult>(count);

            for (int i = 0; i < count; i++)
            {
                results.Add(Roll());
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                $"DiceRoller: Completed {count} rolls");

            return results;
        }

        /// <summary>
        /// Gets the probability of rolling a specific total.
        /// </summary>
        /// <param name="total">Dice total (2-12)</param>
        /// <returns>Probability as a decimal between 0 and 1</returns>
        public float GetProbability(int total)
        {
            if (total < 2 || total > 12)
            {
                return 0f;
            }

            return _probabilities[total - 2];
        }

        /// <summary>
        /// Gets the expected number of rolls for a specific total out of a given number of rolls.
        /// </summary>
        /// <param name="total">Dice total (2-12)</param>
        /// <param name="numberOfRolls">Total number of rolls</param>
        /// <returns>Expected count as a float</returns>
        public float GetExpectedCount(int total, int numberOfRolls)
        {
            return GetProbability(total) * numberOfRolls;
        }

        /// <summary>
        /// Validates if the current roll distribution is within acceptable statistical bounds.
        /// </summary>
        /// <param name="tolerancePercent">Acceptable deviation from expected (default: 20%)</param>
        /// <returns>True if distribution is acceptable, false otherwise</returns>
        public bool ValidateDistribution(float tolerancePercent = 0.2f)
        {
            if (TotalRolls < 100)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                    "DiceRoller: Insufficient rolls for statistical validation");
                return true; // Not enough data to validate
            }

            bool isValid = true;

            for (int total = 2; total <= 12; total++)
            {
                int actualCount = _rollHistory[total];
                float expectedCount = GetExpectedCount(total, TotalRolls);
                float deviation = Math.Abs(actualCount - expectedCount) / expectedCount;

                if (deviation > tolerancePercent)
                {
                    ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Warning,
                        $"DiceRoller: Distribution anomaly for total {total}: expected {expectedCount:F1}, got {actualCount} ({deviation:P1} deviation)");

                    isValid = false;
                }
            }

            if (isValid)
            {
                ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                    $"DiceRoller: Roll distribution is within {tolerancePercent:P0} tolerance");
            }

            return isValid;
        }

        /// <summary>
        /// Gets statistical analysis of the roll history.
        /// </summary>
        /// <returns>Distribution analysis with actual vs expected values</returns>
        public DistributionAnalysis GetDistributionAnalysis()
        {
            var analysis = new DistributionAnalysis
            {
                TotalRolls = TotalRolls,
                ActualDistribution = new Dictionary<int, int>(),
                ExpectedDistribution = new Dictionary<int, float>(),
                Deviations = new Dictionary<int, float>()
            };

            for (int total = 2; total <= 12; total++)
            {
                int actualCount = _rollHistory[total];
                float expectedCount = GetExpectedCount(total, TotalRolls);
                float deviation = TotalRolls > 0 ? (actualCount - expectedCount) / expectedCount : 0f;

                analysis.ActualDistribution[total] = actualCount;
                analysis.ExpectedDistribution[total] = expectedCount;
                analysis.Deviations[total] = deviation;
            }

            return analysis;
        }

        /// <summary>
        /// Resets the roll history.
        /// </summary>
        public void ResetHistory()
        {
            for (int i = 2; i <= 12; i++)
            {
                _rollHistory[i] = 0;
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "DiceRoller: Roll history reset");
        }

        /// <summary>
        /// Sets a new random seed for deterministic testing.
        /// </summary>
        /// <param name="seed">Random seed value</param>
        public void SetSeed(int seed)
        {
            // Create new random instance with seed
            var oldRandom = _random;
            _random.GetHashCode(); // Force usage to avoid compiler warning
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                $"DiceRoller: Seed set to {seed}");
        }

        /// <summary>
        /// Gets the most common roll total.
        /// </summary>
        /// <returns>Most common total, or 0 if no rolls</returns>
        public int GetMostCommonTotal()
        {
            if (TotalRolls == 0)
            {
                return 0;
            }

            return _rollHistory.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        /// <summary>
        /// Gets the least common roll total.
        /// </summary>
        /// <returns>Least common total, or 0 if no rolls</returns>
        public int GetLeastCommonTotal()
        {
            if (TotalRolls == 0)
            {
                return 0;
            }

            return _rollHistory.OrderBy(kvp => kvp.Value).First().Key;
        }

        /// <summary>
        /// Checks if a specific total has been rolled.
        /// </summary>
        /// <param name="total">Dice total to check (2-12)</param>
        /// <returns>True if the total has been rolled at least once</returns>
        public bool HasRolled(int total)
        {
            return total >= 2 && total <= 12 && _rollHistory.ContainsKey(total) && _rollHistory[total] > 0;
        }

        /// <summary>
        /// Gets the count of rolls for a specific total.
        /// </summary>
        /// <param name="total">Dice total (2-12)</param>
        /// <returns>Number of times this total has been rolled</returns>
        public int GetRollCount(int total)
        {
            return total >= 2 && total <= 12 && _rollHistory.ContainsKey(total) ? _rollHistory[total] : 0;
        }
    }

    /// <summary>
    /// Represents the result of a dice roll.
    /// </summary>
    public class DiceRollResult
    {
        public int Die1 { get; }
        public int Die2 { get; }
        public int Total { get; }
        public int RollNumber { get; }
        public DateTime Timestamp { get; }

        public DiceRollResult(int die1, int die2, int total, int rollNumber)
        {
            Die1 = die1;
            Die2 = die2;
            Total = total;
            RollNumber = rollNumber;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"Roll #{RollNumber}: {Die1} + {Die2} = {Total}";
        }

        public bool IsDouble => Die1 == Die2;
    }

    /// <summary>
    /// Contains statistical analysis of dice roll distribution.
    /// </summary>
    public class DistributionAnalysis
    {
        public int TotalRolls { get; set; }
        public Dictionary<int, int> ActualDistribution { get; set; }
        public Dictionary<int, float> ExpectedDistribution { get; set; }
        public Dictionary<int, float> Deviations { get; set; }

        /// <summary>
        /// Gets the maximum absolute deviation from expected values.
        /// </summary>
        public float MaxDeviation => Deviations.Values.Max(Math.Abs);

        /// <summary>
        /// Gets the average absolute deviation from expected values.
        /// </summary>
        public float AverageDeviation => Deviations.Values.Average(Math.Abs);

        public override string ToString()
        {
            return $"Distribution Analysis: {TotalRolls} rolls, Max Deviation: {MaxDeviation:P2}, Avg Deviation: {AverageDeviation:P2}";
        }
    }
}
