using UnityEngine;

/// <summary>Super Punch pickup — big boxing glove; enables SUPERPUNCH_DAMAGE from Inspector for this life.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(7)]
public class DutzSuperPunchPickup : MonoBehaviour
{
    public const string PickupObjectName = "DutzSuperPunchPickup";

    const float PlayerTouchPaddingMeters = 0.15f;
    const float PickupBoundsMarginMeters = 0.35f;

    [SerializeField] float spinSpeed = 75f;
    [SerializeField] float bobAmplitude = 0.2f;
    [SerializeField] float bobFrequency = 1.2f;

    bool collected;
    Vector3 basePosition;
    float bobPhase;
    Transform gloveVisual;
    static bool levelCollected;
    static DutzSuperPunchPickup cachedPickup;

    public bool IsCollected => collected || levelCollected;

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

        var component = pickup.GetComponent<DutzSuperPunchPickup>();
        if (component == null)
        {
            EnsureOnScenePickup();
            component = pickup.GetComponent<DutzSuperPunchPickup>();
        }

        component?.ResetForRespawn();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLevelCollected() => ResetForSceneLoad();

    public static DutzSuperPunchPickup FindPickup()
    {
        if (cachedPickup != null && cachedPickup.isActiveAndEnabled && !cachedPickup.IsCollected)
            return cachedPickup;

        cachedPickup = FindObjectOfType<DutzSuperPunchPickup>();
        return cachedPickup;
    }

    public static void EnsureOnScenePickup()
    {
        var pickup = GameObject.Find(PickupObjectName);
        if (pickup == null)
            return;

        if (pickup.GetComponent<DutzSuperPunchPickup>() == null)
            pickup.AddComponent<DutzSuperPunchPickup>();
    }

    void Reset() => DutzSuperPunchPickupSetup.Apply(gameObject);

    void Awake()
    {
        DetachToSceneRoot();
        DutzSuperPunchPickupSetup.Apply(gameObject);
        gloveVisual = transform.Find(DutzSuperPunchPickupSetup.VisualChildName);
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

        if (gloveVisual != null)
            gloveVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

        var bob = Mathf.Sin(Time.time * bobFrequency + bobPhase) * bobAmplitude;
        transform.position = basePosition + Vector3.up * bob;
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

        collected = true;
        levelCollected = true;
        gameObject.SetActive(false);
        DutzPowerupPickupSounds.Play(DutzPowerupPickupSounds.Kind.SuperPunch);

        DutzPlayerPunch.EnsureFromBoot();
        var punch = player.GetComponent<DutzPlayerPunch>();
        if (punch == null)
        {
            Debug.LogWarning("[Dutz] Super Punch collected but DutzPlayerPunch missing on player.");
            return;
        }

        punch.EnableSuperPunchForLife();
        Debug.Log($"[Dutz] Super Punch collected — {punch.GetCurrentPunchDamage()} damage per punch (SUPERPUNCH_DAMAGE={punch.SuperPunchDamage}).");
    }

    public void ResetForRespawn()
    {
        collected = false;
        transform.position = basePosition;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        if (gloveVisual == null)
            gloveVisual = transform.Find(DutzSuperPunchPickupSetup.VisualChildName);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}

/// <summary>Shared boxergloves.fbx visual for the Super Punch pickup.</summary>
public static class DutzSuperPunchPickupSetup
{
    public const string VisualChildName = "GloveVisual";
    const string SharedMeshResourceName = "DutzSuperPunchGlovesMesh";
    const string SharedMeshAssetPath = "Assets/Resources/DutzSuperPunchGlovesMesh.asset";
    const string GlovesMaterialPath = "Assets/Characters/Level03/Materials/DutzSuperPunchGloves.mat";
    const string GlovesMaterialResourcePath = "Assets/Resources/DutzSuperPunchGloves.mat";
    public const float TargetVisualHeight = 1.6f;

    public static void Apply(GameObject root)
    {
        if (root == null)
            return;

        StripPrimitiveMesh(root);

        if (HasSharedMeshVisual(root.transform))
        {
            EnsurePickupPhysics(root);
            return;
        }

        RemoveLegacyVisual(root.transform);

        var brokenVisual = root.transform.Find(VisualChildName);
        if (brokenVisual != null)
        {
            if (Application.isPlaying)
                Object.Destroy(brokenVisual.gameObject);
            else
                Object.DestroyImmediate(brokenVisual.gameObject);
        }

        if (!TryInstantiateSharedVisual(root.transform))
            Debug.LogWarning("[Dutz] Missing Super Punch gloves mesh in Resources.");

        EnsurePickupPhysics(root);
    }

    public static bool HasSharedMeshVisual(Transform root)
    {
        var visual = root.Find(VisualChildName);
        if (visual == null)
            return false;

        var filter = visual.GetComponent<MeshFilter>();
        return filter != null && filter.sharedMesh != null;
    }

    static void RemoveLegacyVisual(Transform root)
    {
        var legacy = root.Find(VisualChildName);
        if (legacy == null)
            return;

        if (legacy.GetComponent<MeshFilter>()?.sharedMesh != null)
            return;

        if (Application.isPlaying)
            Object.Destroy(legacy.gameObject);
        else
            Object.DestroyImmediate(legacy.gameObject);
    }

    static bool TryInstantiateSharedVisual(Transform root)
    {
        var mesh = LoadSharedMesh();
        var material = LoadSharedMaterial();
        if (mesh == null || material == null)
            return false;

        var visual = new GameObject(VisualChildName);
        visual.transform.SetParent(root, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        visual.AddComponent<MeshRenderer>().sharedMaterial = material;
        var height = Mathf.Max(0.001f, mesh.bounds.size.y);
        visual.transform.localScale = Vector3.one * (TargetVisualHeight / height);
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

    static Material LoadSharedMaterial()
    {
        var runtimeMat = Resources.Load<Material>("DutzSuperPunchGloves");
#if UNITY_EDITOR
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialResourcePath);
        if (runtimeMat == null)
            runtimeMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(GlovesMaterialPath);
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
        trigger.center = new Vector3(0f, 0f, 0f);
        trigger.radius = 0.85f;
        trigger.enabled = true;
    }
}

/// <summary>Collects Super Punch via CharacterController bounds.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(6)]
public class DutzSuperPunchCollector : MonoBehaviour
{
    CharacterController cc;
    DutzPlayerController player;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        player = GetComponent<DutzPlayerController>();
        DutzSuperPunchPickup.EnsureOnScenePickup();
    }

    void FixedUpdate()
    {
        if (cc == null || player == null)
            return;

        if (DutzSuperPunchPickup.IsLevelCollected())
            return;

        var pickup = DutzSuperPunchPickup.FindPickup();
        if (pickup == null || pickup.IsCollected || !pickup.gameObject.activeInHierarchy)
            return;

        if (!pickup.IsPlayerTouching(cc))
            return;

        pickup.Collect(player);
    }
}
