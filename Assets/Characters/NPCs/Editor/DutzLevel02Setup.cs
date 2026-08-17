using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates and prepares Dutz_Level01 from Showcase (batch: DutzLevel02Setup.SetupLevel02Batch).
/// </summary>
public static class DutzLevel02Setup
{
    public const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    public const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    public const string Level02ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    public const string Level03ScenePath = "Assets/Scenes/Dutz_Level03.unity";
    public const string Level07ScenePath = "Assets/Scenes/Dutz_Level07.unity";

    static readonly (string OldName, string NewName)[] GiantRenames =
    {
        (DutzGiantBossNames.PrincessZara, DutzGiantBossNames.GongBong),
        (DutzGiantBossNames.GeneralRook, DutzGiantBossNames.Tamby),
        (DutzGiantBossNames.Trililing, DutzGiantBossNames.ETol),
    };

    public static void SetupFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 2", "Exit Play mode first.", "OK");
            return;
        }

        if (!EnsureLevel02SceneAsset())
            return;

        if (!ApplyToLevel02(log: true))
        {
            EditorUtility.DisplayDialog("Level 2", "Could not prepare Dutz_Level01.", "OK");
            return;
        }

        RegisterInBuildSettings();
        EditorUtility.DisplayDialog(
            "Level 2",
            "Dutz_Level01 is ready.\n\n" +
            "- Giants: Gong Bong, Tamby, E-TOL\n" +
            "- Run Tools/Dutz/Apply Level 2 Game Content for suitcases, suit, crocs\n" +
            "- Build index 0 registered (Dutz_Level01 loads first)",
            "OK");
    }

    public static void ApplyLevel2GameContentMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 2", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyLevel2GameParity(log: true))
            EditorUtility.DisplayDialog("Level 2", "Could not apply Level 2 game content. Check Console.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzLevel02Setup.ApplyLevel2GameParityBatch</summary>
    public static void ApplyLevel2GameParityBatch() => ApplyLevel2GameParity(log: true);

    public static bool ApplyLevel2GameParity(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            if (!EnsureLevel02SceneAsset())
                return false;
        }

        var scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();

        if (!DutzGiantHippieBossFaceBuilder.ApplyGongBongFaceOnLevel02(log: log))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 Gong Bong face setup failed.");
        }

        if (!DutzGiantHippieBossFaceBuilder.ApplyTambyFaceOnLevel02(log: log))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 Tamby face setup failed.");
        }

        if (!DutzGiantHippieBossFaceBuilder.ApplyETolFaceOnLevel02(log: log))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 E-TOL face setup failed.");
        }

        if (!DutzSuitcasePlacer.DistributeOnLevel02(log: log))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 suitcase placement failed.");
        }

        if (!DutzForceFieldSuitPlacer.SetupOnLevel02(log: false))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 force field suit setup failed.");
        }

        if (!DutzShowcaseSceneRepair.EnsureEndHouseColliderOnLevel02(log: false))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 end house collider setup failed.");
        }

        if (!RemoveFlagPoleFromLevel01(log: false))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 1 flagpole removal failed.");
        }

        if (GameObject.Find("GrandmaGiantDialog") == null)
            DutzGiantWorldDialogBuilder.SetupGrandmaDialog(saveScene: false);

        if (!DutzCrocodileAddictBuilder.ApplySegmentCrocodilePoolToLevel02(log: false))
        {
            if (log)
                Debug.LogError("[Dutz] Level 2 crocodile pool apply failed.");
            return false;
        }

        Physics.SyncTransforms();
        scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
        if (!DutzSpawnSetup.SnapSpawnFieldsToBridgeStart(Level01ScenePath))
        {
            if (log)
                Debug.LogWarning("[Dutz] Level 2 Dutz spawn snap failed — check Highway Bridge 1.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                "[Dutz] Level 2 game content applied — suitcases, force field suit, end house, " +
                "grandma dialog, crocodile pool on road decks.");
        }

        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel02Setup.RemoveFlagPoleFromLevel01Batch</summary>
    public static void RemoveFlagPoleFromLevel01Batch() => RemoveFlagPoleFromLevel01(log: true);

    public static bool RemoveFlagPoleFromLevel01(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level01.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level01ScenePath)
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

        var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
        if (pole != null)
            Undo.DestroyObjectImmediate(pole);

        DutzShowcaseSceneRepair.EnsureEndHouseColliderOnLevel02(log: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                pole != null
                    ? "[Dutz] Removed FlagPole from Level 1 — roof win uses Building_House_04_color02."
                    : "[Dutz] Level 1 already has no FlagPole — ensured end house collider.");
        }

        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel02Setup.RemoveFlagPoleFromLevel02Batch</summary>
    public static void RemoveFlagPoleFromLevel02Batch() => RemoveFlagPoleFromLevel02(log: true);

    public static bool RemoveFlagPoleFromLevel02(bool log)
    {
        var scenePath = DutzShowcaseSceneRepair.Level02ScenePath;
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level02.unity not found.");
            return false;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != scenePath)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
        if (pole != null)
            Undo.DestroyObjectImmediate(pole);

        DutzShowcaseSceneRepair.EnsureEndHouseColliderOnScene(scenePath, log: false);

        DutzRobinCarMuralPlacer.RemoveFromLevel02IfPresent(scene, log: false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                pole != null
                    ? "[Dutz] Removed FlagPole from Level 2 — roof win uses Building_House_04_color02; Robin Car mural stripped (Level 1 only)."
                    : "[Dutz] Level 2 already has no FlagPole — ensured end house collider; Robin Car mural stripped (Level 1 only).");
        }

        return true;
    }

    /// <summary>Batch: -executeMethod DutzLevel02Setup.SetupLevel02Batch</summary>
    public static void SetupLevel02Batch() => SetupFromMenu();

    public static void BuildCrocodileAddictPrefabMenu() => DutzCrocodileAddictBuilder.BuildFromMenu();

    public static void ApplyLevel2CrocodilePoolMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Crocodile Pool", "Exit Play mode first.", "OK");
            return;
        }

        DutzCrocodileAddictBuilder.ApplyPoolBatch();
    }

    public static void ApplyLevel2CrocodilePoolSilentMenu() => DutzCrocodileAddictBuilder.ApplyPoolBatch();

    public static void ApplyGreenCrocodileSceneMeshMenu() => DutzCrocodileAddictBuilder.ApplyGreenSceneMeshFromMenu();

    static bool EnsureLevel02SceneAsset()
    {
        if (File.Exists(Level01ScenePath))
            return true;

        if (!File.Exists(Level02ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level02.unity not found — cannot duplicate Level 2.");
            return false;
        }

        if (!AssetDatabase.CopyAsset(Level02ScenePath, Level01ScenePath))
        {
            Debug.LogError("[Dutz] Failed to duplicate Dutz_Level02 → Dutz_Level01.");
            return false;
        }

        AssetDatabase.Refresh();
        Debug.Log("[Dutz] Created Dutz_Level01.unity from Dutz_Level02.");
        return true;
    }

    public static bool ApplyToLevel02(bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

        var renamed = 0;
        foreach (var (oldName, newName) in GiantRenames)
        {
            var go = GameObject.Find(oldName);
            if (go == null)
            {
                if (GameObject.Find(newName) != null)
                    renamed++;
                continue;
            }

            Undo.RecordObject(go, "Rename Level 2 giant");
            go.name = newName;
            renamed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 2 prepared — {renamed}/3 giants renamed (Gong Bong, Tamby, E-TOL).");
        }

        return renamed >= 3;
    }

    public static void RegisterInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(Level00ScenePath))
            scenes.Add(new EditorBuildSettingsScene(Level00ScenePath, true));

        scenes.Add(new EditorBuildSettingsScene(Level01ScenePath, true));
        scenes.Add(new EditorBuildSettingsScene(Level02ScenePath, true));

        if (File.Exists(Level03ScenePath))
            scenes.Add(new EditorBuildSettingsScene(Level03ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log(
            "[Dutz] Build Settings: index 0 Dutz_Level00 (when present), then L01, L02" +
            (File.Exists(Level03ScenePath) ? ", L03." : "."));
    }

    public static void DuplicateLevel03FromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Level 3", "Exit Play mode first.", "OK");
            return;
        }

        if (!DuplicateLevel03FromLevel01(log: true))
        {
            EditorUtility.DisplayDialog("Level 3", "Could not create Dutz_Level03. Check Console.", "OK");
            return;
        }

        RegisterInBuildSettings();
        EditorUtility.DisplayDialog(
            "Level 3",
            "Dutz_Level03 is ready — copied from Dutz_Level01.\n\n" +
            "Build index 2 registered (Dutz_Level03).",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzLevel02Setup.DuplicateLevel03FromLevel01Batch</summary>
    public static void DuplicateLevel03FromLevel01Batch() => DuplicateLevel03FromLevel01(log: true);

    public static bool DuplicateLevel03FromLevel01(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level01.unity not found — cannot duplicate Level 3.");
            return false;
        }

        if (File.Exists(Level03ScenePath))
        {
            if (log)
                Debug.Log("[Dutz] Dutz_Level03.unity already exists — skipping copy.");
            return true;
        }

        if (!AssetDatabase.CopyAsset(Level01ScenePath, Level03ScenePath))
        {
            Debug.LogError("[Dutz] Failed to duplicate Dutz_Level01 → Dutz_Level03.");
            return false;
        }

        AssetDatabase.Refresh();

        if (log)
            Debug.Log("[Dutz] Created Dutz_Level03.unity from Dutz_Level01.");

        return true;
    }
}

/// <summary>
/// Builds crocodile visual prefab for Level 2 segment pool (same chase/teleport as small hippies).
/// </summary>
public static class DutzCrocodileAddictBuilder
{
    const string SceneFbxPath = "Assets/Crocodile.fbx";
    const string CrocMaterialPath = "Assets/Characters/Level02/Materials/Crocodile.mat";
    const string PrefabPath = "Assets/Characters/Level02/Prefabs/DutzCrocodileAddict.prefab";
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";

    const string VisualChildName = "CrocVisual";
    const float SceneFbxVisualScale = 6f;
    static readonly Vector3 SceneFbxVisualEuler = new Vector3(-90f, 0f, -95.928f);
    const float SmallHippieChaseSpeed = 7f;
    const float SmallHippieChaseAnimSpeed = 0.66f;
    const string CrocDeathMessage = "A crocodile killed you!";

    public static void BuildFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Crocodile Addict", "Exit Play mode first.", "OK");
            return;
        }

        if (!BuildPrefab(log: true))
            EditorUtility.DisplayDialog("Crocodile Addict", "Could not build prefab. Check Console.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzCrocodileAddictBuilder.BuildPrefabBatch</summary>
    public static void BuildPrefabBatch() => BuildFromMenu();

    public static void ApplyPoolFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Crocodile Pool", "Exit Play mode first.", "OK");
            return;
        }

        ApplyPoolBatch();
    }

    /// <summary>Batch: -executeMethod DutzCrocodileAddictBuilder.ApplyPoolBatch</summary>
    public static void ApplyPoolBatch()
    {
        if (!BuildPrefab(log: false))
        {
            Debug.LogError("[Dutz] ApplyPoolBatch: could not build crocodile prefab.");
            return;
        }

        if (!DutzLevel02Setup.ApplyLevel2GameParity(log: true))
            Debug.LogError("[Dutz] ApplyPoolBatch: could not apply Level 2 game content.");
    }

    public static void ApplyGreenSceneMeshFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Crocodile", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyGreenMaterialToLevel02SceneCrocodile(log: true))
            EditorUtility.DisplayDialog("Crocodile", "Could not green the scene Crocodile. Check Console.", "OK");
    }

    /// <summary>Batch: -executeMethod DutzCrocodileAddictBuilder.ApplyGreenSceneMeshBatch</summary>
    public static void ApplyGreenSceneMeshBatch() => ApplyGreenSceneMeshFromMenu();

    public static bool ApplyGreenMaterialToLevel02SceneCrocodile(bool log)
    {
        EnsureSceneFbxUsesGreenMaterial();

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

        var crocodile = GameObject.Find("Crocodile");
        if (crocodile == null)
        {
            Debug.LogError("[Dutz] No GameObject named Crocodile in " + Level01ScenePath);
            return false;
        }

        var material = EnsureCrocodileMaterial();
        var rendererCount = 0;
        foreach (var renderer in crocodile.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            Undo.RecordObject(renderer, "Green Crocodile");
            var slots = renderer.sharedMaterials;
            for (var i = 0; i < slots.Length; i++)
                slots[i] = material;

            renderer.sharedMaterials = slots;
            rendererCount++;
        }

        EditorUtility.SetDirty(crocodile);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log($"[Dutz] Applied green Crocodile material to {rendererCount} renderer(s) in Dutz_Level01.");

        return rendererCount > 0;
    }

    static void EnsureSceneFbxUsesGreenMaterial()
    {
        var material = EnsureCrocodileMaterial();
        var importer = AssetImporter.GetAtPath(SceneFbxPath) as ModelImporter;
        if (importer == null)
            return;

        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Material"), material);
        importer.SaveAndReimport();
    }

    public static bool BuildPrefab(bool log)
    {
        EnsureSceneFbxUsesGreenMaterial();
        var fbxSource = AssetDatabase.LoadAssetAtPath<GameObject>(SceneFbxPath);
        if (fbxSource == null)
        {
            Debug.LogError("[Dutz] Missing scene crocodile model: " + SceneFbxPath);
            return false;
        }

        var material = EnsureCrocodileMaterial();
        EnsureAssetFolder("Assets/Characters/Level02/Prefabs");

        var root = new GameObject("DutzCrocodileAddict");
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxSource);
            visual.name = VisualChildName;
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.Euler(SceneFbxVisualEuler);
            visual.transform.localScale =
                Vector3.one * (SceneFbxVisualScale / DutzSmallAddictScale.BodyScale);

            ApplyGreenMaterialToRenderers(visual, material);
            StripVisualColliders(visual);

            SetupCrocodileAddict(root, isPoolMember: false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            if (log)
                Debug.Log("[Dutz] Saved " + PrefabPath + " from " + SceneFbxPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
    }

    public static bool ApplySegmentCrocodilePoolToLevel02(bool log)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing crocodile prefab: " + PrefabPath);
            return false;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);

        RemoveSegmentPoolAndManager();
        RemoveStandaloneSceneCrocodile();
        var removedHippies = SimpleCitizensHippieNpcSetup.RemoveBakedSmallHippiesFromActiveScene();

        var poolRoot = new GameObject(DutzSegmentHippieIdentity.PoolRootName);
        Undo.RegisterCreatedObjectUndo(poolRoot, "Create Crocodile Pool");

        for (var i = 0; i < DutzSegmentHippieIdentity.PoolCount; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(go, "Create Crocodile Addict");
            go.name = $"{DutzSegmentHippieIdentity.HippiePrefix}{i + 1:00}";
            go.transform.SetParent(poolRoot.transform, true);
            SetupCrocodileAddict(go, isPoolMember: true);
        }

        var teleportProfile = poolRoot.GetComponent<DutzSegmentHippieTeleportProfile>();
        if (teleportProfile == null)
            teleportProfile = Undo.AddComponent<DutzSegmentHippieTeleportProfile>(poolRoot);

        teleportProfile.ApplyIndividualAuthoredPositions();
        teleportProfile.SnapPoseHeightsToWalkableDeck();
        teleportProfile.CopyToHippieSlots(poolRoot.transform);
        teleportProfile.PlaceHippiesAtSegmentOne(poolRoot.transform);

        Physics.SyncTransforms();
        foreach (Transform child in poolRoot.transform)
        {
            if (!child.name.StartsWith(DutzSegmentHippieIdentity.HippiePrefix))
                continue;

            SnapCrocodilePivotToRoadDeck(child.gameObject);
        }

        foreach (Transform child in poolRoot.transform)
        {
            if (!child.name.StartsWith(DutzSegmentHippieIdentity.HippiePrefix))
                continue;

            var slots = child.GetComponent<DutzSegmentHippieTeleportSlots>();
            if (slots == null)
                continue;

            var placed = slots.GetPose(0);
            placed.position = child.position;
            placed.eulerAngles = child.eulerAngles;
            slots.SetPose(0, placed);
        }

        foreach (Transform child in poolRoot.transform)
        {
            if (!child.name.StartsWith(DutzSegmentHippieIdentity.HippiePrefix))
                continue;

            if (child.GetComponent<DutzSegmentHippieTeleportSlots>() == null)
                Undo.AddComponent<DutzSegmentHippieTeleportSlots>(child.gameObject);

            var hunter = child.GetComponent<SimpleCitizensHippieHunter>();
            if (hunter != null)
                ApplyPoolHunterSpeeds(hunter);

            ClearPoolRespawnSpawnPoint(child.GetComponent<SimpleCitizensNpcRespawn>());
        }

        EditorUtility.SetDirty(teleportProfile);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Level 2 crocodile pool: removed {removedHippies} baked hippie(s), " +
                $"added {DutzSegmentHippieIdentity.PoolCount} crocodiles at segment teleport positions.");
        }

        return true;
    }

    public static void SetupCrocodileAddict(GameObject go, bool isPoolMember)
    {
        if (go == null)
            return;

        DutzHippieBiteCollider.EnsureSmallHippieColliders(go);

        var physics = go.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = go.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();

        var physicsSo = new SerializedObject(physics);
        physicsSo.FindProperty("walkForward").boolValue = false;
        physicsSo.FindProperty("lockForwardToHighway").boolValue = false;
        physicsSo.FindProperty("walkSpeed").floatValue = SmallHippieChaseSpeed;
        physicsSo.FindProperty("animatorWalkSpeed").floatValue = SmallHippieChaseAnimSpeed;
        physicsSo.FindProperty("groundCheckDistance").floatValue = 0.6f;
        physicsSo.ApplyModifiedPropertiesWithoutUndo();

        if (go.GetComponent<SimpleCitizensHippieSounds>() == null)
            go.AddComponent<SimpleCitizensHippieSounds>();

        if (go.GetComponent<SimpleCitizensHippieBiter>() == null)
            go.AddComponent<SimpleCitizensHippieBiter>();

        var biterSo = new SerializedObject(go.GetComponent<SimpleCitizensHippieBiter>());
        biterSo.FindProperty("deathMessage").stringValue = CrocDeathMessage;
        biterSo.ApplyModifiedPropertiesWithoutUndo();

        var respawn = go.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = go.AddComponent<SimpleCitizensNpcRespawn>();

        var hunter = go.GetComponent<SimpleCitizensHippieHunter>();
        if (hunter == null)
            hunter = go.AddComponent<SimpleCitizensHippieHunter>();
        ApplyPoolHunterSpeeds(hunter);

        var giantHunter = go.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            Object.DestroyImmediate(giantHunter);

        if (isPoolMember || DutzSegmentHippieIdentity.IsPoolHippie(go.name))
            ClearPoolRespawnSpawnPoint(respawn);
        else
            respawn.RecordSpawnPoint();

        EditorUtility.SetDirty(go);
    }

    static void SnapCrocodilePivotToRoadDeck(GameObject go)
    {
        if (go == null)
            return;

        Physics.SyncTransforms();
        var col = go.GetComponent<Collider>();
        var pos = go.transform.position;
        var probe = new Vector3(pos.x, pos.y, pos.z);
        if (!DutzRoadGround.TrySampleRoadDeckForPlacement(probe, pos.y, col, out var deckY))
            return;

        DutzNpcFeet.PlacePivotOnSurface(go, deckY);
    }

    static void ApplyPoolHunterSpeeds(SimpleCitizensHippieHunter hunter)
    {
        if (hunter == null)
            return;

        var hso = new SerializedObject(hunter);
        hso.FindProperty("chaseSpeed").floatValue = SmallHippieChaseSpeed;
        hso.FindProperty("chaseAnimSpeed").floatValue = SmallHippieChaseAnimSpeed;
        hso.FindProperty("huntImmediately").boolValue =
            DutzSegmentHippieIdentity.IsPoolHippie(hunter.gameObject.name);
        hso.FindProperty("wakeDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieWakeDistance;
        hso.FindProperty("maxHuntDistance").floatValue = SimpleCitizensHippieHunter.SmallHippieMaxHuntDistance;
        hso.FindProperty("playerAheadAbandonDistance").floatValue = 8f;
        hso.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ClearPoolRespawnSpawnPoint(SimpleCitizensNpcRespawn respawn)
    {
        if (respawn == null)
            return;

        var so = new SerializedObject(respawn);
        so.FindProperty("spawnPointSet").boolValue = false;
        so.FindProperty("spawnPosition").vector3Value = Vector3.zero;
        so.FindProperty("spawnRotation").quaternionValue = Quaternion.identity;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ApplyGreenMaterialToRenderers(GameObject root, Material material)
    {
        if (root == null || material == null)
            return;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;

            var slots = renderer.sharedMaterials;
            for (var i = 0; i < slots.Length; i++)
                slots[i] = material;

            renderer.sharedMaterials = slots;
        }
    }

    static void StripVisualColliders(GameObject visual)
    {
        if (visual == null)
            return;

        foreach (var col in visual.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
    }

    static void RemoveStandaloneSceneCrocodile()
    {
        var croc = GameObject.Find("Crocodile");
        if (croc == null)
            return;

        Undo.DestroyObjectImmediate(croc);
    }

    static void RemoveSegmentPoolAndManager()
    {
        var pool = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (pool != null)
            Undo.DestroyObjectImmediate(pool);

        var manager = GameObject.Find(DutzSegmentHippieIdentity.ManagerObjectName);
        if (manager != null)
            Undo.DestroyObjectImmediate(manager);
    }

    static Material EnsureCrocodileMaterial()
    {
        EnsureAssetFolder("Assets/Characters/Level02/Materials");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(CrocMaterialPath);
        if (mat != null)
            return mat;

        var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
        mat = new Material(shader) { color = new Color(0.15f, 0.72f, 0.18f) };
        AssetDatabase.CreateAsset(mat, CrocMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static void EnsureAssetFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        var parts = assetFolder.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
