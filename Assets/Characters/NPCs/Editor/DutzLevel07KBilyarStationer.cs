using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations K Bilyar at the top (highest) end of Level07 Highway Straight 2 —
/// pitched deck clamp, keeps chase/burn/HP, bakes spawn.
/// </summary>
public static class DutzLevel07KBilyarStationer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Straight2Name = "Highway Straight 2";
    const string GiantName = "K Bilyar";
    const float LocalAxisInset = 0.08f;

    [MenuItem("Assets/Dutz Authoring/Station K Bilyar On Level07 Straight2 Top")]
    public static void StationFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Station K Bilyar On Level07 Straight2 Top requires Edit Mode.");
            return;
        }

        if (!StationSilent(log: true))
            Debug.LogError("[Dutz] Failed to station K Bilyar on Level07 Highway Straight 2 top.");
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
            giant = DutzGiantBossNames.FindKBilyar();
        if (giant == null)
        {
            Debug.LogError($"[Dutz] '{GiantName}' not found in Level07.");
            return false;
        }

        var straight2 = GameObject.Find(Straight2Name);
        if (straight2 == null)
        {
            Debug.LogError($"[Dutz] '{Straight2Name}' not found in Level07.");
            return false;
        }

        Undo.RecordObject(giant.transform, "Station K Bilyar On Straight2 Top");

        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Undo.DestroyObjectImmediate(stationary);

        if (!TryResolveTopDeckPoint(straight2.transform, out var deckPoint, out var deckUp))
        {
            Debug.LogError("[Dutz] Could not sample Straight 2 top deck for K Bilyar.");
            return false;
        }

        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(giant);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 3.5f);
        var pivot = deckPoint + deckUp * pivotToFeet;

        // Face downslope so she looks toward players climbing Straight 2.
        var downslope = -straight2.transform.forward;
        downslope.y = 0f;
        if (downslope.sqrMagnitude < 0.0001f)
            downslope = Vector3.left;
        downslope.Normalize();
        var rotation = Quaternion.LookRotation(downslope, Vector3.up);

        giant.transform.SetPositionAndRotation(pivot, rotation);
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            Undo.RecordObject(rb, "Station K Bilyar Rigidbody");
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
            hunterSo.FindProperty("huntImmediately").boolValue = true;
            hunterSo.FindProperty("chaseSpeed").floatValue = 25f;
            hunterSo.FindProperty("chaseAnimSpeed").floatValue = 1.7045455f;
            hunterSo.FindProperty("chaseStopDistance").floatValue = 2.5f;
            hunterSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hunter);
        }

        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.Apply();
            physics.ConfigureForChase(25f, 1.7045455f, 2.5f);
            physics.SetWalkingEnabled(true);
            EditorUtility.SetDirty(physics);
        }

        var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = Undo.AddComponent<SimpleCitizensNpcRespawn>(giant);

        Undo.RecordObject(respawn, "Bake K Bilyar Straight2 Spawn");
        respawn.SetLockedSpawnPoint(pivot, rotation);
        EditorUtility.SetDirty(respawn);
        EditorUtility.SetDirty(giant);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = giant;

        if (log)
        {
            Debug.Log(
                $"[Dutz] Stationed {GiantName} at top of {Straight2Name} at {pivot} " +
                $"(pitched deck, downslope face, chase/burn kept, spawn baked).");
        }

        return true;
    }

    static bool TryResolveTopDeckPoint(Transform road, out Vector3 deckPoint, out Vector3 deckUp)
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
        var minZ = Mathf.Lerp(b.min.z, b.max.z, LocalAxisInset);
        var maxZ = Mathf.Lerp(b.min.z, b.max.z, 1f - LocalAxisInset);
        if (minZ > maxZ)
        {
            minZ = b.center.z;
            maxZ = b.center.z;
        }

        var localX = b.center.x;
        var localY = b.max.y;

        var seedMin = road.TransformPoint(new Vector3(localX, localY, minZ));
        var seedMax = road.TransformPoint(new Vector3(localX, localY, maxZ));

        if (!DutzRoadGround.TrySampleLevel07Straight2DeckPoint(seedMin, out var deckMin, out var upMin))
            return false;
        if (!DutzRoadGround.TrySampleLevel07Straight2DeckPoint(seedMax, out var deckMax, out var upMax))
            return false;

        if (deckMax.y >= deckMin.y)
        {
            deckPoint = deckMax;
            deckUp = upMax;
        }
        else
        {
            deckPoint = deckMin;
            deckUp = upMin;
        }

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
