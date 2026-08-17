using UnityEngine;

/// <summary>
/// Subtle idle motion — bobs head; optional nose wiggle when a nose transform exists.
/// </summary>
[RequireComponent(typeof(DutzNPC))]
public class DutzIdleBob : MonoBehaviour
{
    [SerializeField] float bobSpeed = 1.6f;
    [SerializeField] float bobAmount = 0.04f;
    [SerializeField] float noseWiggle = 3f;

    DutzNPC dutz;
    DutzPlayerController player;
    Transform head;
    Transform nose;
    Vector3 headBase;
    Vector3 noseBase;

    void Awake()
    {
        dutz = GetComponent<DutzNPC>();
        player = GetComponent<DutzPlayerController>();
        if (player == null)
            player = FindObjectOfType<DutzPlayerController>();
        head = dutz.Head;
        nose = dutz.Nose;
        if (head != null) headBase = head.localPosition;
        if (nose != null) noseBase = nose.localPosition;
    }

    void Update()
    {
        if (player != null && player.IsMoving)
            return;

        var t = Time.time * bobSpeed;
        if (head != null)
            head.localPosition = headBase + Vector3.up * (Mathf.Sin(t) * bobAmount);
        if (nose != null)
            nose.localEulerAngles = new Vector3(Mathf.Sin(t * 1.3f) * noseWiggle, 0f, 0f);
    }
}
