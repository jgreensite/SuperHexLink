using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorUIManager : MonoBehaviour
{
    private VisualElement root;
    private VisualElement radialMenu;
    private VisualElement centerHub;
    
    private Action<string> currentCallback;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc != null)
        {
            root = uiDoc.rootVisualElement;
            radialMenu = root.Q("radial-menu");
            centerHub = root.Q("center-hub");
            
            // Hide by default
            if (radialMenu != null) radialMenu.style.display = DisplayStyle.None;
            
            // Register button callbacks
            RegisterRadialButton("btn-forest", "forest");
            RegisterRadialButton("btn-sea", "sea");
            RegisterRadialButton("btn-desert", "desert");
            RegisterRadialButton("btn-mountain", "mountain");
            RegisterRadialButton("btn-field", "field");
            RegisterRadialButton("btn-pasture", "pasture");
            
            RegisterRadialButton("btn-rotate", "CMD_ROTATE");
            RegisterRadialButton("btn-number", "CMD_NUMBER");
            
            // Close when clicking center
            if (centerHub != null)
            {
                centerHub.RegisterCallback<ClickEvent>(evt => HideMenu());
            }
        }
    }

    private void RegisterRadialButton(string btnName, string navPayload)
    {
        var btn = root.Q<Button>(btnName);
        if (btn != null)
        {
            btn.clicked += () => 
            {
                currentCallback?.Invoke(navPayload);
                HideMenu();
            };
        }
    }

    public void ShowMenu(Vector2 screenPos, Action<string> onSelect)
    {
        if (radialMenu == null) return;

        currentCallback = onSelect;
        
        // Convert screen pos to local panel pos if necessary, 
        // but for absolute positioning on root, screen coords often work directly 
        // if the panel matches screen size.
        // NOTE: UI Toolkit (0,0) is top-left. Input.mousePosition (0,0) is bottom-left.
        
        float panelHeight = root.resolvedStyle.height;
        float top = panelHeight - screenPos.y; // Invert Y
        
        radialMenu.style.left = screenPos.x - (radialMenu.resolvedStyle.width / 2);
        radialMenu.style.top = top - (radialMenu.resolvedStyle.height / 2);
        
        ArrangeButtonsRadial();
        
        radialMenu.style.display = DisplayStyle.Flex;
    }

    public void HideMenu()
    {
        if (radialMenu != null) radialMenu.style.display = DisplayStyle.None;
    }

    private void ArrangeButtonsRadial()
    {
        // Simple radial layout logic
        // Find all buttons in the menu
        var buttons = radialMenu.Query<Button>(className: "radial-button").ToList();
        int count = buttons.Count;
        float radius = 100f; // pixel radius from center
        float angleStep = 360f / count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            // center is (150, 150) if width/height is 300
            // assuming button center alignment
            
            buttons[i].style.left = 150 + x - 25; // 25 is half button width
            buttons[i].style.top = 150 + y - 25;
        }
    }
}
