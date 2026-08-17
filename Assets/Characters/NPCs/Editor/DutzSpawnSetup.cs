using UnityEditor;
using UnityEngine;

public static class DutzSpawnSetup
{
    public static void ApplyInspectorSpawnToDutz()
    {
        var player = DutzEditorHelpers.FindPrimaryDutzPlayer();
        if (player == null)
        {
            Debug.LogError("[Dutz] No DutzPlayerController in scene.");
            return;
        }

        ApplySpawnAtInspectorPosition(player);
        Debug.Log($"[Dutz] Applied inspector spawn {GetSpawnPosition(player)} to Dutz in scene.");
    }

    public static bool SnapSpawnFieldsToBridgeStart(string requiredScenePath = null, bool logErrors = true)
    {
        if (!string.IsNullOrEmpty(requiredScenePath))
        {
            var active = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (active.path != requiredScenePath)
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                    requiredScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Physics.SyncTransforms();
        }

        DutzHighwayDirection.InvalidateReferenceCache();
        if (!DutzHighwayDirection.TryGetTrackStartSpawnPosition(out var spawn, out _))
        {
            if (logErrors)
                Debug.LogError("[Dutz] Highway Bridge 1 not found — cannot snap spawn fields.");
            return false;
        }

        var player = DutzEditorHelpers.FindPrimaryDutzPlayer();
        if (player == null)
        {
            if (logErrors)
                Debug.LogError("[Dutz] No DutzPlayerController in scene.");
            return false;
        }

        var so = new SerializedObject(player);
        so.FindProperty("spawnPosition").vector3Value = spawn;
        so.ApplyModifiedPropertiesWithoutUndo();

        ApplySpawnAtInspectorPosition(player);
        Debug.Log($"[Dutz] Inspector spawnPosition set to Highway Bridge 1 start: {GetSpawnPosition(player)}");
        return true;
    }

    public static void SnapSpawnFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Snap Player Spawn", "Exit Play mode first.", "OK");
            return;
        }

        if (!SnapSpawnFieldsToBridgeStart())
            EditorUtility.DisplayDialog(
                "Snap Player Spawn",
                "Could not snap Player1 to Highway Bridge 1. Check the Console.",
                "OK");
    }

    static void ApplySpawnAtInspectorPosition(DutzPlayerController player)
    {
        var spawn = GetSpawnPosition(player);
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        player.transform.position = spawn;
        if (DutzRoadGround.TrySampleWalkableRoadDeckY(spawn, spawn.y, cc, out var deckY))
            DutzNpcFeet.PlacePivotOnSurface(player.gameObject, deckY);

        DutzHighwayDirection.InvalidateReferenceCache();
        var soFacing = new SerializedObject(player);
        var invert = soFacing.FindProperty("invertSpawnFacing").boolValue;
        soFacing.Dispose();

        var forward = ResolveEditorSpawnFacing(spawn, invert);
        if (forward.sqrMagnitude > 0.0001f)
            player.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        var cam = Camera.main;
        if (cam != null && forward.sqrMagnitude > 0.0001f)
        {
            var follow = cam.GetComponent<DutzCameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<DutzCameraFollow>();
            follow.enabled = true;
            follow.BindTarget(player.transform);
            follow.SnapRobloxSpawnFacing(forward);
        }

        if (cc != null)
            cc.enabled = true;

        PersistSpawnFields(player);

        if (!EditorApplication.isPlaying)
        {
            EditorUtility.SetDirty(player);
            if (player.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        }
    }

    static void PersistSpawnFields(DutzPlayerController player)
    {
        var so = new SerializedObject(player);
        so.FindProperty("spawnPosition").vector3Value = player.transform.position;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
        }
    }

    static Vector3 GetSpawnPosition(DutzPlayerController player)
    {
        var so = new SerializedObject(player);
        return so.FindProperty("spawnPosition").vector3Value;
    }

    internal static Vector3 ResolveEditorSpawnFacing(Vector3 spawn, bool invertSpawnFacing)
    {
        const float trackStartRadius = 120f;
        var hasTrackStart = DutzHighwayDirection.TryGetTrackStartSpawnPosition(
            out var trackStart, out _);

        if (hasTrackStart
            && (spawn - trackStart).sqrMagnitude <= trackStartRadius * trackStartRadius
            && DutzHighwayDirection.TryGetTrackProgressForward(out var progressForward))
            return progressForward;

        var forward = DutzHighwayDirection.GetSpawnForwardAt(spawn);
        if (forward.sqrMagnitude < 0.0001f)
            forward = DutzHighwayDirection.GetReferenceForward();
        if (forward.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        forward.Normalize();
        if (invertSpawnFacing)
            forward = -forward;

        return forward;
    }
}
