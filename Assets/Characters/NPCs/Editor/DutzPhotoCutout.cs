using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper — strips near-white studio backgrounds into transparent cut-out PNGs.
/// </summary>
public static class DutzPhotoCutout
{
    const float BackgroundLumaMin = 0.86f;
    const float BackgroundSaturationMax = 0.14f;
    const float AlphaClipKeep = 0.12f;
    const int TrimPaddingPixels = 4;
    const int MaxEdgeDefault = 1024;

    public const string DuterMuralsPublicFolder = "DUTERTE MURALS";

    /// <summary>
    /// Resolve a photo under public/DUTERTE MURALS first, then public/ root (case-insensitive).
    /// </summary>
    public static string FindPublicPhoto(string projectRoot, string fileName)
    {
        if (string.IsNullOrEmpty(projectRoot) || string.IsNullOrEmpty(fileName))
            return null;

        var publicRoot = Path.Combine(projectRoot, "public");
        var preferred = Path.Combine(publicRoot, DuterMuralsPublicFolder);
        var hit = FindInDirectory(preferred, fileName);
        if (!string.IsNullOrEmpty(hit))
            return hit;

        return FindInDirectory(publicRoot, fileName);
    }

    static string FindInDirectory(string directory, string fileName)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return null;

        var exact = Path.Combine(directory, fileName);
        if (File.Exists(exact))
            return exact;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        foreach (var candidate in Directory.GetFiles(directory))
        {
            var name = Path.GetFileName(candidate);
            if (string.Equals(name, fileName, System.StringComparison.OrdinalIgnoreCase))
                return candidate;

            if (string.Equals(
                    Path.GetFileNameWithoutExtension(name),
                    baseName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(name).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Load source, remove near-white background, trim, write PNG with alpha, import for cut-out use.
    /// </summary>
    public static bool SyncCutoutPhoto(
        string sourcePath,
        string destAssetPath,
        int maxEdge = MaxEdgeDefault,
        bool forceRewrite = false)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return false;

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return false;

        var destFullPath = Path.Combine(
            projectRoot, destAssetPath.Replace('/', Path.DirectorySeparatorChar));

        if (!forceRewrite
            && File.Exists(destFullPath)
            && File.GetLastWriteTimeUtc(sourcePath) <= File.GetLastWriteTimeUtc(destFullPath))
        {
            ApplyCutoutImportSettings(destAssetPath, maxEdge);
            return true;
        }

        var bytes = File.ReadAllBytes(sourcePath);
        var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!loaded.LoadImage(bytes))
        {
            Object.DestroyImmediate(loaded);
            return false;
        }

        var working = ResizeToMaxEdge(loaded, maxEdge);
        if (working != loaded)
            Object.DestroyImmediate(loaded);

        var cut = MakeCutout(working);
        if (cut != working)
            Object.DestroyImmediate(working);

        var dir = Path.GetDirectoryName(destFullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(destFullPath, cut.EncodeToPNG());
        Object.DestroyImmediate(cut);

        AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
        ApplyCutoutImportSettings(destAssetPath, maxEdge);
        return true;
    }

    public static void ApplyCutoutImportSettings(string assetPath, int maxEdge = MaxEdgeDefault)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        var dirty = false;
        if (importer.maxTextureSize != maxEdge)
        {
            importer.maxTextureSize = maxEdge;
            dirty = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }

        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            dirty = true;
        }

        if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            dirty = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            dirty = true;
        }

        if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            dirty = true;
        }

        if (dirty)
            importer.SaveAndReimport();
    }

    static Texture2D MakeCutout(Texture2D source)
    {
        var w = source.width;
        var h = source.height;
        var pixels = source.GetPixels32();
        var keep = new bool[pixels.Length];
        for (var i = 0; i < keep.Length; i++)
            keep[i] = true;

        var queue = new Queue<int>(w + h);
        for (var x = 0; x < w; x++)
        {
            TryEnqueueBackground(pixels, keep, queue, x, 0, w, h);
            TryEnqueueBackground(pixels, keep, queue, x, h - 1, w, h);
        }

        for (var y = 0; y < h; y++)
        {
            TryEnqueueBackground(pixels, keep, queue, 0, y, w, h);
            TryEnqueueBackground(pixels, keep, queue, w - 1, y, w, h);
        }

        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var x = i % w;
            var y = i / w;
            TryEnqueueBackground(pixels, keep, queue, x - 1, y, w, h);
            TryEnqueueBackground(pixels, keep, queue, x + 1, y, w, h);
            TryEnqueueBackground(pixels, keep, queue, x, y - 1, w, h);
            TryEnqueueBackground(pixels, keep, queue, x, y + 1, w, h);
        }

        for (var i = 0; i < pixels.Length; i++)
        {
            if (keep[i])
                continue;

            var p = pixels[i];
            p.a = 0;
            pixels[i] = p;
        }

        SoftenCutoutEdge(pixels, keep, w, h);

        var minX = w;
        var minY = h;
        var maxX = -1;
        var maxY = -1;
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            if (pixels[y * w + x].a < 8)
                continue;

            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        if (maxX < minX || maxY < minY)
            return source;

        minX = Mathf.Max(0, minX - TrimPaddingPixels);
        minY = Mathf.Max(0, minY - TrimPaddingPixels);
        maxX = Mathf.Min(w - 1, maxX + TrimPaddingPixels);
        maxY = Mathf.Min(h - 1, maxY + TrimPaddingPixels);

        var outW = maxX - minX + 1;
        var outH = maxY - minY + 1;
        var cropped = new Color32[outW * outH];
        for (var y = 0; y < outH; y++)
        for (var x = 0; x < outW; x++)
            cropped[y * outW + x] = pixels[(minY + y) * w + (minX + x)];

        var output = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
        output.SetPixels32(cropped);
        output.Apply(false, false);
        return output;
    }

    static void TryEnqueueBackground(
        Color32[] pixels,
        bool[] keep,
        Queue<int> queue,
        int x,
        int y,
        int w,
        int h)
    {
        if (x < 0 || y < 0 || x >= w || y >= h)
            return;

        var i = y * w + x;
        if (!keep[i])
            return;

        if (!IsBackground(pixels[i]))
            return;

        keep[i] = false;
        queue.Enqueue(i);
    }

    static bool IsBackground(Color32 c)
    {
        var r = c.r / 255f;
        var g = c.g / 255f;
        var b = c.b / 255f;
        var max = Mathf.Max(r, Mathf.Max(g, b));
        var min = Mathf.Min(r, Mathf.Min(g, b));
        var sat = max - min;
        var luma = 0.2126f * r + 0.7152f * g + 0.0722f * b;
        return luma >= BackgroundLumaMin && sat <= BackgroundSaturationMax;
    }

    static void SoftenCutoutEdge(Color32[] pixels, bool[] keep, int w, int h)
    {
        for (var y = 1; y < h - 1; y++)
        for (var x = 1; x < w - 1; x++)
        {
            var i = y * w + x;
            if (!keep[i] || pixels[i].a < 8)
                continue;

            var border = false;
            for (var oy = -1; oy <= 1 && !border; oy++)
            for (var ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0)
                    continue;

                var n = (y + oy) * w + (x + ox);
                if (!keep[n] || pixels[n].a < 8)
                {
                    border = true;
                    break;
                }
            }

            if (!border)
                continue;

            var p = pixels[i];
            var a = Mathf.Max(AlphaClipKeep, p.a / 255f * 0.85f);
            p.a = (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
            pixels[i] = p;
        }
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
            var v = (newH == 1) ? 0f : y / (float)(newH - 1);
            for (var x = 0; x < newW; x++)
            {
                var u = (newW == 1) ? 0f : x / (float)(newW - 1);
                output.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }

        output.Apply(false, false);
        return output;
    }
}
