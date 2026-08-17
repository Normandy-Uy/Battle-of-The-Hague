using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Moves DutzForceField off Player1 onto the Force Field Suit pickup.</summary>
public static class DutzForceFieldSuitHostFixer
{
    [MenuItem("Assets/Dutz Authoring/Host Force Field On Suit Pickup")]
    public static void HostFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Dutz] Host Force Field On Suit Pickup requires Edit Mode.");
            return;
        }

        if (!Host(log: true))
            Debug.LogError("[Dutz] Failed to host Force Field on the suit pickup.");
    }

    public static bool Host(bool log)
    {
        var suit = GameObject.Find(DutzForceFieldSuitPickup.PickupObjectName);
        if (suit == null)
        {
            if (log)
                Debug.LogError("[Dutz] DutzForceFieldSuit not found in the open scene.");
            return false;
        }

        if (suit.GetComponent<DutzForceField>() == null)
            Undo.AddComponent<DutzForceField>(suit);

        DutzForceField.StripFromPlayers();

        var scene = suit.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log("[Dutz] DutzForceField hosted on suit pickup; stripped from Player1.");

        return true;
    }
}
