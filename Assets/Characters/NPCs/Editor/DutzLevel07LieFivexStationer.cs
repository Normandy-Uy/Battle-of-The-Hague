using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Bakes Lie Fivex's authored pose on Level07 Highway Bridge 1 and restores chase/burn.
/// Does not move him — use after manually positioning in the Scene view.
/// </summary>
public static class DutzLevel07LieFivexStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Bridge1Name = "Highway Bridge 1";
    const string LieFivexName = "Lie Fivex";

    [MenuItem("Assets/Dutz Authoring/Bake Lie Fivex On Level07 Bridge1")]
    public static void BakeLieFivexOnLevel07Bridge1()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Bake Lie Fivex On Level07 Bridge1 requires Edit Mode — stop Play first.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to bake Lie Fivex on Level07 Highway Bridge 1.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var lieFivex = GameObject.Find(LieFivexName);
        if (lieFivex == null)
        {
            Debug.LogError($"[Dutz] '{LieFivexName}' not found in Level07.");
            return false;
        }

        var bridge1 = GameObject.Find(Bridge1Name);
        if (bridge1 == null)
        {
            Debug.LogError($"[Dutz] '{Bridge1Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(lieFivex.transform, "Bake Lie Fivex On Bridge 1");

        var stationary = lieFivex.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        // Keep the user's Scene pose — only sync rigidbody + bake spawn.
        var pivot = lieFivex.transform.position;
        var rotation = lieFivex.transform.rotation;
        if (lieFivex.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Bake Lie Fivex Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
            EditorUtility.SetDirty(rb);
        }

        EnableBehaviour<SimpleCitizensGiantHippieHunter>(lieFivex);
        EnableBehaviour<SimpleCitizensNpcPhysics>(lieFivex);
        EnableBehaviour<DutzGiantHeat>(lieFivex);

        var hunter = lieFivex.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            var hunterSo = new SerializedObject(hunter);
            hunterSo.FindProperty("wakeDistance").floatValue = 200f;
            hunterSo.FindProperty("huntImmediately").boolValue = false;
            hunterSo.FindProperty("chaseSpeed").floatValue =
                DutzCollectibleProgress.Level03TrackGiantChaseSpeed;
            hunterSo.FindProperty("chaseAnimSpeed").floatValue =
                DutzCollectibleProgress.GetLevel03TrackGiantChaseAnimSpeed();
            hunterSo.FindProperty("chaseStopDistance").floatValue = 2.5f;
            hunterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        var physics = lieFivex.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        var respawn = lieFivex.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(lieFivex);

        Undo.RecordObject(respawn, "Bake Lie Fivex Spawn");
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(lieFivex);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = lieFivex;
        if (log)
        {
            Debug.Log(
                $"[Dutz] Baked {LieFivexName} on {Bridge1Name} at {pivot} " +
                $"(pose preserved + chase/burn restored + spawn baked).");
        }

        return true;
    }

    static void EnableBehaviour<T>(GameObject go) where T : Behaviour
    {
        var behaviour = go.GetComponent<T>();
        if (behaviour == null)
            return;

        Undo.RecordObject(behaviour, "Enable " + typeof(T).Name);
        behaviour.enabled = true;
        EditorUtility.SetDirty(behaviour);
    }
}
