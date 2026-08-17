using System.IO;
using UnityEditor;

/// <summary>
/// Builds Dutz automatically once when the prefab is missing (e.g. after importing scripts).
/// </summary>
[InitializeOnLoad]
public static class DutzAutoBuild
{
    const string PrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";

    static DutzAutoBuild()
    {
        EditorApplication.delayCall += TryBuild;
    }

    static void TryBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(PrefabPath)) return;
        if (!File.Exists("Assets/Characters/NPCs/Editor/DutzCharacterBuilder.cs")) return;

        DutzCharacterBuilder.CreateDutz();
    }
}
