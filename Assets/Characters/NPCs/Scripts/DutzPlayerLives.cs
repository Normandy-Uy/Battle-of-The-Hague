using UnityEngine;

/// <summary>
/// Shared 3-life counter for campaign levels (L00–L03, L07).
/// Flood Control keeps its own lives on FloodPlayerHealth.
/// </summary>
public static class DutzPlayerLives
{
    public const int MaxLives = 3;

    static int current = MaxLives;
    static bool preserveAcrossNextLoad;

    public static int Current => current;
    public static int Max => MaxLives;
    public static bool CanRespawn => current > 0;
    public static bool MustRestart => current <= 0;

    public static void ResetToFull()
    {
        current = MaxLives;
        preserveAcrossNextLoad = false;
    }

    /// <summary>
    /// Called from DutzGameBootstrap.PrepareForSceneLoad.
    /// Full Restart clears lives; mid-run L07 fail reload can preserve remaining lives.
    /// </summary>
    public static void PrepareForSceneLoad()
    {
        if (preserveAcrossNextLoad)
        {
            preserveAcrossNextLoad = false;
            return;
        }

        current = MaxLives;
    }

    public static void MarkPreserveAcrossNextLoad() => preserveAcrossNextLoad = true;

    /// <summary>Consumes one life when a death/capture/fail dialog begins. Returns remaining lives.</summary>
    public static int ConsumeOne()
    {
        current = Mathf.Max(0, current - 1);
        return current;
    }
}
