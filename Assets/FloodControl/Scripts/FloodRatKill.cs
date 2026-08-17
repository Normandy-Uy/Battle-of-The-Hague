using UnityEngine;

/// <summary>
/// Kills Flood Control Player1 when a rat trigger touches the player.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodRatKill : MonoBehaviour
{
    const string DeathTitle = "A RAT TOUCHED YOU. YOU DIED OF LEPTOSPIROSIS.";

    void OnTriggerEnter(Collider other)
    {
        TryKill(other);
    }

    void TryKill(Collider other)
    {
        if (!enabled || other == null)
            return;

        // Require the player's root capsule / health collider — not a loose child volume.
        FloodPlayerHealth health = other.GetComponent<FloodPlayerHealth>();
        if (health == null || health.IsDead)
            return;

        health.KillWithDialog(DeathTitle, string.Empty);
    }
}
