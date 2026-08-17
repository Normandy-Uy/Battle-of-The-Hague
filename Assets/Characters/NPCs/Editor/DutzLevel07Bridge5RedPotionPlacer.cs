using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Stations the +100 HP red health potion on the topmost surface of Level07 Highway Bridge 5
/// and bakes its spawn pose so respawns keep it there.
/// </summary>
public static class DutzLevel07Bridge5RedPotionPlacer
{
    const string Level07Path = "Assets/Scenes/Dutz_Level07.unity";
    const string BridgeName = "Highway Bridge 5";
    const float LiftAboveSurfaceMeters = 1.5f;
    static readonly Vector3 Level07PotionScale = new Vector3(10f, 10f, 10f);

    [MenuItem("Assets/Dutz Authoring/Place Red Potion On Level07 Bridge5 Top")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Place Red Potion On Level07 Bridge5 Top requires Edit Mode.");
            return;
        }

        if (!PlaceSilent(log: true))
            Debug.LogError("[Dutz] Failed to place red potion on Level07 Highway Bridge 5.");
    }

    public static bool PlaceSilent(bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path.Replace('\\', '/') != Level07Path)
            scene = EditorSceneManager.OpenScene(Level07Path, OpenSceneMode.Single);

        Physics.SyncTransforms();

        var bridge = GameObject.Find(BridgeName);
        if (bridge == null)
        {
            Debug.LogError($"[Dutz] '{BridgeName}' not found in Level07.");
            return false;
        }

        var potion = GameObject.Find(DutzHealthPotion.Bridge5RedPotionName);
        if (potion == null)
        {
            Debug.LogError($"[Dutz] '{DutzHealthPotion.Bridge5RedPotionName}' not found in Level07.");
            return false;
        }

        if (!TryFindBridgeTopPoint(bridge, out var topPoint))
        {
            Debug.LogError($"[Dutz] Could not sample a top surface on {BridgeName}.");
            return false;
        }

        var position = topPoint + Vector3.up * LiftAboveSurfaceMeters;

        Undo.RecordObject(potion.transform, "Place Red Potion On Bridge 5 Top");
        potion.transform.SetPositionAndRotation(position, Quaternion.identity);
        potion.transform.localScale = Level07PotionScale;

        var component = potion.GetComponent<DutzHealthPotion>();
        if (component != null)
        {
            Undo.RecordObject(component, "Bake Red Potion Spawn Pose");
            component.CaptureSpawnPoseFromTransform(force: true);
            EditorUtility.SetDirty(component);
        }

        DutzHealthPotionSetup.ApplyRedVisual(potion);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Red potion (+{DutzHealthPotion.Bridge5RedHealAmount} HP) placed on top of {BridgeName} at {position}.");

        return true;
    }

    /// <summary>Grid-raycast over the bridge AABB; highest upward-facing hit on the bridge collider wins.</summary>
    static bool TryFindBridgeTopPoint(GameObject bridge, out Vector3 topPoint)
    {
        topPoint = default;

        var col = bridge.GetComponent<Collider>();
        if (col == null)
            col = bridge.GetComponentInChildren<Collider>();
        if (col == null)
            return false;

        var bounds = col.bounds;
        const int stepsX = 24;
        const int stepsZ = 10;
        var castTop = bounds.max.y + 25f;
        var castDist = bounds.size.y + 50f;
        var bestY = float.NegativeInfinity;
        var found = false;

        for (var ix = 0; ix <= stepsX; ix++)
        {
            for (var iz = 0; iz <= stepsZ; iz++)
            {
                var x = Mathf.Lerp(bounds.min.x, bounds.max.x, ix / (float)stepsX);
                var z = Mathf.Lerp(bounds.min.z, bounds.max.z, iz / (float)stepsZ);
                var origin = new Vector3(x, castTop, z);

                var hits = Physics.RaycastAll(origin, Vector3.down, castDist, ~0, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.collider == null)
                        continue;
                    if (hit.collider.transform != bridge.transform
                        && !hit.collider.transform.IsChildOf(bridge.transform))
                        continue;
                    if (hit.normal.y < 0.5f)
                        continue;
                    if (hit.point.y <= bestY)
                        continue;

                    bestY = hit.point.y;
                    topPoint = hit.point;
                    found = true;
                }
            }
        }

        return found;
    }
}
