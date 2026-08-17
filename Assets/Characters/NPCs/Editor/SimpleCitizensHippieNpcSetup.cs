using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configures SimpleCitizens_Hippie_Black: gravity, ground snap, SC_Hippie mesh, walk forward.
/// </summary>
public static class SimpleCitizensHippieNpcSetup
{
    const string HippieObjectName = "SimpleCitizens_Hippie_Black";
    const string GiantHippieName = "SimpleCitizens_Hippie_Giant";
    const string MidGiantHippieName = "SimpleCitizens_Hippie_Giant_Mid";
    const string LastHighwayName = "Highway Straight 6";
    const string HippiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";
    const string HippieOutfit = "SC_Hippie";
    const int HippieRoadBlockCount = 4;
    const float HippieRoadSpacing = 5.5f;
    const float GiantHippieScale = 10f;
    const float GiantRoadEndInset = 25f;
    const float SmallHippieHeadScale = 2f;
    const float SmallHippieChaseSpeed = 7f;
    const float GiantHippieChaseSpeed = 19f;
    const float SmallHippieChaseAnimSpeed = 0.66f;
    const string SmallHippieDeathMessage = "An addict killed you!";
    const string ExtraHippiePrefix = "SimpleCitizens_Hippie_Extra_";
    const string NearSpawnHippiePrefix = "SimpleCitizens_Hippie_NearSpawn_";
    const string FlyingHippiePrefix = "SimpleCitizens_Hippie_Flying_";
    const int TargetSmallHippieCount = 30;
    const int TargetFlyingHippieCount = 10;
    const int OriginalSmallHippieCount = 4;
    const int MaxExtraHippies = TargetSmallHippieCount - OriginalSmallHippieCount;
    const float HighwayMarchEndX = 780f;
    const float DefaultPlayerSpawnX = -390f;
    const float FirstWaveOffsetFromSpawnX = 300f;
    const float OriginalBlockOffsetFromSpawnX = 220f;
    const float HighwayMarchLeftZ = 11f;
    const float HighwayMarchRightZ = -11f;
    const string ShowcaseScenePath = "Assets/Scenes/Dutz_Level02.unity";

    public static void EnsureFlyingHippiesFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Flying Hippies", "Exit Play mode first.", "OK");
            return;
        }

        ApplyFlyingHippiesToShowcase();
    }

    public static void ApplyAddictBiteTuningFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippie Bite Tuning", "Exit Play mode first.", "OK");
            return;
        }

        EnsureShowcaseSceneOpen();
        var count = ApplyAddictBiteTuningInActiveScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Dutz] Applied easier addict bite/hunt tuning to {count} hippie biter(s).");
    }

    public static void EnsureSmallHippiesFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippies", "Exit Play mode first.", "OK");
            return;
        }

        ApplySmallHippieCountToShowcase();
    }

    /// <summary>Batch: -executeMethod SimpleCitizensHippieNpcSetup.ApplySmallHippieCountToShowcase</summary>
    public static bool NeedsSmallHippieCountApply() => CollectExtraHippies().Count < MaxExtraHippies;

    public static void TryApplySmallHippieCountToShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return;

        var activePath = EditorSceneManager.GetActiveScene().path.Replace('\\', '/');
        if (!string.IsNullOrEmpty(activePath) && activePath != ShowcaseScenePath)
            return;

        EnsureShowcaseSceneOpen();
        if (ShowcaseUsesSegmentHippiePool() || !NeedsSmallHippieCountApply())
            return;

        ApplySmallHippieCountToShowcase();
    }

    public static void ApplySmallHippieCountToShowcase()
    {
        var scene = EnsureShowcaseSceneOpen();
        var spawned = EnsureMissingExtraHippies();
        var laidOut = LayoutExtraHippiesInHighwayColumns();
        FixAllHippiesOnRoad();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Dutz] Small hippies: spawned {spawned}, laid out {laidOut} extras (target {TargetSmallHippieCount}).");
    }

    public static bool NeedsFlyingHippieApply() => CollectFlyingHippies().Count < TargetFlyingHippieCount;

    public static void TryApplyFlyingHippiesToShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return;

        var activePath = EditorSceneManager.GetActiveScene().path.Replace('\\', '/');
        if (!string.IsNullOrEmpty(activePath) && activePath != ShowcaseScenePath)
            return;

        EnsureShowcaseSceneOpen();
        if (ShowcaseUsesSegmentHippiePool() || !NeedsFlyingHippieApply())
            return;

        ApplyFlyingHippiesToShowcase();
    }

    public static void ApplyFlyingHippiesToShowcase()
    {
        var scene = EnsureShowcaseSceneOpen();
        var spawned = EnsureMissingFlyingHippies();
        var laidOut = LayoutFlyingHippiesEvenlyOnTrack();
        FixAllFlyingHippiesInScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Dutz] Flying hippies: spawned {spawned}, laid out {laidOut} (target {TargetFlyingHippieCount}).");
    }

    public static void PlaceMidTrackGiantHippieFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Giant Hippie", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceMidTrackGiantHippie())
            return;

        DutzGiantHippieBossFaceBuilder.SyncMidBossPhotoFromPublic();
        DutzGiantHippieBossFaceBuilder.EnsureMidBossFaceMaterial();
        foreach (var giant in FindAllGiantHippies())
            DutzGiantHippieBossCaricatureBuilder.ApplyCaricatureToGiant(giant);

        Debug.Log("[Dutz] Mid-track giant hippie placed (10× scale, Torre boss photo).");
    }

    public static void PushBackStartHippiesFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippies", "Exit Play mode first.", "OK");
            return;
        }

        var extraCount = LayoutExtraHippiesInHighwayColumns();
        RepositionOriginalHippieBlock();
        FixAllHippiesOnRoad();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Dutz] Pushed back start hippies ({extraCount} extras + original block). First wave X={GetPlayerSpawnX() + FirstWaveOffsetFromSpawnX:F0}.");
    }

    public static void LayoutHippieHighwayColumnsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippies", "Exit Play mode first.", "OK");
            return;
        }

        var count = LayoutExtraHippiesInHighwayColumns();
        Debug.Log(count > 0
            ? $"[Dutz] Laid out {count} extra hippie(s) in left/right highway columns (march forward)."
            : "[Dutz] No extra hippies found.");
    }

    public static void FixSmallHippieChaseSpeedFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippies", "Exit Play mode first.", "OK");
            return;
        }

        var count = FixSmallHippieChaseSpeedsInScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log(count > 0
            ? $"[Dutz] Set small hippie chase speed to {SmallHippieChaseSpeed} m/s on {count} hunter(s)."
            : "[Dutz] No small hippie hunters found.");
    }

    public static void FixHippiesOnRoadFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hippies", "Exit Play mode first.", "OK");
            return;
        }

        var count = FixAllHippiesOnRoad();
        var chaseFixed = FixSmallHippieChaseSpeedsInScene();
        Debug.Log(count > 0
            ? $"[Dutz] Fixed {count} hippie(s) on road; {chaseFixed} hunter(s) at {SmallHippieChaseSpeed} m/s chase."
            : "[Dutz] No hippies found.");
    }

    public static int FixSmallHippieChaseSpeedsInScene() => FixAllHippieChaseSpeedsInScene();

    public static int FixAllHippieChaseSpeedsInScene()
    {
        var count = 0;
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensHippieHunter>(true))
        {
            if (!IsSmallHippieForChaseFix(hunter.gameObject))
                continue;

            ApplySmallHippieHunterSpeeds(hunter);
            count++;
        }

        foreach (var giant in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            ApplyGiantHippieHunterSpeeds(giant);
            count++;
        }

        return count;
    }

    static void ApplyGiantHippieHunterSpeeds(SimpleCitizensGiantHippieHunter hunter)
    {
        if (hunter == null)
            return;

        var hso = new SerializedObject(hunter);
        hso.FindProperty("chaseSpeed").floatValue = GiantHippieChaseSpeed;
        hso.FindProperty("chaseAnimSpeed").floatValue = 1f;
        hso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hunter);

        var physics = hunter.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            return;

        var pso = new SerializedObject(physics);
        pso.FindProperty("walkSpeed").floatValue = GiantHippieChaseSpeed;
        pso.FindProperty("animatorWalkSpeed").floatValue = 1f;
        pso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(physics);
    }

    static bool IsSmallHippieForChaseFix(GameObject go)
    {
        if (IsGiantHippie(go))
            return false;

        return IsHippieObject(go)
            || go.name.StartsWith(ExtraHippiePrefix, System.StringComparison.Ordinal)
            || go.name.StartsWith(NearSpawnHippiePrefix, System.StringComparison.Ordinal)
            || go.name.StartsWith(FlyingHippiePrefix, System.StringComparison.Ordinal);
    }

    static void ApplySmallHippieHunterSpeeds(SimpleCitizensHippieHunter hunter)
    {
        if (hunter == null)
            return;

        var hso = new SerializedObject(hunter);
        hso.FindProperty("chaseSpeed").floatValue = SmallHippieChaseSpeed;
        hso.FindProperty("chaseAnimSpeed").floatValue = SmallHippieChaseAnimSpeed;
        hso.FindProperty("huntImmediately").boolValue =
            hunter.gameObject.name.StartsWith("DutzSegmentHippie_", System.StringComparison.Ordinal);
        hso.FindProperty("wakeDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieWakeDistance;
        hso.FindProperty("maxHuntDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieMaxHuntDistance;
        hso.FindProperty("playerAheadAbandonDistance").floatValue = 8f;
        hso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hunter);

        var physics = hunter.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            return;

        var pso = new SerializedObject(physics);
        pso.FindProperty("walkSpeed").floatValue = SmallHippieChaseSpeed;
        pso.FindProperty("animatorWalkSpeed").floatValue = SmallHippieChaseAnimSpeed;
        pso.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(physics);
    }

    public static bool PlaceFourHippiesAcrossRoad()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return false;
        }

        var blockCenter = GetRoadBlockCenter();
        var rotation = GetHippieFacingPlayerRotation();

        RemoveAllSceneHippies();

        var half = (HippieRoadBlockCount - 1) * 0.5f;
        for (var i = 0; i < HippieRoadBlockCount; i++)
        {
            var zOffset = (i - half) * HippieRoadSpacing;
            var position = new Vector3(blockCenter.x, blockCenter.y, blockCenter.z + zOffset);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Place Hippie Road Block");
            go.name = i == 0 ? HippieObjectName : $"{HippieObjectName}_{i + 1}";
            go.transform.SetPositionAndRotation(position, rotation);
            SetupHippie(go);
        }

        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Dutz] Placed {HippieRoadBlockCount} hippies on road at {blockCenter}.");
        return true;
    }

    public static int EnsureMissingExtraHippies()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return 0;
        }

        var existingNumbers = new HashSet<int>();
        foreach (var extra in CollectExtraHippies())
        {
            var suffix = extra.name.Substring(ExtraHippiePrefix.Length);
            if (int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                existingNumbers.Add(number);
        }

        var spawned = 0;
        for (var i = 1; i <= MaxExtraHippies; i++)
        {
            if (existingNumbers.Contains(i))
                continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Extra Hippie");
            go.name = $"{ExtraHippiePrefix}{i:00}";
            SetupHippie(go);
            spawned++;
        }

        if (spawned > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        return spawned;
    }

    public static int EnsureMissingFlyingHippies()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return 0;
        }

        var existingNumbers = new HashSet<int>();
        foreach (var flying in CollectFlyingHippies())
        {
            var suffix = flying.name.Substring(FlyingHippiePrefix.Length);
            if (int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                existingNumbers.Add(number);
        }

        var spawned = 0;
        for (var i = 1; i <= TargetFlyingHippieCount; i++)
        {
            if (existingNumbers.Contains(i))
                continue;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Spawn Flying Hippie");
            go.name = $"{FlyingHippiePrefix}{i:00}";
            SetupFlyingHippie(go);
            spawned++;
        }

        return spawned;
    }

    public static int LayoutFlyingHippiesEvenlyOnTrack()
    {
        TrimExcessFlyingHippies(TargetFlyingHippieCount);

        var flying = CollectFlyingHippies();
        if (flying.Count == 0)
            return 0;

        flying.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        var supermanRotation = SimpleCitizensFlyingHippie.SupermanRotation(Vector3.right);
        var marchStartX = GetPlayerSpawnX() + FirstWaveOffsetFromSpawnX;
        var leftCount = (flying.Count + 1) / 2;
        var rightCount = flying.Count - leftCount;

        for (var i = 0; i < flying.Count; i++)
        {
            var go = flying[i];
            var onLeft = i < leftCount;
            var indexInColumn = onLeft ? i : i - leftCount;
            var countInColumn = onLeft ? leftCount : rightCount;
            var t = countInColumn <= 1 ? 0.5f : indexInColumn / (float)(countInColumn - 1);
            var x = Mathf.Lerp(marchStartX, HighwayMarchEndX, t);
            var z = onLeft ? HighwayMarchLeftZ : HighwayMarchRightZ;
            var y = SimpleCitizensFlyingHippie.GetPatrolWorldY(new Vector3(x, 0f, z));

            go.transform.SetPositionAndRotation(new Vector3(x, y, z), supermanRotation);
            SetupFlyingHippie(go);
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return flying.Count;
    }

    static void TrimExcessFlyingHippies(int maxCount)
    {
        var flying = CollectFlyingHippies();
        if (flying.Count <= maxCount)
            return;

        flying.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        for (var i = flying.Count - 1; i >= maxCount; i--)
            Undo.DestroyObjectImmediate(flying[i]);
    }

    static List<GameObject> CollectFlyingHippies()
    {
        var list = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectFlyingHippiesInHierarchy(root, list);

        return list;
    }

    static void CollectFlyingHippiesInHierarchy(GameObject go, List<GameObject> list)
    {
        if (go.name.StartsWith(FlyingHippiePrefix, System.StringComparison.Ordinal))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectFlyingHippiesInHierarchy(child.gameObject, list);
    }

    public static int FixAllFlyingHippiesInScene()
    {
        var count = 0;
        foreach (var go in CollectFlyingHippies())
        {
            SetupFlyingHippie(go);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        return count;
    }

    public static int LayoutExtraHippiesInHighwayColumns()
    {
        TrimExcessExtraHippies(MaxExtraHippies);

        var extras = CollectExtraHippies();
        if (extras.Count == 0)
        {
            RepositionOriginalHippieBlock();
            return 0;
        }

        extras.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        var marchRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        var leftCount = (extras.Count + 1) / 2;
        var rightCount = extras.Count - leftCount;

        for (var i = 0; i < extras.Count; i++)
        {
            var go = extras[i];
            var onLeft = i < leftCount;
            var indexInColumn = onLeft ? i : i - leftCount;
            var countInColumn = onLeft ? leftCount : rightCount;
            var t = countInColumn <= 1 ? 0.5f : indexInColumn / (float)(countInColumn - 1);
            var marchStartX = GetPlayerSpawnX() + FirstWaveOffsetFromSpawnX;
            var x = Mathf.Lerp(marchStartX, HighwayMarchEndX, t);
            var z = onLeft ? HighwayMarchLeftZ : HighwayMarchRightZ;

            go.transform.SetPositionAndRotation(new Vector3(x, go.transform.position.y, z), marchRotation);
            ConfigureExtraHippieForMarch(go);
        }

        RepositionOriginalHippieBlock();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        return extras.Count;
    }

    static void RepositionOriginalHippieBlock()
    {
        var blockCenter = GetRoadBlockCenter();
        var rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        var half = (HippieRoadBlockCount - 1) * 0.5f;

        for (var i = 0; i < HippieRoadBlockCount; i++)
        {
            var name = i == 0 ? HippieObjectName : $"{HippieObjectName}_{i + 1}";
            var go = GameObject.Find(name);
            if (go == null || go.transform.parent != null && IsHippieObject(go.transform.parent.gameObject))
                continue;

            var zOffset = (i - half) * HippieRoadSpacing;
            var position = new Vector3(blockCenter.x, blockCenter.y, blockCenter.z + zOffset);
            go.transform.SetPositionAndRotation(position, rotation);
            SetupHippie(go);
        }
    }

    static Scene EnsureShowcaseSceneOpen()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/').EndsWith("Dutz_Level02.unity"))
            return scene;

        return EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
    }

    static float GetPlayerSpawnX()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var player in root.GetComponentsInChildren<DutzPlayerController>(true))
            {
                var so = new SerializedObject(player);
                return so.FindProperty("spawnPosition").vector3Value.x;
            }
        }

        return DefaultPlayerSpawnX;
    }

    static List<GameObject> CollectExtraHippies()
    {
        var list = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectExtraHippiesInHierarchy(root, list);

        return list;
    }

    static void CollectExtraHippiesInHierarchy(GameObject go, List<GameObject> list)
    {
        if (go.name.StartsWith(ExtraHippiePrefix, System.StringComparison.Ordinal))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectExtraHippiesInHierarchy(child.gameObject, list);
    }

    static void TrimExcessExtraHippies(int maxCount)
    {
        var extras = CollectExtraHippies();
        if (extras.Count <= maxCount)
            return;

        extras.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        for (var i = extras.Count - 1; i >= maxCount; i--)
            Undo.DestroyObjectImmediate(extras[i]);
    }

    static void ConfigureExtraHippieForMarch(GameObject go)
    {
        // Same as original small hippies: hunt player, bite, respawn — only column placement differs.
        SetupHippie(go);
    }

    public static int FixAllHippiesOnRoad()
    {
        var count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += FixHippiesInHierarchy(root);

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        return count;
    }

    static int FixHippiesInHierarchy(GameObject go)
    {
        var count = 0;
        if (IsHippieOrGiant(go))
        {
            SetupHippie(go);
            count++;
        }
        else if (go.name.StartsWith(ExtraHippiePrefix, System.StringComparison.Ordinal))
        {
            SetupHippie(go);
            count++;
        }
        else if (go.name.StartsWith(NearSpawnHippiePrefix, System.StringComparison.Ordinal))
        {
            SetupHippie(go);
            count++;
        }
        else if (go.name.StartsWith(FlyingHippiePrefix, System.StringComparison.Ordinal))
        {
            SetupFlyingHippie(go);
            count++;
        }
        else if (go.name.StartsWith(SegmentHippiePrefix, System.StringComparison.Ordinal))
        {
            SetupHippie(go);
            count++;
        }

        foreach (Transform child in go.transform)
            count += FixHippiesInHierarchy(child.gameObject);

        return count;
    }

    static Vector3 GetRoadBlockCenter()
    {
        var spawnX = GetPlayerSpawnX();
        var center = new Vector3(spawnX + OriginalBlockOffsetFromSpawnX, 18f, 1f);

        if (DutzRoadGround.TrySampleSurfaceY(center, null, out var roadY))
            center.y = roadY;

        return center;
    }

    static Quaternion GetHippieFacingPlayerRotation()
    {
        var player = Object.FindObjectOfType<DutzPlayerController>();
        if (player == null)
            return Quaternion.LookRotation(Vector3.left);

        var flatForward = player.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.01f)
            return Quaternion.LookRotation(Vector3.left);

        return Quaternion.LookRotation(-flatForward.normalized, Vector3.up);
    }

    public static bool PlaceMidTrackGiantHippie()
    {
        var template = GameObject.Find(GiantHippieName);
        if (template == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Dutz] Missing hippie prefab and scene giant for mid-track copy.");
                return false;
            }

            if (!PlaceGiantHippieAtRoadEnd())
                return false;

            template = GameObject.Find(GiantHippieName);
            if (template == null)
                return false;
        }

        RemoveMidGiantHippie();

        if (!TryGetTrackMiddlePosition(out var position, out var rotation))
        {
            Debug.LogError("[Dutz] Could not find track middle for mid giant.");
            return false;
        }

        var go = Object.Instantiate(template);
        Undo.RegisterCreatedObjectUndo(go, "Place Mid-Track Giant Hippie");
        go.name = MidGiantHippieName;
        go.transform.SetPositionAndRotation(position, rotation);
        SetupGiantHippie(go);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Dutz] Mid-track giant hippie at {position} ({GiantHippieScale}× scale, Torre boss).");
        return true;
    }

    public static bool PlaceGiantHippieAtRoadEnd()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + HippiePrefabPath);
            return false;
        }

        var highway = FindLastHighway();
        if (highway == null)
        {
            Debug.LogError("[Dutz] Could not find last highway segment in scene.");
            return false;
        }

        var template = FindSceneHippie();
        var rotation = template != null
            ? template.transform.rotation
            : Quaternion.Euler(25.11f, -89.779f, -17.223f);

        RemoveGiantHippie();

        if (!TryGetRoadEndPosition(highway, out var position))
        {
            Debug.LogError("[Dutz] Could not find ground at highway end.");
            return false;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(go, "Place Giant Hippie");
        go.name = GiantHippieName;
        go.transform.localScale = Vector3.one * GiantHippieScale;
        go.transform.SetPositionAndRotation(position, rotation);
        SetupHippie(go);

        if (go.GetComponent<Rigidbody>() is { } rb)
            rb.mass = 500f;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Dutz] Giant hippie at road end: {position} (10× scale).");
        return true;
    }

    public static bool SetupHippieBlack()
    {
        var go = FindSceneHippie();
        if (go == null)
            return false;

        SetupHippie(go);
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Dutz] Setup complete for SimpleCitizens_Hippie_Black.");
        return true;
    }

    public static int SnapAllHippiesToGround()
    {
        var count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            count += SnapHippiesInHierarchy(root);

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        return count;
    }

    static int SnapHippiesInHierarchy(GameObject go)
    {
        var count = 0;
        if (IsHippieOrGiant(go))
        {
            SetupHippie(go);
            count++;
        }

        foreach (Transform child in go.transform)
            count += SnapHippiesInHierarchy(child.gameObject);

        return count;
    }

    static bool IsGiantHippie(GameObject go) =>
        go.name == GiantHippieName || go.name == MidGiantHippieName;

    static bool IsHippieOrGiant(GameObject go) =>
        IsHippieObject(go) || IsGiantHippie(go);

    public static void SetupHippie(GameObject go)
    {
        if (go == null)
            return;

        DutzSimpleCitizensSetup.EnableOutfitOnly(go, HippieOutfit);
        FitBoxColliderToActiveMeshes(go);

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = go.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();

        var physicsSettings = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physicsSettings != null)
        {
            var so = new SerializedObject(physicsSettings);
            so.FindProperty("walkForward").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (go.GetComponent<SimpleCitizensHippieSounds>() == null)
            go.AddComponent<SimpleCitizensHippieSounds>();

        var sounds = go.GetComponent<SimpleCitizensHippieSounds>();
        if (sounds != null)
            EditorUtility.SetDirty(sounds);

        if (!IsGiantHippie(go) && go.GetComponent<SimpleCitizensHippieBiter>() == null)
            go.AddComponent<SimpleCitizensHippieBiter>();

        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = go.AddComponent<SimpleCitizensNpcRespawn>();

        EnsureBiteTrigger(go);
        AlignUprightOnRoad(go);
        if (IsGiantHippie(go))
        {
            SetupGiantHippie(go);
        }
        else
        {
            SetupSmallHippie(go);
        }
        SnapHippieToRoadInEditor(go);

        if (go.name.StartsWith("DutzSegmentHippie_", System.StringComparison.Ordinal))
            ClearPoolHippieRespawnSpawnPoint(respawn);
        else
            respawn.RecordSpawnPoint();

        EditorUtility.SetDirty(go);
    }

    static void ClearPoolHippieRespawnSpawnPoint(SimpleCitizensNpcRespawn respawn)
    {
        if (respawn == null)
            return;

        var so = new SerializedObject(respawn);
        so.FindProperty("spawnPointSet").boolValue = false;
        so.FindProperty("spawnPosition").vector3Value = Vector3.zero;
        so.FindProperty("spawnRotation").quaternionValue = Quaternion.identity;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(respawn);
    }

    public static void SetupFlyingHippie(GameObject go)
    {
        if (go == null)
            return;

        DutzSimpleCitizensSetup.EnableOutfitOnly(go, HippieOutfit);
        FitBoxColliderToActiveMeshes(go);

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = go.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();

        var physicsSo = new SerializedObject(physics);
        physicsSo.FindProperty("followGround").boolValue = false;
        physicsSo.FindProperty("chaseIn3D").boolValue = true;
        physicsSo.FindProperty("supermanFlight").boolValue = true;
        physicsSo.FindProperty("walkForward").boolValue = true;
        physicsSo.FindProperty("lockForwardToHighway").boolValue = true;
        physicsSo.FindProperty("walkSpeed").floatValue = SimpleCitizensFlyingHippie.PatrolCruiseSpeed;
        physicsSo.FindProperty("animatorWalkSpeed").floatValue = SimpleCitizensFlyingHippie.PatrolAnimSpeed;
        physicsSo.FindProperty("snapToGroundOnStart").boolValue = false;
        physicsSo.FindProperty("groundCheckDistance").floatValue = 0.6f;
        physicsSo.ApplyModifiedPropertiesWithoutUndo();

        if (go.GetComponent<SimpleCitizensFlyingHippie>() == null)
            go.AddComponent<SimpleCitizensFlyingHippie>();

        if (go.GetComponent<SimpleCitizensHippieSounds>() == null)
            go.AddComponent<SimpleCitizensHippieSounds>();

        if (go.GetComponent<SimpleCitizensHippieBiter>() == null)
            go.AddComponent<SimpleCitizensHippieBiter>();

        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = go.AddComponent<SimpleCitizensNpcRespawn>();

        EnsureBiteTrigger(go);
        DutzSmallAddictScale.Apply(go);
        ApplyHeadScale(go, SmallHippieHeadScale);

        var animator = go.GetComponent<Animator>();
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        var groundHunter = go.GetComponent<SimpleCitizensHippieHunter>();
        if (groundHunter != null)
            Undo.DestroyObjectImmediate(groundHunter);

        var flyingHunter = go.GetComponent<SimpleCitizensFlyingHippieHunter>();
        if (flyingHunter == null)
            flyingHunter = go.AddComponent<SimpleCitizensFlyingHippieHunter>();

        var hunterSo = new SerializedObject(flyingHunter);
        hunterSo.FindProperty("chaseSpeed").floatValue = SmallHippieChaseSpeed;
        hunterSo.FindProperty("chaseAnimSpeed").floatValue = SmallHippieChaseAnimSpeed;
        hunterSo.FindProperty("wakeDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieWakeDistance;
        hunterSo.FindProperty("maxHuntDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieMaxHuntDistance;
        hunterSo.FindProperty("playerAheadAbandonDistance").floatValue = 8f;
        hunterSo.ApplyModifiedPropertiesWithoutUndo();

        var giantHunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            Undo.DestroyObjectImmediate(giantHunter);

        ApplySmallHippieDeathMessage(go);

        var xz = new Vector3(go.transform.position.x, 0f, go.transform.position.z);
        var patrolY = SimpleCitizensFlyingHippie.GetPatrolWorldY(xz);
        go.transform.SetPositionAndRotation(
            new Vector3(xz.x, patrolY, xz.z),
            SimpleCitizensFlyingHippie.SupermanRotation(Vector3.right));

        respawn.RecordSpawnPoint();
        EditorUtility.SetDirty(go);
    }

    static void SetupSmallHippie(GameObject go)
    {
        DutzSmallAddictScale.Apply(go);
        ApplyHeadScale(go, SmallHippieHeadScale);

        var animator = go.GetComponent<Animator>();
        if (animator != null)
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            var so = new SerializedObject(physics);
            so.FindProperty("walkForward").boolValue = false;
            so.FindProperty("lockForwardToHighway").boolValue = false;
            so.FindProperty("walkSpeed").floatValue = SmallHippieChaseSpeed;
            so.FindProperty("animatorWalkSpeed").floatValue = SmallHippieChaseAnimSpeed;
            so.FindProperty("groundCheckDistance").floatValue = 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var hunter = go.GetComponent<SimpleCitizensHippieHunter>();
        if (hunter == null)
            hunter = go.AddComponent<SimpleCitizensHippieHunter>();

        ApplySmallHippieHunterSpeeds(hunter);

        var giantHunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            Undo.DestroyObjectImmediate(giantHunter);

        ApplySmallHippieDeathMessage(go);
    }

    static int ApplyAddictBiteTuningInActiveScene()
    {
        var count = 0;
        foreach (var biter in Object.FindObjectsOfType<SimpleCitizensHippieBiter>(true))
        {
            if (biter == null || IsGiantHippie(biter.gameObject))
                continue;

            ApplyAddictBiteTuningToBiter(biter, SmallHippieDeathMessage);
            DutzHippieBiteCollider.EnsureSmallHippieColliders(biter.gameObject);
            count++;
        }

        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensHippieHunter>(true))
            ApplySmallHippieHunterSpeeds(hunter);

        foreach (var flying in Object.FindObjectsOfType<SimpleCitizensFlyingHippieHunter>(true))
            ApplyFlyingHippieHunterTuning(flying);

        return count;
    }

    static void ApplyAddictBiteTuningToBiter(SimpleCitizensHippieBiter biter, string deathMessage)
    {
        if (biter == null)
            return;

        var so = new SerializedObject(biter);
        so.FindProperty("deathMessage").stringValue = deathMessage;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(biter);
    }

    static void ApplyFlyingHippieHunterTuning(SimpleCitizensFlyingHippieHunter hunter)
    {
        if (hunter == null)
            return;

        var so = new SerializedObject(hunter);
        so.FindProperty("wakeDistance").floatValue = 70f;
        so.FindProperty("maxHuntDistance").floatValue = 52f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(hunter);
    }

    static void ApplySmallHippieDeathMessage(GameObject go)
    {
        if (go == null || IsGiantHippie(go))
            return;

        if (!IsHippieObject(go)
            && !go.name.StartsWith(ExtraHippiePrefix, System.StringComparison.Ordinal)
            && !go.name.StartsWith(NearSpawnHippiePrefix, System.StringComparison.Ordinal)
            && !go.name.StartsWith(FlyingHippiePrefix, System.StringComparison.Ordinal))
            return;

        var biter = go.GetComponent<SimpleCitizensHippieBiter>();
        if (biter == null)
            return;

        ApplyAddictBiteTuningToBiter(biter, SmallHippieDeathMessage);

        if (!IsGiantHippie(go))
        {
            FitBoxColliderToActiveMeshes(go);
            EnsureBiteTrigger(go);
        }
    }

    static void SetupGiantHippie(GameObject go)
    {
        ApplyGiantHippieScale(go);

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            var so = new SerializedObject(physics);
            so.FindProperty("walkForward").boolValue = false;
            so.FindProperty("walkSpeed").floatValue = GiantHippieChaseSpeed;
            so.FindProperty("animatorWalkSpeed").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var hunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter == null)
            hunter = go.AddComponent<SimpleCitizensGiantHippieHunter>();

        ApplyGiantHippieHunterSpeeds(hunter);

        var biter = go.GetComponent<SimpleCitizensHippieBiter>();
        if (biter != null)
            Undo.DestroyObjectImmediate(biter);

        var bossFace = go.GetComponent<DutzGiantHippieBossFace>();
        if (bossFace == null)
            bossFace = go.AddComponent<DutzGiantHippieBossFace>();

        DutzGiantHippieBossCaricatureBuilder.ApplyCaricatureToGiant(go);
        bossFace.ApplyFace();
        SnapHippieToRoadInEditor(go);
    }

    static void ApplyGiantHippieScale(GameObject go)
    {
        if (!IsGiantHippie(go))
            return;

        go.transform.localScale = Vector3.one * GiantHippieScale;

        if (go.GetComponent<Rigidbody>() is { } rb)
            rb.mass = 500f;
    }

    static void ApplyHeadScale(GameObject root, float scale)
    {
        foreach (var bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (bone.name != "Head_jnt")
                continue;

            bone.localScale = Vector3.one * scale;
            return;
        }
    }

    static void SnapHippieToRoadInEditor(GameObject go)
    {
        Undo.RecordObject(go.transform, "Snap Hippie To Road");

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SnapFeetToRoad();
            return;
        }

        var col = GetSolidCollider(go);
        if (col == null)
            return;

        Physics.SyncTransforms();
        if (!DutzRoadGround.TrySampleWalkSurface(go.transform.position, col, out var surfaceY))
        {
            Debug.LogWarning("[Dutz] No road surface under " + go.name);
            return;
        }

        DutzNpcFeet.PlacePivotOnSurface(go, surfaceY);
    }

    static Collider GetSolidCollider(GameObject go)
    {
        foreach (var col in go.GetComponents<Collider>())
        {
            if (col != null && !col.isTrigger)
                return col;
        }

        return null;
    }

    static void AlignUprightOnRoad(GameObject go)
    {
        var forward = go.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.right;

        go.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    static GameObject FindSceneHippie()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (IsHippieObject(root))
                return root;

            foreach (var hippie in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsHippieObject(hippie.gameObject))
                    return hippie.gameObject;
            }
        }

        return null;
    }

    static bool IsHippieObject(GameObject go)
    {
        if (IsGiantHippie(go))
            return false;

        return go.name == HippieObjectName || go.name.StartsWith(HippieObjectName + "_");
    }

    static void RemoveGiantHippie()
    {
        var existing = GameObject.Find(GiantHippieName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static void RemoveMidGiantHippie()
    {
        var existing = GameObject.Find(MidGiantHippieName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static List<GameObject> FindAllGiantHippies()
    {
        var list = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectGiantHippiesInHierarchy(root, list);

        return list;
    }

    static void CollectGiantHippiesInHierarchy(GameObject go, List<GameObject> list)
    {
        if (IsGiantHippie(go))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectGiantHippiesInHierarchy(child.gameObject, list);
    }

    static bool TryGetTrackMiddlePosition(out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);

        var template = GameObject.Find(GiantHippieName);
        if (template != null)
            rotation = template.transform.rotation;

        var startX = GetPlayerSpawnX();
        var endHighway = FindLastHighway();
        if (endHighway == null || !TryGetWorldBounds(endHighway, out var endBounds))
        {
            var midX = startX + (HighwayMarchEndX - startX) * 0.5f;
            var sample = new Vector3(midX, 18f, 1f);
            if (DutzRoadGround.TrySampleSurfaceY(sample, null, out var roadY))
                position = new Vector3(midX, roadY, 1f);
            else
                position = sample;

            return true;
        }

        var trackMidX = (startX + endBounds.max.x) * 0.5f;
        var centerZ = endBounds.center.z;
        var groundSample = new Vector3(trackMidX, endBounds.max.y, centerZ);

        if (DutzRoadGround.TrySampleSurfaceY(groundSample, null, out var surfaceY))
            position = new Vector3(trackMidX, surfaceY, centerZ);
        else
            position = groundSample;

        return true;
    }

    static GameObject FindLastHighway()
    {
        var named = GameObject.Find(LastHighwayName);
        if (named != null)
            return named;

        GameObject best = null;
        var bestX = float.NegativeInfinity;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.Contains("Highway"))
                    continue;

                if (t.position.x > bestX)
                {
                    bestX = t.position.x;
                    best = t.gameObject;
                }
            }
        }

        return best;
    }

    static bool TryGetRoadEndPosition(GameObject highway, out Vector3 position)
    {
        position = default;
        if (!TryGetWorldBounds(highway, out var bounds))
            return false;

        var endX = bounds.max.x - GiantRoadEndInset;
        var centerZ = bounds.center.z;
        var sample = new Vector3(endX, bounds.max.y, centerZ);

        if (DutzRoadGround.TrySampleSurfaceY(sample, null, out var surfaceY))
            position = new Vector3(endX, surfaceY, centerZ);
        else
            position = sample;

        return true;
    }

    static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        var hasBounds = false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    static void RemoveAllSceneHippies()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectHippies(root, toRemove);

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);
    }

    static void CollectHippies(GameObject go, List<GameObject> list)
    {
        if (IsHippieObject(go))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectHippies(child.gameObject, list);
    }

    static void EnsureBiteTrigger(GameObject root)
    {
        if (IsGiantHippie(root))
            return;

        DutzHippieBiteCollider.EnsureSmallHippieColliders(root);
        foreach (var col in root.GetComponents<BoxCollider>())
        {
            if (col != null)
                EditorUtility.SetDirty(col);
        }
    }

    static void FitBoxColliderToActiveMeshes(GameObject root)
    {
        BoxCollider box = null;
        foreach (var col in root.GetComponents<BoxCollider>())
        {
            if (col != null && !col.isTrigger)
            {
                box = col;
                break;
            }
        }

        if (box == null)
            box = root.AddComponent<BoxCollider>();

        DutzHippieBiteCollider.ApplyHumanoidSolidCollider(box);
        EditorUtility.SetDirty(box);
    }

    const string SegmentPoolRootName = "DutzSegmentHippiePool";
    const string SegmentManagerName = "DutzSegmentHippieManager";
    const string SegmentHippiePrefix = "DutzSegmentHippie_";
    const int SegmentPoolCount = 7;

    /// <summary>Batch: -executeMethod SimpleCitizensHippieNpcSetup.ApplySegmentHippiePoolToShowcase</summary>
    public static void ApplySegmentHippiePoolToShowcase() => ApplySegmentHippiePoolToShowcase(log: true);

    public static bool TryApplySegmentHippiePoolToShowcase()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return false;

        var activePath = EditorSceneManager.GetActiveScene().path.Replace('\\', '/');
        if (!string.IsNullOrEmpty(activePath) && activePath != ShowcaseScenePath)
            return false;

        if (!ShowcaseNeedsSegmentHippiePoolApply())
            return false;

        if (!ShowcaseUsesSegmentHippiePool() || CountBakedSmallHippiesForSegmentPool() > 0)
            ApplySegmentHippiePoolToShowcase(log: false);
        else
            EnsureSegmentHippieTeleportProfile();

        DutzEarlyHighwayContentPlacer.RemoveNearSpawnAddictsFromShowcase();
        DutzEarlyHighwayContentPlacer.RemoveNearSpawnCoinsFromShowcase();
        return true;
    }

    public static bool ShowcaseUsesSegmentHippiePool()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == SegmentPoolRootName)
                return true;
        }

        return false;
    }

    public static bool ShowcaseNeedsSegmentHippiePoolApply()
    {
        EnsureShowcaseSceneOpen();
        return !ShowcaseUsesSegmentHippiePool()
               || CountBakedSmallHippiesForSegmentPool() > 0
               || !ShowcaseHasTeleportProfile()
               || !ShowcaseHasHippieTeleportSlots();
    }

    public static bool ShowcaseHasTeleportProfile()
    {
        var pool = GameObject.Find(SegmentPoolRootName);
        if (pool == null)
            return false;

        var profile = pool.GetComponent<DutzSegmentHippieTeleportProfile>();
        return profile != null && profile.HasValidData;
    }

    public static bool ShowcaseHasHippieTeleportSlots()
    {
        var pool = GameObject.Find(SegmentPoolRootName);
        if (pool == null)
            return false;

        for (var i = 1; i <= SegmentPoolCount; i++)
        {
            var child = pool.transform.Find($"{SegmentHippiePrefix}{i:00}");
            if (child == null || child.GetComponent<DutzSegmentHippieTeleportSlots>() == null)
                return false;
        }

        return true;
    }

    public static void EnsureSegmentHippieTeleportProfile()
    {
        var pool = GameObject.Find(SegmentPoolRootName);
        if (pool == null)
            return;

        var profile = pool.GetComponent<DutzSegmentHippieTeleportProfile>();
        if (profile == null)
            profile = pool.AddComponent<DutzSegmentHippieTeleportProfile>();

        if (!profile.HasValidData)
            profile.ApplyAuthoredDefaults();

        profile.CopyToHippieSlots(pool.transform);
        profile.PlaceHippiesAtSegmentOne(pool.transform);

        foreach (Transform child in pool.transform)
        {
            if (!child.name.StartsWith(SegmentHippiePrefix, System.StringComparison.Ordinal))
                continue;

            var slots = child.GetComponent<DutzSegmentHippieTeleportSlots>();
            if (slots == null)
                slots = child.gameObject.AddComponent<DutzSegmentHippieTeleportSlots>();

            var hippieIndex = 0;
            if (child.name.Length >= SegmentHippiePrefix.Length + 2
                && int.TryParse(child.name.Substring(SegmentHippiePrefix.Length), out var parsed))
                hippieIndex = Mathf.Clamp(parsed - 1, 0, DutzSegmentHippieTeleportProfile.HippieCount - 1);

            if (profile.HasValidData)
                slots.CopyFromProfileEntry(profile.GetEntry(hippieIndex));
            else
                slots.ApplyAuthoredDefaults(hippieIndex);

            var hunter = child.GetComponent<SimpleCitizensHippieHunter>();
            if (hunter != null)
                ApplySmallHippieHunterSpeeds(hunter);

            ClearPoolHippieRespawnSpawnPoint(child.GetComponent<SimpleCitizensNpcRespawn>());
            EditorUtility.SetDirty(slots);
        }

        EditorUtility.SetDirty(profile);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static int CountBakedSmallHippiesForSegmentPool()
    {
        var count = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CountBakedSmallHippiesForSegmentPoolInHierarchy(root, ref count);

        return count;
    }

    static void CountBakedSmallHippiesForSegmentPoolInHierarchy(GameObject go, ref int count)
    {
        if (ShouldRemoveSmallHippieForSegmentPool(go.name))
        {
            count++;
            return;
        }

        foreach (Transform child in go.transform)
            CountBakedSmallHippiesForSegmentPoolInHierarchy(child.gameObject, ref count);
    }

    public static bool ApplySegmentHippiePoolToShowcase(bool log)
    {
        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var hippiePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (hippiePrefab == null)
        {
            Debug.LogError("[Dutz] Missing hippie prefab: " + HippiePrefabPath);
            return false;
        }

        var removed = RemoveBakedSmallHippiesForSegmentPool();
        RemoveSegmentPoolAndManager();

        var poolRoot = new GameObject(SegmentPoolRootName);
        Undo.RegisterCreatedObjectUndo(poolRoot, "Create Segment Hippie Pool");

        for (var i = 0; i < SegmentPoolCount; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(hippiePrefab);
            Undo.RegisterCreatedObjectUndo(go, "Create Segment Hippie");
            go.name = $"{SegmentHippiePrefix}{i + 1:00}";
            go.transform.SetParent(poolRoot.transform, true);
            SetupHippie(go);
        }

        var teleportProfile = poolRoot.GetComponent<DutzSegmentHippieTeleportProfile>();
        if (teleportProfile == null)
            teleportProfile = Undo.AddComponent<DutzSegmentHippieTeleportProfile>(poolRoot);

        teleportProfile.ApplyIndividualAuthoredPositions();
        teleportProfile.CopyToHippieSlots(poolRoot.transform);
        teleportProfile.PlaceHippiesAtSegmentOne(poolRoot.transform);

        foreach (Transform child in poolRoot.transform)
        {
            if (!child.name.StartsWith(SegmentHippiePrefix, System.StringComparison.Ordinal))
                continue;

            var slots = child.GetComponent<DutzSegmentHippieTeleportSlots>();
            if (slots == null)
                slots = Undo.AddComponent<DutzSegmentHippieTeleportSlots>(child.gameObject);

            var hunter = child.GetComponent<SimpleCitizensHippieHunter>();
            if (hunter != null)
                ApplySmallHippieHunterSpeeds(hunter);

            ClearPoolHippieRespawnSpawnPoint(child.GetComponent<SimpleCitizensNpcRespawn>());
            EditorUtility.SetDirty(slots);
        }

        EditorUtility.SetDirty(teleportProfile);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Segment hippie pool applied: removed {removed} baked small hippies, " +
                $"added {SegmentPoolCount} pooled hippies (manager spawns at runtime).");
        }

        return true;
    }

    public static int RemoveBakedSmallHippiesFromActiveScene() => RemoveBakedSmallHippiesForSegmentPool();

    static int RemoveBakedSmallHippiesForSegmentPool()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectRemovableSmallHippiesForSegmentPool(root, toRemove);

        var destroyed = new HashSet<GameObject>();
        foreach (var go in toRemove)
        {
            if (go == null || !destroyed.Add(go))
                continue;

            Object.DestroyImmediate(go);
        }

        return destroyed.Count;
    }

    static void CollectRemovableSmallHippiesForSegmentPool(GameObject go, List<GameObject> list)
    {
        if (ShouldRemoveSmallHippieForSegmentPool(go.name))
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            if (root == null)
                root = go;

            if (!list.Contains(root))
                list.Add(root);

            return;
        }

        foreach (Transform child in go.transform)
            CollectRemovableSmallHippiesForSegmentPool(child.gameObject, list);
    }

    static bool ShouldRemoveSmallHippieForSegmentPool(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return false;

        if (objectName == GiantHippieName
            || objectName == MidGiantHippieName
            || objectName == "SimpleCitizens_Grandma_White")
            return false;

        if (objectName.StartsWith(SegmentHippiePrefix, System.StringComparison.Ordinal))
            return false;

        return objectName.StartsWith(HippieObjectName, System.StringComparison.Ordinal)
               || objectName.StartsWith(ExtraHippiePrefix, System.StringComparison.Ordinal)
               || objectName.StartsWith(NearSpawnHippiePrefix, System.StringComparison.Ordinal)
               || objectName.StartsWith(FlyingHippiePrefix, System.StringComparison.Ordinal);
    }

    static void RemoveSegmentPoolAndManager()
    {
        var pool = GameObject.Find(SegmentPoolRootName);
        if (pool != null)
            Undo.DestroyObjectImmediate(pool);

        var manager = GameObject.Find(SegmentManagerName);
        if (manager != null)
            Undo.DestroyObjectImmediate(manager);
    }

    // SegmentManagerName kept for cleanup of any legacy baked manager object.

    const string ColorfulTexturePath = "Assets/Characters/NPCs/Textures/SimpleCitizens_Hippie_Colorful.png";
    const string ColorfulMaterialPath = "Assets/Characters/NPCs/Resources/SimpleCitizens_Hippie_Colorful.mat";
    const string AngryFaceTexturePath = "Assets/Characters/NPCs/Textures/SmallAddictAngryFace.png";
    const string HippieMaterialPath = "Assets/SimpleCitizens/Materials/SimpleCitizens_Hippie_Black.mat";
    const int ColorfulAtlasSize = 512;
    const int ColorfulTileSize = 64;
    static readonly Color BrightOrangeHead = new Color(1f, 0.5f, 0f, 1f);
    static readonly Color BrightRedBody = new Color(1f, 0.05f, 0.05f, 1f);

    public static void ApplySmallAddictScaleFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Small Addict Scale", "Exit Play mode first.", "OK");
            return;
        }

        var count = ApplySmallAddictScaleInActiveScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Dutz] Applied {DutzSmallAddictScale.BodyScale}× scale to {count} small addict(s) (giants unchanged).");
    }

    /// <summary>Batch: -executeMethod SimpleCitizensHippieNpcSetup.ApplySmallAddictScaleBatch</summary>
    public static void ApplySmallAddictScaleBatch() => ApplySmallAddictScaleFromMenu();

    public static void ApplyColorfulSmallAddictLookFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Colorful Addicts", "Exit Play mode first.", "OK");
            return;
        }

        if (!BuildColorfulSmallAddictAssets())
            return;

        var colorfulCount = ApplyColorfulSmallAddictLookInActiveScene();
        var scaleCount = ApplySmallAddictScaleInActiveScene();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Dutz] Applied colorful look to {colorfulCount} and {DutzSmallAddictScale.BodyScale}× scale to {scaleCount} small addict(s) (giants unchanged).");
    }

    /// <summary>Batch: -executeMethod SimpleCitizensHippieNpcSetup.ApplyColorfulSmallAddictLookBatch</summary>
    public static void ApplyColorfulSmallAddictLookBatch() => ApplyColorfulSmallAddictLookFromMenu();

    public static bool BuildColorfulSmallAddictAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HippiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing hippie prefab for colorful look.");
            return false;
        }

        SkinnedMeshRenderer hippieRenderer = null;
        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer != null && renderer.gameObject.name == HippieOutfit && renderer.sharedMesh != null)
            {
                hippieRenderer = renderer;
                break;
            }
        }

        if (hippieRenderer == null)
        {
            Debug.LogError("[Dutz] Missing SC_Hippie mesh for colorful look.");
            return false;
        }

        var headTiles = DutzGiantHippieBossFaceBuilder.GetHeadTileOrigins(
            hippieRenderer.sharedMesh, hippieRenderer.bones, 1);
        if (headTiles == null || headTiles.Length == 0)
        {
            Debug.LogError("[Dutz] Could not resolve SC_Hippie head UV tiles for colorful look.");
            return false;
        }

        var headTileSet = new HashSet<Vector2Int>(headTiles);
        var output = new Texture2D(ColorfulAtlasSize, ColorfulAtlasSize, TextureFormat.RGBA32, false);
        for (var y = 0; y < ColorfulAtlasSize; y++)
        {
            for (var x = 0; x < ColorfulAtlasSize; x++)
            {
                var tile = new Vector2Int(
                    x / ColorfulTileSize * ColorfulTileSize,
                    y / ColorfulTileSize * ColorfulTileSize);
                output.SetPixel(x, y, headTileSet.Contains(tile) ? BrightOrangeHead : BrightRedBody);
            }
        }

        var sideCount = PaintAngryFaceOnAtlas(output, hippieRenderer);
        output.Apply();
        var png = output.EncodeToPNG();
        Object.DestroyImmediate(output);

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[Dutz] Could not resolve project root for colorful hippie texture.");
            return false;
        }

        var textureFullPath = Path.Combine(
            projectRoot, ColorfulTexturePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(textureFullPath, png);
        AssetDatabase.ImportAsset(ColorfulTexturePath, ImportAssetOptions.ForceUpdate);

        var template = AssetDatabase.LoadAssetAtPath<Material>(HippieMaterialPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(ColorfulMaterialPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ColorfulTexturePath);
        if (material == null)
        {
            material = template != null
                ? new Material(template) { name = "SimpleCitizens_Hippie_Colorful" }
                : new Material(Shader.Find("Standard")) { name = "SimpleCitizens_Hippie_Colorful" };
            AssetDatabase.CreateAsset(material, ColorfulMaterialPath);
        }

        if (texture != null)
            material.mainTexture = texture;

        material.color = Color.white;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        Debug.Log("[Dutz] Built colorful small addict texture with angry face on " +
                  sideCount + " head side(s), rear left blank.");
        return true;
    }

    static int PaintAngryFaceOnAtlas(Texture2D atlas, SkinnedMeshRenderer hippieRenderer)
    {
        if (atlas == null || hippieRenderer == null || hippieRenderer.sharedMesh == null)
            return 0;

        var sideGroups = DutzGiantHippieBossFaceBuilder.GetThreeSideHeadFaceTileGroups(
            hippieRenderer.sharedMesh, hippieRenderer.bones);
        if (sideGroups == null || sideGroups.Count == 0)
        {
            var fallback = ResolveSmallAddictFacePaintTiles(hippieRenderer);
            if (fallback != null && fallback.Length > 0)
                PaintAngryFaceOnTileGroup(atlas, fallback);
            return fallback?.Length > 0 ? 1 : 0;
        }

        foreach (var tiles in sideGroups)
            PaintAngryFaceOnTileGroup(atlas, tiles);

        return sideGroups.Count;
    }

    static void PaintAngryFaceOnTileGroup(Texture2D atlas, Vector2Int[] tiles)
    {
        if (atlas == null || tiles == null || tiles.Length == 0)
            return;

        var portrait = LoadAngryFacePortrait();
        if (portrait != null)
        {
            var flat = FlattenPortraitOnColor(portrait, BrightOrangeHead);
            BlitPortraitOnHeadFootprint(atlas, flat, tiles);
            Object.DestroyImmediate(flat);
            Object.DestroyImmediate(portrait);
            return;
        }

        DrawProceduralAngryFace(atlas, tiles);
    }

    static Vector2Int[] ResolveSmallAddictFacePaintTiles(SkinnedMeshRenderer hippieRenderer)
    {
        if (hippieRenderer == null || hippieRenderer.sharedMesh == null)
            return null;

        var mesh = hippieRenderer.sharedMesh;
        var bones = hippieRenderer.bones;

        var headTiles = DutzGiantHippieBossFaceBuilder.GetHeadTileOrigins(mesh, bones, 1);
        if (headTiles == null || headTiles.Length == 0)
            return null;

        // Full forehead-to-jaw sheet across all front face columns (not the 1–2 tile front strip).
        var expanded = DutzGiantHippieBossFaceBuilder.GetExpandedFrontFaceTiles(mesh, bones, headTiles);
        if (expanded != null && expanded.Length > 0)
            return expanded;

        var paintTiles = DutzGiantHippieBossFaceBuilder.GetFacePaintTiles(mesh, bones);
        if (paintTiles != null && paintTiles.Length > 0)
            return paintTiles;

        return headTiles;
    }

    public static int ApplyColorfulSmallAddictLookInActiveScene()
    {
        var count = 0;
        foreach (var physics in Object.FindObjectsOfType<SimpleCitizensNpcPhysics>())
        {
            if (physics == null || !SimpleCitizensHippieBiter.IsSmallAddictName(physics.gameObject.name))
                continue;

            DutzSmallAddictColorfulLook.Apply(physics.gameObject);
            EditorUtility.SetDirty(physics.gameObject);
            count++;
        }

        return count;
    }

    public static int ApplySmallAddictScaleInActiveScene()
    {
        var count = 0;
        foreach (var physics in Object.FindObjectsOfType<SimpleCitizensNpcPhysics>())
        {
            if (physics == null || !SimpleCitizensHippieBiter.IsSmallAddictName(physics.gameObject.name))
                continue;

            DutzSmallAddictScale.Apply(physics.gameObject);
            physics.SnapFeetToRoad();
            EditorUtility.SetDirty(physics.gameObject);
            count++;
        }

        return count;
    }

    /// <summary>Stretch one portrait across one head-side UV column footprint.</summary>
    static void BlitPortraitOnHeadFootprint(Texture2D target, Texture2D portrait, Vector2Int[] tiles)
    {
        if (tiles == null || tiles.Length == 0)
            return;

        const int tileSize = 64;
        var minX = tiles.Min(tile => tile.x);
        var minY = tiles.Min(tile => tile.y);
        var maxX = tiles.Max(tile => tile.x) + tileSize;
        var maxY = tiles.Max(tile => tile.y) + tileSize;
        var footprintW = maxX - minX;
        var footprintH = maxY - minY;

        var master = DutzGiantHippieBossFaceBuilder.CropPortraitForFaceBlockPublic(
            portrait, footprintW, footprintH, 0.96f);
        if (master == null)
        {
            DutzGiantHippieBossFaceBuilder.BlitPortraitOnFaceTiles(target, portrait, tiles);
            return;
        }

        foreach (var tile in tiles)
        {
            var slice = ExtractTextureRegion(master, tile.x - minX, tile.y - minY, tileSize, tileSize);
            BlitOpaqueBlock(target, slice, tile.x, tile.y);
            Object.DestroyImmediate(slice);
        }

        Object.DestroyImmediate(master);
    }

    static Texture2D ExtractTextureRegion(Texture2D source, int x, int y, int width, int height)
    {
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (var py = 0; py < height; py++)
        for (var px = 0; px < width; px++)
            result.SetPixel(px, py, source.GetPixel(x + px, y + py));

        result.Apply();
        return result;
    }

    static void BlitOpaqueBlock(Texture2D target, Texture2D block, int destX, int destY)
    {
        for (var py = 0; py < block.height; py++)
        for (var px = 0; px < block.width; px++)
            target.SetPixel(destX + px, destY + py, block.GetPixel(px, py));
    }

    static Texture2D LoadAngryFacePortrait()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        var fullPath = Path.Combine(
            projectRoot, AngryFaceTexturePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return null;

        var bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.DestroyImmediate(tex);
            return null;
        }

        tex.Apply();
        return tex;
    }

    static Texture2D FlattenPortraitOnColor(Texture2D src, Color background)
    {
        var result = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        for (var y = 0; y < src.height; y++)
        {
            for (var x = 0; x < src.width; x++)
            {
                var c = src.GetPixel(x, y);
                result.SetPixel(x, y, new Color(
                    Mathf.Lerp(background.r, c.r, c.a),
                    Mathf.Lerp(background.g, c.g, c.a),
                    Mathf.Lerp(background.b, c.b, c.a),
                    1f));
            }
        }

        result.Apply();
        return result;
    }

    static void DrawProceduralAngryFace(Texture2D atlas, Vector2Int[] faceTiles)
    {
        var minX = faceTiles.Min(tile => tile.x);
        var minY = faceTiles.Min(tile => tile.y);
        var maxX = faceTiles.Max(tile => tile.x) + ColorfulTileSize;
        var maxY = faceTiles.Max(tile => tile.y) + ColorfulTileSize;
        var width = maxX - minX;
        var height = maxY - minY;
        var cx = minX + width * 0.5f;
        var cy = minY + height * 0.56f;
        var brown = new Color(0.29f, 0.17f, 0.13f, 1f);

        DrawThickLine(atlas, cx - width * 0.22f, cy + height * 0.12f, cx - width * 0.04f, cy + height * 0.2f, brown, 4);
        DrawThickLine(atlas, cx + width * 0.22f, cy + height * 0.12f, cx + width * 0.04f, cy + height * 0.2f, brown, 4);
        FillEllipse(atlas, cx - width * 0.14f, cy + height * 0.02f, width * 0.055f, height * 0.09f, brown);
        FillEllipse(atlas, cx + width * 0.14f, cy + height * 0.02f, width * 0.055f, height * 0.09f, brown);
        DrawFrownArc(atlas, cx, cy - height * 0.16f, width * 0.24f, height * 0.1f, brown, 5);
    }

    static void DrawThickLine(Texture2D tex, float x0, float y0, float x1, float y1, Color color, int thickness)
    {
        var steps = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)));
        for (var i = 0; i <= steps; i++)
        {
            var t = steps == 0 ? 0f : i / (float)steps;
            var x = Mathf.Lerp(x0, x1, t);
            var y = Mathf.Lerp(y0, y1, t);
            FillDisc(tex, x, y, thickness * 0.5f, color);
        }
    }

    static void DrawFrownArc(Texture2D tex, float cx, float cy, float radiusX, float radiusY, Color color, int thickness)
    {
        for (var deg = 200f; deg <= 340f; deg += 2f)
        {
            var rad = deg * Mathf.Deg2Rad;
            var x = cx + Mathf.Cos(rad) * radiusX;
            var y = cy + Mathf.Sin(rad) * radiusY;
            FillDisc(tex, x, y, thickness * 0.5f, color);
        }
    }

    static void FillEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color color)
    {
        var minX = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
        var maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + rx));
        var minY = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
        var maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + ry));

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var dx = (x - cx) / Mathf.Max(rx, 0.001f);
                var dy = (y - cy) / Mathf.Max(ry, 0.001f);
                if (dx * dx + dy * dy <= 1f)
                    tex.SetPixel(x, y, color);
            }
        }
    }

    static void FillDisc(Texture2D tex, float cx, float cy, float radius, Color color)
    {
        FillEllipse(tex, cx, cy, radius, radius, color);
    }
}

