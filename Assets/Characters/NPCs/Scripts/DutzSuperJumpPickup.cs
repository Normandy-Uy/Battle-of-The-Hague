using UnityEngine;

/// <summary>Super Jump pickup — UPARROW or kangaroo icon.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(7)]
public class DutzSuperJumpPickup : MonoBehaviour
{
    public const string PickupObjectName = "DutzSuperJumpPickup";

    const float PlayerTouchPaddingMeters = 0.15f;
    const float PickupBoundsMarginMeters = 0.35f;

    [SerializeField] float spinSpeed = 90f;
    [SerializeField] float bobAmplitude = 0.2f;
    [SerializeField] float bobFrequency = 1.25f;

    [Header("Jump Power")]
    [Tooltip("Upward launch velocity granted on collect. Higher = jumps higher.")]
    [SerializeField] float superJumpForce = DutzPlayerController.SuperJumpForceDefault;
    [Tooltip("Level07 only: number of super jumps granted. Other levels grant Super Jump for the whole life.")]
    [SerializeField] int superJumpCharges = 4;

    bool collected;
    Vector3 basePosition;
    float bobPhase;
    Transform arrowVisual;
    static bool levelCollected;
    static DutzSuperJumpPickup cachedPickup;

    public bool IsCollected => collected || levelCollected;

    /// <summary>Launch velocity this pickup grants.</summary>
    public float SuperJumpForce => superJumpForce;

    /// <summary>Level07 charge count (min 1).</summary>
    public int SuperJumpCharges => Mathf.Max(1, superJumpCharges);

    /// <summary>Approx jump apex height in meters, for inspector preview.</summary>
    public float EstimatedJumpHeightMeters => DutzPlayerController.EstimateJumpHeight(superJumpForce);

    public static bool IsLevelCollected() => levelCollected;

    public static void ResetForSceneLoad()
    {
        levelCollected = false;
        cachedPickup = null;
    }

    public static void ResetOnPlayerRespawn()
    {
        ResetForSceneLoad();

        var pickup = GameObject.Find(PickupObjectName);
        if (pickup == null)
            return;

        var component = pickup.GetComponent<DutzSuperJumpPickup>();
        if (component == null)
        {
            EnsureOnScenePickup();
            component = pickup.GetComponent<DutzSuperJumpPickup>();
        }

        component?.ResetForRespawn();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLevelCollected() => ResetForSceneLoad();

    public static DutzSuperJumpPickup FindPickup()
    {
        if (cachedPickup != null && cachedPickup.isActiveAndEnabled && !cachedPickup.IsCollected)
            return cachedPickup;

        cachedPickup = FindObjectOfType<DutzSuperJumpPickup>();
        return cachedPickup;
    }

    public static void EnsureOnScenePickup()
    {
        var pickup = GameObject.Find(PickupObjectName);
        if (pickup == null)
            return;

        if (!pickup.activeSelf)
            pickup.SetActive(true);

        if (pickup.GetComponent<DutzSuperJumpPickup>() == null)
            pickup.AddComponent<DutzSuperJumpPickup>();

        DutzSuperJumpPickupSetup.Apply(pickup);
    }

    void Reset() => DutzSuperJumpPickupSetup.Apply(gameObject);

    void Awake()
    {
        DetachToSceneRoot();
        DutzSuperJumpPickupSetup.Apply(gameObject);
        arrowVisual = transform.Find(DutzSuperJumpPickupSetup.VisualChildName);
        basePosition = transform.position;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void DetachToSceneRoot()
    {
        if (transform.parent == null)
            return;

        transform.SetParent(null, true);
    }

    void OnEnable() => cachedPickup = this;

    void OnDisable()
    {
        if (cachedPickup == this)
            cachedPickup = null;
    }

    void Update()
    {
        if (IsCollected)
            return;

        if (arrowVisual != null)
            arrowVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        var bob = Mathf.Sin(Time.time * bobFrequency + bobPhase) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bob;
    }

    void FixedUpdate()
    {
        if (IsCollected)
            return;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.MovePosition(transform.position);
    }

    void OnTriggerEnter(Collider other) => TryCollectFromCollider(other);

    void OnTriggerStay(Collider other) => TryCollectFromCollider(other);

    void TryCollectFromCollider(Collider other)
    {
        if (IsCollected || other == null)
            return;

        var player = other.GetComponent<DutzPlayerController>()
            ?? other.GetComponentInParent<DutzPlayerController>();
        if (player != null)
            Collect(player);
    }

    public bool IsPlayerTouching(CharacterController cc)
    {
        if (cc == null)
            return false;

        return GetPickupBounds().Intersects(GetPlayerPickupBounds(cc));
    }

    public Bounds GetPickupBounds()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            bounds.Expand(PickupBoundsMarginMeters);
            return bounds;
        }

        var trigger = GetComponent<SphereCollider>();
        if (trigger != null)
            return trigger.bounds;

        return new Bounds(transform.position, Vector3.one * (2f + PickupBoundsMarginMeters));
    }

    static Bounds GetPlayerPickupBounds(CharacterController cc)
    {
        var scale = Mathf.Max(0.01f, cc.transform.lossyScale.y);
        var center = cc.transform.position + cc.center * scale;
        var radius = (cc.radius + PlayerTouchPaddingMeters) * scale;
        var height = (cc.height + PlayerTouchPaddingMeters * 2f) * scale;
        return new Bounds(center, new Vector3(radius * 2f, height, radius * 2f));
    }

    public void Collect(DutzPlayerController player)
    {
        if (IsCollected || player == null)
            return;

        if (player.HasSuperJumpActive)
            return;

        collected = true;
        levelCollected = true;
        gameObject.SetActive(false);
        DutzPowerupPickupSounds.Play(DutzPowerupPickupSounds.Kind.SuperJump);

        if (DutzCollectibleProgress.IsLevel07)
        {
            player.EnableSuperJumpCharges(SuperJumpCharges, superJumpForce);
            Debug.Log($"[Dutz] Super Jump collected — {SuperJumpCharges} charges (~{EstimatedJumpHeightMeters:0.0} m high).");
            return;
        }

        player.EnableSuperJumpForLife(superJumpForce);
        Debug.Log($"[Dutz] Super Jump collected — high jump active for this life (~{EstimatedJumpHeightMeters:0.0} m high).");
    }

    public void ResetForRespawn()
    {
        collected = false;
        transform.position = basePosition;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.position = basePosition;

        if (arrowVisual == null)
            arrowVisual = transform.Find(DutzSuperJumpPickupSetup.VisualChildName);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}

/// <summary>UPARROW billboard (Level00) / Kangaroo_Low_Poly.fbx (Level07) for Super Jump.</summary>
public static class DutzSuperJumpPickupSetup
{
    public const string VisualChildName = "ArrowVisual";
    const string GlowName = "ArrowGlow";
    const string LegacyKangarooPrismMeshName = "DutzSuperJumpKangarooPrism";
    const string KangarooModelResourceName = "DutzSuperJumpKangarooVisual";
    const string KangarooModelPrefabAssetPath = "Assets/Resources/DutzSuperJumpKangarooVisual.prefab";
    const string TextureResourceName = "DutzSuperJumpArrow";
    const string TextureAssetPath = "Assets/Resources/DutzSuperJumpArrow.png";
    const string MaterialResourceName = "DutzSuperJumpArrow";
    const string MaterialAssetPath = "Assets/Resources/DutzSuperJumpArrow.mat";
    public const float TargetVisualHeight = 2f;
    public const float KangarooVisualHeight = 2.4f;

    public static void Apply(GameObject root)
    {
        if (root == null)
            return;

        StripPrimitiveMesh(root);
        if (DutzCollectibleProgress.IsLevel07)
            EnsureKangaroo3DVisual(root.transform, forceRebuild: false);
        else
            EnsureArrowVisual(root.transform);
        EnsurePickupPhysics(root);
        EnsureGlowLight(root.transform);
    }

    /// <summary>Editor/runtime: Level07 Super Jump uses public/Kangaroo_Low_Poly.fbx.</summary>
    public static void EnsureKangaroo3DVisual(Transform root, bool forceRebuild)
    {
        if (root == null)
            return;

        if (!forceRebuild && HasKangaroo3DVisual(root))
            return;

        var existing = root.Find(VisualChildName);
        if (existing != null)
        {
            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        var prefab = Resources.Load<GameObject>(KangarooModelResourceName);
#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(KangarooModelPrefabAssetPath);
#endif
        if (prefab == null)
        {
            Debug.LogWarning("[Dutz] Missing Super Jump kangaroo FBX prefab in Resources: " + KangarooModelResourceName);
            return;
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = VisualChildName;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        StripColliders(visual);
        NormalizeVisualScale(visual.transform, KangarooVisualHeight);
    }

    public static bool HasKangaroo3DVisual(Transform root)
    {
        var visual = root.Find(VisualChildName);
        if (visual == null)
            return false;

        // Reject legacy PNG prism so auto-sync upgrades to the FBX.
        var filter = visual.GetComponent<MeshFilter>();
        if (filter != null
            && filter.sharedMesh != null
            && filter.sharedMesh.name == LegacyKangarooPrismMeshName)
            return false;

        return visual.GetComponentsInChildren<Renderer>(true).Length > 0;
    }

    public static bool HasArrowVisual(Transform root)
    {
        var visual = root.Find(VisualChildName);
        return visual != null && visual.GetComponent<MeshRenderer>() != null;
    }

    static void EnsureArrowVisual(Transform root)
    {
        if (HasArrowVisual(root) && !HasKangaroo3DVisual(root))
            return;

        var existing = root.Find(VisualChildName);
        if (existing != null)
        {
            if (Application.isPlaying)
                Object.Destroy(existing.gameObject);
            else
                Object.DestroyImmediate(existing.gameObject);
        }

        var material = LoadSharedMaterial();
        if (material == null)
        {
            Debug.LogWarning("[Dutz] Missing Super Jump arrow material in Resources.");
            return;
        }

        var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = VisualChildName;
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;

        var faceDir = Vector3.forward;
        DutzHighwayDirection.InvalidateReferenceCache();
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out _, out var travelForward))
            faceDir = travelForward;

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.0001f)
            faceDir = Vector3.forward;

        visual.transform.rotation = Quaternion.LookRotation(-faceDir.normalized, Vector3.up);

        var collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
                Object.Destroy(collider);
            else
                Object.DestroyImmediate(collider);
        }

        var texture = LoadSharedTexture();
        var renderer = visual.GetComponent<MeshRenderer>();
        if (texture != null)
        {
            var instanceMaterial = new Material(material) { mainTexture = texture };
            renderer.sharedMaterial = instanceMaterial;
        }
        else
        {
            renderer.sharedMaterial = material;
        }

        var aspect = texture != null
            ? texture.width / (float)Mathf.Max(1, texture.height)
            : 0.75f;
        var height = TargetVisualHeight;
        var width = height * aspect;
        visual.transform.localScale = new Vector3(width, height, 1f);
    }

    static Texture2D LoadSharedTexture()
    {
        var texture = Resources.Load<Texture2D>(TextureResourceName);
#if UNITY_EDITOR
        if (texture == null)
            texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
#endif
        return texture;
    }

    static Material LoadSharedMaterial()
    {
        var material = Resources.Load<Material>(MaterialResourceName);
#if UNITY_EDITOR
        if (material == null)
            material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
#endif
        return material;
    }

    static void StripColliders(GameObject visual)
    {
        foreach (var col in visual.GetComponentsInChildren<Collider>(true))
        {
            if (col == null)
                continue;

            if (Application.isPlaying)
                Object.Destroy(col);
            else
                Object.DestroyImmediate(col);
        }
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

    static void StripPrimitiveMesh(GameObject root)
    {
        var meshFilter = root.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            if (Application.isPlaying)
                Object.Destroy(meshFilter);
            else
                Object.DestroyImmediate(meshFilter);
        }

        var meshRenderer = root.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (Application.isPlaying)
                Object.Destroy(meshRenderer);
            else
                Object.DestroyImmediate(meshRenderer);
        }
    }

    static void EnsurePickupPhysics(GameObject root)
    {
        var rb = root.GetComponent<Rigidbody>();
        if (rb == null)
            rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var trigger = root.GetComponent<SphereCollider>();
        if (trigger == null)
        {
            foreach (var col in root.GetComponents<Collider>())
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }

            trigger = root.AddComponent<SphereCollider>();
        }

        trigger.isTrigger = true;
        trigger.center = Vector3.zero;
        trigger.radius = DutzCollectibleProgress.IsLevel07 ? 1.15f : 0.85f;
        trigger.enabled = true;
    }

    static void EnsureGlowLight(Transform root)
    {
        if (root.Find(GlowName) != null)
            return;

        var lightGo = new GameObject(GlowName);
        lightGo.transform.SetParent(root, false);
        lightGo.transform.localPosition = Vector3.zero;
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.92f, 0.25f);
        light.intensity = 3f;
        light.range = 18f;
        light.shadows = LightShadows.None;
    }
}

/// <summary>Collects Super Jump via CharacterController bounds.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(6)]
public class DutzSuperJumpCollector : MonoBehaviour
{
    CharacterController cc;
    DutzPlayerController player;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        player = GetComponent<DutzPlayerController>();
        DutzSuperJumpPickup.EnsureOnScenePickup();
    }

    void FixedUpdate()
    {
        if (cc == null || player == null)
            return;

        if (DutzSuperJumpPickup.IsLevelCollected())
            return;

        var pickup = DutzSuperJumpPickup.FindPickup();
        if (pickup == null || pickup.IsCollected || !pickup.gameObject.activeInHierarchy)
            return;

        if (!pickup.IsPlayerTouching(cc))
            return;

        pickup.Collect(player);
    }
}
