using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Lightweight sea-blue plane under highways (no Suimono). Menu: Tools / Dutz / Add Sea Under Highways
/// </summary>
public static class DutzSimpleSeaSetup
{
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string SeaObjectName = "Dutz Sea";
    const string SeaMaterialPath = "Assets/Characters/NPCs/Materials/DutzSea.mat";

    public static readonly Color DefaultSeaColor = new Color(0.02f, 0.14f, 0.32f, 1f);
    public static readonly Color DefaultSkyTint = new Color(0.06f, 0.22f, 0.42f, 1f);

    public static void DistributeGoldCoinsFromMenu() => DutzGoldCoinPlacer.DistributeFromMenu();

    public static void FixCoinHeightsFromMenu() => DutzGoldCoinPlacer.FixHeightsFromMenu();

    public static void DistributeEarlyHighwayFromMenu() => DutzEarlyHighwayContentPlacer.PlaceFromMenu();

    public static void AddFromMenu() => RefreshFromMenu();

    public static void RefreshFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Sea", "Exit Play mode first.", "OK");
            return;
        }

        if (!AddToShowcase(log: true))
        {
            EditorUtility.DisplayDialog("Sea", "Could not add sea to Dutz_Level02.", "OK");
            return;
        }
    }

    public static void AddToShowcase() => AddToShowcase(log: false);

    /// <summary>Resize/create Dutz Sea to cover every Highway/Bridge piece in the open or showcase scene.</summary>
    public static bool TryRefreshSeaInShowcase(bool log = false)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling)
            return false;

        EnsureShowcaseSceneOpen();
        return AddToShowcase(log);
    }

    public static bool NeedsSeaRefresh()
    {
        var sea = GameObject.Find(SeaObjectName);
        if (sea == null)
            return true;

        return !SeaFullyCoversRoads(sea.transform, GetRoadBounds());
    }

    static bool AddToShowcase(bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var material = EnsureSeaMaterial();
        if (material == null)
            return false;

        var roadBounds = GetRoadBounds();
        var sea = GameObject.Find(SeaObjectName);
        if (sea == null)
        {
            sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = SeaObjectName;
            Undo.RegisterCreatedObjectUndo(sea, "Add Dutz Sea");
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.isStatic = true;
        }
        else
        {
            Undo.RecordObject(sea.transform, "Refresh Dutz Sea");
        }

        FitSeaTransform(sea, roadBounds);
        ApplySeaRendererSettings(sea, material);
        ApplySeaAmbientAndCameraColors();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            var scale = sea.transform.localScale;
            Debug.Log(
                $"[Dutz] Sea covers all highways — center {sea.transform.position}, " +
                $"size {scale.x * 10f:F0}×{scale.z * 10f:F0} m, Y={sea.transform.position.y:F1}.");
        }

        return true;
    }

    static void EnsureShowcaseSceneOpen()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path == ScenePath)
            return;

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    static void FitSeaTransform(GameObject sea, Bounds roadBounds)
    {
        var seaY = GetSeaLevel();
        var center = roadBounds.center;
        center.y = seaY;

        const float margin = 200f;
        var sizeX = Mathf.Max(roadBounds.size.x + margin, 400f);
        var sizeZ = Mathf.Max(roadBounds.size.z + margin, 400f);

        sea.transform.position = center;
        sea.transform.rotation = Quaternion.identity;
        sea.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
    }

    static void ApplySeaRendererSettings(GameObject sea, Material material)
    {
        var renderer = sea.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        renderer.sharedMaterial = material;
        if (material != null)
            material.renderQueue = 2000;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static bool SeaFullyCoversRoads(Transform sea, Bounds roads)
    {
        var halfX = sea.lossyScale.x * 5f;
        var halfZ = sea.lossyScale.z * 5f;
        var p = sea.position;
        const float tolerance = 15f;

        return roads.min.x >= p.x - halfX + tolerance
            && roads.max.x <= p.x + halfX - tolerance
            && roads.min.z >= p.z - halfZ + tolerance
            && roads.max.z <= p.z + halfZ - tolerance;
    }

    static Material EnsureSeaMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(SeaMaterialPath);
        if (mat != null)
        {
            mat.color = DefaultSeaColor;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        mat = new Material(shader)
        {
            name = "DutzSea",
            color = DefaultSeaColor
        };
        if (shader.name.Contains("Standard"))
        {
            mat.SetFloat("_Glossiness", 0.65f);
            mat.SetFloat("_Metallic", 0.05f);
        }

        var dir = System.IO.Path.GetDirectoryName(SeaMaterialPath);
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder("Assets/Characters/NPCs/Materials"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Characters/NPCs"))
                AssetDatabase.CreateFolder("Assets", "Characters");
            if (!AssetDatabase.IsValidFolder("Assets/Characters/NPCs"))
                AssetDatabase.CreateFolder("Assets/Characters", "NPCs");
            AssetDatabase.CreateFolder("Assets/Characters/NPCs", "Materials");
        }

        AssetDatabase.CreateAsset(mat, SeaMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void ApplySeaAmbientAndCameraColors()
    {
        DutzMobileLighting.ApplyBrightShowcaseLighting();

        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (!cam.enabled)
                continue;

            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = DefaultSkyTint;
        }
    }

    static Bounds GetRoadBounds()
    {
        var bounds = default(Bounds);
        var hasBounds = false;

        foreach (var meshFilter in Object.FindObjectsOfType<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (!IsUnderRoadSegment(meshFilter.transform))
                continue;

            var worldBounds = GetWorldMeshBounds(meshFilter);
            if (!hasBounds)
            {
                bounds = worldBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(worldBounds);
            }
        }

        if (!hasBounds)
            bounds = new Bounds(new Vector3(180f, 0f, -190f), new Vector3(1800f, 20f, 750f));

        return bounds;
    }

    static Bounds GetWorldMeshBounds(MeshFilter meshFilter)
    {
        var meshBounds = meshFilter.sharedMesh.bounds;
        var matrix = meshFilter.transform.localToWorldMatrix;
        var worldBounds = new Bounds(matrix.MultiplyPoint3x4(meshBounds.center), Vector3.zero);
        var extents = meshBounds.extents;

        for (var xi = -1; xi <= 1; xi += 2)
        for (var yi = -1; yi <= 1; yi += 2)
        for (var zi = -1; zi <= 1; zi += 2)
        {
            var corner = meshBounds.center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
            worldBounds.Encapsulate(matrix.MultiplyPoint3x4(corner));
        }

        return worldBounds;
    }

    static float GetSeaLevel()
    {
        const float defaultSeaY = 2f;
        var seaY = defaultSeaY;

        foreach (var meshFilter in Object.FindObjectsOfType<MeshFilter>(true))
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            if (!IsUnderRoadSegment(meshFilter.transform))
                continue;

            var worldBounds = GetWorldMeshBounds(meshFilter);
            seaY = Mathf.Max(seaY, worldBounds.min.y + 0.5f);
        }

        return Mathf.Clamp(seaY, defaultSeaY, 4f);
    }

    static bool IsUnderRoadSegment(Transform t)
    {
        while (t != null)
        {
            var name = t.name;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.Contains("Slogan") || name.Contains("Wall Slogan"))
                    return false;

                if (name.Contains("Highway") || name.Contains("Bridge"))
                    return true;
            }

            t = t.parent;
        }

        return false;
    }

}

/// <summary>Places 50 gold coins along the Dutz highway track.</summary>
public static class DutzGoldCoinPlacer
{
    const string ShowcaseScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string GoldCoinPrefabPath =
        "Assets/LiquidFire Package 4 - BSH games/Devtoid - Gold Coins/3D Assets/Gold Coin - Single/Prefab/GoldCoin.prefab";
    const string CoinsRootName = "DutzGoldCoins";
    const string CoinPrefix = "DutzGoldCoin_";
    static readonly string[] StrayTemplateNames = { "GoldCoin", "GoldCoins" };

    const int TotalCoins = 50;
    const float CoinWorldScale = 500f;
    const float CoinStandPitchDegrees = 90f;
    /// <summary>Coins more than this above target are snapped down (sky / bridge-roof placements).</summary>
    const float ExcessiveCoinHeightMeters = 6f;

    /// <summary>Batch: -executeMethod DutzGoldCoinPlacer.DistributeOnShowcase</summary>
    public static void DistributeOnShowcase() => DistributeOnShowcase(log: true);

    /// <summary>Batch: -executeMethod DutzGoldCoinPlacer.DistributeOnLevel02</summary>
    public static void DistributeOnLevel02() => DistributeOnLevel02(log: true);

    public static bool DistributeOnLevel02(bool log) => DistributeOnScene(Level01ScenePath, log);

    public static void DistributeFromMenu() => DistributeCollectiblesFromMenu();

    public static void DistributeCollectiblesFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Collectibles", "Exit Play mode first.", "OK");
            return;
        }

        var okCoins = DistributeOnShowcase(log: true);
        var okSuitcases = DutzSuitcasePlacer.DistributeOnLevel02(log: true);

        if (!okCoins || !okSuitcases)
        {
            EditorUtility.DisplayDialog(
                "Collectibles",
                "Distribution failed for one or both levels. Check the Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Collectibles",
            "Coins (Level 2) and suitcases (Level 1) were placed along the track center ahead of Player1.",
            "OK");
    }

    public static void FixHeightsFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Gold Coins", "Exit Play mode first.", "OK");
            return;
        }

        if (!FixCoinHeightsOnShowcase(log: true))
            EditorUtility.DisplayDialog("Gold Coins", "Could not fix coin heights on Dutz_Level02.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzGoldCoinPlacer.FixCoinHeightsOnShowcase</summary>
    public static void FixCoinHeightsOnShowcase() => FixCoinHeightsOnShowcase(log: false);

    /// <summary>Batch: -executeMethod DutzGoldCoinPlacer.FixCoinOrientationsOnShowcaseBatch</summary>
    public static void FixCoinOrientationsOnShowcaseBatch() => FixCoinOrientationsOnShowcase(log: true);

    public static bool FixCoinOrientationsOnShowcase(bool log)
    {
        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        var fixedCount = 0;

        foreach (var coin in Object.FindObjectsOfType<DutzGoldCoin>(true))
        {
            if (coin == null || !DutzGoldCoin.IsTrackCoinRoot(coin.gameObject))
                continue;

            var yaw = coin.transform.eulerAngles.y;
            var target = Quaternion.Euler(CoinStandPitchDegrees, yaw, 0f);
            if (Quaternion.Angle(coin.transform.rotation, target) < 0.5f)
                continue;

            Undo.RecordObject(coin.transform, "Fix Coin Orientation");
            coin.transform.rotation = target;
            DutzCollectibleTrackPlacer.WriteSpawnPose(coin);
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
        {
            Debug.Log(
                fixedCount > 0
                    ? $"[Dutz] Fixed upright rotation on {fixedCount} coin(s) on {ShowcaseScenePath}."
                    : $"[Dutz] All coins on {ShowcaseScenePath} already upright.");
        }

        return true;
    }

    public static bool FixCoinHeightsOnShowcase(bool log)
    {
        var scene = EditorSceneManager.OpenScene(ShowcaseScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();
        var spawn = GetPlayerSpawn();
        var fixedCount = 0;
        var maxDelta = 0f;

        foreach (var coin in Object.FindObjectsOfType<DutzGoldCoin>(true))
        {
            if (coin == null)
                continue;

            var pos = coin.transform.position;
            var deckY = SampleRoadDeckY(pos.x, pos.z, spawn);
            var targetY = deckY + DutzCollectibleTrackPlacer.HeightAboveDeckMeters;
            if (pos.y <= targetY + ExcessiveCoinHeightMeters)
                continue;

            var delta = pos.y - targetY;
            if (delta > maxDelta)
                maxDelta = delta;

            Undo.RecordObject(coin.transform, "Fix Coin Height");
            coin.transform.position = new Vector3(pos.x, targetY, pos.z);
            DutzCollectibleTrackPlacer.WriteSpawnPose(coin);
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (log)
        {
            Debug.Log(
                fixedCount > 0
                    ? $"[Dutz] Fixed {fixedCount} sky-high coin(s) to ground + {DutzCollectibleTrackPlacer.HeightAboveDeckMeters:F0}m (max drop {maxDelta:F1}m)."
                    : $"[Dutz] No coins needed height fix (target: ground + {DutzCollectibleTrackPlacer.HeightAboveDeckMeters:F0}m).");
        }

        return true;
    }

    /// <summary>Batch: -executeMethod DutzGoldCoinPlacer.DistributeCollectiblesBatch</summary>
    public static void DistributeCollectiblesBatch()
    {
        DistributeOnShowcase(log: true);
        DutzSuitcasePlacer.DistributeOnLevel02(log: true);
    }

    public static bool DistributeOnShowcase(bool log) => DistributeOnScene(ShowcaseScenePath, log);

    public static bool DistributeOnScene(string scenePath, bool log)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var prefab = ResolveCoinPrefabAsset();
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing gold coin prefab.");
            return false;
        }

        var positions = DutzCollectibleTrackPlacer.BuildCollectiblePositions(out var diagnostics, log);
        if (positions.Count != TotalCoins)
        {
            if (log)
                Debug.LogError($"[Dutz] Expected {TotalCoins} collectible positions, got {positions.Count}. {diagnostics}");
            return false;
        }

        RemoveExistingCoins();
        RemoveStraySceneTemplate();
        var root = EnsureCoinsRoot();

        var placed = 0;

        for (var i = 0; i < positions.Count; i++)
        {
            var worldPos = positions[i];

            var coin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(coin, "Place Gold Coins");
            coin.name = $"{CoinPrefix}{i + 1:00}";

            coin.transform.SetParent(root.transform, true);
            coin.transform.position = worldPos;
            coin.transform.localScale = Vector3.one * CoinWorldScale;
            coin.transform.rotation = Quaternion.Euler(CoinStandPitchDegrees, i * 37f, 0f);
            ConfigureCoin(coin);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log && positions.Count > 0)
        {
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var p in positions)
            {
                if (p.y < minY)
                    minY = p.y;
                if (p.y > maxY)
                    maxY = p.y;
            }

            Debug.Log(
                $"[Dutz] Placed {placed} coins ({CoinWorldScale:F0}×), Y {minY:F1}–{maxY:F1}. {diagnostics}");
        }

        return placed == TotalCoins;
    }

    static void ConfigureCoin(GameObject go)
    {
        if (go.GetComponent<DutzGoldCoin>() == null)
            Undo.AddComponent<DutzGoldCoin>(go);

        DutzCollectibleTrackPlacer.WriteSpawnPose(go.GetComponent<DutzGoldCoin>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
    }

    static Vector3 GetPlayerSpawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            if (player == null || !player.name.Contains("Player1"))
                continue;

            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackSpawn, out _))
            return trackSpawn;

        return new Vector3(-390f, 18f, 1f);
    }

    static float SampleRoadDeckY(float worldX, float worldZ, Vector3 spawn)
    {
        var hintY = spawn.y - 8f;
        var sample = new Vector3(worldX, hintY, worldZ);
        if (DutzRoadGround.TrySampleRoadDeckY(sample, spawn.y, null, out var deckY))
            return deckY;

        if (DutzRoadGround.TrySampleSurfaceY(sample, null, out var surfaceY))
            return Mathf.Min(surfaceY, spawn.y + 0.5f);

        return hintY;
    }

    static GameObject ResolveCoinPrefabAsset() =>
        AssetDatabase.LoadAssetAtPath<GameObject>(GoldCoinPrefabPath);

    static void RemoveStraySceneTemplate()
    {
        foreach (var templateName in StrayTemplateNames)
        {
            var stray = GameObject.Find(templateName);
            if (stray != null && !stray.name.StartsWith(CoinPrefix, System.StringComparison.Ordinal))
                Undo.DestroyObjectImmediate(stray);
        }
    }

    static GameObject EnsureCoinsRoot()
    {
        var root = GameObject.Find(CoinsRootName);
        if (root != null)
            return root;

        root = new GameObject(CoinsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Gold Coins Root");
        return root;
    }

    static void RemoveExistingCoins()
    {
        var toRemove = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            CollectCoinsInHierarchy(root, toRemove);

        foreach (var go in toRemove)
            Undo.DestroyObjectImmediate(go);
    }

    static void CollectCoinsInHierarchy(GameObject go, List<GameObject> list)
    {
        if (go.name.StartsWith(CoinPrefix, System.StringComparison.Ordinal))
            list.Add(go);

        foreach (Transform child in go.transform)
            CollectCoinsInHierarchy(child.gameObject, list);
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
}
