using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes Highway 8 JOLES + crocs spawn poses from their current Scene transforms,
/// and re-enables chase (does not move them).
/// </summary>
public static class DutzLevel07Highway8SpawnBaker
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string CrocPrefix = "Level07_Highway8_Croc_";
    const string JolesName = "JOLES";

    [MenuItem("Assets/Dutz Authoring/Bake Level07 Highway8 Joles And Crocs Spawns")]
    public static void BakeFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Bake Level07 Highway8 Joles And Crocs Spawns requires Edit Mode.");
            return;
        }

        if (!BakeSilent(log: true))
            Debug.LogError("[Dutz] Failed to bake Highway 8 JOLES/croc spawns.");
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

        var jolesOk = BakeJoles();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Baked spawn+chase for JOLES and {crocs} Highway 8 crocodile(s) from current Scene poses.");

        return jolesOk && crocs == 7;
    }

    static bool BakeJoles()
    {
        var joles = GameObject.Find(JolesName);
        if (joles == null)
            joles = DutzGiantBossNames.FindJoles();
        if (joles == null)
        {
            Debug.LogError("[Dutz] JOLES not found in Level07.");
            return false;
        }

        // Keep user pose — only sync rigidbody + bake spawn + chase.
        SyncRigidbody(joles);

        var stationary = joles.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        EnableBehaviour<SimpleCitizensGiantHippieHunter>(joles);
        EnableBehaviour<SimpleCitizensNpcPhysics>(joles);
        // No burn for JOLES.
        var heat = joles.GetComponent<DutzGiantHeat>();
        if (heat != null)
        {
            Undo.RecordObject(heat, "Disable JOLES burn");
            heat.enabled = false;
            EditorUtility.SetDirty(heat);
        }

        var physics = joles.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        BakeSpawn(joles);
        EditorUtility.SetDirty(joles);
        return true;
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

        // Highway 8 Level07 only — chase like a mid giant (not small-addict hunt).
        ApplyHighway8CrocGiantChase(go);

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
        EditorUtility.SetDirty(go);
    }

    /// <summary>
    /// Level07 Highway 8 crocs only: swap small HippieHunter for GiantHippieHunter
    /// (mid-giant speed ~19, wake ~280, flat XZ chase on sloping deck).
    /// </summary>
    public static void ApplyHighway8CrocGiantChase(GameObject go)
    {
        if (go == null)
            return;

        var small = go.GetComponent<SimpleCitizensHippieHunter>();
        if (small != null)
            Undo.DestroyObjectImmediate(small);

        var giant = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giant == null)
            giant = Undo.AddComponent<SimpleCitizensGiantHippieHunter>(go);

        Undo.RecordObject(giant, "Enable Highway8 Croc Giant Chase");
        giant.enabled = true;

        var so = new SerializedObject(giant);
        so.FindProperty("wakeDistance").floatValue = 280f;
        so.FindProperty("huntImmediately").boolValue = false;
        so.FindProperty("chaseSpeed").floatValue = 19f;
        so.FindProperty("chaseAnimSpeed").floatValue = 1f;
        so.FindProperty("chaseStopDistance").floatValue = 2.5f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(giant);
    }

    static void BakeSpawn(GameObject go)
    {
        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(go);

        Undo.RecordObject(respawn, "Bake Highway8 Spawn");
        respawn.SetLockedSpawnPoint(go.transform.position, go.transform.rotation);
        EditorUtility.SetDirty(respawn);
    }

    static void SyncRigidbody(GameObject go)
    {
        if (!go.TryGetComponent<Rigidbody>(out var rb))
            return;

        Undo.RecordObject(rb, "Sync Highway8 Rigidbody");
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
