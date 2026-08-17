using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Recolors Dutz's Emo atlas: bright blue clothing and blond hair.
/// </summary>
public static class DutzClothingColorBuilder
{
    const string SourceTexturePath = "Assets/SimpleCitizens/Textures/SimpleCitizens_Emo_White.png";
    const string OutputTexturePath = "Assets/Characters/NPCs/Textures/Dutz_Emo_BrightBlue.png";
    const string MaterialPath = "Assets/Characters/NPCs/Materials/Dutz_Emo_BrightBlue.mat";
    const string TemplateMaterialPath = "Assets/SimpleCitizens/Materials/SimpleCitizens_Emo_White.mat";
    const string DutzPrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";
    const string EmoPrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Emo_White.prefab";
    const string EmoOutfit = "SC_Emo";
    const int TileSize = 64;

    static readonly Color BrightBlue = new Color(0.051f, 0.412f, 0.675f, 1f);
    static readonly Color BlondHair = new Color(0.95f, 0.82f, 0.38f, 1f);

    public static void ApplyBrightBlueClothingFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Bright Blue Clothing", "Exit Play mode first.", "OK");
            return;
        }

        if (!BuildBrightBlueClothingAssets())
            return;

        ApplyMaterialToDutzPrefab();
        ApplyMaterialToSceneDutz();
        AssetDatabase.SaveAssets();
        Debug.Log("[Dutz] Dutz clothing is bright blue and hair is blond.");
    }

    /// <summary>Batch: -executeMethod DutzClothingColorBuilder.ApplyBrightBlueClothingBatch</summary>
    public static void ApplyBrightBlueClothingBatch() => ApplyBrightBlueClothingFromMenu();

    public static bool BuildBrightBlueClothingAssets()
    {
        var emoRenderer = GetEmoRendererFromPrefab();
        if (emoRenderer == null)
            return false;

        var source = LoadReadableTexture(SourceTexturePath);
        if (source == null)
            return false;

        var mesh = emoRenderer.sharedMesh;
        var bones = emoRenderer.bones;
        var headTiles = DutzGiantHippieBossFaceBuilder.GetHeadTileOrigins(mesh, bones, 1);
        var headTileSet = headTiles != null ? new HashSet<Vector2Int>(headTiles) : null;
        var output = RecolorDutzLook(source, headTileSet);
        Object.DestroyImmediate(source);

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[Dutz] Could not resolve project root for bright blue clothing texture.");
            Object.DestroyImmediate(output);
            return false;
        }

        Directory.CreateDirectory(Path.Combine(projectRoot, "Assets/Characters/NPCs/Textures"));
        var textureFullPath = Path.Combine(
            projectRoot, OutputTexturePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllBytes(textureFullPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);

        AssetDatabase.ImportAsset(OutputTexturePath, ImportAssetOptions.ForceUpdate);

        var template = AssetDatabase.LoadAssetAtPath<Material>(TemplateMaterialPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(OutputTexturePath);
        if (material == null)
        {
            material = template != null
                ? new Material(template) { name = "Dutz_Emo_BrightBlue" }
                : new Material(Shader.Find("Standard")) { name = "Dutz_Emo_BrightBlue" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        if (texture != null)
            material.mainTexture = texture;

        material.color = Color.white;
        EditorUtility.SetDirty(material);
        return true;
    }

    static Texture2D RecolorDutzLook(Texture2D source, HashSet<Vector2Int> headTiles)
    {
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        var pixels = source.GetPixels();
        var width = source.width;
        var atlasScale = width / 512f;
        var topHeadRowY = headTiles != null && headTiles.Count > 0 ? headTiles.Max(tile => tile.y) : -1;

        for (var i = 0; i < pixels.Length; i++)
        {
            var x = i % width;
            var y = i / width;
            var src = pixels[i];
            var tile = new Vector2Int(
                Mathf.FloorToInt(x / (TileSize * atlasScale)) * TileSize,
                Mathf.FloorToInt(y / (TileSize * atlasScale)) * TileSize);

            if (IsProtectedFacePixel(x, y, width, pixels, src))
            {
                output.SetPixel(x, y, src);
                continue;
            }

            if (headTiles != null && headTiles.Contains(tile) && !IsFaceSkin(src) &&
                (tile.y == topHeadRowY || IsHairPixel(src)))
            {
                output.SetPixel(x, y, ShadeBlond(src));
                continue;
            }

            if (IsClothingDark(src))
            {
                output.SetPixel(x, y, ShadeBlue(src));
                continue;
            }

            output.SetPixel(x, y, src);
        }

        output.Apply();
        return output;
    }

    static bool IsProtectedFacePixel(int x, int y, int width, Color[] pixels, Color src)
    {
        if (Mathf.Max(src.r, src.g, src.b) >= 0.12f)
            return false;

        return IsNearFaceSkin(x, y, width, pixels, 4);
    }

    static bool IsNearFaceSkin(int x, int y, int width, Color[] pixels, int radius)
    {
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= width || ny >= width)
                    continue;

                if (IsFaceSkin(pixels[ny * width + nx]))
                    return true;
            }
        }

        return false;
    }

    static bool IsClothingDark(Color color)
    {
        var max = Mathf.Max(color.r, color.g, color.b);
        if (max > 0.45f)
            return false;

        if (color.r > 0.55f && color.g < 0.45f && color.b > 0.45f)
            return false;

        if (color.r > 0.65f && color.g > 0.55f)
            return false;

        return true;
    }

    static Color ShadeBlue(Color src)
    {
        var max = Mathf.Max(src.r, src.g, src.b);
        var shade = Mathf.Clamp(max / 0.35f, 0.35f, 1f);
        return new Color(BrightBlue.r * shade, BrightBlue.g * shade, BrightBlue.b * shade, src.a);
    }

    static bool IsFaceSkin(Color color)
    {
        return color.r > 0.62f && color.g > 0.5f && color.b > 0.35f && color.b < 0.72f && color.g >= color.b;
    }

    static bool IsHairPixel(Color color)
    {
        if (IsFaceSkin(color))
            return false;

        if (Mathf.Max(color.r, color.g, color.b) < 0.12f)
            return false;

        if (color.r > 0.4f && color.g < 0.55f && color.b > 0.3f && color.r > color.g)
            return true;

        var max = Mathf.Max(color.r, color.g, color.b);
        var min = Mathf.Min(color.r, color.g, color.b);
        return max > 0.2f && max < 0.78f && max - min < 0.28f && color.r > 0.3f && color.g > 0.3f;
    }

    static Color ShadeBlond(Color src)
    {
        var max = Mathf.Max(src.r, src.g, src.b);
        var shade = Mathf.Clamp(max / 0.85f, 0.45f, 1f);
        return new Color(BlondHair.r * shade, BlondHair.g * shade, BlondHair.b * shade, src.a);
    }

    static void ApplyMaterialToDutzPrefab()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
            return;

        var prefabRoot = PrefabUtility.LoadPrefabContents(DutzPrefabPath);
        if (prefabRoot == null)
            return;

        var changed = AssignMaterial(prefabRoot);
        if (changed)
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, DutzPrefabPath);

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    static void ApplyMaterialToSceneDutz()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
            return;

        var dutz = DutzEditorHelpers.FindPrimaryDutzObject();
        if (dutz == null)
            return;

        if (AssignMaterial(dutz))
            EditorSceneManager.MarkSceneDirty(dutz.scene);
    }

    static bool AssignMaterial(GameObject root)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null || root == null)
            return false;

        var changed = false;
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer == null || renderer.gameObject.name != EmoOutfit)
                continue;

            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] == material)
                    continue;

                materials[i] = material;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        return changed;
    }

    static SkinnedMeshRenderer GetEmoRendererFromPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmoPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + EmoPrefabPath);
            return null;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer != null && renderer.gameObject.name == EmoOutfit && renderer.sharedMesh != null)
                return renderer;
        }

        Debug.LogError("[Dutz] Missing SC_Emo mesh on Emo prefab.");
        return null;
    }

    static Texture2D LoadReadableTexture(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[Dutz] Missing texture: " + assetPath);
            return null;
        }

        var previousReadable = importer.isReadable;
        if (!previousReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return null;

        var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        copy.SetPixels(texture.GetPixels());
        copy.Apply();

        if (!previousReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        return copy;
    }
}
