using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Inspector helpers and batch sync for coin/suitcase spawn poses.</summary>
public static class DutzCollectibleSpawnPoseSync
{
    public static void SyncFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sync Collectible Spawn Poses", "Exit Play mode first.", "OK");
            return;
        }

        var level1 = SyncScene(DutzLevel02Setup.Level01ScenePath, log: true);
        var level2 = SyncScene(DutzShowcaseSceneRepair.Level02ScenePath, log: true);

        if (!level1 || !level2)
        {
            EditorUtility.DisplayDialog(
                "Sync Collectible Spawn Poses",
                "One or more scenes failed. Check the Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Sync Collectible Spawn Poses",
            "Level 1 suitcases and Level 2 coins now have spawn positions saved in the Inspector.",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzCollectibleSpawnPoseSync.SyncBatch</summary>
    public static void SyncBatch() => SyncFromMenu();

    /// <summary>Batch: -executeMethod DutzCollectibleSpawnPoseSync.SyncLevel03HealthPotionsBatch</summary>
    public static void SyncLevel03HealthPotionsBatch() => SyncLevel03HealthPotions(log: true);

    public static bool SyncLevel03HealthPotions(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        var potionCount = SyncHealthPotionsFromCurrentTransforms();

        if (potionCount == 0)
        {
            if (log)
                Debug.LogWarning("[Dutz] No health potions found on Level 3.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Synced spawn pose from current transform for {potionCount} Level 3 health potion(s).");

        return potionCount > 0;
    }

    public static bool SyncOpenScene(bool log = false) =>
        SyncScene(EditorSceneManager.GetActiveScene().path, log);

    public static bool SyncScene(string scenePath, bool log)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            if (log)
                Debug.LogWarning("[Dutz] Sync collectible spawn poses: no scene path.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var coinCount = SyncCollectibles<DutzGoldCoin>(log);
        var suitcaseCount = SyncCollectibles<DutzSuitcase>(log);
        var potionCount = SyncHealthPotionsFromCurrentTransforms();

        if (coinCount == 0 && suitcaseCount == 0 && potionCount == 0)
        {
            if (log)
                Debug.LogWarning($"[Dutz] No collectibles found in {scenePath}.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Synced spawn poses in {scene.name}: {coinCount} coin(s), {suitcaseCount} suitcase(s), " +
                $"{potionCount} health potion(s).");
        }

        return true;
    }

    static int SyncHealthPotionsFromCurrentTransforms()
    {
        var count = 0;
        foreach (var potion in Object.FindObjectsOfType<DutzHealthPotion>(true))
        {
            if (potion == null || !DutzHealthPotion.IsTrackPotionRoot(potion.gameObject))
                continue;

            if (potion.SpawnPoseLocked)
                continue;

            Undo.RecordObject(potion, "Sync Health Potion Spawn Pose");
            potion.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(potion);
            count++;
        }

        return count;
    }

    static int SyncCollectibles<T>(bool log) where T : MonoBehaviour
    {
        var count = 0;
        foreach (var collectible in Object.FindObjectsOfType<T>(true))
        {
            if (collectible == null)
                continue;

            Undo.RecordObject(collectible, "Sync Collectible Spawn Pose");
            if (collectible is DutzGoldCoin coin)
            {
                coin.CaptureSpawnPoseFromTransform();
            }
            else if (collectible is DutzSuitcase suitcase)
            {
                suitcase.CaptureSpawnPoseFromTransform();
            }
            else if (collectible is DutzHealthPotion potion)
            {
                if (potion.SpawnPoseLocked)
                    continue;
                potion.CaptureSpawnPoseFromTransform(force: true);
            }

            EditorUtility.SetDirty(collectible);
            count++;
        }

        return count;
    }
}

[CustomEditor(typeof(DutzGoldCoin))]
[CanEditMultipleObjects]
public class DutzGoldCoinSpawnEditor : DutzCollectibleSpawnEditorBase { }

[CustomEditor(typeof(DutzSuitcase))]
[CanEditMultipleObjects]
public class DutzSuitcaseSpawnEditor : DutzCollectibleSpawnEditorBase { }

[CustomEditor(typeof(DutzHealthPotion))]
[CanEditMultipleObjects]
public class DutzHealthPotionSpawnEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "spawnPose", "spawnPoseLocked");

        var spawnPose = serializedObject.FindProperty("spawnPose");
        var spawnPoseLocked = serializedObject.FindProperty("spawnPoseLocked");
        if (spawnPose != null)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Spawn Pose", EditorStyles.boldLabel);
            if (spawnPoseLocked != null)
            {
                EditorGUILayout.PropertyField(
                    spawnPoseLocked,
                    new GUIContent("Lock Spawn Pose", "Blocks auto-sync from overwriting your authored spawn pose."));
            }

            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("position"), new GUIContent("Position"));
            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("eulerAngles"), new GUIContent("Rotation (Euler)"));
            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("localScale"), new GUIContent("Local Scale"));

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Read From Transform"))
                    ReadFromTransform();

                if (GUILayout.Button("Apply To Transform"))
                    ApplyToTransform();
            }

            EditorGUILayout.HelpBox(
                "Position the potion in the Scene, click Read From Transform, then enable Lock Spawn Pose. " +
                "Locked spawn poses are not changed by editor auto-sync.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ReadFromTransform()
    {
        foreach (var t in targets)
        {
            if (t is not DutzHealthPotion potion)
                continue;

            Undo.RecordObject(potion, "Read Health Potion Spawn Pose");
            potion.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(potion);
        }
    }

    void ApplyToTransform()
    {
        foreach (var t in targets)
        {
            if (t is not DutzHealthPotion potion)
                continue;

            Undo.RecordObject(potion.transform, "Apply Health Potion Spawn Pose");
            potion.ApplySpawnPose();
            EditorUtility.SetDirty(potion);
        }
    }
}

public abstract class DutzCollectibleSpawnEditorBase : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "spawnPose");

        var spawnPose = serializedObject.FindProperty("spawnPose");
        if (spawnPose != null)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Spawn Pose", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("position"), new GUIContent("Position"));
            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("eulerAngles"), new GUIContent("Rotation (Euler)"));
            EditorGUILayout.PropertyField(spawnPose.FindPropertyRelative("localScale"), new GUIContent("Local Scale"));

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Read From Transform"))
                    ReadFromTransform();

                if (GUILayout.Button("Apply To Transform"))
                    ApplyToTransform();
            }

            EditorGUILayout.HelpBox(
                "Edit Position Y here if a coin/suitcase is too high. Values are restored on every respawn.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void ReadFromTransform()
    {
        foreach (var t in targets)
        {
            if (t is not MonoBehaviour collectible)
                continue;

            Undo.RecordObject(collectible, "Read Collectible Spawn Pose");
            if (collectible is DutzGoldCoin coin)
                coin.CaptureSpawnPoseFromTransform();
            else if (collectible is DutzSuitcase suitcase)
                suitcase.CaptureSpawnPoseFromTransform();
            else if (collectible is DutzHealthPotion potion)
                potion.CaptureSpawnPoseFromTransform();

            EditorUtility.SetDirty(collectible);
        }
    }

    void ApplyToTransform()
    {
        foreach (var t in targets)
        {
            if (t is not MonoBehaviour collectible)
                continue;

            Undo.RecordObject(collectible.transform, "Apply Collectible Spawn Pose");
            if (collectible is DutzGoldCoin coin)
                coin.ApplySpawnPose();
            else if (collectible is DutzSuitcase suitcase)
                suitcase.ApplySpawnPose();
            else if (collectible is DutzHealthPotion potion)
                potion.ApplySpawnPose();

            EditorUtility.SetDirty(collectible);
        }
    }
}
