using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central startup gate for Dutz_Level01 and Dutz_Level02.
/// Ordered boot steps run first; validation must pass before gameplay unlocks.
/// Disable Enter Play Mode Options → Disable Domain Reload unless testing — stale static
/// boot state from scattered hooks causes intermittent Editor failures.
/// </summary>
[DefaultExecutionOrder(-500)]
public class DutzGameBootstrap : MonoBehaviour
{
    public interface IBootStep
    {
        string Name { get; }
        bool Run(out string error);
    }

    static DutzGameBootstrap instance;
    static bool isReady;
    static bool hasFailed;
    static string lastError;

    public static bool IsReady => isReady;
    public static bool HasFailed => hasFailed;
    public static string LastError => lastError;
    public static event Action Ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RegisterSceneReset()
    {
        SceneManager.sceneLoaded -= OnDutzSceneLoaded;
        SceneManager.sceneLoaded += OnDutzSceneLoaded;
    }

    static void OnDutzSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!DutzMobileRuntime.IsDutzLevelScene(scene.name))
            return;

        PrepareForSceneLoad();
        DutzSceneBootstrapDefer.Run(StartBootstrapForScene);
    }

    static void StartBootstrapForScene()
    {
        var existing = GameObject.Find(nameof(DutzGameBootstrap));
        if (existing != null)
            Destroy(existing);

        instance = null;
        ResetState();

        var go = new GameObject(nameof(DutzGameBootstrap));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzGameBootstrap>();
    }

    /// <summary>Reset static gameplay flags before/after scene transitions within one play session.</summary>
    public static void PrepareForSceneLoad()
    {
        Time.timeScale = 1f;
        DutzVictoryVideoPlayback.ResetForSceneLoad();
        DutzLevelObjective.ResetStaticStateForNewScene();
        DutzForceFieldSuitPickup.ResetForSceneLoad();
        DutzSuperPunchPickup.ResetForSceneLoad();
        DutzSuperJumpPickup.ResetForSceneLoad();
        DutzLevel03Finale.ResetStaticStateForNewScene();
        DutzVictorySelfieProfile.ResetForSceneLoad();
        DutzPlayerLives.PrepareForSceneLoad();
        DutzLevel00WelcomeSplash.ResetForSceneLoad();
        DutzPoliceCaptureDialog.ResetForSceneLoad();
        DutzLevel07BoyIdolGate.ResetForSceneLoad();
        DutzVotesCounter.ResetForSceneLoad();
        DutzSuitcaseCounter.ResetForSceneLoad();
        DutzGoldCoinCounter.ResetForSceneLoad();
        DutzHighwayDirection.InvalidateTrackSegmentCache();
        DutzHighwayDirection.InvalidateReferenceCache();
        DutzBackgroundMusic.ResumeForSceneLoad();
    }

    static void ResetState()
    {
        isReady = false;
        hasFailed = false;
        lastError = null;
    }

    public static void RetrySceneLoad()
    {
        ResetState();
        DutzBootOverlay.DestroyInstance();
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator Start()
    {
        Time.timeScale = 1f;
        DutzBootOverlay.EnsureVisible();
        DutzBootOverlay.SetStatus("Loading scene…");

        yield return null;
        yield return null;
        Physics.SyncTransforms();

        DutzBootOverlay.SetStatus("Ensuring player…");
        if (DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name)
            && !DutzPlayerSpawn.EnsureInScene(out var spawnError))
        {
            Fail(spawnError);
            yield break;
        }

        yield return null;
        Physics.SyncTransforms();

        DutzBootOverlay.SetStatus("Locking player…");
        yield return LockPlayerWhenAvailable();

        DutzBootOverlay.SetStatus("Waiting for scene objects…");
        yield return null;
        Physics.SyncTransforms();

        var steps = BuildBootSteps();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            DutzBootOverlay.SetStatus($"Boot: {step.Name}…");
            yield return null;

            if (step.Run(out var bootError))
                continue;

            Fail(string.IsNullOrWhiteSpace(bootError)
                ? $"Boot step failed: {step.Name}"
                : $"{step.Name}: {bootError}");
            yield break;
        }

        if (DutzMobileRuntime.ShouldDeferNpcBootstrap)
        {
            DutzBootOverlay.SetStatus("Loading NPCs…");
            var timeout = DutzBootValidator.DeferredBootstrapTimeout;
            var elapsed = 0f;
            while (!DutzMobileShowcaseDeferredBootstrap.IsFinished && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!DutzMobileShowcaseDeferredBootstrap.IsFinished)
            {
                Fail("Mobile NPC bootstrap did not finish in time.");
                yield break;
            }
        }

        DutzBootOverlay.SetStatus("Validating…");
        yield return null;
        Physics.SyncTransforms();

        if (!DutzBootValidator.Validate(out var validateError))
        {
            Fail(validateError);
            yield break;
        }

        Succeed();
    }

    IEnumerator LockPlayerWhenAvailable()
    {
        for (var i = 0; i < 120; i++)
        {
            var player = DutzPlayerController.Instance;
            if (player == null)
                player = FindObjectOfType<DutzPlayerController>();

            if (player != null)
            {
                player.SetControlsLocked(true);
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("[Dutz] Bootstrap could not find player to lock within 120 frames.");
    }

    static List<IBootStep> BuildBootSteps()
    {
        return new List<IBootStep>
        {
            new EnvironmentBootStep(),
            new SegmentPoolBootStep(),
            new CollectiblesBootStep(),
            new LevelSystemsBootStep(),
            new NpcBootStep(),
            new PresentationBootStep()
        };
    }

    static void Succeed()
    {
        isReady = true;
        hasFailed = false;
        lastError = null;
        DutzBootOverlay.Hide();
        Debug.Log("[Dutz] Bootstrap ready — difficulty selection unlocked.");
        Ready?.Invoke();
    }

    static void Fail(string message)
    {
        isReady = false;
        hasFailed = true;
        lastError = message;
        DutzBootOverlay.ShowFailure(message);

        var player = DutzPlayerController.Instance;
        if (player == null)
            player = FindObjectOfType<DutzPlayerController>();
        player?.SetControlsLocked(true);
    }

    sealed class EnvironmentBootStep : IBootStep
    {
        public string Name => "Environment";

        public bool Run(out string error)
        {
            error = null;
            if (!DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name))
                return true;

            DutzVoidPlaneBootstrap.EnsureVoidPlane();
            DutzMeshMaterialRepair.EnsureBridgeMeshesRepaired();
            DutzHighwayBridgeStandableDecks.EnsureFromBoot();
            DutzMobileLighting.EnsureFromBoot();
            return true;
        }
    }

    sealed class SegmentPoolBootStep : IBootStep
    {
        public string Name => "Segment pool";

        public bool Run(out string error)
        {
            error = null;
            DutzCrocodilePoolMember.EnsureCrocodileScale();
            // Level07 uses fixed Straight-2 addicts (no segment teleport pool/manager).
            if (!DutzCollectibleProgress.IsLevel07)
                DutzSegmentHippieManager.EnsureFromBoot();
            return true;
        }
    }

    sealed class CollectiblesBootStep : IBootStep
    {
        public string Name => "Collectibles";

        public bool Run(out string error)
        {
            error = null;
            if (DutzCollectibleProgress.UsesSuitcases)
            {
                DutzSuitcaseCounter.EnsureFromBoot();
                if (DutzCollectibleProgress.IsLevel07)
                    DutzVotesCounter.EnsureFromBoot();
            }
            else if (DutzCollectibleProgress.IsLevel03Gameplay)
                DutzHealthPotionRegistry.EnsureFromBoot();
            else
                DutzGoldCoinCounter.EnsureFromBoot();

            return true;
        }
    }

    sealed class LevelSystemsBootStep : IBootStep
    {
        public string Name => "Level systems";

        public bool Run(out string error)
        {
            error = null;
            DutzLevelObjective.EnsureFromBoot();
            DutzFlagPoleGoal.EnsureFromBoot();
            DutzEndHouseCollider.EnsureFromBoot();
            DutzSenateBuildingMuralGoal.EnsureFromBoot();
            DutzSenateBuildingMural.EnsureFromBoot();
            if (DutzCollectibleProgress.IsLevel00)
            {
                DutzLevel00StaticCrowdColliders.EnsureInOpenScene(log: false);
                DutzLevel00CrowdCrossroadRespawn.EnsureFromBoot();
            }
            DutzForceFieldSuitPickup.EnsureOnSceneSuit();
            DutzSuperPunchPickup.EnsureOnScenePickup();
            DutzGrandmaBossPowerShop.EnsureFromBoot();
            DutzLevelCompleteHud.EnsureFromBoot();
            DutzLevelSelectHud.EnsureFromBoot();
            DutzVictorySelfieSetupHud.EnsureFromBoot();
            DutzLevel00WelcomeSplash.EnsureFromBoot();
            DutzPoliceCaptureDialog.EnsureFromBoot();
            DutzTrackProximityAnnouncements.EnsureFromBoot();
            DutzMuralBumpMessage.EnsureFromBoot();
            DutzSenateVotesOffer.EnsureFromBoot();
            DutzTrackGiantProximityHud.EnsureFromBoot();
            DutzPlayerPunch.EnsureFromBoot();
            DutzPlayerHitPoints.EnsureFromBoot();
            DutzLevel03Finale.EnsureFromBoot();
            return true;
        }
    }

    sealed class NpcBootStep : IBootStep
    {
        public string Name => "NPCs";

        public bool Run(out string error)
        {
            error = null;
            if (DutzMobileRuntime.ShouldDeferNpcBootstrap)
                return true;

            foreach (var physics in FindObjectsOfType<SimpleCitizensNpcPhysics>())
            {
                if (SimpleCitizensNpcPhysics.IsLevel00CrowdWalker(physics.gameObject))
                    continue;

                SimpleCitizensGiantHippieHunter.EnsureOnNpc(physics);
                SimpleCitizensGiantHippieHunter.EnsureTrililingColliderOnNpc(physics);
                SimpleCitizensHippieHunter.EnsureOnNpc(physics);
                SimpleCitizensFlyingHippieHunter.EnsureOnNpc(physics);
                SimpleCitizensHippieBiter.EnsureOnNpc(physics);
                SimpleCitizensHippieSounds.EnsureOnNpc(physics);
                SimpleCitizensNpcRespawn.EnsureOnNpc(physics);
            }

            DutzJonremPoliceBehavior.EnsureFromBoot();

            if (DutzCollectibleProgress.IsLevel00)
            {
                DutzLevel00StaticCrowdColliders.EnsureInOpenScene(log: false);
                DutzLevel00CrowdCrossroadRespawn.EnsureFromBoot();
            }

            if (DutzCollectibleProgress.IsLevel03Gameplay)
            {
                DutzNpcHitPoints.EnsureFromBoot();
                DutzGiantHeat.EnsureFromBoot();
                DutzLevel03Finale.BindEndBossDeathHandler();
                DutzLevel03Finale.EnsureTrackGiantsVisible();
                DutzLevel03BonusGiants.EnsureFromBoot();
                DutzLevel03Finale.EnsureEndEtOlScale();
            }

            if (DutzCollectibleProgress.IsLevel07)
            {
                var bird = GameObject.Find(DutzAlienGiantBirdHunter.BirdObjectName);
                if (bird != null)
                    DutzAlienGiantBirdHunter.EnsureConfigured(bird);

                // Duplicated birds (e.g. "AlienGiantBirdSubmit (1)") must be configured too.
                foreach (var hunter in UnityEngine.Object.FindObjectsOfType<DutzAlienGiantBirdHunter>(true))
                    DutzAlienGiantBirdHunter.EnsureConfigured(hunter.gameObject);

                DutzLevel07GiantStationary.EnsureAll();
                DutzLevel07Straight3AddictSpawner.EnsureFromBoot();
            }

            return true;
        }
    }

    sealed class PresentationBootStep : IBootStep
    {
        public string Name => "Presentation";

        public bool Run(out string error)
        {
            error = null;
            DutzGiantHippieBossFace.EnsureFromBoot();
            DutzGiantHeadTopCollider.EnsureFromBoot();
            DutzGiantWorldDialog.EnsureFromBoot();
            DutzGrandmaGiantStationary.EnsureFromBoot();
            DutzBackgroundMusic.EnsureFromBoot();
            return true;
        }
    }
}
