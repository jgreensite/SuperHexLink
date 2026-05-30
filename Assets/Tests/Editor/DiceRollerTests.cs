using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SuperHexLink.Gameplay;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for DiceRoller.
    /// Verifies dice rolling mechanics, probability distribution, and statistical validation.
    /// </summary>
    public class DiceRollerTests
    {
        private DiceRoller _diceRoller;

        [SetUp]
        public void SetUp()
        {
            _diceRoller = new DiceRoller(new ActionLogSettings(), seed: 42); // Deterministic seed for testing
        }

        [Test]
        public void DiceRoller_Roll_ValidRange_ReturnsValidResult()
        {
            // Act
            DiceRollResult result = _diceRoller.Roll();

            // Assert
            Assert.IsTrue(result.Die1 >= 1 && result.Die1 <= 6, "Die 1 should be between 1 and 6");
            Assert.IsTrue(result.Die2 >= 1 && result.Die2 <= 6, "Die 2 should be between 1 and 6");
            Assert.IsTrue(result.Total >= 2 && result.Total <= 12, "Total should be between 2 and 12");
            Assert.AreEqual(result.Die1 + result.Die2, result.Total, "Total should equal sum of dice");
            Assert.AreEqual(1, result.RollNumber, "First roll should be roll number 1");
        }

        [Test]
        public void DiceRoller_RollMultiple_ValidCount_ReturnsCorrectCount()
        {
            // Arrange
            int rollCount = 10;

            // Act
            List<DiceRollResult> results = _diceRoller.RollMultiple(rollCount);

            // Assert
            Assert.AreEqual(rollCount, results.Count, "Should return correct number of results");
            Assert.AreEqual(rollCount + 1, _diceRoller.TotalRolls, "Total rolls should be updated");

            for (int i = 0; i < results.Count; i++)
            {
                Assert.IsTrue(results[i].Total >= 2 && results[i].Total <= 12, $"Roll {i + 1} should be valid");
                Assert.AreEqual(i + 2, results[i].RollNumber, $"Roll {i + 1} should have correct roll number");
            }
        }

        [Test]
        public void DiceRoller_RollMultiple_ZeroCount_ReturnsEmptyList()
        {
            // Act
            List<DiceRollResult> results = _diceRoller.RollMultiple(0);

            // Assert
            Assert.AreEqual(0, results.Count, "Should return empty list for zero count");
        }

        [Test]
        public void DiceRoller_RollMultiple_NegativeCount_ReturnsEmptyList()
        {
            // Act
            List<DiceRollResult> results = _diceRoller.RollMultiple(-5);

            // Assert
            Assert.AreEqual(0, results.Count, "Should return empty list for negative count");
        }

        [Test]
        public void DiceRoller_GetProbability_ValidTotal_ReturnsCorrectProbability()
        {
            // Test known probabilities for Catan dice
            Assert.AreEqual(1f / 36f, _diceRoller.GetProbability(2), 0.001f, "Probability of 2 should be 1/36");
            Assert.AreEqual(2f / 36f, _diceRoller.GetProbability(3), 0.001f, "Probability of 3 should be 2/36");
            Assert.AreEqual(3f / 36f, _diceRoller.GetProbability(4), 0.001f, "Probability of 4 should be 3/36");
            Assert.AreEqual(4f / 36f, _diceRoller.GetProbability(5), 0.001f, "Probability of 5 should be 4/36");
            Assert.AreEqual(5f / 36f, _diceRoller.GetProbability(6), 0.001f, "Probability of 6 should be 5/36");
            Assert.AreEqual(6f / 36f, _diceRoller.GetProbability(7), 0.001f, "Probability of 7 should be 6/36");
            Assert.AreEqual(5f / 36f, _diceRoller.GetProbability(8), 0.001f, "Probability of 8 should be 5/36");
            Assert.AreEqual(4f / 36f, _diceRoller.GetProbability(9), 0.001f, "Probability of 9 should be 4/36");
            Assert.AreEqual(3f / 36f, _diceRoller.GetProbability(10), 0.001f, "Probability of 10 should be 3/36");
            Assert.AreEqual(2f / 36f, _diceRoller.GetProbability(11), 0.001f, "Probability of 11 should be 2/36");
            Assert.AreEqual(1f / 36f, _diceRoller.GetProbability(12), 0.001f, "Probability of 12 should be 1/36");
        }

        [Test]
        public void DiceRoller_GetProbability_InvalidTotal_ReturnsZero()
        {
            // Act & Assert
            Assert.AreEqual(0f, _diceRoller.GetProbability(1), "Probability of 1 should be 0");
            Assert.AreEqual(0f, _diceRoller.GetProbability(13), "Probability of 13 should be 0");
            Assert.AreEqual(0f, _diceRoller.GetProbability(0), "Probability of 0 should be 0");
            Assert.AreEqual(0f, _diceRoller.GetProbability(100), "Probability of 100 should be 0");
        }

        [Test]
        public void DiceRoller_GetExpectedCount_ValidParameters_ReturnsCorrectCount()
        {
            // Act & Assert
            Assert.AreEqual(36f * (1f / 36f), _diceRoller.GetExpectedCount(2, 36), 0.001f, "Expected count for 2 in 36 rolls should be 1");
            Assert.AreEqual(36f * (6f / 36f), _diceRoller.GetExpectedCount(7, 36), 0.001f, "Expected count for 7 in 36 rolls should be 6");
            Assert.AreEqual(100f * (1f / 36f), _diceRoller.GetExpectedCount(2, 100), 0.001f, "Expected count for 2 in 100 rolls should be ~2.78");
        }

        [Test]
        public void DiceRoller_ValidateDistribution_InsufficientRolls_ReturnsTrue()
        {
            // Arrange
            _diceRoller.Roll(); // Only 1 roll

            // Act
            bool isValid = _diceRoller.ValidateDistribution();

            // Assert
            Assert.IsTrue(isValid, "Should return true for insufficient rolls");
        }

        [Test]
        public void DiceRoller_ValidateDistribution_GoodDistribution_ReturnsTrue()
        {
            // Arrange - Roll enough times to get a reasonable distribution
            for (int i = 0; i < 360; i++) // 10x expected distribution
            {
                _diceRoller.Roll();
            }

            // Act
            bool isValid = _diceRoller.ValidateDistribution(0.5f); // 50% tolerance

            // Assert
            Assert.IsTrue(isValid, "Good distribution should pass validation");
        }

        [Test]
        public void DiceRoller_GetDistributionAnalysis_ReturnsCorrectAnalysis()
        {
            // Arrange
            for (int i = 0; i < 100; i++)
            {
                _diceRoller.Roll();
            }

            // Act
            DistributionAnalysis analysis = _diceRoller.GetDistributionAnalysis();

            // Assert
            Assert.AreEqual(100, analysis.TotalRolls, "Should have correct total rolls");
            Assert.AreEqual(11, analysis.ActualDistribution.Count, "Should have 11 possible totals");
            Assert.AreEqual(11, analysis.ExpectedDistribution.Count, "Should have 11 expected values");
            Assert.AreEqual(11, analysis.Deviations.Count, "Should have 11 deviations");

            // Check that all expected values are present
            for (int total = 2; total <= 12; total++)
            {
                Assert.IsTrue(analysis.ActualDistribution.ContainsKey(total), $"Should contain actual count for {total}");
                Assert.IsTrue(analysis.ExpectedDistribution.ContainsKey(total), $"Should contain expected count for {total}");
                Assert.IsTrue(analysis.Deviations.ContainsKey(total), $"Should contain deviation for {total}");
            }
        }

        [Test]
        public void DiceRoller_ResetHistory_ClearsHistory()
        {
            // Arrange
            _diceRoller.Roll();
            _diceRoller.Roll();
            Assert.AreEqual(2, _diceRoller.TotalRolls, "Should have 2 rolls");

            // Act
            _diceRoller.ResetHistory();

            // Assert
            Assert.AreEqual(0, _diceRoller.TotalRolls, "Should have 0 rolls after reset");
            
            for (int total = 2; total <= 12; total++)
            {
                Assert.AreEqual(0, _diceRoller.GetRollCount(total), $"Roll count for {total} should be 0");
            }
        }

        [Test]
        public void DiceRoller_SetSeed_DeterministicResults()
        {
            // Arrange
            int seed = 123;
            DiceRoller roller1 = new DiceRoller(seed: seed);
            DiceRoller roller2 = new DiceRoller(seed: seed);

            // Act
            DiceRollResult result1 = roller1.Roll();
            DiceRollResult result2 = roller2.Roll();

            // Assert
            Assert.AreEqual(result1.Die1, result2.Die1, "Same seed should produce same die 1");
            Assert.AreEqual(result1.Die2, result2.Die2, "Same seed should produce same die 2");
            Assert.AreEqual(result1.Total, result2.Total, "Same seed should produce same total");
        }

        [Test]
        public void DiceRoller_GetMostCommonTotal_ReturnsCorrectTotal()
        {
            // Arrange
            Dictionary<int, int> rollCounts = new Dictionary<int, int>();
            
            // Roll many times and track counts
            for (int i = 0; i < 100; i++)
            {
                DiceRollResult result = _diceRoller.Roll();
                rollCounts[result.Total] = rollCounts.GetValueOrDefault(result.Total, 0) + 1;
            }

            // Act
            int mostCommon = _diceRoller.GetMostCommonTotal();

            // Assert
            int expectedMostCommon = rollCounts.OrderByDescending(kvp => kvp.Value).First().Key;
            Assert.AreEqual(expectedMostCommon, mostCommon, "Should return the most common total");
        }

        [Test]
        public void DiceRoller_GetLeastCommonTotal_ReturnsCorrectTotal()
        {
            // Arrange
            Dictionary<int, int> rollCounts = new Dictionary<int, int>();
            
            // Roll many times and track counts
            for (int i = 0; i < 100; i++)
            {
                DiceRollResult result = _diceRoller.Roll();
                rollCounts[result.Total] = rollCounts.GetValueOrDefault(result.Total, 0) + 1;
            }

            // Act
            int leastCommon = _diceRoller.GetLeastCommonTotal();

            // Assert
            int expectedLeastCommon = rollCounts.OrderBy(kvp => kvp.Value).First().Key;
            Assert.AreEqual(expectedLeastCommon, leastCommon, "Should return the least common total");
        }

        [Test]
        public void DiceRoller_GetMostCommonTotal_NoRolls_ReturnsZero()
        {
            // Act
            int mostCommon = _diceRoller.GetMostCommonTotal();

            // Assert
            Assert.AreEqual(0, mostCommon, "Should return 0 when no rolls");
        }

        [Test]
        public void DiceRoller_GetLeastCommonTotal_NoRolls_ReturnsZero()
        {
            // Act
            int leastCommon = _diceRoller.GetLeastCommonTotal();

            // Assert
            Assert.AreEqual(0, leastCommon, "Should return 0 when no rolls");
        }

        [Test]
        public void DiceRoller_HasRolled_ValidTotal_ReturnsCorrectStatus()
        {
            // Arrange
            DiceRollResult result = _diceRoller.Roll();

            // Act & Assert
            Assert.IsTrue(_diceRoller.HasRolled(result.Total), "Should return true for rolled total");
            Assert.IsFalse(_diceRoller.HasRolled(13), "Should return false for impossible total");
        }

        [Test]
        public void DiceRoller_GetRollCount_ValidTotal_ReturnsCorrectCount()
        {
            // Arrange
            int targetTotal = 7;
            int expectedCount = 0;

            // Roll and count specific totals
            for (int i = 0; i < 20; i++)
            {
                DiceRollResult result = _diceRoller.Roll();
                if (result.Total == targetTotal)
                {
                    expectedCount++;
                }
            }

            // Act
            int actualCount = _diceRoller.GetRollCount(targetTotal);

            // Assert
            Assert.AreEqual(expectedCount, actualCount, "Should return correct roll count");
        }

        [Test]
        public void DiceRoller_GetRollCount_InvalidTotal_ReturnsZero()
        {
            // Act & Assert
            Assert.AreEqual(0, _diceRoller.GetRollCount(1), "Should return 0 for invalid total 1");
            Assert.AreEqual(0, _diceRoller.GetRollCount(13), "Should return 0 for invalid total 13");
        }

        [Test]
        public void DiceRoller_Events_FireCorrectly()
        {
            // Arrange
            bool eventFired = false;
            DiceRollResult capturedResult = null;

            _diceRoller.DiceRolled += (result) =>
            {
                eventFired = true;
                capturedResult = result;
            };

            // Act
            DiceRollResult result = _diceRoller.Roll();

            // Assert
            Assert.IsTrue(eventFired, "DiceRolled event should fire");
            Assert.IsNotNull(capturedResult, "Event should capture result");
            Assert.AreEqual(result.Total, capturedResult.Total, "Event should pass correct result");
        }

        [Test]
        public void DiceRoller_DiceRollResult_IsDouble_ReturnsCorrectValue()
        {
            // Act
            List<DiceRollResult> results = _diceRoller.RollMultiple(100);
            DiceRollResult doubleResult = results.FirstOrDefault(r => r.Die1 == r.Die2);
            DiceRollResult nonDoubleResult = results.FirstOrDefault(r => r.Die1 != r.Die2);

            // Assert
            if (doubleResult != null)
            {
                Assert.IsTrue(doubleResult.IsDouble, "Result with same dice should be double");
            }

            if (nonDoubleResult != null)
            {
                Assert.IsFalse(nonDoubleResult.IsDouble, "Result with different dice should not be double");
            }
        }

        [Test]
        public void DiceRoller_StatisticalTest_LargeSample_ApproximatesExpectedDistribution()
        {
            // Arrange
            int sampleSize = 10000;
            float tolerance = 0.1f; // 10% tolerance

            // Act
            List<DiceRollResult> results = _diceRoller.RollMultiple(sampleSize);
            DistributionAnalysis analysis = _diceRoller.GetDistributionAnalysis();

            // Assert - Check that distribution is within tolerance
            for (int total = 2; total <= 12; total++)
            {
                float actualProbability = (float)analysis.ActualDistribution[total] / sampleSize;
                float expectedProbability = _diceRoller.GetProbability(total);
                float deviation = Math.Abs(actualProbability - expectedProbability) / expectedProbability;

                Assert.Less(deviation, tolerance, 
                    $"Distribution for total {total} should be within {tolerance:P0} tolerance (actual: {actualProbability:P3}, expected: {expectedProbability:P3})");
            }
        }

        [Test]
        public void DiceRoller_Performance_ManyRolls_OperatesEfficiently()
        {
            // Arrange
            int rollCount = 10000;

            // Act
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            List<DiceRollResult> results = _diceRoller.RollMultiple(rollCount);
            
            stopwatch.Stop();

            // Assert
            Assert.AreEqual(rollCount, results.Count, "Should complete all rolls");
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "10000 rolls should complete quickly");
        }

        [Test]
        public void DiceRoller_Constructor_WithSeed_UsesDeterministicRandom()
        {
            // Arrange
            int seed = 999;

            // Act
            DiceRoller roller = new DiceRoller(seed: seed);

            // Assert
            Assert.IsNotNull(roller, "Should create roller with seed");
            Assert.AreEqual(0, roller.TotalRolls, "Should start with 0 rolls");
        }

        [Test]
        public void DistributionAnalysis_ToString_ReturnsFormattedString()
        {
            // Arrange
            for (int i = 0; i < 100; i++)
            {
                _diceRoller.Roll();
            }
            DistributionAnalysis analysis = _diceRoller.GetDistributionAnalysis();

            // Act
            string analysisString = analysis.ToString();

            // Assert
            Assert.IsTrue(analysisString.Contains("Distribution Analysis"), "Should contain analysis name");
            Assert.IsTrue(analysisString.Contains("100 rolls"), "Should contain total rolls");
            Assert.IsTrue(analysisString.Contains("Max Deviation"), "Should contain max deviation");
            Assert.IsTrue(analysisString.Contains("Avg Deviation"), "Should contain average deviation");
        }
    }
}
