using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level 3 finale — at Highway Straight 6, wake BEYBI M; track E-TOLs stay active; defeating BEYBI M wins.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(900)]
public class DutzLevel03Finale : MonoBehaviour
{
    const string ManagerName = "DutzLevel03Finale";
    const string HighwaySixSegmentName = "Highway Straight 6";
    const string HighwayFiveSegmentName = "Highway Bridge 5";
    const string BossDefeatedWinMessage = "BEYBI M defeated! Dutz is free!";
    const float FinaleStartupGraceSeconds = 3f;
    const float MinSegmentSpan = 10f;
    const float BossHudRange = 180f;
    const float BossIncomingAnnounceRange = 200f;
    const float BossHudTopY = 48f;
    const float PlayerTriggerCheckInterval = 0.25f;

    static float nextPlayerTriggerCheckTime;

    static readonly string[] SegmentNames =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        HighwaySixSegmentName
    };

    DutzPlayerController player;
    List<DutzHighwayDeckSampler.SegmentPath> segments = new List<DutzHighwayDeckSampler.SegmentPath>();
    Vector3 spawnRef;
    Vector3 travelForward;
    DutzNpcHitPoints endEtOlHitPoints;
    bool hasReachedHighwaySix;
    bool hasShownBossIncomingAnnouncement;
    string activeAnnouncement;
    float announcementTimeLeft;
    float levelPlayableTime;
    float nextTrackRebuildTime;
    float nextFinaleStateCheckTime;
    bool bossDefeatHandled;
    static bool staticBossDefeatHandled;

    public static bool HasReachedHighwaySix =>
        Instance != null && Instance.hasReachedHighwaySix;

    public static DutzLevel03Finale Instance { get; private set; }

    public static void ResetStaticStateForNewScene()
    {
        Instance = null;
        staticBossDefeatHandled = false;
        nextPlayerTriggerCheckTime = 0f;
    }

    /// <summary>Idempotent — safe from player movement, boot, or proximity checks.</summary>
    public static void TryTriggerFromPlayerPosition(Vector3 playerPosition)
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay || !DutzDifficulty.HasChosen)
            return;

        if (Time.time < nextPlayerTriggerCheckTime)
            return;

        nextPlayerTriggerCheckTime = Time.time + PlayerTriggerCheckInterval;

        EnsureInstance();
        if (Instance == null || Instance.hasReachedHighwaySix)
            return;

        if (Instance.IsFinaleTriggerPosition(playerPosition))
            Instance.ReachHighwaySix();
    }

    static void EnsureInstance()
    {
        if (Instance != null)
            return;

        var existing = FindObjectOfType<DutzLevel03Finale>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevel03Finale>();
    }

    public static void ResetOnPlayerRespawn()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        if (Instance != null)
            Instance.RestoreHighwayGiantsForRespawn();
        else
            RestoreHighwayGiantsWithoutFinale();
    }

    static void RestoreHighwayGiantsWithoutFinale()
    {
        var trackRoot = GameObject.Find("DutzLevel03TrackGiants");
        if (trackRoot != null)
            trackRoot.SetActive(true);

        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (DutzCollectibleProgress.IsLevel03TrackEtOl(hunter.gameObject.name)
                || DutzCollectibleProgress.IsLevel03BonusGiant(hunter.gameObject.name))
                RestoreGiantForRespawn(hunter);
        }
    }

    public static void EnsureFromBoot()
    {
        // Level07 has no BEYBI M / Level 3 finale — Boy Idol + Senate votes handle the climax.
        if (!DutzCollectibleProgress.IsLevel03)
            return;

        var existing = FindObjectOfType<DutzLevel03Finale>();
        if (existing != null)
        {
            Instance = existing;
            existing.PrepareForNewRun();
            BindEndBossDeathHandler();
            return;
        }

        EnsureTrackGiantsVisible();
        EnsureEndEtOlScale();

        var go = new GameObject(ManagerName);
        go.AddComponent<DutzLevel03Finale>();
        BindEndBossDeathHandler();
    }

    public static void BindEndBossDeathHandler()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null)
            return;

        var hp = endEtOl.GetComponent<DutzNpcHitPoints>();
        if (hp == null)
            hp = DutzNpcHitPoints.EnsureOn(endEtOl, DutzNpcHitPoints.EndEtOlHitPoints);
        else if (!hp.IsDead && hp.MaxHitPoints != DutzNpcHitPoints.EndEtOlHitPoints)
            hp.SetMaxHitPoints(DutzNpcHitPoints.EndEtOlHitPoints);

        if (hp == null)
            return;

        hp.Died -= HandleEndBossDiedStatic;
        hp.Died += HandleEndBossDiedStatic;
    }

    static void HandleEndBossDiedStatic()
    {
        if (DutzCollectibleProgress.IsLevel07)
            return;

        if (Instance != null)
        {
            Instance.HandleEndBossDied();
            return;
        }

        if (staticBossDefeatHandled)
            return;

        staticBossDefeatHandled = true;
        DutzLevelObjective.NotifyLevel03BossDefeated("BEYBI M defeated! Dutz is free!");
    }

    public static void EnsureTrackGiantsVisible()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var trackRoot = GameObject.Find("DutzLevel03TrackGiants");
        if (trackRoot != null)
            trackRoot.SetActive(true);

        var ensured = 0;
        foreach (var physics in Object.FindObjectsOfType<SimpleCitizensNpcPhysics>(true))
        {
            var objectName = physics.gameObject.name;
            var isTrack = DutzCollectibleProgress.IsLevel03TrackEtOl(objectName);
            var isBonus = DutzCollectibleProgress.IsLevel03BonusGiant(objectName);
            if (!isTrack && !isBonus)
                continue;

            SimpleCitizensGiantHippieHunter.EnsureOnNpc(physics);
            SimpleCitizensGiantHippieHunter.EnsureTrililingColliderOnNpc(physics);
            SimpleCitizensNpcRespawn.EnsureOnNpc(physics);

            var hitPoints = physics.GetComponent<DutzNpcHitPoints>();
            if (hitPoints != null && hitPoints.IsDead)
                continue;

            if (!physics.gameObject.activeSelf)
                physics.gameObject.SetActive(true);

            physics.SnapFeetToRoad();
            ensured++;
        }

        Debug.Log(
            $"[Dutz] Level 3 giants ensured visible ({ensured}) under " +
            $"{trackRoot?.name ?? "DutzLevel03TrackGiants (missing)"}");
    }

    public static void EnsureEndEtOlScale()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null)
            return;

        DutzCollectibleProgress.ApplyLevel03EndBossScale(endEtOl.transform);
        endEtOl.GetComponent<SimpleCitizensNpcPhysics>()?.SnapFeetToRoad();
        DutzGiantHeadTopCollider.EnsureOnGiant(endEtOl);
    }

    void Awake()
    {
        if (!DutzCollectibleProgress.IsLevel03)
        {
            Destroy(gameObject);
            return;
        }

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeTrack();
        BindEndEtOlHitPoints();
    }

    void OnDestroy()
    {
        UnbindEndEtOlHitPoints();
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
        levelPlayableTime = Time.time + FinaleStartupGraceSeconds;
        nextTrackRebuildTime = Time.time + 0.5f;
        InitializeTrack();
        EnsureTrackGiantsVisible();
        DutzLevel03BonusGiants.EnsureFromBoot();
    }

    void Update()
    {
        if (!DutzCollectibleProgress.IsLevel03)
            return;

        if (player == null)
        {
            player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
            if (player == null)
                return;
        }

        if (Time.time >= levelPlayableTime && Time.time >= nextFinaleStateCheckTime)
        {
            nextFinaleStateCheckTime = Time.time + PlayerTriggerCheckInterval;
            TryUpdateFinaleState();
        }

        if (!hasReachedHighwaySix && Time.time >= nextTrackRebuildTime && !HasValidFinaleSegments())
        {
            InitializeTrack();
            nextTrackRebuildTime = Time.time + 1f;
        }

        if (announcementTimeLeft > 0f)
            announcementTimeLeft -= Time.deltaTime;
    }

    void TryUpdateFinaleState()
    {
        if (player == null)
            return;

        var onHighwaySix = IsPlayerOnHighwaySix();
        var nearEndBoss = IsPlayerNearEndBoss();

        if (!hasReachedHighwaySix && (onHighwaySix || nearEndBoss))
            ReachHighwaySix();

        if (!hasShownBossIncomingAnnouncement && hasReachedHighwaySix && (onHighwaySix || nearEndBoss))
            ShowBossIncomingOnce();
    }

    bool IsFinaleTriggerPosition(Vector3 playerPosition)
    {
        var nearest = DutzHighwayDirection.FindNearestTrackSegment(playerPosition);
        if (nearest != null && nearest.name == HighwaySixSegmentName)
            return true;

        var endBoss = DutzGiantBossNames.FindTrililing();
        if (endBoss != null && endBoss.activeInHierarchy)
        {
            var delta = endBoss.transform.position - playerPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude <= BossIncomingAnnounceRange * BossIncomingAnnounceRange)
                return true;
        }

        var bridgeFive = FindSegmentPath(HighwayFiveSegmentName);
        var highwaySix = FindSegmentPath(HighwaySixSegmentName);
        if (!IsValidSegment(bridgeFive) || !IsValidSegment(highwaySix))
            return false;

        var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(spawnRef, playerPosition, travelForward);
        if (playerAlong < bridgeFive.EndAlong - 1f)
            return false;

        return playerAlong >= highwaySix.StartAlong + 2f;
    }

    bool IsPlayerOnHighwaySix()
    {
        if (player == null)
            return false;

        var bridgeFive = FindSegmentPath(HighwayFiveSegmentName);
        var highwaySix = FindSegmentPath(HighwaySixSegmentName);
        if (IsValidSegment(bridgeFive) && IsValidSegment(highwaySix))
        {
            var playerAlong = DutzHighwayDeckSampler.AlongTrackAhead(
                spawnRef, player.transform.position, travelForward);

            if (playerAlong >= bridgeFive.EndAlong - 1f
                && playerAlong >= highwaySix.StartAlong + 2f)
                return true;
        }

        return IsPlayerOnHighwaySixByNearestSegment();
    }

    bool IsPlayerOnHighwaySixByNearestSegment()
    {
        if (player == null)
            return false;

        var nearest = DutzHighwayDirection.FindNearestTrackSegment(player.transform.position);
        return nearest != null && nearest.name == HighwaySixSegmentName;
    }

    bool HasValidFinaleSegments()
    {
        return IsValidSegment(FindSegmentPath(HighwayFiveSegmentName))
            && IsValidSegment(FindSegmentPath(HighwaySixSegmentName));
    }

    bool IsPlayerNearEndBoss()
    {
        if (player == null)
            return false;

        var endBoss = DutzGiantBossNames.FindTrililing();
        if (endBoss == null || !endBoss.activeInHierarchy)
            return false;

        var delta = endBoss.transform.position - player.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= BossIncomingAnnounceRange * BossIncomingAnnounceRange;
    }

    static bool IsValidSegment(DutzHighwayDeckSampler.SegmentPath path) =>
        !string.IsNullOrEmpty(path.SegmentName) && path.EndAlong - path.StartAlong >= MinSegmentSpan;

    void ShowBossIncomingOnce()
    {
        // Incoming banner disabled — boss still wakes via ReachHighwaySix / EnsureEndBossFinaleReady.
        hasShownBossIncomingAnnouncement = true;
    }

    void ReachHighwaySix()
    {
        if (hasReachedHighwaySix)
            return;

        // Level07 Straight 6 has Boy Idol — never run Level 3 BEYBI M finale here.
        if (DutzCollectibleProgress.IsLevel07)
        {
            hasReachedHighwaySix = true;
            return;
        }

        hasReachedHighwaySix = true;
        Debug.Log("[Dutz] Level 3 finale — Highway Straight 6 reached.");
        EnsureEndBossFinaleReady();
    }

    DutzHighwayDeckSampler.SegmentPath FindSegmentPath(string segmentName)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (segments[i].SegmentName == segmentName)
                return segments[i];
        }

        return default;
    }

    void EnsureEndBossFinaleReady()
    {
        if (DutzCollectibleProgress.IsLevel07)
            return;

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null)
        {
            Debug.LogError("[Dutz] Level 3 finale — BEYBI M not found.");
            return;
        }

        var physics = endEtOl.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            SimpleCitizensGiantHippieHunter.EnsureOnNpc(physics);
            SimpleCitizensGiantHippieHunter.EnsureTrililingColliderOnNpc(physics);
            SimpleCitizensNpcRespawn.EnsureOnNpc(physics);
        }

        DutzCollectibleProgress.ApplyLevel03EndBossScale(endEtOl.transform);
        physics?.SnapFeetToRoad();
        DutzGiantHeadTopCollider.EnsureOnGiant(endEtOl);
        var hp = endEtOl.GetComponent<DutzNpcHitPoints>();
        if (hp != null && hp.IsDead)
        {
            BindEndEtOlHitPoints();
            return;
        }

        DutzNpcHitPoints.EnsureOn(endEtOl, DutzNpcHitPoints.EndEtOlHitPoints, preserveCurrentHealth: true);
        DutzGiantHeat.EnsureOn(endEtOl);

        var hunter = endEtOl.GetComponent<SimpleCitizensGiantHippieHunter>();
        hunter?.WakeForLevel03Finale();

        if (!endEtOl.activeSelf)
            endEtOl.SetActive(true);

        BindEndBossDeathHandler();
        BindEndEtOlHitPoints();
    }

    void PrepareForNewRun()
    {
        hasReachedHighwaySix = false;
        hasShownBossIncomingAnnouncement = false;
        bossDefeatHandled = false;
        activeAnnouncement = null;
        announcementTimeLeft = 0f;
        levelPlayableTime = Time.time + FinaleStartupGraceSeconds;
        nextTrackRebuildTime = Time.time + 0.5f;
        InitializeTrack();
        UnbindEndEtOlHitPoints();
        BindEndEtOlHitPoints();
        EnsureTrackGiantsVisible();
        EnsureEndEtOlScale();
    }

    void BindEndEtOlHitPoints()
    {
        endEtOlHitPoints = null;

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null)
            return;

        endEtOlHitPoints = endEtOl.GetComponent<DutzNpcHitPoints>();
    }

    void UnbindEndEtOlHitPoints()
    {
        endEtOlHitPoints = null;
    }

    void HandleEndBossDied()
    {
        if (bossDefeatHandled)
            return;

        bossDefeatHandled = true;

        // Level 7: BEYBI M can die, but there is no win condition yet.
        if (DutzCollectibleProgress.IsLevel07)
        {
            Debug.Log("[Dutz] BEYBI M defeated on Level 7 — no win condition (placeholder).");
            return;
        }

        Debug.Log("[Dutz] BEYBI M defeated — triggering Level 3 win.");

        if (!hasReachedHighwaySix)
            hasReachedHighwaySix = true;

        DutzLevelObjective.NotifyLevel03BossDefeated(BossDefeatedWinMessage);
    }

    void RestoreHighwayGiantsForRespawn()
    {
        hasReachedHighwaySix = false;
        hasShownBossIncomingAnnouncement = false;
        bossDefeatHandled = false;
        activeAnnouncement = null;
        announcementTimeLeft = 0f;
        levelPlayableTime = Time.time + FinaleStartupGraceSeconds;
        nextTrackRebuildTime = Time.time + 0.5f;
        InitializeTrack();

        RestoreAllLevel03Giants();
        BindEndEtOlHitPoints();
    }

    static void RestoreAllLevel03Giants()
    {
        var trackRoot = GameObject.Find("DutzLevel03TrackGiants");
        if (trackRoot != null)
            trackRoot.SetActive(true);

        foreach (var hunter in FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (DutzCollectibleProgress.IsLevel03TrackEtOl(hunter.gameObject.name)
                || DutzCollectibleProgress.IsLevel03BonusGiant(hunter.gameObject.name)
                || (DutzCollectibleProgress.IsLevel07
                    && DutzCollectibleProgress.IsLevel07CombatGiant(hunter.gameObject.name)))
            {
                RestoreGiantForRespawn(hunter);
                continue;
            }

            if (hunter.gameObject.name == DutzGiantBossNames.BeybiM)
            {
                hunter.gameObject.SetActive(true);
                hunter.ResetLevel03HighwayState();
                hunter.ResetOnPlayerRespawn();
                hunter.GetComponent<DutzNpcHitPoints>()?.ResetForPlayerRespawn();
                hunter.GetComponent<DutzGiantHeat>()?.Configure(
                    DutzGiantHeat.GetBurnPerSecondForGiant(hunter.gameObject.name));
            }
        }
    }

    static void RestoreGiantForRespawn(SimpleCitizensGiantHippieHunter hunter)
    {
        if (hunter == null)
            return;

        hunter.gameObject.SetActive(true);
        hunter.ResetLevel03HighwayState();
        hunter.ResetOnPlayerRespawn();

        var hitPoints = hunter.GetComponent<DutzNpcHitPoints>();
        hitPoints?.ResetForPlayerRespawn();

        var respawn = hunter.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn != null)
            respawn.RespawnToStart();
        else
            hunter.GetComponent<SimpleCitizensNpcPhysics>()?.SnapFeetToRoad();
    }

    void InitializeTrack()
    {
        spawnRef = GetSpawnReference();
        travelForward = GetHighwayTravelForward(spawnRef);
        segments = DutzHighwayDeckSampler.BuildOrderedSegmentPaths(SegmentNames, spawnRef, travelForward);
        DutzHighwayDirection.FindNearestTrackSegment(spawnRef);
    }

    void ShowAnnouncement(string message)
    {
        activeAnnouncement = message;
        announcementTimeLeft = 2.8f;
    }

    void OnGUI()
    {
        DrawBossHealthBar();

        if (announcementTimeLeft <= 0f || string.IsNullOrEmpty(activeAnnouncement) || !hasReachedHighwaySix)
            return;

        DutzAnnouncementHud.DrawFlash(activeAnnouncement, DutzAnnouncementHud.DefaultFlashColor);
    }

    static GUIStyle bossHudLabelStyle;

    void DrawBossHealthBar()
    {
        if (!ShouldShowBossHealthBar())
            return;

        if (bossHudLabelStyle == null)
        {
            bossHudLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.95f, 0.3f, 0.45f) }
            };
        }

        const float width = 280f;
        const float height = 22f;
        const float labelHeight = 28f;
        var labelRect = new Rect((Screen.width - width) * 0.5f, BossHudTopY, width, labelHeight);
        var rect = new Rect(labelRect.x, labelRect.yMax + 2f, width, height);
        var fill = Mathf.Clamp01((float)endEtOlHitPoints.CurrentHitPoints / endEtOlHitPoints.MaxHitPoints);
        var fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fill, rect.height - 4f);

        GUI.Label(
            labelRect,
            $"BEYBI M HP: {endEtOlHitPoints.CurrentHitPoints} / {endEtOlHitPoints.MaxHitPoints}",
            bossHudLabelStyle);
        GUI.Box(rect, GUIContent.none, GUI.skin.box);
        GUI.color = new Color(0.85f, 0.15f, 0.35f, 0.9f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    bool ShouldShowBossHealthBar()
    {
        if (DutzLevelObjective.IsLevelFinishedForActiveScene)
            return false;

        if (endEtOlHitPoints == null)
            BindEndEtOlHitPoints();

        if (endEtOlHitPoints == null || endEtOlHitPoints.IsDead)
            return false;

        var endEtOl = DutzGiantBossNames.FindTrililing();
        if (endEtOl == null || !endEtOl.activeInHierarchy)
            return false;

        if (player == null)
            player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();

        if (player == null)
            return false;

        var delta = endEtOl.transform.position - player.transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= BossHudRange * BossHudRange;
    }

    static Vector3 GetSpawnReference()
    {
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var trackStart, out _))
            return trackStart;

        var player = DutzPlayerController.Instance ?? FindObjectOfType<DutzPlayerController>();
        if (player != null)
            return player.transform.position;

        return Vector3.zero;
    }

    static Vector3 GetHighwayTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackStartSpawnPosition(out _, out var trackForward)
            && trackForward.sqrMagnitude > 0.0001f)
        {
            trackForward.y = 0f;
            return trackForward.normalized;
        }

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;

        forward.y = 0f;
        return forward.normalized;
    }
}
