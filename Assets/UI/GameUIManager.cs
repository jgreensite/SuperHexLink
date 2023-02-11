using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Networking.Transport;
//using Unity.NetCode;

public class GameUIManager : MonoBehaviour
{
    public Button m_LeaveArea;
    public Button m_Spawn;
    public Button m_Load;
    public Button m_Refresh;
    public Button m_Clear;

    public VisualElement m_TitleScreen;
    public VisualElement m_HostScreen;
    public VisualElement m_JoinScreen;
    public VisualElement m_ManualConnectScreen;

    //public new class UxmlFactory : UxmlFactory<GameUIManager, UxmlTraits> { }

    //public GameUIManager()
    //{
    //    this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    //}

    // Start is called before the first frame update
    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        m_LeaveArea = root.Q<Button>("quit-game");
        m_Spawn = root.Q<Button>("spawn-board");
        m_Load = root.Q<Button>("load-board");
        m_Refresh = root.Q<Button>("refresh-board");
        m_Clear = root.Q<Button>("clear-board");

        m_LeaveArea.clicked += () => ClickedButton("quit-game");
        m_Spawn.clicked += () => ClickedButton("spawn-board");
        m_Load.clicked += () => ClickedButton("load-board");
        m_Refresh.clicked += () => ClickedButton("refresh-board");
        m_Clear.clicked += () => ClickedButton("clear-board"); 
    }


    // capture click event and navige to the correct screen
    void ClickedButton(string txt)
    {
        var HexSpawner = GameObject.Find("HexSpawner").GetComponent<HexSpawner>();

        Debug.Log("Clicked" + txt);
        switch (txt)
        {
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