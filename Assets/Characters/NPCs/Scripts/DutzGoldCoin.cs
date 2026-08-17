using UnityEngine;

/// <summary>Collectible gold coin — hides when collected, returns on player respawn.</summary>
[DisallowMultipleComponent]
public class DutzGoldCoin : MonoBehaviour
{
    const string CoinPrefix = "DutzGoldCoin_";
    const float PickupRadius = 3.5f;
    const float BoundsPadding = 3f;
    const float SpinCullDistance = 90f;
    const float SpinCullDistanceSqr = SpinCullDistance * SpinCullDistance;

    [SerializeField] float spinSpeed = 120f;

    [Header("Spawn Pose")]
    [SerializeField] DutzCollectibleSpawnPose spawnPose;

    bool collected;

    public bool IsCollected => collected;

    public float GetPickupRadius() => PickupRadius;

    public bool IsWithinPickupRange(CharacterController cc)
    {
        if (cc == null)
            return false;

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            var playerPos = cc.transform.position + cc.center;
            return (transform.position - playerPos).sqrMagnitude <= PickupRadius * PickupRadius;
        }

        return BuildPlayerPickupBounds(cc).Intersects(renderer.bounds);
    }

    static Bounds BuildPlayerPickupBounds(CharacterController cc)
    {
        var center = cc.transform.position + cc.center;
        var size = new Vector3(
            (cc.radius + BoundsPadding) * 2f,
            cc.height + BoundsPadding * 2f,
            (cc.radius + BoundsPadding) * 2f);
        return new Bounds(center, size);
    }

    public DutzCollectibleSpawnPose SpawnPose => spawnPose;

    public static bool IsTrackCoinRoot(GameObject go) =>
        go != null && go.name.StartsWith(CoinPrefix, System.StringComparison.Ordinal);

    /// <summary>Editor repair: skip Awake/OnValidate pose apply so manual transforms stay put.</summary>
    public static bool SuppressSpawnPoseApply { get; set; }

    public void CaptureSpawnPoseFromTransform() => spawnPose = DutzCollectibleSpawnPose.FromTransform(transform);

    public void ApplySpawnPose() => spawnPose.ApplyTo(transform);

    void Awake()
    {
        if (SuppressSpawnPoseApply)
            return;

        if (!spawnPose.HasPosition)
            CaptureSpawnPoseFromTransform();
    }

    void OnValidate()
    {
        if (Application.isPlaying || SuppressSpawnPoseApply)
            return;

        CaptureSpawnPoseFromTransform();
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

/// <summary>Player-side pickup using distance check (works with CharacterController).</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(5)]
public class DutzGoldCoinCollector : MonoBehaviour
{
    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start() => DutzGoldCoinCounter.EnsureCoinsAreReady();

    void LateUpdate()
    {
        if (cc == null)
            return;

        if (DutzGoldCoinCounter.GetCoins().Length == 0)
            DutzGoldCoinCounter.EnsureCoinsAreReady();

        foreach (var coin in DutzGoldCoinCounter.GetCoins())
        {
            if (coin == null || coin.IsCollected || !coin.gameObject.activeInHierarchy)
                continue;

            if (!coin.IsWithinPickupRange(cc))
                continue;

            DutzGoldCoinCounter.Collect(coin);
        }
    }
}

/// <summary>Tracks collected gold coins and draws a top-right HUD counter.</summary>
[DisallowMultipleComponent]
public class DutzGoldCoinCounter : MonoBehaviour
{
    const string ManagerName = "DutzGoldCoinCounter";
    const string CoinsRootName = "DutzGoldCoins";
    const int SfxSampleRate = 44100;

    static int totalInLevel;
    static int collected;
    static DutzGoldCoin[] coins = System.Array.Empty<DutzGoldCoin>();
    static AudioClip kaChingClip;

    AudioSource oneShotSource;

    public static void EnsureFromBoot()
    {
        if (DutzCollectibleProgress.UsesSuitcases)
            return;

        EnsureManager();
        EnsureCoinsAreReady();
    }

    static void EnsureManager()
    {
        if (FindObjectOfType<DutzGoldCoinCounter>() != null)
            return;

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzGoldCoinCounter>();
    }

    void Awake()
    {
        EnsureCoinsAreReady();
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

        if (kaChingClip == null)
            kaChingClip = CreateKaChingClip();
    }

    public static void EnsureCoinsAreReady()
    {
        PatchMissingCoinScripts();
        RefreshCoinRegistry();
    }

    public static DutzGoldCoin[] GetCoins() => coins;

    public static int CollectedCount => collected;

    static void PatchMissingCoinScripts()
    {
        var root = GameObject.Find(CoinsRootName);
        if (root == null)
            return;

        if (!root.activeSelf)
            root.SetActive(true);

        foreach (Transform child in root.transform)
        {
            if (!DutzGoldCoin.IsTrackCoinRoot(child.gameObject))
                continue;

            if (!child.gameObject.activeSelf)
                child.gameObject.SetActive(true);

            foreach (var behaviour in child.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour is DutzGoldCoin)
                    continue;

                Object.Destroy(behaviour);
            }

            if (child.GetComponents<DutzGoldCoin>().Length == 0)
            {
                var coin = child.gameObject.AddComponent<DutzGoldCoin>();
                coin.CaptureSpawnPoseFromTransform();
            }
        }
    }

    static void RefreshCoinRegistry()
    {
        coins = FindObjectsOfType<DutzGoldCoin>(true);
        totalInLevel = coins.Length;
    }

    public static void Collect(DutzGoldCoin coin)
    {
        if (coin == null || coin.IsCollected || totalInLevel <= 0)
            return;

        coin.HideCollected();
        collected = Mathf.Min(collected + 1, totalInLevel);

        var counter = FindObjectOfType<DutzGoldCoinCounter>();
        if (counter != null)
            counter.PlayKaChing();
    }

    void PlayKaChing()
    {
        EnsureAudio();
        if (kaChingClip == null || oneShotSource == null)
            return;

        oneShotSource.pitch = Random.Range(0.96f, 1.04f);
        oneShotSource.PlayOneShot(kaChingClip, DutzAudioSettings.ScaleSfx(0.85f));
    }

    static AudioClip CreateKaChingClip()
    {
        const float length = 0.38f;
        var samples = Mathf.Max(1, Mathf.CeilToInt(SfxSampleRate * length));
        var data = new float[samples];

        for (var i = 0; i < samples; i++)
        {
            var t = i / (float)SfxSampleRate;

            var dingT = Mathf.Max(0f, t - 0.01f);
            var dingEnv = Mathf.Exp(-dingT * 48f);
            var ding = Mathf.Sin(2f * Mathf.PI * 1180f * t) * dingEnv;

            var chingT = Mathf.Max(0f, t - 0.09f);
            var chingEnv = Mathf.Exp(-chingT * 42f);
            var ching = Mathf.Sin(2f * Mathf.PI * 1960f * (t - 0.02f)) * chingEnv;

            var shimmer = Mathf.Sin(2f * Mathf.PI * 2650f * t) * Mathf.Exp(-t * 55f) * 0.25f;
            data[i] = (ding * 0.55f + ching * 0.65f + shimmer) * 0.7f;
        }

        var clip = AudioClip.Create("DutzKaChing", samples, 1, SfxSampleRate, false);
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
        EnsureCoinsAreReady();
        foreach (var coin in coins)
        {
            if (coin != null)
                coin.ResetForRespawn();
        }

        DutzGrandmaBossPowerShop.ResetOnPlayerRespawn();
    }

    public static void ResetForSceneLoad()
    {
        collected = 0;
        totalInLevel = 0;
        coins = System.Array.Empty<DutzGoldCoin>();
    }

    void OnGUI()
    {
        if (DutzCollectibleProgress.UsesSuitcases || totalInLevel <= 0)
            return;

        DutzCollectibleHudDraw.DrawCoins(collected);
    }
}
