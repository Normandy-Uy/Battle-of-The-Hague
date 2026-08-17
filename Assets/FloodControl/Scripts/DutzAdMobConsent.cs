using System;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

/// <summary>
/// UMP consent at startup + AdMob-required "Privacy and cookie settings" revocation entry.
/// Never blocks gameplay — ads code waits with a timeout, then proceeds.
/// </summary>
[DisallowMultipleComponent]
public sealed class DutzAdMobConsent : MonoBehaviour
{
    const string PrivacyOptionsLabel = "Privacy and cookie settings";
    const float GatherTimeoutSeconds = 12f;

    static DutzAdMobConsent instance;

    bool gathering;
    bool gatherComplete;
    bool showingPrivacyForm;
    float gatherStartedAt;
    Action<string> pendingGatherCallbacks;

    public static bool IsGatherComplete => instance == null || instance.gatherComplete;

    public static bool CanRequestAds => ConsentInformation.CanRequestAds();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        EnsureInstance();
    }

    public static DutzAdMobConsent EnsureInstance()
    {
        if (instance != null)
            return instance;

        var existing = FindObjectOfType<DutzAdMobConsent>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        var go = new GameObject(nameof(DutzAdMobConsent));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzAdMobConsent>();
        return instance;
    }

    /// <summary>Runs once at startup; safe to call again — waits are capped.</summary>
    public static void WhenReady(Action onReady)
    {
        var host = EnsureInstance();
        host.EnqueueReady(onReady);
    }

    public static void ShowPrivacyOptions()
    {
        EnsureInstance().BeginShowPrivacyOptions();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BeginGather();
    }

    void Update()
    {
        if (!gathering || gatherComplete)
            return;

        if (Time.unscaledTime - gatherStartedAt < GatherTimeoutSeconds)
            return;

        Debug.LogWarning("[DutzAdMob] Consent gather timed out — continuing without blocking ads.");
        MarkGatherComplete(null);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void EnqueueReady(Action onReady)
    {
        if (onReady == null)
            return;

        if (gatherComplete)
        {
            onReady();
            return;
        }

        pendingGatherCallbacks += _ => onReady();
        if (!gathering)
            BeginGather();
    }

    void BeginGather()
    {
        if (gatherComplete || gathering)
            return;

        gathering = true;
        gatherStartedAt = Time.unscaledTime;

        var requestParameters = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false
        };

        ConsentInformation.Update(requestParameters, updateError =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (gatherComplete)
                    return;

                if (updateError != null)
                {
                    Debug.LogWarning($"[DutzAdMob] Consent update failed: {updateError.Message}");
                    MarkGatherComplete(updateError.Message);
                    return;
                }

                if (ConsentInformation.CanRequestAds())
                {
                    Debug.Log("[DutzAdMob] Consent already gathered or not required.");
                    MarkGatherComplete(null);
                    return;
                }

                ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        if (gatherComplete)
                            return;

                        if (showError != null)
                            Debug.LogWarning($"[DutzAdMob] Consent form failed: {showError.Message}");
                        else
                            Debug.Log("[DutzAdMob] Consent form completed.");

                        MarkGatherComplete(showError?.Message);
                    });
                });
            });
        });
    }

    void MarkGatherComplete(string error)
    {
        if (gatherComplete)
            return;

        gathering = false;
        gatherComplete = true;

        var cbs = pendingGatherCallbacks;
        pendingGatherCallbacks = null;
        cbs?.Invoke(error);
    }

    void BeginShowPrivacyOptions()
    {
        if (showingPrivacyForm)
            return;

        showingPrivacyForm = true;
        Debug.Log("[DutzAdMob] Showing Privacy and cookie settings form…");
        ConsentForm.ShowPrivacyOptionsForm(showError =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                showingPrivacyForm = false;
                if (showError != null)
                    Debug.LogWarning($"[DutzAdMob] Privacy options failed: {showError.Message}");
            });
        });
    }

    void OnGUI()
    {
        if (showingPrivacyForm || FloodRewardedAdStub.IsShowing)
            return;

        float pad = Mathf.Max(8f, Screen.height * 0.01f);
        float h = Mathf.Clamp(Screen.height * 0.035f, 28f, 44f);
        float w = Mathf.Min(Screen.width * 0.55f, 320f);
        var rect = new Rect(pad, Screen.height - h - pad, w, h);

        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.018f, 12f, 18f)),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        if (GUI.Button(rect, PrivacyOptionsLabel, style))
            BeginShowPrivacyOptions();
    }
}
