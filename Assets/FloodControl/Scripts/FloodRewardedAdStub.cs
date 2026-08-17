using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

/// <summary>
/// Rewarded-ad gate for Restart Level (Flood + campaign death dialogs).
/// Keeps the historical FloodRewardedAdStub.Show API used across call sites.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodRewardedAdStub : MonoBehaviour
{
    const float EditorFallbackSeconds = 0.75f;
    const float LoadTimeoutSeconds = 12f;
    const float ShowFlowTimeoutSeconds = 45f;

    static FloodRewardedAdStub instance;

    Action pendingReward;
    Action pendingDismiss;
    bool showing;
    bool earnedReward;
    bool sdkInitializing;
    bool sdkReady;
    bool isLoading;
    RewardedAd rewardedAd;

    public static bool IsShowing => instance != null && instance.showing;

    public static void Show(Action onRewarded, Action onDismissedOrFailed = null)
    {
        FloodRewardedAdStub host = EnsureInstance();
        host.BeginShow(onRewarded, onDismissedOrFailed);
    }

    static FloodRewardedAdStub EnsureInstance()
    {
        if (instance != null)
            return instance;

        FloodRewardedAdStub existing = FindObjectOfType<FloodRewardedAdStub>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject go = new GameObject(nameof(FloodRewardedAdStub));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FloodRewardedAdStub>();
        return instance;
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
        EnsureSdkInitialized();
    }

    void OnDestroy()
    {
        DestroyLoadedAd();
        if (instance == this)
            instance = null;
    }

    void BeginShow(Action onRewarded, Action onDismissedOrFailed)
    {
        if (showing)
        {
            onDismissedOrFailed?.Invoke();
            return;
        }

        pendingReward = onRewarded;
        pendingDismiss = onDismissedOrFailed;
        showing = true;
        earnedReward = false;
        EnsureSdkInitialized();
        StartCoroutine(ShowRewardedRoutine());
    }

    void EnsureSdkInitialized()
    {
        if (sdkReady || sdkInitializing)
            return;

        sdkInitializing = true;
        DutzAdMobConsent.WhenReady(() =>
        {
            Debug.Log($"[DutzAdMob] Initializing Google Mobile Ads (App ID {DutzAdMobIds.AndroidAppId})…");
            MobileAds.Initialize(initStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    sdkInitializing = false;
                    if (initStatus == null)
                    {
                        Debug.LogError("[DutzAdMob] MobileAds.Initialize failed (null status).");
                        sdkReady = false;
                        return;
                    }

                    sdkReady = true;
                    Debug.Log("[DutzAdMob] MobileAds initialized.");
                    if (DutzAdMobConsent.CanRequestAds)
                        LoadRewardedAd();
                    else
                        Debug.LogWarning("[DutzAdMob] Consent does not allow ads yet — rewarded restart unavailable.");
                });
            });
        });
    }

    IEnumerator ShowRewardedRoutine()
    {
        float flowStarted = Time.unscaledTime;

        float waitSdk = 0f;
        while (!sdkReady && waitSdk < LoadTimeoutSeconds)
        {
            if (Time.unscaledTime - flowStarted >= ShowFlowTimeoutSeconds)
                break;

            waitSdk += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!sdkReady)
        {
            Debug.LogWarning("[DutzAdMob] SDK not ready — aborting rewarded restart.");
            yield return FallbackGrantReward("sdk-not-ready");
            yield break;
        }

        if (!DutzAdMobConsent.CanRequestAds)
        {
            Debug.LogWarning("[DutzAdMob] Ads blocked by consent — open Privacy and cookie settings.");
            DutzAdMobConsent.ShowPrivacyOptions();
            yield return FallbackGrantReward("consent-blocked");
            yield break;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            LoadRewardedAd();
            float waitLoad = 0f;
            while (isLoading && waitLoad < LoadTimeoutSeconds)
            {
                if (Time.unscaledTime - flowStarted >= ShowFlowTimeoutSeconds)
                    break;

                waitLoad += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log($"[DutzAdMob] Showing rewarded ad ({DutzAdMobIds.ActiveRewardedAdUnitId}).");
            var ad = rewardedAd;
            ad.Show(reward =>
            {
                earnedReward = true;
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log($"[DutzAdMob] Reward earned: {reward.Type} x {reward.Amount}");
                });
            });

            float waitShow = 0f;
            while (showing && waitShow < ShowFlowTimeoutSeconds)
            {
                waitShow += Time.unscaledDeltaTime;
                yield return null;
            }

            if (showing)
            {
                Debug.LogWarning("[DutzAdMob] Rewarded ad close timed out — restoring Restart dialog.");
                CompleteShow(grantReward: earnedReward);
                DestroyLoadedAd();
                if (DutzAdMobConsent.CanRequestAds)
                    LoadRewardedAd();
            }

            yield break;
        }

        Debug.LogWarning("[DutzAdMob] Rewarded ad not ready — aborting rewarded restart.");
        yield return FallbackGrantReward("ad-not-ready");
    }

    IEnumerator FallbackGrantReward(string reason)
    {
#if UNITY_EDITOR
        Debug.Log($"[DutzAdMob] Editor fallback grant ({reason}).");
        yield return new WaitForSecondsRealtime(EditorFallbackSeconds);
        CompleteShow(grantReward: true);
#else
        Debug.LogWarning($"[DutzAdMob] No ad available ({reason}); dismissing Restart.");
        yield return null;
        CompleteShow(grantReward: false);
#endif
    }

    void LoadRewardedAd()
    {
        if (isLoading || !sdkReady || !DutzAdMobConsent.CanRequestAds)
            return;

        DestroyLoadedAd();
        isLoading = true;
        var unitId = DutzAdMobIds.ActiveRewardedAdUnitId;
        Debug.Log($"[DutzAdMob] Loading rewarded ad: {unitId}");

        RewardedAd.Load(unitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoading = false;
                if (error != null)
                {
                    Debug.LogError($"[DutzAdMob] Rewarded load failed: {error}");
                    rewardedAd = null;
                    return;
                }

                if (ad == null)
                {
                    Debug.LogError("[DutzAdMob] Rewarded load returned null ad.");
                    rewardedAd = null;
                    return;
                }

                rewardedAd = ad;
                RegisterHandlers(ad);
                Debug.Log("[DutzAdMob] Rewarded ad loaded.");
            });
        });
    }

    void RegisterHandlers(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                CompleteShow(grantReward: earnedReward);
                DestroyLoadedAd();
                if (DutzAdMobConsent.CanRequestAds)
                    LoadRewardedAd();
            });
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogError($"[DutzAdMob] Full-screen failed: {error}");
                CompleteShow(grantReward: false);
                DestroyLoadedAd();
                if (DutzAdMobConsent.CanRequestAds)
                    LoadRewardedAd();
            });
        };
    }

    void CompleteShow(bool grantReward)
    {
        if (!showing)
            return;

        Action reward = pendingReward;
        Action dismiss = pendingDismiss;
        pendingReward = null;
        pendingDismiss = null;
        showing = false;
        earnedReward = false;

        if (grantReward)
        {
            if (reward != null && instance != null)
                instance.StartCoroutine(InvokeActionNextFrame(reward));
            else
                reward?.Invoke();
        }
        else
            dismiss?.Invoke();
    }

    static IEnumerator InvokeActionNextFrame(Action action)
    {
        yield return null;
        action?.Invoke();
    }

    void DestroyLoadedAd()
    {
        if (rewardedAd == null)
            return;

        rewardedAd.Destroy();
        rewardedAd = null;
    }

    /// <summary>Cancel an in-flight Restart ad and restore the death dialog.</summary>
    public void CancelPending()
    {
        if (!showing)
            return;

        StopAllCoroutines();
        CompleteShow(grantReward: false);
    }
}
