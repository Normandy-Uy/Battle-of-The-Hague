using UnityEngine;

public enum MobileMovementGear
{
    Run,
    Walk
}

enum MoveArrow
{
    Up,
    Down,
    Left,
    Right
}

enum MobileActionIconKind
{
    Jump,
    Punch,
    Run
}

/// <summary>
/// Transparent mobile HUD matched to the photo mockup: Up/Down far left, Run/Walk beside
/// them, kangaroo Jump + Punch glove in the middle, Left/Right bottom-right with LOOK above.
/// LEVEL FLOOD CONTROL hides Jump, LOOK, and gear shift; Punch drops into the Jump slot.
/// Multi-touch diagonal combine and camera drag unchanged.
/// </summary>
[DefaultExecutionOrder(40)]
public class DutzRobloxMobileInput : MonoBehaviour
{
    const float LookZoneStart = 0.55f;
    const float BaseVerticalPadCellSize = 118f;
    const float BaseHorizontalPadCellSize = 204f;
    const float BasePadCellGap = 10f;
    const float BaseLookHintRadius = 84f;
    const int GearRowCount = 2;
    const float BaseJumpSize = 132f;
    const float BaseGearRowHeight = 54f;
    const float BaseGearWidth = 108f;

    // Normalized GUI anchors (x,y) — y=0 is top of screen.
    // Left/Right sit near the bottom-right edge; LOOK stays above Right by LookAboveRight.
    static readonly Vector2 AnchorUp = new Vector2(0.068f, 0.61f);
    static readonly Vector2 AnchorDown = new Vector2(0.068f, 0.79f);
    static readonly Vector2 AnchorRun = new Vector2(0.207f, 0.66f);
    static readonly Vector2 AnchorWalk = new Vector2(0.203f, 0.834f);
    static readonly Vector2 AnchorJump = new Vector2(0.596f, 0.895f);
    static readonly Vector2 AnchorPunch = new Vector2(0.596f, 0.68f);
    static readonly Vector2 AnchorRight = new Vector2(0.912f, 0.905f);
    /// <summary>LOOK center is this far above Right (normalized screen height).</summary>
    const float LookAboveRight = 0.424f;
    /// <summary>Extra gap between Left/Right circle edges as a fraction of cell diameter.</summary>
    const float HorizontalPadEdgeGapFraction = 0.06f;

    [SerializeField] bool forceMobileForTesting;

    static DutzRobloxMobileInput instance;

    Vector2 lookDelta;
    bool jumpPressedThisFrame;
    bool punchPressedThisFrame;

    bool upHeld;
    bool downHeld;
    bool leftHeld;
    bool rightHeld;
    Vector2 latchedMoveAxis;

    bool lookActive;
    int lookFingerId = -1;
    Vector2 lastLookPosition;

    bool jumpHeld;
    MobileMovementGear selectedGear = MobileMovementGear.Walk;

    Texture2D circleTexture;
    Texture2D lineTexture;
    Texture2D jumpIconTexture;
    Texture2D punchIconTexture;
    Texture2D runIconTexture;
    Texture2D arrowUpTexture;
    Texture2D arrowDownTexture;
    Texture2D arrowLeftTexture;
    Texture2D arrowRightTexture;

    float cachedLabelUiScale = -1f;
    GUIStyle lookHintShadowStyle;
    GUIStyle lookHintLabelStyleActive;
    GUIStyle lookHintLabelStyleInactive;
    GUIStyle gearLabelStyle;
    GUIStyle gearLabelStyleSelected;

    public static DutzRobloxMobileInput Instance => instance;

    public static bool IsMobileControlsActive =>
        instance != null && instance.UseMobileInputPath;

    public static Vector2 MoveAxis =>
        instance != null && instance.ShouldProcessGameplayInput ? instance.latchedMoveAxis : Vector2.zero;

    public static Vector2 LookDelta =>
        instance != null && instance.ShouldProcessGameplayInput ? instance.lookDelta : Vector2.zero;

    public static bool JumpPressedThisFrame =>
        instance != null && instance.ShouldProcessGameplayInput && instance.jumpPressedThisFrame;

    public static bool JumpHeld =>
        instance != null && instance.ShouldProcessGameplayInput && instance.jumpHeld;

    public static bool PunchPressedThisFrame =>
        instance != null && instance.ShouldProcessGameplayInput && instance.punchPressedThisFrame;

    public static MobileMovementGear MovementGear =>
        instance != null && instance.ShouldProcessGameplayInput ? instance.selectedGear : MobileMovementGear.Walk;

    public static bool HasDirectionHeld =>
        instance != null && instance.ShouldProcessGameplayInput && instance.latchedMoveAxis.sqrMagnitude > 0.01f;

    public static bool MoveStickActive =>
        instance != null && instance.ShouldProcessGameplayInput && instance.AnyMovementHeld;

    public static bool AzimuthSliderActive => MoveStickActive;

    bool AnyMovementHeld => upHeld || downHeld || leftHeld || rightHeld;

    bool UseMobileInputPath =>
        Application.isMobilePlatform || (forceMobileForTesting && Application.isPlaying);

    bool ShouldProcessGameplayInput =>
        UseMobileInputPath && !IsBlockedByMenu();

    static bool IsFloodControlScene =>
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "LEVEL FLOOD CONTROL";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;

        if (circleTexture != null)
            Destroy(circleTexture);
        if (lineTexture != null)
            Destroy(lineTexture);
        if (arrowUpTexture != null)
            Destroy(arrowUpTexture);
        if (arrowDownTexture != null)
            Destroy(arrowDownTexture);
        if (arrowLeftTexture != null)
            Destroy(arrowLeftTexture);
        if (arrowRightTexture != null)
            Destroy(arrowRightTexture);
    }

    public static void EnsureCreated()
    {
        if (instance != null)
            return;

        var existing = FindObjectOfType<DutzRobloxMobileInput>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        var go = new GameObject("DutzMobileControls");
        go.AddComponent<DutzRobloxMobileInput>();
    }

    void Update()
    {
        jumpPressedThisFrame = false;
        punchPressedThisFrame = false;
        lookDelta = Vector2.zero;

        if (!UseMobileInputPath)
        {
            ResetTouchState();
            return;
        }

        if (IsBlockedByMenu())
        {
            ResetTouchState();
            return;
        }

        if (Input.touchSupported && Input.touchCount > 0)
            ProcessTouches();
        else if (forceMobileForTesting && Application.isPlaying)
            ProcessMouseSimulation();
        else
            EndLookOnly();
    }

    static bool IsBlockedByMenu()
    {
        if (DutzGamePause.IsPaused)
            return true;

        if (DutzLevelStartGate.IsBlockingStart)
            return true;

        var player = DutzPlayerController.Instance;
        if (player != null && player.ControlsLocked)
            return true;

        if (player != null)
        {
            var difficulty = player.GetComponent<DutzDifficultySelect>();
            if (difficulty != null && difficulty.AwaitingSelection)
                return true;
        }

        var fallRespawn = player != null ? player.GetComponent<DutzFallRespawn>() : null;
        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return true;

        if (DutzPoliceCaptureDialog.IsShowing)
            return true;

        if (IntroSequenceController.IsBlockingMobileInput)
            return true;

        if (FloodPlayerHealth.IsBlockingMobileInput)
            return true;

        if (FloodVictoryGoal.IsBlockingMobileInput)
            return true;

        if (DutzLevelObjective.IsShowingLevelCompleteChoice
            || DutzLevelObjective.ShouldShowLevelCompleteDialog())
            return true;

        return false;
    }

    void ProcessTouches()
    {
        var seenLook = lookFingerId >= 0;

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            var pos = touch.position;

            if (touch.fingerId == lookFingerId)
            {
                seenLook = false;
                UpdateLook(touch);
                continue;
            }

            if (touch.phase == TouchPhase.Began)
            {
                if (DutzGamePause.ContainsScreenPoint(pos))
                    continue;

                if (IsInsideJumpButton(pos))
                {
                    jumpPressedThisFrame = true;
                    jumpHeld = true;
                    continue;
                }

                if (IsInsidePunchButton(pos))
                {
                    punchPressedThisFrame = true;
                    continue;
                }

                if (TrySelectGearAt(pos))
                    continue;

                if (IsInsideLookZone(pos) && lookFingerId < 0)
                    BeginLook(touch);
            }
        }

        if (seenLook)
            EndLook();

        RefreshMovementFromActiveTouches();

        if (jumpHeld)
        {
            var jumpStillHeld = false;
            for (var i = 0; i < Input.touchCount; i++)
            {
                if (IsInsideJumpButton(Input.GetTouch(i).position))
                {
                    jumpStillHeld = true;
                    break;
                }
            }

            if (!jumpStillHeld)
                jumpHeld = false;
        }
    }

    void ProcessMouseSimulation()
    {
        var screenPos = Input.mousePosition;
        var guiPos = ToGuiPosition(screenPos);

        if (Input.GetMouseButtonDown(0))
        {
            if (DutzGamePause.ContainsScreenPoint(screenPos))
            {
                // Pause owns this press — do not start look/move/actions.
            }
            else if (IsInsideJumpButton(screenPos))
            {
                jumpPressedThisFrame = true;
                jumpHeld = true;
            }
            else if (IsInsidePunchButton(screenPos))
                punchPressedThisFrame = true;
            else if (TrySelectGearAt(screenPos)) { }
            else if (IsInsideLookZone(screenPos))
                BeginLookMouse(guiPos);
        }

        if (lookActive && Input.GetMouseButton(0))
            UpdateLookMouse(guiPos);
        else if (lookActive)
            EndLook();

        RefreshMovementFromActiveTouches();
    }

    void RefreshMovementFromActiveTouches()
    {
        upHeld = false;
        downHeld = false;
        leftHeld = false;
        rightHeld = false;

        if (Input.touchSupported && Input.touchCount > 0)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    continue;

                if (touch.fingerId == lookFingerId)
                    continue;

                ApplyMovementFromScreenPoint(touch.position);
            }
        }
        else if (forceMobileForTesting && Application.isPlaying && Input.GetMouseButton(0) && !lookActive)
        {
            ApplyMovementFromGuiPoint(ToGuiPosition(Input.mousePosition));
        }

        RebuildMoveAxisFromHeldDirections();
    }

    void ApplyMovementFromScreenPoint(Vector2 screenPos)
    {
        if (IsInsideJumpButton(screenPos)
            || IsInsidePunchButton(screenPos)
            || IsInsideAnyGearRow(screenPos))
        {
            return;
        }

        ApplyMovementFromGuiPoint(ToGuiPosition(screenPos));
    }

    void ApplyMovementFromGuiPoint(Vector2 guiPos)
    {
        if (IsInsideVerticalPadArrow(MoveArrow.Up, guiPos))
            upHeld = true;

        if (IsInsideVerticalPadArrow(MoveArrow.Down, guiPos))
            downHeld = true;

        if (IsInsideHorizontalPadArrow(MoveArrow.Left, guiPos))
            leftHeld = true;

        if (IsInsideHorizontalPadArrow(MoveArrow.Right, guiPos))
            rightHeld = true;
    }

    void RebuildMoveAxisFromHeldDirections()
    {
        var x = (rightHeld ? 1f : 0f) + (leftHeld ? -1f : 0f);
        var y = (upHeld ? 1f : 0f) + (downHeld ? -1f : 0f);
        latchedMoveAxis = x == 0f && y == 0f ? Vector2.zero : new Vector2(x, y).normalized;
    }

    void ClearMovement()
    {
        upHeld = false;
        downHeld = false;
        leftHeld = false;
        rightHeld = false;
        latchedMoveAxis = Vector2.zero;
    }

    bool TrySelectGearAt(Vector2 screenPos)
    {
        if (IsFloodControlScene)
            return false;

        for (var row = 0; row < GearRowCount; row++)
        {
            if (!IsInsideGearRow(screenPos, row))
                continue;

            selectedGear = row == 0 ? MobileMovementGear.Run : MobileMovementGear.Walk;
            return true;
        }

        return false;
    }

    void BeginLook(Touch touch)
    {
        lookFingerId = touch.fingerId;
        lookActive = true;
        lastLookPosition = touch.position;
    }

    void UpdateLook(Touch touch)
    {
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            EndLook();
            return;
        }

        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
        {
            var delta = touch.position - lastLookPosition;
            lastLookPosition = touch.position;
            lookDelta += new Vector2(delta.x, -delta.y);
        }
    }

    void BeginLookMouse(Vector2 pos)
    {
        lookFingerId = -2;
        lookActive = true;
        lastLookPosition = pos;
    }

    void UpdateLookMouse(Vector2 pos)
    {
        var delta = pos - lastLookPosition;
        lastLookPosition = pos;
        lookDelta += new Vector2(delta.x, delta.y);
    }

    void EndLook()
    {
        lookFingerId = -1;
        lookActive = false;
    }

    void EndLookOnly()
    {
        EndLook();
        jumpHeld = false;
        ClearMovement();
    }

    void ResetTouchState()
    {
        ClearMovement();
        EndLook();
        jumpHeld = false;
    }

    float GetVerticalPadCellSize() => BaseVerticalPadCellSize * GetActionButtonScale();

    float GetHorizontalPadCellSize() => BaseHorizontalPadCellSize * GetActionButtonScale();

    float GetPadCellGap() => BasePadCellGap * GetActionButtonScale();

    static Rect CenteredRect(Vector2 normalizedAnchor, float width, float height)
    {
        var center = new Vector2(
            Screen.width * normalizedAnchor.x,
            Screen.height * normalizedAnchor.y);
        return new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }

    Rect GetVerticalPadArrowRect(MoveArrow arrow)
    {
        var cell = GetVerticalPadCellSize();
        var anchor = arrow == MoveArrow.Up ? AnchorUp : AnchorDown;
        return CenteredRect(anchor, cell, cell);
    }

    Rect GetHorizontalPadBounds()
    {
        var left = GetHorizontalPadArrowRect(MoveArrow.Left);
        var right = GetHorizontalPadArrowRect(MoveArrow.Right);
        var xMin = Mathf.Min(left.xMin, right.xMin);
        var yMin = Mathf.Min(left.yMin, right.yMin);
        var xMax = Mathf.Max(left.xMax, right.xMax);
        var yMax = Mathf.Max(left.yMax, right.yMax);
        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    bool IsInsidePlayStationButton(Vector2 guiPos, Rect rect)
    {
        var radius = rect.width * 0.5f;
        return (guiPos - rect.center).sqrMagnitude <= radius * radius;
    }

    bool IsInsideVerticalPadArrow(MoveArrow arrow, Vector2 guiPos) =>
        IsInsidePlayStationButton(guiPos, GetVerticalPadArrowRect(arrow));

    bool IsInsideHorizontalPadArrow(MoveArrow arrow, Vector2 guiPos) =>
        IsInsidePlayStationButton(guiPos, GetHorizontalPadArrowRect(arrow));

    Rect GetHorizontalPadArrowRect(MoveArrow arrow)
    {
        var cell = GetHorizontalPadCellSize();
        var rightCenter = new Vector2(
            Screen.width * AnchorRight.x,
            Screen.height * AnchorRight.y);
        // Hit circles use diameter = cell — keep a thin gap so rings never overlap.
        var centerGap = cell * (1f + HorizontalPadEdgeGapFraction);
        var leftCenter = new Vector2(rightCenter.x - centerGap, rightCenter.y);
        var center = arrow == MoveArrow.Left ? leftCenter : rightCenter;
        return new Rect(center.x - cell * 0.5f, center.y - cell * 0.5f, cell, cell);
    }

    bool IsInsideAnyMovementPad(Vector2 screenPos)
    {
        var guiPos = ToGuiPosition(screenPos);
        return IsInsideVerticalPadArrow(MoveArrow.Up, guiPos)
            || IsInsideVerticalPadArrow(MoveArrow.Down, guiPos)
            || IsInsideHorizontalPadArrow(MoveArrow.Left, guiPos)
            || IsInsideHorizontalPadArrow(MoveArrow.Right, guiPos);
    }

    Vector2 GetLookHintCircleCenterGui()
    {
        // Track Right: same X, raised by LookAboveRight so LOOK moves when Right drops.
        return new Vector2(
            Screen.width * AnchorRight.x,
            Screen.height * (AnchorRight.y - LookAboveRight));
    }

    float GetLookHintRadius() => BaseLookHintRadius * GetUiScale();

    float GetUiScale() => Mathf.Min(Screen.width, Screen.height) / 720f;

    float GetActionButtonScale() => GetUiScale() * (Application.isMobilePlatform ? 1.12f : 1f);

    float GetActionButtonSize() => BaseJumpSize * GetActionButtonScale();

    Rect GetJumpButtonRect()
    {
        var size = GetActionButtonSize();
        return CenteredRect(AnchorJump, size, size);
    }

    Rect GetPunchButtonRect()
    {
        var size = GetActionButtonSize();
        // Flood Control has no jump button — park the glove where the kangaroo sat.
        var anchor = IsFloodControlScene ? AnchorJump : AnchorPunch;
        return CenteredRect(anchor, size, size);
    }

    Rect GetGearRowRect(int row)
    {
        var scale = GetActionButtonScale();
        var rowHeight = BaseGearRowHeight * scale;
        var width = BaseGearWidth * scale;
        var anchor = row == 0 ? AnchorRun : AnchorWalk;
        return CenteredRect(anchor, width, rowHeight);
    }

    bool IsInsideGearRow(Vector2 screenPos, int row)
    {
        var guiPos = ToGuiPosition(screenPos);
        return GetGearRowRect(row).Contains(guiPos);
    }

    bool IsInsideAnyGearRow(Vector2 screenPos)
    {
        if (IsFloodControlScene)
            return false;

        for (var row = 0; row < GearRowCount; row++)
        {
            if (IsInsideGearRow(screenPos, row))
                return true;
        }

        return false;
    }

    bool IsInsideActionButton(Vector2 screenPos, Rect rect)
    {
        var guiPos = ToGuiPosition(screenPos);
        var center = rect.center;
        var radius = rect.width * 0.56f;
        return (guiPos - center).sqrMagnitude <= radius * radius;
    }

    bool IsInsidePunchButton(Vector2 screenPos) =>
        IsInsideActionButton(screenPos, GetPunchButtonRect());

    bool IsInsideJumpButton(Vector2 screenPos)
    {
        if (IsFloodControlScene)
            return false;

        return IsInsideActionButton(screenPos, GetJumpButtonRect());
    }

    bool IsInsideBlockedRightUi(Vector2 screenPos) =>
        IsInsideJumpButton(screenPos)
        || IsInsidePunchButton(screenPos)
        || IsInsideAnyGearRow(screenPos);

    bool IsInsideLookZone(Vector2 screenPos)
    {
        if (IsFloodControlScene)
            return false;

        if (DutzGamePause.ContainsScreenPoint(screenPos))
            return false;

        var guiPos = ToGuiPosition(screenPos);
        if (IsInsideBlockedRightUi(screenPos))
            return false;

        if (IsInsideAnyMovementPad(screenPos))
            return false;

        return guiPos.x >= Screen.width * LookZoneStart;
    }

    static Vector2 ToGuiPosition(Vector2 screenPos) =>
        new Vector2(screenPos.x, Screen.height - screenPos.y);

    void OnGUI()
    {
        if (!UseMobileInputPath || IsBlockedByMenu())
            return;

        EnsureCircleTexture();
        EnsureLineTexture();
        EnsureArrowTextures();
        EnsureActionIconTextures();

        var scale = GetUiScale();
        EnsureLabelStyles(scale);
        if (!IsFloodControlScene)
            DrawLookZoneHint(scale);
        if (!IsFloodControlScene)
            DrawGearShift();
        DrawPunchButton();
        if (!IsFloodControlScene)
            DrawJumpButton();
        DrawVerticalMovementPad(scale);
        DrawHorizontalMovementPad(scale);
    }

    void DrawVerticalMovementPad(float scale)
    {
        DrawPlayStationArrowButton(GetVerticalPadArrowRect(MoveArrow.Up), MoveArrow.Up, upHeld, scale, true);
        DrawPlayStationArrowButton(GetVerticalPadArrowRect(MoveArrow.Down), MoveArrow.Down, downHeld, scale, true);
    }

    void DrawHorizontalMovementPad(float scale)
    {
        DrawPlayStationArrowButton(GetHorizontalPadArrowRect(MoveArrow.Left), MoveArrow.Left, leftHeld, scale, false);
        DrawPlayStationArrowButton(GetHorizontalPadArrowRect(MoveArrow.Right), MoveArrow.Right, rightHeld, scale, false);
    }

    void DrawPlayStationArrowButton(Rect rect, MoveArrow direction, bool pressed, float scale, bool isVerticalPad)
    {
        var center = rect.center;
        var radius = rect.width * 0.48f;

        if (!isVerticalPad)
        {
            DrawTransparentArrowButton(center, radius, direction, pressed, scale);
            return;
        }

        // Up/Down: arrow only — no opaque disc plate.
        var arrowColor = pressed
            ? new Color(1f, 0.92f, 0.35f, 1f)
            : new Color(0.96f, 0.97f, 1f, 1f);
        DrawDirectionalArrow(center, direction, arrowColor, radius * 0.78f);
    }

    /// <summary>Left/Right: faint accent ring + arrow, no fill plate.</summary>
    void DrawTransparentArrowButton(Vector2 center, float radius, MoveArrow direction, bool pressed, float scale)
    {
        var accent = new Color(0.92f, 0.4f, 0.44f, pressed ? 0.55f : 0.28f);
        DrawRing(center, radius * 0.78f, Mathf.Max(1.5f, 2f * scale), accent);

        if (pressed)
            DrawRing(center, radius * 0.98f, Mathf.Max(3f, 4f * scale), new Color(1f, 0.82f, 0.18f, 0.55f));

        var arrowColor = pressed ? new Color(1f, 0.92f, 0.35f, 0.95f) : new Color(0.96f, 0.97f, 1f, 0.82f);
        DrawDirectionalArrow(center, direction, arrowColor, radius * 0.72f);
    }

    void DrawLookZoneHint(float scale)
    {
        var center = GetLookHintCircleCenterGui();
        var radius = GetLookHintRadius();
        var active = lookActive;

        // Rings / brackets / crosshair only — no filled wash that obscures the view.
        DrawRing(center, radius, Mathf.Max(3.5f, 4.5f * scale), new Color(0.04f, 0.05f, 0.09f, 0.55f));
        DrawRing(center, radius * 0.84f, Mathf.Max(2.5f, 3f * scale), new Color(0.18f, 0.78f, 1f, active ? 0.82f : 0.5f));
        DrawRing(center, radius * 0.7f, Mathf.Max(1.8f, 2.2f * scale), new Color(0.42f, 0.92f, 1f, active ? 0.62f : 0.32f));

        if (active)
            DrawRing(center, radius * 0.99f, Mathf.Max(3f, 4f * scale), new Color(1f, 0.84f, 0.22f, 0.88f));

        DrawLookViewfinderBrackets(center, radius * 0.74f, scale, active);
        DrawLookCrosshair(center, radius * 0.24f, scale, active);
        DrawLookGimbalTicks(center, radius * 0.56f, scale, active);

        var labelRect = new Rect(center.x - radius, center.y + radius * 0.42f, radius * 2f, radius * 0.32f);
        DrawLookHintLabel(labelRect, active ? "PAN" : "LOOK", scale, active);
    }

    void DrawLookViewfinderBrackets(Vector2 center, float extent, float scale, bool active)
    {
        var arm = extent * 0.34f;
        var thickness = Mathf.Max(2.5f, 3.2f * scale);
        var color = active
            ? new Color(0.55f, 0.96f, 1f, 0.92f)
            : new Color(0.75f, 0.88f, 0.98f, 0.72f);

        var topLeft = center + new Vector2(-extent, -extent);
        var topRight = center + new Vector2(extent, -extent);
        var bottomLeft = center + new Vector2(-extent, extent);
        var bottomRight = center + new Vector2(extent, extent);

        DrawLine(topLeft, topLeft + new Vector2(arm, 0f), color, thickness);
        DrawLine(topLeft, topLeft + new Vector2(0f, arm), color, thickness);
        DrawLine(topRight, topRight + new Vector2(-arm, 0f), color, thickness);
        DrawLine(topRight, topRight + new Vector2(0f, arm), color, thickness);
        DrawLine(bottomLeft, bottomLeft + new Vector2(arm, 0f), color, thickness);
        DrawLine(bottomLeft, bottomLeft + new Vector2(0f, -arm), color, thickness);
        DrawLine(bottomRight, bottomRight + new Vector2(-arm, 0f), color, thickness);
        DrawLine(bottomRight, bottomRight + new Vector2(0f, -arm), color, thickness);
    }

    void DrawLookCrosshair(Vector2 center, float arm, float scale, bool active)
    {
        var thickness = Mathf.Max(2f, 2.6f * scale);
        var core = active
            ? new Color(1f, 0.9f, 0.35f, 0.95f)
            : new Color(0.92f, 0.97f, 1f, 0.88f);
        var shadow = new Color(0f, 0f, 0f, 0.55f);
        var gap = arm * 0.22f;

        DrawLine(center + new Vector2(-arm, 0f), center + new Vector2(-gap, 0f), shadow, thickness + 1f);
        DrawLine(center + new Vector2(gap, 0f), center + new Vector2(arm, 0f), shadow, thickness + 1f);
        DrawLine(center + new Vector2(0f, -arm), center + new Vector2(0f, -gap), shadow, thickness + 1f);
        DrawLine(center + new Vector2(0f, gap), center + new Vector2(0f, arm), shadow, thickness + 1f);

        DrawLine(center + new Vector2(-arm, 0f), center + new Vector2(-gap, 0f), core, thickness);
        DrawLine(center + new Vector2(gap, 0f), center + new Vector2(arm, 0f), core, thickness);
        DrawLine(center + new Vector2(0f, -arm), center + new Vector2(0f, -gap), core, thickness);
        DrawLine(center + new Vector2(0f, gap), center + new Vector2(0f, arm), core, thickness);
        DrawCircle(center, Mathf.Max(3f, 4f * scale), core);
    }

    void DrawLookGimbalTicks(Vector2 center, float tickRadius, float scale, bool active)
    {
        var tickLen = Mathf.Max(5f, 7f * scale);
        var thickness = Mathf.Max(1.5f, 2f * scale);
        var color = active
            ? new Color(0.35f, 0.86f, 1f, 0.75f)
            : new Color(0.55f, 0.78f, 0.92f, 0.45f);

        for (var i = 0; i < 4; i++)
        {
            var angle = (45f + i * 90f) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var inner = center + dir * (tickRadius - tickLen * 0.35f);
            var outer = center + dir * (tickRadius + tickLen * 0.35f);
            DrawLine(inner, outer, color, thickness);
        }
    }

    void DrawLookHintLabel(Rect rect, string label, float scale, bool active)
    {
        var shadowRect = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
        GUI.Label(shadowRect, label, lookHintShadowStyle);
        GUI.Label(rect, label, active ? lookHintLabelStyleActive : lookHintLabelStyleInactive);
    }

    void DrawDirectionalArrow(Vector2 center, MoveArrow direction, Color color, float size)
    {
        var tex = GetArrowTexture(direction);
        if (tex == null)
            return;

        var rect = new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        var shadowRect = new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height);
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(shadowRect, tex, ScaleMode.ScaleToFit, true);
        GUI.color = color;
        GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
        GUI.color = prev;
    }

    Texture2D GetArrowTexture(MoveArrow direction)
    {
        switch (direction)
        {
            case MoveArrow.Up: return arrowUpTexture;
            case MoveArrow.Down: return arrowDownTexture;
            case MoveArrow.Left: return arrowLeftTexture;
            default: return arrowRightTexture;
        }
    }

    void EnsureArrowTextures()
    {
        if (arrowUpTexture != null)
            return;

        arrowUpTexture = CreateArrowTexture(MoveArrow.Up);
        arrowDownTexture = CreateArrowTexture(MoveArrow.Down);
        arrowLeftTexture = CreateArrowTexture(MoveArrow.Left);
        arrowRightTexture = CreateArrowTexture(MoveArrow.Right);
    }

    static Texture2D CreateArrowTexture(MoveArrow direction)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 tip;
        Vector2 baseA;
        Vector2 baseB;
        var mid = (size - 1) * 0.5f;
        const float extent = 24f;

        switch (direction)
        {
            case MoveArrow.Up:
                tip = new Vector2(mid, mid - extent);
                baseA = new Vector2(mid - extent * 0.85f, mid + extent * 0.65f);
                baseB = new Vector2(mid + extent * 0.85f, mid + extent * 0.65f);
                break;
            case MoveArrow.Down:
                tip = new Vector2(mid, mid + extent);
                baseA = new Vector2(mid - extent * 0.85f, mid - extent * 0.65f);
                baseB = new Vector2(mid + extent * 0.85f, mid - extent * 0.65f);
                break;
            case MoveArrow.Left:
                tip = new Vector2(mid - extent, mid);
                baseA = new Vector2(mid + extent * 0.65f, mid - extent * 0.85f);
                baseB = new Vector2(mid + extent * 0.65f, mid + extent * 0.85f);
                break;
            default:
                tip = new Vector2(mid + extent, mid);
                baseA = new Vector2(mid - extent * 0.65f, mid - extent * 0.85f);
                baseB = new Vector2(mid - extent * 0.65f, mid + extent * 0.85f);
                break;
        }

        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                // Texture2D y=0 is bottom; flip so tip directions match GUI (y down).
                var py = size - 1 - y;
                var inside = PointInTriangle(new Vector2(x, py), tip, baseA, baseB);
                pixels[y * size + x] = inside
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        return tex;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        var v0 = c - a;
        var v1 = b - a;
        var v2 = p - a;
        var dot00 = Vector2.Dot(v0, v0);
        var dot01 = Vector2.Dot(v0, v1);
        var dot02 = Vector2.Dot(v0, v2);
        var dot11 = Vector2.Dot(v1, v1);
        var dot12 = Vector2.Dot(v1, v2);
        var invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;
        return u >= 0f && v >= 0f && (u + v) <= 1f;
    }

    void DrawGearShift()
    {
        var scale = GetUiScale();
        var labels = new[] { "RUN", "WALK" };

        for (var row = 0; row < GearRowCount; row++)
        {
            var rect = GetGearRowRect(row);
            var gear = row == 0 ? MobileMovementGear.Run : MobileMovementGear.Walk;
            DrawGearRow(rect, labels[row], selectedGear == gear, row == 0, scale);
        }
    }

    void DrawGearRow(Rect rect, string label, bool selected, bool isRunRow, float scale)
    {
        var outline = selected
            ? new Color(1f, 0.92f, 0.35f, 0.95f)
            : new Color(1f, 1f, 1f, 0.28f);
        var thickness = selected ? Mathf.Max(2.5f, 3f * scale) : Mathf.Max(1.5f, 2f * scale);
        DrawRectOutline(rect, outline, thickness);

        if (isRunRow && runIconTexture != null)
        {
            var iconSize = rect.height * 0.78f;
            var iconRect = new Rect(rect.x + 6f, rect.center.y - iconSize * 0.5f, iconSize, iconSize);
            // No solid backdrop — icon only; brighter when selected.
            DrawTintedIcon(iconRect, runIconTexture, 0.92f, GetIconTint(MobileActionIconKind.Run, selected));
        }

        var textRect = isRunRow && runIconTexture != null
            ? new Rect(rect.x + rect.height, rect.y, rect.width - rect.height, rect.height)
            : rect;
        GUI.Label(textRect, label, selected ? gearLabelStyleSelected : gearLabelStyle);
    }

    void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        var topLeft = new Vector2(rect.xMin, rect.yMin);
        var topRight = new Vector2(rect.xMax, rect.yMin);
        var bottomLeft = new Vector2(rect.xMin, rect.yMax);
        var bottomRight = new Vector2(rect.xMax, rect.yMax);
        DrawLine(topLeft, topRight, color, thickness);
        DrawLine(topRight, bottomRight, color, thickness);
        DrawLine(bottomRight, bottomLeft, color, thickness);
        DrawLine(bottomLeft, topLeft, color, thickness);
    }

    void EnsureLabelStyles(float scale)
    {
        if (lookHintShadowStyle != null && Mathf.Approximately(cachedLabelUiScale, scale))
            return;

        cachedLabelUiScale = scale;
        var lookFontSize = Mathf.RoundToInt(16f * scale);
        lookHintShadowStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = lookFontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0f, 0f, 0f, 0.65f) }
        };
        lookHintLabelStyleActive = new GUIStyle(lookHintShadowStyle)
        {
            normal = { textColor = new Color(1f, 0.92f, 0.38f, 0.98f) }
        };
        lookHintLabelStyleInactive = new GUIStyle(lookHintShadowStyle)
        {
            normal = { textColor = new Color(0.82f, 0.94f, 1f, 0.92f) }
        };

        gearLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(24f * scale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 1f, 1f, 0.72f) }
        };
        gearLabelStyleSelected = new GUIStyle(gearLabelStyle)
        {
            normal = { textColor = new Color(1f, 0.92f, 0.35f, 0.98f) }
        };
    }

    void EnsureCircleTexture()
    {
        if (circleTexture != null)
            return;

        const int size = 64;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.wrapMode = TextureWrapMode.Clamp;
        var center = (size - 1) * 0.5f;
        var radius = size * 0.5f;
        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = dist <= radius ? (byte)Mathf.Clamp(200f * (1f - dist / radius), 40f, 200f) : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        circleTexture.SetPixels32(pixels);
        circleTexture.Apply();
    }

    void EnsureLineTexture()
    {
        if (lineTexture != null)
            return;

        lineTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        lineTexture.SetPixel(0, 0, Color.white);
        lineTexture.Apply();
    }

    void EnsureActionIconTextures()
    {
        if (jumpIconTexture == null)
        {
            jumpIconTexture = Resources.Load<Texture2D>("MobileUI/DutzMobileKangaroo");
            if (jumpIconTexture == null)
                jumpIconTexture = Resources.Load<Texture2D>("MobileUI/DutzMobileJump");
        }

        if (punchIconTexture == null)
            punchIconTexture = Resources.Load<Texture2D>("MobileUI/DutzMobilePunch");

        if (runIconTexture == null)
            runIconTexture = Resources.Load<Texture2D>("MobileUI/DutzMobileRun");
    }

    void DrawJumpButton() =>
        DrawVibrantActionButton(GetJumpButtonRect(), jumpIconTexture, MobileActionIconKind.Jump, jumpHeld);

    void DrawPunchButton() =>
        DrawVibrantActionButton(GetPunchButtonRect(), punchIconTexture, MobileActionIconKind.Punch, false);

    void DrawVibrantActionButton(Rect rect, Texture2D icon, MobileActionIconKind kind, bool pressed)
    {
        DrawVibrantIconBackdrop(rect, kind, pressed);
        DrawTintedIcon(rect, icon, 0.88f, GetIconTint(kind, pressed));
    }

    void DrawVibrantIconBackdrop(Rect rect, MobileActionIconKind kind, bool active)
    {
        // Idle: no plate — icon alone. Pressed/active: rim highlight only.
        if (!active)
            return;

        GetVibrantIconPalette(kind, active, out _, out _, out var rim);
        var center = rect.center;
        var outerRadius = rect.width * 0.54f;
        DrawRing(center, outerRadius * 0.96f, Mathf.Max(3f, rect.width * 0.04f), rim);
    }

    static void GetVibrantIconPalette(
        MobileActionIconKind kind,
        bool active,
        out Color outerGlow,
        out Color innerGlow,
        out Color rim)
    {
        switch (kind)
        {
            case MobileActionIconKind.Jump:
                outerGlow = active
                    ? new Color(1f, 0.88f, 0.12f, 0.48f)
                    : new Color(1f, 0.8f, 0.08f, 0.32f);
                innerGlow = active
                    ? new Color(1f, 0.62f, 0.04f, 0.42f)
                    : new Color(1f, 0.55f, 0.02f, 0.26f);
                rim = new Color(1f, 0.96f, 0.42f, 0.88f);
                return;
            case MobileActionIconKind.Punch:
                outerGlow = active
                    ? new Color(1f, 0.28f, 0.12f, 0.5f)
                    : new Color(1f, 0.22f, 0.1f, 0.34f);
                innerGlow = active
                    ? new Color(0.95f, 0.1f, 0.06f, 0.44f)
                    : new Color(0.88f, 0.08f, 0.05f, 0.28f);
                rim = new Color(1f, 0.58f, 0.28f, 0.9f);
                return;
            default:
                outerGlow = active
                    ? new Color(0.18f, 0.98f, 1f, 0.46f)
                    : new Color(0.12f, 0.88f, 1f, 0.3f);
                innerGlow = active
                    ? new Color(0.08f, 0.72f, 0.98f, 0.4f)
                    : new Color(0.06f, 0.58f, 0.86f, 0.24f);
                rim = new Color(0.45f, 1f, 1f, 0.88f);
                return;
        }
    }

    static Color GetIconTint(MobileActionIconKind kind, bool active)
    {
        switch (kind)
        {
            case MobileActionIconKind.Jump:
                return active
                    ? new Color(1.18f, 1.1f, 0.88f, 1f)
                    : new Color(1.1f, 1.05f, 0.9f, 1f);
            case MobileActionIconKind.Punch:
                return active
                    ? new Color(1.2f, 1.04f, 0.98f, 1f)
                    : new Color(1.12f, 1f, 0.96f, 1f);
            default:
                return active
                    ? new Color(0.92f, 1.18f, 1.22f, 1f)
                    : new Color(0.88f, 1.1f, 1.16f, 1f);
        }
    }

    void DrawTintedIcon(Rect rect, Texture2D icon, float sizeFraction, Color tint)
    {
        if (icon == null)
            return;

        var iconSize = rect.width * sizeFraction;
        var iconRect = new Rect(
            rect.center.x - iconSize * 0.5f,
            rect.center.y - iconSize * 0.5f,
            iconSize,
            iconSize);
        var prev = GUI.color;
        GUI.color = tint;
        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        GUI.color = prev;
    }

    void DrawCircle(Vector2 center, float radius, Color color)
    {
        var size = radius * 2f;
        var rect = new Rect(center.x - radius, center.y - radius, size, size);
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, circleTexture);
        GUI.color = prev;
    }

    void DrawRing(Vector2 center, float radius, float thickness, Color color)
    {
        if (lineTexture == null)
            return;

        var segments = Application.isMobilePlatform ? 24 : 48;
        var step = 360f / segments;
        for (var i = 0; i < segments; i++)
        {
            var a0 = i * step * Mathf.Deg2Rad;
            var a1 = (i + 1) * step * Mathf.Deg2Rad;
            var p0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * radius;
            var p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
            DrawLine(p0, p1, color, thickness);
        }
    }

    void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (lineTexture == null)
            return;

        var delta = end - start;
        var length = delta.magnitude;
        if (length < 0.5f)
            return;

        var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        var rect = new Rect(start.x, start.y - thickness * 0.5f, length, thickness);
        var matrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, start);
        var prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, lineTexture);
        GUI.color = prev;
        GUI.matrix = matrix;
    }
}
