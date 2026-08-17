using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authoring helpers for Level 0 vehicle spawn/move components.</summary>
public static class DutzVehicleSpawnSetup
{
    public const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string VehiclesRootName = "VEHICLES_";

    /// <summary>Batch: -executeMethod DutzVehicleSpawnSetup.SetupLevel00VehiclesBatch</summary>
    public static void SetupLevel00VehiclesBatch() => SetupLevel00Vehicles(log: true);

    public static bool SetupLevel00Vehicles(bool log)
    {
        if (EditorApplication.isPlaying)
        {
            if (log)
                Debug.LogError("[Dutz] Exit Play mode before setting up Level 0 vehicles.");
            return false;
        }

        if (!File.Exists(Level00ScenePath))
        {
            if (log)
                Debug.LogError("[Dutz] Dutz_Level00.unity not found.");
            return false;
        }

        EditorSceneManager.OpenScene(Level00ScenePath, OpenSceneMode.Single);
        EnsureVehiclesRoot();
        var count = ApplySpawnComponentsInOpenScene(log);
        if (count <= 0)
        {
            if (log)
                Debug.LogWarning("[Dutz] No Vehicle_* objects found in Dutz_Level00.");
            return false;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        if (log)
            Debug.Log($"[Dutz] Level 0 vehicles ready: {count} spawn component(s) baked under {VehiclesRootName}.");
        return true;
    }

    static void EnsureVehiclesRoot()
    {
        var root = GameObject.Find(VehiclesRootName);
        if (root == null)
        {
            root = new GameObject(VehiclesRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create VEHICLES_ root");
        }

        ReparentVehiclesUnderRoot(root.transform);
    }

    static void ReparentVehiclesUnderRoot(Transform root)
    {
        var vehicles = CollectVehicleRoots();
        for (var i = 0; i < vehicles.Count; i++)
        {
            var vehicle = vehicles[i];
            if (vehicle == null || vehicle.transform.parent == root)
                continue;

            Undo.SetTransformParent(vehicle.transform, root, "Parent vehicle under VEHICLES_");
        }
    }

    static int ApplySpawnComponentsInOpenScene(bool log)
    {
        var vehicles = CollectVehicleRoots();
        var count = 0;
        for (var i = 0; i < vehicles.Count; i++)
        {
            var vehicle = vehicles[i];
            if (vehicle == null)
                continue;

            var spawn = vehicle.GetComponent<DutzVehicleSpawn>();
            if (spawn == null)
            {
                spawn = Undo.AddComponent<DutzVehicleSpawn>(vehicle);
            }

            spawn.MoveSpeed = DutzVehicleSpawn.DefaultMoveSpeed;
            spawn.PrepareGroundContact();
            spawn.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(spawn);
            count++;
        }

        if (log && count > 0)
            Debug.Log($"[Dutz] Baked spawn pose + move speed {DutzVehicleSpawn.DefaultMoveSpeed} on {count} vehicle(s).");

        return count;
    }

    static System.Collections.Generic.List<GameObject> CollectVehicleRoots()
    {
        var results = new System.Collections.Generic.List<GameObject>();
        var root = GameObject.Find(VehiclesRootName);
        if (root != null)
        {
            for (var i = 0; i < root.transform.childCount; i++)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (DutzVehicleSpawn.IsVehicleRoot(child))
                    results.Add(child);
            }
        }

        var allTransforms = Object.FindObjectsOfType<Transform>();
        for (var i = 0; i < allTransforms.Length; i++)
        {
            var go = allTransforms[i].gameObject;
            if (!DutzVehicleSpawn.IsVehicleRoot(go))
                continue;

            if (go.transform.parent != null
                && go.transform.parent.gameObject.name == VehiclesRootName)
                continue;

            if (results.Contains(go))
                continue;

            results.Add(go);
        }

        results.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return results;
    }
}
