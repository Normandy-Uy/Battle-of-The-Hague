using System.Collections;
using UnityEngine;

/// <summary>
/// Spreads showcase NPC setup across frames on phones (one big AfterSceneLoad pass was crashing at splash).
/// </summary>
public class DutzMobileShowcaseDeferredBootstrap : MonoBehaviour
{
    const int NpcsPerFrame = 1;

    public static bool IsFinished { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Kickoff()
    {
        var existing = GameObject.Find(nameof(DutzMobileShowcaseDeferredBootstrap));
        if (existing != null)
            Destroy(existing);

        if (!DutzMobileRuntime.ShouldDeferNpcBootstrap)
        {
            IsFinished = true;
            return;
        }

        IsFinished = false;
        var go = new GameObject(nameof(DutzMobileShowcaseDeferredBootstrap));
        DontDestroyOnLoad(go);
        go.AddComponent<DutzMobileShowcaseDeferredBootstrap>();
    }

    IEnumerator Start()
    {
        DutzAndroidBootLog.Write("Showcase deferred NPC bootstrap started");
        DutzBootOverlay.SetStatus("Showcase loading NPCs…");
        yield return null;

        var physics = FindObjectsOfType<SimpleCitizensNpcPhysics>();
        for (var i = 0; i < physics.Length; i++)
        {
            var npc = physics[i];
            if (npc == null)
                continue;

            SimpleCitizensHippieHunter.EnsureOnNpc(npc);
            SimpleCitizensFlyingHippieHunter.EnsureOnNpc(npc);
            SimpleCitizensGiantHippieHunter.EnsureOnNpc(npc);
            SimpleCitizensGiantHippieHunter.EnsureTrililingColliderOnNpc(npc);
            SimpleCitizensHippieBiter.EnsureOnNpc(npc);
            SimpleCitizensHippieSounds.EnsureOnNpc(npc);
            SimpleCitizensNpcRespawn.EnsureOnNpc(npc);

            if (i > 0 && i % NpcsPerFrame == 0)
            {
                DutzBootOverlay.SetStatus($"Showcase NPCs {i}/{physics.Length}");
                yield return null;
            }
        }

        DutzAndroidBootLog.Write("Showcase deferred NPC bootstrap finished");
        DutzBootOverlay.SetStatus("Showcase NPCs ready");
        DutzJonremPoliceBehavior.EnsureFromBoot();
        if (DutzCollectibleProgress.IsLevel00)
        {
            DutzLevel00StaticCrowdColliders.EnsureInOpenScene(log: false);
            DutzLevel00CrowdCrossroadRespawn.EnsureFromBoot();
        }
        IsFinished = true;
    }
}
