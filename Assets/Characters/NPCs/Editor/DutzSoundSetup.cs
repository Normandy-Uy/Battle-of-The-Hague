using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds walk/run/jump sounds to Dutz (prefab + open scene).
/// </summary>
public static class DutzSoundSetup
{
    const string PrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";

    public static void ApplyToPrefab()
    {
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            return;

        if (!DutzPrefabRepair.CanLoadPrefabContents())
        {
            Debug.LogWarning("[Dutz] Cannot update prefab — Dutz.prefab may be corrupt.");
            return;
        }

        GameObject root = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(PrefabPath);
            ApplyToGameObject(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Dutz] Prefab sound setup failed: " + ex.Message);
        }
        finally
        {
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static bool PrefabHasSoundSetup()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            return false;

        if (HasSoundSetup(prefab))
            return true;

        return prefab.GetComponentInChildren<DutzMovementSounds>(true) != null;
    }

    public static bool SceneHasSoundSetup()
    {
        var target = FindSceneDutz();
        return target != null && HasSoundSetup(target);
    }

    public static bool HasSoundSetup(GameObject go)
    {
        if (go == null)
            return false;

        var sounds = go.GetComponent<DutzMovementSounds>();
        return go.GetComponent<AudioSource>() != null && sounds != null && sounds.enabled;
    }

    public static void ApplyToScene()
    {
        var target = FindSceneDutz();
        if (target == null)
        {
            if (!EditorSceneManager.GetActiveScene().path.Equals(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            target = FindSceneDutz();
        }

        if (target == null)
            return;

        ApplyToGameObject(target);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    static GameObject FindSceneDutz()
    {
        return DutzEditorHelpers.FindPrimaryDutzObject();
    }

    public static bool ApplyToGameObject(GameObject go)
    {
        if (go == null)
            return false;

        var changed = false;

        if (go.GetComponent<AudioSource>() == null)
        {
            go.AddComponent<AudioSource>();
            changed = true;
        }

        var source = go.GetComponent<AudioSource>();
        if (source != null && source.playOnAwake)
        {
            source.playOnAwake = false;
            changed = true;
        }

        var sounds = go.GetComponent<DutzMovementSounds>();
        if (sounds == null)
        {
            sounds = go.AddComponent<DutzMovementSounds>();
            changed = true;
        }

        if (sounds != null && !sounds.enabled)
        {
            sounds.enabled = true;
            changed = true;
        }

        return changed;
    }
}
