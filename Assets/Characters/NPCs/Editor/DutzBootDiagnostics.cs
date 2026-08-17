using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Edit-mode and play-mode boot validation for Dutz level scenes.
/// </summary>
public static class DutzBootDiagnostics
{
    public static void DiagnoseFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            if (DutzBootValidator.Validate(out var playError))
                Debug.Log("[Dutz] Boot diagnostics PASSED (play mode runtime validation).");
            else
                Debug.LogError("[Dutz] Boot diagnostics FAILED (play mode): " + playError);
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!DutzMobileRuntime.IsDutzLevelScene(scene.name))
        {
            EditorUtility.DisplayDialog(
                "Diagnose Scene Boot",
                "Open Dutz_Level01 or Dutz_Level02, then run this again.\n\n" +
                "Or enter Play mode to run full runtime validation.",
                "OK");
            return;
        }

        if (DutzBootValidator.ValidateSceneHierarchy(out var hierarchyError))
            Debug.Log($"[Dutz] Boot hierarchy diagnostics PASSED for {scene.name}.");
        else
            Debug.LogError($"[Dutz] Boot hierarchy diagnostics FAILED for {scene.name}: {hierarchyError}");
    }

    /// <summary>Batch: -executeMethod DutzBootDiagnostics.DiagnoseActiveSceneBatch</summary>
    public static void DiagnoseActiveSceneBatch() => DiagnoseFromMenu();
}
