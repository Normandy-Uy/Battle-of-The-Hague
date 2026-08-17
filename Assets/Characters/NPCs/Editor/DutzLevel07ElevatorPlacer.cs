using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places Level07 elevators on highway bridge walkable decks (mid-span, under the beams).
/// </summary>
public static class DutzLevel07ElevatorPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string Bridge1Name = "Highway Bridge 1";
    const string Bridge4Name = "Highway Bridge 4";
    const string Bridge5Name = "Highway Bridge 5";
    const string ElevatorName = "Elevator";
    const string Elevator1Name = "Elevator 1";
    const string Elevator4Name = "Elevator 4";
    const float MidAlongFraction = 0.5f;
    const float DeckClearanceMeters = 0.05f;

    [MenuItem("Assets/Dutz Authoring/Place Elevator On Level07 Bridge5 Mid Deck")]
    public static void PlaceBridge5FromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Elevator On Level07 Bridge5 Mid Deck requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(ElevatorName, Bridge5Name, log: true))
            Debug.LogError("[Dutz] Failed to place Elevator on Level07 Highway Bridge 5 mid deck.");
    }

    [MenuItem("Assets/Dutz Authoring/Place Elevator 1 On Level07 Bridge1 Mid Deck")]
    public static void PlaceElevator1Bridge1FromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Elevator 1 On Level07 Bridge1 Mid Deck requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(Elevator1Name, Bridge1Name, log: true))
            Debug.LogError("[Dutz] Failed to place Elevator 1 on Level07 Highway Bridge 1 mid deck.");
    }

    [MenuItem("Assets/Dutz Authoring/Place Elevator 4 On Level07 Bridge4 Mid Deck")]
    public static void PlaceElevator4Bridge4FromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Elevator 4 On Level07 Bridge4 Mid Deck requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(Elevator4Name, Bridge4Name, log: true))
            Debug.LogError("[Dutz] Failed to place Elevator 4 on Level07 Highway Bridge 4 mid deck.");
    }

    [MenuItem("Assets/Dutz Authoring/Fix Level07 Elevator Stand Collider + Patrol")]
    public static void FixColliderFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Fix Level07 Elevator Stand Collider requires Edit Mode.");
            return;
        }

        var fixedAny = false;
        foreach (var name in new[] { Elevator1Name, Elevator4Name, ElevatorName })
        {
            var elevator = GameObject.Find(name);
            if (elevator == null)
                continue;

            var patrol = elevator.GetComponent<DutzElevatorVerticalPatrol>();
            if (patrol == null)
                patrol = Undo.AddComponent<DutzElevatorVerticalPatrol>(elevator);

            Undo.RecordObject(elevator, "Fix Elevator Stand Collider");
            patrol.EnsureStandableCollider();
            EditorUtility.SetDirty(elevator);
            fixedAny = true;
            Debug.Log($"[Dutz] Fixed standable collider on '{name}'.");
        }

        if (!fixedAny)
        {
            Debug.LogError("[Dutz] No Elevator / Elevator 4 found.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    public static bool PlaceSilent(bool log) => PlaceSilent(ElevatorName, Bridge5Name, log);

    public static bool PlaceSilent(string elevatorName, string bridgeName, bool log)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return false;

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();

        var elevator = GameObject.Find(elevatorName);
        if (elevator == null)
        {
            if (log)
                Debug.LogError($"[Dutz] '{elevatorName}' not found in Level07.");
            return false;
        }

        var bridge = GameObject.Find(bridgeName);
        if (bridge == null)
        {
            if (log)
                Debug.LogError($"[Dutz] '{bridgeName}' not found in Level07.");
            return false;
        }

        var spawn = GetPlayerSpawn();
        var travelForward = GetTravelForward(spawn);
        var path = DutzHighwayDeckSampler.BuildSegmentPath(bridge, bridgeName, spawn, travelForward);
        if (path.Samples == null || path.Samples.Count == 0)
        {
            if (log)
                Debug.LogError($"[Dutz] No deck samples for {bridgeName}.");
            return false;
        }

        if (!DutzHighwayDeckSampler.TrySampleOnPath(path.Samples, MidAlongFraction, out var sample))
        {
            if (log)
                Debug.LogError($"[Dutz] Could not sample mid deck of {bridgeName}.");
            return false;
        }

        var deck = sample.Position;
        var probe = deck + Vector3.up * 40f;
        if (DutzRoadGround.TrySampleLevel07NamedHighwayDeckPoint(
                bridgeName, deck, out var namedDeck, out _))
            deck = namedDeck;
        else if (DutzRoadGround.TrySampleWalkableRoadDeckY(probe, deck.y, null, out var deckY)
                 || DutzRoadGround.TrySampleSurfaceY(probe, null, out deckY))
            deck.y = deckY;

        // Prefer Liron deck height on Bridge 4 — true walkable ribbon under mid beams.
        if (bridgeName == Bridge4Name)
        {
            var liron = GameObject.Find("Liron Sinta") ?? DutzGiantBossNames.FindLironSinta();
            if (liron != null)
                deck.y = liron.transform.position.y;
        }

        var forward = sample.Forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = travelForward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = bridge.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        forward.Normalize();

        var position = deck + Vector3.up * DeckClearanceMeters;
        var rotation = Quaternion.LookRotation(forward, Vector3.up);

        Undo.RecordObject(elevator.transform, $"Place {elevatorName} On {bridgeName} Mid Deck");
        elevator.transform.SetPositionAndRotation(position, rotation);

        var patrol = elevator.GetComponent<DutzElevatorVerticalPatrol>();
        if (patrol == null)
            patrol = Undo.AddComponent<DutzElevatorVerticalPatrol>(elevator);
        patrol.EnsureStandableCollider();

        var so = new SerializedObject(patrol);
        so.FindProperty("suitcaseCost").intValue = DutzElevatorVerticalPatrol.SuitcaseFare;

        // All Level07 elevators use the same pay-before-start gate.
        so.FindProperty("requirePayDialogToStart").boolValue = true;
        so.FindProperty("chargeSuitcasesOnLand").boolValue = false;

        // Absolute world Y ceiling (Unity units) — no parachute Transform reference.
        var maxWorldY = 0f;
        if (bridgeName == Bridge4Name || bridgeName == Bridge5Name)
        {
            var parachuteName = bridgeName == Bridge4Name
                ? "DutzParachutePickup_Bridge4"
                : "DutzParachutePickup_Bridge5";
            var parachute = GameObject.Find(parachuteName);
            if (parachute != null)
            {
                var top = parachute.transform.position.y;
                if (parachute.TryGetComponent<Renderer>(out var pr))
                    top = pr.bounds.max.y;
                if (top > position.y + 5f)
                    maxWorldY = top + 6f;
            }
        }
        else if (bridge.TryGetComponent<Renderer>(out var bridgeRenderer))
        {
            maxWorldY = bridgeRenderer.bounds.max.y + 6f;
        }

        so.FindProperty("maxHeightWorldY").floatValue = maxWorldY;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log(
                $"[Dutz] {elevatorName} on {bridgeName} mid deck under beams at {position}.");

        return true;
    }

    static Vector3 GetPlayerSpawn()
    {
        foreach (var player in Object.FindObjectsOfType<DutzPlayerController>(true))
        {
            var so = new SerializedObject(player);
            return so.FindProperty("spawnPosition").vector3Value;
        }

        return Vector3.zero;
    }

    static Vector3 GetTravelForward(Vector3 spawn)
    {
        if (DutzHighwayDirection.TryGetTrackProgressForward(out var progress)
            && progress.sqrMagnitude > 0.0001f)
            return progress.normalized;

        return Vector3.right;
    }
}
