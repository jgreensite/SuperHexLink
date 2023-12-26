using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Toggle = UnityEngine.UIElements.Toggle;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using SimpleFileBrowser;
using System.Collections;

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

    public string filePathText;
    public string currentDir;

    public GameSpawner GameSpawner;

    // Start is called before the first frame update
    void Start()
    {
        GameSpawner = GameObject.Find("GameSpawner").GetComponent<GameSpawner>();

        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
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

        m_ShowUI.value = true;

        //file browser
        currentDir = System.IO.Path.GetDirectoryName(Application.dataPath);
        // Set filters (optional)
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Maps", ".json"));

        // Set default filter (optional)
        FileBrowser.SetDefaultFilter(".json");

        // Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
        // Note that when you use this function, .lnk and .tmp extensions will no longer be
        // excluded unless you explicitly add them as parameters to the function
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // Add a listener to the file browser

        FileBrowser.AddQuickLink("Maps", currentDir, null);

        //Used for capturing unsupported platforms
        if (!IsFileBrowserSupported())
        {
            //currently do nothing
        }
    }

    //capture toggle change event and show/hide the UI
    void OnToggleValueChanged(ChangeEvent<bool> evt)
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        //todo - This is a but clumsy, would be more elegant as a single line using a where and also changing the property
        //todo - have a look at https://docs.unity3d.com/Manual/UIE-UQuery.html for inspiration
        List<VisualElement> elementsToHide = root.Query("bottom-container").ToList();
        elementsToHide.Add(root.Query("top-right-container"));
        elementsToHide.Add(root.Query("quit-game"));
        foreach (var element in elementsToHide)
        {
            element.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    // capture button click event and naviate to the correct screen/ call the correct function
    void ClickedButton(string txt)
    {

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
                GameSpawner.Spawn();
                break;
            case "load-board":
                Debug.Log("Load Board");
                StartCoroutine (OpenFileBrowser());
                break;
            case "save-board":
                Debug.Log("Save Board");
                GameSpawner.SaveHexes(null);
                break;
            case "refresh-board":
                Debug.Log("Refresh Board");
                GameSpawner.Refresh();
                break;
            case "clear-board":
                Debug.Log("Clear Board");
                GameSpawner.Clear();
                break;
            default:
                Debug.Log("Unknown Button");
                break;
        }
    }
 
    IEnumerator OpenFileBrowser()
    {
        /*
        FileBrowser.ShowLoadDialog((string[] paths) => {
            filePathText = paths[0];
        }, () => {
            filePathText = null;
        }, SimpleFileBrowser.FileBrowser.PickMode.Files, false, null, currentDir);
        */
        // Show a load file dialog and wait for a response from user
        //yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, currentDir,"map.jsopn", "Select Map", "Select" );
		
        // Show a select folder dialog 
		yield return FileBrowser.ShowLoadDialog( ( paths ) => { Debug.Log( "Selected: " + paths[0] ); filePathText = paths[0];GameSpawner.LoadState(filePathText);},
								   () => { Debug.Log( "Canceled" ); },
								   FileBrowser.PickMode.Files, false, currentDir, "map.json", "Select Map", "Select" );
    }
 
    bool IsFileBrowserSupported()
    {
        return Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer;
    }   
}