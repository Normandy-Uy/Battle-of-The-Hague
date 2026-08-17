using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persists which levels the player may jump to from the Level 0 start screen.
/// Level 0 is always available; beating level N unlocks level N+1.
/// </summary>
public static class DutzLevelUnlockProgress
{
    const string UnlockedMaskPrefsKey = "dutz_unlocked_levels_mask";

    public const int LevelCount = 4;

    static readonly string[] LevelSceneNames =
    {
        DutzMobileRuntime.Level00SceneName,
        DutzMobileRuntime.Level01SceneName,
        DutzMobileRuntime.Level02SceneName,
        DutzMobileRuntime.Level03SceneName,
    };

    static readonly string[] LevelMenuLabels =
    {
        "LEVEL 2 — EDSA",
        "LEVEL 3 — SENATE",
        "LEVEL 4 — AIRPORT",
        "LEVEL 5 — THE HAGUE",
    };

    static int unlockedMask = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCached() => unlockedMask = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadSaved() => ReloadFromDisk();

    public static void ReloadFromDisk()
    {
        unlockedMask = PlayerPrefs.GetInt(UnlockedMaskPrefsKey, 1);
        unlockedMask |= 1;
    }

    public static bool IsUnlocked(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= LevelCount)
            return false;

        if (levelIndex == 0)
            return true;

        return (unlockedMask & (1 << levelIndex)) != 0;
    }

    public static bool HasJumpOptions()
    {
        for (var i = 1; i < LevelCount; i++)
        {
            if (IsUnlocked(i))
                return true;
        }

        return false;
    }

    public static string GetSceneName(int levelIndex) =>
        levelIndex >= 0 && levelIndex < LevelCount ? LevelSceneNames[levelIndex] : string.Empty;

    public static string GetMenuLabel(int levelIndex) =>
        levelIndex >= 0 && levelIndex < LevelCount ? LevelMenuLabels[levelIndex] : $"LEVEL {levelIndex}";

    public static void Unlock(int levelIndex)
    {
        if (levelIndex <= 0 || levelIndex >= LevelCount)
            return;

        var bit = 1 << levelIndex;
        if ((unlockedMask & bit) != 0)
            return;

        unlockedMask |= bit;
        PlayerPrefs.SetInt(UnlockedMaskPrefsKey, unlockedMask);
        PlayerPrefs.Save();
        Debug.Log($"[Dutz] Unlocked {GetMenuLabel(levelIndex)} for future play.");
    }

    public static void UnlockOnLevelComplete(string sceneName)
    {
        var index = IndexOfScene(sceneName);
        if (index < 0 || index + 1 >= LevelCount)
            return;

        Unlock(index + 1);
    }

    public static void LoadLevel(int levelIndex)
    {
        if (!IsUnlocked(levelIndex))
        {
            Debug.LogWarning($"[Dutz] Level {levelIndex} is locked.");
            return;
        }

        var sceneName = GetSceneName(levelIndex);
        if (string.IsNullOrEmpty(sceneName))
            return;

        DutzGameBootstrap.PrepareForSceneLoad();
        SceneManager.LoadScene(sceneName);
    }

    static int IndexOfScene(string sceneName)
    {
        for (var i = 0; i < LevelSceneNames.Length; i++)
        {
            if (LevelSceneNames[i] == sceneName)
                return i;
        }

        return -1;
    }

#if UNITY_EDITOR
    public static void EditorUnlockAll()
    {
        unlockedMask = (1 << LevelCount) - 1;
        PlayerPrefs.SetInt(UnlockedMaskPrefsKey, unlockedMask);
        PlayerPrefs.Save();
    }

    public static void EditorResetUnlocks()
    {
        unlockedMask = 1;
        PlayerPrefs.SetInt(UnlockedMaskPrefsKey, unlockedMask);
        PlayerPrefs.Save();
    }
#endif
}
