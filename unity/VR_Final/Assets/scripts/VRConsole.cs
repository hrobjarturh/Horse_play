using UnityEngine;
using TMPro; // or using UnityEngine.UI if you're not using TMP

public class VRConsole : MonoBehaviour
{
    public TextMeshProUGUI consoleText; // or Text if using legacy UI
    private string logOutput = "";

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
        
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logOutput += logString + "\n";
        if (consoleText != null)
        {
            consoleText.text = logOutput;
        }
    }
}
