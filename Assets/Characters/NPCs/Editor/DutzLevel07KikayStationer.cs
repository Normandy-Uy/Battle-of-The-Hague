using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Bakes KIKAY P's authored pose on Level07 Highway 8 and restores chase/burn.
/// Does not move him — use after manually positioning in the Scene view.
/// </summary>
public static class DutzLevel07KikayStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway8Name = "Highway 8";
    const string KikayName = "KIKAY P";

    [MenuItem("Assets/Dutz Authoring/Bake KIKAY P On Level07 Highway8")]
    public static void BakeKikayOnLevel07Highway8()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Bake KIKAY P On Level07 Highway8 requires Edit Mode — stop Play first.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to bake KIKAY P on Level07 Highway 8.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var kikay = GameObject.Find(KikayName);
        if (kikay == null)
        {
            Debug.LogError($"[Dutz] '{KikayName}' not found in Level07.");
            return false;
        }

        var highway8 = GameObject.Find(Highway8Name);
        if (highway8 == null)
        {
            Debug.LogError($"[Dutz] '{Highway8Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(kikay.transform, "Bake KIKAY P On Highway 8");

        var stationary = kikay.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        // Keep the user's Scene pose — only sync rigidbody + bake spawn.
        var pivot = kikay.transform.position;
        var rotation = kikay.transform.rotation;
        if (kikay.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Bake KIKAY P Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        EnableBehaviour<SimpleCitizensGiantHippieHunter>(kikay);
        EnableBehaviour<SimpleCitizensNpcPhysics>(kikay);
        EnableBehaviour<DutzGiantHeat>(kikay);

        var physics = kikay.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            // Do not SnapFeetToRoad — authored Highway 8 pose must stick.
        }

        var respawn = kikay.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(kikay);
        respawn.SetLockedSpawnPoint(pivot, rotation);

        EditorUtility.SetDirty(kikay);
        if (respawn != null)
            EditorUtility.SetDirty(respawn);
        if (physics != null)
            EditorUtility.SetDirty(physics);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = kikay;
        if (log)
        {
            Debug.Log(
                $"[Dutz] Baked {KikayName} on {Highway8Name} at {pivot} " +
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
