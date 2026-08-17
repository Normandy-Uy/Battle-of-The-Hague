using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Places Level02-style small addicts on Level07 Highway Straight 2 as fixed-spawn NPCs.
/// No DutzSegmentHippie teleport pool / slots / manager.
/// </summary>
public static class DutzLevel07SegmentHippieDuplicator
{
    public const string GroupRootName = "Level07_Straight2_Addicts";
    public const string AddictPrefix = "SimpleCitizens_Hippie_Extra_L07_";
    public const int AddictCount = 7;

    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string HippiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";
    const string Straight2Name = "Highway Straight 2";

    static readonly float[] PathLocalZ =
    {
        -0.28f, -0.18f, -0.08f, 0.02f, 0.12f, 0.22f, 0.32f
    };

    static readonly float[] LaneLocalX =
    {
        1.25f, 0f, -1.25f, -2.5f, -3.75f, -5f, -6.25f
    };

    [MenuItem("Assets/Dutz Authoring/Duplicate Level02 Addicts To Level07 Straight2")]
    public static void DuplicateAddictsToLevel07Straight2()
    {
        var scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);
        var hippiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (hippiePrefab == null)
            throw new System.InvalidOperationException("Missing hippie prefab: " + HippiePrefabPath);

        var straight2 = GameObject.Find(Straight2Name);
        if (straight2 == null)
            throw new System.InvalidOperationException($"'{Straight2Name}' not found in Level07.");

        RemoveLegacyTeleportPoolAndManager();
        RemoveExistingFixedGroup();

        var group = new GameObject(GroupRootName);
        Undo.RegisterCreatedObjectUndo(group, "Create Level07 Straight2 Addicts");

        var road = straight2.transform;
        var hintY = road.position.y;

        for (var i = 0; i < AddictCount; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(hippiePrefab);
            Undo.RegisterCreatedObjectUndo(go, "Create Level07 Addict");
            go.name = $"{AddictPrefix}{i + 1:00}";
            go.transform.SetParent(group.transform, true);
            SimpleCitizensHippieNpcSetup.SetupHippie(go);

            // Strip any teleport leftovers if SetupHippie/prefab ever adds them.
            StripTeleportComponents(go);

            var local = new Vector3(LaneLocalX[i], 0.5f, PathLocalZ[i]);
            var world = road.TransformPoint(local);
            if (DutzRoadGround.TrySampleRoadDeckForPlacement(world, hintY, go.GetComponent<Collider>(), out var deckY))
                world.y = deckY;
            else
                world.y = hintY + 1.5f;

            var forward = road.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.right;
            forward.Normalize();
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            go.transform.SetPositionAndRotation(world, rotation);

            // Fixed spawn (no segment teleport respawn path).
            var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = go.AddComponent<SimpleCitizensNpcRespawn>();
            respawn.SetLockedSpawnPoint(world, rotation);

            var hunter = go.GetComponent<SimpleCitizensHippieHunter>();
            if (hunter != null)
            {
                var hso = new SerializedObject(hunter);
                // Same as Level02 Straight 2: chase as soon as active on this segment.
                hso.FindProperty("huntImmediately").boolValue = true;
                hso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hunter);
            }

            EditorUtility.SetDirty(go);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[Dutz] Level07 Straight2 addicts created: {AddictCount} fixed-spawn hippies " +
            $"(no segment teleport). Group '{GroupRootName}'.");
    }

    [MenuItem("Assets/Dutz Authoring/Bake Level07 Addict Current Poses As Spawn")]
    public static void BakeCurrentPosesAsSpawn()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        var baked = 0;
        for (var i = 1; i <= AddictCount; i++)
        {
            var go = GameObject.Find($"{AddictPrefix}{i:00}");
            if (go == null)
                continue;

            var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = go.AddComponent<SimpleCitizensNpcRespawn>();

            respawn.SetLockedSpawnPoint(go.transform.position, go.transform.rotation);
            EditorUtility.SetDirty(respawn);
            EditorUtility.SetDirty(go);
            baked++;
            Debug.Log(
                $"[Dutz] Level07 addict spawn baked: {go.name} @ {go.transform.position} " +
                $"euler {go.transform.eulerAngles}");
        }

        if (baked == 0)
            throw new System.InvalidOperationException("No Level07 Straight2 addicts found to bake.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Dutz] Baked spawn for {baked} Level07 addicts from their current scene poses.");
    }

    [MenuItem("Assets/Dutz Authoring/Snap Level07 Addicts To Road")]
    public static void SnapAddictsToRoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Snap Level07 Addicts To Road requires Edit Mode — stop Play first.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        var straight2 = GameObject.Find(Straight2Name);
        if (straight2 == null)
            throw new System.InvalidOperationException($"'{Straight2Name}' not found in Level07.");

        Physics.SyncTransforms();

        var snapped = 0;
        for (var i = 1; i <= AddictCount; i++)
        {
            var go = GameObject.Find($"{AddictPrefix}{i:00}");
            if (go == null)
                continue;

            Undo.RecordObject(go.transform, "Snap Level07 Addict To Road");
            var before = go.transform.position;

            if (!TrySnapToStraight2Deck(go, straight2, out var after))
            {
                Debug.LogWarning($"[Dutz] Could not snap {go.name} to Straight 2 deck at {before}");
                continue;
            }

            // Flat upright facing along the highway (no pitched hover pose).
            var forward = straight2.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.right;
            forward.Normalize();
            go.transform.SetPositionAndRotation(after, Quaternion.LookRotation(forward, Vector3.up));

            var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn == null)
                respawn = go.AddComponent<SimpleCitizensNpcRespawn>();
            respawn.SetLockedSpawnPoint(go.transform.position, go.transform.rotation);

            EditorUtility.SetDirty(respawn);
            EditorUtility.SetDirty(go);
            snapped++;
            Debug.Log($"[Dutz] Snapped {go.name} to Straight 2 deck: {before} → {go.transform.position}");
        }

        if (snapped == 0)
            throw new System.InvalidOperationException("No Level07 Straight2 addicts were snapped.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Dutz] Snapped {snapped} Level07 addicts onto Straight 2 walkable deck and baked spawn.");
    }

    /// <summary>
    /// Places feet on the steep pitched Straight 2 top face (along road.up), not the
    /// tall world-AABB shell that vertical raycasts often hit.
    /// </summary>
    static bool TrySnapToStraight2Deck(GameObject addict, GameObject straight2, out Vector3 pivotWorld)
    {
        pivotWorld = addict.transform.position;
        if (!DutzRoadGround.TrySampleLevel07Straight2DeckPoint(
                addict.transform.position, out var deckPoint, out var deckUp))
            return false;

        Physics.SyncTransforms();
        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(addict);
        pivotWorld = deckPoint + deckUp * pivotToFeet;

        addict.transform.position = pivotWorld;
        if (addict.TryGetComponent<Rigidbody>(out var rb))
            rb.position = pivotWorld;

        return true;
    }

    public static bool NeedsApplyOrRepair()
    {
        // Convert leftover Level02-style teleport pool only.
        if (GameObject.Find(DutzSegmentHippieIdentity.PoolRootName) != null)
            return true;

        if (GameObject.Find(DutzSegmentHippieIdentity.ManagerObjectName) != null)
            return true;

        foreach (var t in Object.FindObjectsOfType<Transform>(true))
        {
            if (t != null && DutzSegmentHippieIdentity.IsPoolHippie(t.name))
                return true;
        }

        // First-time create if Straight 2 exists and fixed group is missing.
        if (GameObject.Find(Straight2Name) != null
            && GameObject.Find(GroupRootName) == null)
            return true;

        return false;
    }

    static void StripTeleportComponents(GameObject go)
    {
        if (go == null)
            return;

        var slots = go.GetComponent<DutzSegmentHippieTeleportSlots>();
        if (slots != null)
            Object.DestroyImmediate(slots);
    }

    static void RemoveLegacyTeleportPoolAndManager()
    {
        var pool = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (pool != null)
            Object.DestroyImmediate(pool);

        var manager = GameObject.Find(DutzSegmentHippieIdentity.ManagerObjectName);
        if (manager != null)
            Object.DestroyImmediate(manager);

        // Orphaned DutzSegmentHippie_* anywhere in Level07.
        foreach (var root in Object.FindObjectsOfType<Transform>(true))
        {
            if (root == null || !DutzSegmentHippieIdentity.IsPoolHippie(root.name))
                continue;
            if (root.parent != null && root.parent.name == DutzSegmentHippieIdentity.PoolRootName)
                continue;
            Object.DestroyImmediate(root.gameObject);
        }
    }

    static void RemoveExistingFixedGroup()
    {
        var group = GameObject.Find(GroupRootName);
        if (group != null)
            Object.DestroyImmediate(group);
    }
}
