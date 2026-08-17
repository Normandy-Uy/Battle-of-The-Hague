using UnityEditor;
using UnityEngine;

/// <summary>Builds a prefab from Assets/3d-model.fbx (uploaded via public/3d-model.fbx).</summary>
public static class Dutz3dModelPrefabBuilder
{
    const string ModelFbxPath = "Assets/3d-model.fbx";
    const string PrefabPath = "Assets/Prefabs/Dutz3dModel.prefab";

    /// <summary>Batch: -executeMethod Dutz3dModelPrefabBuilder.BuildPrefabBatch</summary>
    public static void BuildPrefabBatch() => BuildPrefab(log: true);

    public static bool BuildPrefab(bool log)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelFbxPath);
        if (fbx == null)
        {
            Debug.LogError("[Dutz] Missing 3d model: " + ModelFbxPath);
            return false;
        }

        System.IO.Directory.CreateDirectory("Assets/Prefabs");

        var temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        temp.name = "Dutz3dModel";

        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
        Object.DestroyImmediate(temp);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (prefab == null)
        {
            Debug.LogError("[Dutz] Failed to save prefab: " + PrefabPath);
            return false;
        }

        if (log)
            Debug.Log("[Dutz] Saved " + PrefabPath + " from " + ModelFbxPath);

        return true;
    }
}
