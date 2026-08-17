using UnityEngine;

/// <summary>
/// Level07 chase giants/crocs stay on their authored home highway.
/// Used for deck clamp and chase leash (no cross-highway pursuits).
/// </summary>
public static class DutzLevel07GiantHomes
{
    public const string Highway8 = "Highway 8";
    public const string Highway7 = "Highway 7";
    public const string Straight2 = "Highway Straight 2";
    public const string Straight3 = "Highway Straight 3";
    public const string Bridge1 = "Highway Bridge 1";
    public const string Bridge4 = "Highway Bridge 4";
    public const string Bridge5 = "Highway Bridge 5";

    /// <summary>Player must be this close (flat) to the home deck to keep chase active.</summary>
    public const float ChaseNearHighwayMeters = 42f;

    public static bool TryGetHomeHighway(string objectName, out string highwayObjectName)
    {
        highwayObjectName = null;
        if (string.IsNullOrEmpty(objectName) || !DutzCollectibleProgress.IsLevel07)
            return false;

        // Highway 6 Boy Idol — never clamp/leash; leave authored as-is.
        if (DutzGiantBossNames.IsBoyIdol(objectName))
            return false;

        // Piyaya — no home lock (Bridge 1 clamp made her vanish in play).
        if (DutzGiantBossNames.IsPiyaya(objectName))
            return false;

        // Homes locked from MCP Level07 positions (MeshRenderer.bounds).
        // Highway 8: MARKO LEKTA + M BILYAR (+ crocs).
        if (DutzGiantBossNames.IsMarkoLekta(objectName)
            || DutzGiantBossNames.IsMBilyar(objectName)
            || objectName.StartsWith("Level07_Highway8_Croc_", System.StringComparison.Ordinal))
        {
            highwayObjectName = Highway8;
            return true;
        }

        // Highway 7: Cawetan (+ HONTAVIRUS if restored) (+ crocs).
        if (DutzGiantBossNames.IsHontavirus(objectName)
            || DutzGiantBossNames.IsCawetan(objectName)
            || objectName.StartsWith("Level07_Highway7_Croc_", System.StringComparison.Ordinal))
        {
            highwayObjectName = Highway7;
            return true;
        }

        // Straight 2: STONE + K Bilyar (+ RAPTOR if restored) (+ small addicts).
        if (string.Equals(objectName, "RAPTOR", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsKBilyar(objectName)
            || DutzGiantBossNames.IsStone(objectName)
            || DutzLevel07Straight3AddictSpawner.IsStraight2Addict(objectName))
        {
            highwayObjectName = Straight2;
            return true;
        }

        // Straight 3: I am baby (+ small addicts).
        if (DutzGiantBossNames.IsIAmBaby(objectName)
            || DutzLevel07Straight3AddictSpawner.IsStraight3Addict(objectName))
        {
            highwayObjectName = Straight3;
            return true;
        }

        // Bridge 1: Lie Fivex only if restored. Piyaya is intentionally unlocked —
        // home-highway clamp made her disappear in play.
        if (string.Equals(objectName, "Lie Fivex", System.StringComparison.Ordinal))
        {
            highwayObjectName = Bridge1;
            return true;
        }

        // Gong Bong / Liron — no home lock. Bridge multi-deck MeshColliders made
        // home clamp teleport/blink them (same failure mode as Piyaya on Bridge 1).
        if (DutzGiantBossNames.IsGongBong(objectName))
            return false;

        if (DutzGiantBossNames.IsLironSinta(objectName))
            return false;

        return false;
    }

    public static bool HasHomeHighway(string objectName) =>
        TryGetHomeHighway(objectName, out _);

    public static bool TryClampOntoHomeHighway(string objectName, ref Vector3 worldPosition, float pivotToFeet)
    {
        if (!TryGetHomeHighway(objectName, out var highway))
            return false;

        if (highway == Straight2)
            return DutzRoadGround.TryClampOntoLevel07Straight2Deck(ref worldPosition, pivotToFeet);

        if (highway == Straight3)
            return DutzRoadGround.TryClampOntoLevel07Straight3Deck(ref worldPosition, pivotToFeet);

        return DutzRoadGround.TryClampOntoLevel07NamedHighwayDeck(highway, ref worldPosition, pivotToFeet);
    }

    /// <summary>
    /// True when the player is close enough to this giant's home highway deck to allow chase.
    /// Uses AABB proximity (no per-hunter raycast) and caches the result per highway per frame.
    /// </summary>
    public static bool IsPlayerNearHomeHighway(string objectName, Vector3 playerWorldPosition)
    {
        if (!TryGetHomeHighway(objectName, out var highway))
            return true;

        return DutzRoadGround.IsNearHighwayAabb(highway, playerWorldPosition, ChaseNearHighwayMeters);
    }
}

/// <summary>
/// Level07 — Senate mural dialog unlocks only after Boy Idol is killed.
/// </summary>
public static class DutzLevel07BoyIdolGate
{
    static GameObject cachedBoyIdol;
    static int cachedBoyIdolFrame = -1;
    static bool defeatedThisSession;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticState() => ResetForSceneLoad();

    /// <summary>Clear sticky defeat when reloading / repeating Level07 within one play session.</summary>
    public static void ResetForSceneLoad()
    {
        cachedBoyIdol = null;
        cachedBoyIdolFrame = -1;
        defeatedThisSession = false;
    }

    public static bool IsBoyIdolDefeated
    {
        get
        {
            // destroyOnDeath used to wipe the GO — keep a sticky flag so Senate/win still unlock.
            if (defeatedThisSession)
                return true;

            var boy = FindBoyIdol();
            if (boy == null)
                return false;

            var hp = boy.GetComponent<DutzNpcHitPoints>();
            return hp != null && hp.IsDead;
        }
    }

    public static void MarkDefeated() => defeatedThisSession = true;

    public static GameObject FindBoyIdol()
    {
        if (cachedBoyIdol != null && cachedBoyIdolFrame == Time.frameCount)
            return cachedBoyIdol;

        cachedBoyIdol = GameObject.Find(DutzGiantBossNames.BoyIdol) ?? DutzGiantBossNames.FindBoyIdol();
        cachedBoyIdolFrame = Time.frameCount;
        return cachedBoyIdol;
    }

    public static bool IsBoyIdol(GameObject target) =>
        target != null && DutzGiantBossNames.IsBoyIdol(target.name);

    public static bool IsTouchingBoyIdolHeat(CharacterController playerCc)
    {
        if (playerCc == null)
            return false;

        var heats = DutzGiantHeat.AllActive;
        if (heats == null || heats.Count == 0)
            return false;

        for (var i = 0; i < heats.Count; i++)
        {
            var heat = heats[i];
            if (heat == null || !heat.enabled)
                continue;
            if (!IsBoyIdol(heat.gameObject))
                continue;
            if (heat.IsTouchingPlayer(playerCc))
                return true;
        }

        return false;
    }
}
