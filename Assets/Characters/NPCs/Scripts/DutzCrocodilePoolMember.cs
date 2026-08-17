using UnityEngine;

/// <summary>Level 2 segment-pool crocodiles built from Crocodile.fbx under a CrocVisual child.</summary>
public static class DutzCrocodilePoolMember
{
    public const string VisualChildName = "CrocVisual";
    const float SceneFbxVisualScale = 6f;
    const float BodyScale = 1.75f;

    public static bool IsCrocodile(GameObject go) =>
        go != null && go.transform.Find(VisualChildName) != null;

    public static float TargetVisualScale => SceneFbxVisualScale / BodyScale;

    public static void EnsureCrocodileScale()
    {
        var pool = GameObject.Find(DutzSegmentHippieIdentity.PoolRootName);
        if (pool == null)
            return;

        var targetScale = Vector3.one * TargetVisualScale;
        foreach (Transform child in pool.transform)
        {
            if (child == null || !IsCrocodile(child.gameObject))
                continue;

            var visual = child.Find(VisualChildName);
            if (visual == null)
                continue;

            visual.localScale = targetScale;
            RefreshCombatColliders(child.gameObject);
        }
    }

    public static void RefreshCombatColliders(GameObject root)
    {
        if (root == null || !IsCrocodile(root))
            return;

        DutzHippieBiteCollider.EnsureCrocodileColliders(root);

        var biter = root.GetComponent<SimpleCitizensHippieBiter>();
        biter?.RefreshContactColliders();
    }
}
