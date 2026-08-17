using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SUIMONO was removed from the project. This menu only cleans leftover water objects from scenes.
/// Use Tools / Dutz / Blue Void (DutzVoidPlaneSetup) for the ocean look instead.
/// </summary>
public static class DutzSuimonoSeaSetup
{
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    static readonly string[] LegacyObjectNames =
    {
        "SUIMONO_Module",
        "SUIMONO_Surface_Ocean",
        "SUIMONO_Surface"
    };

    public static void AddFromMenu()
    {
        EditorUtility.DisplayDialog(
            "Suimono Sea",
            "SUIMONO was removed from this project.\n\nUse Tools / Dutz / Blue Void Under Roads instead.",
            "OK");
    }

    public static void RemoveFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Suimono Sea", "Exit Play mode first.", "OK");
            return;
        }

        if (!RemoveFromShowcase(log: true))
        {
            EditorUtility.DisplayDialog("Suimono Sea", "No leftover Suimono objects found in Dutz_Level02.", "OK");
            return;
        }

        Debug.Log("[Dutz] Leftover Suimono scene objects removed from Dutz_Level02.");
    }

    /// <summary>Batch: -executeMethod DutzSuimonoSeaSetup.AddToShowcase</summary>
    public static void AddToShowcase() =>
        Debug.LogWarning("[Dutz] SUIMONO is not installed. Use DutzVoidPlaneSetup for blue void under roads.");

    /// <summary>Batch: -executeMethod DutzSuimonoSeaSetup.RemoveFromShowcase</summary>
    public static void RemoveFromShowcase() => RemoveFromShowcase(log: false);

    static bool RemoveFromShowcase(bool log)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var hadWater = RemoveLegacySuimonoObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log && hadWater)
            Debug.Log("[Dutz] Leftover Suimono objects removed.");

        return hadWater;
    }

    static bool RemoveLegacySuimonoObjects()
    {
        var removed = false;

        foreach (var objectName in LegacyObjectNames)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
                continue;

            Undo.DestroyObjectImmediate(go);
            removed = true;
        }

        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            if (cam == null || cam.GetComponent<DutzCameraFollow>() == null)
                continue;

            if (cam.gameObject.name == "SUIMONO_Module")
            {
                cam.gameObject.name = "Main Camera";
                cam.tag = "MainCamera";
                removed = true;
            }

            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(cam.gameObject) > 0)
                removed = true;
        }

        return removed;
    }
}
