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
        if (float.IsNaN(panelHeight) || panelHeight <= 0) panelHeight = Screen.height; // Fallback
        
        float top = panelHeight - screenPos.y; // Invert Y
        
        float menuWidth = radialMenu.resolvedStyle.width;
        float menuHeight = radialMenu.resolvedStyle.height;
        
        if (float.IsNaN(menuWidth) || menuWidth <= 0) menuWidth = 300f; // Default fallback
        if (float.IsNaN(menuHeight) || menuHeight <= 0) menuHeight = 300f; // Default fallback
        
        radialMenu.style.left = screenPos.x - (menuWidth / 2);
        radialMenu.style.top = top - (menuHeight / 2);
        
        ArrangeButtonsRadial(menuWidth, menuHeight);
        
        radialMenu.style.display = DisplayStyle.Flex;
    }

    public void HideMenu()
    {
        if (radialMenu != null) radialMenu.style.display = DisplayStyle.None;
    }

    private void ArrangeButtonsRadial(float width = 300f, float height = 300f)
    {
        if (radialMenu == null) return;

        // Simple radial layout logic
        // Find all buttons in the menu
        var buttons = radialMenu.Query<Button>(className: "radial-button").ToList();
        int count = buttons.Count;
        
        if (count == 0) return;
        
        float radius = 100f; // pixel radius from center
        float angleStep = 360f / count;
        
        float centerX = width / 2;
        float centerY = height / 2;
        
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            
            // Dynamic button size offset
            float btnHalfSize = 25f; // Default if layout not ready
            if (!float.IsNaN(buttons[i].resolvedStyle.width) && buttons[i].resolvedStyle.width > 0)
            {
                btnHalfSize = buttons[i].resolvedStyle.width / 2;
            }

            buttons[i].style.left = centerX + x - btnHalfSize;
            buttons[i].style.top = centerY + y - btnHalfSize;
        }
    }
}
