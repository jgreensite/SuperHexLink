using NUnit.Framework;
using System.Collections.Generic;
using SuperHexLink.Rules;
using SuperHexLink.Logging;

namespace SuperHexLink.Tests.Editor
{
    [TestFixture]
    public class GameOntologyTests
    {
        private GameOntology _ontology;
        private ActionLogSettings _logSettings;

        [SetUp]
        public void SetUp()
        {
            _logSettings = new ActionLogSettings
            {
                IsEnabled = (category, severity) => true,
                LogLevel = ActionLogSeverity.Info
            };
            _ontology = new GameOntology(_logSettings);
        }

        // ── Construction ──────────────────────────────────────────────────────────

        [Test]
        public void Constructor_InitializesCoreOntology_WithEntries()
        {
            var entries = _ontology.GetAllEntries();
            Assert.IsNotNull(entries);
            Assert.Greater(entries.Count, 0, "Core ontology should be pre-populated");
        }

        [Test]
        public void GetStatistics_ReflectsInitialCoreEntries()
        {
            var stats = _ontology.GetStatistics();
            Assert.IsNotNull(stats);
            Assert.Greater(stats.TotalEntries, 0);
        }

        // ── RegisterOntologyEntry / GetOntologyEntry ──────────────────────────────

        [Test]
        public void RegisterOntologyEntry_ThenGetOntologyEntry_ReturnsEntry()
        {
            var entry = new OntologyEntry
            {
                Name = "TestMechanic",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Algorithm,
                EconomicRole = EconomicRole.Source
            };

            _ontology.RegisterOntologyEntry("test_mechanic", entry);

            var retrieved = _ontology.GetOntologyEntry("test_mechanic");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("TestMechanic", retrieved.Name);
            Assert.AreEqual(MDACategory.Mechanic, retrieved.MDACategory);
        }

        [Test]
        public void GetOntologyEntry_ReturnsNull_ForUnknownId()
        {
            var result = _ontology.GetOntologyEntry("does_not_exist");
            Assert.IsNull(result);
        }

        [Test]
        public void RegisterOntologyEntry_IncreasesTotalEntryCount()
        {
            var initialCount = _ontology.GetAllEntries().Count;

            _ontology.RegisterOntologyEntry("new_entry", new OntologyEntry
            {
                Name = "NewEntry",
                MDACategory = MDACategory.Dynamic,
                PrimaryType = PrimaryType.Action
            });

            Assert.AreEqual(initialCount + 1, _ontology.GetAllEntries().Count);
        }

        // ── GetAllEntries ─────────────────────────────────────────────────────────

        [Test]
        public void GetAllEntries_ReturnsCopy_NotReference()
        {
            var first = _ontology.GetAllEntries();
            var second = _ontology.GetAllEntries();

            // Mutating the returned dictionary should not affect the ontology
            first["mutated_key"] = new OntologyEntry { Name = "mutated" };

            Assert.IsFalse(second.ContainsKey("mutated_key"));
        }

        // ── FindMatchingEntries ───────────────────────────────────────────────────

        [Test]
        public void FindMatchingEntries_ByMDACategory_FiltersCorrectly()
        {
            // Add an Aesthetic entry so we have at least one in that category
            _ontology.RegisterOntologyEntry("ae_test", new OntologyEntry
            {
                Name = "AestheticTest",
                MDACategory = MDACategory.Aesthetic,
                PrimaryType = PrimaryType.Goal
            });

            var aesthetics = _ontology.FindMatchingEntries(mdaCategory: MDACategory.Aesthetic);
            Assert.IsNotNull(aesthetics);
            Assert.GreaterOrEqual(aesthetics.Count, 1);
            foreach (var e in aesthetics)
            {
                Assert.AreEqual(MDACategory.Aesthetic, e.MDACategory);
            }
        }

        [Test]
        public void FindMatchingEntries_NoFilter_ReturnsAllEntries()
        {
            var all = _ontology.FindMatchingEntries();
            Assert.AreEqual(_ontology.GetAllEntries().Count, all.Count);
        }

        // ── Semantic Relationships ────────────────────────────────────────────────

        [Test]
        public void AddSemanticRelationship_ThenGetSemanticNeighbors_ReturnsNeighbor()
        {
            _ontology.RegisterOntologyEntry("node_a", new OntologyEntry { Name = "NodeA", MDACategory = MDACategory.Mechanic, PrimaryType = PrimaryType.Algorithm });
            _ontology.RegisterOntologyEntry("node_b", new OntologyEntry { Name = "NodeB", MDACategory = MDACategory.Mechanic, PrimaryType = PrimaryType.Resource });

            _ontology.AddSemanticRelationship("node_a", "node_b", "influences");

            var neighbors = _ontology.GetSemanticNeighbors("node_a");
            Assert.IsNotNull(neighbors);
            Assert.IsTrue(neighbors.Exists(e => e.Name == "NodeB"),
                "NodeB should appear as a semantic neighbor of NodeA");
        }

        [Test]
        public void GetSemanticNeighbors_ReturnsEmpty_ForEntryWithNoRelationships()
        {
            _ontology.RegisterOntologyEntry("lone_node", new OntologyEntry
            {
                Name = "LoneNode",
                MDACategory = MDACategory.Dynamic,
                PrimaryType = PrimaryType.DataRepresentation
            });

            var neighbors = _ontology.GetSemanticNeighbors("lone_node");
            Assert.IsNotNull(neighbors);
            Assert.AreEqual(0, neighbors.Count);
        }

        // ── ValidateRuleCoherence ─────────────────────────────────────────────────

        [Test]
        public void ValidateRuleCoherence_ReturnsValidationResult()
        {
            // Register the entries referenced by the rule
            _ontology.RegisterOntologyEntry("src_el", new OntologyEntry { Name = "Source", MDACategory = MDACategory.Mechanic, PrimaryType = PrimaryType.Action });
            _ontology.RegisterOntologyEntry("tgt_el", new OntologyEntry { Name = "Target", MDACategory = MDACategory.Mechanic, PrimaryType = PrimaryType.Resource });

            var rule = new AIGeneratedRule
            {
                RuleId = "coherence_rule",
                SourceElementId = "src_el",
                TargetElementId = "tgt_el",
                ActionType = "modify"
            };

            var result = _ontology.ValidateRuleCoherence(rule);
            Assert.IsNotNull(result);
        }
    }
}
