#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bakes Sen Gong Bong spawn lock poses into Dutz_Level01 scene objects.</summary>
public static class DutzJonremEscortSpawnSync
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";

    /// <summary>Batch: -executeMethod DutzJonremEscortSpawnSync.ApplyOnLevel01Batch</summary>
    public static void ApplyOnLevel01Batch() => ApplyOnLevel01(log: true);

    public static bool ApplyOnLevel01(bool log)
    {
        if (!System.IO.File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level01.unity not found.");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

        DutzJonremEscortSpawnLock.BakeAllEscortSpawnPointsFromScene();

        var jonrem = DutzGiantBossNames.FindJonrem();
        if (jonrem == null)
        {
            Debug.LogError("[Dutz] JONREM not found on Dutz_Level01.");
            return false;
        }

        Undo.RecordObject(jonrem.transform, "Sync Jonrem Escort Spawns");
        EditorUtility.SetDirty(jonrem);

        foreach (var officer in DutzJonremPoliceBehavior.FindJonremPolice())
        {
            if (officer == null)
                continue;

            Undo.RecordObject(officer.transform, "Sync Jonrem Escort Spawns");
            var respawn = officer.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn != null)
                Undo.RecordObject(respawn, "Sync Jonrem Escort Spawns");
            EditorUtility.SetDirty(officer);
        }

        var jonremRespawn = jonrem.GetComponent<SimpleCitizensNpcRespawn>();
        if (jonremRespawn != null)
            Undo.RecordObject(jonremRespawn, "Sync Jonrem Escort Spawns");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Baked JONREM + Jonrem Police spawn points from scene layout behind {DutzJonremEscortSpawnLock.AnchorGiantName} " +
                $"(JONREM at {DutzJonremEscortSpawnLock.JonremPosition}).");
        }

        return true;
    }
}
#endif
