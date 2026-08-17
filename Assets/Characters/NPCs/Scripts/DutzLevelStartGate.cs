/// <summary>True while Level 0 pre-game menus are still blocking gameplay.</summary>
public static class DutzLevelStartGate
{
    public static bool IsBlockingStart =>
        DutzLevelSelectHud.IsBlockingStart
        || DutzVictorySelfieSetupHud.IsBlockingStart
        || DutzLevel00WelcomeSplash.IsBlockingStart
        || FloodDifficultySelect.IsBlockingStart;
}
