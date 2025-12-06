#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using SuperHexLink.Validation;

/// <summary>
/// Editor window for validating map state and fixing issues.
/// </summary>
public class MapValidationWindow : EditorWindow
{
    private MapValidationResult _validationResult;
    private Vector2 _scrollPosition;
    private Vector2 _detailScrollPosition;
    
    // Filtering
    private bool _showErrors = true;
    private bool _showWarnings = true;
    private bool _showInfo = false;
    private ValidationCategory? _filterCategory = null;
    
    // Selection
    private ValidationIssue _selectedIssue;
    private Hex _selectedHex;
    
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

    [MenuItem("SuperHexLink/Map Validation Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapValidationWindow>("Map Validation");
        window.minSize = new Vector2(450, 400);
        window.Show();
    }

    private void OnEnable()
    {
        FindReferences();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        ClearHexHighlight();
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
        
        foreach (var issue in filteredIssues)
        {
            DrawIssueRow(issue);
        }
        
        EditorGUILayout.EndScrollView();
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
            
            // Message
            if (GUILayout.Button(issue.Message, EditorStyles.label))
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
        
        Undo.RecordObject(_selectedHex, "Modify Hex");
        
        var hexState = _selectedHex.hexState;
        bool changed = false;
        
        if (!string.IsNullOrEmpty(_newHexType) && _newHexType != hexState.HexType)
        {
            hexState.HexType = _newHexType;
            changed = true;
        }
        
        if (_newRotation != hexState.Rotation)
        {
            hexState.Rotation = _newRotation;
            changed = true;
        }
        
        if (_newHexNum != hexState.HexNum)
        {
            hexState.HexNum = _newHexNum;
            changed = true;
        }
        
        if (_newGroupID != hexState.GroupID)
        {
            hexState.GroupID = _newGroupID;
            changed = true;
        }
        
        if (changed)
        {
            // Update the visual representation
            if (_hexSpawner != null)
            {
                // Trigger a refresh of just this hex
                _hexSpawner.SendMessage("RefreshHex", _selectedHex, SendMessageOptions.DontRequireReceiver);
            }
            
            EditorUtility.SetDirty(_selectedHex);
            Debug.Log($"Applied changes to hex [{hexState.Col}, {hexState.Row}]");
            
            // Re-validate to update the issue list
            RunValidation();
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
        // Draw gizmos for hex issues
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
