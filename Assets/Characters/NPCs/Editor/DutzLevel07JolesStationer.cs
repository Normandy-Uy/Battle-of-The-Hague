using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations JOLES in the middle of Level07 Highway 8 — chase + HP 50, no burn.
/// </summary>
public static class DutzLevel07JolesStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway8Name = "Highway 8";
    const string JolesName = "JOLES";
    const int HitPoints = 50;

    [MenuItem("Assets/Dutz Authoring/Station JOLES On Level07 Highway8")]
    public static void StationJolesOnLevel07Highway8()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station JOLES On Level07 Highway8 requires Edit Mode — stop Play first.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station JOLES on Level07 Highway 8.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var joles = GameObject.Find(JolesName);
        if (joles == null)
            joles = DutzGiantBossNames.FindJoles();
        if (joles == null)
        {
            Debug.LogError($"[Dutz] '{JolesName}' not found in Level07.");
            return false;
        }

        var highway8 = GameObject.Find(Highway8Name);
        if (highway8 == null)
        {
            Debug.LogError($"[Dutz] '{Highway8Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(joles.transform, "Station JOLES On Highway 8");

        var stationary = joles.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        var road = highway8.transform;
        var mesh = road.GetComponent<MeshCollider>()?.sharedMesh;
        var localX = 0f;
        var localY = 0.5f;
        var localZ = 0f;
        if (mesh != null)
        {
            var b = mesh.bounds;
            localX = b.center.x;
            localY = b.center.y;
            localZ = b.center.z;
        }

        // Same walkable band as KIKAY when present — Highway 8 has stacked decks.
        var seedY = 8.42f;
        var kikay = GameObject.Find("KIKAY P");
        if (kikay != null)
            seedY = kikay.transform.position.y;

        var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
        seed.y = seedY;

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(joles);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);
        var pivot = seed;
        if (!DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pivot, pivotToFeet))
        {
            var deckUp = road.up.sqrMagnitude > 0.0001f ? road.up.normalized : Vector3.up;
            if (deckUp.y < 0f)
                deckUp = -deckUp;
            pivot = seed + deckUp * pivotToFeet;
            Debug.LogWarning("[Dutz] Highway 8 deck sample missed for JOLES — using mesh center seed.");
        }

        var forward = road.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();
        var rotation = Quaternion.LookRotation(forward, Vector3.up);

        joles.transform.SetPositionAndRotation(pivot, rotation);
        if (joles.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station JOLES Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Chase yes, burn no.
        EnableBehaviour<SimpleCitizensGiantHippieHunter>(joles);
        EnableBehaviour<SimpleCitizensNpcPhysics>(joles);
        DisableOrRemoveHeat(joles);

        var physics = joles.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
        }

        var hp = DutzNpcHitPoints.EnsureOn(joles, HitPoints);
        if (hp != null)
            EditorUtility.SetDirty(hp);

        var respawn = joles.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(joles);
        respawn.SetLockedSpawnPoint(joles.transform.position, joles.transform.rotation);

        EditorUtility.SetDirty(joles);
        if (respawn != null)
            EditorUtility.SetDirty(respawn);
        if (physics != null)
            EditorUtility.SetDirty(physics);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = joles;
        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {JolesName} mid {Highway8Name} at {joles.transform.position} " +
                $"(chase on, HP {HitPoints}, no burn, spawn baked).");
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

    static void DisableOrRemoveHeat(GameObject go)
    {
        var heat = go.GetComponent<DutzGiantHeat>();
        if (heat == null)
            return;

        Undo.RecordObject(heat, "Disable JOLES burn");
        heat.enabled = false;
        EditorUtility.SetDirty(heat);
    }
}
