using UnityEditor;

/// <summary>
/// After script reload, spawns missing small hippies in Dutz_Level02 when count is below target.
/// </summary>
[InitializeOnLoad]
static class DutzHippieCountAutoApply
{
    static DutzHippieCountAutoApply()
    {
        EditorApplication.delayCall += TryApplyOnce;
    }

    static void TryApplyOnce()
    {
        EditorApplication.delayCall -= TryApplyOnce;

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        if (SimpleCitizensHippieNpcSetup.TryApplySegmentHippiePoolToShowcase())
            return;

        SimpleCitizensHippieNpcSetup.TryApplySmallHippieCountToShowcase();
        SimpleCitizensHippieNpcSetup.TryApplyFlyingHippiesToShowcase();
    }
}
