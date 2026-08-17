using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rebuilds Level07 Boy Idol by cloning another giant's body parts, keeps BoyIdolBossFace,
/// and wires chase + burn on Highway Straight 6.
/// </summary>
public static class DutzLevel07BoyIdolRestorer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string TrackGiantsName = "DutzLevel03TrackGiants";
    const string GiantName = "Boy Idol";
    const string HighwayName = "Highway Straight 6";
    const int HitPoints = 50;
    const float MidAlongFraction = 0.5f;

    static readonly string[] DonorNames =
    {
        "Gong Bong",
        "Cawetan",
        "STONE",
        "K Bilyar",
        "MARKO LEKTA"
    };

    [MenuItem("Assets/Dutz Authoring/Restore Boy Idol Body On Level07")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Restore Boy Idol Body On Level07 requires Edit Mode.");
            return;
        }

        if (!RestoreSilent(log: true))
            Debug.LogError("[Dutz] Failed to restore Boy Idol body on Level07.");
    }

    public static bool RestoreSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();

        var parent = GameObject.Find(TrackGiantsName);
        if (parent == null)
        {
            Debug.LogError($"[Dutz] '{TrackGiantsName}' not found.");
            return false;
        }

        var donor = FindDonor();
        if (donor == null)
        {
            Debug.LogError("[Dutz] No intact giant donor found to rebuild Boy Idol body.");
            return false;
        }

        // Remove broken shell (missing mesh / joints).
        foreach (var existing in Object.FindObjectsOfType<Transform>(true))
        {
            if (existing == null || !DutzGiantBossNames.IsBoyIdol(existing.name))
                continue;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var clone = Object.Instantiate(donor);
        Undo.RegisterCreatedObjectUndo(clone, "Restore Boy Idol");
        clone.name = GiantName;
        clone.transform.SetParent(parent.transform, true);
        clone.transform.localScale = new Vector3(4f, 3f, 4f);

        StripDonorIdentity(clone);
        EnsureCombat(clone);
        if (!StationOnHighwayStraight6(clone))
        {
            Debug.LogError("[Dutz] Could not station Boy Idol on Highway Straight 6.");
            return false;
        }

        if (!DutzLevel07BoyIdolFaceApplier.ApplySilent(log: false))
            Debug.LogWarning("[Dutz] Boy Idol body restored but face apply failed — photo assets may be missing.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = clone;

        if (log)
        {
            Debug.Log(
                $"[Dutz] Restored '{GiantName}' body from '{donor.name}' on {HighwayName} " +
                $"at {clone.transform.position} (chase + burn + Boy Idol photo).");
        }

        return true;
    }

    static GameObject FindDonor()
    {
        foreach (var name in DonorNames)
        {
            var go = GameObject.Find(name);
            if (go == null)
                continue;
            if (go.transform.childCount < 5)
                continue;
            if (go.GetComponent<Animator>() == null)
                continue;
            return go;
        }

        return null;
    }

    static void StripDonorIdentity(GameObject giant)
    {
        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        var grandma = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (grandma != null)
            Undo.DestroyObjectImmediate(grandma);

        // Drop donor face billboards; Boy Idol face applier rebuilds them.
        for (var i = giant.transform.childCount - 1; i >= 0; i--)
        {
            var child = giant.transform.GetChild(i);
            if (child != null && child.name.Contains("BossFace"))
                Undo.DestroyObjectImmediate(child.gameObject);
        }
    }

    static void EnsureCombat(GameObject giant)
    {
        if (giant.GetComponent<SimpleCitizensNpcPhysics>() == null)
            Undo.AddComponent<SimpleCitizensNpcPhysics>(giant);

        var hunter = giant.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null)
            hunter = Undo.AddComponent<SimpleCitizensGiantHippieHunter>(giant);
        hunter.enabled = true;

        var hunterSo = new SerializedObject(hunter);
        hunterSo.FindProperty("wakeDistance").floatValue = 200f;
        hunterSo.FindProperty("huntImmediately").boolValue = false;
        hunterSo.FindProperty("chaseSpeed").floatValue = 19f;
        hunterSo.FindProperty("chaseAnimSpeed").floatValue = 1f;
        hunterSo.FindProperty("chaseStopDistance").floatValue = 2.5f;
        hunterSo.ApplyModifiedPropertiesWithoutUndo();

        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        var hp = DutzNpcHitPoints.EnsureOn(giant, HitPoints);
        if (hp != null)
        {
            var hpSo = new SerializedObject(hp);
            var destroyProp = hpSo.FindProperty("destroyOnDeath");
            if (destroyProp != null)
                destroyProp.boolValue = false;
            hpSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hp);
        }

        var heat = DutzGiantHeat.EnsureOn(giant);
        if (heat != null)
        {
            heat.Configure(DutzGiantHeat.TrackBurnPerSecond);
            EditorUtility.SetDirty(heat);
        }

        if (giant.GetComponent<DutzGiantHippieBossFace>() == null)
            Undo.AddComponent<DutzGiantHippieBossFace>(giant);

        EditorUtility.SetDirty(hunter);
        EditorUtility.SetDirty(giant);
    }

    static bool StationOnHighwayStraight6(GameObject giant)
    {
        var highway = GameObject.Find(HighwayName);
        if (highway == null)
        {
            Debug.LogError($"[Dutz] '{HighwayName}' not found.");
            return false;
        }

        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);
        var path = DutzHighwayDeckSampler.BuildSegmentPath(highway, HighwayName, spawn, travelForward);
        if (path.Samples == null || path.Samples.Count == 0)
        {
            // Fallback: renderer bounds center on deck top.
            if (!highway.TryGetComponent<Renderer>(out var renderer))
                return false;

            var pivotToFeet = Mathf.Clamp(DutzNpcFeet.GetPivotToFeetOffset(giant), 0.05f, 8f);
            var deck = renderer.bounds.center;
            deck.y = renderer.bounds.max.y;
            var probe = deck + Vector3.up * 40f;
            if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, deck.y, null, out var deckY)
                || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
                deck.y = deckY;

            var pivot = deck + Vector3.up * pivotToFeet;
            var face = -travelForward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f)
                face = Vector3.right;
            face.Normalize();
            var rot = Quaternion.LookRotation(face, Vector3.up);
            ApplyPose(giant, pivot, rot);
            return true;
        }

        if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, MidAlongFraction, out var sample))
            return false;

        var feet = Mathf.Clamp(DutzNpcFeet.GetPivotToFeetOffset(giant), 0.05f, 8f);
        var deckPos = sample.Position;
        var highProbe = deckPos + Vector3.up * 40f;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(highProbe, deckPos.y, null, out var y)
            || DutzRoadGround.TrySampleSurfaceY(highProbe, null, out y))
            deckPos.y = y;

        var pivotPos = deckPos + Vector3.up * feet;
        var look = -travelForward;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f)
            look = -sample.Forward;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f)
            look = Vector3.right;
        look.Normalize();
        ApplyPose(giant, pivotPos, Quaternion.LookRotation(look, Vector3.up));
        return true;
    }

    static void ApplyPose(GameObject giant, Vector3 pivot, Quaternion rotation)
    {
        Undo.RecordObject(giant.transform, "Station Boy Idol");
        giant.transform.SetPositionAndRotation(pivot, rotation);
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station Boy Idol Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
            EditorUtility.SetDirty(rb);
        }

        var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(giant);
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(giant);
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
