using UnityEngine;

/// <summary>
/// Level07 Senate mural — unlocks only after Boy Idol is killed.
/// Buy a vote package to reach 16 (Sara convicted), or accept defeat.
/// Buying a package and still under 16 votes plays Sara acquitted.
/// No dismiss / escape exit.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(2400)]
public class DutzSenateVotesOffer : MonoBehaviour
{
    public const string PanelName = "SenateMural_Highway Straight 6";
    public const string LegacyPanelName = "SenateMural_Highway 8";
    public const string RootName = "DutzLevel07SenateMural";
    public const int MaletaCost = 38;
    public const int VotesReward = 13;
    public const int VotesToImpeach = 16;
    const float NearReachMeters = 10f;
    const float ConfirmDelay = 0.35f;
    const int OverlayGuiDepth = -2600;

    struct VotePackage
    {
        public readonly int SuitcaseCost;
        public readonly int Votes;
        public readonly string ButtonLabel;

        public VotePackage(int suitcaseCost, int votes)
        {
            SuitcaseCost = suitcaseCost;
            Votes = votes;
            ButtonLabel = $"GIVE {suitcaseCost} SUITCASES FOR {votes} VOTES";
        }
    }

    // Cheapest first — stays visible when the dialog frame is height-clamped.
    static readonly VotePackage[] Packages =
    {
        new VotePackage(29, 10),
        new VotePackage(32, 11),
        new VotePackage(35, 12),
        new VotePackage(38, 13),
    };

    static readonly string Title = "SENATE";
    static readonly string Hint =
        "Need 16 votes to impeach Princess Z.\n" +
        "Buy votes, or accept defeat (Sara acquitted).";
    static readonly string DefeatLabel = "ACCEPT DEFEAT";

    static readonly Color OverlayTitleColor = new Color(1f, 0.95f, 0.55f, 1f);
    static readonly Color OverlayHintColor = new Color(1f, 1f, 1f, 0.95f);
    static readonly Color BuyButtonFill = new Color(0.35f, 0.78f, 1f, 0.72f);
    static readonly Color DefeatButtonFill = new Color(0.95f, 0.32f, 0.28f, 0.72f);

    DutzPlayerController player;
    bool showing;
    bool wasNear;
    float shownAt;
    string statusMessage = string.Empty;
    Texture muralBackdrop;
    Vector2 packageScroll;

    public static bool IsShowing
    {
        get
        {
            foreach (var offer in Object.FindObjectsOfType<DutzSenateVotesOffer>(true))
            {
                if (offer != null && offer.showing)
                    return true;
            }

            return false;
        }
    }

    public static void EnsureOn(GameObject mural)
    {
        if (mural == null)
            return;

        var bump = mural.GetComponent<DutzMuralBumpMessage>();
        if (bump != null)
        {
            if (Application.isPlaying)
                Object.Destroy(bump);
            else
                Object.DestroyImmediate(bump);
        }

        if (mural.GetComponent<DutzSenateVotesOffer>() == null)
            mural.AddComponent<DutzSenateVotesOffer>();

        EnsureNearTrigger(mural);
    }

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        var panel = FindPanel();
        if (panel != null)
            EnsureOn(panel);
    }

    static GameObject FindPanel()
    {
        var panel = GameObject.Find(PanelName);
        if (panel != null)
            return panel;

        panel = GameObject.Find(LegacyPanelName);
        if (panel != null)
            return panel;

        var root = GameObject.Find(RootName);
        if (root == null)
            return null;

        var child = root.transform.Find(PanelName);
        if (child != null)
            return child.gameObject;

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name.StartsWith("SenateMural_", System.StringComparison.Ordinal))
                return t.gameObject;
        }

        return null;
    }

    static void EnsureNearTrigger(GameObject mural)
    {
        if (mural == null)
            return;

        var box = mural.GetComponent<BoxCollider>();
        if (box == null)
            box = mural.AddComponent<BoxCollider>();

        box.isTrigger = true;

        var renderer = mural.GetComponent<Renderer>();
        if (renderer == null)
            return;

        var bounds = renderer.bounds;
        var localCenter = mural.transform.InverseTransformPoint(bounds.center);
        var localSize = mural.transform.InverseTransformVector(bounds.size);
        localSize.x = Mathf.Abs(localSize.x);
        localSize.y = Mathf.Abs(localSize.y);
        localSize.z = Mathf.Abs(localSize.z);

        localSize.x = Mathf.Max(localSize.x, NearReachMeters * 2f);
        localSize.y = Mathf.Max(localSize.y, NearReachMeters);
        localSize.z = Mathf.Max(localSize.z, NearReachMeters * 2f);
        box.center = localCenter;
        box.size = localSize;
    }

    void Awake() => EnsureNearTrigger(gameObject);

    void Update()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        if (player == null)
            player = DutzPlayerController.Instance;

        if (showing)
            return;

        if (player == null || DutzLevelObjective.IsLevelFinishedForActiveScene)
            return;

        if (DutzPoliceCaptureDialog.IsShowing || DutzGrandmaBossPowerShop.IsShowingDialog)
            return;

        // Senate unlocks only after Boy Idol is defeated.
        if (!DutzLevel07BoyIdolGate.IsBoyIdolDefeated)
        {
            wasNear = false;
            return;
        }

        var near = IsPlayerNear();
        if (near && !wasNear)
            OpenDialog();

        wasNear = near;
    }

    bool IsPlayerNear()
    {
        if (player == null)
            return false;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null)
            return false;

        var col = GetComponent<Collider>();
        if (col != null && col.enabled)
        {
            return DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(
                col,
                cc,
                DutzHippieBiteCollider.PlayerCapsulePadding,
                NearReachMeters * 0.35f);
        }

        var closest = GetComponent<Renderer>() != null
            ? GetComponent<Renderer>().bounds.ClosestPoint(cc.bounds.center)
            : transform.position;
        var pad = NearReachMeters + cc.radius;
        return (closest - cc.bounds.center).sqrMagnitude <= pad * pad;
    }

    void OpenDialog()
    {
        // Already at 16+ after Boy Idol — win without the deal table.
        if (DutzVotesCounter.Votes >= VotesToImpeach)
        {
            DutzLevelObjective.NotifyLevel07ImpeachmentWon();
            return;
        }

        showing = true;
        shownAt = Time.unscaledTime;
        statusMessage = string.Empty;
        packageScroll = Vector2.zero;
        if (player != null)
            player.SetControlsLocked(true);

        if (!Application.isMobilePlatform)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void TryBuyVotes(VotePackage package)
    {
        if (!CanConfirm())
            return;

        if (DutzCollectibleProgress.CollectedCount < package.SuitcaseCost)
        {
            statusMessage =
                $"Need {package.SuitcaseCost} suitcases (you have {DutzCollectibleProgress.CollectedCount}).";
            return;
        }

        if (!DutzCollectibleProgress.TrySpend(package.SuitcaseCost))
        {
            statusMessage = "Could not spend suitcases.";
            return;
        }

        showing = false;
        if (player != null)
            player.SetControlsLocked(true);

        DutzVotesCounter.AddVotes(package.Votes);
        Debug.Log(
            $"[Dutz] Senate deal: {package.SuitcaseCost} suitcases → {package.Votes} votes " +
            $"(total {DutzVotesCounter.Votes}/{VotesToImpeach}).");

        // Purchase that still falls short of 16 → Sara acquitted.
        if (DutzVotesCounter.Votes < VotesToImpeach)
            DutzLevelObjective.NotifyLevel07ImpeachmentFailedNotEnoughVotes();
    }

    void AcceptDefeat()
    {
        if (!CanConfirm())
            return;

        showing = false;
        if (player != null)
            player.SetControlsLocked(true);

        StartCoroutine(AcceptDefeatRoutine());
    }

    System.Collections.IEnumerator AcceptDefeatRoutine()
    {
        yield return DutzLevel07ImpeachmentVideo.PlayFailThenReloadLevel();
    }

    bool CanConfirm() => Time.unscaledTime - shownAt >= ConfirmDelay;

    void OnGUI()
    {
        if (!showing)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = OverlayGuiDepth;

        EnsureMuralBackdrop();
        DutzCartoonDialogGui.DrawFullscreenBackdrop(muralBackdrop);

        var suitcases = DutzCollectibleProgress.CollectedCount;
        var affordableCount = 0;
        for (var i = 0; i < Packages.Length; i++)
        {
            if (suitcases >= Packages[i].SuitcaseCost)
                affordableCount++;
        }

        var hint = BuildHint(suitcases, affordableCount);
        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(OverlayTitleColor);
        var hintStyle = DutzCartoonDialogGui.HintStyle(OverlayHintColor);
        var bodyStyle = DutzCartoonDialogGui.BodyStyle();
        bodyStyle.normal.textColor = OverlayHintColor;

        var contentWidth = DutzCartoonDialogGui.PanelWidth
            - DutzCartoonDialogGui.ContentInset * 2f
            - DutzCartoonDialogGui.PanelPadding * 2f;
        var spacing = DutzCartoonDialogGui.Scale(6f, 10f);
        var defeatHeight = DutzCartoonDialogGui.MeasureActionButtonHeight(DefeatLabel);

        // Header (title/hint/ACCEPT DEFEAT) is always visible; buy list scrolls if needed.
        // Old layout stacked 4 buy rows + need-hints + defeat at the bottom — ChoiceDialogFrame
        // clamps to ~94% of screen and BeginArea crops anything past that, so 29 + defeat vanished.
        var headerInner =
            DutzCartoonDialogGui.PanelPadding
            + DutzCartoonDialogGui.MeasureLabelHeight(Title, titleStyle, contentWidth)
            + spacing
            + DutzCartoonDialogGui.MeasureLabelHeight(hint, hintStyle, contentWidth)
            + DutzCartoonDialogGui.Scale(10f, 14f)
            + defeatHeight
            + spacing;

        var packageInner = 0f;
        if (affordableCount > 0)
        {
            for (var i = 0; i < Packages.Length; i++)
            {
                if (suitcases < Packages[i].SuitcaseCost)
                    continue;
                packageInner += DutzCartoonDialogGui.MeasureActionButtonHeight(Packages[i].ButtonLabel)
                    + spacing;
            }
        }

        var statusInner = 0f;
        if (!string.IsNullOrEmpty(statusMessage))
        {
            statusInner = spacing
                + DutzCartoonDialogGui.MeasureLabelHeight(statusMessage, bodyStyle, contentWidth);
        }

        var naturalHeight = DutzCartoonDialogGui.ContentInset * 2f
            + headerInner
            + packageInner
            + statusInner
            + DutzCartoonDialogGui.PanelPadding;
        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(naturalHeight);
        DutzCartoonDialogGui.DrawTransparentFrame(frame);

        var content = DutzCartoonDialogGui.ContentRect(frame);
        var headerHeight = Mathf.Min(headerInner, content.height);
        var statusHeight = Mathf.Min(statusInner, Mathf.Max(0f, content.height - headerHeight));
        var packageAreaHeight = Mathf.Max(0f, content.height - headerHeight - statusHeight);

        var headerRect = new Rect(content.x, content.y, content.width, headerHeight);
        var packageRect = new Rect(content.x, content.y + headerHeight, content.width, packageAreaHeight);
        var statusRect = new Rect(
            content.x,
            content.y + headerHeight + packageAreaHeight,
            content.width,
            statusHeight);

        var previousBg = GUI.backgroundColor;

        GUILayout.BeginArea(headerRect);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(Title, titleStyle);
        GUILayout.Space(spacing);
        GUILayout.Label(hint, hintStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 14f));
        GUI.enabled = CanConfirm();
        if (DutzCartoonDialogGui.ActionButton(DefeatLabel, DutzCartoonDialogGui.PlasticButtonColor.Red, defeatHeight))
            AcceptDefeat();
        GUI.enabled = true;
        GUILayout.EndArea();

        if (affordableCount > 0 && packageAreaHeight > 1f)
        {
            GUILayout.BeginArea(packageRect);
            var needsScroll = packageInner > packageAreaHeight + 1f;
            if (needsScroll)
                packageScroll = GUILayout.BeginScrollView(packageScroll);

            for (var i = 0; i < Packages.Length; i++)
            {
                var package = Packages[i];
                if (suitcases < package.SuitcaseCost)
                    continue;

                GUI.enabled = CanConfirm();
                if (DutzCartoonDialogGui.ActionButton(
                        package.ButtonLabel,
                        DutzCartoonDialogGui.PlasticButtonColor.Blue,
                        DutzCartoonDialogGui.MeasureActionButtonHeight(package.ButtonLabel)))
                    TryBuyVotes(package);
                GUI.enabled = true;
                GUILayout.Space(spacing);
            }

            if (needsScroll)
                GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        if (!string.IsNullOrEmpty(statusMessage) && statusHeight > 1f)
        {
            GUILayout.BeginArea(statusRect);
            GUILayout.Space(spacing);
            GUILayout.Label(statusMessage, bodyStyle);
            GUILayout.EndArea();
        }

        GUI.backgroundColor = previousBg;
        GUI.depth = previousDepth;
    }

    void EnsureMuralBackdrop()
    {
        if (muralBackdrop != null)
            return;

        var renderer = GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
            muralBackdrop = renderer.sharedMaterial.mainTexture;

        if (muralBackdrop == null)
            muralBackdrop = Resources.Load<Texture2D>("CollectibleHud/Level07Senate")
                ?? Resources.Load<Texture2D>("Level07Senate");
    }

    string BuildHint(int suitcases, int affordableCount)
    {
        var status =
            $"Votes: {DutzVotesCounter.Votes}/{VotesToImpeach}  ·  Suitcases: {suitcases}";
        if (affordableCount <= 0)
        {
            var cheapest = Packages[0].SuitcaseCost;
            return
                $"{Hint}\n{status}\n" +
                $"You need at least {cheapest} suitcases to buy votes.";
        }

        return $"{Hint}\n{status}";
    }
}
