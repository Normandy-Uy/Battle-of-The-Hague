using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Places 30 health potions on Level 3 â€” 5 per highway segment.
/// One-time authoring: instances live in Dutz_Level03.unity; adjust each potion's Spawn Pose in the Inspector.
/// Batch recovery only: -executeMethod DutzHealthPotionPlacer.PlaceOnLevel03Batch</summary>
public static class DutzHealthPotionPlacer
{
    const string PotionsRootName = "DutzHealthPotions";
    const string PotionPrefabPath = "Assets/Characters/Level03/Prefabs/DutzHealthPotion.prefab";
    const string PotionMaterialPath = "Assets/Characters/Level03/Materials/DutzHealthPotionGreen.mat";
    const int PotionsPerSegment = 5;
    const float HeightAboveDeckMeters = 1.5f;
    const float JumpHeightSafetyMargin = 0.75f;
    const float PotionWorldScale = 16f;

    static readonly string[] HighwaySegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6"
    };

    static readonly float[] AlongFractions = { 0.15f, 0.35f, 0.50f, 0.65f, 0.85f };

    /// <summary>Batch: -executeMethod DutzHealthPotionPlacer.PlaceOnLevel03Batch</summary>
    public static void PlaceOnLevel03Batch() => PlaceOnLevel03(log: true);

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Health Potions", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Health Potions",
                "Could not place health potions on Dutz_Level03. Check the Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Health Potions",
            "30 health potions placed on Level 3 (5 per highway segment).",
            "OK");
    }

    public static bool PlaceOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        var prefab = EnsurePotionPrefab();
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing health potion prefab.");
            return false;
        }

        var positions = BuildPotionPositions(out var diagnostics, log);
        if (positions.Count != PotionsPerSegment * HighwaySegmentNames.Length)
        {
            Debug.LogError(
                $"[Dutz] Expected {PotionsPerSegment * HighwaySegmentNames.Length} potion positions, got {positions.Count}. {diagnostics}");
            return false;
        }

        RemoveExistingPotions();

        var root = EnsurePotionsRoot();
        var placed = 0;

        for (var i = 0; i < positions.Count; i++)
        {
            var pose = positions[i];
            var potion = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(potion, "Place Health Potions");
            potion.name = pose.Name;

            potion.transform.SetParent(root.transform, true);
            potion.transform.position = pose.Position;
            potion.transform.rotation = pose.Rotation;
            potion.transform.localScale = Vector3.one * PotionWorldScale;

            var component = potion.GetComponent<DutzHealthPotion>();
            if (component == null)
                component = potion.AddComponent<DutzHealthPotion>();

            component.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(component);
            placed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Placed {placed} health potion(s) on Level 3 (5 per segment). {diagnostics}");
        }

        return placed == positions.Count;
    }

    struct PotionPose
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    static List<PotionPose> BuildPotionPositions(out string diagnostics, bool log)
    {
        diagnostics = string.Empty;
        var poses = new List<PotionPose>(PotionsPerSegment * HighwaySegmentNames.Length);

        var spawn = GetPlayer1Spawn();
        var travelForward = GetPlayerTravelForward(spawn);
        var paths = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(HighwaySegmentNames, spawn, travelForward);

        if (paths.Count == 0)
        {
            diagnostics = "No highway segment paths found.";
            if (log)
                Debug.LogWarning($"[Dutz] {diagnostics}");
            return poses;
        }

        var maxReachAboveDeck = GetMaxJumpHeightAboveDeck();
        var lift = Mathf.Min(HeightAboveDeckMeters, maxReachAboveDeck);
        var laneIndex = 0;

        for (var segmentIndex = 0; segmentIndex < paths.Count; segmentIndex++)
        {
            var path = paths[segmentIndex];
            if (path.Samples == null || path.Samples.Count == 0)
            {
                Debug.LogWarning($"[Dutz] No deck samples for segment: {path.SegmentName}");
                continue;
            }

            for (var potionIndex = 0; potionIndex < PotionsPerSegment; potionIndex++)
            {
                var along = AlongFractions[potionIndex];
                if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, along, out var sample))
                    continue;

                var laneZ = DutzHighwayDeckSampler.SevenLaneZ[laneIndex % DutzHighwayDeckSampler.SevenLaneZ.Length];
                laneIndex++;

                var deck = DutzHighwayDeckSampler.PlaceOnLane(sample, laneZ, spawn);
                var worldPos = new Vector3(deck.x, deck.y + lift, deck.z);
                var rotation = Quaternion.LookRotation(sample.Forward, Vector3.up);

                poses.Add(new PotionPose
                {
                    Name = $"{DutzHealthPotion.PotionPrefix}Seg{segmentIndex + 1:00}_{potionIndex + 1:00}",
                    Position = worldPos,
                    Rotation = rotation
                });
            }
        }

        diagnostics =
            $"spawn=({spawn.x:F1},{spawn.y:F1},{spawn.z:F1}) lift={lift:F1}m jumpMax={maxReachAboveDeck:F1}m " +
            $"segments={paths.Count} scale={PotionWorldScale:F0}";

        if (log && poses.Count > 0)
        {
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            foreach (var pose in poses)
            {
                if (pose.Position.y < minY)
                    minY = pose.Position.y;
                if (pose.Position.y > maxY)
                    maxY = pose.Position.y;
            }

            Debug.Log(
                $"[Dutz] Built {poses.Count} health potion position(s). Y {minY:F1}â€“{maxY:F1}. {diagnostics}");
        }

        return poses;
    }

    static GameObject EnsurePotionPrefab()
    {
        EnsurePotionMaterial();

        if (!DutzHealthPotionModelBuilder.SyncAndBuildSharedAssets(log: false))
            return AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);

        return DutzHealthPotionModelBuilder.RebuildGreenPotionPrefab(log: false)
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(PotionPrefabPath);
    }

    static void EnsurePotionMaterial()
    {
        var existingMat = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        if (existingMat != null)
        {
            if (!existingMat.enableInstancing)
            {
                existingMat.enableInstancing = true;
                EditorUtility.SetDirty(existingMat);
            }

            return;
        }

        EnsureAssetFolder("Assets/Characters/Level03/Materials");

        var material = new Material(Shader.Find("Standard"))
        {
            name = "DutzHealthPotionGreen",
            color = new Color(0.15f, 0.92f, 0.25f, 1f),
        };
        material.SetFloat("_Glossiness", 0.65f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.1f, 0.45f, 0.12f));
        material.enableInstancing = true;

        AssetDatabase.CreateAsset(material, PotionMaterialPath);
        AssetDatabase.SaveAssets();
    }

    static void ApplyPotionMaterial(GameObject root)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        if (material == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = material;
    }

    static void RemoveExistingPotions()
    {
        var existing = GameObject.Find(PotionsRootName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    static GameObject EnsurePotionsRoot()
    {
        var root = new GameObject(PotionsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Place Health Potions");
        return root;
    }

    internal static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
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
        return jumpForce * jumpForce / (2f * gravityMag) - JumpHeightSafetyMargin;
    }

    static Vector3 GetPlayer1Spawn()
    {
        return GetPlayer1SpawnForAuthoring();
    }

    internal static Vector3 GetPlayer1SpawnForAuthoring()
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

    static Vector3 GetPlayerTravelForward(Vector3 spawn)
    {
        return GetPlayerTravelForwardForAuthoring(spawn);
    }

    internal static Vector3 GetPlayerTravelForwardForAuthoring(Vector3 spawn)
    {
        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            if (so.FindProperty("invertSpawnFacing").boolValue)
                forward = -forward;
            break;
        }

        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.right;
    }
}

/// <summary>
/// Syncs public/healthpotion20.fbx, bakes one shared mesh for all green potions (29 instances = 1 GPU mesh).
/// </summary>
public static class DutzHealthPotionModelBuilder
{
    const string SourceFileName = "healthpotion20.fbx";
    const string AssetFbxPath = "Assets/Characters/Level03/Models/healthpotion20.fbx";
    const string SharedMeshPath = "Assets/Resources/DutzHealthPotionGreenMesh.asset";
    const string PotionPrefabPath = "Assets/Characters/Level03/Prefabs/DutzHealthPotion.prefab";
    const string PotionMaterialPath = "Assets/Characters/Level03/Materials/DutzHealthPotionGreen.mat";
    const string RedMaterialPath = "Assets/Characters/Level03/Materials/DutzHealthPotionRed.mat";
    const string PotionsRootName = "DutzHealthPotions";
    const float TargetVisualHeight = DutzHealthPotionSetup.TargetVisualHeight;

    /// <summary>Batch: -executeMethod DutzHealthPotionModelBuilder.ApplyGreenFbxOnLevel03Batch</summary>
    public static void ApplyGreenFbxOnLevel03Batch() => ApplyGreenFbxOnLevel03(log: true);

    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Green Health Potion FBX", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyGreenFbxOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Green Health Potion FBX",
                "Could not apply healthpotion20.fbx on Level 3. Check the Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Green Health Potion FBX",
            "All green Level 3 health potions now use healthpotion20.fbx.\n\n" +
            "Cost savings: one shared mesh + one material for all 29 pickups (GPU instancing enabled).",
            "OK");
    }

    public static void ApplyFromMenuRed()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Red Health Potion FBX", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyRedFbxOnLevel03(log: true))
        {
            EditorUtility.DisplayDialog(
                "Red Health Potion FBX",
                "Could not apply red healthpotion20.fbx on Level 3. Check the Console.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Red Health Potion FBX",
            "Bridge 5 red health potion now uses healthpotion20.fbx with red material.\n\n+100 HP (ignores the 100 HP cap).",
            "OK");
    }

    public static bool ApplyGreenFbxOnLevel03(bool log)
    {
        if (!SyncAndBuildSharedAssets(log))
            return false;

        if (!RebuildGreenPotionPrefab(log))
            return false;

        return UpgradeGreenPotionsInScene(DutzLevel02Setup.Level03ScenePath, log);
    }

    /// <summary>Batch: -executeMethod DutzHealthPotionModelBuilder.ApplyRedFbxOnLevel03Batch</summary>
    public static void ApplyRedFbxOnLevel03Batch() => ApplyRedFbxOnLevel03(log: true);

    public static bool ApplyRedFbxOnLevel03(bool log)
    {
        if (!SyncAndBuildSharedAssets(log))
            return false;

        EnsureRedMaterialAsset(log);
        return UpgradeRedPotionInScene(DutzLevel02Setup.Level03ScenePath, log);
    }

    public static bool EnsureOnOpenLevel03(bool log) =>
        EnsurePotionsInScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), log);

    public static bool EnsurePotionsInScene(UnityEngine.SceneManagement.Scene scene, bool log)
    {
        if (!scene.IsValid() || scene.path != DutzLevel02Setup.Level03ScenePath)
            return false;

        EnsureRedMaterialAssetInternal();
        if (!SyncAndBuildSharedAssets(log: false)
            && AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath) == null)
            return false;

        var root = GameObject.Find(PotionsRootName);
        if (root == null)
            return false;

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var greenMat = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        var redMat = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        if (mesh == null || greenMat == null || redMat == null)
            return false;

        var changed = false;
        foreach (Transform child in root.transform)
        {
            if (DutzHealthPotionSetup.IsRedTrackPotion(child.gameObject))
            {
                if (!NeedsPotionVisualRepair(child, mesh))
                    continue;

                AttachRedVisual(child, log: false);
                changed = true;
            }
            else if (DutzHealthPotionSetup.IsGreenTrackPotion(child.gameObject))
            {
                if (!NeedsPotionVisualRepair(child, mesh))
                    continue;

                AttachSharedVisual(child, mesh, greenMat);
                changed = true;
            }
        }

        if (!changed)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Restored Level 3 health potion visuals (including bridge red potion).");

        return true;
    }

    static bool NeedsPotionVisualRepair(Transform potion, Mesh sharedMesh)
    {
        if (potion.Find("Bottle") != null || potion.Find("Cork") != null)
            return true;

        var visual = potion.Find(DutzHealthPotionSetup.VisualChildName);
        if (visual == null)
            return true;

        var filter = visual.GetComponent<MeshFilter>();
        return filter == null || filter.sharedMesh == null || filter.sharedMesh != sharedMesh;
    }

    public static bool AttachRedVisual(Transform root, bool log = false)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        if (mesh == null || material == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing shared potion mesh or red material.");
            return false;
        }

        AttachSharedVisual(root, mesh, material);
        return true;
    }

    static bool UpgradeRedPotionInScene(string scenePath, bool log)
    {
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Scene not found: " + scenePath);
            return false;
        }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        if (mesh == null || material == null)
            return false;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var redPotion = GameObject.Find(DutzHealthPotion.Bridge5RedPotionName);
        if (redPotion == null)
        {
            if (log)
                Debug.LogError("[Dutz] " + DutzHealthPotion.Bridge5RedPotionName + " not found on Level 3.");
            return false;
        }

        Undo.RecordObject(redPotion.transform, "Apply Red Health Potion FBX");
        AttachSharedVisual(redPotion.transform, mesh, material);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Applied shared healthpotion20.fbx visual (red) to {DutzHealthPotion.Bridge5RedPotionName}.");
        }

        return true;
    }

    static void EnsureRedMaterialAsset(bool log)
    {
        EnsureRedMaterialAssetInternal();

        var material = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        if (material != null && !material.enableInstancing)
        {
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }
    }

    internal static void EnsureRedMaterialAssetInternal()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        if (existing != null)
        {
            if (!existing.enableInstancing)
            {
                existing.enableInstancing = true;
                EditorUtility.SetDirty(existing);
            }

            return;
        }

        DutzHealthPotionPlacer.EnsureAssetFolder("Assets/Characters/Level03/Materials");

        var material = new Material(Shader.Find("Standard"))
        {
            name = "DutzHealthPotionRed",
            color = new Color(0.95f, 0.12f, 0.1f, 1f),
        };
        material.SetFloat("_Glossiness", 0.72f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0.55f, 0.05f, 0.04f));
        material.enableInstancing = true;

        AssetDatabase.CreateAsset(material, RedMaterialPath);
        AssetDatabase.SaveAssets();
    }

    public static bool SyncAndBuildSharedAssets(bool log)
    {
        EnsureRedMaterialAssetInternal();
        EnsureMaterialsInResources();

        if (AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath) != null)
            return true;

        if (!SyncSourceFbx(log))
            return false;

        ApplyOptimizedImportSettings(AssetFbxPath);

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFbxPath);
        if (fbx == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing imported health potion FBX: " + AssetFbxPath);
            return false;
        }

        var temp = Object.Instantiate(fbx);
        temp.name = "DutzHealthPotionBake";
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        foreach (var col in temp.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        var mesh = BakeCombinedMesh(temp);
        Object.DestroyImmediate(temp);

        if (mesh == null)
        {
            if (log)
                Debug.LogError("[Dutz] Could not bake shared health potion mesh.");
            return false;
        }

        EnsureAssetFolder("Assets/Resources");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(SharedMeshPath);

        AssetDatabase.CreateAsset(mesh, SharedMeshPath);

        EnsureMaterialsInResources();

        var material = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        if (material != null)
        {
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (log)
        {
            Debug.Log(
                $"[Dutz] Baked shared green potion mesh ({mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris) " +
                $"from public/{SourceFileName} -> {SharedMeshPath}");
        }

        return true;
    }

    static void EnsureMaterialsInResources()
    {
        EnsureAssetFolder("Assets/Resources");
        CopyMaterialToResources(PotionMaterialPath, "Assets/Resources/DutzHealthPotionGreen.mat");
        CopyMaterialToResources(RedMaterialPath, "Assets/Resources/DutzHealthPotionRed.mat");
        AssetDatabase.SaveAssets();
    }

    static void CopyMaterialToResources(string sourcePath, string destPath)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(destPath) != null)
            return;

        if (AssetDatabase.LoadAssetAtPath<Material>(sourcePath) == null)
            return;

        AssetDatabase.CopyAsset(sourcePath, destPath);
    }

    public static GameObject RebuildGreenPotionPrefab(bool log)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        if (mesh == null || material == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing shared potion mesh or material.");
            return null;
        }

        DutzHealthPotionPlacer.EnsureAssetFolder("Assets/Characters/Level03/Prefabs");

        var root = new GameObject("DutzHealthPotion");
        root.AddComponent<DutzHealthPotion>();
        AttachSharedVisual(root.transform, mesh, material);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PotionPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log("[Dutz] Rebuilt green health potion prefab with shared FBX mesh.");

        return prefab;
    }

    static bool UpgradeGreenPotionsInScene(string scenePath, bool log)
    {
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Scene not found: " + scenePath);
            return false;
        }

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);
        if (mesh == null || material == null)
            return false;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var root = GameObject.Find(PotionsRootName);
        if (root == null)
        {
            if (log)
                Debug.LogError("[Dutz] DutzHealthPotions root not found in Level 3.");
            return false;
        }

        var upgraded = 0;
        foreach (Transform child in root.transform)
        {
            if (!DutzHealthPotionSetup.IsGreenTrackPotion(child.gameObject))
                continue;

            Undo.RecordObject(child, "Apply Green Health Potion FBX");
            RemoveLegacyVisuals(child);
            AttachSharedVisual(child, mesh, material);
            upgraded++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Applied shared healthpotion20.fbx visual to {upgraded} green potion(s) on Level 3.");

        return upgraded > 0;
    }

    static void RemoveLegacyVisuals(Transform root)
    {
        RemoveChild(root, "Bottle");
        RemoveChild(root, "Cork");

        var visual = root.Find(DutzHealthPotionSetup.VisualChildName);
        if (visual == null)
            return;

        var filter = visual.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            return;

        RemoveChild(root, DutzHealthPotionSetup.VisualChildName);
    }

    static void RemoveChild(Transform root, string childName)
    {
        var child = root.Find(childName);
        if (child != null)
            Undo.DestroyObjectImmediate(child.gameObject);
    }

    public static void AttachSharedVisual(Transform root, Mesh mesh, Material material)
    {
        if (mesh == null || material == null)
            return;

        if (RepairPotionVisualMesh(root, mesh, material))
            return;

        if (DutzHealthPotionSetup.HasSharedMeshVisual(root))
            return;

        RemoveLegacyVisuals(root);

        var visual = new GameObject(DutzHealthPotionSetup.VisualChildName);
        Undo.RegisterCreatedObjectUndo(visual, "Apply Health Potion FBX");
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var filter = visual.AddComponent<MeshFilter>();
        var filterSo = new SerializedObject(filter);
        filterSo.FindProperty("m_Mesh").objectReferenceValue = mesh;
        filterSo.ApplyModifiedPropertiesWithoutUndo();

        var renderer = visual.AddComponent<MeshRenderer>();
        var rendererSo = new SerializedObject(renderer);
        rendererSo.FindProperty("m_Materials").arraySize = 1;
        rendererSo.FindProperty("m_Materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
        rendererSo.ApplyModifiedPropertiesWithoutUndo();

        NormalizeVisualScale(visual.transform, TargetVisualHeight);
        EditorUtility.SetDirty(visual);
    }

    static bool RepairPotionVisualMesh(Transform root, Mesh mesh, Material material)
    {
        var visual = root.Find(DutzHealthPotionSetup.VisualChildName);
        if (visual == null)
            return false;

        var filter = visual.GetComponent<MeshFilter>();
        if (filter == null)
            filter = Undo.AddComponent<MeshFilter>(visual.gameObject);

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(visual.gameObject);

        var changed = false;
        if (filter.sharedMesh != mesh)
        {
            var filterSo = new SerializedObject(filter);
            filterSo.FindProperty("m_Mesh").objectReferenceValue = mesh;
            filterSo.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        var rendererSo = new SerializedObject(renderer);
        var materialsProp = rendererSo.FindProperty("m_Materials");
        if (materialsProp.arraySize != 1
            || materialsProp.GetArrayElementAtIndex(0).objectReferenceValue != material)
        {
            materialsProp.arraySize = 1;
            materialsProp.GetArrayElementAtIndex(0).objectReferenceValue = material;
            rendererSo.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        if (changed && filter.sharedMesh == mesh)
            NormalizeVisualScale(visual, TargetVisualHeight);

        if (!changed)
            return false;

        EditorUtility.SetDirty(visual.gameObject);
        return true;
    }

    public static bool RepairPotionVisualInEditor(GameObject potionRoot)
    {
        if (potionRoot == null || !DutzHealthPotion.IsTrackPotionRoot(potionRoot))
            return false;

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        if (mesh == null)
            return false;

        Material material;
        if (DutzHealthPotionSetup.IsRedTrackPotion(potionRoot))
            material = AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
        else
            material = AssetDatabase.LoadAssetAtPath<Material>(PotionMaterialPath);

        if (material == null)
            return false;

        if (!NeedsPotionVisualRepair(potionRoot.transform, mesh))
            return false;

        return RepairPotionVisualMesh(potionRoot.transform, mesh, material);
    }

    static void NormalizeVisualScale(Transform visual, float targetHeight)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        var height = Mathf.Max(0.001f, bounds.size.y);
        visual.localScale *= targetHeight / height;
    }

    static Mesh BakeCombinedMesh(GameObject source)
    {
        var filters = source.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
            return null;

        var combines = new CombineInstance[filters.Length];
        for (var i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null)
                continue;

            combines[i].mesh = filters[i].sharedMesh;
            combines[i].transform = filters[i].transform.localToWorldMatrix;
        }

        var combined = new Mesh
        {
            name = "DutzHealthPotionGreenMesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combined.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);
        combined.Optimize();
        combined.UploadMeshData(markNoLongerReadable: true);
        return combined;
    }

    static void ApplyOptimizedImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        importer.globalScale = 1f;
        importer.meshCompression = ModelImporterMeshCompression.High;
        importer.isReadable = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.addCollider = false;
        importer.weldVertices = true;
        importer.optimizeMeshVertices = true;
        importer.optimizeMeshPolygons = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
    }

    static bool SyncSourceFbx(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve project root.");
            return false;
        }

        var source = Path.Combine(projectRoot, "public", SourceFileName);
        if (!File.Exists(source))
        {
            if (log)
                Debug.LogError("[Dutz] Missing public/" + SourceFileName + " â€” add the FBX and run again.");
            return false;
        }

        EnsureAssetFolder("Assets/Characters/Level03/Models");
        File.Copy(source, AssetFbxPath, overwrite: true);
        AssetDatabase.ImportAsset(AssetFbxPath, ImportAssetOptions.ForceUpdate);

        if (log)
            Debug.Log("[Dutz] Synced public/" + SourceFileName + " -> " + AssetFbxPath);

        return true;
    }

    /// <summary>Batch: -executeMethod DutzHealthPotionModelBuilder.ScaleAllPotionsOnLevel03Batch</summary>
    public static void ScaleAllPotionsOnLevel03Batch() => ScaleAllPotionsOnLevel03(3f, log: true);

    public static bool ScaleAllPotionsOnLevel03(float multiplier, bool log)
    {
        if (multiplier <= 0f)
            return false;

        var scenePath = DutzLevel02Setup.Level03ScenePath;
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var root = GameObject.Find(PotionsRootName);
        if (root == null)
        {
            if (log)
                Debug.LogError("[Dutz] DutzHealthPotions root not found.");
            return false;
        }

        var scaled = 0;
        foreach (Transform child in root.transform)
        {
            if (!DutzHealthPotion.IsTrackPotionRoot(child.gameObject))
                continue;

            var visual = child.Find(DutzHealthPotionSetup.VisualChildName);
            if (visual == null)
                continue;

            Undo.RecordObject(visual, "Scale Health Potions");
            visual.localScale *= multiplier;
            scaled++;
        }

        RebuildGreenPotionPrefab(log: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Scaled {scaled} Level 3 health potion(s) by {multiplier}x (green + red).");

        return scaled > 0;
    }

    static void EnsureAssetFolder(string path)
    {
        DutzHealthPotionPlacer.EnsureAssetFolder(path);
    }
}

/// <summary>Syncs public/boxergloves.fbx and applies it to the Level 3 Super Punch pickup.</summary>
public static class DutzSuperPunchModelBuilder
{
    const string SourceFileName = "boxergloves.fbx";
    const string AssetFbxPath = "Assets/Characters/Level03/Models/boxergloves.fbx";
    const string SharedMeshPath = "Assets/Resources/DutzSuperPunchGlovesMesh.asset";
    const string GlovesMaterialPath = "Assets/Characters/Level03/Materials/DutzSuperPunchGloves.mat";

    const string BridgeSegmentName = "Highway Bridge 1";
    static readonly Vector3 AuthoredMiddleDeckPosition = new(-22f, 33.406925f, 0f);

    /// <summary>Batch: -executeMethod DutzSuperPunchModelBuilder.ApplyFbxOnLevel03Batch</summary>
    public static void ApplyFbxOnLevel03Batch() => ApplyFbxOnLevel03(log: true);

    /// <summary>Batch: -executeMethod DutzSuperPunchModelBuilder.EnsureSuperPunchOnLevel03Batch</summary>
    public static void EnsureSuperPunchOnLevel03Batch() => EnsureOnLevel03(log: true);

    public static bool ApplyFbxOnLevel03(bool log)
    {
        if (!SyncAndBuildSharedAssets(log))
            return false;

        return EnsureOnLevel03(log);
    }

    public static bool EnsureOnOpenLevel03(bool log) =>
        EnsurePickupInScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), log);

    public static bool EnsureOnLevel03(bool log)
    {
        if (!System.IO.File.Exists(DutzLevel02Setup.Level03ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level03.unity not found.");
            return false;
        }

        if (!SyncAndBuildSharedAssets(log: false))
            return false;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != DutzLevel02Setup.Level03ScenePath)
            scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level03ScenePath, OpenSceneMode.Single);

        return EnsurePickupInScene(scene, log);
    }

    public static bool SyncAndBuildSharedAssets(bool log)
    {
        EnsureGlovesMaterial();
        CopyGlovesMaterialToResources();

        if (AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath) != null)
            return true;

        if (!SyncSourceFbx(log))
            return false;

        ApplyOptimizedImportSettings(AssetFbxPath);

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFbxPath);
        if (fbx == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing imported boxer gloves FBX: " + AssetFbxPath);
            return false;
        }

        var temp = Object.Instantiate(fbx);
        temp.name = "DutzSuperPunchGlovesBake";
        foreach (var col in temp.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        var mesh = BakeCombinedMesh(temp);
        Object.DestroyImmediate(temp);
        if (mesh == null)
        {
            if (log)
                Debug.LogError("[Dutz] Could not bake shared Super Punch gloves mesh.");
            return false;
        }

        EnsureAssetFolder("Assets/Resources");
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(SharedMeshPath);

        AssetDatabase.CreateAsset(mesh, SharedMeshPath);
        EnsureGlovesMaterial();
        CopyGlovesMaterialToResources();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (log)
        {
            Debug.Log(
                $"[Dutz] Baked Super Punch gloves mesh ({mesh.vertexCount} verts) from public/{SourceFileName}.");
        }

        return true;
    }

    public static void AttachSharedVisual(Transform root)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialPath);
        if (mesh == null || material == null)
            return;

        if (DutzSuperPunchPickupSetup.HasSharedMeshVisual(root))
        {
            var existingFilter = root.Find(DutzSuperPunchPickupSetup.VisualChildName)?.GetComponent<MeshFilter>();
            if (existingFilter != null && existingFilter.sharedMesh == mesh)
                return;
        }

        var legacy = root.Find(DutzSuperPunchPickupSetup.VisualChildName);
        if (legacy != null)
            Undo.DestroyObjectImmediate(legacy.gameObject);

        var visual = new GameObject(DutzSuperPunchPickupSetup.VisualChildName);
        Undo.RegisterCreatedObjectUndo(visual, "Apply Super Punch FBX");
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var filter = visual.AddComponent<MeshFilter>();
        var filterSo = new SerializedObject(filter);
        filterSo.FindProperty("m_Mesh").objectReferenceValue = mesh;
        filterSo.ApplyModifiedPropertiesWithoutUndo();

        var renderer = visual.AddComponent<MeshRenderer>();
        var rendererSo = new SerializedObject(renderer);
        rendererSo.FindProperty("m_Materials").arraySize = 1;
        rendererSo.FindProperty("m_Materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
        rendererSo.ApplyModifiedPropertiesWithoutUndo();

        var height = Mathf.Max(0.001f, mesh.bounds.size.y);
        visual.transform.localScale = Vector3.one * (DutzSuperPunchPickupSetup.TargetVisualHeight / height);
        EditorUtility.SetDirty(visual);
    }

    static bool RepairGloveVisualMesh(Transform root)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialPath);
        if (mesh == null || material == null)
            return false;

        var visual = root.Find(DutzSuperPunchPickupSetup.VisualChildName);
        if (visual == null)
            return false;

        var filter = visual.GetComponent<MeshFilter>();
        if (filter == null)
            filter = Undo.AddComponent<MeshFilter>(visual.gameObject);

        if (filter.sharedMesh == mesh)
            return false;

        var filterSo = new SerializedObject(filter);
        filterSo.FindProperty("m_Mesh").objectReferenceValue = mesh;
        filterSo.ApplyModifiedPropertiesWithoutUndo();

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<MeshRenderer>(visual.gameObject);

        var rendererSo = new SerializedObject(renderer);
        rendererSo.FindProperty("m_Materials").arraySize = 1;
        rendererSo.FindProperty("m_Materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
        rendererSo.ApplyModifiedPropertiesWithoutUndo();

        var height = Mathf.Max(0.001f, mesh.bounds.size.y);
        visual.localScale = Vector3.one * (DutzSuperPunchPickupSetup.TargetVisualHeight / height);
        EditorUtility.SetDirty(visual.gameObject);
        return true;
    }

    static bool EnsurePickupInScene(UnityEngine.SceneManagement.Scene scene, bool log)
    {
        if (!scene.IsValid() || scene.path != DutzLevel02Setup.Level03ScenePath)
            return false;

        Physics.SyncTransforms();

        var pickup = GameObject.Find(DutzSuperPunchPickup.PickupObjectName);
        var changed = false;
        var created = pickup == null;
        if (pickup == null)
        {
            pickup = new GameObject(DutzSuperPunchPickup.PickupObjectName);
            Undo.RegisterCreatedObjectUndo(pickup, "Create Super Punch Pickup");
            changed = true;
        }

        if (created)
        {
            Undo.RecordObject(pickup.transform, "Reposition Super Punch Pickup");
            pickup.transform.SetPositionAndRotation(AuthoredMiddleDeckPosition, Quaternion.Euler(0f, 90f, 0f));
            changed = true;
        }
        else if ((pickup.transform.position - AuthoredMiddleDeckPosition).sqrMagnitude > 1f)
        {
            Undo.RecordObject(pickup.transform, "Restore Super Punch Pickup");
            pickup.transform.SetPositionAndRotation(AuthoredMiddleDeckPosition, Quaternion.Euler(0f, 90f, 0f));
            changed = true;
        }

        if (pickup.transform.localScale != Vector3.one)
        {
            Undo.RecordObject(pickup.transform, "Normalize Super Punch Pickup Scale");
            pickup.transform.localScale = Vector3.one;
            changed = true;
        }

        if (!DutzSuperPunchPickupSetup.HasSharedMeshVisual(pickup.transform))
        {
            AttachSharedVisual(pickup.transform);
            changed = true;
        }
        else
        {
            changed |= RepairGloveVisualMesh(pickup.transform);
        }

        DutzSuperPunchPickupSetup.Apply(pickup);

        if (pickup.GetComponent<DutzSuperPunchPickup>() == null)
        {
            Undo.AddComponent<DutzSuperPunchPickup>(pickup);
            changed = true;
        }

        if (!changed)
            return false;

        EditorUtility.SetDirty(pickup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Super Punch pickup restored on {BridgeSegmentName} middle deck at {pickup.transform.position}.");
        }

        return true;
    }

    static Mesh BakeCombinedMesh(GameObject source)
    {
        var filters = source.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
            return null;

        var combines = new CombineInstance[filters.Length];
        for (var i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null)
                continue;

            combines[i].mesh = filters[i].sharedMesh;
            combines[i].transform = filters[i].transform.localToWorldMatrix;
        }

        var combined = new Mesh
        {
            name = "DutzSuperPunchGlovesMesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combined.CombineMeshes(combines, mergeSubMeshes: true, useMatrices: true);
        combined.Optimize();
        combined.UploadMeshData(markNoLongerReadable: true);
        return combined;
    }

    static void ApplyOptimizedImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
            return;

        importer.globalScale = 1f;
        importer.meshCompression = ModelImporterMeshCompression.High;
        importer.isReadable = false;
        importer.importBlendShapes = false;
        importer.importVisibility = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.addCollider = false;
        importer.weldVertices = true;
        importer.optimizeMeshVertices = true;
        importer.optimizeMeshPolygons = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
    }

    static void EnsureGlovesMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialPath);
        if (existing != null)
        {
            if (!existing.enableInstancing)
            {
                existing.enableInstancing = true;
                EditorUtility.SetDirty(existing);
            }

            return;
        }

        EnsureAssetFolder("Assets/Characters/Level03/Materials");

        var material = new Material(Shader.Find("Standard"))
        {
            name = "DutzSuperPunchGloves",
            color = new Color(0.82f, 0.1f, 0.12f, 1f),
        };
        material.SetFloat("_Metallic", 0.08f);
        material.SetFloat("_Glossiness", 0.42f);
        material.enableInstancing = true;

        AssetDatabase.CreateAsset(material, GlovesMaterialPath);
    }

    static void CopyGlovesMaterialToResources()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialPath) == null)
            return;

        EnsureAssetFolder("Assets/Resources");
        const string resourcePath = "Assets/Resources/DutzSuperPunchGloves.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(resourcePath);
        if (existing != null)
            AssetDatabase.DeleteAsset(resourcePath);

        AssetDatabase.CopyAsset(GlovesMaterialPath, resourcePath);
    }

    static bool SyncSourceFbx(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve project root.");
            return false;
        }

        var source = Path.Combine(projectRoot, "public", SourceFileName);
        if (!File.Exists(source))
        {
            if (log)
                Debug.LogError("[Dutz] Missing public/" + SourceFileName + " â€” add the FBX and run again.");
            return false;
        }

        EnsureAssetFolder("Assets/Characters/Level03/Models");
        File.Copy(source, AssetFbxPath, overwrite: true);
        AssetDatabase.ImportAsset(AssetFbxPath, ImportAssetOptions.ForceUpdate);

        if (log)
            Debug.Log("[Dutz] Synced public/" + SourceFileName + " -> " + AssetFbxPath);

        return true;
    }

    static void EnsureAssetFolder(string path)
    {
        DutzHealthPotionPlacer.EnsureAssetFolder(path);
    }
}
