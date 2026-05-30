using System;
using System.Collections.Generic;
using System.Linq;
using SuperHexLink.CoreLogic;
using SuperHexLink.Logging;

namespace SuperHexLink.Rules
{
    /// <summary>
    /// Implements semantic ontology tagging for AI rule generation.
    /// Based on NotebookLM research: MDA framework and Game Mechanics Ontology for thematic coherence.
    /// </summary>
    public class GameOntology
    {
        private readonly ActionLogSettings _logSettings;
        private readonly Dictionary<string, OntologyEntry> _ontologyEntries;
        private readonly Dictionary<string, List<string>> _semanticRelationships;

        public GameOntology(ActionLogSettings logSettings)
        {
            _logSettings = logSettings;
            _ontologyEntries = new Dictionary<string, OntologyEntry>();
            _semanticRelationships = new Dictionary<string, List<string>>();
            
            InitializeCoreOntology();
        }

        #region Core Ontology Initialization

        /// <summary>
        /// Initialize the core game ontology based on MDA framework and BoardGameGeek research.
        /// </summary>
        private void InitializeCoreOntology()
        {
            // MDA Categories
            RegisterMDACategories();
            
            // Game Mechanics Ontology
            RegisterGameMechanics();
            
            // Economic Roles
            RegisterEconomicRoles();
            
            // Semantic Relationships
            RegisterSemanticRelationships();

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Initialized core game ontology with {0} entries", _ontologyEntries.Count);
        }

        /// <summary>
        /// Register MDA (Mechanics, Dynamics, Aesthetics) categories.
        /// </summary>
        private void RegisterMDACategories()
        {
            // Mechanics - Rules and systems
            RegisterOntologyEntry("mechanic_resource_collection", new OntologyEntry
            {
                Name = "Resource Collection",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Algorithm,
                SpecificMechanics = new List<string> { "worker_placement", "set_collection", "dice_rolling" },
                EconomicRole = EconomicRole.Source,
                SemanticTags = new List<string> { "collection", "gathering", "resources", "economy" }
            });

            RegisterOntologyEntry("mechanic_building", new OntologyEntry
            {
                Name = "Building Construction",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Action,
                SpecificMechanics = new List<string> { "resource_management", "area_control", "grid_movement" },
                EconomicRole = EconomicRole.Sink,
                SemanticTags = new List<string> { "construction", "building", "development", "infrastructure" }
            });

            RegisterOntologyEntry("mechanic_trading", new OntologyEntry
            {
                Name = "Trading System",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Action,
                SpecificMechanics = new List<string> { "auction", "negotiation", "set_collection" },
                EconomicRole = EconomicRole.Trader,
                SemanticTags = new List<string> { "trade", "exchange", "commerce", "negotiation" }
            });

            // Dynamics - Runtime behaviors
            RegisterOntologyEntry("dynamic_economic_flow", new OntologyEntry
            {
                Name = "Economic Flow",
                MDACategory = MDACategory.Dynamic,
                PrimaryType = PrimaryType.DataRepresentation,
                SpecificMechanics = new List<string> { "resource_management", "economic" },
                EconomicRole = EconomicRole.Converter,
                SemanticTags = new List<string> { "economy", "flow", "resources", "balance" }
            });

            RegisterOntologyEntry("dynamic_territory_control", new OntologyEntry
            {
                Name = "Territory Control",
                MDACategory = MDACategory.Dynamic,
                PrimaryType = PrimaryType.DataRepresentation,
                SpecificMechanics = new List<string> { "area_control", "grid_movement" },
                EconomicRole = EconomicRole.Source,
                SemanticTags = new List<string> { "territory", "control", "expansion", "spatial" }
            });

            // Aesthetics - Emotional responses
            RegisterOntologyEntry("aesthetic_strategic_planning", new OntologyEntry
            {
                Name = "Strategic Planning",
                MDACategory = MDACategory.Aesthetic,
                PrimaryType = PrimaryType.Goal,
                SpecificMechanics = new List<string> { "resource_management", "worker_placement" },
                EconomicRole = EconomicRole.Converter,
                SemanticTags = new List<string> { "strategy", "planning", "foresight", "optimization" }
            });

            RegisterOntologyEntry("aesthetic_competition", new OntologyEntry
            {
                Name = "Competition",
                MDACategory = MDACategory.Aesthetic,
                PrimaryType = PrimaryType.Goal,
                SpecificMechanics = new List<string> { "area_control", "auction" },
                EconomicRole = EconomicRole.Sink,
                SemanticTags = new List<string> { "competition", "conflict", "rivalry", "winning" }
            });
        }

        /// <summary>
        /// Register specific game mechanics from BoardGameGeek research.
        /// </summary>
        private void RegisterGameMechanics()
        {
            // Core mechanics for hex-based resource games
            RegisterOntologyEntry("hex_grid_placement", new OntologyEntry
            {
                Name = "Hex Grid Placement",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Action,
                SpecificMechanics = new List<string> { "grid_movement", "area_control" },
                EconomicRole = EconomicRole.Sink,
                SemanticTags = new List<string> { "hex", "grid", "placement", "spatial" }
            });

            RegisterOntologyEntry("dice_production", new OntologyEntry
            {
                Name = "Dice-Based Production",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Algorithm,
                SpecificMechanics = new List<string> { "dice_rolling", "resource_management" },
                EconomicRole = EconomicRole.Source,
                SemanticTags = new List<string> { "dice", "production", "randomness", "resources" }
            });

            RegisterOntologyEntry("network_building", new OntologyEntry
            {
                Name = "Network Building",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Action,
                SpecificMechanics = new List<string> { "area_control", "network_building" },
                EconomicRole = EconomicRole.Converter,
                SemanticTags = new List<string> { "network", "connections", "infrastructure", "expansion" }
            });
        }

        /// <summary>
        /// Register economic roles for AI reasoning.
        /// </summary>
        private void RegisterEconomicRoles()
        {
            // Resource sources
            RegisterOntologyEntry("resource_wood", new OntologyEntry
            {
                Name = "Wood Resource",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Resource,
                SpecificMechanics = new List<string> { "resource_management", "set_collection" },
                EconomicRole = EconomicRole.Source,
                SemanticTags = new List<string> { "wood", "construction", "building", "material" }
            });

            RegisterOntologyEntry("resource_grain", new OntologyEntry
            {
                Name = "Grain Resource",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.Resource,
                SpecificMechanics = new List<string> { "resource_management", "set_collection" },
                EconomicRole = EconomicRole.Source,
                SemanticTags = new List<string> { "grain", "food", "sustenance", "agriculture" }
            });

            // Infrastructure (frozen commodities)
            RegisterOntologyEntry("building_settlement", new OntologyEntry
            {
                Name = "Settlement Building",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.DataRepresentation,
                SpecificMechanics = new List<string> { "area_control", "building" },
                EconomicRole = EconomicRole.Sink,
                SemanticTags = new List<string> { "settlement", "village", "community", "development" }
            });

            RegisterOntologyEntry("building_city", new OntologyEntry
            {
                Name = "City Building",
                MDACategory = MDACategory.Mechanic,
                PrimaryType = PrimaryType.DataRepresentation,
                SpecificMechanics = new List<string> { "area_control", "building" },
                EconomicRole = EconomicRole.Sink,
                SemanticTags = new List<string> { "city", "metropolis", "urban", "civilization" }
            });
        }

        /// <summary>
        /// Register semantic relationships between ontology entries.
        /// </summary>
        private void RegisterSemanticRelationships()
        {
            // Wood is used for building
            AddSemanticRelationship("resource_wood", "building_settlement", "used_for");
            AddSemanticRelationship("resource_wood", "building_city", "used_for");
            
            // Grain is used for sustenance/maintenance
            AddSemanticRelationship("resource_grain", "dynamic_economic_flow", "maintains");
            
            // Settlements are part of network building
            AddSemanticRelationship("building_settlement", "network_building", "part_of");
            AddSemanticRelationship("building_city", "network_building", "part_of");
            
            // Dice production drives economic flow
            AddSemanticRelationship("dice_production", "dynamic_economic_flow", "drives");
            
            // Hex grid placement enables building
            AddSemanticRelationship("hex_grid_placement", "building_settlement", "enables");
            AddSemanticRelationship("hex_grid_placement", "building_city", "enables");
            
            // Trading enables resource exchange
            AddSemanticRelationship("mechanic_trading", "resource_wood", "exchanges");
            AddSemanticRelationship("mechanic_trading", "resource_grain", "exchanges");
        }

        #endregion

        #region Ontology Management

        /// <summary>
        /// Register a new ontology entry.
        /// </summary>
        public void RegisterOntologyEntry(string entryId, OntologyEntry entry)
        {
            _ontologyEntries[entryId] = entry;
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Registered ontology entry: {0} ({1})", entryId, entry.Name);
        }

        /// <summary>
        /// Add semantic relationship between entries.
        /// </summary>
        public void AddSemanticRelationship(string fromEntry, string toEntry, string relationshipType)
        {
            if (!_semanticRelationships.ContainsKey(fromEntry))
            {
                _semanticRelationships[fromEntry] = new List<string>();
            }
            
            var relationship = $"{relationshipType}:{toEntry}";
            _semanticRelationships[fromEntry].Add(relationship);
            
            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Debug,
                "Added semantic relationship: {0} -> {1} ({2})", fromEntry, toEntry, relationshipType);
        }

        /// <summary>
        /// Get ontology entry by ID.
        /// </summary>
        public OntologyEntry GetOntologyEntry(string entryId)
        {
            return _ontologyEntries.GetValueOrDefault(entryId);
        }

        /// <summary>
        /// Get all ontology entries.
        /// </summary>
        public Dictionary<string, OntologyEntry> GetAllEntries()
        {
            return new Dictionary<string, OntologyEntry>(_ontologyEntries);
        }

        #endregion

        #region AI Rule Generation Support

        /// <summary>
        /// Get ontology entries that match criteria for AI rule generation.
        /// </summary>
        public List<OntologyEntry> FindMatchingEntries(MDACategory? mdaCategory = null, 
                                                       PrimaryType? primaryType = null,
                                                       EconomicRole? economicRole = null,
                                                       List<string> requiredTags = null)
        {
            var matchingEntries = _ontologyEntries.Values.ToList();

            if (mdaCategory.HasValue)
            {
                matchingEntries = matchingEntries.Where(e => e.MDACategory == mdaCategory.Value).ToList();
            }

            if (primaryType.HasValue)
            {
                matchingEntries = matchingEntries.Where(e => e.PrimaryType == primaryType.Value).ToList();
            }

            if (economicRole.HasValue)
            {
                matchingEntries = matchingEntries.Where(e => e.EconomicRole == economicRole.Value).ToList();
            }

            if (requiredTags != null && requiredTags.Any())
            {
                matchingEntries = matchingEntries.Where(e => 
                    requiredTags.Any(tag => e.SemanticTags.Contains(tag))).ToList();
            }

            return matchingEntries;
        }

        /// <summary>
        /// Validate AI-generated rule for thematic coherence.
        /// </summary>
        public RuleValidationResult ValidateRuleCoherence(AIGeneratedRule rule)
        {
            var result = new RuleValidationResult { IsValid = true };

            // Check if rule components are thematically coherent
            var sourceEntry = GetOntologyEntry(rule.SourceElementId);
            var targetEntry = GetOntologyEntry(rule.TargetElementId);
            var actionEntry = GetOntologyEntry(rule.ActionElementId);

            if (sourceEntry == null || targetEntry == null || actionEntry == null)
            {
                result.IsValid = false;
                result.Reasons.Add("One or more rule components not found in ontology");
                return result;
            }

            // Validate semantic relationships
            if (!HasValidSemanticRelationship(sourceEntry, targetEntry, rule.ActionType))
            {
                result.IsValid = false;
                result.Reasons.Add($"Invalid semantic relationship between {sourceEntry.Name} and {targetEntry.Name}");
            }

            // Validate economic role compatibility
            if (!AreEconomicRolesCompatible(sourceEntry.EconomicRole, targetEntry.EconomicRole, rule.ActionType))
            {
                result.IsValid = false;
                result.Reasons.Add($"Economic roles incompatible: {sourceEntry.EconomicRole} -> {targetEntry.EconomicRole}");
            }

            // Validate MDA category consistency
            if (!AreMDACategoriesConsistent(sourceEntry.MDACategory, targetEntry.MDACategory, actionEntry.MDACategory))
            {
                result.Warnings.Add("MDA categories may be inconsistent for this rule");
            }

            ActionLogger.Log(_logSettings, ActionLogCategory.Gameplay, ActionLogSeverity.Info,
                "Rule coherence validation: {0} - {1}", result.IsValid ? "Valid" : "Invalid", 
                string.Join("; ", result.Reasons.Concat(result.Warnings)));

            return result;
        }

        /// <summary>
        /// Check if there's a valid semantic relationship between entries.
        /// </summary>
        private bool HasValidSemanticRelationship(OntologyEntry source, OntologyEntry target, string actionType)
        {
            // Check direct relationships
            if (_semanticRelationships.ContainsKey(source.Name))
            {
                var relationships = _semanticRelationships[source.Name];
                var hasValidRelationship = relationships.Any(r => 
                    r.Contains(target.Name) && IsValidRelationshipForAction(r, actionType));
                
                if (hasValidRelationship) return true;
            }

            // Check inverse relationships
            if (_semanticRelationships.ContainsKey(target.Name))
            {
                var relationships = _semanticRelationships[target.Name];
                var hasValidRelationship = relationships.Any(r => 
                    r.Contains(source.Name) && IsValidRelationshipForAction(r, actionType));
                
                if (hasValidRelationship) return true;
            }

            // Default to allowing relationships between compatible economic roles
            return AreEconomicRolesCompatible(source.EconomicRole, target.EconomicRole, actionType);
        }

        /// <summary>
        /// Check if relationship is valid for the given action type.
        /// </summary>
        private bool IsValidRelationshipForAction(string relationship, string actionType)
        {
            if (relationship.StartsWith("used_for") && actionType == "convert")
                return true;
            if (relationship.StartsWith("drives") && actionType == "produce")
                return true;
            if (relationship.StartsWith("enables") && actionType == "build")
                return true;
            if (relationship.StartsWith("exchanges") && actionType == "trade")
                return true;
            
            return false;
        }

        /// <summary>
        /// Check if economic roles are compatible for the given action.
        /// </summary>
        private bool AreEconomicRolesCompatible(EconomicRole source, EconomicRole target, string actionType)
        {
            return actionType switch
            {
                "convert" => source == EconomicRole.Source && target == EconomicRole.Sink,
                "trade" => source == EconomicRole.Trader || target == EconomicRole.Trader,
                "produce" => source == EconomicRole.Source,
                "build" => target == EconomicRole.Sink,
                "maintain" => source == EconomicRole.Sink,
                _ => true  // Allow unknown actions by default
            };
        }

        /// <summary>
        /// Check if MDA categories are consistent.
        /// </summary>
        private bool AreMDACategoriesConsistent(MDACategory source, MDACategory target, MDACategory action)
        {
            // Actions should typically be mechanics
            if (action != MDACategory.Mechanic)
                return false;

            // Sources and targets can be any category, but some combinations make more sense
            return (source, target) switch
            {
                (MDACategory.Mechanic, MDACategory.Mechanic) => true,
                (MDACategory.Mechanic, MDACategory.Dynamic) => true,
                (MDACategory.Dynamic, MDACategory.Mechanic) => true,
                (MDACategory.Dynamic, MDACategory.Dynamic) => true,
                _ => false
            };
        }

        #endregion

        #region Ontology Analysis

        /// <summary>
        /// Get ontology statistics for analysis.
        /// </summary>
        public OntologyStatistics GetStatistics()
        {
            var stats = new OntologyStatistics
            {
                TotalEntries = _ontologyEntries.Count,
                EntriesByMDACategory = _ontologyEntries.Values
                    .GroupBy(e => e.MDACategory)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EntriesByPrimaryType = _ontologyEntries.Values
                    .GroupBy(e => e.PrimaryType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EntriesByEconomicRole = _ontologyEntries.Values
                    .GroupBy(e => e.EconomicRole)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TotalRelationships = _semanticRelationships.Values.Sum(list => list.Count)
            };

            return stats;
        }

        /// <summary>
        /// Get semantic neighbors for an entry.
        /// </summary>
        public List<OntologyEntry> GetSemanticNeighbors(string entryId)
        {
            var neighbors = new List<OntologyEntry>();
            
            if (_semanticRelationships.ContainsKey(entryId))
            {
                var relationships = _semanticRelationships[entryId];
                foreach (var relationship in relationships)
                {
                    var targetId = relationship.Split(':')[1];
                    var targetEntry = GetOntologyEntry(targetId);
                    if (targetEntry != null)
                    {
                        neighbors.Add(targetEntry);
                    }
                }
            }

            return neighbors;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Ontology entry defining a game element.
    /// </summary>
    public class OntologyEntry
    {
        public string Name { get; set; }
        public MDACategory MDACategory { get; set; }
        public PrimaryType PrimaryType { get; set; }
        public List<string> SpecificMechanics { get; set; } = new List<string>();
        public EconomicRole EconomicRole { get; set; }
        public List<string> SemanticTags { get; set; } = new List<string>();
    }

    /// <summary>
    /// AI-generated rule for validation.
    /// </summary>
    public class AIGeneratedRule
    {
        public string RuleId { get; set; }
        public string SourceElementId { get; set; }
        public string TargetElementId { get; set; }
        public string ActionElementId { get; set; }
        public string ActionType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result of rule validation.
    /// </summary>
    public class RuleValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Reasons { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public float ConfidenceScore { get; set; }
    }

    /// <summary>
    /// Ontology statistics.
    /// </summary>
    public class OntologyStatistics
    {
        public int TotalEntries { get; set; }
        public Dictionary<MDACategory, int> EntriesByMDACategory { get; set; } = new Dictionary<MDACategory, int>();
        public Dictionary<PrimaryType, int> EntriesByPrimaryType { get; set; } = new Dictionary<PrimaryType, int>();
        public Dictionary<EconomicRole, int> EntriesByEconomicRole { get; set; } = new Dictionary<EconomicRole, int>();
        public int TotalRelationships { get; set; }
    }

    #endregion

    #region Enums

    public enum MDACategory
    {
        Mechanic,    // Rules and systems
        Dynamic,     // Runtime behaviors  
        Aesthetic    // Emotional responses
    }

    public enum PrimaryType
    {
        Algorithm,           // Computational processes
        DataRepresentation,  // Data structures
        Action,              // Player actions
        Resource,            // Game resources
        Goal                 // Game objectives
    }

    public enum EconomicRole
    {
        Source,      // Generates resources
        Sink,        // Consumes resources
        Converter,   // Transforms resources
        Trader       // Exchanges resources
    }

    #endregion
}
