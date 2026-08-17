using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Clears the Unity Console (used by MCP verification / log cleanup).</summary>
public static class DutzClearConsole
{
    const string MenuPath = "Window/Clear Console (Dutz)";

    [MenuItem(MenuPath)]
    public static void Clear()
    {
        var logEntries = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
        var clear = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        if (clear == null)
        {
            Debug.LogWarning("[Dutz] Could not clear console (LogEntries.Clear missing).");
            return;
        }

        clear.Invoke(null, null);
        Debug.Log("[Dutz] Console cleared.");
    }
}
