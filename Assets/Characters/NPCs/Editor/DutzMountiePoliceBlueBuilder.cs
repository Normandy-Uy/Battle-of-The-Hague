using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Recolors the Level 01 mountie's outfit atlas to police blue, including the hat.
/// </summary>
public static class DutzMountiePoliceBlueBuilder
{
    const string Level01ScenePath = "Assets/Scenes/Dutz_Level01.unity";
    const string SourceTexturePath = "Assets/SimpleCitizens/Textures/SimpleCitizens_Mountie_Brown.png";
    const string OutputTexturePath = "Assets/Characters/NPCs/Textures/SimpleCitizens_Mountie_PoliceBlue.png";
    const string MaterialPath = "Assets/Characters/NPCs/Materials/SimpleCitizens_Mountie_PoliceBlue.mat";
    const string TemplateMaterialPath = "Assets/SimpleCitizens/Materials/SimpleCitizens_Mountie_Brown.mat";
    const string MountiePrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Mountie_Brown.prefab";
    const string MountieOutfit = "SC_Mountie";
    const string MountieRootName = "SimpleCitizens_Mountie_Brown";
    const int TileSize = 64;

    static readonly Color PoliceBlue = new Color(0.02f, 0.16f, 0.42f, 1f);

    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Mountie Police Blue", "Exit Play mode first.", "OK");
            return;
        }

        if (!ApplyOnLevel01(log: true))
        {
            EditorUtility.DisplayDialog(
                "Mountie Police Blue",
                "Could not apply police blue outfit.\n\nEnsure SimpleCitizens_Mountie_Brown exists on Dutz_Level01.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Mountie Police Blue",
                "Mountie outfit on Level 01 is now police blue (including the hat).",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzMountiePoliceBlueBuilder.ApplyOnLevel01Batch</summary>
    public static void ApplyOnLevel01Batch() => ApplyOnLevel01(log: true);

    public static bool ApplyOnLevel01(bool log)
    {
        if (!File.Exists(Level01ScenePath))
        {
            Debug.LogError("[Dutz] Dutz_Level01.unity not found.");
            return false;
        }

        if (!BuildPoliceBlueAssets())
            return false;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level01ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level01ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var count = ApplyPoliceBlueToLevel01Mounties();
        if (count == 0)
        {
            Debug.LogError("[Dutz] SimpleCitizens_Mountie_Brown not found in Dutz_Level01.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (log)
            Debug.Log($"[Dutz] Applied police blue outfit to {count} mountie(s) on Level 01.");

        return true;
    }

    public static bool BuildPoliceBlueAssets()
    {
        var mountieRenderer = GetMountieRendererFromPrefab();
        if (mountieRenderer == null)
            return false;

        var source = LoadReadableTexture(SourceTexturePath);
        if (source == null)
            return false;

        var headTiles = DutzGiantHippieBossFaceBuilder.GetHeadTileOrigins(
            mountieRenderer.sharedMesh, mountieRenderer.bones, 1);
        var facePaintTiles = DutzGiantHippieBossFaceBuilder.GetFacePaintTiles(
            mountieRenderer.sharedMesh, mountieRenderer.bones);
        var facePaintTileSet = facePaintTiles != null && facePaintTiles.Length > 0
            ? new HashSet<Vector2Int>(facePaintTiles)
            : null;
        var hatTileSet = BuildHatTileSet(mountieRenderer.sharedMesh, mountieRenderer.bones);
        var output = RecolorMountieOutfit(source, facePaintTileSet, hatTileSet);
        Object.DestroyImmediate(source);

        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            Debug.LogError("[Dutz] Could not resolve project root for mountie police blue texture.");
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
                ? new Material(template) { name = "SimpleCitizens_Mountie_PoliceBlue" }
                : new Material(Shader.Find("Standard")) { name = "SimpleCitizens_Mountie_PoliceBlue" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        if (texture != null)
            material.mainTexture = texture;

        material.color = Color.white;
        EditorUtility.SetDirty(material);
        return true;
    }

    static int ApplyPoliceBlueToLevel01Mounties()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
            return 0;

        var count = 0;
        foreach (var root in Object.FindObjectsOfType<Transform>(true))
        {
            if (root == null || root.name != MountieRootName)
                continue;

            if (AssignMaterial(root.gameObject, material))
            {
                EditorUtility.SetDirty(root.gameObject);
                count++;
            }
        }

        return count;
    }

    static bool AssignMaterial(GameObject root, Material material)
    {
        if (root == null || material == null)
            return false;

        var changed = false;
        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer == null || renderer.gameObject.name != MountieOutfit)
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

    static HashSet<Vector2Int> BuildHatTileSet(Mesh mesh, Transform[] bones)
    {
        var hatTiles = DutzGiantHippieBossFaceBuilder.GetHeadExclusiveAtlasTiles(mesh, bones);
        if (hatTiles == null || hatTiles.Count == 0)
            return hatTiles;

        var facePaintTiles = DutzGiantHippieBossFaceBuilder.GetFacePaintTiles(mesh, bones);
        if (facePaintTiles != null)
        {
            foreach (var tile in facePaintTiles)
                hatTiles.Remove(tile);
        }

        return hatTiles;
    }

    static Texture2D RecolorMountieOutfit(
        Texture2D source,
        HashSet<Vector2Int> facePaintTiles,
        HashSet<Vector2Int> hatTiles)
    {
        var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        var pixels = source.GetPixels();
        var width = source.width;
        var atlasScale = width / 512f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var x = i % width;
            var y = i / width;
            var src = pixels[i];
            var tile = new Vector2Int(
                Mathf.FloorToInt(x / (TileSize * atlasScale)) * TileSize,
                Mathf.FloorToInt(y / (TileSize * atlasScale)) * TileSize);

            if (facePaintTiles != null && facePaintTiles.Contains(tile))
            {
                output.SetPixel(x, y, src);
                continue;
            }

            if (IsProtectedFacePixel(x, y, width, pixels, src))
            {
                output.SetPixel(x, y, src);
                continue;
            }

            if (IsFaceSkin(src))
            {
                output.SetPixel(x, y, src);
                continue;
            }

            if (IsMountieOutfit(src))
            {
                output.SetPixel(x, y, ShadePoliceBlue(src));
                continue;
            }

            if (hatTiles != null && hatTiles.Contains(tile) && IsMountieHatLeather(src))
            {
                output.SetPixel(x, y, ShadePoliceBlue(src));
                continue;
            }

            output.SetPixel(x, y, src);
        }

        output.Apply();
        return output;
    }

    static bool IsMountieHatLeather(Color color)
    {
        var max = Mathf.Max(color.r, color.g, color.b);
        var min = Mathf.Min(color.r, color.g, color.b);
        if (max < 0.12f)
            return false;

        return color.r > 0.55f && color.g > 0.45f && color.b > 0.25f && color.b < 0.45f &&
               color.r > color.g && color.g > color.b && max - min > 0.08f;
    }

    static bool IsMountieOutfit(Color color)
    {
        var max = Mathf.Max(color.r, color.g, color.b);
        var min = Mathf.Min(color.r, color.g, color.b);

        if (max < 0.48f)
            return true;

        if (color.r > 0.55f && color.r > color.g * 1.4f && color.r > color.b * 1.4f)
            return true;

        if (color.r >= color.b && color.g >= color.b * 0.55f && max < 0.88f && min < 0.55f)
            return true;

        if (max - min < 0.12f && max < 0.78f)
            return true;

        return false;
    }

    static Color ShadePoliceBlue(Color src)
    {
        var max = Mathf.Max(src.r, src.g, src.b);
        var shade = Mathf.Clamp(max / 0.55f, 0.3f, 1f);
        return new Color(PoliceBlue.r * shade, PoliceBlue.g * shade, PoliceBlue.b * shade, src.a);
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

    static bool IsFaceSkin(Color color)
    {
        if (color.r > 0.62f && color.g > 0.5f && color.b > 0.35f && color.b < 0.72f && color.g >= color.b)
            return true;

        return color.r > 0.85f && color.g > 0.72f && color.b > 0.62f && color.r >= color.g && color.g >= color.b;
    }

    static SkinnedMeshRenderer GetMountieRendererFromPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MountiePrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[Dutz] Missing prefab: " + MountiePrefabPath);
            return null;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer != null && renderer.gameObject.name == MountieOutfit && renderer.sharedMesh != null)
                return renderer;
        }

        Debug.LogError("[Dutz] Missing SC_Mountie mesh on mountie prefab.");
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
