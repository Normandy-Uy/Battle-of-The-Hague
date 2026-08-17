using UnityEngine;

/// <summary>
/// Player avatar identity (Roblox-style mophead on the Dutz rig).
/// </summary>
[DisallowMultipleComponent]
public class DutzNPC : MonoBehaviour
{
    [SerializeField] string displayName = "Mophead";
    [SerializeField] Transform head;
    [SerializeField] Transform nose;

    public string DisplayName => displayName;
    public Transform Head => head;
    public Transform Nose => nose;

    public void ConfigureDisplayName(string name)
    {
        if (!string.IsNullOrEmpty(name))
            displayName = name;
    }

    public void SetHeadLookAt(Vector3 worldTarget)
    {
        if (head == null) return;
        var dir = worldTarget - head.position;
        dir.y *= 0.35f;
        if (dir.sqrMagnitude < 0.001f) return;
        head.rotation = Quaternion.Slerp(head.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * 4f);
    }

#if UNITY_EDITOR
    public void BindReferences(Transform headTransform, Transform noseTransform, string name = null)
    {
        head = headTransform;
        nose = noseTransform;
        if (!string.IsNullOrEmpty(name))
            displayName = name;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
