using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot builder for the standalone LEVEL FLOOD CONTROL scene.
/// </summary>
public static class FloodControlSceneBuilder
{
    const string ScenePath = "Assets/Scenes/LEVEL FLOOD CONTROL.unity";
    const string MaterialsFolder = "Assets/FloodControl/Materials";
    const string PrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";
    const string CrocodilePrefabPath =
        "Assets/Characters/Level02/Prefabs/DutzCrocodileAddict.prefab";
    const string RatSourceRelativePath = "public/Rat.fbx";
    const string RatAssetPath = "Assets/FloodControl/Models/Rat.fbx";
    const string SmallAddictPrefabPath =
        "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Hippie_Black.prefab";
    const string LastPipeSmallAddictName = "SimpleCitizens_Hippie_Black_FloodLastPipe";
    const string UpperForceFieldSuitName = "FloodForceFieldSuit_44_45";
    const string BottomHealthPotionName = "DutzHealthPotion_Flood_30_31";
    const string HealthPotionPrefabPath =
        "Assets/Characters/Level03/Prefabs/DutzHealthPotion.prefab";
    const string FloodMusicAssetPath =
        "Assets/cotton-toys-soundroll-main-version-16753-01-17.mp3";
    const string FloodMusicObjectName = "GameMusic";
    const string FloodVictoryRoadName = "Freeway Join Road";
    const string EdsaScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string FloodOpeningSourceFileName = "FLOOD CONTROL OPENING.MP4";
    const string FloodOpeningStreamingAssetPath =
        "Assets/StreamingAssets/FLOOD CONTROL OPENING.mp4";
    const string FloodOpeningRuntimeFileName = "FLOOD CONTROL OPENING.mp4";
    const string FloodCompletionSourceRelativePath = "public/Level_Flood_Complete.mp4";
    const string FloodCompletionStreamingAssetPath =
        "Assets/StreamingAssets/Level_Flood_Complete.mp4";
    const string FloodCompletionRuntimeFileName = "Level_Flood_Complete.mp4";

    const float CourseWidth = 6f;
    const float CourseHeight = 22f;
    const float FloorY = -4f;
    const int OriginalPipeSlotCount = 30;
    const int AdditionalPipeSlotCount = 20;
    const int PipeSlotCount = OriginalPipeSlotCount + AdditionalPipeSlotCount;
    const float FirstSlotX = 20f;
    const float OriginalLastSlotX = 290f;
    const float PipeSlotSpacing =
        (OriginalLastSlotX - FirstSlotX) / (OriginalPipeSlotCount - 1);
    const float LastSlotX = FirstSlotX + PipeSlotSpacing * (PipeSlotCount - 1);
    const float CourseEndMargin = 10f;
    const float CourseLength = LastSlotX + CourseEndMargin;
    const float PlayerStartX = 5f;
    const float PlayerStartY = 10f;
    const float PlayerMinY = -7f;
    const float PlayerMaxY = 27f;
    const float LockedZ = 0f;
    // Keep hazard roots at unit scale; size the visual to the in-scene ~7.2 m length.
    const float RatTargetLength = 7.2f;
    const float RatMovementSpeed = 4.5f;
    const float RatBodyColliderLength = 0.38f;
    const float RatBodyColliderThickness = 0.5f;
    const float RatBodyColliderForwardOffset = 0.2f;
    const float RatEndX = FirstSlotX;
    const string FishSourceFolderRelativePath = "public/FISHES";
    const string FishAssetFolder = "Assets/FloodControl/Models/Fishes";
    const float FishTargetLength = 2.5f;
    const float FishPatrolSpeed = 3.5f;
    const float FishPatrolMinX = PlayerStartX;
    const float FishPatrolMaxX = CourseLength;

    [MenuItem("Window/FloodControl/Build LEVEL FLOOD CONTROL Scene")]
    public static void BuildFromMenu()
    {
        Build();
    }

    [MenuItem("Window/FloodControl/Apply Planar Pickup Triggers")]
    public static void ApplyPlanarPickupTriggersFromMenu()
    {
        string[] pickupNames =
        {
            UpperForceFieldSuitName,
            BottomHealthPotionName,
            "DutzSuperPunchPickup"
        };

        int applied = 0;
        for (int i = 0; i < pickupNames.Length; i++)
        {
            GameObject pickup = GameObject.Find(pickupNames[i]);
            if (pickup == null)
            {
                Debug.LogWarning($"[FloodControl] Planar pickup not found: {pickupNames[i]}");
                continue;
            }

            Transform visual = pickup.transform.Find("GloveVisual");
            if (visual == null)
                visual = pickup.transform.Find("PotionModelVisual");

            FloodPlanarPickup.RecenterRootOnVisual(pickup.transform, visual);
            FloodPlanarPickup.SnapToPlayPlane(pickup.transform);
            FloodPlanarPickup.EnsureKinematicBody(pickup);
            FloodPlanarPickup.EnsureDeepTrigger(pickup);
            EditorUtility.SetDirty(pickup);
            applied++;
        }

        Scene active = SceneManager.GetActiveScene();
        if (active.IsValid())
            EditorSceneManager.MarkSceneDirty(active);

        Debug.Log($"[FloodControl] Applied planar pickup triggers to {applied}/{pickupNames.Length} pickups.");
    }

    [MenuItem("Window/FloodControl/Add Temporary Test Pipes")]
    public static void AddTemporaryTestPipesFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before adding test pipes.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before adding test pipes.");
            return;
        }

        GameObject slotsRoot = GameObject.Find("Environment/PipeSlots");
        if (slotsRoot == null)
        {
            Debug.LogError("[FloodControl] Environment/PipeSlots was not found.");
            return;
        }

        EnsureFolders();
        Material pipeMaterial = EnsureSteamPipeMaterial();
        Mesh pipeUnitMesh = EnsureTestPipeUnitMesh();

        PipeSlot[] slots = slotsRoot.GetComponentsInChildren<PipeSlot>(true);
        for (int i = 0; i < slots.Length; i++)
            AddTemporaryTestPipes(slots[i], i, pipeMaterial, pipeUnitMesh);

        EnsurePlayerHealthOnScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = slotsRoot;
        Debug.Log($"[FloodControl] Added temporary collidable pipes to {slots.Length} slots.");
    }

    [MenuItem("Window/FloodControl/Apply HP And Pipe Burn")]
    public static void ApplyHpAndPipeBurnFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying HP/burn.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying HP/burn.");
            return;
        }

        int pipeCount = EnsurePipeBurnOnScene();
        EnsurePlayerHealthOnScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[FloodControl] Applied FloodPlayerHealth and PipeBurn to {pipeCount} pipes.");
    }

    [MenuItem("Window/FloodControl/Apply Configured Pipe Gap")]
    public static void ApplyConfiguredPipeGapFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying the pipe gap.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying the pipe gap.");
            return;
        }

        GameManager manager = Object.FindObjectOfType<GameManager>();
        GameObject slotsRoot = GameObject.Find("Environment/PipeSlots");
        if (manager == null || slotsRoot == null)
        {
            Debug.LogError("[FloodControl] GameManager or PipeSlots was not found.");
            return;
        }

        float gap = manager.GetGapForCurrentLevel();
        PipeSlot[] slots = slotsRoot.GetComponentsInChildren<PipeSlot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            PipeSlot slot = slots[i];
            if (slot == null || slot.TopSpawn == null || slot.BottomSpawn == null)
                continue;

            float centreY = (slot.TopSpawn.position.y + slot.BottomSpawn.position.y) * 0.5f;
            slot.ApplyGap(centreY, gap);
        }

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log($"[FloodControl] Applied a {gap:0.##} metre gap to {slots.Length} spawn pairs.");
    }

    [MenuItem("Window/FloodControl/Apply Player Air Bubbles")]
    public static void ApplyPlayerAirBubblesFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying air bubbles.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying air bubbles.");
            return;
        }

        GameObject player = GameObject.Find(DutzPlayerController.PlayerObjectName);
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (player == null)
        {
            Debug.LogError("[FloodControl] Player1 was not found.");
            return;
        }

        EnsureFolders();
        EnsurePlayerAirBubbles(player, manager);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = player.transform.Find("AirBubbles")?.gameObject;
        Debug.Log("[FloodControl] Applied periodic air bubbles to Player1.");
    }

    [MenuItem("Window/FloodControl/Apply Crocodile Patrols")]
    public static void ApplyCrocodilePatrolsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying crocodile patrols.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying crocodile patrols.");
            return;
        }

        GameObject environment = GameObject.Find("Environment");
        GameObject slotsRoot = GameObject.Find("Environment/PipeSlots");
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (environment == null || slotsRoot == null || manager == null)
        {
            Debug.LogError("[FloodControl] Environment, PipeSlots, or GameManager was not found.");
            return;
        }

        PipeSlot[] slots = slotsRoot.GetComponentsInChildren<PipeSlot>(true);
        GameObject patrolRoot = BuildCrocodilePatrols(environment.transform, slots, manager);
        if (patrolRoot == null)
            return;

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = patrolRoot;
        Debug.Log("[FloodControl] Applied ten vertical crocodile patrols.");
    }

    [MenuItem("Window/FloodControl/Extend Course To 50 Pipe Slots")]
    public static void ExtendCourseToFiftyPipeSlotsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before extending the course.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before extending the course.");
            return;
        }

        Transform slotsRoot = GameObject.Find("Environment/PipeSlots")?.transform;
        if (slotsRoot == null)
        {
            Debug.LogError("[FloodControl] Environment/PipeSlots was not found.");
            return;
        }

        var orderedSlots = new PipeSlot[PipeSlotCount];
        for (int slotNumber = 1; slotNumber <= OriginalPipeSlotCount; slotNumber++)
        {
            Transform existing = slotsRoot.Find($"PipeSlot{slotNumber:00}");
            PipeSlot slot = existing != null ? existing.GetComponent<PipeSlot>() : null;
            if (slot == null)
            {
                Debug.LogError($"[FloodControl] PipeSlot{slotNumber:00} is missing.");
                return;
            }

            orderedSlots[slotNumber - 1] = slot;
        }

        for (int slotNumber = OriginalPipeSlotCount + 1;
             slotNumber <= PipeSlotCount;
             slotNumber++)
        {
            string targetName = $"PipeSlot{slotNumber:00}";
            Transform existing = slotsRoot.Find(targetName);
            GameObject slotObject;

            if (existing != null)
            {
                slotObject = existing.gameObject;
            }
            else
            {
                // Repeat the latest 20-slot obstacle layout while preserving all
                // manual spawn and pipe-unit offsets in the original 30 slots.
                int sourceSlotNumber = slotNumber - AdditionalPipeSlotCount;
                Transform source = slotsRoot.Find($"PipeSlot{sourceSlotNumber:00}");
                if (source == null)
                {
                    Debug.LogError(
                        $"[FloodControl] Pattern source PipeSlot{sourceSlotNumber:00} is missing.");
                    return;
                }

                slotObject = Object.Instantiate(source.gameObject, slotsRoot);
                slotObject.name = targetName;

                float x = FirstSlotX + (slotNumber - 1) * PipeSlotSpacing;
                Vector3 sourcePosition = source.localPosition;
                slotObject.transform.localPosition =
                    new Vector3(x, sourcePosition.y, sourcePosition.z);
            }

            slotObject.transform.SetSiblingIndex(slotNumber - 1);
            PipeSlot slot = slotObject.GetComponent<PipeSlot>();
            if (slot == null)
            {
                Debug.LogError($"[FloodControl] {targetName} has no PipeSlot component.");
                return;
            }

            orderedSlots[slotNumber - 1] = slot;
        }

        ConfigureCourseGeometryOnScene();
        ConfigurePipeGeneratorSlots(orderedSlots);
        ConfigurePlayerCourseBoundary();

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = slotsRoot.gameObject;
        Debug.Log(
            $"[FloodControl] Extended course to {PipeSlotCount} slots and " +
            $"{CourseLength:0.##} metres.");
    }

    [MenuItem("Window/FloodControl/Apply Rat Hazards And Timer")]
    public static void ApplyRatHazardsAndTimerFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying rat hazards.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying rat hazards.");
            return;
        }

        Transform environment = GameObject.Find("Environment")?.transform;
        GameManager manager = Object.FindObjectOfType<GameManager>();
        FloodPlayerHealth health = Object.FindObjectOfType<FloodPlayerHealth>();
        if (environment == null || manager == null || health == null)
        {
            Debug.LogError("[FloodControl] Environment, GameManager, or Player1 health was not found.");
            return;
        }

        EnsureFolders();
        GameObject ratRoot = BuildRatHazards(environment, manager);
        if (ratRoot == null)
            return;

        EnsureFloodGameTimer(manager, health);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = ratRoot;
        Debug.Log("[FloodControl] Applied nine one-pass rat hazards and a four-minute timer.");
    }

    [MenuItem("Window/FloodControl/Apply Fish School")]
    public static void ApplyFishSchoolFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before placing the fish school.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before placing the fish school.");
            return;
        }

        Transform environment = GameObject.Find("Environment")?.transform;
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (environment == null || manager == null)
        {
            Debug.LogError("[FloodControl] Environment or GameManager was not found.");
            return;
        }

        EnsureFolders();
        GameObject fishRoot = BuildFishSchool(environment, manager);
        if (fishRoot == null)
            return;

        EditorUtility.SetDirty(fishRoot);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = fishRoot;
        Debug.Log("[FloodControl] Applied nine cosmetic fish along the swimming course.");
    }

    [MenuItem("Window/FloodControl/Apply Last Pipe Small Addict")]
    public static void ApplyLastPipeSmallAddictFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before placing the small addict.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before placing the small addict.");
            return;
        }

        GameObject pipeUnit = GameObject.Find(
            "Environment/PipeSlots/PipeSlot50/BottomSpawn/BottomPipeUnit_TEST");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallAddictPrefabPath);
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        GameManager manager = Object.FindObjectOfType<GameManager>();
        if (pipeUnit == null || prefab == null || player == null || manager == null)
        {
            Debug.LogError(
                "[FloodControl] Last bottom pipe, Small Addict prefab, Player1, or GameManager was not found.");
            return;
        }

        Transform existing = pipeUnit.transform.Find(LastPipeSmallAddictName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject addict = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (addict == null)
        {
            Debug.LogError("[FloodControl] Could not instantiate the Level02 Small Addict prefab.");
            return;
        }

        addict.name = LastPipeSmallAddictName;
        addict.transform.SetParent(pipeUnit.transform, true);
        // The bottom unit's origin is the exposed flange surface. Use world rotation
        // so the character stands upright and initially faces the approaching player.
        addict.transform.SetPositionAndRotation(
            pipeUnit.transform.position,
            Quaternion.Euler(0f, -90f, 0f));
        DutzSmallAddictScale.Apply(addict);
        DutzSmallAddictColorfulLook.Apply(addict);

        DutzHippieBiteCollider.EnsureSmallHippieColliders(addict);

        SimpleCitizensNpcPhysics physics = addict.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics == null)
            physics = addict.AddComponent<SimpleCitizensNpcPhysics>();
        physics.Apply();

        SerializedObject physicsSo = new SerializedObject(physics);
        physicsSo.FindProperty("snapToGroundOnStart").boolValue = false;
        physicsSo.FindProperty("followGround").boolValue = false;
        physicsSo.FindProperty("chaseIn3D").boolValue = true;
        physicsSo.FindProperty("walkForward").boolValue = false;
        physicsSo.FindProperty("lockForwardToHighway").boolValue = false;
        physicsSo.FindProperty("walkSpeed").floatValue = 7f;
        physicsSo.FindProperty("animatorWalkSpeed").floatValue = 0.66f;
        physicsSo.ApplyModifiedPropertiesWithoutUndo();

        if (addict.GetComponent<SimpleCitizensHippieSounds>() == null)
            addict.AddComponent<SimpleCitizensHippieSounds>();

        SimpleCitizensHippieBiter biter = addict.GetComponent<SimpleCitizensHippieBiter>();
        if (biter == null)
            biter = addict.AddComponent<SimpleCitizensHippieBiter>();
        SerializedObject biterSo = new SerializedObject(biter);
        biterSo.FindProperty("deathMessage").stringValue = "An addict killed you!";
        biterSo.ApplyModifiedPropertiesWithoutUndo();

        // Flood Control has no ground-fall respawn behavior for this hazard.
        RemoveComponentsOfTypeName(addict, "SimpleCitizensNpcRespawn");

        SimpleCitizensHippieHunter hunter = addict.GetComponent<SimpleCitizensHippieHunter>();
        if (hunter == null)
            hunter = addict.AddComponent<SimpleCitizensHippieHunter>();
        SerializedObject hunterSo = new SerializedObject(hunter);
        hunterSo.FindProperty("huntImmediately").boolValue = true;
        hunterSo.FindProperty("wakeDistance").floatValue = 55f;
        hunterSo.FindProperty("chaseSpeed").floatValue = 7f;
        hunterSo.FindProperty("chaseAnimSpeed").floatValue = 0.66f;
        hunterSo.FindProperty("maxHuntDistance").floatValue = 55f;
        hunterSo.ApplyModifiedPropertiesWithoutUndo();

        SimpleCitizensGiantHippieHunter giantHunter =
            addict.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            Object.DestroyImmediate(giantHunter);

        FloodSmallAddictController floodController =
            addict.GetComponent<FloodSmallAddictController>();
        if (floodController == null)
            floodController = addict.AddComponent<FloodSmallAddictController>();
        floodController.Configure(7f, 55f, pipeUnit.transform.position.z, manager, player);
        floodController.ConfigureCombat(50);

        FloodPlayerPunch playerPunch = player.GetComponent<FloodPlayerPunch>();
        if (playerPunch == null)
            playerPunch = player.gameObject.AddComponent<FloodPlayerPunch>();
        playerPunch.Configure(10);

        EditorUtility.SetDirty(addict);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = addict;
        Debug.Log(
            "[FloodControl] Placed Level02 Small Addict on PipeSlot50 bottom flange " +
            "with original hunter/biter plus Flood compatibility.");
    }

    [MenuItem("Window/FloodControl/Remove Last Pipe Addict Fall Detection")]
    public static void RemoveLastPipeAddictFallDetectionFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before removing fall detection.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before removing fall detection.");
            return;
        }

        GameObject addict = GameObject.Find(
            "Environment/PipeSlots/PipeSlot50/BottomSpawn/BottomPipeUnit_TEST/"
            + LastPipeSmallAddictName);
        if (addict == null)
        {
            Debug.LogError("[FloodControl] The PipeSlot50 small addict was not found.");
            return;
        }

        RemoveComponentsOfTypeName(addict, "SimpleCitizensNpcRespawn");
        EditorUtility.SetDirty(addict);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("[FloodControl] Removed fall detection from the PipeSlot50 small addict.");
    }

    [MenuItem("Window/FloodControl/Apply Addict Punch Combat")]
    public static void ApplyAddictPunchCombatFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying addict punch combat.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying addict punch combat.");
            return;
        }

        GameObject addict = GameObject.Find(
            "Environment/PipeSlots/PipeSlot50/BottomSpawn/BottomPipeUnit_TEST/"
            + LastPipeSmallAddictName);
        GameObject player = GameObject.Find(DutzPlayerController.PlayerObjectName);
        if (addict == null || player == null)
        {
            Debug.LogError("[FloodControl] The PipeSlot50 addict or Player1 was not found.");
            return;
        }

        FloodSmallAddictController addictController =
            addict.GetComponent<FloodSmallAddictController>();
        if (addictController == null)
            addictController = addict.AddComponent<FloodSmallAddictController>();
        addictController.ConfigureCombat(50);

        FloodPlayerPunch playerPunch = player.GetComponent<FloodPlayerPunch>();
        if (playerPunch == null)
            playerPunch = player.AddComponent<FloodPlayerPunch>();
        playerPunch.Configure(10);

        EditorUtility.SetDirty(addict);
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log(
            "[FloodControl] Set the last-pipe addict to 50 HP, Player1 punch to 10 damage, " +
            "and made addict contact non-blocking.");
    }

    [MenuItem("Window/FloodControl/Apply Sound Effects")]
    public static void ApplySoundEffectsFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying sound effects.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying sound effects.");
            return;
        }

        int swim = EnsurePlayerFloodSounds();
        int crocs = EnsureCrocodileFloodSounds();
        int rats = EnsureRatFloodSounds();

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log(
            $"[FloodControl] Applied Flood SFX — player={swim}, crocs={crocs}, rats={rats}.");
    }

    [MenuItem("Window/FloodControl/Apply Game Music")]
    public static void ApplyGameMusicFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying game music.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying game music.");
            return;
        }

        GameObject music = EnsureFloodBackgroundMusic();
        if (music == null)
            return;

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = music;
        Debug.Log("[FloodControl] Applied uploaded MP3 as looping game music.");
    }

    [MenuItem("Window/FloodControl/Apply Opening Video")]
    public static void ApplyOpeningVideoFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying the opening video.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying the opening video.");
            return;
        }

        if (!EnsureFloodOpeningVideoFile())
            return;

        IntroSequenceController intro = Object.FindObjectOfType<IntroSequenceController>();
        if (intro == null)
        {
            Debug.LogError("[FloodControl] IntroSequenceController was not found.");
            return;
        }

        SerializedObject introSo = new SerializedObject(intro);
        introSo.FindProperty("videoFileName").stringValue = FloodOpeningRuntimeFileName;
        introSo.FindProperty("startIntroOnEnable").boolValue = true;
        introSo.FindProperty("prepareTimeoutSeconds").floatValue = 60f;
        introSo.FindProperty("welcomeMessage").stringValue =
            "WELCOME TO BATTLE OF THE HAGUE - FLOOD CONTROL";
        introSo.FindProperty("welcomeDurationSeconds").floatValue = 1f;
        introSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(intro);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = intro.gameObject;
        Debug.Log("[FloodControl] Replaced the timed intro with the opening video.");
    }

    [MenuItem("Window/FloodControl/Apply Victory Goal")]
    public static void ApplyVictoryGoalFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before applying the victory goal.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before applying the victory goal.");
            return;
        }

        GameObject road = GameObject.Find(FloodVictoryRoadName);
        if (road == null)
        {
            Debug.LogError("[FloodControl] Freeway Join Road was not found.");
            return;
        }

        GameManager manager = Object.FindObjectOfType<GameManager>();
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (manager == null || player == null)
        {
            Debug.LogError("[FloodControl] GameManager or Player1 was not found.");
            return;
        }

        Bounds roadBounds;
        if (!TryGetRendererBounds(road, out roadBounds))
        {
            roadBounds = new Bounds(road.transform.position, new Vector3(6f, 40f, 8f));
        }

        BoxCollider victoryTrigger = null;
        BoxCollider[] boxColliders = road.GetComponents<BoxCollider>();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            if (!boxColliders[i].isTrigger)
                continue;

            victoryTrigger = boxColliders[i];
            break;
        }
        if (victoryTrigger == null)
            victoryTrigger = road.AddComponent<BoxCollider>();

        const float triggerWidth = 6f;
        const float triggerHeight = 40f;
        const float triggerDepth = 8f;
        Vector3 worldCenter = new Vector3(
            roadBounds.min.x + triggerWidth * 0.5f,
            10f,
            0f);
        Vector3 scale = road.transform.lossyScale;
        scale = new Vector3(
            Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        victoryTrigger.center = road.transform.InverseTransformPoint(worldCenter);
        victoryTrigger.size = new Vector3(
            triggerWidth / scale.x,
            triggerHeight / scale.y,
            triggerDepth / scale.z);
        victoryTrigger.isTrigger = true;

        const float completionX = 483f;
        const float completionY = 26f;
        FloodVictoryGoal goal = road.GetComponent<FloodVictoryGoal>();
        if (goal == null)
            goal = road.AddComponent<FloodVictoryGoal>();
        goal.Configure(manager, player, completionX, completionY);

        SerializedObject goalSo = new SerializedObject(goal);
        goalSo.FindProperty("completionVideoFileName").stringValue = FloodCompletionRuntimeFileName;
        goalSo.FindProperty("prepareTimeoutSeconds").floatValue = 60f;
        goalSo.FindProperty("completionX").floatValue = completionX;
        goalSo.FindProperty("completionY").floatValue = completionY;
        goalSo.FindProperty("requirePositionGatesForTriggers").boolValue = true;
        goalSo.ApplyModifiedPropertiesWithoutUndo();

        if (!EnsureFloodCompletionVideoFile())
        {
            Debug.LogError("[FloodControl] Completion video was not copied to StreamingAssets.");
            return;
        }

        BoundaryLimiter limiter = player.GetComponent<BoundaryLimiter>();
        if (limiter != null)
        {
            SerializedObject limiterSo = new SerializedObject(limiter);
            SerializedProperty maxXProperty = limiterSo.FindProperty("maxX");
            SerializedProperty maxYProperty = limiterSo.FindProperty("maxY");
            maxXProperty.floatValue = completionX + 1f;
            maxYProperty.floatValue = Mathf.Max(
                maxYProperty.floatValue,
                completionY + 1f);
            limiterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(limiter);
        }

        EnsureSceneInBuildSettings(EdsaScenePath);
        EditorUtility.SetDirty(road);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = road;
        Debug.Log(
            $"[FloodControl] Victory requires X>{completionX:0.##} and Y>{completionY:0.##}, " +
            "then plays Level_Flood_Complete.mp4 before the dialog.");
    }

    static void EnsureSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path != scenePath)
                continue;

            if (!scenes[i].enabled)
            {
                scenes[i].enabled = true;
                EditorBuildSettings.scenes = scenes;
            }
            return;
        }

        System.Array.Resize(ref scenes, scenes.Length + 1);
        scenes[scenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = scenes;
    }

    [MenuItem("Window/FloodControl/Apply Upper Force Field Suit")]
    public static void ApplyUpperForceFieldSuitFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before placing the Force Field Suit.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before placing the suit.");
            return;
        }

        Transform top44 = GameObject.Find("Environment/PipeSlots/PipeSlot44/TopSpawn")?.transform;
        Transform top45 = GameObject.Find("Environment/PipeSlots/PipeSlot45/TopSpawn")?.transform;
        Transform environment = GameObject.Find("Environment")?.transform;
        if (top44 == null || top45 == null || environment == null)
        {
            Debug.LogError("[FloodControl] PipeSlot44/45 upper spawns or Environment were not found.");
            return;
        }

        Transform existing = environment.Find(UpperForceFieldSuitName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        Vector3 midpoint = (top44.position + top45.position) * 0.5f;
        // Keep the collectible visibly between the upper pipe openings.
        midpoint.y -= 3f;
        midpoint.z = LockedZ;

        GameObject suit = new GameObject(UpperForceFieldSuitName);
        suit.transform.SetParent(environment, true);
        suit.transform.SetPositionAndRotation(midpoint, Quaternion.identity);
        suit.transform.localScale = Vector3.one;

        DutzForceFieldSuitSetup.Apply(suit);
        if (suit.GetComponent<FloodForceFieldSuitPickup>() == null)
            suit.AddComponent<FloodForceFieldSuitPickup>();

        EditorUtility.SetDirty(suit);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = suit;
        Debug.Log(
            $"[FloodControl] Placed Force Field Suit between upper PipeSlots 44 and 45 at {midpoint}.");
    }

    [MenuItem("Window/FloodControl/Apply Bottom Health Potion")]
    public static void ApplyBottomHealthPotionFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before placing the health potion.");
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            Debug.LogError("[FloodControl] Open LEVEL FLOOD CONTROL before placing the potion.");
            return;
        }

        Transform bottom30 = GameObject.Find("Environment/PipeSlots/PipeSlot30/BottomSpawn")?.transform;
        Transform bottom31 = GameObject.Find("Environment/PipeSlots/PipeSlot31/BottomSpawn")?.transform;
        Transform environment = GameObject.Find("Environment")?.transform;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPotionPrefabPath);
        if (bottom30 == null || bottom31 == null || environment == null || prefab == null)
        {
            Debug.LogError(
                "[FloodControl] PipeSlot30/31 bottom spawns, Environment, or potion prefab was not found.");
            return;
        }

        Transform existing = environment.Find(BottomHealthPotionName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject namedExisting = GameObject.Find(BottomHealthPotionName);
        if (namedExisting != null)
            Object.DestroyImmediate(namedExisting);

        Vector3 midpoint = (bottom30.position + bottom31.position) * 0.5f;
        // Raise into the swim gap above the bottom flanges.
        midpoint.y += 3f;
        midpoint.z = LockedZ;

        GameObject potion = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (potion == null)
        {
            Debug.LogError("[FloodControl] Could not instantiate the green health potion prefab.");
            return;
        }

        potion.name = BottomHealthPotionName;
        potion.transform.SetParent(environment, true);
        potion.transform.SetPositionAndRotation(midpoint, Quaternion.identity);
        potion.transform.localScale = Vector3.one;

        DutzHealthPotionSetup.ApplyGreenVisual(potion);

        DutzHealthPotion campaignPotion = potion.GetComponent<DutzHealthPotion>();
        if (campaignPotion != null)
            Object.DestroyImmediate(campaignPotion);

        // Prefab may already have a collider/rigidbody; ensure Flood pickup owns the trigger.
        SphereCollider trigger = potion.GetComponent<SphereCollider>();
        if (trigger == null)
            trigger = potion.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.4f, 0f);
        trigger.radius = 0.85f;

        Rigidbody body = potion.GetComponent<Rigidbody>();
        if (body == null)
            body = potion.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;

        FloodHealthPotionPickup pickup = potion.GetComponent<FloodHealthPotionPickup>();
        if (pickup == null)
            pickup = potion.AddComponent<FloodHealthPotionPickup>();

        SerializedObject pickupSo = new SerializedObject(pickup);
        pickupSo.FindProperty("healAmount").intValue = DutzHealthPotion.DefaultHealAmount;
        pickupSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(potion);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Selection.activeGameObject = potion;
        Debug.Log(
            $"[FloodControl] Placed green health potion between bottom PipeSlots 30 and 31 at {midpoint}.");
    }

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[FloodControl] Stop Play Mode before building the scene.");
            return;
        }

        EnsureFolders();
        Material waterMat = EnsureColorMaterial("FloodWater", new Color(0.2f, 0.55f, 0.85f, 0.28f), true);
        Material wallMat = EnsureColorMaterial("FloodWall", new Color(0.25f, 0.35f, 0.45f, 1f), false);
        Material floorMat = EnsureColorMaterial("FloodFloor", new Color(0.2f, 0.3f, 0.35f, 1f), false);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject environment = new GameObject("Environment");
        GameObject pool = new GameObject("Pool");
        pool.transform.SetParent(environment.transform, false);

        GameObject pipeSlotsRoot = new GameObject("PipeSlots");
        pipeSlotsRoot.transform.SetParent(environment.transform, false);

        BuildPool(pool.transform, waterMat, wallMat, floorMat);
        PipeSlot[] slots = BuildPipeSlots(pipeSlotsRoot.transform);

        GameObject gameManagerGo = BuildGameManager(slots);
        GameManager manager = gameManagerGo.GetComponent<GameManager>();
        GameObject player = BuildPlayer(manager);
        BuildCrocodilePatrols(environment.transform, slots, manager);
        BuildRatHazards(environment.transform, manager);
        BuildFishSchool(environment.transform, manager);
        EnsureFloodGameTimer(manager, player.GetComponent<FloodPlayerHealth>());
        GameObject mainCamera = BuildCamera(player.transform);
        GameObject lighting = BuildLighting();
        GameObject intro = BuildIntro(manager);
        GameObject ui = new GameObject("UI");

        // Keep hierarchy tidy and deterministic.
        environment.transform.SetAsFirstSibling();
        player.transform.SetSiblingIndex(1);
        mainCamera.transform.SetSiblingIndex(2);
        gameManagerGo.transform.SetSiblingIndex(3);
        lighting.transform.SetSiblingIndex(4);
        intro.transform.SetSiblingIndex(5);
        ui.transform.SetSiblingIndex(6);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        AddSceneToBuildSettings(ScenePath);

        Selection.activeGameObject = player;
        Debug.Log("[FloodControl] Built scene at " + ScenePath + " with " + slots.Length + " pipe slots.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl"))
            AssetDatabase.CreateFolder("Assets", "FloodControl");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Materials"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Meshes"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Meshes");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Models"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Models");
        if (!AssetDatabase.IsValidFolder(FishAssetFolder))
            AssetDatabase.CreateFolder("Assets/FloodControl/Models", "Fishes");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Textures"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Textures");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Scripts"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Scripts");
        if (!AssetDatabase.IsValidFolder("Assets/FloodControl/Editor"))
            AssetDatabase.CreateFolder("Assets/FloodControl", "Editor");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static Material EnsureSteamPipeMaterial()
    {
        Material mat = EnsureColorMaterial(
            "FloodTestPipe",
            new Color(0.72f, 0.74f, 0.76f, 1f),
            false);
        mat.SetFloat("_Metallic", 0.8f);
        mat.SetFloat("_Glossiness", 0.62f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material EnsureColorMaterial(string name, Color color, bool transparent)
    {
        string path = MaterialsFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = Shader.Find("Standard");
        mat.color = color;
        mat.SetFloat("_Glossiness", 0.05f);
        mat.SetFloat("_Metallic", 0f);

        if (transparent)
        {
            // Standard shader Fade mode — cheap enough for a single corridor volume.
            mat.SetFloat("_Mode", 2f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        else
        {
            mat.SetFloat("_Mode", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Mesh EnsureTestPipeUnitMesh()
    {
        const string meshPath = "Assets/FloodControl/Meshes/FloodTestPipeUnit.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing != null)
            return existing;

        GameObject stemSource = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        GameObject flangeSource = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Mesh cylinder = stemSource.GetComponent<MeshFilter>().sharedMesh;

        var combine = new CombineInstance[2];
        combine[0] = new CombineInstance
        {
            mesh = cylinder,
            transform = Matrix4x4.TRS(
                new Vector3(0f, 15f, 0f),
                Quaternion.identity,
                new Vector3(1.5f, 15f, 1.5f))
        };
        combine[1] = new CombineInstance
        {
            mesh = flangeSource.GetComponent<MeshFilter>().sharedMesh,
            transform = Matrix4x4.TRS(
                new Vector3(0f, 0.6f, 0f),
                Quaternion.identity,
                new Vector3(2.1f, 0.6f, 2.1f))
        };

        var combined = new Mesh
        {
            name = "FloodTestPipeUnit"
        };
        combined.CombineMeshes(combine, true, true, false);
        combined.RecalculateBounds();
        combined.UploadMeshData(true);

        Object.DestroyImmediate(stemSource);
        Object.DestroyImmediate(flangeSource);
        AssetDatabase.CreateAsset(combined, meshPath);
        AssetDatabase.SaveAssets();
        return combined;
    }

    static Material EnsureBubbleMaterial()
    {
        const string texturePath = "Assets/FloodControl/Textures/FloodBubble.png";
        const string materialPath = "Assets/FloodControl/Materials/FloodBubbleParticle.mat";

        Texture2D bubbleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (bubbleTexture == null)
        {
            const int size = 32;
            var generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];
            var centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedRadius = Vector2.Distance(new Vector2(x, y), centre) / maxRadius;
                    float ring = Mathf.Clamp01(1f - Mathf.Abs(normalizedRadius - 0.78f) * 8f);
                    float fill = normalizedRadius <= 0.78f ? 0.1f : 0f;
                    float edgeFade = Mathf.Clamp01((1f - normalizedRadius) * 6f);
                    float alpha = Mathf.Max(ring * 0.9f, fill) * edgeFade;
                    pixels[y * size + x] = new Color(0.78f, 0.94f, 1f, alpha);
                }
            }

            generated.SetPixels(pixels);
            generated.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(texturePath), generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                importer.SaveAndReimport();
            }

            bubbleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Particles/Standard Unlit");
            material = new Material(shader)
            {
                name = "FloodBubbleParticle"
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = bubbleTexture;
        material.color = Color.white;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    static Material EnsureSmokeMaterial()
    {
        const string texturePath = "Assets/FloodControl/Textures/FloodSmoke.png";
        const string materialPath = "Assets/FloodControl/Materials/FloodSmokeParticle.mat";

        Texture2D smokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (smokeTexture == null)
        {
            const int size = 32;
            var generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];
            var centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxRadius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalizedRadius = Vector2.Distance(new Vector2(x, y), centre) / maxRadius;
                    float soft = Mathf.Clamp01(1f - normalizedRadius);
                    float alpha = soft * soft * 0.85f;
                    pixels[y * size + x] = new Color(0.85f, 0.87f, 0.9f, alpha);
                }
            }

            generated.SetPixels(pixels);
            generated.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(texturePath), generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                importer.SaveAndReimport();
            }

            smokeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Particles/Standard Unlit");
            material = new Material(shader)
            {
                name = "FloodSmokeParticle"
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = smokeTexture;
        material.color = Color.white;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    static void BuildPool(Transform parent, Material waterMat, Material wallMat, Material floorMat)
    {
        // Floor: Unity Plane is 10x10 units.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(parent, false);
        floor.transform.position = new Vector3(CourseLength * 0.5f, FloorY, LockedZ);
        floor.transform.localScale = new Vector3(CourseLength / 10f, 1f, CourseWidth / 10f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;
        Object.DestroyImmediate(floor.GetComponent<Collider>());

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
        water.name = "WaterVolume";
        water.transform.SetParent(parent, false);
        float waterHeight = CourseHeight - FloorY;
        float waterCentreY = (CourseHeight + FloorY) * 0.5f;
        water.transform.position = new Vector3(CourseLength * 0.5f, waterCentreY, LockedZ);
        water.transform.localScale = new Vector3(CourseLength, waterHeight, CourseWidth);
        var waterRenderer = water.GetComponent<Renderer>();
        waterRenderer.sharedMaterial = waterMat;
        // Visual-only volume — no collider cost.
        Object.DestroyImmediate(water.GetComponent<Collider>());

        // Soft back wall for silhouette (no collider).
        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "BackWall";
        backWall.transform.SetParent(parent, false);
        backWall.transform.position = new Vector3(CourseLength * 0.5f, waterCentreY, CourseWidth * 0.5f + 0.25f);
        backWall.transform.localScale = new Vector3(CourseLength, waterHeight, 0.5f);
        backWall.GetComponent<Renderer>().sharedMaterial = wallMat;
        Object.DestroyImmediate(backWall.GetComponent<Collider>());
    }

    static void ConfigureCourseGeometryOnScene()
    {
        Transform floor = GameObject.Find("Environment/Pool/Floor")?.transform;
        Transform water = GameObject.Find("Environment/Pool/WaterVolume")?.transform;
        Transform backWall = GameObject.Find("Environment/Pool/BackWall")?.transform;

        if (floor == null || water == null || backWall == null)
        {
            Debug.LogError("[FloodControl] Floor, WaterVolume, or BackWall was not found.");
            return;
        }

        float centreX = CourseLength * 0.5f;

        Vector3 floorPosition = floor.position;
        floorPosition.x = centreX;
        floor.position = floorPosition;
        Vector3 floorScale = floor.localScale;
        floorScale.x = CourseLength / 10f;
        floor.localScale = floorScale;

        Vector3 waterPosition = water.position;
        waterPosition.x = centreX;
        water.position = waterPosition;
        Vector3 waterScale = water.localScale;
        waterScale.x = CourseLength;
        water.localScale = waterScale;

        Vector3 wallPosition = backWall.position;
        wallPosition.x = centreX;
        backWall.position = wallPosition;
        Vector3 wallScale = backWall.localScale;
        wallScale.x = CourseLength;
        backWall.localScale = wallScale;
    }

    static void ConfigurePipeGeneratorSlots(PipeSlot[] slots)
    {
        PipeGenerator generator = Object.FindObjectOfType<PipeGenerator>();
        if (generator == null)
        {
            Debug.LogError("[FloodControl] PipeGenerator was not found.");
            return;
        }

        SerializedObject generatorSo = new SerializedObject(generator);
        SerializedProperty slotsProperty = generatorSo.FindProperty("slots");
        slotsProperty.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        generatorSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ConfigurePlayerCourseBoundary()
    {
        BoundaryLimiter limiter = Object.FindObjectOfType<BoundaryLimiter>();
        if (limiter == null)
        {
            Debug.LogError("[FloodControl] Player BoundaryLimiter was not found.");
            return;
        }

        SerializedObject limiterSo = new SerializedObject(limiter);
        limiterSo.FindProperty("maxX").floatValue = CourseLength;
        limiterSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static PipeSlot[] BuildPipeSlots(Transform parent)
    {
        var slots = new PipeSlot[PipeSlotCount];
        float span = LastSlotX - FirstSlotX;

        for (int i = 0; i < PipeSlotCount; i++)
        {
            float t = PipeSlotCount == 1 ? 0f : i / (float)(PipeSlotCount - 1);
            float x = FirstSlotX + span * t;

            GameObject slotGo = new GameObject($"PipeSlot{(i + 1):00}");
            slotGo.transform.SetParent(parent, false);
            slotGo.transform.position = new Vector3(x, 0f, LockedZ);

            GameObject top = new GameObject("TopSpawn");
            top.transform.SetParent(slotGo.transform, false);
            top.transform.localPosition = new Vector3(0f, 19.4f, 0f);

            GameObject bottom = new GameObject("BottomSpawn");
            bottom.transform.SetParent(slotGo.transform, false);
            bottom.transform.localPosition = new Vector3(0f, -1.4f, 0f);

            PipeSlot slot = slotGo.AddComponent<PipeSlot>();
            slot.SetSpawnReferences(top.transform, bottom.transform);
            slots[i] = slot;
        }

        return slots;
    }

    static GameObject BuildRatHazards(Transform environment, GameManager manager)
    {
        if (environment == null)
            return null;

        GameObject ratAsset = EnsureRatAsset();
        if (ratAsset == null)
            return null;

        Transform existing = environment.Find("RatHazards");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("RatHazards");
        root.transform.SetParent(environment, false);

        Material ratMaterial = EnsureColorMaterial(
            "FloodRat",
            new Color(0.32f, 0.22f, 0.16f, 1f),
            false);

        // Globally staggered by two pipe spacings so no two rats share an arrival time.
        float firstX = FirstSlotX + 33f * PipeSlotSpacing;
        float horizontalSpacing = PipeSlotSpacing * 2f;
        float[] rowY = { 16f, 10f, 4f };
        int[] rowCounts = new int[3];

        for (int i = 0; i < 9; i++)
        {
            int rowIndex = i % 3;
            rowCounts[rowIndex]++;
            float x = firstX + i * horizontalSpacing;
            Vector3 start = new Vector3(x, rowY[rowIndex], LockedZ);
            Vector3 end = new Vector3(RatEndX, rowY[rowIndex], LockedZ);

            string rowName = rowIndex == 0
                ? "Top"
                : rowIndex == 1
                    ? "Middle"
                    : "Bottom";

            GameObject hazard = new GameObject($"Rat{rowName}{rowCounts[rowIndex]:00}");
            hazard.transform.SetParent(root.transform, false);
            hazard.transform.SetPositionAndRotation(start, Quaternion.identity);
            hazard.transform.localScale = Vector3.one;

            GameObject visual = PrefabUtility.InstantiatePrefab(ratAsset) as GameObject;
            if (visual == null)
            {
                Object.DestroyImmediate(hazard);
                continue;
            }

            visual.name = "RatVisual";
            visual.transform.SetParent(hazard.transform, false);
            visual.transform.localPosition = Vector3.zero;
            // Put the imported local-Y length along X, then roll it so the camera
            // looking down Z sees the rat's side profile.
            visual.transform.localRotation =
                Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 0f, -90f);
            visual.transform.localScale = Vector3.one;

            ConfigureRatRenderers(visual, ratMaterial);
            if (!NormalizeRatVisual(hazard.transform, visual.transform, out Bounds bounds))
            {
                Debug.LogError($"[FloodControl] Rat renderer bounds were not found on {hazard.name}.");
                Object.DestroyImmediate(hazard);
                continue;
            }

            BoxCollider trigger = hazard.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            Vector3 colliderSize = hazard.transform.InverseTransformVector(bounds.size);
            Vector3 localBoundsCenter = hazard.transform.InverseTransformPoint(bounds.center);
            // Rats travel toward -X. Shift the trigger into the forward torso/head
            // and shorten it enough that the trailing tail cannot kill Player1.
            trigger.center = localBoundsCenter
                + Vector3.left * (Mathf.Abs(colliderSize.x) * RatBodyColliderForwardOffset);
            trigger.size = new Vector3(
                Mathf.Max(0.05f, Mathf.Abs(colliderSize.x) * RatBodyColliderLength),
                Mathf.Max(0.05f, Mathf.Abs(colliderSize.y) * RatBodyColliderThickness),
                Mathf.Max(0.05f, Mathf.Abs(colliderSize.z) * RatBodyColliderThickness));

            Rigidbody body = hazard.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            FloodRatMover mover = hazard.AddComponent<FloodRatMover>();
            mover.Configure(start, end, RatMovementSpeed, manager);
            hazard.AddComponent<FloodRatKill>();
            hazard.AddComponent<FloodRatSounds>();
        }

        return root;
    }

    static GameObject EnsureRatAsset()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[FloodControl] Unity project root could not be resolved.");
            return null;
        }

        string sourcePath = Path.Combine(
            projectRoot,
            RatSourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string destinationPath = Path.Combine(
            projectRoot,
            RatAssetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(sourcePath))
        {
            Debug.LogError("[FloodControl] Uploaded rat FBX was not found at " + sourcePath);
            return null;
        }

        bool shouldCopy = !File.Exists(destinationPath)
            || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath);
        if (shouldCopy)
        {
            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(
                RatAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        GameObject ratAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RatAssetPath);
        if (ratAsset == null)
            Debug.LogError("[FloodControl] Unity could not import " + RatAssetPath);
        return ratAsset;
    }

    static GameObject BuildFishSchool(Transform environment, GameManager manager)
    {
        if (environment == null)
            return null;

        Transform existing = environment.Find("FishSchool");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("FishSchool");
        root.transform.SetParent(environment, false);

        // Cosmetic swim lanes: start fish go toward course end; end fish go toward start.
            var placements = new[]
            {
                new FishPlacement(
                    "FishStartTop01",
                    "Fish.fbx",
                    new Vector3(PlayerStartX + 8f, 20f, LockedZ),
                    true,
                    "FloodFish_Sunshine",
                    new Color(1f, 0.85f, 0.15f),
                    new Color(1f, 0.45f, 0.05f),
                    FishDecorPattern.HorizontalStripes),
                new FishPlacement(
                    "FishStartTop02",
                    "coralfish.fbx",
                    new Vector3(PlayerStartX + 12f, 18f, LockedZ),
                    true,
                    "FloodFish_CoralBloom",
                    new Color(1f, 0.35f, 0.55f),
                    new Color(0.95f, 0.75f, 0.2f),
                    FishDecorPattern.Spots),
                new FishPlacement(
                    "FishStartBottom01",
                    "platy.fbx",
                    new Vector3(PlayerStartX + 14f, 2f, LockedZ),
                    true,
                    "FloodFish_Lagoon",
                    new Color(0.15f, 0.85f, 0.9f),
                    new Color(0.1f, 0.35f, 0.95f),
                    FishDecorPattern.VerticalStripes),
                new FishPlacement(
                    "FishStartBottom02",
                    "Fish_Low_Poly.fbx",
                    new Vector3(PlayerStartX + 18f, 0f, LockedZ),
                    true,
                    "FloodFish_LimePop",
                    new Color(0.45f, 1f, 0.25f),
                    new Color(0.05f, 0.45f, 0.15f),
                    FishDecorPattern.Checker),
                new FishPlacement(
                    "FishEndUp01",
                    "EmperorAngelfish+FBX.fbx",
                    new Vector3(CourseLength - 20f, 20f, LockedZ),
                    false,
                    "FloodFish_Emperor",
                    new Color(0.15f, 0.35f, 1f),
                    new Color(1f, 0.95f, 0.2f),
                    FishDecorPattern.DiagonalStripes),
                new FishPlacement(
                    "FishEndUp02",
                    "Orange+Fish.fbx",
                    new Vector3(CourseLength - 16f, 18f, LockedZ),
                    false,
                    "FloodFish_Tangerine",
                    new Color(1f, 0.4f, 0.05f),
                    new Color(1f, 0.85f, 0.35f),
                    FishDecorPattern.HorizontalStripes),
                new FishPlacement(
                    "FishEndBottom01",
                    "fish_HP.fbx",
                    new Vector3(CourseLength - 14f, 2f, LockedZ),
                    false,
                    "FloodFish_VioletWave",
                    new Color(0.65f, 0.25f, 1f),
                    new Color(0.95f, 0.45f, 0.9f),
                    FishDecorPattern.Spots),
                new FishPlacement(
                    "FishEndBottom02",
                    "crab.fbx",
                    new Vector3(CourseLength - 10f, 0f, LockedZ),
                    false,
                    "FloodFish_CrabCarnival",
                    new Color(0.95f, 0.12f, 0.08f),
                    new Color(1f, 0.75f, 0.15f),
                    FishDecorPattern.Bands),
                new FishPlacement(
                    "FishMiddleTrackEnd",
                    "3d-model.fbx",
                    new Vector3(CourseLength - 8f, 10f, LockedZ),
                    false,
                    "FloodFish_RainbowRibbon",
                    new Color(1f, 0.2f, 0.55f),
                    new Color(0.2f, 0.95f, 0.55f),
                    FishDecorPattern.RainbowBands),
            };

        int placed = 0;
        for (int i = 0; i < placements.Length; i++)
        {
            FishPlacement placement = placements[i];
            GameObject fishAsset = EnsureFishAsset(placement.fileName);
            if (fishAsset == null)
                continue;

            Vector3 spawn = placement.spawnPosition;
            Vector3 farPoint = new Vector3(
                placement.swimsTowardEnd ? FishPatrolMaxX : FishPatrolMinX,
                spawn.y,
                LockedZ);
            // Start fish begin near the left and swim right; end fish begin near the right
            // and swim left. Both reverse at the course X bounds.
            Vector3 patrolStart = spawn;
            Vector3 patrolEnd = farPoint;

            GameObject fish = new GameObject(placement.objectName);
            fish.transform.SetParent(root.transform, false);
            fish.transform.SetPositionAndRotation(patrolStart, Quaternion.identity);
            fish.transform.localScale = Vector3.one;

            GameObject visual = PrefabUtility.InstantiatePrefab(fishAsset) as GameObject;
            if (visual == null)
            {
                Object.DestroyImmediate(fish);
                continue;
            }

            visual.name = "FishVisual";
            visual.transform.SetParent(fish.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Side-view camera looks down -Z; put the longest axis along X for a profile.
            visual.transform.localRotation =
                Quaternion.Euler(0f, 90f, 0f);

            StripImportedColliders(visual);
            Material decorMaterial = EnsureFishDecorMaterial(
                placement.materialName,
                placement.primaryColor,
                placement.secondaryColor,
                placement.pattern);
            OptimizeFishRenderers(visual, decorMaterial);
            if (!NormalizeFishVisual(fish.transform, visual.transform, out _))
            {
                Debug.LogError(
                    $"[FloodControl] Fish renderer bounds were not found on {fish.name}.");
                Object.DestroyImmediate(fish);
                continue;
            }

            Rigidbody body = fish.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.detectCollisions = false;

            FloodFishPatrol patrol = fish.AddComponent<FloodFishPatrol>();
            patrol.Configure(patrolStart, patrolEnd, FishPatrolSpeed, manager);
            placed++;
        }

        if (placed == 0)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("[FloodControl] No fish assets could be imported or placed.");
            return null;
        }

        Debug.Log($"[FloodControl] Placed {placed} cosmetic fish under Environment/FishSchool.");
        return root;
    }

    enum FishDecorPattern
    {
        HorizontalStripes = 0,
        VerticalStripes = 1,
        Spots = 2,
        Checker = 3,
        DiagonalStripes = 4,
        Bands = 5,
        RainbowBands = 6
    }

    readonly struct FishPlacement
    {
        public readonly string objectName;
        public readonly string fileName;
        public readonly Vector3 spawnPosition;
        public readonly bool swimsTowardEnd;
        public readonly string materialName;
        public readonly Color primaryColor;
        public readonly Color secondaryColor;
        public readonly FishDecorPattern pattern;

        public FishPlacement(
            string objectName,
            string fileName,
            Vector3 spawnPosition,
            bool swimsTowardEnd,
            string materialName,
            Color primaryColor,
            Color secondaryColor,
            FishDecorPattern pattern)
        {
            this.objectName = objectName;
            this.fileName = fileName;
            this.spawnPosition = spawnPosition;
            this.swimsTowardEnd = swimsTowardEnd;
            this.materialName = materialName;
            this.primaryColor = primaryColor;
            this.secondaryColor = secondaryColor;
            this.pattern = pattern;
        }
    }

    static GameObject EnsureFishAsset(string fileName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[FloodControl] Unity project root could not be resolved.");
            return null;
        }

        string sourcePath = Path.Combine(
            projectRoot,
            FishSourceFolderRelativePath.Replace('/', Path.DirectorySeparatorChar),
            fileName);
        string assetPath = FishAssetFolder + "/" + fileName;
        string destinationPath = Path.Combine(
            projectRoot,
            assetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(sourcePath))
        {
            Debug.LogError("[FloodControl] Fish FBX was not found at " + sourcePath);
            return null;
        }

        EnsureFolders();
        bool shouldCopy = !File.Exists(destinationPath)
            || File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destinationPath);
        if (shouldCopy)
        {
            File.Copy(sourcePath, destinationPath, true);
            AssetDatabase.ImportAsset(
                assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        GameObject fishAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (fishAsset == null)
            Debug.LogError("[FloodControl] Unity could not import " + assetPath);
        return fishAsset;
    }

    static void StripImportedColliders(GameObject visual)
    {
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                Object.DestroyImmediate(colliders[i]);
        }
    }

    static void OptimizeFishRenderers(GameObject visual, Material decorMaterial)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (decorMaterial != null)
            {
                var materials = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
                for (int m = 0; m < materials.Length; m++)
                    materials[m] = decorMaterial;
                renderer.sharedMaterials = materials;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode =
                UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    static Material EnsureFishDecorMaterial(
        string name,
        Color primary,
        Color secondary,
        FishDecorPattern pattern)
    {
        string texturePath = "Assets/FloodControl/Textures/" + name + ".png";
        string materialPath = MaterialsFolder + "/" + name + ".mat";

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            const int size = 64;
            var generated = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    pixels[y * size + x] = SampleFishDecorColor(
                        u,
                        v,
                        primary,
                        secondary,
                        pattern);
                }
            }

            generated.SetPixels(pixels);
            generated.Apply(false, false);
            File.WriteAllBytes(Path.GetFullPath(texturePath), generated.EncodeToPNG());
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(
                texturePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.sRGBTexture = true;
                importer.textureCompression = TextureImporterCompression.CompressedLQ;
                importer.SaveAndReimport();
            }

            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"))
            {
                name = name
            };
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = Shader.Find("Standard");
        material.mainTexture = texture;
        material.color = Color.white;
        material.SetFloat("_Glossiness", 0.55f);
        material.SetFloat("_Metallic", 0.05f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", primary * 0.18f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    static Color SampleFishDecorColor(
        float u,
        float v,
        Color primary,
        Color secondary,
        FishDecorPattern pattern)
    {
        switch (pattern)
        {
            case FishDecorPattern.HorizontalStripes:
                return ((int)(v * 8f) % 2 == 0) ? primary : secondary;
            case FishDecorPattern.VerticalStripes:
                return ((int)(u * 8f) % 2 == 0) ? primary : secondary;
            case FishDecorPattern.Spots:
            {
                float cellU = (u * 6f) % 1f - 0.5f;
                float cellV = (v * 6f) % 1f - 0.5f;
                return (cellU * cellU + cellV * cellV) < 0.08f ? secondary : primary;
            }
            case FishDecorPattern.Checker:
                return (((int)(u * 6f) + (int)(v * 6f)) % 2 == 0) ? primary : secondary;
            case FishDecorPattern.DiagonalStripes:
                return ((int)((u + v) * 10f) % 2 == 0) ? primary : secondary;
            case FishDecorPattern.Bands:
                return ((int)(v * 5f) % 2 == 0) ? primary : secondary;
            case FishDecorPattern.RainbowBands:
            {
                float hue = Mathf.Repeat(u * 1.4f + v * 0.35f, 1f);
                return Color.HSVToRGB(hue, 0.85f, 1f);
            }
            default:
                return primary;
        }
    }

    static bool NormalizeFishVisual(
        Transform host,
        Transform visual,
        out Bounds normalizedBounds)
    {
        if (!TryGetRendererBounds(visual.gameObject, out Bounds initialBounds))
        {
            normalizedBounds = default;
            return false;
        }

        Vector3 size = initialBounds.size;
        float largestDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (largestDimension <= 0.0001f)
        {
            normalizedBounds = default;
            return false;
        }

        visual.localScale = Vector3.one * (FishTargetLength / largestDimension);
        if (!TryGetRendererBounds(visual.gameObject, out normalizedBounds))
            return false;

        Vector3 worldOffset = host.position - normalizedBounds.center;
        visual.localPosition += host.InverseTransformVector(worldOffset);
        return TryGetRendererBounds(visual.gameObject, out normalizedBounds);
    }

    static void ConfigureRatRenderers(GameObject visual, Material material)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode =
                UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
        }
    }

    static bool NormalizeRatVisual(
        Transform hazard,
        Transform visual,
        out Bounds normalizedBounds)
    {
        if (!TryGetRendererBounds(visual.gameObject, out Bounds initialBounds))
        {
            normalizedBounds = default;
            return false;
        }

        Vector3 size = initialBounds.size;
        float largestDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        if (largestDimension <= 0.0001f)
        {
            normalizedBounds = default;
            return false;
        }

        visual.localScale = Vector3.one * (RatTargetLength / largestDimension);
        if (!TryGetRendererBounds(visual.gameObject, out normalizedBounds))
            return false;

        Vector3 worldOffset = hazard.position - normalizedBounds.center;
        visual.localPosition += hazard.InverseTransformVector(worldOffset);
        return TryGetRendererBounds(visual.gameObject, out normalizedBounds);
    }

    static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool found = false;
        bounds = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    static void EnsureFloodGameTimer(
        GameManager manager,
        FloodPlayerHealth health)
    {
        if (manager == null || health == null)
            return;

        FloodGameTimer timer = manager.GetComponent<FloodGameTimer>();
        if (timer == null)
            timer = manager.gameObject.AddComponent<FloodGameTimer>();
        timer.Configure(240f, manager, health);
    }

    static GameObject BuildCrocodilePatrols(
        Transform environment,
        PipeSlot[] slots,
        GameManager manager)
    {
        if (environment == null || slots == null || slots.Length < PipeSlotCount)
        {
            Debug.LogError(
                $"[FloodControl] All {PipeSlotCount} pipe slots are required for crocodile placement.");
            return null;
        }

        GameObject crocodilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrocodilePrefabPath);
        if (crocodilePrefab == null)
        {
            Debug.LogError("[FloodControl] Crocodile prefab was not found at " + CrocodilePrefabPath);
            return null;
        }

        Transform existing = environment.Find("CrocodilePatrols");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = new GameObject("CrocodilePatrols");
        root.transform.SetParent(environment, false);

        // Existing pairs: 04-05, 08-09, 14-15, 18-19, 22-23, and 27-28.
        // Extended-course pairs: 33-34, 38-39, 43-44, and 48-49.
        // Alternating directions produce two top and two bottom patrols in slots 31-50.
        int[] leftSlotIndices = { 3, 7, 13, 17, 21, 26, 32, 37, 42, 47 };
        const float patrolRangeExtension = 5f;
        const float patrolSpeed = 1.75f;
        int topNumber = 0;
        int bottomNumber = 0;

        for (int i = 0; i < leftSlotIndices.Length; i++)
        {
            PipeSlot left = slots[leftSlotIndices[i]];
            PipeSlot right = slots[leftSlotIndices[i] + 1];
            bool startsAtTop = i % 2 == 0;

            float x = (left.transform.position.x + right.transform.position.x) * 0.5f;
            // Push start/end farther past the pipe openings to lengthen the vertical run.
            float topY = Mathf.Min(
                GetPipeOpeningY(left, true),
                GetPipeOpeningY(right, true)) + patrolRangeExtension;
            float bottomY = Mathf.Max(
                GetPipeOpeningY(left, false),
                GetPipeOpeningY(right, false)) - patrolRangeExtension;
            topY = Mathf.Min(topY, CourseHeight + 4f);
            bottomY = Mathf.Max(bottomY, FloorY + 0.5f);
            if (topY <= bottomY + 1f)
            {
                float mid = (topY + bottomY) * 0.5f;
                topY = mid + 0.5f;
                bottomY = mid - 0.5f;
            }

            Vector3 topPoint = new Vector3(x, topY, LockedZ);
            Vector3 bottomPoint = new Vector3(x, bottomY, LockedZ);
            Vector3 start = startsAtTop ? topPoint : bottomPoint;
            Vector3 end = startsAtTop ? bottomPoint : topPoint;

            GameObject crocodile = PrefabUtility.InstantiatePrefab(crocodilePrefab) as GameObject;
            if (crocodile == null)
                continue;

            crocodile.transform.SetParent(root.transform, true);
            PrefabUtility.UnpackPrefabInstance(
                crocodile,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            if (startsAtTop)
            {
                topNumber++;
                crocodile.name = $"TopCrocodile{topNumber:00}";
            }
            else
            {
                bottomNumber++;
                crocodile.name = $"BottomCrocodile{bottomNumber:00}";
            }

            crocodile.transform.SetPositionAndRotation(
                start,
                Quaternion.Euler(0f, 0f, startsAtTop ? 90f : -90f));
            crocodile.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            DutzHippieBiteCollider.EnsureCrocodileColliders(crocodile);
            StripCrocodileCampaignBehaviours(crocodile);
            ConfigureCrocodileVisual(crocodile, startsAtTop);

            Rigidbody body = crocodile.GetComponent<Rigidbody>();
            if (body == null)
                body = crocodile.AddComponent<Rigidbody>();

            VerticalCrocodilePatrol patrol = crocodile.AddComponent<VerticalCrocodilePatrol>();
            patrol.Configure(start, end, patrolSpeed, manager);

            CrocodileBiteAnimation biteAnimation =
                crocodile.AddComponent<CrocodileBiteAnimation>();
            FloodCrocodileKill kill = crocodile.AddComponent<FloodCrocodileKill>();
            kill.Configure("A crocodile killed you!", biteAnimation);
            FloodCrocodileSounds sounds = crocodile.AddComponent<FloodCrocodileSounds>();
            sounds.ConfigureAudibility(30f, 1f, 0.45f, 10f, 40f);
        }

        return root;
    }

    static float GetPipeOpeningY(PipeSlot slot, bool top)
    {
        if (slot == null)
            return top ? CourseHeight : FloorY;

        Transform spawn = top ? slot.TopSpawn : slot.BottomSpawn;
        if (spawn == null)
            return top ? CourseHeight : FloorY;

        string unitName = top ? "TopPipeUnit_TEST" : "BottomPipeUnit_TEST";
        Transform pipeUnit = spawn.Find(unitName);
        return pipeUnit != null ? pipeUnit.position.y : spawn.position.y;
    }

    static void StripCrocodileCampaignBehaviours(GameObject crocodile)
    {
        // Remove dependants first so RequireComponent does not block cleanup.
        RemoveComponentsOfTypeName(crocodile, "SimpleCitizensHippieBiter");
        RemoveComponentsOfTypeName(crocodile, "SimpleCitizensHippieHunter");
        RemoveComponentsOfTypeName(crocodile, "SimpleCitizensNpcRespawn");
        RemoveComponentsOfTypeName(crocodile, "SimpleCitizensHippieSounds");
        RemoveComponentsOfTypeName(crocodile, "DutzCrocodilePoolMember");
        RemoveComponentsOfTypeName(crocodile, "DutzSegmentHippieTeleportSlots");
        RemoveComponentsOfTypeName(crocodile, "SimpleCitizensNpcPhysics");
    }

    static void ConfigureCrocodileVisual(GameObject crocodile, bool mirrored)
    {
        Transform visual = crocodile.transform.Find("CrocVisual");
        if (visual != null)
        {
            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);
            visual.localScale = scale;
        }

        Renderer[] renderers = crocodile.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderers[i].receiveShadows = false;
            renderers[i].lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderers[i].reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }

    static void AddTemporaryTestPipes(
        PipeSlot slot,
        int slotIndex,
        Material material,
        Mesh pipeUnitMesh)
    {
        if (slot == null || slot.TopSpawn == null || slot.BottomSpawn == null)
            return;

        // Deterministic sparse smoke: about one third of openings, same every rebuild.
        bool topSteam = (slotIndex % 3) == 0;
        bool bottomSteam = ((slotIndex + 1) % 3) == 0;

        ReplaceTestPipeUnit(
            slot.TopSpawn,
            "TopPipeUnit_TEST",
            "TopPipe_TEST",
            "TopFlange_TEST",
            1f,
            material,
            pipeUnitMesh,
            topSteam);

        ReplaceTestPipeUnit(
            slot.BottomSpawn,
            "BottomPipeUnit_TEST",
            "BottomPipe_TEST",
            "BottomFlange_TEST",
            -1f,
            material,
            pipeUnitMesh,
            bottomSteam);
    }

    static void ReplaceTestPipeUnit(
        Transform parent,
        string unitName,
        string legacyPipeName,
        string legacyFlangeName,
        float direction,
        Material material,
        Mesh pipeUnitMesh,
        bool emitSteam)
    {
        Transform existingUnit = parent.Find(unitName);
        if (existingUnit != null)
            Object.DestroyImmediate(existingUnit.gameObject);

        Transform existingPipe = parent.Find(legacyPipeName);
        if (existingPipe != null)
            Object.DestroyImmediate(existingPipe.gameObject);

        Transform existingFlange = parent.Find(legacyFlangeName);
        if (existingFlange != null)
            Object.DestroyImmediate(existingFlange.gameObject);

        GameObject unit = new GameObject(unitName);
        unit.transform.SetParent(parent, false);
        unit.transform.localPosition = Vector3.zero;
        unit.transform.localRotation = direction > 0f
            ? Quaternion.identity
            : Quaternion.Euler(0f, 0f, 180f);
        unit.transform.localScale = Vector3.one;

        MeshFilter filter = unit.AddComponent<MeshFilter>();
        filter.sharedMesh = pipeUnitMesh;

        MeshRenderer renderer = unit.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // Two inexpensive colliders preserve the stem and wider flange contact shapes.
        BoxCollider stemCollider = unit.AddComponent<BoxCollider>();
        stemCollider.center = new Vector3(0f, 15f, 0f);
        stemCollider.size = new Vector3(1.5f, 30f, 1.5f);

        BoxCollider flangeCollider = unit.AddComponent<BoxCollider>();
        flangeCollider.center = new Vector3(0f, 0.6f, 0f);
        flangeCollider.size = new Vector3(2.1f, 1.2f, 2.1f);

        PipeBurn burn = unit.AddComponent<PipeBurn>();
        SerializedObject burnSo = new SerializedObject(burn);
        burnSo.FindProperty("burnPerSecond").floatValue = 20f;
        burnSo.ApplyModifiedPropertiesWithoutUndo();

        if (emitSteam)
            AttachSteamSmoke(unit.transform);
    }

    static void AttachSteamSmoke(Transform pipeUnit)
    {
        if (pipeUnit == null)
            return;

        Transform existing = pipeUnit.Find("SteamSmoke");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject smokeObject = new GameObject("SteamSmoke");
        smokeObject.transform.SetParent(pipeUnit, false);
        // Emit from the flange opening (inner end of the pipe unit).
        smokeObject.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        smokeObject.transform.localRotation = Quaternion.identity;
        smokeObject.transform.localScale = Vector3.one;

        ParticleSystem particles = smokeObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.92f, 0.94f, 0.98f, 0.7f),
            new Color(1f, 1f, 1f, 0.9f));
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = -0.05f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 6f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 24f;
        shape.radius = 0.5f;
        shape.radiusThickness = 1f;
        // The pipe stems extend along local +Y, so local -Y points out into the gap.
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.Linear(0f, 0.65f, 1f, 1.65f));

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Low;
        noise.strength = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.82f, 0.84f, 0.88f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.5f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer particleRenderer =
            smokeObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sharedMaterial = EnsureSmokeMaterial();
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        particleRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        particleRenderer.motionVectorGenerationMode =
            UnityEngine.MotionVectorGenerationMode.ForceNoMotion;
    }

    static int EnsurePipeBurnOnScene()
    {
        PipeBurn[] existing = Object.FindObjectsOfType<PipeBurn>(true);
        int configured = 0;

        // Prefer named temporary test pipes; fall back to any mesh collider under PipeSlots.
        Transform slotsRoot = GameObject.Find("Environment/PipeSlots")?.transform;
        if (slotsRoot != null)
        {
            MeshRenderer[] renderers = slotsRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                GameObject go = renderers[i].gameObject;
                if (!go.name.Contains("Pipe"))
                    continue;

                PipeBurn burn = go.GetComponent<PipeBurn>();
                if (burn == null)
                    burn = go.AddComponent<PipeBurn>();

                SerializedObject burnSo = new SerializedObject(burn);
                burnSo.FindProperty("burnPerSecond").floatValue = 20f;
                burnSo.ApplyModifiedPropertiesWithoutUndo();
                configured++;
            }
        }

        if (configured > 0)
            return configured;

        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null)
                continue;

            SerializedObject burnSo = new SerializedObject(existing[i]);
            burnSo.FindProperty("burnPerSecond").floatValue = 20f;
            burnSo.ApplyModifiedPropertiesWithoutUndo();
            configured++;
        }

        return configured;
    }

    static void EnsurePlayerHealthOnScene()
    {
        GameObject player = GameObject.Find(DutzPlayerController.PlayerObjectName);
        if (player == null)
        {
            Debug.LogError("[FloodControl] Player1 was not found.");
            return;
        }

        GameManager manager = Object.FindObjectOfType<GameManager>();
        FloodPlayerHealth health = player.GetComponent<FloodPlayerHealth>();
        if (health == null)
            health = player.AddComponent<FloodPlayerHealth>();

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("maxHitPoints").intValue = 100;
        healthSo.FindProperty("currentHitPoints").intValue = 100;
        healthSo.FindProperty("gameManager").objectReferenceValue = manager;
        healthSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static int EnsurePlayerFloodSounds()
    {
        GameObject player = GameObject.Find(DutzPlayerController.PlayerObjectName);
        if (player == null)
        {
            Debug.LogError("[FloodControl] Player1 was not found.");
            return 0;
        }

        FloodSwimSounds swimSounds = player.GetComponent<FloodSwimSounds>();
        if (swimSounds == null)
            swimSounds = player.AddComponent<FloodSwimSounds>();
        swimSounds.ConfigureAudibility(1f);
        if (player.GetComponent<FloodBurnScreech>() == null)
            player.AddComponent<FloodBurnScreech>();

        EditorUtility.SetDirty(player);
        return 1;
    }

    static int EnsureCrocodileFloodSounds()
    {
        Transform root = GameObject.Find("Environment/CrocodilePatrols")?.transform;
        if (root == null)
            return 0;

        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            FloodCrocodileSounds sounds = child.GetComponent<FloodCrocodileSounds>();
            if (sounds == null)
                sounds = child.gameObject.AddComponent<FloodCrocodileSounds>();
            sounds.ConfigureAudibility(30f, 1f, 0.45f, 10f, 40f);
            EditorUtility.SetDirty(child.gameObject);
            count++;
        }

        return count;
    }

    static int EnsureRatFloodSounds()
    {
        Transform root = GameObject.Find("Environment/RatHazards")?.transform;
        if (root == null)
            return 0;

        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;

            if (child.GetComponent<FloodRatSounds>() == null)
                child.gameObject.AddComponent<FloodRatSounds>();
            EditorUtility.SetDirty(child.gameObject);
            count++;
        }

        return count;
    }

    static GameObject EnsureFloodBackgroundMusic(Transform parent = null)
    {
        ConfigureFloodMusicImporter();

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(FloodMusicAssetPath);
        if (clip == null)
        {
            Debug.LogError("[FloodControl] Game music was not found at " + FloodMusicAssetPath);
            return null;
        }

        if (parent == null)
            parent = GameObject.Find("GameManager")?.transform;
        if (parent == null)
        {
            Debug.LogError("[FloodControl] GameManager was not found for game music.");
            return null;
        }

        Transform existing = parent.Find(FloodMusicObjectName);
        GameObject music = existing != null
            ? existing.gameObject
            : new GameObject(FloodMusicObjectName);
        music.transform.SetParent(parent, false);

        if (music.GetComponent<AudioSource>() == null)
            music.AddComponent<AudioSource>();
        FloodBackgroundMusic controller = music.GetComponent<FloodBackgroundMusic>();
        if (controller == null)
            controller = music.AddComponent<FloodBackgroundMusic>();
        controller.Configure(clip, 1f);

        EditorUtility.SetDirty(music);
        return music;
    }

    static void ConfigureFloodMusicImporter()
    {
        AudioImporter importer = AssetImporter.GetAtPath(FloodMusicAssetPath) as AudioImporter;
        if (importer == null)
            return;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        bool changed = settings.loadType != AudioClipLoadType.Streaming
            || settings.compressionFormat != AudioCompressionFormat.Vorbis
            || Mathf.Abs(settings.quality - 0.7f) > 0.001f
            || settings.sampleRateSetting != AudioSampleRateSetting.OptimizeSampleRate
            || !importer.loadInBackground
            || settings.preloadAudioData;
        if (!changed)
            return;

        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
        settings.preloadAudioData = false;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }

    static bool EnsureFloodOpeningVideoFile()
    {
        string source = Path.Combine(Application.dataPath, FloodOpeningSourceFileName);
        string destination = Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
            FloodOpeningStreamingAssetPath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(destination))
        {
            long existingLength = new FileInfo(destination).Length;
            if (existingLength > 0)
            {
                Debug.Log(
                    "[FloodControl] Using opening video already in StreamingAssets ("
                    + existingLength
                    + " bytes).");
                return true;
            }
        }

        if (!File.Exists(source))
        {
            Debug.LogError("[FloodControl] Opening video was not found at " + source);
            return false;
        }

        long sourceLength = new FileInfo(source).Length;
        if (sourceLength <= 0)
        {
            Debug.LogError(
                "[FloodControl] The downloaded opening video is empty (0 bytes): "
                + source
                + ". Replace it with the complete MP4, then apply the opening video again.");
            return false;
        }

        string destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(destinationDirectory))
            Directory.CreateDirectory(destinationDirectory);

        File.Copy(source, destination, true);
        long destinationLength = new FileInfo(destination).Length;
        if (destinationLength != sourceLength)
        {
            Debug.LogError(
                "[FloodControl] Opening video copy was incomplete. Source bytes: "
                + sourceLength
                + ", copied bytes: "
                + destinationLength);
            return false;
        }

        AssetDatabase.ImportAsset(
            FloodOpeningStreamingAssetPath,
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        Debug.Log("[FloodControl] Opening video copied successfully (" + sourceLength + " bytes).");
        return true;
    }

    static bool EnsureFloodCompletionVideoFile()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[FloodControl] Unity project root could not be resolved.");
            return false;
        }

        string source = Path.Combine(
            projectRoot,
            FloodCompletionSourceRelativePath.Replace('/', Path.DirectorySeparatorChar));
        string destination = Path.Combine(
            projectRoot,
            FloodCompletionStreamingAssetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(source))
        {
            Debug.LogError("[FloodControl] Completion video was not found at " + source);
            return false;
        }

        long sourceLength = new FileInfo(source).Length;
        if (sourceLength <= 0)
        {
            Debug.LogError("[FloodControl] Completion video is empty: " + source);
            return false;
        }

        bool shouldCopy = !File.Exists(destination)
            || new FileInfo(destination).Length != sourceLength
            || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(destination);
        if (shouldCopy)
        {
            string destinationDirectory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            File.Copy(source, destination, true);
            AssetDatabase.ImportAsset(
                FloodCompletionStreamingAssetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            Debug.Log(
                "[FloodControl] Completion video copied successfully ("
                + sourceLength
                + " bytes).");
        }

        return File.Exists(destination) && new FileInfo(destination).Length > 0;
    }

    static void EnsurePlayerAirBubbles(GameObject player, GameManager manager)
    {
        Transform existing = player.transform.Find("AirBubbles");
        GameObject bubbleObject;
        if (existing != null)
        {
            bubbleObject = existing.gameObject;
        }
        else
        {
            bubbleObject = new GameObject("AirBubbles");
            bubbleObject.transform.SetParent(player.transform, false);
        }

        bubbleObject.transform.localPosition = new Vector3(0f, 1.75f, 0.2f);
        bubbleObject.transform.localRotation = Quaternion.identity;
        bubbleObject.transform.localScale = Vector3.one;

        ParticleSystem particles = bubbleObject.GetComponent<ParticleSystem>();
        if (particles == null)
            particles = bubbleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = 5f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.75f, 0.93f, 1f, 0.8f),
            new Color(0.9f, 0.98f, 1f, 0.95f));
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.7f, 1.15f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        ParticleSystemRenderer particleRenderer =
            bubbleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;
        particleRenderer.sharedMaterial = EnsureBubbleMaterial();
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;
        particleRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        particleRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        AirBubbleEmitter emitter = bubbleObject.GetComponent<AirBubbleEmitter>();
        if (emitter == null)
            emitter = bubbleObject.AddComponent<AirBubbleEmitter>();

        SerializedObject emitterSo = new SerializedObject(emitter);
        emitterSo.FindProperty("bubbleParticles").objectReferenceValue = particles;
        emitterSo.FindProperty("gameManager").objectReferenceValue = manager;
        emitterSo.FindProperty("emissionInterval").vector2Value = new Vector2(2.5f, 4f);
        emitterSo.FindProperty("bubblesPerBurst").vector2IntValue = new Vector2Int(6, 12);
        emitterSo.FindProperty("emitOnlyDuringGameplay").boolValue = true;
        emitterSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject BuildPlayer(GameManager manager)
    {
        GameObject dutzPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (dutzPrefab == null)
            throw new FileNotFoundException("Required Player1 prefab was not found.", PrefabPath);

        // Use the same Dutz prefab instance as the campaign levels, directly as Player1.
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(dutzPrefab);
        player.name = DutzPlayerController.PlayerObjectName;
        player.transform.position = new Vector3(PlayerStartX, PlayerStartY, LockedZ);
        player.transform.rotation = Quaternion.LookRotation(Vector3.right);

        // Only replace incompatible campaign locomotion/physics on this scene instance.
        StripCampaignComponents(player);

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.mass = 1f;
        body.drag = 0f;
        body.angularDrag = 0.05f;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = 2f;
        capsule.radius = 0.4f;
        capsule.center = new Vector3(0f, 1f, 0f);

        PlayerController controller = player.AddComponent<PlayerController>();
        BoundaryLimiter limiter = player.AddComponent<BoundaryLimiter>();
        SwimmingAnimationController swimmingAnimation =
            player.AddComponent<SwimmingAnimationController>();
        FloodPlayerHealth health = player.AddComponent<FloodPlayerHealth>();
        FloodPlayerPunch punch = player.AddComponent<FloodPlayerPunch>();
        punch.Configure(10);
        FloodSwimSounds swimSounds = player.AddComponent<FloodSwimSounds>();
        swimSounds.ConfigureAudibility(1f);
        player.AddComponent<FloodBurnScreech>();

        SerializedObject limiterSo = new SerializedObject(limiter);
        limiterSo.FindProperty("minX").floatValue = 0f;
        limiterSo.FindProperty("maxX").floatValue = CourseLength;
        limiterSo.FindProperty("minY").floatValue = PlayerMinY;
        limiterSo.FindProperty("maxY").floatValue = PlayerMaxY;
        limiterSo.FindProperty("lockZPosition").floatValue = LockedZ;
        limiterSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("body").objectReferenceValue = body;
        controllerSo.FindProperty("gameManager").objectReferenceValue = manager;
        controllerSo.FindProperty("forwardSpeed").floatValue = 10f;
        controllerSo.FindProperty("automaticForwardMovement").boolValue = true;
        controllerSo.FindProperty("automaticForwardSpeed").floatValue = 5f;
        controllerSo.FindProperty("backwardSwimDelay").floatValue = 1.5f;
        controllerSo.FindProperty("brakingAcceleration").floatValue = 24f;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject swimmingSo = new SerializedObject(swimmingAnimation);
        swimmingSo.FindProperty("animator").objectReferenceValue = player.GetComponent<Animator>();
        swimmingSo.FindProperty("body").objectReferenceValue = body;
        swimmingSo.FindProperty("gameManager").objectReferenceValue = manager;
        swimmingSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject healthSo = new SerializedObject(health);
        healthSo.FindProperty("maxHitPoints").intValue = 100;
        healthSo.FindProperty("currentHitPoints").intValue = 100;
        healthSo.FindProperty("gameManager").objectReferenceValue = manager;
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        EnsurePlayerAirBubbles(player, manager);
        return player;
    }

    static void StripCampaignComponents(GameObject visual)
    {
        // Disable campaign behaviours first so RequireComponent chains can be removed.
        MonoBehaviour[] behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.StartsWith("Dutz"))
                behaviour.enabled = false;
        }

        // Remove dependents before DutzPlayerController / CharacterController.
        RemoveComponentsOfTypeName(visual, "DutzFallRespawn");
        RemoveComponentsOfTypeName(visual, "DutzPlayerPunch");
        RemoveComponentsOfTypeName(visual, "DutzWalkAnimation");
        RemoveComponentsOfTypeName(visual, "DutzSimpleCitizensAnimator");
        RemoveComponentsOfTypeName(visual, "DutzSimpleCitizensSecondaryMotion");
        RemoveComponentsOfTypeName(visual, "DutzGoldCoinCollector");
        RemoveComponentsOfTypeName(visual, "DutzSuitcaseCollector");
        RemoveComponentsOfTypeName(visual, "DutzForceFieldSuitCollector");
        RemoveComponentsOfTypeName(visual, "DutzSuperPunchCollector");
        RemoveComponentsOfTypeName(visual, "DutzSuperJumpCollector");
        RemoveComponentsOfTypeName(visual, "DutzParachuteCollector");
        RemoveComponentsOfTypeName(visual, "DutzPlayerParachute");
        RemoveComponentsOfTypeName(visual, "DutzPlayerHitPoints");
        RemoveComponentsOfTypeName(visual, "DutzHealthPotionCollector");
        RemoveComponentsOfTypeName(visual, "DutzLevel00CrowdPushback");
        RemoveComponentsOfTypeName(visual, "DutzAddictCollisionBite");
        RemoveComponentsOfTypeName(visual, "DutzMovementSounds");
        RemoveComponentsOfTypeName(visual, "DutzPlayerController");
        RemoveComponentsOfTypeName(visual, "CharacterController");

        // Remove leftover campaign colliders; Player1 receives one Rigidbody collider below.
        Collider[] colliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null || col is CharacterController)
                continue;
            Object.DestroyImmediate(col);
        }
    }

    static void RemoveComponentsOfTypeName(GameObject root, string typeName)
    {
        Component[] components = root.GetComponentsInChildren<Component>(true);
        for (int i = components.Length - 1; i >= 0; i--)
        {
            Component component = components[i];
            if (component == null)
                continue;
            if (component.GetType().Name != typeName)
                continue;
            Object.DestroyImmediate(component);
        }
    }

    static void RemoveComponentsOfType<T>(GameObject root) where T : Component
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                Object.DestroyImmediate(components[i]);
        }
    }

    static GameObject BuildCamera(Transform player)
    {
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 14.2857f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 100f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.35f, 0.6f, 0.85f, 1f);
        camGo.AddComponent<AudioListener>();

        camGo.transform.position = new Vector3(PlayerStartX - 8f, 10f, -24f);
        camGo.transform.rotation = Quaternion.identity;

        CameraController cameraController = camGo.AddComponent<CameraController>();
        SerializedObject so = new SerializedObject(cameraController);
        so.FindProperty("target").objectReferenceValue = player;
        so.FindProperty("fixedY").floatValue = 10f;
        so.FindProperty("fixedZ").floatValue = -24f;
        so.FindProperty("orthographicSize").floatValue = 14.2857f;
        so.FindProperty("viewportAnchorX").floatValue = 0.25f;
        so.FindProperty("leftDeadZoneX").floatValue = 0.17f;
        so.FindProperty("rightDeadZoneX").floatValue = 0.30f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return camGo;
    }

    static GameObject BuildGameManager(PipeSlot[] slots)
    {
        GameObject go = new GameObject("GameManager");
        GameManager manager = go.AddComponent<GameManager>();
        PipeGenerator generator = go.AddComponent<PipeGenerator>();

        SerializedObject managerSo = new SerializedObject(manager);
        managerSo.FindProperty("currentLevel").intValue = 1;
        managerSo.FindProperty("minimumGap").floatValue = 7.8f;
        managerSo.FindProperty("maximumGap").floatValue = 20.8f;
        managerSo.FindProperty("gapReductionPerLevel").floatValue = 1.3f;
        managerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject generatorSo = new SerializedObject(generator);
        generatorSo.FindProperty("gameManager").objectReferenceValue = manager;
        generatorSo.FindProperty("minCentreY").floatValue = 4f;
        generatorSo.FindProperty("maxCentreY").floatValue = 14f;
        generatorSo.FindProperty("generateOnStart").boolValue = false;

        SerializedProperty slotsProp = generatorSo.FindProperty("slots");
        slotsProp.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

        generatorSo.ApplyModifiedPropertiesWithoutUndo();
        EnsureFloodBackgroundMusic(go.transform);
        return go;
    }

    static GameObject BuildLighting()
    {
        GameObject lighting = new GameObject("Lighting");
        GameObject lightGo = new GameObject("Directional Light");
        lightGo.transform.SetParent(lighting.transform, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.None;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
        return lighting;
    }

    static GameObject BuildIntro(GameManager manager)
    {
        GameObject intro = new GameObject("IntroSequence");
        IntroSequenceController controller = intro.AddComponent<IntroSequenceController>();
        EnsureFloodOpeningVideoFile();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("videoFileName").stringValue = FloodOpeningRuntimeFileName;
        so.FindProperty("gameManager").objectReferenceValue = manager;
        so.FindProperty("startIntroOnEnable").boolValue = true;
        so.FindProperty("prepareTimeoutSeconds").floatValue = 60f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return intro;
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (current[i].path == scenePath)
            {
                if (!current[i].enabled)
                {
                    current[i].enabled = true;
                    EditorBuildSettings.scenes = current;
                }
                return;
            }
        }

        var next = new EditorBuildSettingsScene[current.Length + 1];
        for (int i = 0; i < current.Length; i++)
            next[i] = current[i];
        next[current.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = next;
    }
}
