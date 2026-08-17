using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Player1 health — Level 3 / Level 7; giants burn HP on contact (rate per DutzGiantHeat).</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DutzPlayerController))]
[DefaultExecutionOrder(110)]
public class DutzPlayerHitPoints : MonoBehaviour
{
    public const int Level03MaxHitPoints = 100;

    [SerializeField] int maxHitPoints = Level03MaxHitPoints;
    [SerializeField] int currentHitPoints = Level03MaxHitPoints;

    DutzPlayerController player;
    CharacterController characterController;
    DutzFallRespawn fallRespawn;
    float burnAccumulator;
    float burnWarningPhase;
    bool isBeingBurned;
    bool isDead;
    bool boyIdolDefeatStarted;
    bool boyIdolBurningThisFrame;

    public int MaxHitPoints => maxHitPoints;
    public int CurrentHitPoints => currentHitPoints;
    public bool IsDead => isDead;

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var playerController = DutzPlayerController.Instance
            ?? Object.FindObjectOfType<DutzPlayerController>();
        if (playerController == null)
            return;

        EnsureOn(playerController.gameObject);
        DutzGiantHeat.EnsureFromBoot();
    }

    public static DutzPlayerHitPoints EnsureOn(GameObject target)
    {
        if (target == null)
            return null;

        var hp = target.GetComponent<DutzPlayerHitPoints>();
        if (hp == null)
            hp = target.AddComponent<DutzPlayerHitPoints>();

        hp.Configure(Level03MaxHitPoints);
        hp.enabled = true;
        return hp;
    }

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        characterController = GetComponent<CharacterController>();
        fallRespawn = GetComponent<DutzFallRespawn>();

        if (!DutzCollectibleProgress.IsLevel03Gameplay)
        {
            enabled = false;
            return;
        }

        Configure(Level03MaxHitPoints);
    }

    public void Configure(int hitPoints)
    {
        maxHitPoints = Mathf.Max(1, hitPoints);
        currentHitPoints = maxHitPoints;
        isDead = false;
        boyIdolDefeatStarted = false;
        boyIdolBurningThisFrame = false;
        burnAccumulator = 0f;
        isBeingBurned = false;
        burnWarningPhase = 0f;
    }

    public void ResetOnRespawn()
    {
        currentHitPoints = maxHitPoints;
        isDead = false;
        boyIdolDefeatStarted = false;
        boyIdolBurningThisFrame = false;
        burnAccumulator = 0f;
        isBeingBurned = false;
        burnWarningPhase = 0f;
    }

    void Update()
    {
        if (!enabled || isDead || player == null || characterController == null)
            return;

        if (player.ControlsLocked || DutzLevelObjective.IsLevelFinishedForActiveScene)
            return;

        if (fallRespawn != null && (fallRespawn.IsShowingRespawnDialog || fallRespawn.IsSpawnGraceActive))
            return;

        if (DutzForceField.IsPlayerShielded(player))
            return;

        if (!IsAnyGiantHeatTouching(out var burnPerSecond))
        {
            burnAccumulator = 0f;
            isBeingBurned = false;
            boyIdolBurningThisFrame = false;
            return;
        }

        isBeingBurned = true;
        boyIdolBurningThisFrame = DutzCollectibleProgress.IsLevel07
            && DutzLevel07BoyIdolGate.IsTouchingBoyIdolHeat(characterController);
        burnWarningPhase += Time.deltaTime * 10f;

        burnAccumulator += Time.deltaTime * burnPerSecond;
        while (burnAccumulator >= 1f)
        {
            TakeDamage(1);
            burnAccumulator -= 1f;
        }
    }

    const float HeatCullDistance = 320f;

    bool IsAnyGiantHeatTouching(out float totalBurnPerSecond)
    {
        totalBurnPerSecond = 0f;

        var heats = DutzGiantHeat.AllActive;
        if (heats == null || heats.Count == 0)
            return false;

        var playerPos = characterController.transform.position;
        var cullDistSq = HeatCullDistance * HeatCullDistance;
        var touching = false;
        for (var i = 0; i < heats.Count; i++)
        {
            var heat = heats[i];
            if (heat == null || !heat.enabled)
                continue;

            var delta = heat.transform.position - playerPos;
            delta.y = 0f;
            if (delta.sqrMagnitude > cullDistSq)
                continue;

            if (!heat.IsTouchingPlayer(characterController))
                continue;

            touching = true;
            totalBurnPerSecond += heat.BurnPerSecond;
        }

        return touching;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
    }

    /// <summary>Bridge 5 red potion — adds HP on top of current total (ignores the 100 cap).</summary>
    public void HealUncapped(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHitPoints += amount;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHitPoints = Mathf.Max(0, currentHitPoints - amount);
        if (currentHitPoints > 0)
            return;

        Die();
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        burnAccumulator = 0f;
        isBeingBurned = false;

        // Level07 — Boy Idol burn kill = impeachment fail (Sara acquitted) then reload.
        if (DutzCollectibleProgress.IsLevel07
            && !boyIdolDefeatStarted
            && (boyIdolBurningThisFrame
                || DutzLevel07BoyIdolGate.IsTouchingBoyIdolHeat(characterController)))
        {
            boyIdolDefeatStarted = true;
            if (player != null)
                player.SetControlsLocked(true);
            StartCoroutine(BoyIdolDefeatRoutine());
            return;
        }

        fallRespawn?.TriggerDeathDialog("The giants' heat burned you!");
    }

    IEnumerator BoyIdolDefeatRoutine()
    {
        Debug.Log("[Dutz] Defeated by Boy Idol — playing Sara acquitted.");
        yield return DutzLevel07ImpeachmentVideo.PlayFailThenReloadLevel();
    }

    static GUIStyle hpLabelStyle;

    /// <summary>Bottom of the HP label+bar block (upper-left).</summary>
    public static float HpBlockBottomY => 16f + 36f + 22f;

    /// <summary>Deprecated — use DutzUpperLeftHudLayout.YFor.</summary>
    public static float BelowHpHudY => DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.SuperJump);

    /// <summary>Deprecated — use DutzUpperLeftHudLayout.YFor.</summary>
    public static float BelowHpHudSecondRowY => DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.SuperPunch);

    void OnGUI()
    {
        if (!enabled || isDead || player == null || player.ControlsLocked)
            return;

        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return;

        if (hpLabelStyle == null)
        {
            hpLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(1f, 0.45f, 0.2f) }
            };
        }

        var style = hpLabelStyle;

        var barStyle = GUI.skin.box;
        const float padding = 16f;
        const float width = 220f;
        const float height = 22f;
        const float labelRowHeight = 28f;
        var rect = new Rect(padding, padding + 36f, width, height);
        var fill = maxHitPoints > 0
            ? Mathf.Clamp01((float)currentHitPoints / maxHitPoints)
            : 0f;
        var fillRect = new Rect(rect.x + 2f, rect.y + 2f, (rect.width - 4f) * fill, rect.height - 4f);

        GUI.Box(rect, GUIContent.none, barStyle);
        GUI.color = new Color(0.95f, 0.3f, 0.1f, 0.85f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        var hpText = $"HP: {currentHitPoints} / {maxHitPoints}";
        var labelRow = new Rect(padding, padding, Screen.width - padding * 2f, labelRowHeight);
        DutzGameplayModeHud.DrawCombinedRow(labelRow, hpText, style);

        if (isBeingBurned)
            DrawBurnWarning();
    }

    void DrawBurnWarning()
    {
        var pulse = 0.7f + 0.3f * Mathf.Sin(burnWarningPhase);
        var bangSize = Mathf.RoundToInt(88f * pulse);
        var labelSize = Mathf.RoundToInt(28f * pulse);

        var bangStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = bangSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.2f, 0.05f, pulse) }
        };

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = labelSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.55f, 0.1f, pulse) }
        };

        var bangRect = new Rect(0f, Screen.height * 0.22f, Screen.width, bangSize + 8f);
        var labelRect = new Rect(0f, bangRect.yMax + 4f, Screen.width, labelSize + 8f);

        var shadowStyle = new GUIStyle(bangStyle)
        {
            normal = { textColor = new Color(0f, 0f, 0f, pulse * 0.75f) }
        };
        GUI.Label(new Rect(bangRect.x + 3f, bangRect.y + 3f, bangRect.width, bangRect.height), "!", shadowStyle);
        GUI.Label(bangRect, "!", bangStyle);
        GUI.Label(labelRect, "TOO HOT!", labelStyle);
    }
}

/// <summary>
/// Shared upper-left status stack — HP stays on top; status rows stack under it without overlap.
/// Order: Lives → Super Jump → Super Punch → Force Field → Parachute.
/// </summary>
public static class DutzUpperLeftHudLayout
{
    public enum Slot
    {
        Lives,
        SuperJump,
        SuperPunch,
        ForceField,
        Parachute
    }

    public const float PaddingX = 16f;
    public const float Gap = 6f;
    public const float TextRowHeight = 28f;
    public const float ForceFieldBlockHeight = 78f;
    public const float ParachuteBlockHeight = 92f;

    public static float StackStartY
    {
        get
        {
            if (DutzCollectibleProgress.IsLevel03Gameplay)
                return DutzPlayerHitPoints.HpBlockBottomY + Gap;

            var y = PaddingX;
            if (DutzGameplayModeHud.ShouldDrawStandaloneBadge())
                y += DutzGameplayModeHud.TopRowHeight + Gap;
            return y;
        }
    }

    public static float YFor(Slot slot)
    {
        var y = StackStartY;

        if (slot == Slot.Lives)
            return y;

        // Lives is always shown on campaign levels.
        y += TextRowHeight + Gap;

        if (slot == Slot.SuperJump)
            return y;

        if (ShowsSuperJump())
            y += TextRowHeight + Gap;
        if (slot == Slot.SuperPunch)
            return y;

        if (ShowsSuperPunch())
            y += TextRowHeight + Gap;
        if (slot == Slot.ForceField)
            return y;

        if (ShowsForceField())
            y += ForceFieldBlockHeight + Gap;

        return y;
    }

    static bool ShowsSuperJump()
    {
        var player = DutzPlayerController.Instance;
        return player != null && player.ShowsSuperJumpHud;
    }

    static bool ShowsSuperPunch()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
            return false;

        var punch = player.GetComponent<DutzPlayerPunch>();
        return punch != null && punch.HasSuperPunchActive;
    }

    static bool ShowsForceField()
    {
        var player = DutzPlayerController.Instance;
        if (player == null)
            return false;

        var field = DutzForceField.FindForPlayer(player);
        return field != null && field.IsActive;
    }
}

/// <summary>Top-row gameplay badge — Senior Citizen Mode beside HP on HP levels, standalone elsewhere.</summary>
public static class DutzGameplayModeHud
{
    public const string ModeLabel = "SENIOR CITIZEN MODE";

    static GUIStyle modeStyle;

    public static bool IsActive()
    {
        if (DutzMobileRuntime.IsFloodControlScene)
            return FloodDifficulty.HasChosen && FloodDifficulty.IsSeniorCitizenMode();

        var scene = SceneManager.GetActiveScene().name;
        if (!DutzMobileRuntime.IsDutzLevelScene(scene))
            return false;

        return DutzDifficulty.HasChosen && DutzDifficulty.IsSeniorCitizenMode();
    }

    public static bool UsesCombinedHpRow =>
        DutzCollectibleProgress.IsLevel03Gameplay || DutzMobileRuntime.IsFloodControlScene;

    public static bool ShouldDrawStandaloneBadge() =>
        IsActive() && !UsesCombinedHpRow;

    public static float TopRowHeight => DutzCartoonDialogGui.Scale(28f, 36f);

    static GUIStyle ModeStyle()
    {
        if (modeStyle == null)
        {
            modeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = DutzCartoonDialogGui.ScaleFont(18, 26),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.78f, 0.45f, 1f) }
            };
        }

        return modeStyle;
    }

    /// <summary>Left: Senior Citizen badge when active. Right: HP text (right-aligned).</summary>
    public static void DrawCombinedRow(Rect rowRect, string rightText, GUIStyle rightStyle)
    {
        if (!IsActive())
        {
            if (!string.IsNullOrEmpty(rightText))
                GUI.Label(rowRect, rightText, rightStyle);
            return;
        }

        var leftWidth = rowRect.width * 0.56f;
        var rightWidth = rowRect.width - leftWidth;
        var leftRect = new Rect(rowRect.x, rowRect.y, leftWidth, rowRect.height);
        var rightRect = new Rect(rowRect.xMax - rightWidth, rowRect.y, rightWidth, rowRect.height);
        var right = new GUIStyle(rightStyle) { alignment = TextAnchor.UpperRight };

        DutzCartoonDialogGui.DrawOutlinedLabel(leftRect, ModeLabel, ModeStyle(), Color.black);
        if (!string.IsNullOrEmpty(rightText))
            GUI.Label(rightRect, rightText, right);
    }

    public static void DrawStandaloneBadgeIfNeeded()
    {
        if (!ShouldDrawStandaloneBadge())
            return;

        var pad = DutzUpperLeftHudLayout.PaddingX;
        var row = new Rect(pad, pad, Screen.width - pad * 2f, TopRowHeight);
        DutzCartoonDialogGui.DrawOutlinedLabel(row, ModeLabel, ModeStyle(), Color.black);
    }
}
