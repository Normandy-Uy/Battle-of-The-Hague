using UnityEngine;

/// <summary>
/// Reserves a 300×250 dp strip on the right for AdMob MREC in forced landscape.
/// Shifts mobile HUD into the playable width and letterboxes the game camera.
/// </summary>
public static class DutzLandscapeBannerLayout
{
    public const float BannerWidthDp = 300f;
    public const float BannerHeightDp = 250f;

    static bool layoutReserved;

    public static bool ReservesRightStrip => layoutReserved;

    public static float Density => Screen.dpi > 1f ? Screen.dpi / 160f : 1f;

    public static int BannerWidthPixels => Mathf.RoundToInt(BannerWidthDp * Density);

    public static int BannerHeightPixels => Mathf.RoundToInt(BannerHeightDp * Density);

    public static float PlayableWidthPixels =>
        layoutReserved ? Mathf.Max(1f, Screen.width - BannerWidthPixels) : Screen.width;

    public static void SetLayoutReserved(bool reserved) => layoutReserved = reserved;

    public static int GetBannerPositionX() => Screen.width - BannerWidthPixels;

    public static int GetBannerPositionY() =>
        Mathf.Max(0, (Screen.height - BannerHeightPixels) / 2);

    public static void ApplyCameraRect(Camera camera)
    {
        if (camera == null)
            return;

        if (!layoutReserved)
        {
            camera.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        var widthFraction = PlayableWidthPixels / Screen.width;
        camera.rect = new Rect(0f, 0f, widthFraction, 1f);
    }

#if UNITY_EDITOR
    public static Rect GetDebugBannerGuiRect()
    {
        var x = GetBannerPositionX();
        var y = GetBannerPositionY();
        return new Rect(x, y, BannerWidthPixels, BannerHeightPixels);
    }
#endif
}
