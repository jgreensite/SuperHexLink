using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CircularMenu : MonoBehaviour
{
    [System.Serializable]
    public class CircularMenuData
    {
        public CircularMenu circularMenu;
    }

    public VisualElement menuRoot;

    [SerializeField] private List<VisualTreeAsset> menuItems;
    [SerializeField] private float radius = 100f;
    [SerializeField] private float startingAngle = 0f;
    [SerializeField] private List<CircularMenuData> subMenus;
    [SerializeField] private CircularMenu parentMenu;

    private VisualElement menuContainer;

    private void Awake()
    {
        // Initialize the menuRoot property for the current menu
        menuRoot.style.display = DisplayStyle.None;

        // Initialize the menuRoot property for each submenu
        InitializeSubmenus();
    }

    private void Start()
    {
        // Disable the menu by default
        gameObject.SetActive(false);
    }

    private void InitializeSubmenus()
    {
        foreach (CircularMenuData submenuData in subMenus)
        {
            CircularMenu submenu = submenuData.circularMenu;
            if (submenu != null && submenu.menuRoot != null)
            {
                submenu.menuRoot.style.display = DisplayStyle.None;
                submenu.InitializeSubmenus(); // Call InitializeSubmenus() method recursively for each submenu
            }
        }
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

            if (menuRoot == null)
            {
                menuRoot = new VisualElement();
                GetComponentInParent<UIDocument>().rootVisualElement.Add(menuRoot);
                CreateCircularMenu();
            }

            // Position the menu container at the click position
            menuRoot.style.position = Position.Absolute;
            menuRoot.style.left = new StyleLength(clickPosition.x);
            menuRoot.style.top = new StyleLength(Screen.height - clickPosition.y);

            // Hide submenus
            foreach (CircularMenuData submenuData in subMenus)
            {
                CircularMenu submenu = submenuData.circularMenu;
                submenu.menuRoot.style.display = DisplayStyle.None;
            }

            Debug.Log("Menu Container: " + menuRoot);
            Debug.Log("Menu Container Position: " + menuRoot.style.left.value + ", " + menuRoot.style.top.value);
        }
    }

    private void CreateCircularMenu()
    {
        int itemCount = menuItems.Count;
        float angleIncrement = 360f / itemCount;

        for (int i = 0; i < itemCount; i++)
        {
            int submenuItemCount = subMenus.Count > i && subMenus[i].circularMenu != null ? subMenus[i].circularMenu.menuItems.Count : 0;

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

            // Find the existing button within the menuItemInstance
            Button button = menuItemInstance.Q<Button>();

            if (subMenus.Count > i && subMenus[i].circularMenu != null)
            {
                button.clickable.clicked += () =>
                {
                    subMenus[i].circularMenu.menuRoot.style.display = DisplayStyle.Flex;
                    subMenus[i].circularMenu.parentMenu = this;
                    menuRoot.style.display = DisplayStyle.None;
                };
            }
            else
            {
                button.clickable.clicked += () =>
                {
                    Debug.Log("Clicked on menu item: " + menuItemInstance.name);
                };
            }
        }

        // Add a "Back" or "Cancel" button at the center of the circle, depending on whether there is a parent menu
        var centerButton = new Button();
        if (parentMenu != null)
        {
            centerButton.text = "Back";
            centerButton.clickable.clicked += () =>
            {
                parentMenu.gameObject.SetActive(true);
                gameObject.SetActive(false);
            };
        }
        else
        {
            centerButton.text = "Cancel";
            centerButton.clickable.clicked += () =>
            {
                gameObject.SetActive(false);
            };
        }

        menuContainer.Add(centerButton);
        centerButton.style.position = Position.Absolute;
        centerButton.style.left = new StyleLength(-25f); // Set half of the desired button width
        centerButton.style.top = new StyleLength(-25f);  // Set half of the desired button height
    }
}