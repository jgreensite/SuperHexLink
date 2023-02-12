using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;
//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Networking.Transport;
//using Unity.NetCode;

public class GameUIManager : MonoBehaviour
{
    public Toggle m_ShowUI;
    public Button m_LeaveArea;
    public Button m_Spawn;
    public Button m_Load;
    public Button m_Save;
    public Button m_Refresh;
    public Button m_Clear;

    public VisualElement m_TitleScreen;
    public VisualElement m_HostScreen;
    public VisualElement m_JoinScreen;
    public VisualElement m_ManualConnectScreen;

    private VisualElement[] elementsToHide;

    //public new class UxmlFactory : UxmlFactory<GameUIManager, UxmlTraits> { }

    //public GameUIManager()
    //{
    //    this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    //}

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_ShowUI = root.Q<Toggle>("show-ui");
        m_LeaveArea = root.Q<Button>("quit-game");
        m_Spawn = root.Q<Button>("spawn-board");
        m_Load = root.Q<Button>("load-board");
        m_Save = root.Q<Button>("save-board");
        m_Refresh = root.Q<Button>("refresh-board");
        m_Clear = root.Q<Button>("clear-board");

        m_ShowUI.RegisterValueChangedCallback(OnToggleValueChanged);
        m_LeaveArea.clicked += () => ClickedButton("quit-game");
        m_Spawn.clicked += () => ClickedButton("spawn-board");
        m_Load.clicked += () => ClickedButton("load-board");
        m_Save.clicked += () => ClickedButton("save-board");
        m_Refresh.clicked += () => ClickedButton("refresh-board");
        m_Clear.clicked += () => ClickedButton("clear-board");

        //populate the array of elements to hide with all the elements in the UI except m_ShowUI
        elementsToHide = root.Children()
            .Where(element => element.name != "show-ui")
        .ToArray();

        //make the toggle m_ShowUI on by default
        m_ShowUI.value = true;
    }


    //capture toggle change event and show/hide the UI
    void OnToggleValueChanged(ChangeEvent<bool> evt)
    {
        foreach (var element in elementsToHide)
        {
            element.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // capture button click event and naviate to the correct screen/ call the correct function
    void ClickedButton(string txt)
    {
        var HexSpawner = GameObject.Find("HexSpawner").GetComponent<HexSpawner>();

        Debug.Log("Clicked" + txt);
        switch (txt)
        {
            case "show-ui":
                Debug.Log("Show UI");
                break;
            case "quit-game":
                SceneManager.LoadScene("MainMenu");
                break;
            case "spawn-board":
                Debug.Log("Spawn Board");
                HexSpawner.SpawnHexes();
                break;
            case "load-board":
                Debug.Log("Load Board");
                HexSpawner.LoadState(null);
                break;
            case "save-board":
                Debug.Log("Save Board");
                HexSpawner.SaveHexes(null);
                break;
            case "refresh-board":
                Debug.Log("Refresh Board");
                HexSpawner.RefeshHexes();
                break;
            case "clear-board":
                Debug.Log("Clear Board");
                HexSpawner.ClearHexes();
                break;
            default:
                Debug.Log("Unknown Button");
                break;
        }
    }   
}