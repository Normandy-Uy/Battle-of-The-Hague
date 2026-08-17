using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Syncs public/UPARROW.PNG and places the Super Jump pickup on Highway Bridge 1 in Dutz_Level00 only.</summary>
public static class DutzSuperJumpPlacer
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string BridgeSegmentName = "Highway Bridge 1";
    const string PickupsRootName = "DutzPickups";

    /// <summary>Batch: -executeMethod DutzSuperJumpPlacer.SetupOnLevel00Batch</summary>
    public static void SetupOnLevel00Batch() => SetupOnLevel00(log: true);

    public static bool SetupOnLevel00(bool log)
    {
        if (!DutzSuperJumpModelBuilder.SyncSharedAssets(log))
            return false;

        return SetupOnScene(Level00ScenePath, log);
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != Level00ScenePath)
            return false;

        return EnsurePickupInScene(scene, log);
    }

    public static bool RemoveFromSceneIfPresent(Scene scene, bool log)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || scene.path == Level00ScenePath)
            return false;

        var pickup = GameObject.Find(DutzSuperJumpPickup.PickupObjectName);
        if (pickup == null)
            return false;

        Undo.DestroyObjectImmediate(pickup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] Removed Super Jump pickup from " + scene.path + " (Level 00 only).");

        return true;
    }

    static bool SetupOnScene(string scenePath, bool log)
    {
        if (!File.Exists(scenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Scene not found: " + scenePath);
            return false;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        return EnsurePickupInScene(scene, log);
    }

    public static bool EnsurePickupInScene(Scene scene, bool log)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path) || scene.path != Level00ScenePath)
            return false;

        if (!DutzSuperJumpModelBuilder.SyncSharedAssets(log: false))
            return false;

        Physics.SyncTransforms();

        var bridge = GameObject.Find(BridgeSegmentName);
        if (bridge == null)
        {
            if (log)
                Debug.LogError("[Dutz] " + BridgeSegmentName + " not found in scene.");
            return false;
        }

        Vector3? anchorHint = null;
        var existing = GameObject.Find(DutzSuperJumpPickup.PickupObjectName);
        if (existing != null)
            anchorHint = existing.transform.position;

        if (!DutzSuperJumpPlacement.TryGetTopBarWorldPosition(out var position, anchorHint))
        {
            if (log)
                Debug.LogError("[Dutz] Could not find top bar position on " + BridgeSegmentName);
            return false;
        }

        var changed = false;
        var pickupsRoot = EnsurePickupsRoot(ref changed);
        var pickup = existing;
        if (pickup == null)
        {
            pickup = new GameObject(DutzSuperJumpPickup.PickupObjectName);
            Undo.RegisterCreatedObjectUndo(pickup, "Create Super Jump Pickup");
            changed = true;
        }

        if ((pickup.transform.position - position).sqrMagnitude > 0.25f)
        {
            Undo.RecordObject(pickup.transform, "Place Super Jump Pickup");
            pickup.transform.position = position;
            changed = true;
        }

        if (pickup.transform.parent != pickupsRoot.transform)
        {
            Undo.SetTransformParent(pickup.transform, pickupsRoot.transform, "Parent Super Jump Pickup");
            changed = true;
        }

        if (pickup.transform.localScale != Vector3.one * DutzSuperJumpPlacement.PickupWorldScale)
        {
            Undo.RecordObject(pickup.transform, "Scale Super Jump Pickup");
            pickup.transform.localScale = Vector3.one * DutzSuperJumpPlacement.PickupWorldScale;
            changed = true;
        }

        if (pickup.GetComponent<DutzSuperJumpPickup>() == null)
        {
            Undo.AddComponent<DutzSuperJumpPickup>(pickup);
            changed = true;
        }

        DutzSuperJumpPickupSetup.Apply(pickup);
        DutzSuperJumpModelBuilder.AttachSharedVisual(pickup.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Super Jump pickup ready at {pickup.transform.position} on {BridgeSegmentName}.");

        return true;
    }

    static GameObject EnsurePickupsRoot(ref bool changed)
    {
        var root = GameObject.Find(PickupsRootName);
        if (root != null)
            return root;

        root = new GameObject(PickupsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create DutzPickups");
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        changed = true;
        return root;
    }
}

/// <summary>Syncs public/UPARROW.PNG into Resources for the Super Jump pickup.</summary>
public static class DutzSuperJumpModelBuilder
{
    const string SourceFileName = "UPARROW.PNG";
    const string TextureAssetPath = "Assets/Resources/DutzSuperJumpArrow.png";
    const string MaterialAssetPath = "Assets/Resources/DutzSuperJumpArrow.mat";

    public static bool SyncSharedAssets(bool log)
    {
        if (!SyncTexture(log))
            return false;

        EnsureMaterial(log);
        return true;
    }

    public static void AttachSharedVisual(Transform root)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        if (material == null || texture == null)
            return;

        var visual = root.Find(DutzSuperJumpPickupSetup.VisualChildName);
        if (visual == null)
            return;

        var renderer = visual.GetComponent<MeshRenderer>();
        if (renderer == null)
            return;

        var instanceMaterial = new Material(material) { mainTexture = texture };
        renderer.sharedMaterial = instanceMaterial;

        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var height = DutzSuperJumpPickupSetup.TargetVisualHeight;
        visual.localScale = new Vector3(height * aspect, height, 1f);
        EditorUtility.SetDirty(visual.gameObject);
    }

    static bool SyncTexture(bool log)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            if (log)
                Debug.LogError("[Dutz] Could not resolve project root.");
            return false;
        }

        var source = FindSourceTexture(projectRoot);
        if (string.IsNullOrEmpty(source))
        {
            if (log)
                Debug.LogError("[Dutz] Missing public/" + SourceFileName + " — add the image and run again.");
            return false;
        }

        Directory.CreateDirectory("Assets/Resources");

        var bytes = File.ReadAllBytes(source);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            if (log)
                Debug.LogError("[Dutz] Could not decode public/" + SourceFileName);
            return false;
        }

        var processed = MakeBlackTransparent(loaded);
        if (processed != loaded)
            Object.DestroyImmediate(loaded);

        File.WriteAllBytes(TextureAssetPath, processed.EncodeToPNG());
        Object.DestroyImmediate(processed);

        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
        ConfigureTextureImport(TextureAssetPath);

        if (log)
            Debug.Log("[Dutz] Synced public/" + SourceFileName + " -> " + TextureAssetPath);

        return true;
    }

    static string FindSourceTexture(string projectRoot)
    {
        var exact = Path.Combine(projectRoot, "public", SourceFileName);
        if (File.Exists(exact))
            return exact;

        var publicDir = Path.Combine(projectRoot, "public");
        if (!Directory.Exists(publicDir))
            return null;

        foreach (var file in Directory.GetFiles(publicDir))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, SourceFileName, System.StringComparison.OrdinalIgnoreCase))
                return file;
        }

        return null;
    }

    static Texture2D MakeBlackTransparent(Texture2D source)
    {
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        var pixels = source.GetPixels32();
        for (var i = 0; i < pixels.Length; i++)
        {
            var pixel = pixels[i];
            if (pixel.r <= 24 && pixel.g <= 24 && pixel.b <= 24)
                pixel.a = 0;
            pixels[i] = pixel;
        }

        output.SetPixels32(pixels);
        output.Apply();
        return output;
    }

    static void ConfigureTextureImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();
    }

    static Material EnsureMaterial(bool log)
    {
        Directory.CreateDirectory("Assets/Resources");

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            if (log)
                Debug.LogError("[Dutz] Standard shader not found for Super Jump arrow material.");
            return null;
        }

        if (material == null)
        {
            material = new Material(shader) { name = "DutzSuperJumpArrow" };
            AssetDatabase.CreateAsset(material, MaterialAssetPath);
        }

        material.shader = shader;
        material.SetFloat("_Mode", 1f);
        material.SetInt("_SrcBlend", (int)BlendMode.One);
        material.SetInt("_DstBlend", (int)BlendMode.Zero);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.AlphaTest;
        material.SetFloat("_Cutoff", 0.15f);
        material.SetColor("_Color", Color.white);

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        if (texture != null)
            material.mainTexture = texture;

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }
}

/// <summary>
/// Syncs public/Kangaroo_Low_Poly.fbx and places Super Jump on Level07 Highway Bridge 1.
/// Four Super Jump charges (same force as shop Super Jump, not for-life).
/// </summary>
public static class DutzLevel07SuperJumpPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string BridgeSegmentName = "Highway Bridge 1";
    const string PickupsRootName = "DutzPickups";
    const string SourceFileName = "Kangaroo_Low_Poly.fbx";
    const string AssetFbxPath = "Assets/Characters/NPCs/Models/Kangaroo_Low_Poly.fbx";
    const string VisualPrefabPath = "Assets/Resources/DutzSuperJumpKangarooVisual.prefab";

    [MenuItem("Assets/Dutz Authoring/Place Super Jump On Level07 Bridge1")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Super Jump On Level07 Bridge1 requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(log: true))
            Debug.LogError("[Dutz] Failed to place Super Jump on Level07 Highway Bridge 1.");
    }

    public static bool PlaceSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!SyncKangarooAssets(log))
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        // Menu / batch only — force Bridge 1 top-bar pose. Auto-sync must not overwrite authored transforms.
        return EnsurePickupInScene(scene, log, forceAuthoredPlacement: true);
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path.Replace('\\', '/') != Level07Path)
            return false;

        // Assets sync separately via DutzPublicPickupAutoSync.SyncPermanentPickupAssets.
        // Never re-snap position/scale here — manual scene posing must stick after Save.
        return EnsurePickupInScene(scene, log, forceAuthoredPlacement: false);
    }

    public static bool SyncKangarooAssets(bool log)
    {
        if (!SyncSourceFbx(log))
            return false;

        return BuildVisualPrefab(log);
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
                Debug.LogError("[Dutz] Missing public/" + SourceFileName + " — add the FBX and run again.");
            return false;
        }

        Directory.CreateDirectory("Assets/Characters/NPCs/Models");
        File.Copy(source, AssetFbxPath, overwrite: true);
        AssetDatabase.ImportAsset(AssetFbxPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log("[Dutz] Synced public/" + SourceFileName + " -> " + AssetFbxPath);

        return true;
    }

    static bool BuildVisualPrefab(bool log)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(AssetFbxPath);
        if (fbx == null)
        {
            if (log)
                Debug.LogError("[Dutz] Missing imported kangaroo FBX: " + AssetFbxPath);
            return false;
        }

        Directory.CreateDirectory("Assets/Resources");

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        temp.name = "DutzSuperJumpKangarooVisual";
        temp.transform.localPosition = Vector3.zero;
        temp.transform.localRotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        foreach (var col in temp.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, VisualPrefabPath);
        Object.DestroyImmediate(temp);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (prefab == null)
        {
            if (log)
                Debug.LogError("[Dutz] Failed to save Super Jump kangaroo visual prefab.");
            return false;
        }

        if (log)
            Debug.Log("[Dutz] Super Jump kangaroo visual prefab saved from public/" + SourceFileName);

        return true;
    }

    static bool EnsurePickupInScene(Scene scene, bool log, bool forceAuthoredPlacement)
    {
        Physics.SyncTransforms();

        var changed = false;
        var pickupsRoot = EnsurePickupsRoot(ref changed);
        var pickup = GameObject.Find(DutzSuperJumpPickup.PickupObjectName);
        var created = false;

        if (pickup == null)
        {
            pickup = new GameObject(DutzSuperJumpPickup.PickupObjectName);
            Undo.RegisterCreatedObjectUndo(pickup, "Create Level07 Super Jump Pickup");
            created = true;
            changed = true;
        }

        // Only snap to Bridge 1 when missing or when the authoring menu forces it.
        if (created || forceAuthoredPlacement)
        {
            var bridge = GameObject.Find(BridgeSegmentName);
            if (bridge == null)
            {
                if (log)
                    Debug.LogError($"[Dutz] '{BridgeSegmentName}' not found in Level07.");
                return false;
            }

            var bridgeCenter = bridge.transform.position;
            if (bridge.TryGetComponent<Renderer>(out var renderer))
                bridgeCenter = renderer.bounds.center;
            else if (bridge.TryGetComponent<Collider>(out var col))
                bridgeCenter = col.bounds.center;

            if (!DutzSuperJumpPlacement.TryGetTopBarWorldPosition(out var position, bridgeCenter))
            {
                if (log)
                    Debug.LogError($"[Dutz] Could not find top bar on {BridgeSegmentName}.");
                return false;
            }

            if ((pickup.transform.position - position).sqrMagnitude > 0.0001f)
            {
                Undo.RecordObject(pickup.transform, "Place Level07 Super Jump");
                pickup.transform.position = position;
                changed = true;
            }

            var defaultScale = Vector3.one * DutzSuperJumpPlacement.PickupWorldScale;
            if (pickup.transform.localScale != defaultScale)
            {
                Undo.RecordObject(pickup.transform, "Scale Level07 Super Jump");
                pickup.transform.localScale = defaultScale;
                changed = true;
            }
        }

        if (pickup.transform.parent != pickupsRoot.transform)
        {
            Undo.SetTransformParent(pickup.transform, pickupsRoot.transform, "Parent Level07 Super Jump");
            changed = true;
        }

        if (pickup.GetComponent<DutzSuperJumpPickup>() == null)
        {
            Undo.AddComponent<DutzSuperJumpPickup>(pickup);
            changed = true;
        }

        var hadKangaroo = DutzSuperJumpPickupSetup.HasKangaroo3DVisual(pickup.transform);
        DutzSuperJumpPickupSetup.Apply(pickup);
        // Upgrade legacy PNG prism → kangaroo once; never wipe authored visual scale on every open.
        if (!hadKangaroo)
        {
            DutzSuperJumpPickupSetup.EnsureKangaroo3DVisual(pickup.transform, forceRebuild: true);
            changed = true;
        }

        if (!changed)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        if (forceAuthoredPlacement)
            EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Level07 Super Jump ready at {pickup.transform.position} scale {pickup.transform.localScale}.");

        return true;
    }

    public static void AttachKangarooVisual(Transform root)
    {
        DutzSuperJumpPickupSetup.EnsureKangaroo3DVisual(root, forceRebuild: true);
    }

    static GameObject EnsurePickupsRoot(ref bool changed)
    {
        var root = GameObject.Find(PickupsRootName);
        if (root != null)
            return root;

        root = new GameObject(PickupsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create DutzPickups");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        root.transform.localScale = Vector3.one;
        changed = true;
        return root;
    }
}

/// <summary>
/// Creates a missing Level07 Force Field Suit only. Never moves an existing authored pose.
/// </summary>
public static class DutzLevel07ForceFieldSuitPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string BridgeSegmentName = "Highway Bridge 4";
    const string PickupsRootName = "DutzPickups";

    [MenuItem("Assets/Dutz Authoring/Ensure Level07 Force Field Suit Exists")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Ensure Level07 Force Field Suit Exists requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(log: true))
            Debug.LogError("[Dutz] Failed to ensure Force Field Suit exists in Level07.");
    }

    public static bool PlaceSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!DutzForceFieldSuitModelBuilder.SyncAndBuildVisualPrefab(log))
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        return EnsurePickupInScene(scene, log);
    }

    public static bool EnsureOnOpenScene(bool log)
    {
        // Never auto-move an existing suit — only create if missing.
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path.Replace('\\', '/') != Level07Path)
            return false;

        if (GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName) != null)
            return false;

        if (!DutzForceFieldSuitModelBuilder.SyncAndBuildVisualPrefab(log: false))
            return false;

        return EnsurePickupInScene(scene, log);
    }

    static bool EnsurePickupInScene(Scene scene, bool log)
    {
        Physics.SyncTransforms();

        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit != null)
        {
            if (suit.GetComponent<DutzForceFieldSuitPickup>() == null)
                Undo.AddComponent<DutzForceFieldSuitPickup>(suit);
            DutzForceFieldSuitSetup.Apply(suit);
            if (log)
                Debug.Log(
                    $"[Dutz] Level07 Force Field Suit left at authored pose {suit.transform.position}.");
            return true;
        }

        var bridge = GameObject.Find(BridgeSegmentName);
        if (bridge == null)
        {
            if (log)
                Debug.LogError($"[Dutz] '{BridgeSegmentName}' not found in Level07.");
            return false;
        }

        if (!TryGetBridge4WalkableDeckPosition(bridge, out var position))
        {
            if (log)
                Debug.LogError($"[Dutz] Could not sample walkable deck on {BridgeSegmentName}.");
            return false;
        }

        var changed = false;
        var pickupsRoot = EnsurePickupsRoot(ref changed);
        suit = new GameObject(DutzForceFieldSuitPickup.PickupObjectName);
        Undo.RegisterCreatedObjectUndo(suit, "Create Level07 Force Field Suit");

        Undo.RecordObject(suit.transform, "Create Level07 Force Field Suit");
        suit.transform.position = position;
        Undo.SetTransformParent(suit.transform, pickupsRoot.transform, "Parent Level07 Force Field Suit");
        suit.transform.localScale = Vector3.one * DutzForceFieldSuitPlacement.SuitWorldScale;

        Undo.AddComponent<DutzForceFieldSuitPickup>(suit);
        DutzForceFieldSuitSetup.Apply(suit);
        suit.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log(
                $"[Dutz] Level07 Force Field Suit created (missing) on {BridgeSegmentName} at {position}.");

        return true;
    }

    static bool TryGetBridge4WalkableDeckPosition(GameObject bridge, out Vector3 position)
    {
        position = Vector3.zero;
        var spawn = Vector3.zero;
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            spawn = so.FindProperty("spawnPosition").vector3Value;
            break;
        }

        var travelForward = Vector3.right;
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.0001f)
            travelForward = progress.normalized;

        var path = DutzHighwayDeckSampler.BuildSegmentPath(bridge, bridge.name, spawn, travelForward);
        if (path.Samples == null || path.Samples.Count == 0)
            return false;

        if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, 0.5f, out var sample))
            return false;

        var walkableHintY = bridge.transform.position.y;
        if (bridge.TryGetComponent<Renderer>(out var renderer))
            walkableHintY = renderer.bounds.center.y;
        else if (bridge.TryGetComponent<Collider>(out var col))
            walkableHintY = col.bounds.center.y;

        var deck = new Vector3(sample.Position.x, walkableHintY, sample.Position.z);

        // Prefer Liron's station height — true walkable ribbon on Bridge 4 (not mid beam).
        var liron = GameObject.Find("Liron Sinta") ?? DutzGiantBossNames.FindLironSinta();
        if (liron != null)
        {
            position = new Vector3(sample.Position.x, liron.transform.position.y + 2.2f, sample.Position.z);
            return true;
        }

        if (!DutzRoadGround.TrySampleLevel07NamedHighwayDeckPoint(
                BridgeSegmentName, deck, out var deckPoint, out _))
        {
            var probe = deck + Vector3.up * 40f;
            if (!DutzRoadGround.TrySampleWalkableRoadDeckY(probe, deck.y, null, out var deckY)
                && !DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
                return false;

            deckPoint = new Vector3(deck.x, deckY, deck.z);
        }

        if (deckPoint.y > walkableHintY + 18f)
            return false;

        position = deckPoint + Vector3.up * 2.2f;
        return true;
    }

    static GameObject EnsurePickupsRoot(ref bool changed)
    {
        var root = GameObject.Find(PickupsRootName);
        if (root != null)
            return root;

        root = new GameObject(PickupsRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create DutzPickups Root");
        changed = true;
        return root;
    }
}
