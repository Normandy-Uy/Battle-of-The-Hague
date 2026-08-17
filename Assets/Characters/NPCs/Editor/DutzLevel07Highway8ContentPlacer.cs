using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level07 Highway 8: 7 chase crocs mid-road, suitcases along length (min-jump height),
/// Player1 + spawn near the start end.
/// </summary>
public static class DutzLevel07Highway8ContentPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway8Name = "Highway 8";
    const string CrocPrefabPath = "Assets/Characters/Level02/Prefabs/DutzCrocodileAddict.prefab";
    const string SuitcasePrefabPath = "Assets/Characters/Level02/Prefabs/DutzSuitcase.prefab";
    const string CrocsRootName = "Level07_Highway8_Crocs";
    const string CrocPrefix = "Level07_Highway8_Croc_";
    const string SuitcasesRootName = "Level07_Highway8_Suitcases";
    const string SuitcasePrefix = "Level07_Highway8_Suitcase_";
    const string PlayerName = "Player1";

    const int CrocCount = 7;
    const int SuitcaseCount = 12;
    const float SuitcaseWorldScale = 8f;
    const float LocalAxisInset = 0.08f;
    /// <summary>Spread crocs around mid-length (± fraction of usable local-X span).</summary>
    const float CrocMidSpreadFraction = 0.12f;
    static readonly Vector3 SuitcaseEuler = new Vector3(270f, 0f, 0f);

    [MenuItem("Assets/Dutz Authoring/Place Level07 Highway8 Crocs Suitcases Player")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Level07 Highway8 content requires Edit Mode — stop Play first.");
            return;
        }

        if (!PlaceSilent(log: true))
            Debug.LogError("[Dutz] Failed to place Level07 Highway 8 crocs/suitcases/player. Check Console.");
    }

    [MenuItem("Assets/Dutz Authoring/Restore Level07 Highway8 Suitcases Only")]
    public static void RestoreSuitcasesFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Restore Level07 Highway8 Suitcases Only requires Edit Mode — stop Play first.");
            return;
        }

        if (!RestoreSuitcasesSilent(log: true))
            Debug.LogError("[Dutz] Failed to restore Level07 Highway 8 suitcases. Check Console.");
    }

    public static bool PlaceSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var highway8 = GameObject.Find(Highway8Name);
        if (highway8 == null)
        {
            Debug.LogError($"[Dutz] '{Highway8Name}' not found in Level07.");
            return false;
        }

        var road = highway8.transform;
        if (!TryGetHighway8LocalExtents(road, out var minX, out var maxX, out var localZ, out var localY))
        {
            Debug.LogError("[Dutz] Highway 8 has no usable MeshCollider bounds.");
            return false;
        }

        var crocsOk = PlaceCrocs(road, minX, maxX, localZ, localY, log);
        var bagsOk = PlaceSuitcases(road, minX, maxX, localZ, localY, log);
        var playerOk = PlacePlayerNearStart(road, minX, maxX, localZ, localY, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return crocsOk && bagsOk && playerOk;
    }

    /// <summary>
    /// Restores Highway 8 suitcases without touching track bags (Straight2/3, Bridge4/5)
    /// or relocating Player1 / crocs.
    /// </summary>
    public static bool RestoreSuitcasesSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var highway8 = GameObject.Find(Highway8Name);
        if (highway8 == null)
        {
            Debug.LogError($"[Dutz] '{Highway8Name}' not found in Level07.");
            return false;
        }

        var road = highway8.transform;
        if (!TryGetHighway8LocalExtents(road, out var minX, out var maxX, out var localZ, out var localY))
        {
            Debug.LogError("[Dutz] Highway 8 has no usable MeshCollider bounds.");
            return false;
        }

        var bagsOk = PlaceSuitcases(road, minX, maxX, localZ, localY, log);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return bagsOk;
    }

    static bool PlaceCrocs(Transform road, float minX, float maxX, float localZ, float localY, bool log)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrocPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing crocodile prefab: " + CrocPrefabPath);
            return false;
        }

        RemoveExistingCrocs();
        var root = EnsureRoot(CrocsRootName, "Create Highway8 Crocs Root");
        var midX = (minX + maxX) * 0.5f;
        var halfSpan = (maxX - minX) * 0.5f * CrocMidSpreadFraction;

        var placed = 0;
        for (var i = 0; i < CrocCount; i++)
        {
            var t = CrocCount <= 1 ? 0.5f : i / (float)(CrocCount - 1);
            var localX = Mathf.Lerp(midX - halfSpan, midX + halfSpan, t);
            // Mild lateral stagger so they are not stacked.
            var staggerZ = localZ + ((i % 2 == 0) ? -0.08f : 0.08f) * (road.GetComponent<MeshCollider>()?.sharedMesh?.bounds.size.z ?? 1f);

            var seed = road.TransformPoint(new Vector3(localX, localY, staggerZ));
            // Top-of-slope sample — ignore buried Y hints.
            seed.y = 200f;

            var pivot = seed;
            var pivotToFeet = 0.35f;
            if (!DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pivot, pivotToFeet))
            {
                Debug.LogWarning($"[Dutz] Croc {i + 1} deck sample missed at localX={localX:F2}.");
                continue;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Highway8 Crocodile");
            go.name = $"{CrocPrefix}{i + 1:00}";
            go.transform.SetParent(root.transform, true);

            DutzCrocodileAddictBuilder.SetupCrocodileAddict(go, isPoolMember: false);
            DutzLevel07Highway8SpawnBaker.ApplyHighway8CrocGiantChase(go);

            // Travel downslope from high (−X) toward +X, matching Player1 facing.
            var rotation = Quaternion.LookRotation(GetDownslopeForward(road), Vector3.up);
            go.transform.SetPositionAndRotation(pivot, rotation);
            if (go.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.position = pivot;
                rb.rotation = rotation;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
            if (physics != null)
            {
                physics.Apply();
                physics.SetWalkingEnabled(true);
            }

            var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(go);
            respawn.SetLockedSpawnPoint(pivot, rotation);

            DutzCrocodilePoolMember.RefreshCombatColliders(go);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            placed++;
        }

        if (log)
            Debug.Log($"[Dutz] Placed {placed}/{CrocCount} crocodile(s) mid {Highway8Name} with chase retained.");

        return placed == CrocCount;
    }

    static bool PlaceSuitcases(Transform road, float minX, float maxX, float localZ, float localY, bool log)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuitcasePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing suitcase prefab: " + SuitcasePrefabPath);
            return false;
        }

        var jumpMax = GetMaxJumpHeightAboveDeck();
        var lift = Mathf.Min(DutzCollectibleTrackPlacer.HeightAboveDeckMeters, jumpMax);

        RemoveExistingSuitcases();
        var root = EnsureRoot(SuitcasesRootName, "Create Suitcases Root");

        var placed = 0;
        for (var i = 0; i < SuitcaseCount; i++)
        {
            var t = SuitcaseCount <= 1 ? 0.5f : i / (float)(SuitcaseCount - 1);
            var localX = Mathf.Lerp(minX, maxX, t);
            var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
            seed.y = 200f;

            if (!DutzRoadGround.TrySampleLevel07Highway8DeckPoint(seed, out var deckPoint, out _))
            {
                Debug.LogWarning($"[Dutz] Suitcase {i + 1} deck sample missed at localX={localX:F2}.");
                continue;
            }

            var pos = deckPoint + Vector3.up * lift;
            var suitcase = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(suitcase, "Place Highway8 Suitcases");
            suitcase.name = $"{SuitcasePrefix}{i + 1:00}";
            suitcase.transform.SetParent(root.transform, true);
            suitcase.transform.position = pos;
            suitcase.transform.rotation = Quaternion.Euler(SuitcaseEuler);
            suitcase.transform.localScale = Vector3.one * SuitcaseWorldScale;

            if (suitcase.GetComponent<DutzSuitcase>() == null)
                Undo.AddComponent<DutzSuitcase>(suitcase);

            DutzCollectibleTrackPlacer.WriteSpawnPose(suitcase.GetComponent<DutzSuitcase>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(suitcase);
            placed++;
        }

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed {placed} suitcase(s) along {Highway8Name} " +
                $"(evenly along length, lift={lift:F1}m, jumpMax={jumpMax:F1}m).");
        }

        return placed == SuitcaseCount;
    }

    static bool PlacePlayerNearStart(Transform road, float minX, float maxX, float localZ, float localY, bool log)
    {
        var player = GameObject.Find(PlayerName);
        if (player == null)
        {
            Debug.LogError($"[Dutz] '{PlayerName}' not found in Level07.");
            return false;
        }

        // Start = high local X end (world ~-1653), nearest the rest of the Level07 track.
        var startLocalX = Mathf.Lerp(minX, maxX, 0.92f);
        var seed = road.TransformPoint(new Vector3(startLocalX, localY, localZ));
        var joles = GameObject.Find("JOLES");
        if (joles != null)
            seed.y = joles.transform.position.y;

        var pivot = seed;
        var pivotToFeet = 1.0f;
        if (player.TryGetComponent<CharacterController>(out var cc))
            pivotToFeet = Mathf.Max(0.5f, cc.height * player.transform.lossyScale.y * 0.5f + cc.skinWidth);

        if (!DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pivot, pivotToFeet))
        {
            Debug.LogWarning("[Dutz] Player1 Highway 8 start deck sample missed — using TransformPoint seed.");
            pivot = seed;
        }

        var facing = GetIntoHighwayForward(road);
        var rotation = Quaternion.LookRotation(facing, Vector3.up);
        var euler = rotation.eulerAngles;

        Undo.RecordObject(player.transform, "Station Player1 On Highway 8");
        player.transform.SetPositionAndRotation(pivot, rotation);

        var controller = player.GetComponent<DutzPlayerController>();
        if (controller == null)
        {
            Debug.LogError("[Dutz] Player1 missing DutzPlayerController.");
            return false;
        }

        var so = new SerializedObject(controller);
        so.FindProperty("spawnPosition").vector3Value = pivot;
        so.FindProperty("useSpawnRotation").boolValue = true;
        so.FindProperty("spawnEulerAngles").vector3Value = euler;
        so.FindProperty("invertSpawnFacing").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(player);

        if (log)
            Debug.Log($"[Dutz] Stationed {PlayerName} near {Highway8Name} start at {pivot} (spawn baked, facing into highway).");

        return true;
    }

    /// <summary>Downslope travel on Highway 8: from high (−X) toward +X (matches Player1 facing).</summary>
    static Vector3 GetDownslopeForward(Transform road)
    {
        var into = road.right;
        into.y = 0f;
        if (into.sqrMagnitude < 0.0001f)
            into = Vector3.right;
        return into.normalized;
    }

    /// <summary>Highway 8 is elongated on local X (world −X). Into-track from start = +X.</summary>
    static Vector3 GetIntoHighwayForward(Transform road) => GetDownslopeForward(road);

    static bool TryGetHighway8LocalExtents(
        Transform road,
        out float minX,
        out float maxX,
        out float localZ,
        out float localY)
    {
        minX = maxX = localZ = localY = 0f;
        var col = road.GetComponent<MeshCollider>();
        var mesh = col != null ? col.sharedMesh : null;
        if (mesh == null)
            return false;

        var b = mesh.bounds;
        minX = Mathf.Lerp(b.min.x, b.max.x, LocalAxisInset);
        maxX = Mathf.Lerp(b.min.x, b.max.x, 1f - LocalAxisInset);
        if (minX > maxX)
        {
            minX = b.center.x;
            maxX = b.center.x;
        }

        localZ = b.center.z;
        localY = b.max.y;
        return true;
    }

    static float GetMaxJumpHeightAboveDeck()
    {
        var jumpForce = 14f;
        var gravity = -20f;
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            jumpForce = so.FindProperty("jumpForce").floatValue;
            gravity = so.FindProperty("gravity").floatValue;
            break;
        }

        var gravityMag = Mathf.Max(0.01f, Mathf.Abs(gravity));
        return jumpForce * jumpForce / (2f * gravityMag) - DutzCollectibleTrackPlacer.JumpHeightSafetyMargin;
    }

    static GameObject EnsureRoot(string name, string undoLabel)
    {
        var root = GameObject.Find(name);
        if (root != null)
            return root;

        root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, undoLabel);
        return root;
    }

    static void RemoveExistingCrocs()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByPrefix(root, CrocPrefix, toRemove);

        var crocsRoot = GameObject.Find(CrocsRootName);
        if (crocsRoot != null)
            toRemove.Add(crocsRoot);

        foreach (var go in toRemove.Distinct())
            Undo.DestroyObjectImmediate(go);
    }

    static void RemoveExistingSuitcases()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByPrefix(root, SuitcasePrefix, toRemove);

        var suitcasesRoot = GameObject.Find(SuitcasesRootName);
        if (suitcasesRoot != null)
            toRemove.Add(suitcasesRoot);

        foreach (var go in toRemove.Distinct())
            Undo.DestroyObjectImmediate(go);
    }

    static void CollectByPrefix(GameObject go, string prefix, List<GameObject> list)
    {
        if (go.name.StartsWith(prefix, System.StringComparison.Ordinal))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectByPrefix(child.gameObject, prefix, list);
    }
}
