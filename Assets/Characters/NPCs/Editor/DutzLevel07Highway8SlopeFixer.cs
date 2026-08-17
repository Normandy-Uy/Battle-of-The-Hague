using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level07 Highway 8 sloping-deck fix: rebake Player1 spawn from scene pose,
/// lift suitcases above the top of the slope, and keep giants/crocs on the top surface.
/// </summary>
public static class DutzLevel07Highway8SlopeFixer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway8Name = "Highway 8";
    const string SuitcasePrefabPath = "Assets/Characters/Level02/Prefabs/DutzSuitcase.prefab";
    const string SuitcasesRootName = "DutzSuitcases";
    const string SuitcasePrefix = "DutzSuitcase_";
    const string CrocPrefix = "Level07_Highway8_Croc_";
    const string PlayerName = "Player1";

    const int SuitcaseCount = 12;
    const float SuitcaseWorldScale = 8f;
    const float LocalAxisInset = 0.08f;
    static readonly Vector3 SuitcaseEuler = new Vector3(270f, 0f, 0f);
    static readonly string[] Highway8Giants = { "JOLES", "KIKAY P" };

    [MenuItem("Assets/Dutz Authoring/Fix Level07 Highway8 Slope Deck")]
    public static void FixFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Fix Level07 Highway8 Slope Deck requires Edit Mode — stop Play first.");
            return;
        }

        if (!FixSilent(log: true))
            Debug.LogError("[Dutz] Failed to fix Level07 Highway 8 slope deck. Check Console.");
    }

    public static bool FixSilent(bool log)
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
            Debug.LogError($"[Dutz] '{Highway8Name}' not found.");
            return false;
        }

        var road = highway8.transform;
        if (!TryGetLocalExtents(road, out var minX, out var maxX, out var localZ, out var localY))
        {
            Debug.LogError("[Dutz] Highway 8 has no MeshCollider bounds.");
            return false;
        }

        var playerOk = BakePlayer1FromCurrentPose(log);
        var bagsOk = ReplaceSuitcasesOnTop(road, minX, maxX, localZ, localY, log);
        var crocsOk = SnapNamedPrefixToTop(CrocPrefix, bakeSpawn: true, log);
        var giantsOk = SnapGiantsToTop(log);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return playerOk && bagsOk && crocsOk && giantsOk;
    }

    static bool BakePlayer1FromCurrentPose(bool log)
    {
        var player = GameObject.Find(PlayerName);
        if (player == null)
        {
            Debug.LogError($"[Dutz] '{PlayerName}' not found.");
            return false;
        }

        var controller = player.GetComponent<DutzPlayerController>();
        if (controller == null)
        {
            Debug.LogError("[Dutz] Player1 missing DutzPlayerController.");
            return false;
        }

        var pivot = player.transform.position;
        var euler = player.transform.eulerAngles;

        Undo.RecordObject(controller, "Bake Player1 Highway8 Spawn");
        var so = new SerializedObject(controller);
        so.FindProperty("spawnPosition").vector3Value = pivot;
        so.FindProperty("useSpawnRotation").boolValue = true;
        so.FindProperty("spawnEulerAngles").vector3Value = euler;
        so.FindProperty("invertSpawnFacing").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(player);

        if (log)
            Debug.Log($"[Dutz] Baked {PlayerName} spawn to {pivot} euler={euler} (scene pose preserved).");

        return true;
    }

    static bool ReplaceSuitcasesOnTop(
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

        RemoveByPrefix(SuitcasePrefix, SuitcasesRootName);
        var root = EnsureRoot(SuitcasesRootName);

        var placed = 0;
        for (var i = 0; i < SuitcaseCount; i++)
        {
            var t = SuitcaseCount <= 1 ? 0.5f : i / (float)(SuitcaseCount - 1);
            // Match player travel: high end (−X) → low end (+X). Suitcase_01 near player start.
            var localX = Mathf.Lerp(minX, maxX, t);
            var seed = road.TransformPoint(new Vector3(localX, localY, localZ));
            // Seed Y does not matter — sampler always takes the highest Highway 8 top hit.
            seed.y = 200f;

            if (!DutzRoadGround.TrySampleLevel07Highway8DeckPoint(seed, out var deckPoint, out _))
            {
                Debug.LogWarning($"[Dutz] Suitcase {i + 1} missed slope top at localX={localX:F2}.");
                continue;
            }

            // World-up lift so min-jump reach matches player jump axis on the slope.
            var pos = deckPoint + Vector3.up * lift;
            var suitcase = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(suitcase, "Fix Highway8 Suitcases");
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
                $"[Dutz] Replaced {placed} suitcase(s) on sloping {Highway8Name} top " +
                $"(lift={lift:F1}m above deck, jumpMax={jumpMax:F1}m).");
        }

        return placed == SuitcaseCount;
    }

    static bool SnapNamedPrefixToTop(string prefix, bool bakeSpawn, bool log)
    {
        var snaps = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            snaps += SnapTreeByPrefix(root, prefix, bakeSpawn);

        if (log)
            Debug.Log($"[Dutz] Snapped {snaps} '{prefix}*' onto Highway 8 slope top.");

        return true;
    }

    static int SnapTreeByPrefix(GameObject go, string prefix, bool bakeSpawn)
    {
        var count = 0;
        if (go.name.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            if (SnapToTop(go, bakeSpawn))
                count++;
        }

        foreach (Transform child in go.transform)
            count += SnapTreeByPrefix(child.gameObject, prefix, bakeSpawn);

        return count;
    }

    static bool SnapGiantsToTop(bool log)
    {
        var ok = true;
        foreach (var name in Highway8Giants)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.LogWarning($"[Dutz] Giant '{name}' not found — skip.");
                continue;
            }

            if (!SnapToTop(go, bakeSpawn: true))
                ok = false;
            else if (log)
                Debug.Log($"[Dutz] Snapped {name} onto Highway 8 slope top at {go.transform.position}.");
        }

        return ok;
    }

    static bool SnapToTop(GameObject go, bool bakeSpawn)
    {
        if (go == null)
            return false;

        Undo.RecordObject(go.transform, "Snap To Highway8 Top");
        var pivot = go.transform.position;
        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(go);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);

        if (!DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pivot, pivotToFeet))
        {
            Debug.LogWarning($"[Dutz] Could not snap '{go.name}' onto Highway 8 top.");
            return false;
        }

        var rotation = go.transform.rotation;
        go.transform.SetPositionAndRotation(pivot, rotation);
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Snap Rigidbody Highway8");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (bakeSpawn)
        {
            var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(go);
            respawn.SetLockedSpawnPoint(pivot, rotation);
            EditorUtility.SetDirty(respawn);
        }

        EditorUtility.SetDirty(go);
        return true;
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

    static GameObject EnsureRoot(string name)
    {
        var root = GameObject.Find(name);
        if (root != null)
            return root;

        root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Create " + name);
        return root;
    }

    static void RemoveByPrefix(string prefix, string rootName)
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
