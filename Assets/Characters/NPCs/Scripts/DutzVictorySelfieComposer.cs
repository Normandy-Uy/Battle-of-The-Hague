using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Composites the registration photo into public/DUTZSELFIE.png photoholder for Level 3 share.
/// </summary>
public static class DutzVictorySelfieComposer
{
    public const string TemplateFileName = "DUTZSELFIE.png";

    const int TemplateWidth = 2126;
    const int TemplateHeight = 2016;

    // Fallback bust slot for the current DUTZSELFIE.png (PNG top-left coords).
    const int PhotoSlotX = 620;
    const int PhotoSlotYTop = 1160;
    const int PhotoSlotWidth = 480;
    const int PhotoSlotHeight = 400;

    static Color32 GetPixelTopDown(Texture2D texture, int x, int yTop)
    {
        return texture.GetPixel(x, texture.height - 1 - yTop);
    }

    public static Texture2D EnsureReadable(Texture2D source)
    {
        if (source == null)
            return null;

        if (source.isReadable)
            return source;

        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    public static IEnumerator ComposeAndSaveAsync(Texture2D userPhoto, Action<string> onSavedPath)
    {
        byte[] templateBytes = null;
        yield return LoadTemplateBytes(bytes => templateBytes = bytes);

        if (templateBytes == null || templateBytes.Length == 0)
        {
            Debug.LogWarning("[Dutz] DUTZSELFIE.png template missing from StreamingAssets.");
            onSavedPath?.Invoke(null);
            yield break;
        }

        yield return null;

        var path = ComposeFromTemplateBytes(templateBytes, userPhoto);
        onSavedPath?.Invoke(path);
    }

    public static string ComposeAndSaveSync(Texture2D userPhoto)
    {
        var templateBytes = LoadTemplateBytesSync();
        if (templateBytes == null || templateBytes.Length == 0)
        {
            Debug.LogWarning("[Dutz] DUTZSELFIE.png template missing from StreamingAssets.");
            return null;
        }

        return ComposeFromTemplateBytes(templateBytes, userPhoto);
    }

    static string ComposeFromTemplateBytes(byte[] templateBytes, Texture2D userPhoto)
    {
        var template = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!template.LoadImage(templateBytes))
        {
            UnityEngine.Object.Destroy(template);
            return null;
        }

        var output = CompositeUserPhotoIntoHolder(template, userPhoto);
        UnityEngine.Object.Destroy(template);

        if (output == null)
            return null;

        var fileName = $"dutz_victory_share_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        var savedPath = Path.Combine(Application.persistentDataPath, fileName);
        File.WriteAllBytes(savedPath, output.EncodeToPNG());
        UnityEngine.Object.Destroy(output);
        return savedPath;
    }

    static Texture2D CompositeUserPhotoIntoHolder(Texture2D template, Texture2D userPhoto)
    {
        var width = template.width;
        var height = template.height;

        if (!TryDetectPhotoSlotFromTemplate(template, width, height, out var slotX, out var slotYTop, out var slotW, out var slotH))
            ComputePhotoSlotRect(width, height, out slotX, out slotYTop, out slotW, out slotH);

        var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        output.SetPixels32(template.GetPixels32());

        if (userPhoto != null)
        {
            var readablePhoto = EnsureReadable(userPhoto);
            if (readablePhoto != null)
            {
                BlitCoverWithSetPixel(readablePhoto, output, slotX, slotYTop, slotW, slotH);
                if (readablePhoto != userPhoto)
                    UnityEngine.Object.Destroy(readablePhoto);
            }
        }

        output.Apply();
        return output;
    }

    static bool TryDetectPhotoSlotFromTemplate(
        Texture2D template,
        int width,
        int height,
        out int slotX,
        out int slotYTop,
        out int slotW,
        out int slotH)
    {
        slotX = slotYTop = slotW = slotH = 0;

        // New template: white bust silhouette sits mid-lower, inside a black frame.
        // Avoid stadium lights (upper/left) that used to inflate the white AABB.
        var yScanStart = Mathf.FloorToInt(height * 0.52f);
        var yScanEnd = Mathf.FloorToInt(height * 0.80f);
        var xScanStart = Mathf.FloorToInt(width * 0.26f);
        var xScanEnd = Mathf.FloorToInt(width * 0.56f);

        var minX = width;
        var maxX = -1;
        var minYTop = int.MaxValue;
        var maxYTop = -1;

        for (var yTop = yScanStart; yTop <= yScanEnd; yTop++)
        {
            for (var x = xScanStart; x < xScanEnd; x++)
            {
                var c = GetPixelTopDown(template, x, yTop);
                if (c.r < 245 || c.g < 245 || c.b < 245 || c.a < 200)
                    continue;

                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minYTop = Mathf.Min(minYTop, yTop);
                maxYTop = Mathf.Max(maxYTop, yTop);
            }
        }

        if (maxX < 0 || minYTop == int.MaxValue)
            return false;

        const int inset = 10;
        slotX = minX + inset;
        slotYTop = minYTop + inset;
        slotW = Mathf.Max(1, maxX - minX + 1 - inset * 2);
        slotH = Mathf.Max(1, maxYTop - minYTop + 1 - inset * 2);

        slotX = Mathf.Clamp(slotX, 0, Mathf.Max(0, width - 1));
        slotYTop = Mathf.Clamp(slotYTop, 0, Mathf.Max(0, height - 1));
        slotW = Mathf.Clamp(slotW, 1, width - slotX);
        slotH = Mathf.Clamp(slotH, 1, height - slotYTop);
        return slotW >= 80 && slotH >= 80;
    }

    static void BlitCoverWithSetPixel(
        Texture2D source,
        Texture2D dest,
        int slotX,
        int slotYTop,
        int slotW,
        int slotH)
    {
        var srcW = source.width;
        var srcH = source.height;
        var scale = Mathf.Max(slotW / (float)srcW, slotH / (float)srcH);
        var cropLeft = (srcW * scale - slotW) * 0.5f;
        var cropTop = (srcH * scale - slotH) * 0.5f;

        for (var dy = 0; dy < slotH; dy++)
        {
            var destYTop = slotYTop + dy;
            var destY = dest.height - 1 - destYTop;

            for (var dx = 0; dx < slotW; dx++)
            {
                var srcX = Mathf.Clamp(Mathf.FloorToInt((dx + cropLeft) / scale), 0, srcW - 1);
                var srcYTop = Mathf.Clamp(Mathf.FloorToInt((dy + cropTop) / scale), 0, srcH - 1);
                var srcY = srcH - 1 - srcYTop;

                var color = source.GetPixel(srcX, srcY);
                color.a = 255;
                dest.SetPixel(slotX + dx, destY, color);
            }
        }
    }

    static void ComputePhotoSlotRect(int width, int height, out int slotX, out int slotYTop, out int slotW, out int slotH)
    {
        var scaleX = width / (float)TemplateWidth;
        var scaleY = height / (float)TemplateHeight;

        slotX = Mathf.RoundToInt(PhotoSlotX * scaleX);
        slotYTop = Mathf.RoundToInt(PhotoSlotYTop * scaleY);
        slotW = Mathf.Max(1, Mathf.RoundToInt(PhotoSlotWidth * scaleX));
        slotH = Mathf.Max(1, Mathf.RoundToInt(PhotoSlotHeight * scaleY));

        slotX = Mathf.Clamp(slotX, 0, Mathf.Max(0, width - slotW));
        slotYTop = Mathf.Clamp(slotYTop, 0, Mathf.Max(0, height - slotH));
        slotW = Mathf.Clamp(slotW, 1, width - slotX);
        slotH = Mathf.Clamp(slotH, 1, height - slotYTop);
    }

    static IEnumerator LoadTemplateBytes(Action<byte[]> onLoaded)
    {
        var path = Path.Combine(Application.streamingAssetsPath, TemplateFileName);
#if UNITY_ANDROID && !UNITY_EDITOR
        using var request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[Dutz] Could not load DUTZSELFIE.png: " + request.error);
            onLoaded?.Invoke(null);
            yield break;
        }

        onLoaded?.Invoke(request.downloadHandler.data);
#else
        onLoaded?.Invoke(File.Exists(path) ? File.ReadAllBytes(path) : TryLoadFromPublicFolder());
        yield break;
#endif
    }

    static byte[] LoadTemplateBytesSync()
    {
        var path = Path.Combine(Application.streamingAssetsPath, TemplateFileName);
        if (File.Exists(path))
            return File.ReadAllBytes(path);

        return TryLoadFromPublicFolder();
    }

    static byte[] TryLoadFromPublicFolder()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
            return null;

        var publicPath = Path.Combine(projectRoot, "public", TemplateFileName);
        return File.Exists(publicPath) ? File.ReadAllBytes(publicPath) : null;
    }
}
