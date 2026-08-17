using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations I am baby at mid Level07 Highway Straight 3 —
/// pitched deck clamp, keeps chase/burn/HP, bakes spawn.
/// </summary>
public static class DutzLevel07IAmBabyStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Straight3Name = "Highway Straight 3";
    const string GiantName = "I am baby";
    const float LocalAxisInset = 0.08f;

    [MenuItem("Assets/Dutz Authoring/Station I am baby On Level07 Straight3 Mid")]
    public static void StationFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station I am baby On Level07 Straight3 Mid requires Edit Mode.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station I am baby on Level07 Highway Straight 3 mid.");
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
            giant = DutzGiantBossNames.FindIAmBaby();
        if (giant == null)
        {
            Debug.LogError($"[Dutz] '{GiantName}' not found in Level07.");
            return false;
        }

        var straight3 = GameObject.Find(Straight3Name);
        if (straight3 == null)
        {
            Debug.LogError($"[Dutz] '{Straight3Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(giant.transform, "Station I am baby On Straight3 Mid");

        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        if (!TryResolveMidDeckPoint(straight3.transform, out var deckPoint, out var deckUp))
        {
            Debug.LogError("[Dutz] Could not sample Straight 3 mid deck for I am baby.");
            return false;
        }

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(giant);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 3.5f);
        var pivot = deckPoint + deckUp * pivotToFeet;

        // Face downslope so she looks toward players climbing Straight 3.
        var downslope = -straight3.transform.forward;
        downslope.y = 0f;
        if (downslope.sqrMagnitude < 0.0001f)
            downslope = Vector3.left;
        downslope.Normalize();
        var rotation = Quaternion.LookRotation(downslope, Vector3.up);

        giant.transform.SetPositionAndRotation(pivot, rotation);
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station I am baby Rigidbody");
            rb.position = pivot;
            rb.rotation = rotation;
            rb.isKinematic = true;
            rb.useGravity = false;
            EditorUtility.SetDirty(rb);
        }

        EnableBehaviour<SimpleCitizensGiantHippieHunter>(giant);
        EnableBehaviour<SimpleCitizensNpcPhysics>(giant);
        EnableBehaviour<DutzGiantHeat>(giant);

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

        Undo.RecordObject(respawn, "Bake I am baby Straight3 Spawn");
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(giant);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = giant;

        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {GiantName} at mid of {Straight3Name} at {pivot} " +
                $"(pitched deck, downslope face, chase/burn kept, spawn baked).");
        }

        return true;
    }

    static bool TryResolveMidDeckPoint(Transform road, out Vector3 deckPoint, out Vector3 deckUp)
    {
        deckPoint = road.position;
        deckUp = road.up.normalized;
        if (deckUp.y < 0f)
            deckUp = -deckUp;

        var col = road.GetComponent<MeshCollider>();
        var mesh = col != null ? col.sharedMesh : null;
        if (mesh == null)
            return false;

        var b = mesh.bounds;
        var localX = b.center.x;
        var localZ = b.center.z;
        var localY = b.max.y;
        var seed = road.TransformPoint(new Vector3(localX, localY, localZ));

        if (!DutzRoadGround.TrySampleLevel07Straight3DeckPoint(seed, out deckPoint, out deckUp))
            return false;

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
