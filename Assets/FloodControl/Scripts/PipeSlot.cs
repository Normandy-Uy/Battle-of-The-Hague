using UnityEngine;

/// <summary>
/// Placeholder spawn anchors for one Flappy-style pipe gap.
/// Actual pipe prefabs are assigned later.
/// </summary>
[DisallowMultipleComponent]
public class PipeSlot : MonoBehaviour
{
    [SerializeField] Transform topSpawn;
    [SerializeField] Transform bottomSpawn;

    public Transform TopSpawn => topSpawn;
    public Transform BottomSpawn => bottomSpawn;

    public void SetSpawnReferences(Transform top, Transform bottom)
    {
        topSpawn = top;
        bottomSpawn = bottom;
    }

    /// <summary>
    /// Places top/bottom anchors around a gap centre at the given half-gap distance.
    /// </summary>
    public void ApplyGap(float centreY, float gapSize)
    {
        float halfGap = Mathf.Max(0.05f, gapSize * 0.5f);

        if (topSpawn != null)
        {
            Vector3 top = topSpawn.position;
            top.y = centreY + halfGap;
            topSpawn.position = top;
        }

        if (bottomSpawn != null)
        {
            Vector3 bottom = bottomSpawn.position;
            bottom.y = centreY - halfGap;
            bottomSpawn.position = bottom;
        }
    }

    void OnValidate()
    {
        if (topSpawn == null)
        {
            Transform found = transform.Find("TopSpawn");
            if (found != null)
                topSpawn = found;
        }

        if (bottomSpawn == null)
        {
            Transform found = transform.Find("BottomSpawn");
            if (found != null)
                bottomSpawn = found;
        }
    }
}
