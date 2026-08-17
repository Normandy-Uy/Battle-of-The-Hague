using UnityEngine;

/// <summary>
/// Raycast audio occlusion from the listener to a world point (walls block, open air passes).
/// Does not use renderer visibility — off-screen hippies are audible when unobstructed.
/// </summary>
public static class DutzAudioOcclusion
{
    const float ListenerEarHeight = 1.55f;
    const float SourceMouthHeight = 1.35f;
    const float RayEndInset = 0.35f;
    const float MuffledAudibility = 0.22f;

    public struct Sample
    {
        public float audibility;
        public bool lineClear;
    }

    public static Sample Evaluate(Transform sourceRoot, Vector3 sourceWorldPosition)
    {
        if (!TryGetListenerPosition(out var listenerPos))
            return new Sample { audibility = 1f, lineClear = true };

        var source = sourceWorldPosition + Vector3.up * SourceMouthHeight;
        var toSource = source - listenerPos;
        var distance = toSource.magnitude;
        if (distance < 0.05f)
            return new Sample { audibility = 1f, lineClear = true };

        var direction = toSource / distance;
        if (!Physics.Raycast(listenerPos, direction, out var hit, distance - RayEndInset, ~0, QueryTriggerInteraction.Ignore))
            return new Sample { audibility = 1f, lineClear = true };

        if (IsIgnoredOccluder(hit.collider, sourceRoot))
            return new Sample { audibility = 1f, lineClear = true };

        return new Sample { audibility = MuffledAudibility, lineClear = false };
    }

    public static bool TryGetListenerPosition(out Vector3 position)
    {
        position = default;

        var listener = Object.FindObjectOfType<AudioListener>();
        if (listener != null)
        {
            position = listener.transform.position;
            return true;
        }

        var cam = Camera.main;
        if (cam != null)
        {
            position = cam.transform.position;
            return true;
        }

        var player = DutzPlayerController.Instance;
        if (player == null)
            return false;

        position = player.transform.position + Vector3.up * ListenerEarHeight;
        return true;
    }

    static bool IsIgnoredOccluder(Collider col, Transform sourceRoot)
    {
        if (col == null)
            return true;

        if (sourceRoot != null && (col.transform == sourceRoot || col.transform.IsChildOf(sourceRoot)))
            return true;

        if (col.GetComponent<DutzPlayerController>() != null || col.GetComponentInParent<DutzPlayerController>() != null)
            return true;

        if (col.GetComponent<SimpleCitizensHippieBiter>() != null || col.GetComponentInParent<SimpleCitizensHippieBiter>() != null)
            return true;

        if (col.GetComponent<SimpleCitizensHippieSounds>() != null || col.GetComponentInParent<SimpleCitizensHippieSounds>() != null)
            return true;

        return false;
    }
}
