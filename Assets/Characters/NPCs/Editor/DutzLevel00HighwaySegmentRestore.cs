using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores missing Level 00 highway prefab instances (from backup scene transforms).
/// </summary>
public static class DutzLevel00HighwaySegmentRestore
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string StraightPrefabPath = "Assets/BrokenVector/LowPolyRoadPack/Prefabs/Highway Straight 1.prefab";
    const string BridgePrefabPath = "Assets/BrokenVector/LowPolyRoadPack/Prefabs/Highway Bridge 1.prefab";

    struct SegmentSpec
    {
        public string Name;
        public string PrefabPath;
        public Vector3 Position;
        public Vector3 Scale;
        public Quaternion Rotation;
    }

    static readonly SegmentSpec[] Segments =
    {
        new SegmentSpec
        {
            Name = "Highway Bridge 1",
            PrefabPath = BridgePrefabPath,
            Position = new Vector3(-1047.8f, -2.1f, 15.4f),
            Scale = new Vector3(2f, 1f, 4f),
            Rotation = Quaternion.Euler(-0.515f, 180.516f, -1.71f)
        },
        new SegmentSpec
        {
            Name = "Highway Straight 2",
            PrefabPath = StraightPrefabPath,
            Position = new Vector3(-464f, -79f, -44f),
            Scale = new Vector3(4f, 12f, 100.00003f),
            Rotation = Quaternion.Euler(0.156f, 90.399f, 1.877f)
        },
        new SegmentSpec
        {
            Name = "Highway Straight 3",
            PrefabPath = StraightPrefabPath,
            Position = new Vector3(-443.7f, -87.3f, 9f),
            Scale = new Vector3(4f, 12f, 100.00003f),
            Rotation = Quaternion.Euler(0f, -90f, 0f)
        },
        new SegmentSpec
        {
            Name = "Highway Bridge 4",
            PrefabPath = BridgePrefabPath,
            Position = new Vector3(133f, 0f, -55f),
            Scale = new Vector3(2f, 1f, 6f),
            Rotation = Quaternion.identity
        },
        new SegmentSpec
        {
            Name = "Highway Bridge 5",
            PrefabPath = BridgePrefabPath,
            Position = new Vector3(545f, 0f, -54.8f),
            Scale = new Vector3(2.5f, 1f, 6f),
            Rotation = Quaternion.identity
        },
        new SegmentSpec
        {
            Name = "Highway Straight 6",
            PrefabPath = StraightPrefabPath,
            Position = new Vector3(556.6f, 7.9f, 12.8f),
            Scale = new Vector3(0.5f, 2.6868f, 100f),
            Rotation = Quaternion.Euler(0f, -90f, 0f)
        },
        new SegmentSpec
        {
            Name = "Highway Straight 7",
            PrefabPath = StraightPrefabPath,
            Position = new Vector3(811.5f, 5f, 11.3f),
            Scale = new Vector3(0.4952194f, 2.826f, 128.56f),
            Rotation = Quaternion.Euler(0f, -90f, 0f)
        }
    };

    /// <summary>Batch: -executeMethod DutzLevel00HighwaySegmentRestore.RestoreMissingBatch</summary>
    public static void RestoreMissingBatch() => RestoreMissing(log: true);

    [MenuItem("Assets/Dutz Authoring/Restore Level 00 Highway Segments")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Restore Highway Segments", "Exit Play mode first.", "OK");
            return;
        }

        RestoreMissing(log: true);
    }

    public static bool RestoreMissing(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = SceneManager.GetActiveScene();
        if (scene.path != Level00ScenePath)
        {
            scene = EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        var changed = false;
        foreach (var spec in Segments)
        {
            if (FindSegment(spec.Name) != null)
                continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (prefab == null)
            {
                if (log)
                    Debug.LogError($"[Dutz] Missing highway prefab: {spec.PrefabPath}");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Restore Level 00 Highway Segment");
            instance.name = spec.Name;
            instance.transform.SetPositionAndRotation(spec.Position, spec.Rotation);
            instance.transform.localScale = spec.Scale;
            changed = true;

            if (log)
                Debug.Log($"[Dutz] Restored missing highway segment: {spec.Name}");
        }

        if (!changed)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return true;
    }

    static GameObject FindSegment(string segmentName)
    {
        var direct = GameObject.Find(segmentName);
        if (direct != null)
            return direct;

        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform != null && transform.name == segmentName)
                return transform.gameObject;
        }

        return null;
    }
}
