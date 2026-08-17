using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Syncs the seven Level 00 timeline photos (DUTZ2016–DUTERTE2022).
/// Duter portraits prefer public/DUTERTE MURALS and are synced as white-BG cut-outs.
/// </summary>
public static class DutzLevel00TimelineMuralBuilder
{
    const string TexturesFolder = "Assets/Characters/HighwayBillboards/Textures/Level00Timeline";

    public static readonly string[] TimelinePhotoFileNames =
    {
        "DUTZ2016.png",
        "DUTZ2017.png",
        "DUTERTE2018.png",
        "DUTERTE2019.png",
        "DUTERTE2020.png",
        "DUTERTE2021.png",
        "DUTERTE2022.png",
    };

    /// <summary>Batch: -executeMethod DutzLevel00TimelineMuralBuilder.SyncPhotosBatch</summary>
    public static void SyncPhotosBatch() => SyncPhotos(log: true, force: true);

    public static int SyncPhotos(bool log) => SyncPhotos(log, force: false);

    public static int SyncPhotos(bool log, bool force)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return 0;

        var publicDir = Path.Combine(projectRoot, "public");
        if (!Directory.Exists(publicDir))
        {
            if (log)
                Debug.LogError("[Dutz] public/ folder not found for Level 00 timeline murals.");
            return 0;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards"))
            AssetDatabase.CreateFolder("Assets/Characters", "HighwayBillboards");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards", "Textures");
        if (!AssetDatabase.IsValidFolder(TexturesFolder))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards/Textures", "Level00Timeline");

        var synced = 0;
        foreach (var fileName in TimelinePhotoFileNames)
        {
            var sourcePath = DutzPhotoCutout.FindPublicPhoto(projectRoot, fileName);
            if (string.IsNullOrEmpty(sourcePath))
            {
                if (log)
                    Debug.LogWarning("[Dutz] Missing Level 00 timeline photo: " + fileName);
                continue;
            }

            var baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var destPath = $"{TexturesFolder}/Timeline_{baseName}.png";
            var isDuterCutout = baseName.StartsWith("DUTERTE", System.StringComparison.OrdinalIgnoreCase);
            var ok = isDuterCutout
                ? DutzPhotoCutout.SyncCutoutPhoto(sourcePath, destPath, forceRewrite: force)
                : DutzHaguePhotoBillboardBuilder.SyncPosterizedPhoto(sourcePath, destPath, forceRewrite: force);
            if (ok)
                synced++;
        }

        if (synced == 0)
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] No Level 00 timeline photos synced. Add DUTZ2016/2017 to public/ and " +
                    "DUTERTE2018–2022 to public/DUTERTE MURALS/.");
            }

            return 0;
        }

        DutzHaguePhotoBillboardBuilder.EnsureMaterial();
        AssetDatabase.Refresh();

        if (log)
            Debug.Log($"[Dutz] Synced {synced} Level 00 timeline mural texture(s) to {TexturesFolder}.");

        return synced;
    }

    public static List<Texture2D> LoadSyncedTextures()
    {
        var textures = new List<Texture2D>(TimelinePhotoFileNames.Length);
        foreach (var fileName in TimelinePhotoFileNames)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var assetPath = $"{TexturesFolder}/Timeline_{baseName}.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
                textures.Add(texture);
        }

        return textures;
    }
}
