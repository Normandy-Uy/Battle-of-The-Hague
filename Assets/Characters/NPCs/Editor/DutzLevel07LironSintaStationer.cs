using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations Liron Sinta mid Level07 Highway Bridge 4 — chase + burn + HP 50, bakes spawn.
/// </summary>
public static class DutzLevel07LironSintaStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Bridge4Name = "Highway Bridge 4";
    const string GiantName = "Liron Sinta";
    const int HitPoints = 50;
    const float MidAlongFraction = 0.5f;

    [MenuItem("Assets/Dutz Authoring/Station Liron Sinta On Level07 Bridge4 Mid")]
    public static void StationFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station Liron Sinta On Level07 Bridge4 Mid requires Edit Mode.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station Liron Sinta on Level07 Highway Bridge 4 mid.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();

        var giant = GameObject.Find(GiantName);
        if (giant == null)
            giant = DutzGiantBossNames.FindLironSinta();
        if (giant == null)
        {
            Debug.LogError($"[Dutz] '{GiantName}' not found in Level07.");
            return false;
        }

        var bridge4 = GameObject.Find(Bridge4Name);
        if (bridge4 == null)
        {
            Debug.LogError($"[Dutz] '{Bridge4Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(giant.transform, "Station Liron Sinta On Bridge 4 Mid");

        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        var grandmaLock = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (grandmaLock != null)
            Undo.DestroyObjectImmediate(grandmaLock);

        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);
        var path = DutzHighwayDeckSampler.BuildSegmentPath(bridge4, Bridge4Name, spawn, travelForward);
        if (path.Samples == null || path.Samples.Count == 0)
        {
            Debug.LogError($"[Dutz] No deck samples for {Bridge4Name}.");
            return false;
        }

        if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, MidAlongFraction, out var sample))
        {
            Debug.LogError($"[Dutz] Could not sample mid of {Bridge4Name}.");
            return false;
        }

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(giant);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);

        var deck = sample.Position;
        var probe = deck;
        probe.y += 40f;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, deck.y, null, out var deckY)
            || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
            deck.y = deckY;

        var pivot = deck + Vector3.up * pivotToFeet;

        // Face incoming track traffic (toward earlier segments).
        var face = -travelForward;
        face.y = 0f;
        if (face.sqrMagnitude < 0.0001f)
            face = -sample.Forward;
        face.y = 0f;
        if (face.sqrMagnitude < 0.0001f)
            face = Vector3.left;
        face.Normalize();
        var rotation = Quaternion.LookRotation(face, Vector3.up);

        giant.transform.SetPositionAndRotation(pivot, rotation);
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station Liron Sinta Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
            EditorUtility.SetDirty(rb);
        }

        EnsureChaseHunter(giant);
        EnableBehaviour<SimpleCitizensNpcPhysics>(giant);
        EnsureBurn(giant);

        var hunter = giant.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            var hunterSo = new SerializedObject(hunter);
            hunterSo.FindProperty("wakeDistance").floatValue = 200f;
            hunterSo.FindProperty("huntImmediately").boolValue = false;
            hunterSo.FindProperty("chaseSpeed").floatValue = 19f;
            hunterSo.FindProperty("chaseAnimSpeed").floatValue = 1f;
            hunterSo.FindProperty("chaseStopDistance").floatValue = 2.5f;
            hunterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        var hp = DutzNpcHitPoints.EnsureOn(giant, HitPoints);
        if (hp != null)
            EditorUtility.SetDirty(hp);

        var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(giant);

        Undo.RecordObject(respawn, "Bake Liron Sinta Bridge4 Spawn");
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(giant);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = giant;

        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {GiantName} at mid of {Bridge4Name} at {pivot} " +
                $"(chase/burn on, HP {HitPoints}, spawn baked).");
        }

        return true;
    }

    static void EnsureChaseHunter(GameObject go)
    {
        var hunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null)
            hunter = Undo.AddComponent<SimpleCitizensGiantHippieHunter>(go);

        Undo.RecordObject(hunter, "Enable Liron Sinta chase");
        hunter.enabled = true;
        EditorUtility.SetDirty(hunter);
    }

    static void EnsureBurn(GameObject go)
    {
        var heat = go.GetComponent<DutzGiantHeat>();
        if (heat == null)
            heat = Undo.AddComponent<DutzGiantHeat>(go);

        Undo.RecordObject(heat, "Enable Liron Sinta burn");
        heat.Configure(DutzGiantHeat.TrackBurnPerSecond);
        heat.enabled = true;
        EditorUtility.SetDirty(heat);
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

    static Vector3 GetPlayerSpawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var position, out _))
            return position;

        return new Vector3(-1002f, 7.4f, -9.1f);
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.0001f)
            return progress.normalized;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        return forward.normalized;
    }
}
