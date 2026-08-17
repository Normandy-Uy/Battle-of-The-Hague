/// <summary>
/// Central AdMob IDs for Battle of the Hague (Flood Control Android app).
/// </summary>
public static class DutzAdMobIds
{
    /// <summary>AdMob Android App ID (GoogleMobileAds Settings + AndroidManifest).</summary>
    public const string AndroidAppId = "ca-app-pub-6454550375142005~3271014798";

    /// <summary>Production rewarded ad unit (Restart Level after death).</summary>
    public const string RewardedAdUnitId = "ca-app-pub-6454550375142005/3075910381";

    /// <summary>Google sample rewarded unit — safe for Editor / early fill testing.</summary>
    public const string GoogleTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

#if UNITY_EDITOR
    /// <summary>Editor Play uses Google's test unit so local Restart never depends on live fill.</summary>
    public static string ActiveRewardedAdUnitId => GoogleTestRewardedAdUnitId;
#else
    public static string ActiveRewardedAdUnitId => RewardedAdUnitId;
#endif
}
