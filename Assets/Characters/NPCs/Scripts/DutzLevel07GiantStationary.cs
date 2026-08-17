using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Level 7 only: locks non-bird giants in place and strips chase/burn behavior.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(500)]
public class DutzLevel07GiantStationary : MonoBehaviour
{
    static readonly int SpeedId = Animator.StringToHash("Speed_f");

    Vector3 lockedPosition;
    Quaternion lockedRotation;
    bool locked;
    bool stripped;

    public static void EnsureAll()
    {
        if (!DutzCollectibleProgress.IsLevel07)
            return;

        var level07 = SceneManager.GetActiveScene();
        foreach (var transform in FindObjectsOfType<Transform>(true))
        {
            if (transform == null || transform.gameObject.scene != level07)
                continue;

            var target = transform.gameObject;
            if (!IsStationaryGiant(target))
                continue;

            var stationary = target.GetComponent<DutzLevel07GiantStationary>();
            if (stationary == null)
                stationary = target.AddComponent<DutzLevel07GiantStationary>();

            stationary.ApplyStationary();
        }
    }

    static bool IsStationaryGiant(GameObject target)
    {
        if (target == null)
            return false;

        // Birds, small addicts, and chase giants with home-highway locks must never be frozen.
        if (target.GetComponent<DutzAlienGiantBirdHunter>() != null)
            return false;
        if (target.name.StartsWith(DutzAlienGiantBirdHunter.BirdObjectName, System.StringComparison.Ordinal))
            return false;
        if (target.name.StartsWith("SimpleCitizens_Hippie_Extra_L07_", System.StringComparison.Ordinal))
            return false;
        if (target.name.StartsWith("Level07_Straight2_Addicts", System.StringComparison.Ordinal))
            return false;
        if (target.name.StartsWith("Level07_Straight3_Addicts", System.StringComparison.Ordinal))
            return false;
        if (string.Equals(target.name, "RAPTOR", System.StringComparison.Ordinal))
            return false;
        if (string.Equals(target.name, "K Bilyar", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsKBilyar(target.name))
            return false;
        if (string.Equals(target.name, "I am baby", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsIAmBaby(target.name))
            return false;
        if (string.Equals(target.name, "M BILYAR", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsMBilyar(target.name))
            return false;
        if (target.name.StartsWith("Level07_Highway8_Croc_", System.StringComparison.Ordinal))
            return false;
        if (target.name.StartsWith("Level07_Highway7_Croc_", System.StringComparison.Ordinal))
            return false;
        if (string.Equals(target.name, "Lie Fivex", System.StringComparison.Ordinal))
            return false;
        if (string.Equals(target.name, "Piyaya", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsPiyaya(target.name))
            return false;
        if (string.Equals(target.name, "STONE", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsStone(target.name))
            return false;
        if (string.Equals(target.name, "MARKO LEKTA", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsMarkoLekta(target.name))
            return false;
        if (string.Equals(target.name, "HONTAVIRUS", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsHontavirus(target.name))
            return false;
        if (string.Equals(target.name, "Liron Sinta", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsLironSinta(target.name))
            return false;
        if (string.Equals(target.name, "Gong Bong", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsGongBong(target.name))
            return false;
        if (string.Equals(target.name, "Boy Idol", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsBoyIdol(target.name))
            return false;
        if (string.Equals(target.name, "Cawetan", System.StringComparison.Ordinal)
            || DutzGiantBossNames.IsCawetan(target.name))
            return false;

        // Small ground addicts (non-giant).
        if (target.GetComponent<SimpleCitizensHippieHunter>() != null
            && target.GetComponent<SimpleCitizensGiantHippieHunter>() == null
            && !DutzGiantBossNames.IsAnyGiantBoss(target.name)
            && !DutzCollectibleProgress.IsLevel03Giant(target.name))
            return false;

        return target.GetComponent<SimpleCitizensGiantHippieHunter>() != null
            || DutzCollectibleProgress.IsLevel03Giant(target.name)
            || DutzGiantBossNames.IsAnyGiantBoss(target.name);
    }

    void Awake()
    {
        if (!DutzCollectibleProgress.IsLevel07 || !IsStationaryGiant(gameObject))
        {
            Destroy(this);
            return;
        }

        ApplyStationary();
    }

    void OnEnable()
    {
        if (DutzCollectibleProgress.IsLevel07 && IsStationaryGiant(gameObject))
            ApplyStationary();
    }

    public void ApplyStationary()
    {
        if (!DutzCollectibleProgress.IsLevel07 || !IsStationaryGiant(gameObject))
            return;

        if (!locked)
        {
            lockedPosition = transform.position;
            lockedRotation = transform.rotation;
            locked = true;
        }

        StripChaseAndBurn();
        EnforcePose();
    }

    void StripChaseAndBurn()
    {
        DisableOnly(GetComponent<SimpleCitizensGiantHippieHunter>());
        DisableOnly(GetComponent<SimpleCitizensHippieHunter>());
        DisableOnly(GetComponent<SimpleCitizensFlyingHippieHunter>());
        DisableOnly(GetComponent<SimpleCitizensHippieBiter>());
        DisableOnly(GetComponent<DutzGiantHeat>());

        // Never Destroy NpcPhysics — NpcRespawn requires it.
        var physics = GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.enabled = false;
            physics.ClearChaseTarget();
            physics.SetWalkingEnabled(false);
        }

        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (animator.isActiveAndEnabled)
                animator.SetFloat(SpeedId, 0f);
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            LockRigidbody(rb);

        stripped = true;
    }

    static void DisableOnly(Behaviour component)
    {
        if (component != null)
            component.enabled = false;
    }

    void LockRigidbody(Rigidbody rb)
    {
        // Never write velocity/angularVelocity while kinematic (Unity spam).
        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.constraints |= RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        rb.position = lockedPosition;
        rb.rotation = lockedRotation;
    }

    void EnforcePose()
    {
        transform.SetPositionAndRotation(lockedPosition, lockedRotation);

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        if (!rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        rb.useGravity = false;
        rb.position = lockedPosition;
        rb.rotation = lockedRotation;
    }

    void LateUpdate()
    {
        if (!locked)
            return;

        // Re-disable chase if boot/finale re-enables it — never Destroy NpcPhysics.
        if (!stripped)
            StripChaseAndBurn();
        else
        {
            DisableOnly(GetComponent<SimpleCitizensGiantHippieHunter>());
            DisableOnly(GetComponent<SimpleCitizensHippieHunter>());
            DisableOnly(GetComponent<SimpleCitizensFlyingHippieHunter>());
            DisableOnly(GetComponent<SimpleCitizensHippieBiter>());
            DisableOnly(GetComponent<DutzGiantHeat>());
            var physics = GetComponent<SimpleCitizensNpcPhysics>();
            if (physics != null && physics.enabled)
            {
                physics.enabled = false;
                physics.ClearChaseTarget();
                physics.SetWalkingEnabled(false);
            }
        }

        EnforcePose();
    }
}
