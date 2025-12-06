#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SuperHexLink.Validation;

/// <summary>
/// Editor window for editing and validating map state.
/// </summary>
public class MapEditorWindow : EditorWindow
{
    private MapValidationResult _validationResult;
    private Vector2 _scrollPosition;
    private Vector2 _detailScrollPosition;
    
    // Filtering
    private bool _showErrors = true;
    private bool _showWarnings = true;
    private bool _showInfo = false;
    private bool _showAutoFixableOnly = false;
    private bool _groupByFixType = true;
    private ValidationCategory? _filterCategory = null;
    
    // Group fold states
    private bool _foldAutoFix = true;
    private bool _foldSaveReload = true;
    private bool _foldManualEdit = true;
    private bool _foldNoFix = true;
    
    // Selection
    private ValidationIssue _selectedIssue;
    private Hex _selectedHex;
    private Hex _hoveredHex;
    
    // Selection mode
    private bool _hexSelectionMode = false;
    
    // Hex editing
    private string _newHexType;
    private int _newRotation;
    private int? _newHexNum;
    private string _newGroupID;
    
    // References
    private GameSpawner _gameSpawner;
    private HexSpawner _hexSpawner;
    private GameConstants _gameConstants;

    // Land type options for dropdown
    private static readonly string[] LandTypeOptions = new[]
    {
        GameConstants.CAR_TYPE_FOREST,
        GameConstants.CAR_TYPE_PASTURE,
        GameConstants.CAR_TYPE_FIELD,
        GameConstants.CAR_TYPE_HILL,
        GameConstants.CAR_TYPE_MOUNTAIN,
        GameConstants.CAR_TYPE_MINE,
        GameConstants.CAR_TYPE_GOLD,
        GameConstants.CAR_TYPE_SEA,
        GameConstants.CAR_TYPE_HARBOUR,
        GameConstants.CAR_TYPE_DESERT,
        GameConstants.CAR_TYPE_NONE
    };

    [MenuItem("SuperHexLink/Map Editor Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapEditorWindow>("Map Editor");
        window.minSize = new Vector2(550, 550);
        window.Show();
    }

    private void OnEnable()
    {
        FindReferences();
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChanged;
        ClearAllHighlights();
    }

    private void FindReferences()
    {
        _gameSpawner = FindObjectOfType<GameSpawner>();
        _hexSpawner = FindObjectOfType<HexSpawner>();
        _gameConstants = FindObjectOfType<GameConstants>();
        
        if (_gameConstants == null)
        {
            // Try to find GameConstants asset
            var guids = AssetDatabase.FindAssets("t:GameConstants");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _gameConstants = AssetDatabase.LoadAssetAtPath<GameConstants>(path);
            }
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        
        EditorGUILayout.Space(5);
        
        // Summary and quick actions bar
        DrawSummaryBar();
        
        EditorGUILayout.Space(5);
        
        using (new EditorGUILayout.HorizontalScope())
        {
            // Left panel - Issue list
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.55f)))
            {
                DrawIssueList();
            }
            
            // Separator
            EditorGUILayout.Space(5);
            
            // Right panel - Details and editing
            using (new EditorGUILayout.VerticalScope())
            {
                DrawDetailsPanel();
            }
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Validate Map", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RunValidation();
            }
            
            GUILayout.Space(10);
            
            _showErrors = GUILayout.Toggle(_showErrors, $"Errors", EditorStyles.toolbarButton, GUILayout.Width(60));
            _showWarnings = GUILayout.Toggle(_showWarnings, $"Warnings", EditorStyles.toolbarButton, GUILayout.Width(70));
            _showInfo = GUILayout.Toggle(_showInfo, $"Info", EditorStyles.toolbarButton, GUILayout.Width(50));
            
            GUILayout.Space(5);
            _groupByFixType = GUILayout.Toggle(_groupByFixType, "Group", EditorStyles.toolbarButton, GUILayout.Width(50));
            
            GUILayout.Space(10);
            
            // Hex Selection Mode toggle
            var oldColor = GUI.backgroundColor;
            if (_hexSelectionMode)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            }
            EditorGUI.BeginChangeCheck();
            _hexSelectionMode = GUILayout.Toggle(_hexSelectionMode, "🎯 Pick Hex", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (EditorGUI.EndChangeCheck())
            {
                if (!_hexSelectionMode)
                {
                    ClearHoverHighlight();
                }
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = oldColor;
            
            GUILayout.FlexibleSpace();
            
            // Category filter dropdown
            EditorGUI.BeginChangeCheck();
            var categoryOptions = new[] { "All Categories" }.Concat(Enum.GetNames(typeof(ValidationCategory))).ToArray();
            int selectedIndex = _filterCategory.HasValue ? Array.IndexOf(categoryOptions, _filterCategory.Value.ToString()) : 0;
            selectedIndex = EditorGUILayout.Popup(selectedIndex, categoryOptions, EditorStyles.toolbarPopup, GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck())
            {
                _filterCategory = selectedIndex == 0 ? null : (ValidationCategory?)Enum.Parse(typeof(ValidationCategory), categoryOptions[selectedIndex]);
            }
        }
        
        // Summary bar
        if (_validationResult != null)
        {
            var summaryStyle = _validationResult.IsValid ? EditorStyles.helpBox : EditorStyles.helpBox;
            var bgColor = _validationResult.ErrorCount > 0 ? new Color(1, 0.3f, 0.3f, 0.3f) :
                          _validationResult.WarningCount > 0 ? new Color(1, 0.8f, 0.3f, 0.3f) :
                          new Color(0.3f, 1, 0.3f, 0.3f);
            
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.HelpBox(_validationResult.GetSummary(), MessageType.None);
            GUI.backgroundColor = oldBg;
        }
    }

    private void DrawSummaryBar()
    {
        if (_validationResult == null || !_validationResult.HasIssues) return;
        
        // Count fixable issues
        int autoFixCount = _validationResult.Issues.Count(i => i.CanAutoFix);
        int saveReloadFixCount = _validationResult.Issues.Count(i => i.FixedBySaveReload);
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                // Auto-fix button
                GUI.enabled = autoFixCount > 0;
                if (GUILayout.Button($"🔧 Apply {autoFixCount} Auto-Fixes", GUILayout.Height(25)))
                {
                    ApplyAllAutoFixes();
                }
                GUI.enabled = true;
                
                // Save & Reload info
                if (saveReloadFixCount > 0)
                {
                    var oldColor = GUI.color;
                    GUI.color = new Color(0.8f, 0.9f, 1f);
                    EditorGUILayout.HelpBox($"💾 {saveReloadFixCount} issue(s) will be fixed by Save & Reload", MessageType.None);
                    GUI.color = oldColor;
                }
            }
            
            // Save & Reload buttons
            if (saveReloadFixCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("💾 Save Map", GUILayout.Height(25)))
                    {
                        if (_gameSpawner != null)
                        {
                            _gameSpawner.SaveToConfiguredPath();
                            Debug.Log("Map saved. Reload to apply default fixes.");
                        }
                    }
                    
                    if (GUILayout.Button("🔄 Reload Map", GUILayout.Height(25)))
                    {
                        if (_gameSpawner != null)
                        {
                            _gameSpawner.LoadFromConfiguredPath();
                            RunValidation();
                        }
                    }
                    
                    if (GUILayout.Button("💾🔄 Save & Reload", GUILayout.Height(25)))
                    {
                        if (_gameSpawner != null)
                        {
                            _gameSpawner.SaveToConfiguredPath();
                            _gameSpawner.LoadFromConfiguredPath();
                            RunValidation();
                            Debug.Log("Map saved and reloaded - default fixes applied.");
                        }
                    }
                }
            }
        }
    }

    private void ApplyAllAutoFixes()
    {
        if (_validationResult == null || _hexSpawner == null) return;
        
        var autoFixIssues = _validationResult.Issues.Where(i => i.CanAutoFix).ToList();
        int fixedCount = 0;
        
        foreach (var issue in autoFixIssues)
        {
            if (ApplyFix(issue))
            {
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"Applied {fixedCount} auto-fixes. Re-validating...");
            RunValidation();
        }
    }

    private bool ApplyFix(ValidationIssue issue)
    {
        if (!issue.CanAutoFix || _hexSpawner?.State?.hexes == null) return false;
        
        // Get the hex state from the master array
        if (issue.Col < 0 || issue.Col >= _hexSpawner.State.hexes.Count) return false;
        var column = _hexSpawner.State.hexes[issue.Col];
        if (issue.Row < 0 || issue.Row >= column.Count) return false;
        
        var hexState = column[issue.Row];
        if (hexState == null) return false;
        
        bool applied = false;
        
        switch (issue.FieldName)
        {
            case "GroupID":
                hexState.GroupID = issue.FixValue ?? "1";
                applied = true;
                break;
                
            case "Rotation":
                if (int.TryParse(issue.FixValue, out int rotation))
                {
                    hexState.Rotation = rotation;
                    applied = true;
                }
                break;
                
            case "HexNum":
                if (string.IsNullOrEmpty(issue.FixValue))
                {
                    hexState.HexNum = null;
                    applied = true;
                }
                else if (int.TryParse(issue.FixValue, out int num))
                {
                    hexState.HexNum = num;
                    applied = true;
                }
                break;
                
            case "Col":
                if (int.TryParse(issue.FixValue, out int col))
                {
                    hexState.Col = col;
                    applied = true;
                }
                break;
                
            case "Row":
                if (int.TryParse(issue.FixValue, out int row))
                {
                    hexState.Row = row;
                    applied = true;
                }
                break;
        }
        
        if (applied)
        {
            // Find and refresh the visual hex
            RefreshHexVisual(issue.Col, issue.Row);
            
            // Clear highlight if this was the selected/hovered hex
            ClearHighlightIfMatches(issue.Col, issue.Row);
            
            // Remove issue from result if fixed
            _validationResult?.Issues.Remove(issue);
            
            // Clear selection if this was the selected issue
            if (_selectedIssue == issue)
            {
                _selectedIssue = null;
            }
        }
        
        return applied;
    }

    private void ClearHighlightIfMatches(int col, int row)
    {
        // Clear hover highlight if it matches
        if (_hoveredHex?.hexState != null && 
            _hoveredHex.hexState.Col == col && _hoveredHex.hexState.Row == row)
        {
            ClearHoverHighlight();
        }
        
        // Clear selection highlight if it matches
        if (_selectedHex?.hexState != null && 
            _selectedHex.hexState.Col == col && _selectedHex.hexState.Row == row)
        {
            ClearHexHighlight();
            _selectedHex = null;
        }
    }
    
    private void ClearAllHighlights()
    {
        ClearHoverHighlight();
        ClearHexHighlight();
        _selectedHex = null;
        _hoveredHex = null;
    }

    private void RefreshHexVisual(int col, int row)
    {
        if (_hexSpawner == null) return;
        
        var hexes = _hexSpawner.GetComponentsInChildren<Hex>();
        var hex = hexes.FirstOrDefault(h => h.hexState?.Col == col && h.hexState?.Row == row);
        
        if (hex != null)
        {
            // Sync the visual hex's state from master array
            if (_hexSpawner.State?.hexes != null && 
                col < _hexSpawner.State.hexes.Count && 
                row < _hexSpawner.State.hexes[col].Count)
            {
                var masterState = _hexSpawner.State.hexes[col][row];
                hex.hexState.GroupID = masterState.GroupID;
                hex.hexState.Rotation = masterState.Rotation;
                hex.hexState.HexNum = masterState.HexNum;
                hex.hexState.HexType = masterState.HexType;
                hex.hexState.Col = masterState.Col;
                hex.hexState.Row = masterState.Row;
            }
            
            _hexSpawner.RefreshHex(hex);
            EditorUtility.SetDirty(hex);
        }
    }

    private void DrawIssueList()
    {
        EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
        
        if (_validationResult == null)
        {
            EditorGUILayout.HelpBox("Click 'Validate Map' to check for issues.", MessageType.Info);
            return;
        }

        var filteredIssues = GetFilteredIssues();
        
        if (filteredIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues match the current filter.", MessageType.Info);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        
        if (_groupByFixType)
        {
            DrawGroupedIssues(filteredIssues);
        }
        else
        {
            foreach (var issue in filteredIssues)
            {
                DrawIssueRow(issue);
            }
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawGroupedIssues(List<ValidationIssue> issues)
    {
        // Group by fix type
        var autoFixIssues = issues.Where(i => i.FixType == FixType.AutoFix).ToList();
        var saveReloadIssues = issues.Where(i => i.FixType == FixType.SaveAndReload).ToList();
        var manualEditIssues = issues.Where(i => i.FixType == FixType.ManualEdit).ToList();
        var noFixIssues = issues.Where(i => i.FixType == FixType.None).ToList();
        
        // Further group auto-fix issues by field name for batch operations
        var autoFixByField = autoFixIssues.GroupBy(i => i.FieldName ?? "Unknown").ToList();
        
        // Auto-Fix Group
        if (autoFixIssues.Count > 0)
        {
            DrawIssueGroup(
                ref _foldAutoFix, 
                $"🔧 Auto-Fixable ({autoFixIssues.Count})", 
                autoFixIssues, 
                new Color(0.3f, 0.8f, 0.3f, 0.3f),
                () => ApplyFixesToGroup(autoFixIssues),
                autoFixByField
            );
        }
        
        // Save & Reload Group
        if (saveReloadIssues.Count > 0)
        {
            DrawIssueGroup(
                ref _foldSaveReload, 
                $"💾 Fixed by Save & Reload ({saveReloadIssues.Count})", 
                saveReloadIssues, 
                new Color(0.3f, 0.5f, 0.9f, 0.3f),
                () => {
                    if (_gameSpawner != null)
                    {
                        _gameSpawner.SaveToConfiguredPath();
                        _gameSpawner.LoadFromConfiguredPath();
                        RunValidation();
                    }
                }
            );
        }
        
        // Manual Edit Group
        if (manualEditIssues.Count > 0)
        {
            DrawIssueGroup(
                ref _foldManualEdit, 
                $"✏️ Manual Edit Required ({manualEditIssues.Count})", 
                manualEditIssues, 
                new Color(0.9f, 0.7f, 0.3f, 0.3f)
            );
        }
        
        // No Fix Group
        if (noFixIssues.Count > 0)
        {
            DrawIssueGroup(
                ref _foldNoFix, 
                $"ℹ️ Informational ({noFixIssues.Count})", 
                noFixIssues, 
                new Color(0.5f, 0.5f, 0.5f, 0.3f)
            );
        }
    }

    private void DrawIssueGroup(
        ref bool foldout, 
        string title, 
        List<ValidationIssue> issues, 
        Color bgColor,
        Action fixAllAction = null,
        List<IGrouping<string, ValidationIssue>> subGroups = null)
    {
        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;
        
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = oldBg;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
                
                GUILayout.FlexibleSpace();
                
                if (fixAllAction != null)
                {
                    if (GUILayout.Button($"Fix All ({issues.Count})", EditorStyles.miniButton, GUILayout.Width(80)))
                    {
                        fixAllAction();
                    }
                }
            }
            
            if (foldout)
            {
                // Draw sub-groups if provided (for auto-fix by field type)
                if (subGroups != null && subGroups.Count > 1)
                {
                    foreach (var subGroup in subGroups)
                    {
                        DrawSubGroup(subGroup.Key, subGroup.ToList());
                    }
                }
                else
                {
                    // Draw issues directly
                    foreach (var issue in issues)
                    {
                        DrawIssueRow(issue);
                    }
                }
            }
        }
        
        EditorGUILayout.Space(2);
    }

    private void DrawSubGroup(string fieldName, List<ValidationIssue> issues)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(15);
            
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"  {GetFieldDisplayName(fieldName)} ({issues.Count})", EditorStyles.boldLabel);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button($"Fix All", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        ApplyFixesToGroup(issues);
                    }
                }
                
                foreach (var issue in issues)
                {
                    DrawIssueRow(issue);
                }
            }
        }
    }

    private string GetFieldDisplayName(string fieldName)
    {
        return fieldName switch
        {
            "GroupID" => "Empty GroupID → '1'",
            "Rotation" => "Invalid Rotation",
            "HexNum" => "Number Token Issues",
            "HexType" => "Land Type Issues",
            _ => fieldName
        };
    }

    private void ApplyFixesToGroup(List<ValidationIssue> issues)
    {
        if (_hexSpawner == null) return;
        
        int fixedCount = 0;
        foreach (var issue in issues.Where(i => i.CanAutoFix))
        {
            if (ApplyFix(issue))
            {
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"Applied {fixedCount} fixes. Re-validating...");
            RunValidation();
        }
    }

    private void DrawIssueRow(ValidationIssue issue)
    {
        var isSelected = _selectedIssue == issue;
        var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.5f) : Color.clear;
        
        var iconColor = issue.Severity switch
        {
            ValidationSeverity.Error => new Color(1, 0.3f, 0.3f),
            ValidationSeverity.Warning => new Color(1, 0.7f, 0.2f),
            ValidationSeverity.Info => new Color(0.5f, 0.7f, 1f),
            _ => Color.white
        };

        var oldBg = GUI.backgroundColor;
        GUI.backgroundColor = bgColor;
        
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUI.backgroundColor = oldBg;
            
            // Severity icon
            var oldColor = GUI.color;
            GUI.color = iconColor;
            GUILayout.Label(GetSeverityIcon(issue.Severity), GUILayout.Width(20));
            GUI.color = oldColor;
            
            // Fix type indicator
            string fixIndicator = issue.FixType switch
            {
                FixType.AutoFix => "🔧",
                FixType.SaveAndReload => "💾",
                FixType.ManualEdit => "✏️",
                _ => ""
            };
            GUILayout.Label(fixIndicator, GUILayout.Width(20));
            
            // Location
            if (issue.HasHexReference)
            {
                if (GUILayout.Button(issue.LocationString, EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    SelectIssue(issue);
                    SelectHexInScene(issue.Col, issue.Row);
                }
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(50));
            }
            
            // Message (shortened to make room for fix button)
            GUILayout.Label(issue.Message, EditorStyles.label, GUILayout.ExpandWidth(true));
            
            // Quick fix button for auto-fixable issues
            if (issue.CanAutoFix)
            {
                if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    if (ApplyFix(issue))
                    {
                        Debug.Log($"Applied fix to [{issue.Col},{issue.Row}]: {issue.SuggestedFix}");
                        RunValidation();
                    }
                }
            }
            
            // Select button
            if (GUILayout.Button("→", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                SelectIssue(issue);
            }
        }
        
        GUI.backgroundColor = oldBg;
    }

    private string GetSeverityIcon(ValidationSeverity severity)
    {
        return severity switch
        {
            ValidationSeverity.Error => "✖",
            ValidationSeverity.Warning => "⚠",
            ValidationSeverity.Info => "ℹ",
            _ => "•"
        };
    }

    private void DrawDetailsPanel()
    {
        EditorGUILayout.LabelField("Details", EditorStyles.boldLabel);
        
        _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);
        
        if (_selectedIssue == null)
        {
            EditorGUILayout.HelpBox("Select an issue to view details.", MessageType.Info);
        }
        else
        {
            DrawIssueDetails();
        }
        
        EditorGUILayout.Space(10);
        
        if (_selectedHex != null)
        {
            DrawHexEditor();
        }
        
        EditorGUILayout.EndScrollView();
    }

    private void DrawIssueDetails()
    {
        EditorGUILayout.LabelField("Severity:", _selectedIssue.Severity.ToString());
        EditorGUILayout.LabelField("Category:", _selectedIssue.Category.ToString());
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Message:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(_selectedIssue.Message, MessageType.None);
        
        if (!string.IsNullOrEmpty(_selectedIssue.Details))
        {
            EditorGUILayout.LabelField("Details:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_selectedIssue.Details, MessageType.None);
        }
        
        if (_selectedIssue.HasHexReference)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Location: Column {_selectedIssue.Col}, Row {_selectedIssue.Row}");
            
            if (GUILayout.Button("Select Hex in Scene"))
            {
                SelectHexInScene(_selectedIssue.Col, _selectedIssue.Row);
            }
        }
        
        if (!string.IsNullOrEmpty(_selectedIssue.SuggestedFix))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Suggested Fix:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(_selectedIssue.SuggestedFix, MessageType.None);
        }
    }

    private void DrawHexEditor()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Hex Editor", EditorStyles.boldLabel);
        
        var hexState = _selectedHex.hexState;
        if (hexState == null)
        {
            EditorGUILayout.HelpBox("Selected hex has no state.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.LabelField($"Hex [{hexState.Col}, {hexState.Row}]", EditorStyles.boldLabel);
        
        // Current values
        EditorGUILayout.LabelField("Current Type:", hexState.HexType ?? "(null)");
        EditorGUILayout.LabelField("Current Rotation:", hexState.Rotation.ToString());
        EditorGUILayout.LabelField("Current Number:", hexState.HexNum?.ToString() ?? "(none)");
        EditorGUILayout.LabelField("Current GroupID:", hexState.GroupID ?? "(null)");
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Modify Hex", EditorStyles.boldLabel);
        
        // Type dropdown
        int currentTypeIndex = Array.IndexOf(LandTypeOptions, hexState.HexType);
        if (currentTypeIndex < 0) currentTypeIndex = 0;
        
        EditorGUI.BeginChangeCheck();
        int newTypeIndex = EditorGUILayout.Popup("Land Type", currentTypeIndex, LandTypeOptions);
        if (EditorGUI.EndChangeCheck())
        {
            _newHexType = LandTypeOptions[newTypeIndex];
        }
        else if (string.IsNullOrEmpty(_newHexType))
        {
            _newHexType = hexState.HexType;
        }
        
        // Rotation slider (multiples of 60)
        _newRotation = EditorGUILayout.IntSlider("Rotation", _newRotation, 0, 300);
        _newRotation = Mathf.RoundToInt(_newRotation / 60f) * 60; // Snap to 60 degree increments
        
        // Rotation quick buttons
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Quick Rotate:", GUILayout.Width(80));
            if (GUILayout.Button("↺ -60°", GUILayout.Width(60)))
            {
                _newRotation = ((_newRotation - 60) + 360) % 360;
            }
            if (GUILayout.Button("↻ +60°", GUILayout.Width(60)))
            {
                _newRotation = (_newRotation + 60) % 360;
            }
        }
        
        // Number
        bool hasNumber = _newHexNum.HasValue;
        EditorGUI.BeginChangeCheck();
        hasNumber = EditorGUILayout.Toggle("Has Number", hasNumber);
        if (EditorGUI.EndChangeCheck())
        {
            _newHexNum = hasNumber ? (hexState.HexNum ?? 2) : null;
        }
        
        if (_newHexNum.HasValue)
        {
            var numbers = new[] { 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 };
            var numberStrings = numbers.Select(n => n.ToString()).ToArray();
            int numIndex = Array.IndexOf(numbers, _newHexNum.Value);
            if (numIndex < 0) numIndex = 0;
            numIndex = EditorGUILayout.Popup("Number", numIndex, numberStrings);
            _newHexNum = numbers[numIndex];
        }
        
        // GroupID
        _newGroupID = EditorGUILayout.TextField("GroupID", _newGroupID ?? hexState.GroupID ?? "1");
        
        EditorGUILayout.Space(10);
        
        // Apply button
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Changes", GUILayout.Height(30)))
            {
                ApplyHexChanges();
            }
            
            if (GUILayout.Button("Reset", GUILayout.Width(60), GUILayout.Height(30)))
            {
                ResetHexEditorValues();
            }
        }
    }

    private void ApplyHexChanges()
    {
        if (_selectedHex == null || _selectedHex.hexState == null) return;
        
        var hexState = _selectedHex.hexState;
        int col = hexState.Col;
        int row = hexState.Row;
        
        // First, update the MASTER state array (this is what gets saved and what SetLand reads from)
        if (_hexSpawner?.State?.hexes != null && 
            col >= 0 && col < _hexSpawner.State.hexes.Count &&
            row >= 0 && row < _hexSpawner.State.hexes[col].Count)
        {
            var masterState = _hexSpawner.State.hexes[col][row];
            if (masterState != null)
            {
                Undo.RecordObject(_hexSpawner, "Modify Hex State");
                
                bool changed = false;
                
                if (!string.IsNullOrEmpty(_newHexType) && _newHexType != masterState.HexType)
                {
                    masterState.HexType = _newHexType;
                    hexState.HexType = _newHexType;
                    changed = true;
                }
                
                if (_newRotation != masterState.Rotation)
                {
                    masterState.Rotation = _newRotation;
                    hexState.Rotation = _newRotation;
                    changed = true;
                }
                
                if (_newHexNum != masterState.HexNum)
                {
                    masterState.HexNum = _newHexNum;
                    hexState.HexNum = _newHexNum;
                    changed = true;
                }
                
                if (_newGroupID != masterState.GroupID)
                {
                    masterState.GroupID = _newGroupID;
                    hexState.GroupID = _newGroupID;
                    changed = true;
                }
                
                if (changed)
                {
                    // Refresh the visual representation
                    _hexSpawner.RefreshHex(_selectedHex);
                    
                    EditorUtility.SetDirty(_hexSpawner);
                    EditorUtility.SetDirty(_selectedHex);
                    Debug.Log($"Applied changes to hex [{col}, {row}]: Type={masterState.HexType}, Rot={masterState.Rotation}, Num={masterState.HexNum}, Group={masterState.GroupID}");
                    
                    // Re-validate to update the issue list
                    RunValidation();
                }
            }
        }
        else
        {
            Debug.LogError($"Cannot apply changes: hex [{col}, {row}] not found in master state array");
        }
    }

    private void ResetHexEditorValues()
    {
        if (_selectedHex?.hexState == null) return;
        
        var hexState = _selectedHex.hexState;
        _newHexType = hexState.HexType;
        _newRotation = hexState.Rotation;
        _newHexNum = hexState.HexNum;
        _newGroupID = hexState.GroupID;
    }

    private List<ValidationIssue> GetFilteredIssues()
    {
        if (_validationResult == null) return new List<ValidationIssue>();
        
        return _validationResult.Issues.Where(i =>
        {
            // Severity filter
            if (i.Severity == ValidationSeverity.Error && !_showErrors) return false;
            if (i.Severity == ValidationSeverity.Warning && !_showWarnings) return false;
            if (i.Severity == ValidationSeverity.Info && !_showInfo) return false;
            
            // Category filter
            if (_filterCategory.HasValue && i.Category != _filterCategory.Value) return false;
            
            return true;
        }).ToList();
    }

    private void SelectIssue(ValidationIssue issue)
    {
        _selectedIssue = issue;
        
        if (issue.HasHexReference)
        {
            SelectHexInScene(issue.Col, issue.Row);
        }
        
        Repaint();
    }

    private void SelectHexInScene(int col, int row)
    {
        ClearHexHighlight();
        
        if (_hexSpawner == null)
        {
            FindReferences();
            if (_hexSpawner == null)
            {
                Debug.LogWarning("Cannot find HexSpawner in scene");
                return;
            }
        }
        
        // Find the hex at the given coordinates
        var hexes = _hexSpawner.GetComponentsInChildren<Hex>();
        foreach (var hex in hexes)
        {
            if (hex.hexState != null && hex.hexState.Col == col && hex.hexState.Row == row)
            {
                _selectedHex = hex;
                Selection.activeGameObject = hex.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
                
                // Initialize editor values
                ResetHexEditorValues();
                
                // Highlight the hex
                HighlightHex(hex);
                
                return;
            }
        }
        
        Debug.LogWarning($"Could not find hex at [{col}, {row}]");
    }

    private void HighlightHex(Hex hex)
    {
        if (hex == null) return;
        
        // Store original colors and apply highlight
        var renderers = hex.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                if (!hex.hexState.originalMaterialColors.ContainsKey(renderer.gameObject))
                {
                    hex.hexState.originalMaterialColors[renderer.gameObject] = renderer.material.color;
                }
                renderer.material.color = Color.Lerp(renderer.material.color, Color.yellow, 0.5f);
            }
        }
    }

    private void ClearHexHighlight()
    {
        if (_selectedHex == null) return;
        
        // Restore original colors
        var renderers = _selectedHex.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null && 
                _selectedHex.hexState?.originalMaterialColors != null &&
                _selectedHex.hexState.originalMaterialColors.TryGetValue(renderer.gameObject, out var originalColor))
            {
                renderer.material.color = originalColor;
            }
        }
        
        _selectedHex.hexState?.originalMaterialColors?.Clear();
    }

    private void ClearHoverHighlight()
    {
        if (_hoveredHex == null || _hoveredHex == _selectedHex) return;
        
        // Restore original colors for hovered hex
        var renderers = _hoveredHex.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null && 
                _hoveredHex.hexState?.originalMaterialColors != null &&
                _hoveredHex.hexState.originalMaterialColors.TryGetValue(renderer.gameObject, out var originalColor))
            {
                renderer.material.color = originalColor;
            }
        }
        
        _hoveredHex.hexState?.originalMaterialColors?.Clear();
        _hoveredHex = null;
    }

    private void HighlightHexForHover(Hex hex)
    {
        if (hex == null || hex == _selectedHex || hex == _hoveredHex) return;
        
        // Clear previous hover
        ClearHoverHighlight();
        
        _hoveredHex = hex;
        
        // Store original colors and apply hover highlight (cyan tint)
        var renderers = hex.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer.material != null)
            {
                if (!hex.hexState.originalMaterialColors.ContainsKey(renderer.gameObject))
                {
                    hex.hexState.originalMaterialColors[renderer.gameObject] = renderer.material.color;
                }
                renderer.material.color = Color.Lerp(renderer.material.color, Color.cyan, 0.5f);
            }
        }
    }

    private Hex GetHexUnderMouse(SceneView sceneView)
    {
        if (_hexSpawner == null) return null;
        
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        
        // Raycast to find hex
        float closestDist = float.MaxValue;
        Hex closestHex = null;
        
        var hexes = _hexSpawner.GetComponentsInChildren<Hex>();
        foreach (var hex in hexes)
        {
            // Get collider or use bounds
            var collider = hex.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                if (collider.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        closestHex = hex;
                    }
                }
            }
            else
            {
                // Use bounds-based check as fallback
                var bounds = new Bounds(hex.transform.position, Vector3.one * 2f);
                if (bounds.IntersectRay(ray, out float dist))
                {
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestHex = hex;
                    }
                }
            }
        }
        
        return closestHex;
    }

    private void RunValidation()
    {
        FindReferences();
        
        if (_gameSpawner == null)
        {
            Debug.LogError("Cannot find GameSpawner in scene");
            _validationResult = new MapValidationResult();
            _validationResult.AddError(ValidationCategory.GridStructure, "GameSpawner not found in scene");
            return;
        }
        
        _validationResult = MapValidator.ValidateMap(
            _gameSpawner.State,
            _hexSpawner?.State,
            null, // Edge state - could add later
            null, // Corner state - could add later
            _gameConstants
        );
        
        _validationResult.MapPath = _gameSpawner.loadMapPath;
        
        Debug.Log($"Validation complete: {_validationResult.GetSummary()}");
        Repaint();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        // Handle hex selection mode
        if (_hexSelectionMode && _hexSpawner != null)
        {
            HandleHexSelectionMode(sceneView);
        }
        
        // Draw gizmos for hex issues
        DrawIssueGizmos(sceneView);
    }
    
    private void HandleHexSelectionMode(SceneView sceneView)
    {
        Event e = Event.current;
        
        // Change cursor to indicate selection mode
        EditorGUIUtility.AddCursorRect(new Rect(0, 0, sceneView.position.width, sceneView.position.height), MouseCursor.Link);
        
        // Get hex under mouse
        var hexUnderMouse = GetHexUnderMouse(sceneView);
        
        // Handle hover highlighting
        if (hexUnderMouse != null && hexUnderMouse != _hoveredHex && hexUnderMouse != _selectedHex)
        {
            HighlightHexForHover(hexUnderMouse);
            sceneView.Repaint();
        }
        else if (hexUnderMouse == null && _hoveredHex != null)
        {
            ClearHoverHighlight();
            sceneView.Repaint();
        }
        
        // Handle mouse events
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (hexUnderMouse != null)
            {
                // Clear hover highlight first
                ClearHoverHighlight();
                
                // Clear previous selection highlight
                ClearHexHighlight();
                
                // Select this hex
                _selectedHex = hexUnderMouse;
                Selection.activeGameObject = hexUnderMouse.gameObject;
                
                // Initialize editor values
                ResetHexEditorValues();
                
                // Highlight the selected hex
                HighlightHex(hexUnderMouse);
                
                // Look for related issues
                _selectedIssue = _validationResult?.Issues.FirstOrDefault(i => 
                    i.HasHexReference && i.Col == hexUnderMouse.hexState?.Col && i.Row == hexUnderMouse.hexState?.Row);
                
                // Repaint the editor window
                Repaint();
                
                Debug.Log($"Selected hex at [{hexUnderMouse.hexState?.Col}, {hexUnderMouse.hexState?.Row}] - {hexUnderMouse.hexState?.Type}");
                
                e.Use();
            }
        }
        
        // Right-click to exit selection mode
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            _hexSelectionMode = false;
            ClearHoverHighlight();
            Repaint();
            e.Use();
        }
        
        // ESC to exit selection mode
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            _hexSelectionMode = false;
            ClearHoverHighlight();
            Repaint();
            e.Use();
        }
        
        // Draw selection mode indicator
        Handles.BeginGUI();
        
        var indicatorStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        GUI.Box(new Rect(10, 10, 200, 30), "🎯 Hex Selection Mode", indicatorStyle);
        
        // Show hex info under cursor
        if (hexUnderMouse != null)
        {
            var hexInfo = $"[{hexUnderMouse.hexState?.Col}, {hexUnderMouse.hexState?.Row}] {hexUnderMouse.hexState?.Type}";
            GUI.Box(new Rect(10, 45, 200, 25), hexInfo, indicatorStyle);
        }
        
        GUI.backgroundColor = Color.white;
        Handles.EndGUI();
    }
    
    private void DrawIssueGizmos(SceneView sceneView)
    {
        if (_validationResult == null || !_showInfo) return;
        
        var hexIssues = _validationResult.Issues.Where(i => i.HasHexReference && 
            ((_showErrors && i.Severity == ValidationSeverity.Error) ||
             (_showWarnings && i.Severity == ValidationSeverity.Warning))).ToList();
        
        if (hexIssues.Count == 0) return;
        
        Handles.BeginGUI();
        
        foreach (var issue in hexIssues)
        {
            // Find the hex position
            if (_hexSpawner != null)
            {
                var hexes = _hexSpawner.GetComponentsInChildren<Hex>();
                var hex = hexes.FirstOrDefault(h => h.hexState?.Col == issue.Col && h.hexState?.Row == issue.Row);
                
                if (hex != null)
                {
                    var screenPos = HandleUtility.WorldToGUIPoint(hex.transform.position + Vector3.up);
                    var color = issue.Severity == ValidationSeverity.Error ? Color.red : Color.yellow;
                    
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        normal = { textColor = color },
                        fontSize = 16,
                        fontStyle = FontStyle.Bold
                    };
                    
                    GUI.Label(new Rect(screenPos.x - 10, screenPos.y - 20, 20, 20), 
                        GetSeverityIcon(issue.Severity), style);
                }
            }
        }
        
        Handles.EndGUI();
    }
}
#endif
