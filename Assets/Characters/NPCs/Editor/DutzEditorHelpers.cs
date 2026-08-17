using UnityEditor;
using UnityEngine;

public static class DutzEditorHelpers
{
    public static DutzPlayerController FindPrimaryDutzPlayer()
    {
        DutzPlayerController fallback = null;

        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
                return player;

            if (fallback == null)
                fallback = player;
        }

        return fallback;
    }

    public static GameObject FindPrimaryDutzObject()
    {
        return FindPrimaryDutzPlayer() != null ? FindPrimaryDutzPlayer().gameObject : null;
    }
}
