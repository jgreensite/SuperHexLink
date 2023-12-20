using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class CircularMenu : MonoBehaviour
{
    [System.Serializable]
    public class CircularMenuData
    {
        public GameObject menuItemPrefab; // Prefab for menu item.
        public CircularMenu circularMenu; // Submenu, if any.
    }

    [SerializeField] private List<CircularMenuData> menuDataList;
    [SerializeField] private GameObject backButtonPrefab; // Prefab for back button.
    [SerializeField] private GameObject cancelButtonPrefab; // Prefab for cancel button.
    [SerializeField] private float menuRadius = 1f;
    [SerializeField] private float startingAngle = 0f;
    [SerializeField] private CircularMenu parentMenu;

    private void Start()
    {
        CreateCircularMenu();
        //gameObject.SetActive(false); // Initially hide the menu.
    }

    private void CreateCircularMenu()
    {
        float angleIncrement = 360f / (menuDataList.Count + (parentMenu ? 1 : 0));

        // Instantiate regular menu items.
        for (int i = 0; i < menuDataList.Count; i++)
        {
            InstantiateMenuItem(menuDataList[i].menuItemPrefab, angleIncrement, i);
        }

        // Optionally add back or cancel button.
        if (parentMenu != null)
        {
            InstantiateMenuItem(backButtonPrefab, angleIncrement, menuDataList.Count); // Back button.
        }
        else
        {
            InstantiateMenuItem(cancelButtonPrefab, angleIncrement, menuDataList.Count); // Cancel button.
        }
    }

    private void InstantiateMenuItem(GameObject prefab, float angleIncrement, int index)
    {
    if (prefab == null)
    {
        Debug.LogError("Prefab is null in InstantiateMenuItem.");
        return;
    }

    if (menuRadius <= 0)
    {
        Debug.LogError("Invalid menuRadius: " + menuRadius + ". Radius must be positive.");
        return;
    }

    if (menuDataList.Count == 0)
    {
        Debug.LogWarning("menuDataList is empty. No menu items to instantiate.");
        return;
    }

    float angle = startingAngle + (angleIncrement * index);
    float x = menuRadius * Mathf.Cos(angle * Mathf.Deg2Rad);
    float z = menuRadius * Mathf.Sin(angle * Mathf.Deg2Rad);

    // Check for NaN values
    if (float.IsNaN(x) || float.IsNaN(z))
    {
        Debug.LogError("Invalid position calculated for menu item: x=" + x + ", z=" + z);
        return;
    }

    GameObject menuItem = Instantiate(prefab, transform);
    menuItem.transform.localPosition = new Vector3(x, GameConstants.FLOATING_MENU_OFFSET, z);

    // Set the layer of the menu item
    menuItem.layer = LayerMask.NameToLayer("CircularMenuLayer");

    // If the menu item has child objects, you need to set their layers too
    SetLayerRecursively(menuItem, menuItem.layer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /*
    public void ShowCircularMenu(Vector3 position)
    {
        Debug.Log("ShowCircularMenu");
        transform.position = position;
        gameObject.SetActive(true); // Make sure the menu is active

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true); // Also activate each menu item
        }
    }
    */
    public void ForceCircularMenuVisible()
    {
        Debug.Log("ForceCicularMenuVisible");
        gameObject.SetActive(true); // Make sure the menu is active

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true); // Also activate each menu item
            Debug.Log("child.gameObject.name: " + child.gameObject.name + " activeSelf: " + child.gameObject.activeSelf);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (parentMenu != null)
        {
            parentMenu.Hide();
        }
    }
}
