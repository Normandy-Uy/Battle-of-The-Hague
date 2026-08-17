using UnityEngine;

/// <summary>Collectible suitcase for Level 2 — same pickup flow as gold coins on Level 1.</summary>
[DisallowMultipleComponent]
public class DutzSuitcase : MonoBehaviour
{
    const string SuitcasePrefix = "DutzSuitcase_";
    const float SpinCullDistance = 90f;
    const float SpinCullDistanceSqr = SpinCullDistance * SpinCullDistance;

    [SerializeField] float spinSpeed = 45f;

    [Header("Spawn Pose")]
    [SerializeField] DutzCollectibleSpawnPose spawnPose;

    bool collected;

    public bool IsCollected => collected;

    public DutzCollectibleSpawnPose SpawnPose => spawnPose;

    public static bool IsTrackSuitcaseRoot(GameObject go) =>
        go != null && go.name.StartsWith(SuitcasePrefix, System.StringComparison.Ordinal);

    public void CaptureSpawnPoseFromTransform() => spawnPose = DutzCollectibleSpawnPose.FromTransform(transform);

    public void ApplySpawnPose() => spawnPose.ApplyTo(transform);

    void Awake()
    {
        if (!spawnPose.HasPosition)
            CaptureSpawnPoseFromTransform();

        ApplySpawnPose();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (!spawnPose.HasPosition)
            CaptureSpawnPoseFromTransform();

        ApplySpawnPose();
    }

    void Update()
    {
        if (collected)
            return;

        var player = DutzPlayerController.Instance;
        if (player != null)
        {
            var delta = player.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > SpinCullDistanceSqr)
                return;
        }

        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
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
        gameObject.SetActive(true);
    }
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(5)]
public class DutzSuitcaseCollector : MonoBehaviour
{
    const float BoundsPadding = 3f;

    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start() => DutzSuitcaseCounter.EnsureSuitcasesAreReady();

    void LateUpdate()
    {
        if (cc == null)
            return;

        if ((Time.frameCount & 1) != 0)
            return;

        var playerBounds = GetPlayerPickupBounds();
        foreach (var suitcase in DutzSuitcaseCounter.GetSuitcases())
        {
            if (suitcase == null || suitcase.IsCollected || !suitcase.gameObject.activeInHierarchy)
                continue;

            var renderer = suitcase.GetComponentInChildren<Renderer>();
            if (renderer == null)
                continue;

            if (!playerBounds.Intersects(renderer.bounds))
                continue;

            DutzSuitcaseCounter.Collect(suitcase);
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

[DisallowMultipleComponent]
public class DutzSuitcaseCounter : MonoBehaviour
{
    const string ManagerName = "DutzSuitcaseCounter";
    const string SuitcasesRootName = "DutzSuitcases";
    const int SfxSampleRate = 44100;

    static int totalInLevel;
    static int collected;
    static DutzSuitcase[] suitcases = System.Array.Empty<DutzSuitcase>();
    static AudioClip collectClip;

    AudioSource oneShotSource;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.UsesSuitcases)
            return;

        EnsureManager();
        EnsureSuitcasesAreReady();
    }

    static void EnsureManager()
    {
        if (FindObjectOfType<DutzSuitcaseCounter>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzSuitcaseCounter>();
    }

    void Awake()
    {
        EnsureSuitcasesAreReady();
        EnsureAudio();
    }

    void EnsureAudio()
    {
        oneShotSource = GetComponent<AudioSource>();
        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.volume = 1f;

        if (collectClip == null)
            collectClip = CreateCollectClip();
    }

    public static void EnsureSuitcasesAreReady()
    {
        if (!DutzCollectibleProgress.UsesSuitcases)
            return;

        PatchMissingSuitcaseScripts();
        RefreshSuitcaseRegistry();
    }

    public static DutzSuitcase[] GetSuitcases() => suitcases;

    public static int CollectedCount => collected;

    public static bool TryGetHudCounts(out int collectedCount, out int totalCount)
    {
        collectedCount = collected;
        totalCount = totalInLevel;
        return totalInLevel > 0;
    }

    static void PatchMissingSuitcaseScripts()
    {
        var root = GameObject.Find(SuitcasesRootName);
        if (root == null)
            return;

        foreach (Transform child in root.transform)
        {
            if (!DutzSuitcase.IsTrackSuitcaseRoot(child.gameObject))
                continue;

            if (child.GetComponent<DutzSuitcase>() == null)
                child.gameObject.AddComponent<DutzSuitcase>();
        }
    }

    static void RefreshSuitcaseRegistry()
    {
        suitcases = FindObjectsOfType<DutzSuitcase>(true);
        totalInLevel = suitcases.Length;
    }

    public static void Collect(DutzSuitcase suitcase)
    {
        if (suitcase == null || suitcase.IsCollected || totalInLevel <= 0)
            return;

        suitcase.HideCollected();
        collected = Mathf.Min(collected + 1, totalInLevel);

        var counter = FindObjectOfType<DutzSuitcaseCounter>();
        if (counter != null)
            counter.PlayCollectSfx();
    }

    void PlayCollectSfx()
    {
        EnsureAudio();
        if (collectClip == null || oneShotSource == null)
            return;

        oneShotSource.pitch = Random.Range(0.94f, 1.06f);
        oneShotSource.PlayOneShot(collectClip, DutzAudioSettings.ScaleSfx(0.8f));
    }

    static AudioClip CreateCollectClip()
    {
        const float length = 0.32f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;
            var thump = Mathf.Sin(2f * Mathf.PI * 180f * t) * Mathf.Exp(-t * 38f);
            var click = Mathf.Sin(2f * Mathf.PI * 920f * t) * Mathf.Exp(-t * 52f) * 0.35f;
            data[i] = (thump * 0.7f + click) * 0.65f;
        }

        var clip = AudioClip.Create("DutzSuitcaseCollect", samples, 1, SfxSampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static bool TrySpend(int amount)
    {
        if (amount <= 0 || collected < amount)
            return false;

        collected -= amount;
        return true;
    }

    public static void ResetOnPlayerRespawn()
    {
        collected = 0;
        EnsureSuitcasesAreReady();
        foreach (var suitcase in suitcases)
        {
            if (suitcase != null)
                suitcase.ResetForRespawn();
        }

        DutzGrandmaBossPowerShop.ResetOnPlayerRespawn();
        DutzElevatorVerticalPatrol.ResetOnPlayerRespawn();
    }

    /// <summary>Zero suitcase tally before a fresh scene load (objects respawn with the scene).</summary>
    public static void ResetForSceneLoad()
    {
        collected = 0;
        totalInLevel = 0;
        suitcases = System.Array.Empty<DutzSuitcase>();
    }

    void OnGUI()
    {
        if (!DutzCollectibleProgress.UsesSuitcases || totalInLevel <= 0)
            return;

        DutzCollectibleHudDraw.DrawSuitcases(collected, totalInLevel);
    }
}
