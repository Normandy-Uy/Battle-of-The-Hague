using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Syncs Hague photos from public/ (Hague1–Hague8), posterizes them, and writes textures to Assets.
/// Menu: Tools / Dutz / Sync Hague Photos (Low Poly)
/// </summary>
public static class DutzHaguePhotoBillboardBuilder
{
    static readonly string[] PublicFolders = { "public", "public/hague" };
    const string TexturesFolder = "Assets/Characters/HighwayBillboards/Textures";
    const string SettingsPath = "Assets/Characters/HighwayBillboards/DutzHighwayPhotoBillboardSettings.asset";
    const string MaterialPath = "Assets/Characters/HighwayBillboards/Materials/DutzLowPolyPhoto.mat";
    const string ShaderName = "Dutz/LowPolyPhotoBillboard";
    static readonly Regex HagueNumberPattern = new Regex(@"^hague\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static void SyncFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Hague Photos", "Exit Play mode first.", "OK");
            return;
        }

        var count = SyncPhotos(log: true);
        if (count == 0)
        {
            EditorUtility.DisplayDialog(
                "Hague Photos",
                "No textures were created.\n\nDrop Hague1.jpg … Hague8.png into public/ and run again.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Hague Photos",
            $"Synced {count} low-poly Hague texture(s).\n\nRun Place Hague Highway Billboards (Level 03) next.",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzHaguePhotoBillboardBuilder.SyncPhotosBatch</summary>
    public static void SyncPhotosBatch() => SyncPhotos(log: true);

    public static int SyncPhotos(bool log)
    {
        EnsureFolders();
        var settings = EnsureSettings();
        var sources = CollectHagueSourcePhotos();
        var synced = 0;

        if (sources.Count > 0)
            ClearPlaceholderTextures();

        foreach (var sourcePath in sources)
        {
            var baseName = Path.GetFileNameWithoutExtension(sourcePath);
            var destAssetPath = $"{TexturesFolder}/Hague_{SanitizeFileName(baseName)}.png";
            if (SyncPosterizedPhoto(sourcePath, destAssetPath))
                synced++;
        }

        if (synced == 0)
        {
            Debug.LogError(
                "[Dutz] No Hague photos found. Add Hague1.jpg … Hague8.png to public/ or public/hague/.");
            return 0;
        }

        AssetDatabase.Refresh();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(TexturesFolder) || path.Contains("Placeholder"))
                continue;

            ApplyImportSettings(path, settings.maxTextureEdge);
        }

        EnsureMaterial();

        if (log)
            Debug.Log($"[Dutz] Synced {synced} Hague mural texture(s) from public/ to {TexturesFolder}.");

        return synced;
    }

    static List<string> CollectHagueSourcePhotos()
    {
        var matches = new List<(int number, string path)>();
        foreach (var folder in PublicFolders)
        {
            var publicDir = Path.GetFullPath(folder);
            if (!Directory.Exists(publicDir))
                continue;

            foreach (var sourcePath in Directory.GetFiles(publicDir))
            {
                var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
                    continue;

                var baseName = Path.GetFileNameWithoutExtension(sourcePath);
                var match = HagueNumberPattern.Match(baseName.Trim());
                if (!match.Success)
                    continue;

                if (!int.TryParse(match.Groups[1].Value, out var number))
                    continue;

                matches.Add((number, sourcePath));
            }
        }

        matches.Sort((a, b) => a.number.CompareTo(b.number));
        var paths = new List<string>(matches.Count);
        foreach (var entry in matches)
            paths.Add(entry.path);

        return paths;
    }

    static void ClearPlaceholderTextures()
    {
        if (!Directory.Exists(TexturesFolder))
            return;

        foreach (var file in Directory.GetFiles(TexturesFolder, "Hague_Placeholder_*.png"))
            File.Delete(file);

        foreach (var file in Directory.GetFiles(TexturesFolder, "Hague_Placeholder_*.png.meta"))
            File.Delete(file);
    }

    public static List<Texture2D> LoadSyncedTextures()
    {
        var textures = new List<Texture2D>();
        if (!AssetDatabase.IsValidFolder(TexturesFolder))
            return textures;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexturesFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith(TexturesFolder) || path.Contains("Placeholder"))
                continue;

            if (path.EndsWith("DutzJail.png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
                textures.Add(texture);
        }

        textures.Sort((a, b) => ExtractHagueNumber(a.name).CompareTo(ExtractHagueNumber(b.name)));
        return textures;
    }

    static int ExtractHagueNumber(string textureName)
    {
        var match = HagueNumberPattern.Match(textureName.Replace("Hague_", "").Replace("_", ""));
        if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
            return number;

        match = Regex.Match(textureName, @"(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out number) ? number : int.MaxValue;
    }

    public static Material EnsureMaterial()
    {
        EnsureFolders();
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[Dutz] Shader not found: {ShaderName}");
            return null;
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "DutzLowPolyPhoto" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        return material;
    }

    public static DutzHighwayPhotoBillboardSettings EnsureSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<DutzHighwayPhotoBillboardSettings>(SettingsPath);
        if (settings != null)
            return settings;

        EnsureFolders();
        settings = ScriptableObject.CreateInstance<DutzHighwayPhotoBillboardSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        AssetDatabase.SaveAssets();
        return settings;
    }

    public static bool SyncPosterizedPhoto(string sourcePath, string destAssetPath, int? maxEdgeOverride = null, bool forceRewrite = false)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        EnsureFolders();
        var settings = EnsureSettings();
        var maxEdge = maxEdgeOverride ?? settings.maxTextureEdge;
        var destFullPath = Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
            destAssetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!forceRewrite
            && File.Exists(destFullPath)
            && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destFullPath))
        {
            return ApplyImportSettings(destAssetPath, maxEdge);
        }

        if (!WritePosterizedTexture(sourcePath, destFullPath, settings, maxEdge))
            return false;

        AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
        ApplyImportSettings(destAssetPath, maxEdge);
        return true;
    }

    static bool WritePosterizedTexture(
        string sourcePath,
        string destPath,
        DutzHighwayPhotoBillboardSettings settings,
        int maxEdge)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var resized = ResizeToMaxEdge(loaded, maxEdge);
        if (resized != loaded)
            Object.DestroyImmediate(loaded);

        var posterized = PosterizeTexture(resized, settings);
        if (posterized != resized)
            Object.DestroyImmediate(resized);

        WriteTexturePng(posterized, destPath, settings);
        Object.DestroyImmediate(posterized);
        return true;
    }

    static void WriteTexturePng(Texture2D texture, string destPath, DutzHighwayPhotoBillboardSettings settings)
    {
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(destPath, texture.EncodeToPNG());
    }

    static Texture2D ResizeToMaxEdge(Texture2D source, int maxEdge)
    {
        var longest = Mathf.Max(source.width, source.height);
        if (longest <= maxEdge)
            return source;

        var scale = maxEdge / (float)longest;
        var newW = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
        var newH = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
        var output = new Texture2D(newW, newH, TextureFormat.RGBA32, false);

        for (var y = 0; y < newH; y++)
        {
            var v = y / (float)(newH - 1);
            for (var x = 0; x < newW; x++)
            {
                var u = x / (float)(newW - 1);
                output.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }

        output.Apply();
        return output;
    }

    static Texture2D PosterizeTexture(Texture2D source, DutzHighwayPhotoBillboardSettings settings)
    {
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        var levels = Mathf.Max(4, settings.posterizeLevels);

        for (var y = 0; y < source.height; y++)
        for (var x = 0; x < source.width; x++)
        {
            var color = source.GetPixel(x, y);
            color = AdjustSaturation(color, settings.saturationBoost);
            color = ApplyContrast(color, settings.contrastBoost);
            color = PosterizeColor(color, levels);
            output.SetPixel(x, y, color);
        }

        output.Apply();
        return output;
    }

    static Color PosterizeColor(Color color, int levels)
    {
        var step = 1f / Mathf.Max(1, levels - 1);
        return new Color(
            Mathf.Round(color.r / step) * step,
            Mathf.Round(color.g / step) * step,
            Mathf.Round(color.b / step) * step,
            color.a);
    }

    static Color AdjustSaturation(Color color, float boost)
    {
        if (boost <= 0.0001f)
            return color;

        Color.RGBToHSV(color, out var h, out var s, out var v);
        s = Mathf.Clamp01(s + boost);
        var adjusted = Color.HSVToRGB(h, s, v);
        adjusted.a = color.a;
        return adjusted;
    }

    static Color ApplyContrast(Color color, float contrast)
    {
        return new Color(
            Mathf.Clamp01((color.r - 0.5f) * contrast + 0.5f),
            Mathf.Clamp01((color.g - 0.5f) * contrast + 0.5f),
            Mathf.Clamp01((color.b - 0.5f) * contrast + 0.5f),
            color.a);
    }

    static bool ApplyImportSettings(string assetPath, int maxEdge)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        var needsReimport = importer.maxTextureSize != maxEdge
            || importer.mipmapEnabled
            || importer.filterMode != FilterMode.Point
            || importer.textureCompression != TextureImporterCompression.Compressed;

        if (!needsReimport)
            return false;

        importer.maxTextureSize = maxEdge;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        return true;
    }

    static void EnsureFolders()
    {
        EnsureAssetFolder("Assets/Characters");
        EnsureAssetFolder("Assets/Characters/HighwayBillboards");
        EnsureAssetFolder("Assets/Characters/HighwayBillboards/Textures");
        EnsureAssetFolder("Assets/Characters/HighwayBillboards/Materials");
        EnsureAssetFolder("Assets/Characters/HighwayBillboards/Shaders");
    }

    static void EnsureAssetFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var leaf = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureAssetFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }
}
