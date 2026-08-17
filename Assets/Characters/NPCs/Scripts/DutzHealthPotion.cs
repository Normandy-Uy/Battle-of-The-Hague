using UnityEngine;

/// <summary>Level 3 health potion — restores HP on pickup (green capped at max; red adds flat HP).</summary>
[DisallowMultipleComponent]
public class DutzHealthPotion : MonoBehaviour
{
    public const string PotionPrefix = "DutzHealthPotion_";
    public const int HealAmount = 20;
    public const int DefaultHealAmount = 20;
    public const int Bridge5RedHealAmount = 100;
    public const string Bridge5RedPotionName = "DutzHealthPotion_Bridge5Red99";

    [SerializeField] int healAmount = DefaultHealAmount;
    [SerializeField] float spinSpeed = 60f;

    [Header("Spawn Pose")]
    [SerializeField] DutzCollectibleSpawnPose spawnPose;
    [Tooltip("When enabled, only you can change Spawn Pose (Inspector or Read From Transform). Auto-sync will not overwrite it.")]
    [SerializeField] bool spawnPoseLocked = true;

    bool collected;
    Transform potionVisual;
    Renderer pickupRenderer;

    public bool IsCollected => collected;

    public Renderer PickupRenderer
    {
        get
        {
            if (pickupRenderer == null)
                pickupRenderer = GetComponentInChildren<Renderer>(true);
            return pickupRenderer;
        }
    }

    public int HealAmountValue => Mathf.Max(1, healAmount);

    public DutzCollectibleSpawnPose SpawnPose => spawnPose;

    public bool SpawnPoseLocked => spawnPoseLocked;

    public static bool IsTrackPotionRoot(GameObject go) =>
        go != null && go.name.StartsWith(PotionPrefix, System.StringComparison.Ordinal);

    public void CaptureSpawnPoseFromTransform(bool force = false)
    {
        if (!force && (spawnPoseLocked || spawnPose.HasPosition))
            return;

        spawnPose = DutzCollectibleSpawnPose.FromTransform(transform);
    }

    public void ApplySpawnPose()
    {
        if (!spawnPose.HasPosition)
            return;

        spawnPose.ApplyTo(transform);
    }

    void Awake()
    {
        if (gameObject.name == Bridge5RedPotionName)
        {
            healAmount = Bridge5RedHealAmount;
            DutzHealthPotionSetup.ApplyRedVisual(gameObject);
        }
        else
            DutzHealthPotionSetup.ApplyGreenVisual(gameObject);

        ApplySpawnPose();
        potionVisual = transform.Find(DutzHealthPotionSetup.VisualChildName);
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

#if UNITY_EDITOR
        if (!DutzHealthPotionSetup.NeedsVisualRepair(gameObject))
            return;

        UnityEditor.EditorApplication.delayCall += RepairVisualDeferred;
#endif
    }

#if UNITY_EDITOR
    void RepairVisualDeferred()
    {
        if (this == null || Application.isPlaying)
            return;

        if (gameObject.name == Bridge5RedPotionName)
            DutzHealthPotionSetup.ApplyRedVisual(gameObject);
        else if (IsTrackPotionRoot(gameObject))
            DutzHealthPotionSetup.ApplyGreenVisual(gameObject);
    }
#endif

    void Update()
    {
        if (collected)
            return;

        if (Application.isMobilePlatform
            && DutzCollectibleProgress.IsLevel03Gameplay
            && !IsNearPlayerForSpin())
        {
            return;
        }

        if (potionVisual != null)
            potionVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
        else
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

    bool IsNearPlayerForSpin()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
            return true;

        var delta = transform.position - player.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= 40f * 40f;
    }

    public void HideCollected()
    {
        collected = true;
        gameObject.SetActive(false);
    }

    public void ResetForRespawn()
    {
        collected = false;
        ApplySpawnPose();
        if (potionVisual == null)
            potionVisual = transform.Find(DutzHealthPotionSetup.VisualChildName);
        gameObject.SetActive(true);
    }
}

/// <summary>Shared health potion FBX visual — one mesh for all Level 3 pickups, green or red material.</summary>
public static class DutzHealthPotionSetup
{
    public const string VisualChildName = "PotionModelVisual";
    const string LegacyBottleName = "Bottle";
    const string LegacyCorkName = "Cork";
    const string SharedMeshResourceName = "DutzHealthPotionGreenMesh";
    const string SharedMeshAssetPath = "Assets/Resources/DutzHealthPotionGreenMesh.asset";
    const string GreenMaterialPath = "Assets/Characters/Level03/Materials/DutzHealthPotionGreen.mat";
    const string RedMaterialPath = "Assets/Characters/Level03/Materials/DutzHealthPotionRed.mat";
    const string GreenMaterialResourcePath = "Assets/Resources/DutzHealthPotionGreen.mat";
    const string RedMaterialResourcePath = "Assets/Resources/DutzHealthPotionRed.mat";
    public const float TargetVisualHeight = 3.3f;

    public static bool IsGreenTrackPotion(GameObject go) =>
        DutzHealthPotion.IsTrackPotionRoot(go)
        && go.name != DutzHealthPotion.Bridge5RedPotionName;

    public static bool IsRedTrackPotion(GameObject go) =>
        go != null && go.name == DutzHealthPotion.Bridge5RedPotionName;

    public static void ApplyGreenVisual(GameObject root, bool replaceLegacy = true)
    {
        if (root == null || !IsGreenTrackPotion(root))
            return;

        ApplySharedVisual(root, LoadGreenMaterial(), replaceLegacy);
    }

    public static void ApplyRedVisual(GameObject root, bool replaceLegacy = true)
    {
        if (root == null || !IsRedTrackPotion(root))
            return;

        ApplySharedVisual(root, LoadRedMaterial(), replaceLegacy);
    }

    static void ApplySharedVisual(GameObject root, Material material, bool replaceLegacy)
    {
        if (TryRepairBrokenVisual(root.transform, material))
            return;

        if (HasSharedMeshVisual(root.transform))
            return;

        RemoveBrokenVisual(root.transform);

        if (replaceLegacy)
            RemoveLegacyPrimitives(root.transform);

        if (root.transform.Find(VisualChildName) != null)
            return;

        if (!TryInstantiateSharedVisual(root.transform, material))
            Debug.LogWarning("[Dutz] Missing shared potion mesh in Resources.");
    }

    static bool TryRepairBrokenVisual(Transform root, Material material)
    {
        var visual = root.Find(VisualChildName);
        if (visual == null)
            return false;

        var filter = visual.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh != null)
            return false;

        var mesh = LoadSharedMesh();
        if (mesh == null || material == null)
            return false;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var filterSo = new UnityEditor.SerializedObject(filter);
            filterSo.FindProperty("m_Mesh").objectReferenceValue = mesh;
            filterSo.ApplyModifiedPropertiesWithoutUndo();

            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = visual.gameObject.AddComponent<MeshRenderer>();

            var rendererSo = new UnityEditor.SerializedObject(renderer);
            rendererSo.FindProperty("m_Materials").arraySize = 1;
            rendererSo.FindProperty("m_Materials").GetArrayElementAtIndex(0).objectReferenceValue = material;
            rendererSo.ApplyModifiedPropertiesWithoutUndo();

            NormalizeVisualScale(visual, TargetVisualHeight);
            UnityEditor.EditorUtility.SetDirty(visual.gameObject);
            return true;
        }
#endif

        filter.sharedMesh = mesh;

        var meshRenderer = visual.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sharedMaterial = material;

        NormalizeVisualScale(visual, TargetVisualHeight);
        return true;
    }

    static void RemoveBrokenVisual(Transform root)
    {
        var visual = root.Find(VisualChildName);
        if (visual == null)
            return;

        var filter = visual.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            return;

        RemoveChild(root, VisualChildName);
    }

    public static bool HasSharedMeshVisual(Transform root)
    {
        var visual = root.Find(VisualChildName);
        if (visual == null)
            return false;

        var filter = visual.GetComponent<MeshFilter>();
        return filter != null && filter.sharedMesh != null;
    }

    public static bool NeedsVisualRepair(GameObject root)
    {
        if (root == null || !DutzHealthPotion.IsTrackPotionRoot(root))
            return false;

        if (root.transform.Find(LegacyBottleName) != null || root.transform.Find(LegacyCorkName) != null)
            return true;

        var visual = root.transform.Find(VisualChildName);
        if (visual == null)
            return true;

        var filter = visual.GetComponent<MeshFilter>();
        return filter == null || filter.sharedMesh == null;
    }

    static void RemoveLegacyPrimitives(Transform root)
    {
        RemoveChild(root, LegacyBottleName);
        RemoveChild(root, LegacyCorkName);
    }

    static void RemoveLegacyVisuals(Transform root)
    {
        RemoveLegacyPrimitives(root);

        var visual = root.Find(VisualChildName);
        if (visual == null)
            return;

        if (visual.GetComponent<MeshFilter>()?.sharedMesh != null)
            return;

        RemoveChild(root, VisualChildName);
    }

    static void RemoveChild(Transform root, string childName)
    {
        var child = root.Find(childName);
        if (child == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(child.gameObject);
        else
            Object.DestroyImmediate(child.gameObject);
    }

    static bool TryInstantiateSharedVisual(Transform root, Material material)
    {
        var mesh = LoadSharedMesh();
        if (mesh == null || material == null)
            return false;

        var visual = new GameObject(VisualChildName);
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterial = material;
        NormalizeVisualScale(visual.transform, TargetVisualHeight);
        return true;
    }

    static Mesh LoadSharedMesh()
    {
        var mesh = Resources.Load<Mesh>(SharedMeshResourceName);
#if UNITY_EDITOR
        if (mesh == null)
            mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(SharedMeshAssetPath);
#endif
        return mesh;
    }

    static Material LoadGreenMaterial()
    {
        var runtimeMat = Resources.Load<Material>("DutzHealthPotionGreen");
#if UNITY_EDITOR
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GreenMaterialResourcePath);
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GreenMaterialPath);
#endif
        return runtimeMat;
    }

    static Material LoadRedMaterial()
    {
        var runtimeMat = Resources.Load<Material>("DutzHealthPotionRed");
#if UNITY_EDITOR
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(RedMaterialResourcePath);
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(RedMaterialPath);
#endif
        return runtimeMat;
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
}

/// <summary>Player-side pickup using mesh bounds (works with CharacterController).</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(5)]
public class DutzHealthPotionCollector : MonoBehaviour
{
    const float BoundsPadding = 3f;

    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            enabled = false;
    }

    void Start() => DutzHealthPotionRegistry.EnsurePotionsAreReady();

    void LateUpdate()
    {
        if (!enabled || cc == null)
            return;

        var playerBounds = GetPlayerPickupBounds();
        foreach (var potion in DutzHealthPotionRegistry.GetPotions())
        {
            if (potion == null || potion.IsCollected || !potion.gameObject.activeInHierarchy)
                continue;

            var renderer = potion.PickupRenderer;
            if (renderer == null)
                continue;

            if (!playerBounds.Intersects(renderer.bounds))
                continue;

            DutzHealthPotionRegistry.Collect(potion, GetComponent<DutzPlayerHitPoints>());
        }
    }

    Bounds GetPlayerPickupBounds()
    {
        var center = transform.position + cc.center;
        var size = new Vector3(
            (cc.radius + BoundsPadding) * 2f,
            cc.height + BoundsPadding * 2f,
            (cc.radius + BoundsPadding) * 2f);
        return new Bounds(center, size);
    }
}

/// <summary>Tracks Level 3 health potions and applies healing on pickup.</summary>
public static class DutzHealthPotionRegistry
{
    const string PotionsRootName = "DutzHealthPotions";
    const int SfxSampleRate = 44100;

    static DutzHealthPotion[] potions = System.Array.Empty<DutzHealthPotion>();
    static AudioClip collectClip;
    static AudioSource oneShotSource;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        EnsurePotionsAreReady();
    }

    public static void EnsurePotionsAreReady()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        PatchMissingPotionScripts();
        RefreshPotionRegistry();
        RepairBrokenPotionVisuals();
    }

    static void RepairBrokenPotionVisuals()
    {
        var root = GameObject.Find(PotionsRootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (!DutzHealthPotion.IsTrackPotionRoot(child.gameObject))
                continue;

            if (DutzHealthPotionSetup.IsRedTrackPotion(child.gameObject))
                DutzHealthPotionSetup.ApplyRedVisual(child.gameObject);
            else
                DutzHealthPotionSetup.ApplyGreenVisual(child.gameObject);
        }
    }

    public static DutzHealthPotion[] GetPotions() => potions;

    static void PatchMissingPotionScripts()
    {
        var root = GameObject.Find(PotionsRootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (!DutzHealthPotion.IsTrackPotionRoot(child.gameObject))
                continue;

            if (child.GetComponent<DutzHealthPotion>() == null)
                child.gameObject.AddComponent<DutzHealthPotion>();
        }
    }

    static void RefreshPotionRegistry()
    {
        potions = Object.FindObjectsOfType<DutzHealthPotion>(true);
    }

    public static void Collect(DutzHealthPotion potion, DutzPlayerHitPoints playerHitPoints)
    {
        if (potion == null || potion.IsCollected)
            return;

        potion.HideCollected();
        if (DutzHealthPotionSetup.IsRedTrackPotion(potion.gameObject))
            playerHitPoints?.HealUncapped(DutzHealthPotion.Bridge5RedHealAmount);
        else
            playerHitPoints?.Heal(potion.HealAmountValue);
        PlayCollectSfx(potion.HealAmountValue);
    }

    static void PlayCollectSfx(int healAmount)
    {
        EnsureAudio();
        if (collectClip == null || oneShotSource == null)
            return;

        oneShotSource.pitch = healAmount >= DutzHealthPotion.Bridge5RedHealAmount
            ? Random.Range(1.02f, 1.12f)
            : Random.Range(0.94f, 1.06f);
        oneShotSource.PlayOneShot(
            collectClip,
            DutzAudioSettings.ScaleSfx(healAmount >= DutzHealthPotion.Bridge5RedHealAmount ? 0.95f : 0.75f));
    }

    static void EnsureAudio()
    {
        if (oneShotSource != null)
            return;

        var go = new GameObject("DutzHealthPotionAudio");
        Object.DontDestroyOnLoad(go);
        oneShotSource = go.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.volume = 1f;

        if (collectClip == null)
            collectClip = CreateCollectClip();
    }

    static AudioClip CreateCollectClip()
    {
        const float length = 0.28f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var bubble = Mathf.Sin(2f * Mathf.PI * 420f * t) * Mathf.Exp(-t * 18f);
            var chime = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 28f) * 0.4f;
            data[i] = (bubble * 0.65f + chime) * 0.6f;
        }

        var clip = AudioClip.Create("DutzHealthPotionCollect", samples, 1, SfxSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static void ResetOnPlayerRespawn()
    {
        EnsurePotionsAreReady();
        foreach (var potion in potions)
        {
            if (potion != null)
                potion.ResetForRespawn();
        }
    }
}
