using UnityEngine;

/// <summary>
/// Burns Flood Control Player1 while in physical contact with this pipe.
/// </summary>
[DisallowMultipleComponent]
public class PipeBurn : MonoBehaviour
{
    [Header("Burn")]
    [Tooltip("Hit points removed from the player each second while touching this pipe.")]
    [SerializeField] float burnPerSecond = 20f;

    public float BurnPerSecond => burnPerSecond;

    public void Configure(float burnRate)
    {
        burnPerSecond = Mathf.Max(0f, burnRate);
    }

    void OnCollisionStay(Collision collision)
    {
        ApplyBurn(collision != null ? collision.collider : null);
    }

    void OnTriggerStay(Collider other)
    {
        ApplyBurn(other);
    }

    void ApplyBurn(Collider other)
    {
        if (!enabled || other == null || burnPerSecond <= 0f)
            return;

        FloodPlayerHealth health = other.GetComponentInParent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;

        health.ApplyBurnDamage(burnPerSecond);
    }

    void OnValidate()
    {
        burnPerSecond = Mathf.Max(0f, burnPerSecond);
    }
}
