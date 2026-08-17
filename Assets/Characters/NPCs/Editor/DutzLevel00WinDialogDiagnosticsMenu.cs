#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>MCP entry points for Level 00 win dialog diagnostics (Play mode).</summary>
public static class DutzLevel00WinDialogDiagnosticsMenu
{
    const string MenuPath = "Tools/Dutz/Diagnostics/Level00 Win Dialog";

    [MenuItem(MenuPath)]
    public static void RunFromMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Dutz] Level00 win dialog diagnostic requires Play mode on Dutz_Level00.");
            return;
        }

        DutzLevel00WinDialogDiagnostics.SimulateWinPlayModeBatch();
    }
}
#endif
