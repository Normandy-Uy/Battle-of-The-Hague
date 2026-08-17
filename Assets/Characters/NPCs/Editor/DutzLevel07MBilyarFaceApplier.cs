using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Imports the compressed M BILYAR boss face photo and applies it on Level07.
/// </summary>
public static class DutzLevel07MBilyarFaceApplier
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string GiantName = "M BILYAR";
    const string TextureAssetPath = "Assets/Characters/Level07/Textures/MBilyarBossFace.jpg";
    const string ResourcesPhotoPath = "Assets/Characters/NPCs/Resources/MBilyarBossFacePhoto.jpg";
    const string MaterialPath = "Assets/Characters/NPCs/Resources/MBilyarBossFace.mat";
    const int MaxEdge = 512;

    [MenuItem("Assets/Dutz Authoring/Apply M BILYAR Boss Face On Level07")]
    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Apply M BILYAR Boss Face requires Edit Mode.");
            return;
        }

        if (!ApplySilent(log: true))
            Debug.LogError("[Dutz] Failed to apply M BILYAR boss face.");
    }

    public static bool ApplySilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!File.Exists(TextureAssetPath) || !File.Exists(ResourcesPhotoPath))
        {
            Debug.LogError(
                "[Dutz] Missing MBilyarBossFace.jpg — expected at Level07/Textures and NPCs/Resources.");
            return false;
        }

        AssetDatabase.ImportAsset(TextureAssetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ResourcesPhotoPath, ImportAssetOptions.ForceUpdate);
        ApplyImportSettings(TextureAssetPath);
        ApplyImportSettings(ResourcesPhotoPath);

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResourcesPhotoPath);
        if (texture == null)
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureAssetPath);
        if (texture == null)
        {
            Debug.LogError("[Dutz] Could not load MBilyarBossFace texture after import.");
            return false;
        }

        var material = EnsureMaterial(texture);
        if (material == null)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        var giant = GameObject.Find(GiantName);
        if (giant == null)
            giant = DutzGiantBossNames.FindMBilyar();
        if (giant == null)
        {
            Debug.LogError("[Dutz] M BILYAR not found in Level07.");
            return false;
        }

        var face = giant.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = Undo.AddComponent<DutzGiantHippieBossFace>(giant);

        var so = new SerializedObject(face);
        so.FindProperty("faceMaterial").objectReferenceValue = material;
        so.ApplyModifiedPropertiesWithoutUndo();
        face.ApplyFace();
        EditorUtility.SetDirty(face);
        EditorUtility.SetDirty(giant);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = giant;

        if (log)
        {
            var bytes = new FileInfo(ResourcesPhotoPath).Length;
            Debug.Log(
                $"[Dutz] Applied MBilyarBossFace on M BILYAR ({MaxEdge}px max, {bytes} bytes JPG).");
        }

        return true;
    }

    static Material EnsureMaterial(Texture2D texture)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            if (existing.mainTexture != texture)
            {
                existing.mainTexture = texture;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
            }

            return existing;
        }

        var template = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Characters/NPCs/Resources/MarkoLektaBossFace.mat");
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Characters/NPCs/Resources/StoneBossFace.mat");
        if (template == null)
        {
            Debug.LogError("[Dutz] No boss-face material template found for M BILYAR.");
            return null;
        }

        var mat = new Material(template) { name = "MBilyarBossFace" };
        mat.mainTexture = texture;
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
    }

    static void ApplyImportSettings(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.maxTextureSize = MaxEdge;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }
}
