using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Imports the compressed Piyaya boss face photo and applies it on Level07.
/// </summary>
public static class DutzLevel07PiyayaFaceApplier
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string GiantName = "Piyaya";
    const string TextureAssetPath = "Assets/Characters/Level07/Textures/PiyayaBossFace.jpg";
    const string ResourcesPhotoPath = "Assets/Characters/NPCs/Resources/PiyayaBossFacePhoto.jpg";
    const string MaterialPath = "Assets/Characters/NPCs/Resources/PiyayaBossFace.mat";
    const int MaxEdge = 512;

    [MenuItem("Assets/Dutz Authoring/Apply Piyaya Boss Face On Level07")]
    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Apply Piyaya Boss Face requires Edit Mode.");
            return;
        }

        if (!ApplySilent(log: true))
            Debug.LogError("[Dutz] Failed to apply Piyaya boss face.");
    }

    public static bool ApplySilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        if (!File.Exists(TextureAssetPath) || !File.Exists(ResourcesPhotoPath))
        {
            Debug.LogError(
                "[Dutz] Missing PiyayaBossFace.jpg — expected at Level07/Textures and NPCs/Resources.");
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
            Debug.LogError("[Dutz] Could not load PiyayaBossFace texture after import.");
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
            giant = DutzGiantBossNames.FindPiyaya();
        if (giant == null)
        {
            Debug.LogError("[Dutz] Piyaya not found in Level07.");
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
                $"[Dutz] Applied PiyayaBossFace on Piyaya ({MaxEdge}px max, {bytes} bytes JPG).");
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
            "Assets/Characters/NPCs/Resources/MBilyarBossFace.mat");
        if (template == null)
            template = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Characters/NPCs/Resources/MarkoLektaBossFace.mat");
        if (template == null)
        {
            Debug.LogError("[Dutz] No boss-face material template found for Piyaya.");
            return null;
        }

        var mat = new Material(template) { name = "PiyayaBossFace" };
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
