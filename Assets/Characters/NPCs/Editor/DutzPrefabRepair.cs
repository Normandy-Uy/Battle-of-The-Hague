using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Fixes corrupt Dutz.prefab (duplicate file IDs / merge damage).
/// </summary>
public static class DutzPrefabRepair
{
    const string PrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";

    public static bool Repair()
    {
        try
        {
            if (File.Exists(PrefabPath))
            {
                var backup = PrefabPath + ".backup";
                File.Copy(PrefabPath, backup, true);
                Debug.Log("[Dutz] Backed up prefab to " + backup);
            }

            DutzCharacterBuilder.CreateDutz();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        }
        catch (Exception ex)
        {
            Debug.LogError("[Dutz] Prefab repair failed: " + ex.Message);
            return false;
        }
    }

    public static bool CanLoadPrefabContents()
    {
        if (!File.Exists(PrefabPath))
            return false;

        try
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            return root != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Dutz] Prefab load failed: " + ex.Message);
            return false;
        }
    }
}
