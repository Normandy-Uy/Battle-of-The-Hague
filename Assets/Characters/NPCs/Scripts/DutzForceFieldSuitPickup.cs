using UnityEngine;

/// <summary>Builds armor-vest visuals + trigger rigidbody for the force field suit pickup.</summary>
public static class DutzForceFieldSuitSetup
{
    const string VisualName = "SuitModelVisual";
    public static string SuitModelVisualName => VisualName;
    const string LegacyVisualName = "VestVisual";
    const string ModelResourceName = "DutzForceFieldSuitVisual";
    const string ModelPrefabAssetPath = "Assets/Resources/DutzForceFieldSuitVisual.prefab";
    const string GlowName = "SuitGlow";
    const float TargetVisualHeight = 1.6f;

    public static void Apply(GameObject root)
    {
        if (root == null)
            return;

        StripPrimitiveMesh(root);
        RemoveLegacyVisual(root.transform);
        EnsureFbxVisual(root.transform);
        EnsurePickupPhysics(root);
        EnsureGlowLight(root.transform);
    }

    static void RemoveLegacyVisual(Transform root)
    {
        var legacy = root.Find(LegacyVisualName);
        if (legacy == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(legacy.gameObject);
        else
            Object.DestroyImmediate(legacy.gameObject);
    }

    static void EnsureFbxVisual(Transform root)
    {
        if (root.Find(VisualName) != null)
            return;

        var prefab = Resources.Load<GameObject>(ModelResourceName);
#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefabAssetPath);
#endif
        if (prefab == null)
        {
            Debug.LogWarning("[Dutz] Missing force field suit model prefab in Resources.");
            return;
        }

        var visual = Object.Instantiate(prefab, root, false);
        visual.name = VisualName;
        StripColliders(visual);
        NormalizeVisualScale(visual.transform, TargetVisualHeight);
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
        var scaleFactor = targetHeight / height;
        visual.localScale *= scaleFactor;
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

        var oldCol = root.GetComponent<Collider>();
        if (oldCol != null && !oldCol.isTrigger)
        {
            if (Application.isPlaying)
                Object.Destroy(oldCol);
            else
                Object.DestroyImmediate(oldCol);
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
        trigger.center = new Vector3(0f, 0.05f, 0f);
        trigger.radius = 0.75f;
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
        light.color = new Color(0.35f, 0.9f, 1f);
        light.intensity = 3.5f;
        light.range = 22f;
        light.shadows = LightShadows.None;
    }
}

/// <summary>Force field suit collectible. Scene-authored transform is the play spawn pose.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(7)]
public class DutzForceFieldSuitPickup : MonoBehaviour
{
    public const string PickupObjectName = "DutzForceFieldSuit";
    const float PlayerTouchPaddingMeters = 0.15f;
    const float PickupBoundsMarginMeters = 0.25f;

    [SerializeField] float spinSpeed = 90f;
    [SerializeField] float bobAmplitude = 0.15f;
    [SerializeField] float bobFrequency = 1.4f;

    bool collected;
    Vector3 basePosition;
    float bobPhase;
    Transform suitVisual;
    static bool levelCollected;
    static DutzForceFieldSuitPickup cachedPickup;

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

        var suit = GameObject.Find(PickupObjectName);
        if (suit == null)
            return;

        var pickup = suit.GetComponent<DutzForceFieldSuitPickup>();
        if (pickup == null)
        {
            EnsureOnSceneSuit();
            pickup = suit.GetComponent<DutzForceFieldSuitPickup>();
        }

        pickup?.ResetForRespawn();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLevelCollected() => ResetForSceneLoad();

    public static DutzForceFieldSuitPickup FindPickup()
    {
        if (cachedPickup != null && cachedPickup.isActiveAndEnabled && !cachedPickup.IsCollected)
            return cachedPickup;

        cachedPickup = FindObjectOfType<DutzForceFieldSuitPickup>();
        return cachedPickup;
    }

    /// <summary>Repairs scene objects with missing or wrong script slots (e.g. DutzLevelObjective).</summary>
    public static void EnsureOnSceneSuit()
    {
        var suit = GameObject.Find(PickupObjectName);
        if (suit == null)
            return;

        if (!suit.activeSelf)
            suit.SetActive(true);

        foreach (var behaviour in suit.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null
                || behaviour is DutzForceFieldSuitPickup
                || behaviour is DutzForceField)
                continue;

            Object.Destroy(behaviour);
        }

        var pickups = suit.GetComponents<DutzForceFieldSuitPickup>();
        for (var i = 1; i < pickups.Length; i++)
            Object.Destroy(pickups[i]);

        if (suit.GetComponent<DutzForceFieldSuitPickup>() == null)
            suit.AddComponent<DutzForceFieldSuitPickup>();

        // Never move the suit — authored scene transform is the play spawn.
        DutzForceFieldSuitSetup.Apply(suit);
        DutzForceField.EnsureOnSuit(suit);
    }

    void Reset() => DutzForceFieldSuitSetup.Apply(gameObject);

    void Awake()
    {
        DetachToSceneRoot();
        DutzForceFieldSuitSetup.Apply(gameObject);
        DutzForceField.EnsureOnSuit(gameObject);
        suitVisual = transform.Find(DutzForceFieldSuitSetup.SuitModelVisualName);
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

        if (suitVisual != null)
            suitVisual.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

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

        return GetPickupBounds().Intersects(BuildPlayerTouchBounds(cc));
    }

    static Bounds BuildPlayerTouchBounds(CharacterController cc)
    {
        var scale = Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.y, cc.transform.lossyScale.z);
        var center = cc.transform.TransformPoint(cc.center);
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
        DutzPowerupPickupSounds.Play(DutzPowerupPickupSounds.Kind.ForceFieldSuit);

        // Keep this GameObject alive — DutzForceField lives on the suit, not Player1.
        HidePickupPresentation();

        var field = DutzForceField.EnsureOnSuit(gameObject);
        field?.Activate(player);
    }

    void HidePickupPresentation()
    {
        if (suitVisual == null)
            suitVisual = transform.Find(DutzForceFieldSuitSetup.SuitModelVisualName);

        if (suitVisual != null)
            suitVisual.gameObject.SetActive(false);

        var glow = transform.Find("SuitGlow");
        if (glow != null)
            glow.gameObject.SetActive(false);

        foreach (var col in GetComponents<Collider>())
        {
            if (col != null)
                col.enabled = false;
        }

        var bob = GetComponent<Rigidbody>();
        if (bob != null)
            bob.detectCollisions = false;
    }

    public void ResetForRespawn()
    {
        collected = false;
        transform.position = basePosition;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.position = basePosition;
            rb.detectCollisions = true;
        }

        GetComponent<DutzForceField>()?.Deactivate();

        if (suitVisual == null)
            suitVisual = transform.Find(DutzForceFieldSuitSetup.SuitModelVisualName);
        if (suitVisual != null)
            suitVisual.gameObject.SetActive(true);

        var glow = transform.Find("SuitGlow");
        if (glow != null)
            glow.gameObject.SetActive(true);

        foreach (var col in GetComponents<Collider>())
        {
            if (col != null)
                col.enabled = true;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
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

        var scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        return new Bounds(transform.position, Vector3.one * scale * 1.6f);
    }
}

/// <summary>Picks up the force field suit via CharacterController body bounds.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(6)]
public class DutzForceFieldSuitCollector : MonoBehaviour
{
    CharacterController cc;
    DutzPlayerController player;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        player = GetComponent<DutzPlayerController>();
        DutzForceFieldSuitPickup.EnsureOnSceneSuit();
    }

    void FixedUpdate()
    {
        if (cc == null || player == null)
            return;

        if (DutzForceFieldSuitPickup.IsLevelCollected())
            return;

        var pickup = DutzForceFieldSuitPickup.FindPickup();
        if (pickup == null || pickup.IsCollected || !pickup.gameObject.activeInHierarchy)
            return;

        if (!pickup.IsPlayerTouching(cc))
            return;

        pickup.Collect(player);
    }
}
