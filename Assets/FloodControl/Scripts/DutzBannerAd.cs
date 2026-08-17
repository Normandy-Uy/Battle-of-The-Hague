using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

/// <summary>
/// Right-edge 300×250 MREC banner. Uses Google's test unit until DutzAdMobIds.BannerAdUnitId is set.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class DutzBannerAd : MonoBehaviour
{
    const float LoadTimeoutSeconds = 12f;

    static DutzBannerAd instance;

    bool sdkInitializing;
    bool sdkReady;
    bool isLoading;
    bool wantsVisible;
    BannerView bannerView;
    int lastScreenWidth;
    int lastScreenHeight;

    public static bool IsEnabled => Application.isMobilePlatform;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    public static DutzBannerAd EnsureInstance()
    {
        if (instance != null)
            return instance;

        var existing = FindObjectOfType<DutzBannerAd>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        var go = new GameObject(nameof(DutzBannerAd));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzBannerAd>();
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
        DestroyBanner();
        DutzLandscapeBannerLayout.SetLayoutReserved(false);
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (!IsEnabled)
        {
            DutzLandscapeBannerLayout.SetLayoutReserved(false);
            return;
        }

        wantsVisible = ShouldShowBanner();
        DutzLandscapeBannerLayout.SetLayoutReserved(wantsVisible);

        if (!wantsVisible)
        {
            bannerView?.Hide();
            return;
        }

        if (!sdkReady || !DutzAdMobConsent.CanRequestAds)
            return;

        if (bannerView == null)
        {
            CreateAndLoadBanner();
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            RepositionBanner();

        bannerView.Show();
    }

    void LateUpdate()
    {
        if (!IsEnabled)
            return;

        var cam = Camera.main;
        if (cam != null)
            DutzLandscapeBannerLayout.ApplyCameraRect(cam);
    }

    static bool ShouldShowBanner()
    {
        if (!IsEnabled || !DutzAdMobConsent.IsGatherComplete)
            return false;

        if (!DutzAdMobConsent.CanRequestAds)
            return false;

        if (DutzLevelStartGate.IsBlockingStart)
            return false;

        if (FloodRewardedAdStub.IsShowing || DutzAdMobConsent.IsShowingPrivacyForm)
            return false;

        if (DutzGamePause.IsPaused)
            return false;

        if (DutzPoliceCaptureDialog.IsShowing)
            return false;

        if (FloodPlayerHealth.IsShowingAnyDeathDialog)
            return false;

        if (DutzFallRespawn.IsShowingAnyDeathDialog)
            return false;

        if (DutzVictoryVideoPlayback.ShouldHideWinGui)
            return false;

        return true;
    }

    void EnsureSdkInitialized()
    {
        if (sdkReady || sdkInitializing || !IsEnabled)
            return;

        sdkInitializing = true;
        DutzAdMobConsent.WhenReady(() =>
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"[DutzAdMob] Initializing banner SDK path (App ID {DutzAdMobIds.AndroidAppId})…");
            MobileAds.Initialize(initStatus =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    sdkInitializing = false;
                    sdkReady = initStatus != null;
                    if (!sdkReady)
                        Debug.LogError("[DutzAdMob] MobileAds.Initialize failed for banner (null status).");
                    else if (wantsVisible && DutzAdMobConsent.CanRequestAds)
                        CreateAndLoadBanner();
                });
            });
#else
            sdkInitializing = false;
            sdkReady = false;
#endif
        });
    }

    void CreateAndLoadBanner()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!sdkReady || !DutzAdMobConsent.CanRequestAds || isLoading)
            return;

        DestroyBanner();
        isLoading = true;

        var unitId = DutzAdMobIds.ActiveBannerAdUnitId;
        var x = DutzLandscapeBannerLayout.GetBannerPositionX();
        var y = DutzLandscapeBannerLayout.GetBannerPositionY();
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Debug.Log($"[DutzAdMob] Loading MREC banner {unitId} at ({x},{y}).");
        bannerView = new BannerView(unitId, AdSize.MediumRectangle, x, y);
        bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoading = false;
                Debug.Log("[DutzAdMob] MREC banner loaded.");
                if (wantsVisible)
                    bannerView?.Show();
                else
                    bannerView?.Hide();
            });
        };
        bannerView.OnBannerAdLoadFailed += error =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoading = false;
                Debug.LogError($"[DutzAdMob] MREC banner load failed: {error}");
            });
        };

        bannerView.LoadAd(new AdRequest());
        StartCoroutine(LoadTimeoutRoutine());
#endif
    }

    IEnumerator LoadTimeoutRoutine()
    {
        float elapsed = 0f;
        while (isLoading && elapsed < LoadTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isLoading)
        {
            isLoading = false;
            Debug.LogWarning("[DutzAdMob] MREC banner load timed out.");
        }
    }

    void RepositionBanner()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bannerView == null)
            return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        bannerView.SetPosition(DutzLandscapeBannerLayout.GetBannerPositionX(), DutzLandscapeBannerLayout.GetBannerPositionY());
#endif
    }

    void DestroyBanner()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (bannerView == null)
            return;

        bannerView.Destroy();
        bannerView = null;
#endif
        isLoading = false;
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!wantsVisible || !Application.isMobilePlatform)
            return;

        var rect = DutzLandscapeBannerLayout.GetDebugBannerGuiRect();
        var prev = GUI.color;
        GUI.color = new Color(0.15f, 0.55f, 0.95f, 0.35f);
        GUI.Box(rect, "MREC 300×250\n(test on Android device)");
        GUI.color = prev;
    }
#endif
}
