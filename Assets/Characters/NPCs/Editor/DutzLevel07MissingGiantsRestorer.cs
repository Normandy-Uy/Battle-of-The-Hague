using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Restores missing Level07 track giants (Cawetan, Gong Bong, MARKO LEKTA) after orphan cleanup.
/// </summary>
public static class DutzLevel07MissingGiantsRestorer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string TrackGiantsName = "DutzLevel03TrackGiants";
    const string CawetanPrefab = "Assets/Characters/Level07/Prefabs/Level07_Cawetan.prefab";
    const string GongBongPrefab = "Assets/Characters/Level07/Prefabs/Level07_GongBong.prefab";

    // Last known baked poses from MCP before they vanished.
    static readonly Vector3 MarkoPos = new Vector3(-1954.2f, 37.16173f, -143.2f);
    static readonly Quaternion MarkoRot = Quaternion.Euler(0f, 271.577637f, 0f);
    static readonly Vector3 CawetanPos = new Vector3(-1224.57043f, 57.0751648f, -103.342834f);
    static readonly Quaternion CawetanRot = Quaternion.Euler(0f, 271.235229f, 0f);
    static readonly Vector3 GongPos = new Vector3(-325.4899f, 408.6453f, -509.022156f);
    static readonly Quaternion GongRot = Quaternion.Euler(0f, 287.253754f, 0f);

    [MenuItem("Assets/Dutz Authoring/Restore Missing Level07 Track Giants")]
    public static void RestoreFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Restore Missing Level07 Track Giants requires Edit Mode.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        var parent = GameObject.Find(TrackGiantsName);
        if (parent == null)
        {
            Debug.LogError($"[Dutz] '{TrackGiantsName}' not found.");
            return;
        }

        var restored = 0;
        if (EnsureFromPrefab("Cawetan", CawetanPrefab, parent.transform, CawetanPos, CawetanRot))
            restored++;
        if (EnsureFromPrefab("Gong Bong", GongBongPrefab, parent.transform, GongPos, GongRot))
            restored++;
        if (EnsureMarkoFromClone(parent.transform))
            restored++;

        // Re-station onto decks (clamp + chase wiring).
        DutzLevel07CawetanStationer.StationSilent(log: true);
        DutzLevel07GongBongStationer.StationSilent(log: true);
        StationMarkoOnHighway8();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Dutz] Restored {restored} missing Level07 track giant(s) and saved scene.");
    }

    static bool EnsureFromPrefab(
        string giantName,
        string prefabPath,
        Transform parent,
        Vector3 pos,
        Quaternion rot)
    {
        if (GameObject.Find(giantName) != null)
        {
            Debug.Log($"[Dutz] '{giantName}' already present — skip prefab spawn.");
            return false;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Dutz] Missing prefab: {prefabPath}");
            return false;
        }

        var clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        PrefabUtility.UnpackPrefabInstance(
            clone,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);

        clone.name = giantName;
        clone.transform.SetParent(parent, true);
        clone.transform.SetPositionAndRotation(pos, rot);
        clone.transform.localScale = new Vector3(4f, 3f, 4f);

        StripStationaryLocks(clone);
        EnsureCoreCombat(clone, burn: giantName == "Cawetan");
        BakeSpawn(clone);

        Undo.RegisterCreatedObjectUndo(clone, $"Restore {giantName}");
        Debug.Log($"[Dutz] Restored '{giantName}' from prefab at {pos}.");
        return true;
    }

    static bool EnsureMarkoFromClone(Transform parent)
    {
        if (GameObject.Find("MARKO LEKTA") != null
            || DutzGiantBossNames.FindMarkoLekta() != null)
        {
            Debug.Log("[Dutz] 'MARKO LEKTA' already present — skip clone.");
            return false;
        }

        var template = GameObject.Find("M BILYAR");
        if (template == null)
            template = DutzGiantBossNames.FindMBilyar();
        if (template == null)
        {
            Debug.LogError("[Dutz] Cannot restore MARKO LEKTA — M BILYAR template missing.");
            return false;
        }

        var clone = Object.Instantiate(template);
        clone.name = "MARKO LEKTA";
        clone.transform.SetParent(parent, true);
        clone.transform.SetPositionAndRotation(MarkoPos, MarkoRot);
        clone.transform.localScale = new Vector3(4f, 3f, 4f);

        StripStationaryLocks(clone);
        EnsureCoreCombat(clone, burn: false);

        // Face + hunter speeds match prior Marko authoring.
        var face = clone.GetComponent<DutzGiantHippieBossFace>();
        if (face == null)
            face = clone.AddComponent<DutzGiantHippieBossFace>();
        face.ApplyFace();

        var hunter = clone.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            var so = new SerializedObject(hunter);
            so.FindProperty("wakeDistance").floatValue = 200f;
            so.FindProperty("huntImmediately").boolValue = false;
            so.FindProperty("chaseSpeed").floatValue = 19f;
            so.FindProperty("chaseAnimSpeed").floatValue = 1f;
            so.FindProperty("chaseStopDistance").floatValue = 2.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var physics = clone.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            var so = new SerializedObject(physics);
            so.FindProperty("walkSpeed").floatValue = 19f;
            so.FindProperty("animatorWalkSpeed").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        BakeSpawn(clone);
        Undo.RegisterCreatedObjectUndo(clone, "Restore MARKO LEKTA");
        Debug.Log($"[Dutz] Restored 'MARKO LEKTA' (cloned from M BILYAR) at {MarkoPos}.");
        return true;
    }

    static void StationMarkoOnHighway8()
    {
        var giant = GameObject.Find("MARKO LEKTA") ?? DutzGiantBossNames.FindMarkoLekta();
        if (giant == null)
            return;

        var pivot = MarkoPos;
        var pivotToFeet = DutzNpcFeet.GetPivotToFeetOffset(giant);
        pivotToFeet = Mathf.Clamp(pivotToFeet, 0.05f, 8f);
        if (DutzRoadGround.TryClampOntoLevel07Highway8Deck(ref pivot, pivotToFeet))
            giant.transform.position = pivot;

        giant.transform.rotation = MarkoRot;
        if (giant.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = giant.transform.position;
            rb.rotation = MarkoRot;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        BakeSpawn(giant);
        DutzLevel07MarkoLektaFaceApplier.ApplySilent(log: false);
    }

    static void StripStationaryLocks(GameObject giant)
    {
        var stationary = giant.GetComponent<DutzLevel07GiantStationary>();
        if (stationary != null)
            Object.DestroyImmediate(stationary);
        var grandma = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (grandma != null)
            Object.DestroyImmediate(grandma);
    }

    static void EnsureCoreCombat(GameObject giant, bool burn)
    {
        if (giant.GetComponent<SimpleCitizensGiantHippieHunter>() == null)
            giant.AddComponent<SimpleCitizensGiantHippieHunter>();
        if (giant.GetComponent<SimpleCitizensNpcPhysics>() == null)
            giant.AddComponent<SimpleCitizensNpcPhysics>();
        if (giant.GetComponent<DutzNpcHitPoints>() == null)
            giant.AddComponent<DutzNpcHitPoints>();

        var hp = giant.GetComponent<DutzNpcHitPoints>();
        if (hp != null)
        {
            var so = new SerializedObject(hp);
            so.FindProperty("maxHitPoints").intValue = 50;
            so.FindProperty("currentHitPoints").intValue = 50;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var heat = giant.GetComponent<DutzGiantHeat>();
        if (burn)
        {
            if (heat == null)
                heat = giant.AddComponent<DutzGiantHeat>();
            var so = new SerializedObject(heat);
            so.FindProperty("burnPerSecond").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else if (heat != null)
        {
            Object.DestroyImmediate(heat);
        }

        if (giant.GetComponent<DutzGiantHippieBossFace>() == null)
            giant.AddComponent<DutzGiantHippieBossFace>();
    }

    static void BakeSpawn(GameObject giant)
    {
        var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
        if (respawn == null)
            respawn = giant.AddComponent<SimpleCitizensNpcRespawn>();
        respawn.SetLockedSpawnPoint(giant.transform.position, giant.transform.rotation);
        EditorUtility.SetDirty(respawn);
    }
}
