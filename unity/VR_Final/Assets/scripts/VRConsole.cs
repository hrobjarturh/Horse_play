using UnityEngine;
using TMPro;

public class VRConsole : MonoBehaviour
{
    public TextMeshProUGUI consoleText;
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
