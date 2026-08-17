using UnityEngine;

/// <summary>
/// Level 3 parachute pickup — scene-authored only. No spawning, movement, or auto-setup.
/// Place the object, mesh, and collider manually in the scene.
/// </summary>
[DisallowMultipleComponent]
public class DutzParachutePickup : MonoBehaviour
{
    public const float DefaultDeploySeconds = 10f;
    public const string Bridge5PickupName = "DutzParachutePickup_Bridge5";
    public const string Bridge5HighwayName = "Highway Bridge 5";
    public const string Straight6LandHighwayName = "Highway Straight 6";

    const float PlayerTouchPaddingMeters = 0.35f;

    [Tooltip("Seconds the parachute stays deployed after pickup. When it ends, the player falls normally.")]
    [SerializeField] float deploySeconds = DefaultDeploySeconds;
    [SerializeField] float collectReachMeters = 3.5f;

    bool collected;

    public bool IsCollected => collected;
    public float DeploySeconds => Mathf.Max(0.1f, deploySeconds);

    /// <summary>Bridge 5 chute only lands/clears on Highway Straight 6 (timer still expires normally).</summary>
    public bool LandsOnlyOnStraight6 =>
        string.Equals(gameObject.name, Bridge5PickupName, System.StringComparison.Ordinal);

    public bool IsPlayerTouching(CharacterController cc)
    {
        if (cc == null)
            return false;

        var playerCenter = cc.transform.position + cc.center * Mathf.Max(0.01f, cc.transform.lossyScale.y);
        var pickupCenter = GetCollectCenter();
        var delta = playerCenter - pickupCenter;

        var horizontalReach = collectReachMeters + cc.radius * cc.transform.lossyScale.x;
        var horizontal = new Vector2(delta.x, delta.z);
        if (horizontal.magnitude > horizontalReach)
            return false;

        var verticalReach = collectReachMeters + cc.height * 0.55f * cc.transform.lossyScale.y;
        return Mathf.Abs(delta.y) <= verticalReach;
    }

    Vector3 GetCollectCenter()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.center;
        }

        return transform.position;
    }

    public void Collect(DutzPlayerController player)
    {
        if (collected || player == null)
            return;

        collected = true;

        var parachute = player.GetComponent<DutzPlayerParachute>();
        if (parachute == null)
            parachute = player.gameObject.AddComponent<DutzPlayerParachute>();

        if (LandsOnlyOnStraight6)
        {
            PrepareForWearOnPlayer();
            parachute.AttachWornPickup(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }

        parachute.GrantParachuteTime(
            DeploySeconds,
            LandsOnlyOnStraight6 ? Straight6LandHighwayName : null);
        Debug.Log(LandsOnlyOnStraight6
            ? $"[Dutz] Parachute 5 collected — {DeploySeconds:0}s glide; lands only on {Straight6LandHighwayName}."
            : $"[Dutz] Parachute collected — {DeploySeconds:0}s safe glide active.");
    }

    void PrepareForWearOnPlayer()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
                col.enabled = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        enabled = false;
    }
}

/// <summary>Parachute glide timer — slow fall, no edge/long-fall death until timer expires.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(51)]
public class DutzPlayerParachute : MonoBehaviour
{
    const float ParachuteGravity = -5.5f;
    const float MaxGlideDownSpeed = 9f;
    const float LandHighwayRayLength = 12f;

    float parachuteActiveUntil;
    float timerBarMaxSeconds;
    int parachutePickupsCollected;
    bool hasLeftGroundSinceGrant;
    string landOnlyHighwayName;
    string takeoffHighwayName;
    GameObject wornPickup;
    readonly System.Collections.Generic.List<Collider> ignoredLandColliders =
        new System.Collections.Generic.List<Collider>(16);

    DutzPlayerController player;
    CharacterController cc;
    DutzFallRespawn fallRespawn;

    public bool IsParachuteActive => Time.time < parachuteActiveUntil;
    public float ParachuteSecondsRemaining =>
        IsParachuteActive ? parachuteActiveUntil - Time.time : 0f;

    public int ParachutePickupsCollected => parachutePickupsCollected;
    public bool HasParachuteActive => IsParachuteActive;
    public bool IsGlidingSafely => IsParachuteActive && cc != null && IsAirborneForParachute();
    public bool IsProtectedFromFallDeath => IsParachuteActive;

    /// <summary>Bridge 5 chute: ignore OTHER highways until Straight 6 — keep Bridge 5 solid.</summary>
    public bool ShouldSuppressGroundWhileGliding =>
        IsParachuteActive
        && !string.IsNullOrEmpty(landOnlyHighwayName)
        && hasLeftGroundSinceGrant
        && !IsStandingOnLandHighway()
        && !IsStandingOnTakeoffHighway();

    public void GrantParachuteTime(float seconds) => GrantParachuteTime(seconds, null);

    public void GrantParachuteTime(float seconds, string landOnlyOnHighway)
    {
        if (seconds <= 0f)
            return;

        parachutePickupsCollected = Mathf.Min(2, parachutePickupsCollected + 1);

        var extendFrom = Mathf.Max(Time.time, parachuteActiveUntil);
        parachuteActiveUntil = extendFrom + seconds;
        timerBarMaxSeconds = ParachuteSecondsRemaining;
        hasLeftGroundSinceGrant = false;
        takeoffHighwayName = null;
        if (!string.IsNullOrEmpty(landOnlyOnHighway))
        {
            landOnlyHighwayName = landOnlyOnHighway;
            // Bridge 5 → Straight 6: never strip collision from the takeoff deck.
            if (string.Equals(
                    landOnlyOnHighway,
                    DutzParachutePickup.Straight6LandHighwayName,
                    System.StringComparison.Ordinal))
                takeoffHighwayName = DutzParachutePickup.Bridge5HighwayName;
        }
        else
        {
            landOnlyHighwayName = null;
        }

        Debug.Log(string.IsNullOrEmpty(landOnlyHighwayName)
            ? $"[Dutz] Parachute pickup {parachutePickupsCollected}/2 — {ParachuteSecondsRemaining:0.0}s glide time."
            : $"[Dutz] Parachute pickup {parachutePickupsCollected}/2 — {ParachuteSecondsRemaining:0.0}s glide; land only on {landOnlyHighwayName}.");
    }

    public void ClearForRespawn()
    {
        RestoreIgnoredLandColliders();
        ReleaseWornPickup();
        parachuteActiveUntil = 0f;
        timerBarMaxSeconds = 0f;
        parachutePickupsCollected = 0;
        hasLeftGroundSinceGrant = false;
        landOnlyHighwayName = null;
        takeoffHighwayName = null;
    }

    void ClearParachute(string reason)
    {
        if (parachuteActiveUntil <= 0f && timerBarMaxSeconds <= 0f)
            return;

        RestoreIgnoredLandColliders();
        ReleaseWornPickup();
        parachuteActiveUntil = 0f;
        timerBarMaxSeconds = 0f;
        hasLeftGroundSinceGrant = false;
        landOnlyHighwayName = null;
        takeoffHighwayName = null;
        Debug.Log($"[Dutz] Parachute cleared — {reason}.");
    }

    public void AttachWornPickup(GameObject pickup)
    {
        if (pickup == null)
            return;

        ReleaseWornPickup();
        wornPickup = pickup;

        var scale = Mathf.Max(1f, transform.lossyScale.y);
        pickup.transform.SetParent(transform, false);
        pickup.transform.localPosition = Vector3.zero;
        pickup.transform.localRotation = Quaternion.identity;
        pickup.transform.localScale = Vector3.one;
        pickup.SetActive(true);

        var visual = pickup.transform.Find("ParachuteVisual");
        if (visual != null)
        {
            // Canopy sits above/behind Player1 like a real chute.
            visual.localPosition = new Vector3(0f, 3.2f * scale, -0.45f * scale);
            visual.localRotation = Quaternion.identity;
            visual.localScale = new Vector3(48f, 22f, 48f);
            visual.gameObject.SetActive(true);
        }
    }

    void ReleaseWornPickup()
    {
        if (wornPickup == null)
            return;

        var go = wornPickup;
        wornPickup = null;
        if (go != null)
            Object.Destroy(go);
    }

    void LateUpdate()
    {
        if (wornPickup == null || !IsParachuteActive)
            return;

        // Keep the canopy upright in world space while locked to the player yaw.
        var visual = wornPickup.transform.Find("ParachuteVisual");
        if (visual == null)
            return;

        var scale = Mathf.Max(1f, transform.lossyScale.y);
        visual.position = transform.position
            + Vector3.up * (3.2f * scale)
            - transform.forward * (0.45f * scale);

        var flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = Vector3.forward;

        visual.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }

    void OnDisable()
    {
        RestoreIgnoredLandColliders();
        ReleaseWornPickup();
    }

    void FixedUpdate()
    {
        if (!IsParachuteActive || string.IsNullOrEmpty(landOnlyHighwayName) || cc == null)
        {
            RestoreIgnoredLandColliders();
            return;
        }

        // Keep Bridge 5 (and any takeoff deck) solid until the player jumps off.
        // Ignoring it immediately after pickup was sinking players through the road.
        if (!hasLeftGroundSinceGrant || IsStandingOnLandHighway() || IsStandingOnTakeoffHighway())
        {
            RestoreIgnoredLandColliders();
            return;
        }

        IgnoreNonTargetHighwaysAndStructuresNearPlayer();
    }

    void IgnoreNonTargetHighwaysAndStructuresNearPlayer()
    {
        var scale = Mathf.Max(1f, transform.lossyScale.y);
        var center = transform.position + Vector3.up * (0.8f * scale);
        var radius = Mathf.Max(2.5f, cc.radius * transform.lossyScale.x + 2f);
        var nearby = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < nearby.Length; i++)
        {
            var col = nearby[i];
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            if (IsColliderOnNamedHighway(col, landOnlyHighwayName))
                continue;

            if (!string.IsNullOrEmpty(takeoffHighwayName)
                && IsColliderOnNamedHighway(col, takeoffHighwayName))
                continue;

            if (!IsHighwayOrStructureCollider(col))
                continue;

            if (ignoredLandColliders.Contains(col))
                continue;

            Physics.IgnoreCollision(cc, col, true);
            ignoredLandColliders.Add(col);
        }
    }

    void RestoreIgnoredLandColliders()
    {
        if (ignoredLandColliders.Count == 0)
            return;

        for (var i = 0; i < ignoredLandColliders.Count; i++)
        {
            var col = ignoredLandColliders[i];
            if (col != null && cc != null)
                Physics.IgnoreCollision(cc, col, false);
        }

        ignoredLandColliders.Clear();
    }

    static bool IsHighwayOrStructureCollider(Collider col)
    {
        var t = col != null ? col.transform : null;
        while (t != null)
        {
            var name = t.name;
            if (name.StartsWith("Highway", System.StringComparison.Ordinal)
                || name.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }

        return false;
    }

    void Awake()
    {
        player = GetComponent<DutzPlayerController>();
        cc = GetComponent<CharacterController>();
        fallRespawn = GetComponent<DutzFallRespawn>();
    }

    void Update()
    {
        if (parachuteActiveUntil <= 0f)
            return;

        if (IsStandingOnGround())
        {
            if (hasLeftGroundSinceGrant && CanClearOnCurrentGround())
            {
                ClearParachute(string.IsNullOrEmpty(landOnlyHighwayName)
                    ? "touched ground"
                    : $"landed on {landOnlyHighwayName}");
                return;
            }
        }
        else
        {
            hasLeftGroundSinceGrant = true;
        }

        if (Time.time >= parachuteActiveUntil)
            ClearParachute("timer expired");
    }

    bool CanClearOnCurrentGround()
    {
        if (string.IsNullOrEmpty(landOnlyHighwayName))
            return true;

        // Parachute 5: only Straight 6 ends the chute while the timer is running.
        return IsStandingOnLandHighway();
    }

    bool IsStandingOnGround()
    {
        if (cc == null)
            return false;

        if (!cc.isGrounded)
            return false;

        return player == null || player.VerticalSpeed <= 0.5f;
    }

    bool IsStandingOnLandHighway()
    {
        if (cc == null || string.IsNullOrEmpty(landOnlyHighwayName))
            return false;

        return IsStandingOnNamedHighway(landOnlyHighwayName);
    }

    bool IsStandingOnTakeoffHighway()
    {
        if (cc == null || string.IsNullOrEmpty(takeoffHighwayName))
            return false;

        return IsStandingOnNamedHighway(takeoffHighwayName);
    }

    bool IsStandingOnNamedHighway(string highwayName)
    {
        var probe = transform.position + Vector3.up * 0.6f;
        var hits = Physics.RaycastAll(
            probe,
            Vector3.down,
            LandHighwayRayLength,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (var i = 0; i < hits.Length; i++)
        {
            var col = hits[i].collider;
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            if (IsColliderOnNamedHighway(col, highwayName))
                return true;
        }

        return false;
    }

    static bool IsColliderOnNamedHighway(Collider col, string highwayName)
    {
        var t = col != null ? col.transform : null;
        while (t != null)
        {
            if (string.Equals(t.name, highwayName, System.StringComparison.Ordinal))
                return true;
            t = t.parent;
        }

        return false;
    }

    bool IsAirborneForParachute()
    {
        if (cc == null)
            return false;

        if (!cc.isGrounded)
            return true;

        return player != null && player.VerticalSpeed > 0.5f;
    }

    void OnGUI()
    {
        if (!IsParachuteActive || player == null || player.ControlsLocked)
            return;

        if (fallRespawn != null && fallRespawn.IsShowingRespawnDialog)
            return;

        DrawParachuteTimerHud();
    }

    void DrawParachuteTimerHud()
    {
        const float padding = 16f;
        const float width = 240f;
        const float labelHeight = 26f;
        const float countdownHeight = 44f;
        const float barHeight = 18f;
        var top = DutzUpperLeftHudLayout.YFor(DutzUpperLeftHudLayout.Slot.Parachute);

        var remaining = ParachuteSecondsRemaining;
        var countdownSeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
        var barFill = timerBarMaxSeconds > 0.01f
            ? Mathf.Clamp01(remaining / timerBarMaxSeconds)
            : 0f;

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.55f, 0.9f, 1f) }
        };

        var countdownStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        var subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.75f, 0.95f, 1f, 0.9f) }
        };

        var chargeLabel = parachutePickupsCollected > 0
            ? $"PARACHUTE GLIDE  ({parachutePickupsCollected}/2)"
            : "PARACHUTE GLIDE";
        GUI.Label(new Rect(padding, top, width, labelHeight), chargeLabel, titleStyle);
        GUI.Label(new Rect(padding, top + labelHeight, width, countdownHeight), $"{countdownSeconds:00}s", countdownStyle);
        GUI.Label(
            new Rect(padding + 92f, top + labelHeight + 8f, width, 24f),
            "remaining",
            subStyle);

        var barRect = new Rect(padding, top + labelHeight + countdownHeight + 4f, width, barHeight);
        var fillRect = new Rect(barRect.x + 2f, barRect.y + 2f, (barRect.width - 4f) * barFill, barRect.height - 4f);

        GUI.Box(barRect, GUIContent.none, GUI.skin.box);
        GUI.color = new Color(0.2f, 0.75f, 1f, 0.9f);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    public bool TryArmBridgeJump(Vector3 moveDir, float speed, ref float velocityY, float jumpForce)
    {
        if (!IsParachuteActive || cc == null || !cc.isGrounded)
            return false;

        if (!IsOnBridgeForParachute(transform.position, cc))
            return false;

        velocityY = jumpForce;

        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.01f)
            player?.ApplyHorizontalImpulse(moveDir.normalized * speed * 0.35f);

        return true;
    }

    public float GetEffectiveGravity(float defaultGravity, float verticalVelocity, Vector3 position, bool grounded)
    {
        if (!IsParachuteActive || cc == null)
            return defaultGravity;

        // Keep gliding past other highways/structures until Straight 6 (or timer).
        if (ShouldSuppressGroundWhileGliding)
            return ParachuteGravity;

        if (grounded && verticalVelocity <= 0.5f)
            return defaultGravity;

        return ParachuteGravity;
    }

    public float ClampVerticalVelocity(float verticalVelocity)
    {
        if (!IsParachuteActive || cc == null)
            return verticalVelocity;

        if (ShouldSuppressGroundWhileGliding)
            return Mathf.Max(verticalVelocity, -MaxGlideDownSpeed);

        if (cc.isGrounded && verticalVelocity <= 0.5f)
            return verticalVelocity;

        return Mathf.Max(verticalVelocity, -MaxGlideDownSpeed);
    }

    static bool IsOnBridgeForParachute(Vector3 position, CharacterController controller) =>
        DutzRoadGround.IsNearBridgeStructure(position, controller);
}

/// <summary>Collects scene-placed parachute pickups via CharacterController bounds.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(6)]
public class DutzParachuteCollector : MonoBehaviour
{
    CharacterController cc;
    DutzPlayerController player;
    DutzParachutePickup[] cachedPickups;
    int cacheFrame = -1;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        player = GetComponent<DutzPlayerController>();
    }

    void RefreshPickupCacheIfNeeded()
    {
        // Refresh occasionally — pickups are static scene objects, not spawned every frame.
        if (cachedPickups != null && Time.frameCount - cacheFrame < 30)
            return;

        cachedPickups = Object.FindObjectsOfType<DutzParachutePickup>();
        cacheFrame = Time.frameCount;
    }

    void FixedUpdate()
    {
        if (cc == null || player == null || !DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        RefreshPickupCacheIfNeeded();
        if (cachedPickups == null)
            return;

        for (var i = 0; i < cachedPickups.Length; i++)
        {
            var pickup = cachedPickups[i];
            if (pickup == null || pickup.IsCollected || !pickup.gameObject.activeInHierarchy)
                continue;

            if (!pickup.IsPlayerTouching(cc))
                continue;

            pickup.Collect(player);
            return;
        }
    }
}
