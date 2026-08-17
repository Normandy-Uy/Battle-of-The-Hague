using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-shot authoring utility for copying requested configured giants into Level 07.</summary>
public static class DutzLevel07RequestedGiantDuplicator
{
    const string Level01Path = "Assets/Scenes/Dutz_Level01.unity";
    const string Level02Path = "Assets/Scenes/Dutz_Level02.unity";
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string PrefabFolder = "Assets/Characters/Level07/Prefabs";

    readonly struct Source
    {
        public readonly string scenePath;
        public readonly string objectName;
        public readonly string prefabName;
        public readonly float sideOffset;

        public Source(string scenePath, string objectName, string prefabName, float sideOffset)
        {
            this.scenePath = scenePath;
            this.objectName = objectName;
            this.prefabName = prefabName;
            this.sideOffset = sideOffset;
        }
    }

    static readonly Source[] Sources =
    {
        new Source(Level01Path, DutzGiantBossNames.GongBong, "Level07_GongBong", -24f),
        new Source(Level02Path, "Joles", "Level07_Joles", -48f),
        new Source(Level02Path, DutzGiantBossNames.Cawetan, "Level07_Cawetan", 24f),
        new Source(Level01Path, DutzGiantBossNames.ETol, "Level07_ETol", 48f),
    };

    [MenuItem("Assets/Dutz Authoring/Duplicate Requested Giants To Level07")]
    public static void DuplicateRequestedGiantsToLevel07()
    {
        EnsureFolder("Assets/Characters/Level07");
        EnsureFolder(PrefabFolder);

        var prefabPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in Sources)
        {
            var scene = EditorSceneManager.OpenScene(source.scenePath, OpenSceneMode.Single);
            var sourceObject = FindSceneObject(scene, source.objectName);
            if (sourceObject == null)
                throw new InvalidOperationException(
                    $"Could not find '{source.objectName}' in {source.scenePath}.");

            var prefabPath = $"{PrefabFolder}/{source.prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(sourceObject, prefabPath);
            prefabPaths[source.objectName] = prefabPath;
        }

        var level07 = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);
        var raptor = FindSceneObject(level07, "RAPTOR");
        if (raptor == null)
            throw new InvalidOperationException("RAPTOR was not found in Dutz_Level07.");

        var parent = raptor.transform.parent;
        var groundY = GetRenderedBottomY(raptor, raptor.transform.position.y);
        var side = raptor.transform.right;
        side.y = 0f;
        if (side.sqrMagnitude < 0.0001f)
            side = Vector3.forward;
        side.Normalize();

        foreach (var source in Sources)
        {
            var targetName = source.objectName == "Joles" ? DutzGiantBossNames.Joles : source.objectName;
            var existing = FindSceneObject(level07, targetName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[source.objectName]);
            if (prefab == null)
                throw new InvalidOperationException($"Could not load prefab for {source.objectName}.");

            var clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, level07);
            PrefabUtility.UnpackPrefabInstance(
                clone,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);

            clone.name = targetName;
            clone.transform.SetParent(parent, true);
            clone.transform.position = raptor.transform.position + side * source.sideOffset;
            clone.transform.rotation = raptor.transform.rotation;
            AlignRenderedBottom(clone, groundY);

            var respawn = clone.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn != null)
                UnityEngine.Object.DestroyImmediate(respawn);
            respawn = clone.AddComponent<SimpleCitizensNpcRespawn>();
        }

        EditorSceneManager.MarkSceneDirty(level07);
        EditorSceneManager.SaveScene(level07);
        Selection.activeGameObject = raptor;
        Debug.Log("[Dutz] Level07: duplicated Gong Bong, JOLES, Cawetan, and E-TOL beside RAPTOR.");
    }

    [MenuItem("Assets/Dutz Authoring/Make Level07 Giants Stationary")]
    public static void MakeLevel07GiantsStationary()
    {
        var level07 = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);
        DutzLevel07GiantStationary.EnsureAll();
        EditorSceneManager.MarkSceneDirty(level07);
        EditorSceneManager.SaveScene(level07);
        Debug.Log("[Dutz] Level07: non-bird giants are stationary with chase and burn removed.");
    }

    static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == objectName)
                    return transform.gameObject;
            }
        }

        return null;
    }

    static float GetRenderedBottomY(GameObject target, float fallback)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return fallback;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.min.y;
    }

    static void AlignRenderedBottom(GameObject target, float groundY)
    {
        var bottom = GetRenderedBottomY(target, target.transform.position.y);
        var position = target.transform.position;
        position.y += groundY - bottom;
        target.transform.position = position;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            throw new InvalidOperationException($"Invalid asset folder: {path}");

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
