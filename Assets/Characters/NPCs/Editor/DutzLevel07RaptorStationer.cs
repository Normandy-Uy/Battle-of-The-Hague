using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations RAPTOR on Level07 Highway Straight 2 — snaps to pitched deck and bakes spawn.
/// </summary>
public static class DutzLevel07RaptorStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Straight2Name = "Highway Straight 2";
    const string RaptorName = "RAPTOR";

    [MenuItem("Assets/Dutz Authoring/Station RAPTOR On Level07 Straight2")]
    public static void StationRaptorOnLevel07Straight2()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station RAPTOR On Level07 Straight2 requires Edit Mode — stop Play first.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station RAPTOR on Level07 Straight 2.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var raptor = GameObject.Find(RaptorName);
        if (raptor == null)
        {
            Debug.LogError($"[Dutz] '{RaptorName}' not found in Level07.");
            return false;
        }

        var straight2 = GameObject.Find(Straight2Name);
        if (straight2 == null)
        {
            Debug.LogError($"[Dutz] '{Straight2Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(raptor.transform, "Station RAPTOR On Straight2");

        // Strip Level07 freeze so RAPTOR can patrol/chase.
        var stationary = raptor.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        var road = straight2.transform;
        var mesh = road.GetComponent<MeshCollider>()?.sharedMesh;
        var localX = 0f;
        var localY = 0.5f;
        var localZ = 0f;
        if (mesh != null)
        {
            var b = mesh.bounds;
            localX = b.center.x;
            localY = b.max.y;
            localZ = b.center.z;
        }

        var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
        if (!DutzRoadGround.TrySampleLevel07Straight2DeckPoint(seed, out var deckPoint, out var deckUp))
        {
            Debug.LogError("[Dutz] Could not sample Straight 2 deck for RAPTOR.");
            return false;
        }

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(raptor);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);
        var pivot = deckPoint + deckUp * pivotToFeet;

        var forward = road.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        forward.Normalize();
        var rotation = Quaternion.LookRotation(forward, Vector3.up);

        raptor.transform.SetPositionAndRotation(pivot, rotation);
        if (raptor.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station RAPTOR Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Re-enable chase / footing / burn (stationary may have disabled them previously in Play).
        EnableBehaviour<SimpleCitizensGiantHippieHunter>(raptor);
        EnableBehaviour<SimpleCitizensNpcPhysics>(raptor);
        EnableBehaviour<DutzGiantHeat>(raptor);

        var physics = raptor.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            physics.SnapFeetToRoad();
        }

        var respawn = raptor.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(raptor);
        respawn.SetLockedSpawnPoint(raptor.transform.position, raptor.transform.rotation);

        EditorUtility.SetDirty(raptor);
        if (respawn != null)
            EditorUtility.SetDirty(respawn);
        if (physics != null)
            EditorUtility.SetDirty(physics);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = raptor;
        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {RaptorName} on {Straight2Name} at {raptor.transform.position} " +
                $"(Straight2 deck snap + baked spawn).");
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
