using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes Highway 7 crocs spawn poses from their current Scene transforms
/// (sync Rigidbody + NpcRespawn inspector) and keeps giant chase enabled.
/// Does not move crocs — use after manually repositioning them.
/// </summary>
public static class DutzLevel07Highway7SpawnBaker
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string CrocPrefix = "Level07_Highway7_Croc_";

    [MenuItem("Assets/Dutz Authoring/Bake Level07 Highway7 Crocs Spawns")]
    public static void BakeFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Bake Level07 Highway7 Crocs Spawns requires Edit Mode.");
            return;
        }

        if (!BakeSilent(log: true))
            Debug.LogError("[Dutz] Failed to bake Highway 7 croc spawns.");
    }

    public static bool BakeSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var crocs = 0;
        foreach (var root in scene.GetRootGameObjects())
            crocs += BakeTreeByPrefix(root, CrocPrefix);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Baked spawn+chase for {crocs} Highway 7 crocodile(s) from current Scene poses.");

        return crocs > 0;
    }

    static int BakeTreeByPrefix(GameObject go, string prefix)
    {
        var count = 0;
        if (go.name.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            BakeCroc(go);
            count++;
        }

        foreach (Transform child in go.transform)
            count += BakeTreeByPrefix(child.gameObject, prefix);

        return count;
    }

    static void BakeCroc(GameObject go)
    {
        SyncRigidbody(go);

        // Same giant-chase tuning as Highway 8 crocs (flat XZ on sloping deck).
        DutzLevel07Highway8SpawnBaker.ApplyHighway8CrocGiantChase(go);

        EnableBehaviour<SimpleCitizensNpcPhysics>(go);
        EnableBehaviour<SimpleCitizensHippieBiter>(go);

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        DutzCrocodilePoolMember.RefreshCombatColliders(go);
        BakeSpawn(go);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        EditorUtility.SetDirty(go);
    }

    static void BakeSpawn(GameObject go)
    {
        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(go);

        Undo.RecordObject(respawn, "Bake Highway7 Croc Spawn");
        respawn.SetLockedSpawnPoint(go.transform.position, go.transform.rotation);
        EditorUtility.SetDirty(respawn);
    }

    static void SyncRigidbody(GameObject go)
    {
        if (!go.TryGetComponent<Rigidbody>(out var rb))
            return;

        Undo.RecordObject(rb, "Sync Highway7 Croc Rigidbody");
        rb.position = go.transform.position;
        rb.rotation = go.transform.rotation;
        rb.isKinematic = true;
        rb.useGravity = false;
        EditorUtility.SetDirty(rb);
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
