using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mirrors Level07 Highway 8 crocs + suitcases onto Highway 7.
/// Highway 7 slope runs opposite Highway 8 (high end toward +X) — crocs face downslope (−X).
/// Does not touch Highway 8 content or Player1.
/// </summary>
public static class DutzLevel07Highway7ContentMirror
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway7Name = "Highway 7";
    const string CrocPrefabPath = "Assets/Characters/Level02/Prefabs/DutzCrocodileAddict.prefab";
    const string SuitcasePrefabPath = "Assets/Characters/Level02/Prefabs/DutzSuitcase.prefab";
    const string CrocsRootName = "Level07_Highway7_Crocs";
    const string CrocPrefix = "Level07_Highway7_Croc_";
    const string SuitcasesRootName = "Level07_Highway7_Suitcases";
    const string SuitcasePrefix = "Level07_Highway7_Suitcase_";

    const int CrocCount = 7;
    const int SuitcaseCount = 12;
    const float SuitcaseWorldScale = 8f;
    const float CrocWorldScale = 3f;
    const float LocalAxisInset = 0.08f;
    const float CrocMidSpreadFraction = 0.12f;
    static readonly Vector3 SuitcaseEuler = new Vector3(270f, 0f, 0f);

    [MenuItem("Assets/Dutz Authoring/Mirror Level07 Highway8 Crocs Suitcases Onto Highway7")]
    public static void MirrorFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Mirror Level07 Highway7 content requires Edit Mode — stop Play first.");
            return;
        }

        if (!MirrorSilent(log: true))
            Debug.LogError("[Dutz] Failed to mirror Highway 8 crocs/suitcases onto Highway 7.");
    }

    public static bool MirrorSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var highway7 = GameObject.Find(Highway7Name);
        if (highway7 == null)
        {
            Debug.LogError($"[Dutz] '{Highway7Name}' not found in Level07.");
            return false;
        }

        var road = highway7.transform;
        if (!TryGetLocalExtents(road, out var minX, out var maxX, out var localZ, out var localY))
        {
            Debug.LogError("[Dutz] Highway 7 has no usable MeshCollider bounds.");
            return false;
        }

        var downslope = ResolveDownslopeForward(road, minX, maxX, localZ, localY, out var highIsMaxX);
        var crocsOk = PlaceCrocs(road, minX, maxX, localZ, localY, downslope, log);
        var bagsOk = PlaceSuitcases(road, minX, maxX, localZ, localY, log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Highway 7 mirror done — high end localX={(highIsMaxX ? "max" : "min")}, " +
                $"downslope={downslope}, crocs={crocsOk}, suitcases={bagsOk}.");
        }

        return crocsOk && bagsOk;
    }

    /// <summary>
    /// Opposite of Highway 8: sample both ends and face from high → low.
    /// Highway 8 rises toward −X; Highway 7 rises toward +X.
    /// </summary>
    static Vector3 ResolveDownslopeForward(
        Transform road,
        float minX,
        float maxX,
        float localZ,
        float localY,
        out bool highIsMaxX)
    {
        highIsMaxX = true;

        var seedMin = road.TransformPoint(new Vector3(minX, localY, localZ));
        seedMin.y = 200f;
        var seedMax = road.TransformPoint(new Vector3(maxX, localY, localZ));
        seedMax.y = 200f;

        var yMin = seedMin.y;
        var yMax = seedMax.y;
        if (DutzRoadGround.TrySampleLevel07Highway7DeckPoint(seedMin, out var deckMin, out _))
            yMin = deckMin.y;
        if (DutzRoadGround.TrySampleLevel07Highway7DeckPoint(seedMax, out var deckMax, out _))
            yMax = deckMax.y;

        highIsMaxX = yMax >= yMin;

        // Local +X maps roughly to world −X on these highway pieces; use world X from deck samples.
        var high = highIsMaxX ? deckMax : deckMin;
        var low = highIsMaxX ? deckMin : deckMax;
        var down = low - high;
        down.y = 0f;
        if (down.sqrMagnitude < 0.0001f)
        {
            // Fallback: opposite of Highway 8 downslope (Highway 8 uses +road.right).
            down = -road.right;
            down.y = 0f;
        }

        if (down.sqrMagnitude < 0.0001f)
            down = Vector3.left;

        return down.normalized;
    }

    static bool PlaceCrocs(
        Transform road,
        float minX,
        float maxX,
        float localZ,
        float localY,
        Vector3 downslope,
        bool log)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrocPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing crocodile prefab: " + CrocPrefabPath);
            return false;
        }

        RemoveExistingByPrefix(CrocPrefix, CrocsRootName);
        var root = EnsureRoot(CrocsRootName, "Create Highway7 Crocs Root");
        var midX = (minX + maxX) * 0.5f;
        var halfSpan = (maxX - minX) * 0.5f * CrocMidSpreadFraction;
        var meshZ = road.GetComponent<MeshCollider>()?.sharedMesh?.bounds.size.z ?? 1f;

        var placed = 0;
        for (var i = 0; i < CrocCount; i++)
        {
            var t = CrocCount <= 1 ? 0.5f : i / (float)(CrocCount - 1);
            var localX = Mathf.Lerp(midX - halfSpan, midX + halfSpan, t);
            var staggerZ = localZ + ((i % 2 == 0) ? -0.08f : 0.08f) * meshZ;

            var seed = road.TransformPoint(new Vector3(localX, localY, staggerZ));
            seed.y = 200f;

            var pivot = seed;
            var pivotToFeet = 0.35f;
            if (!DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref pivot, pivotToFeet))
            {
                Debug.LogWarning($"[Dutz] Highway7 croc {i + 1} deck sample missed at localX={localX:F2}.");
                continue;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Highway7 Crocodile");
            go.name = $"{CrocPrefix}{i + 1:00}";
            go.transform.SetParent(root.transform, true);

            DutzCrocodileAddictBuilder.SetupCrocodileAddict(go, isPoolMember: false);
            DutzLevel07Highway8SpawnBaker.ApplyHighway8CrocGiantChase(go);

            // Match Highway 8 crocs (prefab default is 1; H8 instances are authored at 3).
            go.transform.localScale = Vector3.one * CrocWorldScale;

            var rotation = Quaternion.LookRotation(downslope, Vector3.up);
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
            Debug.Log($"[Dutz] Placed {placed}/{CrocCount} crocodile(s) mid {Highway7Name} (giant chase, opposite slope).");

        return placed == CrocCount;
    }

    static bool PlaceSuitcases(
        Transform road,
        float minX,
        float maxX,
        float localZ,
        float localY,
        bool log)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SuitcasePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing suitcase prefab: " + SuitcasePrefabPath);
            return false;
        }

        var jumpMax = GetMaxJumpHeightAboveDeck();
        var lift = Mathf.Min(DutzCollectibleTrackPlacer.HeightAboveDeckMeters, jumpMax);

        RemoveExistingByPrefix(SuitcasePrefix, SuitcasesRootName);
        var root = EnsureRoot(SuitcasesRootName, "Create Highway7 Suitcases Root");

        var placed = 0;
        for (var i = 0; i < SuitcaseCount; i++)
        {
            var t = SuitcaseCount <= 1 ? 0.5f : i / (float)(SuitcaseCount - 1);
            var localX = Mathf.Lerp(minX, maxX, t);
            var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
            seed.y = 200f;

            if (!DutzRoadGround.TrySampleLevel07Highway7DeckPoint(seed, out var deckPoint, out _))
            {
                Debug.LogWarning($"[Dutz] Highway7 suitcase {i + 1} deck sample missed at localX={localX:F2}.");
                continue;
            }

            var pos = deckPoint + Vector3.up * lift;
            var suitcase = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(suitcase, "Place Highway7 Suitcases");
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
                $"[Dutz] Placed {placed} suitcase(s) along {Highway7Name} " +
                $"(lift={lift:F1}m, separate from Highway 8 bags).");
        }

        return placed == SuitcaseCount;
    }

    static bool TryGetLocalExtents(
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

    static void RemoveExistingByPrefix(string prefix, string rootName)
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectByPrefix(root, prefix, toRemove);

        var namedRoot = GameObject.Find(rootName);
        if (namedRoot != null)
            toRemove.Add(namedRoot);

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
