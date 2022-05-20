using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Networking.Transport;
//using Unity.NetCode;

public class GameUIManager : VisualElement
{
    VisualElement m_LeaveArea;
    VisualElement m_Spawn;
    VisualElement m_Load;
    VisualElement m_Refresh;

    VisualElement m_TitleScreen;
    VisualElement m_HostScreen;
    VisualElement m_JoinScreen;
    VisualElement m_ManualConnectScreen;

    public new class UxmlFactory : UxmlFactory<GameUIManager, UxmlTraits> { }

    public GameUIManager()
    {
        this.RegisterCallback<GeometryChangedEvent>(OnGeometryChange);
    }

    void OnGeometryChange(GeometryChangedEvent evt)
    {
        m_LeaveArea = this.Q("quit-game");
        m_Spawn = this.Q("spawn-board");
        m_Load = this.Q("load-board");
        m_Refresh = this.Q("refresh-board");

        m_LeaveArea?.RegisterCallback<ClickEvent>(ev => ClickedButton());
        

        m_TitleScreen = this.Q("TitleScreen");
        m_HostScreen = this.Q("HostGameScreen");
        m_JoinScreen = this.Q("JoinGameScreen");
        m_ManualConnectScreen = this.Q("ManualConnectScreen");

        m_TitleScreen?.Q("host-local-game")?.RegisterCallback<ClickEvent>(ev => EnableHostScreen());
        m_TitleScreen?.Q("join-local-game")?.RegisterCallback<ClickEvent>(ev => EnableJoinScreen());
        m_TitleScreen?.Q("manual-connect")?.RegisterCallback<ClickEvent>(ev => EnableManualScreen());

        m_HostScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
        m_JoinScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());
        m_ManualConnectScreen?.Q("back-button")?.RegisterCallback<ClickEvent>(ev => EnableTitleScreen());

    }

    // Start is called before the first frame update
    void Start()
    {

    }

    void ClickedButton()
    {

        Debug.Log("Clicked quit game");
    }
}