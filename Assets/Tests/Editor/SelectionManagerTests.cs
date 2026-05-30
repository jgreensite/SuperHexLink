using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SuperHexLink.Services;
using SuperHexLink.Logging;

namespace Tests.Editor
{
    /// <summary>
    /// Tests for SelectionManager service.
    /// Verifies hex type selection, placement validation, and rule management.
    /// </summary>
    public class SelectionManagerTests
    {
        private SelectionManager _selectionManager;
        private GameConstants _gameConstants;
        private GameObject _testHex;

        [SetUp]
        public void SetUp()
        {
            _gameConstants = new GameConstants();
            _selectionManager = new SelectionManager(_gameConstants, new ActionLogSettings());
            _testHex = new GameObject("TestHex");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testHex);
        }

        [Test]
        public void SelectionManager_SelectHexType_NullHex_ReturnsNullResult()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water" };

            // Act
            SelectionResult result = _selectionManager.SelectHexType(null, candidates);

            // Assert
            Assert.AreEqual(GameConstants.CAR_TYPE_WORD_NULL, result.SelectedType, "Should return null type for null hex");
            Assert.IsFalse(result.UsedCandidate, "Should not use candidate for null hex");
            Assert.IsFalse(result.UsedFallback, "Should not use fallback for null hex");
        }

        [Test]
        public void SelectionManager_SelectHexType_EmptyCandidates_ReturnsNullResult()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 0, Row = 0 };
            List<string> candidates = new List<string>();

            // Act
            SelectionResult result = _selectionManager.SelectHexType(hex, candidates);

            // Assert
            Assert.AreEqual(GameConstants.CAR_TYPE_WORD_NULL, result.SelectedType, "Should return null type for empty candidates");
            Assert.IsFalse(result.UsedCandidate, "Should not use candidate for empty candidates");
            Assert.IsFalse(result.UsedFallback, "Should not use fallback for empty candidates");
            Assert.AreEqual(0, result.Attempts, "Should have 0 attempts for empty candidates");
        }

        [Test]
        public void SelectionManager_SelectHexType_NullCandidates_ReturnsNullResult()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 0, Row = 0 };

            // Act
            SelectionResult result = _selectionManager.SelectHexType(hex, null);

            // Assert
            Assert.AreEqual(GameConstants.CAR_TYPE_WORD_NULL, result.SelectedType, "Should return null type for null candidates");
            Assert.IsFalse(result.UsedCandidate, "Should not use candidate for null candidates");
            Assert.IsFalse(result.UsedFallback, "Should not use fallback for null candidates");
        }

        [Test]
        public void SelectionManager_SelectHexType_ValidHexWithNoRules_ReturnsCandidate()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            List<string> candidates = new List<string> { "forest", "water", "mountain" };

            // Act
            SelectionResult result = _selectionManager.SelectHexType(hex, candidates);

            // Assert
            Assert.IsTrue(candidates.Contains(result.SelectedType), "Should return one of the candidate types");
            Assert.IsTrue(result.UsedCandidate, "Should use candidate when no rules exist");
            Assert.IsFalse(result.UsedFallback, "Should not use fallback when no rules exist");
            Assert.Greater(result.Attempts, 0, "Should have at least 1 attempt");
        }

        [Test]
        public void SelectionManager_SelectHexType_DeterministicSeed_ReturnsSameResult()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            List<string> candidates = new List<string> { "forest", "water", "mountain" };

            _selectionManager.SetRandomSeed(42);

            // Act
            SelectionResult result1 = _selectionManager.SelectHexType(hex, candidates);
            _selectionManager.SetRandomSeed(42);
            SelectionResult result2 = _selectionManager.SelectHexType(hex, candidates);

            // Assert
            Assert.AreEqual(result1.SelectedType, result2.SelectedType, "Should return same result with same seed");
        }

        [Test]
        public void SelectionManager_GetSelectionStatistics_EmptySelection_ReturnsEmptyStats()
        {
            // Act
            IReadOnlyDictionary<string, int> stats = _selectionManager.GetSelectionStatistics();

            // Assert
            Assert.AreEqual(0, stats.Count, "Should have empty statistics for no selection");
        }

        [Test]
        public void SelectionManager_ClearStatistics_AfterSelection_ClearsStats()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            List<string> candidates = new List<string> { "forest", "water" };
            _selectionManager.SelectHexType(hex, candidates);

            // Act
            _selectionManager.ClearStatistics();
            IReadOnlyDictionary<string, int> stats = _selectionManager.GetSelectionStatistics();

            // Assert
            Assert.AreEqual(0, stats.Count, "Should have empty statistics after clearing");
        }

        [Test]
        public void SelectionManager_ValidatePlacement_NullHexType_ReturnsFalse()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 0, Row = 0 };

            // Act
            bool result = _selectionManager.ValidatePlacement(null, hex);

            // Assert
            Assert.IsFalse(result, "Should return false for null hex type");
        }

        [Test]
        public void SelectionManager_ValidatePlacement_NullHex_ReturnsFalse()
        {
            // Act
            bool result = _selectionManager.ValidatePlacement("forest", null);

            // Assert
            Assert.IsFalse(result, "Should return false for null hex");
        }

        [Test]
        public void SelectionManager_ValidatePlacement_EmptyHexType_ReturnsFalse()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 0, Row = 0 };

            // Act
            bool result = _selectionManager.ValidatePlacement("", hex);

            // Assert
            Assert.IsFalse(result, "Should return false for empty hex type");
        }

        [Test]
        public void SelectionManager_GetValidCandidates_ValidInputs_ReturnsFilteredCandidates()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            List<string> candidates = new List<string> { "forest", "water", "mountain" };

            // Act
            List<string> validCandidates = _selectionManager.GetValidCandidates(hex, candidates);

            // Assert
            Assert.IsNotNull(validCandidates, "Should return non-null list");
            // With no rules, all candidates should be valid
            Assert.AreEqual(candidates.Count, validCandidates.Count, "All candidates should be valid with no rules");
        }

        [Test]
        public void SelectionManager_GetValidCandidates_NullHex_ReturnsEmptyList()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water" };

            // Act
            List<string> validCandidates = _selectionManager.GetValidCandidates(null, candidates);

            // Assert
            Assert.AreEqual(0, validCandidates.Count, "Should return empty list for null hex");
        }

        [Test]
        public void SelectionManager_GetValidCandidates_NullCandidates_ReturnsEmptyList()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };

            // Act
            List<string> validCandidates = _selectionManager.GetValidCandidates(hex, null);

            // Assert
            Assert.AreEqual(0, validCandidates.Count, "Should return empty list for null candidates");
        }

        [Test]
        public void SelectionManager_GetPlacementRule_ValidHexType_ReturnsRule()
        {
            // This test would require actual GameConstants with placement rules
            // For now, it's a placeholder that demonstrates the expected behavior

            // Act
            HexPlacementRuleConfig rule = _selectionManager.GetPlacementRule("forest");

            // Assert
            // Rule would be null if no rule is configured in GameConstants
            // This test would need to be updated when actual rules are implemented
        }

        [Test]
        public void SelectionManager_GetRandomCandidate_ValidList_ReturnsCandidate()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water", "mountain" };

            // Act
            string candidate = _selectionManager.GetRandomCandidate(candidates);

            // Assert
            Assert.IsNotNull(candidate, "Should return a candidate");
            Assert.IsTrue(candidates.Contains(candidate), "Should return one of the provided candidates");
        }

        [Test]
        public void SelectionManager_GetRandomCandidate_EmptyList_ReturnsNull()
        {
            // Arrange
            List<string> candidates = new List<string>();

            // Act
            string candidate = _selectionManager.GetRandomCandidate(candidates);

            // Assert
            Assert.IsNull(candidate, "Should return null for empty list");
        }

        [Test]
        public void SelectionManager_GetRandomCandidate_NullList_ReturnsNull()
        {
            // Act
            string candidate = _selectionManager.GetRandomCandidate(null);

            // Assert
            Assert.IsNull(candidate, "Should return null for null list");
        }

        [Test]
        public void SelectionManager_FilterCandidates_ValidInputs_ReturnsFilteredList()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water", "mountain", "desert" };
            List<string> excludeTypes = new List<string> { "water", "desert" };

            // Act
            List<string> filtered = _selectionManager.FilterCandidates(candidates, excludeTypes);

            // Assert
            Assert.AreEqual(2, filtered.Count, "Should filter out 2 types");
            Assert.IsTrue(filtered.Contains("forest"), "Should contain forest");
            Assert.IsTrue(filtered.Contains("mountain"), "Should contain mountain");
            Assert.IsFalse(filtered.Contains("water"), "Should not contain water");
            Assert.IsFalse(filtered.Contains("desert"), "Should not contain desert");
        }

        [Test]
        public void SelectionManager_FilterCandidates_EmptyExcludeList_ReturnsOriginalList()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water", "mountain" };
            List<string> excludeTypes = new List<string>();

            // Act
            List<string> filtered = _selectionManager.FilterCandidates(candidates, excludeTypes);

            // Assert
            Assert.AreEqual(candidates.Count, filtered.Count, "Should return all candidates when no exclusions");
            foreach (string candidate in candidates)
            {
                Assert.IsTrue(filtered.Contains(candidate), $"Should contain {candidate}");
            }
        }

        [Test]
        public void SelectionManager_FilterCandidates_NullExcludeList_ReturnsOriginalList()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water", "mountain" };

            // Act
            List<string> filtered = _selectionManager.FilterCandidates(candidates, null);

            // Assert
            Assert.AreEqual(candidates.Count, filtered.Count, "Should return all candidates when exclude list is null");
            foreach (string candidate in candidates)
            {
                Assert.IsTrue(filtered.Contains(candidate), $"Should contain {candidate}");
            }
        }

        [Test]
        public void SelectionManager_FilterCandidates_NullCandidates_ReturnsEmptyList()
        {
            // Arrange
            List<string> excludeTypes = new List<string> { "water" };

            // Act
            List<string> filtered = _selectionManager.FilterCandidates(null, excludeTypes);

            // Assert
            Assert.AreEqual(0, filtered.Count, "Should return empty list for null candidates");
        }

        [Test]
        public void SelectionManager_SetRandomSeed_ValidSeed_UsesDeterministicRandom()
        {
            // Arrange
            List<string> candidates = new List<string> { "forest", "water", "mountain" };
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };

            // Act
            _selectionManager.SetRandomSeed(123);
            SelectionResult result1 = _selectionManager.SelectHexType(hex, candidates);
            
            _selectionManager.SetRandomSeed(123);
            SelectionResult result2 = _selectionManager.SelectHexType(hex, candidates);

            // Assert
            Assert.AreEqual(result1.SelectedType, result2.SelectedType, "Should produce same result with same seed");
        }

        [Test]
        public void SelectionResult_ToString_ReturnsFormattedString()
        {
            // Arrange
            var rule = new HexPlacementRuleConfig { ruleName = "TestRule" };
            SelectionResult result = new SelectionResult("forest", rule, true, false, 3);

            // Act
            string resultString = result.ToString();

            // Assert
            Assert.IsTrue(resultString.Contains("forest"), "Should contain selected type");
            Assert.IsTrue(resultString.Contains("TestRule"), "Should contain rule name");
            Assert.IsTrue(resultString.Contains("True"), "Should contain used candidate flag");
            Assert.IsTrue(resultString.Contains("False"), "Should contain used fallback flag");
            Assert.IsTrue(resultString.Contains("3"), "Should contain attempts count");
        }

        [Test]
        public void SelectionManager_Constructor_NullGameConstants_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => 
                new SelectionManager(null, new ActionLogSettings()), 
                "Should throw ArgumentNullException for null GameConstants");
        }

        [Test]
        public void SelectionManager_Performance_MultipleSelections_OperatesEfficiently()
        {
            // Arrange
            Hex hex = _testHex.AddComponent<Hex>();
            hex.hexState = new Hex.HexState { Col = 1, Row = 2 };
            List<string> candidates = new List<string> { "forest", "water", "mountain", "desert", "plains" };

            // Act
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                SelectionResult result = _selectionManager.SelectHexType(hex, candidates);
                Assert.IsNotNull(result, $"Selection {i} should return a result");
            }
            
            stopwatch.Stop();

            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "100 selections should complete quickly");
        }
    }
}
