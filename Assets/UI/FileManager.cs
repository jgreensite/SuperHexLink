using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using SimpleFileBrowser;


public class FileManager : MonoBehaviour
{
    public Text filePathText;

    void Start()
    {
        // Set filters (optional)
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Text Files", ".txt"), new FileBrowser.Filter("Images", ".jpg", ".png"));

        // Set default filter (optional)
        FileBrowser.SetDefaultFilter(".txt");

        // Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
        // Note that when you use this function, .lnk and .tmp extensions will no longer be
        // excluded unless you explicitly add them as parameters to the function
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe");

        // Add a listener to the file browser
        FileBrowser.AddQuickLink("Users", "C:\\Users", null);
        FileBrowser.AddQuickLink("Desktop", "C:\\Users\\%USERNAME%\\Desktop", null);
        FileBrowser.AddQuickLink("My Documents", "C:\\Users\\%USERNAME%\\Documents", null);

        // Disable the text component if not supported
        if (!IsFileBrowserSupported())
        {
            filePathText.text = "Native file browser not supported";
            filePathText.enabled = false;
        }
    }

    public void OpenFileBrowser()
    {
        FileBrowser.ShowLoadDialog((string[] paths) => {
            filePathText.text = "Selected file: " + paths[0];
        }, () => {
            filePathText.text = "File selection cancelled";
        }, FileBrowser.PickMode.FilesAndFolders, false, null);
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
