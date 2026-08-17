using UnityEngine;

/// <summary>Authoritative spawn pose for coins/suitcases — editable in the Inspector.</summary>
[System.Serializable]
public struct DutzCollectibleSpawnPose
{
    [Tooltip("Fixed world spawn position. Edit Y if the collectible is too high.")]
    public Vector3 position;

    [Tooltip("World rotation at spawn (degrees).")]
    public Vector3 eulerAngles;

    [Tooltip("Local scale at spawn.")]
    public Vector3 localScale;

    public static DutzCollectibleSpawnPose FromTransform(Transform transform)
    {
        if (transform == null)
            return default;

        return new DutzCollectibleSpawnPose
        {
            position = transform.position,
            eulerAngles = transform.eulerAngles,
            localScale = transform.localScale
        };
    }

    public void ApplyTo(Transform transform)
    {
        if (transform == null)
            return;

        var scale = localScale == Vector3.zero ? Vector3.one : localScale;
        transform.SetPositionAndRotation(position, Quaternion.Euler(eulerAngles));
        transform.localScale = scale;
    }

    public void ApplyPositionAndRotationTo(Transform transform)
    {
        if (transform == null)
            return;

        transform.SetPositionAndRotation(position, Quaternion.Euler(eulerAngles));
    }

    public bool HasPosition =>
        position != Vector3.zero || eulerAngles != Vector3.zero || localScale != Vector3.zero;
}
