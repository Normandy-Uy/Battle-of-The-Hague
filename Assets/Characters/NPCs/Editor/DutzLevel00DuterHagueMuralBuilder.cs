using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Syncs the DUTERTEHAGUE photo from public/DUTERTE MURALS as a white-BG cut-out.
/// </summary>
public static class DutzLevel00DuterHagueMuralBuilder
{
    const string TexturesFolder = "Assets/Characters/HighwayBillboards/Textures/Level00Timeline";
    const string DestAssetPath = TexturesFolder + "/Timeline_DUTERTEHAGUE.png";

    static readonly string[] PublicFileNames =
    {
        "DUTERTEHAGUE.png",
        "DUTERHAGUE.png",
    };

    /// <summary>Batch: -executeMethod DutzLevel00DuterHagueMuralBuilder.SyncPhotoBatch</summary>
    public static void SyncPhotoBatch() => SyncPhoto(log: true, force: true);

    public static bool SyncPhoto(bool log) => SyncPhoto(log, force: false);

    public static bool SyncPhoto(bool log, bool force)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;

        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards"))
            AssetDatabase.CreateFolder("Assets/Characters", "HighwayBillboards");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards", "Textures");
        if (!AssetDatabase.IsValidFolder(TexturesFolder))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards/Textures", "Level00Timeline");

        var sourcePath = FindSource(projectRoot);
        if (string.IsNullOrEmpty(sourcePath))
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] Missing DUTERTEHAGUE photo. Add DUTERTEHAGUE.png under public/DUTERTE MURALS/.");
            }

            return false;
        }

        if (!DutzPhotoCutout.SyncCutoutPhoto(sourcePath, DestAssetPath, forceRewrite: force))
            return false;

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        AssetDatabase.Refresh();

        if (log)
            Debug.Log("[Dutz] Synced DUTERTEHAGUE cut-out mural → " + DestAssetPath);

        return true;
    }

    public static Texture2D LoadSyncedTexture() =>
        AssetDatabase.LoadAssetAtPath<Texture2D>(DestAssetPath);

    static string FindSource(string projectRoot)
    {
        foreach (var fileName in PublicFileNames)
        {
            var path = DutzPhotoCutout.FindPublicPhoto(projectRoot, fileName);
            if (!string.IsNullOrEmpty(path))
                return path;
        }

        return null;
    }
}
