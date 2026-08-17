using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authoring helper — adds airplane win marker and touch colliders on Dutz3dModel in Level 0.</summary>
public static class DutzAirplaneGoalSetup
{
    /// <summary>Batch: -executeMethod DutzAirplaneGoalSetup.EnsureOnLevel00Batch</summary>
    public static void EnsureOnLevel00Batch() => EnsureOnLevel00(log: true);

    public static bool EnsureOnLevel00(bool log)
    {
        if (EditorApplication.isPlaying)
        {
            if (log)
                Debug.LogError("[Dutz] Exit Play mode before setting up Level 0 airplane goal.");
            return false;
        }

        var scenePath = DutzLevel02Setup.Level00ScenePath;
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var airplane = DutzAirplaneGoal.FindAirplaneObject();
        if (airplane == null)
        {
            if (log)
                Debug.LogError("[Dutz] Dutz3dModel not found in Dutz_Level00.");
            return false;
        }

        var marker = airplane.GetComponent<DutzAirplaneGoal>();
        if (marker == null)
            marker = Undo.AddComponent<DutzAirplaneGoal>(airplane);

        marker.EnsureTouchColliders();
        EditorUtility.SetDirty(airplane);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        if (log)
            Debug.Log("[Dutz] Airplane win goal ready on Dutz3dModel in Dutz_Level00.");
        return true;
    }
}
