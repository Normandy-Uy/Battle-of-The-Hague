using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Syncs Level 00 EDSA side-wall photos from public/EDSA_murals_level00 (L001–L018, R001–R018).
/// </summary>
public static class DutzLevel00EdsaMuralBuilder
{
    const string PublicFolderName = "EDSA_murals_level00";
    const string TexturesFolder = "Assets/Characters/HighwayBillboards/Textures/Level00Edsa";
    public const int MuralsPerSide = 18;
    public const int PanelsPerSegment = 3;
    public const int SegmentCount = 6;

    static readonly Regex EdsaNumberPattern = new Regex(@"^([LR])(\d{3})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Batch: -executeMethod DutzLevel00EdsaMuralBuilder.SyncPhotosBatch</summary>
    public static void SyncPhotosBatch() => ResyncTextures(log: true, force: true);

    public static int SyncPhotos(bool log) => ResyncTextures(log, force: false);

    public static bool NeedsTextureResync()
    {
        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();
        var targetEdge = Mathf.Max(256, settings.edsaMaxTextureEdge);

        for (var i = 1; i <= MuralsPerSide; i++)
        {
            if (NeedsTextureResyncForSide('L', i, targetEdge) || NeedsTextureResyncForSide('R', i, targetEdge))
                return true;
        }

        return false;
    }

    public static int ResyncTextures(bool log, bool force = false)
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return 0;

        var publicDir = Path.Combine(projectRoot, "public", PublicFolderName);
        if (!Directory.Exists(publicDir))
        {
            if (log)
            {
                Debug.LogError(
                    "[Dutz] public/EDSA_murals_level00/ not found. Add L001–L018 and R001–R018 PNGs.");
            }

            return 0;
        }

        EnsureTextureFolder();
        var sources = CollectEdsaSourcePhotos(publicDir);
        if (sources.Count == 0)
        {
            if (log)
                Debug.LogError("[Dutz] No EDSA mural PNGs found in public/EDSA_murals_level00/.");
            return 0;
        }

        var settings = DutzHaguePhotoBillboardBuilder.EnsureSettings();
        var maxEdge = Mathf.Max(256, settings.edsaMaxTextureEdge);
        var rewrite = force || NeedsTextureResync();

        var synced = 0;
        foreach (var entry in sources)
        {
            var destPath = $"{TexturesFolder}/Edsa_{entry.side}{entry.number:000}.png";
            if (DutzHaguePhotoBillboardBuilder.SyncPosterizedPhoto(entry.path, destPath, maxEdge, rewrite))
                synced++;
        }

        if (synced == 0 && !HasAllSyncedTextures())
        {
            if (log)
                Debug.LogError("[Dutz] No Level 00 EDSA mural textures were synced.");
            return 0;
        }

        if (synced > 0)
        {
            DutzHaguePhotoBillboardBuilder.EnsureMaterial();
            AssetDatabase.Refresh();
        }

        if (log && synced > 0)
        {
            Debug.Log(
                $"[Dutz] Synced {synced} Level 00 EDSA mural texture(s) to {TexturesFolder} at {maxEdge}px max edge.");
        }

        return synced;
    }

    public static Texture2D LoadTexture(char side, int muralNumber)
    {
        var assetPath = $"{TexturesFolder}/Edsa_{char.ToUpperInvariant(side)}{muralNumber:000}.png";
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    public static bool HasAllSyncedTextures()
    {
        for (var i = 1; i <= MuralsPerSide; i++)
        {
            if (LoadTexture('L', i) == null || LoadTexture('R', i) == null)
                return false;
        }

        return true;
    }

    static bool NeedsTextureResyncForSide(char side, int muralNumber, int targetEdge)
    {
        var texture = LoadTexture(side, muralNumber);
        if (texture == null)
            return true;

        return Mathf.Max(texture.width, texture.height) < targetEdge - 4;
    }

    static void EnsureTextureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards"))
            AssetDatabase.CreateFolder("Assets/Characters", "HighwayBillboards");
        if (!AssetDatabase.IsValidFolder("Assets/Characters/HighwayBillboards/Textures"))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards", "Textures");
        if (!AssetDatabase.IsValidFolder(TexturesFolder))
            AssetDatabase.CreateFolder("Assets/Characters/HighwayBillboards/Textures", "Level00Edsa");
    }

    struct EdsaSourceEntry
    {
        public char side;
        public int number;
        public string path;
    }

    static List<EdsaSourceEntry> CollectEdsaSourcePhotos(string publicDir)
    {
        var entries = new List<EdsaSourceEntry>();
        foreach (var sourcePath in Directory.GetFiles(publicDir))
        {
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                continue;

            var baseName = Path.GetFileNameWithoutExtension(sourcePath).Trim();
            var match = EdsaNumberPattern.Match(baseName);
            if (!match.Success)
                continue;

            if (!int.TryParse(match.Groups[2].Value, out var number) || number < 1 || number > MuralsPerSide)
                continue;

            entries.Add(new EdsaSourceEntry
            {
                side = char.ToUpperInvariant(match.Groups[1].Value[0]),
                number = number,
                path = sourcePath,
            });
        }

        entries.Sort((a, b) =>
        {
            var sideCompare = a.side.CompareTo(b.side);
            return sideCompare != 0 ? sideCompare : a.number.CompareTo(b.number);
        });

        return entries;
    }
}
