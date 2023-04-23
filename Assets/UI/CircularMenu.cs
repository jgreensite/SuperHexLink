using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class CircularMenu : MonoBehaviour
{
    [SerializeField] private List<VisualTreeAsset> menuItems;
    [SerializeField] private float radius = 100f;
    [SerializeField] private float startingAngle = 0f;
    [SerializeField] private float menuItemSize = 50f;
    [SerializeField] private List<CircularMenu> subMenus;
    [SerializeField] private CircularMenu parentMenu;

    private VisualElement menuContainer;

    private void Start()
    {
        // Disable the menu by default
        gameObject.SetActive(false);
    }

public void ShowCircularMenu()
{
    // Check if the user has clicked the mouse button
    if (Input.GetMouseButtonDown(0))
    {
        // Get the position of the mouse click in screen coordinates
        Vector3 clickPosition = Input.mousePosition;
        clickPosition.z = -10f; // Set the z position to a constant value

        // Activate the menu at the click position
        gameObject.SetActive(true);

        if (menuContainer == null)
        {
            menuContainer = new VisualElement();
            GetComponentInParent<UIDocument>().rootVisualElement.Add(menuContainer);
            CreateCircularMenu();
        }

        // Position the menu container at the click position
        menuContainer.style.position = Position.Absolute;
        menuContainer.style.left = new StyleLength(clickPosition.x);
        menuContainer.style.top = new StyleLength(Screen.height - clickPosition.y);

        Debug.Log("Menu Container: " + menuContainer);
        Debug.Log("Menu Container Position: " + menuContainer.style.left.value + ", " + menuContainer.style.top.value);
    }
}

private void CreateCircularMenu()
{
    int itemCount = menuItems.Count;
    float angleIncrement = 360f / itemCount;

    for (int i = 0; i < itemCount; i++)
    {
        var menuItemTemplate = menuItems[i];
        var menuItemContainer = new VisualElement();
        menuItemContainer.name = "MenuItemContainer";
        menuContainer.Add(menuItemContainer);

        float angle = startingAngle + (angleIncrement * i);
        float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

        // Position the menu item container around the circle
        menuItemContainer.style.position = Position.Absolute;
        menuItemContainer.style.left = new StyleLength(x);
        menuItemContainer.style.top = new StyleLength(y);

        // Instantiate each child object separately and add them to the parent container
        var menuItemInstance = menuItemTemplate.CloneTree();
        menuItemContainer.Add(menuItemInstance);
        menuItemInstance.style.position = Position.Absolute;
        menuItemInstance.style.left = new StyleLength(0f);
        menuItemInstance.style.top = new StyleLength(0f);

        var button = new Button();
        menuItemInstance.Add(button);
        button.clickable.clicked += () =>
        {
            if (subMenus.Count > i && subMenus[i] != null)
            {
                subMenus[i].gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
        };
    }

    // Add a "Back" button to the menu, if there is a parent menu assigned
    if (parentMenu != null)
    {
        var backButton = new Button();
        backButton.text = "Back";
        backButton.clickable.clicked += () =>
        {
            gameObject.SetActive(false);
            parentMenu.gameObject.SetActive(true);
        };
        menuContainer.Add(backButton);
        backButton.style.position = Position.Absolute;
        backButton.style.left = new StyleLength(-50f);
        backButton.style.top = new StyleLength(0f);
    }
}


/*
private void CreateCircularMenu()
{
    int itemCount = menuItems.Count;
    float angleIncrement = 360f / itemCount;

    for (int i = 0; i < itemCount; i++)
    {
        var menuItemTemplate = menuItems[i];
        var menuItemContainer = new VisualElement();
        menuItemContainer.name = "MenuItemContainer";
        menuContainer.Add(menuItemContainer);

        float angle = startingAngle + (angleIncrement * i);
        float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);

        // Position the menu item container around the circle
        menuItemContainer.style.position = Position.Absolute;
        menuItemContainer.style.left = new StyleLength(x);
        menuItemContainer.style.top = new StyleLength(y);

        // Instantiate each child object separately and add them to the parent container
        var menuItemInstance = new TemplateContainer(menuItemTemplate);
        menuItemContainer.Add(menuItemInstance);
        menuItemInstance.style.position = Position.Absolute;
        menuItemInstance.style.left = new StyleLength(0f);
        menuItemInstance.style.top = new StyleLength(0f);

        var button = new Button();
        menuItemInstance.Add(button);
        button.clickable.clicked += () =>
        {
            if (subMenus.Count > i && subMenus[i] != null)
            {
                subMenus[i].gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
        };
    }

    // Add a "Back" button to the menu, if there is a parent menu assigned
    if (parentMenu != null)
    {
        var backButton = new Button();
        backButton.text = "Back";
        backButton.clickable.clicked += () =>
        {
            gameObject.SetActive(false);
            parentMenu.gameObject.SetActive(true);
        };
        menuContainer.Add(backButton);
        backButton.style.position = Position.Absolute;
        backButton.style.left = new StyleLength(-50f);
        backButton.style.top = new StyleLength(0f);
    }
}

 public void ShowCircularMenu2()
        {
            // Check if the user has clicked the mouse button
            if (Input.GetMouseButtonDown(0))
            {
                // Get the position of the mouse click in world coordinates
                Vector3 clickPosition = Input.mousePosition;
                clickPosition.z = -10f; // Set the z position to a constant value
                //clickPosition = GetComponentInParent<UIPanel>().transform.TransformPoint(clickPosition);

                // Activate the menu at the click position
                gameObject.SetActive(true);
                menuContainer = new VisualElement();
                GetComponentInParent<UIDocument>().rootVisualElement.Add(menuContainer);
                menuContainer.transform.position = clickPosition;

                Debug.Log("Menu Container: " + menuContainer);
                Debug.Log("Menu Container Position: " + menuContainer.transform.position);
                CreateCircularMenu();
            }
        }
*/
/*
private void CreateCircularMenu()
{
    int itemCount = menuItems.Count;
    float angleIncrement = 360f / itemCount;

    for (int i = 0; i < itemCount; i++)
    {
        var menuItemPrefab = menuItems[i];
        var menuItemInstance = Instantiate(menuItemPrefab);
        var menuItemContainer = menuItemInstance.GetComponent<VisualElement>();
        menuItemContainer.name = "MenuItemContainer";
        menuContainer.Add(menuItemContainer);

        float angle = startingAngle + (angleIncrement * i);
        float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        var position = new Vector2(x, y);

        var button = new Button();
        menuItemContainer.Add(button);
        button.clickable.clicked += () =>
        {
            if (subMenus.Count > i && subMenus[i] != null)
            {
                subMenus[i].gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
        };

        // Position the menu item container around the circle
        menuItemContainer.style.position = Position.Absolute;
        menuItemContainer.style.left = new StyleLength(x);
        menuItemContainer.style.top = new StyleLength(y);
    }

    // Add a "Back" button to the menu, if there is a parent menu assigned
    if (parentMenu != null)
    {
        var backButton = new Button();
        backButton.text = "Back";
        backButton.clickable.clicked += () =>
        {
            gameObject.SetActive(false);
            parentMenu.gameObject.SetActive(true);
        };
        menuContainer.Add(backButton);
        backButton.style.position = Position.Absolute;
        backButton.style.left = new StyleLength(-50f);
        backButton.style.top = new StyleLength(0f);
    }
}

    private void CreateCircularMenu2()
{
    int itemCount = menuItems.Count;
    float angleIncrement = 360f / itemCount;

    for (int i = 0; i < itemCount; i++)
    {
        var menuItemPrefab = menuItems[i];
        var menuItemContainer = new VisualElement();
        menuItemContainer.name = "MenuItemContainer";
        menuContainer.Add(menuItemContainer);

        float angle = startingAngle + (angleIncrement * i);
        float x = radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        var position = new Vector2(x, y);

        // Instantiate each child object separately and add them to the parent container
        foreach (Transform child in menuItemPrefab.transform)
        {
            var childInstance = new VisualElement();
            childInstance.name = child.gameObject.name;
            menuItemContainer.Add(childInstance);
        }

        var button = new Button();
        menuItemContainer.Add(button);
        button.clickable.clicked += () =>
        {
            if (subMenus.Count > i && subMenus[i] != null)
            {
                subMenus[i].gameObject.SetActive(true);
                gameObject.SetActive(false);
            }
        };

        // Position the menu item container around the circle
        menuItemContainer.transform.position = position;
    }

    // Add a "Back" button to the menu, if there is a parent menu assigned
    if (parentMenu != null)
    {
        var backButton = new Button();
        backButton.text = "Back";
        backButton.clickable.clicked += () =>
        {
            gameObject.SetActive(false);
            parentMenu.gameObject.SetActive(true);
        };
        menuContainer.Add(backButton);
        backButton.transform.position = new Vector2(-50f, 0f);
    }
}
*/

}
