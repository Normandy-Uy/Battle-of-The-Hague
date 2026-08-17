using System.Collections;
using UnityEngine;

/// <summary>Damageable NPC health — punch and other attacks call TakeDamage.</summary>
[DisallowMultipleComponent]
public class DutzNpcHitPoints : MonoBehaviour
{
    [SerializeField] int maxHitPoints = 20;
    [SerializeField] int currentHitPoints = 20;
    [SerializeField] bool destroyOnDeath;
    [SerializeField] float destroyDelaySeconds = 2f;

    bool isDead;

    public int MaxHitPoints => maxHitPoints;
    public int CurrentHitPoints => currentHitPoints;
    public bool IsDead => isDead;

    public event System.Action Died;

    public const float Level03GiantPunchStunSeconds = 1f;
    public const float EtOlPunchStunSeconds = Level03GiantPunchStunSeconds;
    public const float EndEtOlPunchStunSeconds = Level03GiantPunchStunSeconds;

    public const int EndEtOlHitPoints = 200;
    public const int TrackEtOlHitPoints = 50;

    public static void EnsureFromBoot()
    {
        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            if (DutzCollectibleProgress.ShowsProximityHitPoints(hunter.gameObject.name))
            {
                EnsureOn(hunter.gameObject, TrackEtOlHitPoints);
                continue;
            }

            if (!DutzCollectibleProgress.IsLevel03Gameplay)
                continue;

            if (DutzGiantBossNames.IsLevel03EndBoss(hunter.gameObject.name))
            {
                var existing = hunter.GetComponent<DutzNpcHitPoints>();
                if (existing != null && existing.IsDead)
                    continue;

                EnsureOn(hunter.gameObject, EndEtOlHitPoints);
            }
        }
    }

    public static DutzNpcHitPoints EnsureOn(GameObject target, int hitPoints, bool preserveCurrentHealth = false)
    {
        if (target == null)
            return null;

        var hp = target.GetComponent<DutzNpcHitPoints>();
        if (hp == null)
        {
            hp = target.AddComponent<DutzNpcHitPoints>();
            hp.Configure(hitPoints);
            return hp;
        }

        if (hp.IsDead)
            return hp;

        if (preserveCurrentHealth)
        {
            if (hp.MaxHitPoints != hitPoints)
                hp.SetMaxHitPoints(hitPoints);
            return hp;
        }

        hp.Configure(hitPoints);
        return hp;
    }

    public void SetMaxHitPoints(int hitPoints)
    {
        maxHitPoints = Mathf.Max(1, hitPoints);
        currentHitPoints = Mathf.Clamp(currentHitPoints, 0, maxHitPoints);
    }

    public void Configure(int hitPoints)
    {
        maxHitPoints = Mathf.Max(1, hitPoints);
        currentHitPoints = maxHitPoints;
        isDead = false;
    }

    public bool TakeDamage(int amount, GameObject source)
    {
        if (isDead || amount <= 0)
            return false;

        if (source != null && source == gameObject)
            return false;

        currentHitPoints = Mathf.Max(0, currentHitPoints - amount);

        if (currentHitPoints > 0)
            return true;

        Die();
        return true;
    }

    public void ResetForPlayerRespawn()
    {
        isDead = false;
        currentHitPoints = maxHitPoints;

        if (!gameObject.activeSelf
            && DutzCollectibleProgress.IsLevel03Gameplay
            && (DutzCollectibleProgress.IsLevel03TrackEtOl(gameObject.name)
                || DutzCollectibleProgress.IsLevel03BonusGiant(gameObject.name)
                || (DutzCollectibleProgress.IsLevel07
                    && DutzCollectibleProgress.IsLevel07CombatGiant(gameObject.name))))
            gameObject.SetActive(true);

        var hunter = GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
            hunter.enabled = true;

        var physics = GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.enabled = true;
            physics.SetWalkingEnabled(true);
        }

        var heat = GetComponent<DutzGiantHeat>();
        if (heat != null)
            heat.enabled = true;

        if (DutzCollectibleProgress.IsLevel03Gameplay && DutzGiantBossNames.IsLevel03EndBoss(gameObject.name))
            DutzLevel03EndBossKnockdown.EnsureOn(gameObject)?.ResetKnockdown();

        var anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.speed = 1f;
            anim.SetBool(Animator.StringToHash("Death_b"), false);
            anim.SetFloat(Animator.StringToHash("Speed_f"), 0f);
            anim.Play("Idle", 0, 0f);
            anim.Update(0f);
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        if (DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsBoyIdol(gameObject.name))
            DutzLevel07BoyIdolGate.MarkDefeated();

        var isLevel03EndBoss = DutzCollectibleProgress.IsLevel03Gameplay
            && DutzGiantBossNames.IsLevel03EndBoss(gameObject.name);

        var hunter = GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
        {
            if (isLevel03EndBoss)
                hunter.RestoreAnimatorAfterPunchStunPublic();
            hunter.enabled = false;
        }

        var smallHunter = GetComponent<SimpleCitizensHippieHunter>();
        if (smallHunter != null)
            smallHunter.enabled = false;

        var physics = GetComponent<SimpleCitizensNpcPhysics>();
        physics?.ClearChaseTarget();
        if (isLevel03EndBoss)
        {
            if (physics != null)
                physics.enabled = false;
            DutzLevel03EndBossKnockdown.EnsureOn(gameObject)?.BeginKnockdown();
        }

        var biter = GetComponent<SimpleCitizensHippieBiter>();
        if (biter != null)
            biter.enabled = false;

        var heat = GetComponent<DutzGiantHeat>();
        if (heat != null)
            heat.enabled = false;

        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.speed = 1f;
            animator.SetFloat(Animator.StringToHash("Speed_f"), 0f);
            animator.SetBool(Animator.StringToHash("Death_b"), true);
            if (isLevel03EndBoss)
            {
                animator.Play("Death_01", 0, 0f);
                animator.Update(0f);
            }
        }

        if (destroyOnDeath && !(DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsBoyIdol(gameObject.name)))
            Destroy(gameObject, destroyDelaySeconds);
        else if (ShouldHideAfterPunchDeath())
            Invoke(nameof(HideTrackGiantAfterDeath), destroyDelaySeconds);

        DutzVotesCounter.RegisterGiantKill(gameObject);
        Died?.Invoke();
    }

    bool ShouldHideAfterPunchDeath()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return false;

        if (DutzCollectibleProgress.IsLevel03TrackEtOl(gameObject.name)
            || DutzCollectibleProgress.IsLevel03BonusGiant(gameObject.name))
            return true;

        return DutzCollectibleProgress.IsLevel07
            && DutzCollectibleProgress.IsLevel07CombatGiant(gameObject.name);
    }

    void HideTrackGiantAfterDeath()
    {
        if (!isDead)
            return;

        gameObject.SetActive(false);
    }
}

/// <summary>Procedural fall for BEYBI M — Death_01 keeps root position so giants stay upright without this.</summary>
[DisallowMultipleComponent]
public class DutzLevel03EndBossKnockdown : MonoBehaviour
{
    const float DurationSeconds = 0.95f;
    const float FallPitchDegrees = 85f;
    const float DropHeightFactor = 0.34f;

    bool knockedDown;
    Coroutine routine;

    public bool IsKnockedDown => knockedDown;

    public static DutzLevel03EndBossKnockdown EnsureOn(GameObject target)
    {
        if (target == null)
            return null;

        var knockdown = target.GetComponent<DutzLevel03EndBossKnockdown>();
        if (knockdown == null)
            knockdown = target.AddComponent<DutzLevel03EndBossKnockdown>();

        return knockdown;
    }

    public void BeginKnockdown()
    {
        if (knockedDown)
            return;

        knockedDown = true;
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(KnockdownRoutine());
    }

    public void ResetKnockdown()
    {
        knockedDown = false;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    IEnumerator KnockdownRoutine()
    {
        var rb = GetComponent<Rigidbody>();
        var startRot = transform.rotation;
        var forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.right;
        else
            forward.Normalize();

        var fallAxis = Vector3.Cross(Vector3.up, forward);
        if (fallAxis.sqrMagnitude < 0.0001f)
            fallAxis = Vector3.right;
        else
            fallAxis.Normalize();

        var endRot = startRot * Quaternion.AngleAxis(FallPitchDegrees, fallAxis);
        var startPos = transform.position;
        var endPos = startPos - Vector3.up * EstimateDropMeters();

        var elapsed = 0f;
        while (elapsed < DurationSeconds)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / DurationSeconds);
            var pos = Vector3.Lerp(startPos, endPos, t);
            var rot = Quaternion.Slerp(startRot, endRot, t);
            transform.SetPositionAndRotation(pos, rot);

            if (rb != null)
            {
                rb.position = pos;
                rb.rotation = rot;
            }

            yield return null;
        }

        routine = null;
    }

    float EstimateDropMeters()
    {
        var maxY = transform.position.y;
        var minY = maxY;
        var renderers = GetComponentsInChildren<Renderer>();
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;

            maxY = Mathf.Max(maxY, renderer.bounds.max.y);
            minY = Mathf.Min(minY, renderer.bounds.min.y);
        }

        return Mathf.Max(2f, maxY - minY) * DropHeightFactor;
    }
}
