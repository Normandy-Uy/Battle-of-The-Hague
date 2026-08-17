using UnityEditor;
using UnityEngine;

/// <summary>
/// Shows how high the Super Jump pickup launches the player, computed from its launch velocity.
/// </summary>
[CustomEditor(typeof(DutzSuperJumpPickup))]
public class DutzSuperJumpPickupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var pickup = (DutzSuperJumpPickup)target;
        var height = pickup.EstimatedJumpHeightMeters;
        var normalJump = DutzPlayerController.EstimateJumpHeight(14f);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            $"Jump height: ~{height:0.0} m\n" +
            $"Launch velocity {pickup.SuperJumpForce:0.#} at gravity 20 (normal jump ≈ {normalJump:0.0} m).",
            MessageType.Info);
    }
}
