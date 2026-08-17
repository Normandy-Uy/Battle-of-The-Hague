using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations HONTAVIRUS mid Level07 Highway 7 — chase + burn, deck clamp, faces downslope
/// (Highway 7 slope is opposite Highway 8: high toward +X, downslope −X).
/// </summary>
public static class DutzLevel07HontavirusStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Highway7Name = "Highway 7";
    const string GiantName = "HONTAVIRUS";
    const float LocalAxisInset = 0.08f;

    [MenuItem("Assets/Dutz Authoring/Station HONTAVIRUS On Level07 Highway7")]
    public static void StationFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station HONTAVIRUS On Level07 Highway7 requires Edit Mode.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station HONTAVIRUS on Level07 Highway 7.");
    }

    public static bool StationSilent(bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var giant = GameObject.Find(GiantName);
        if (giant == null)
            giant = DutzGiantBossNames.FindHontavirus();
        if (giant == null)
        {
            Debug.LogError($"[Dutz] '{GiantName}' not found in Level07.");
            return false;
        }

        var highway7 = GameObject.Find(Highway7Name);
        if (highway7 == null)
        {
            Debug.LogError($"[Dutz] '{Highway7Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(giant.transform, "Station HONTAVIRUS On Highway 7");

        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        var road = highway7.transform;
        if (!TryGetLocalExtents(road, out var minX, out var maxX, out var localZ, out var localY))
        {
            Debug.LogError("[Dutz] Highway 7 has no usable MeshCollider bounds.");
            return false;
        }

        var midX = (minX + maxX) * 0.5f;
        var seed = road.TransformPoint(new Vector3(midX, localY, localZ));
        seed.y = 200f;

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(giant);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);
        var pivot = seed;
        if (!DutzRoadGround.TryClampOntoLevel07Highway7Deck(ref pivot, pivotToFeet))
        {
            Debug.LogWarning("[Dutz] Highway 7 deck sample missed for HONTAVIRUS — using TransformPoint seed.");
            pivot = seed;
        }

        var downslope = ResolveDownslopeForward(road, minX, maxX, localZ, localY);
        var rotation = Quaternion.LookRotation(downslope, Vector3.up);

        giant.transform.SetPositionAndRotation(pivot, rotation);
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station HONTAVIRUS Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
            EditorUtility.SetDirty(rb);
        }

        EnableBehaviour<SimpleCitizensGiantHippieHunter>(giant);
        EnableBehaviour<SimpleCitizensNpcPhysics>(giant);
        EnableBehaviour<DutzGiantHeat>(giant);

        var hunter = giant.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            var hunterSo = new SerializedObject(hunter);
            hunterSo.FindProperty("wakeDistance").floatValue = 200f;
            hunterSo.FindProperty("huntImmediately").boolValue = false;
            hunterSo.FindProperty("chaseSpeed").floatValue = 19f;
            hunterSo.FindProperty("chaseAnimSpeed").floatValue = 1f;
            hunterSo.FindProperty("chaseStopDistance").floatValue = 2.5f;
            hunterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(giant);

        Undo.RecordObject(respawn, "Bake HONTAVIRUS Highway7 Spawn");
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(giant);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = giant;

        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {GiantName} mid {Highway7Name} at {pivot} " +
                $"(downslope face, chase/burn on, spawn baked).");
        }

        return true;
    }

    static Vector3 ResolveDownslopeForward(
        Transform road,
        float minX,
        float maxX,
        float localZ,
        float localY)
    {
        var seedMin = road.TransformPoint(new Vector3(minX, localY, localZ));
        seedMin.y = 200f;
        var seedMax = road.TransformPoint(new Vector3(maxX, localY, localZ));
        seedMax.y = 200f;

        var yMin = seedMin.y;
        var yMax = seedMax.y;
        Vector3 deckMin = seedMin;
        Vector3 deckMax = seedMax;
        if (DutzRoadGround.TrySampleLevel07Highway7DeckPoint(seedMin, out deckMin, out _))
            yMin = deckMin.y;
        if (DutzRoadGround.TrySampleLevel07Highway7DeckPoint(seedMax, out deckMax, out _))
            yMax = deckMax.y;

        var highIsMaxX = yMax >= yMin;
        var high = highIsMaxX ? deckMax : deckMin;
        var low = highIsMaxX ? deckMin : deckMax;
        var down = low - high;
        down.y = 0f;
        if (down.sqrMagnitude < 0.0001f)
        {
            down = -road.right;
            down.y = 0f;
        }

        if (down.sqrMagnitude < 0.0001f)
            down = Vector3.left;

        return down.normalized;
    }

    static bool TryGetLocalExtents(
        Transform road,
        out float minX,
        out float maxX,
        out float localZ,
        out float localY)
    {
        minX = maxX = localZ = localY = 0f;
        var col = road.GetComponent<MeshCollider>();
        var mesh = col != null ? col.sharedMesh : null;
        if (mesh == null)
            return false;

        var b = mesh.bounds;
        minX = Mathf.Lerp(b.min.x, b.max.x, LocalAxisInset);
        maxX = Mathf.Lerp(b.min.x, b.max.x, 1f - LocalAxisInset);
        if (minX > maxX)
        {
            minX = b.center.x;
            maxX = b.center.x;
        }

        localZ = b.center.z;
        localY = b.max.y;
        return true;
    }

    static void EnableBehaviour<T>(GameObject go) where T : Behaviour
    {
        var behaviour = go.GetComponent<T>();
        if (behaviour == null)
            return;

        Undo.RecordObject(behaviour, "Enable " + typeof(T).Name);
        behaviour.enabled = true;
        EditorUtility.SetDirty(behaviour);
    }
}
