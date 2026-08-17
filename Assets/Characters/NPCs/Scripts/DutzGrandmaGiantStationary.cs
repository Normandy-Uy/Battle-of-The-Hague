using UnityEngine;

/// <summary>
/// Keeps the Sara / grandma giant frozen in place — no walk, chase, or root motion.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(400)]
public class DutzGrandmaGiantStationary : MonoBehaviour
{
    public const string GrandmaGiantName = DutzGiantBossNames.PrincessZara;

    Vector3 lockedPosition;
    Quaternion lockedRotation;
    bool locked;

    public static void EnsureFromBoot()
    {
        // Level07 Gong Bong is a Bridge 5 chase boss — never freeze him as Level01 shop Sara.
        if (DutzCollectibleProgress.IsLevel07)
        {
            StripChaseBlockingShopLock(DutzGiantBossNames.FindGongBong());
            StripChaseBlockingShopLock(DutzGiantBossNames.FindCawetan());
            return;
        }

        ApplyStationaryFor(DutzGiantBossNames.FindPrincessZara());
        ApplyStationaryFor(DutzGiantBossNames.FindCawetan());
    }

    static void StripChaseBlockingShopLock(GameObject giant)
    {
        if (giant == null)
            return;

        if (!DutzGiantBossNames.IsGongBong(giant.name) && !DutzGiantBossNames.IsCawetan(giant.name))
            return;

        var stationary = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (stationary != null)
            Object.Destroy(stationary);

        var hunter = giant.GetComponent<SimpleCitizensGiantHippieHunter>();
        if (hunter != null)
            hunter.enabled = true;

        var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
        if (physics != null)
        {
            physics.enabled = true;
            physics.SetWalkingEnabled(true);
        }

        var heat = giant.GetComponent<DutzGiantHeat>();
        if (heat != null)
            heat.enabled = true;

        var animator = giant.GetComponent<Animator>();
        if (animator != null && animator.speed < 0.001f)
            animator.speed = 1f;
    }

    static void ApplyStationaryFor(GameObject giant)
    {
        if (giant == null)
            return;

        // Level07 chase bosses that share shop-giant aliases must never get the Sara freeze.
        if (DutzCollectibleProgress.IsLevel07
            && (DutzGiantBossNames.IsGongBong(giant.name) || DutzGiantBossNames.IsCawetan(giant.name)))
        {
            StripChaseBlockingShopLock(giant);
            return;
        }

        if (!DutzGiantBossNames.IsPrincessZara(giant.name) && !DutzGiantBossNames.IsCawetan(giant.name))
            return;

        // On Level07, "Gong Bong" matches IsPrincessZara — do not freeze him as shop Sara.
        if (DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsGongBong(giant.name))
        {
            StripChaseBlockingShopLock(giant);
            return;
        }

        var stationary = giant.GetComponent<DutzGrandmaGiantStationary>();
        if (stationary == null)
            stationary = giant.AddComponent<DutzGrandmaGiantStationary>();

        stationary.ApplyStationary();
    }

    static bool IsStationaryGiant(string objectName)
    {
        if (DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsGongBong(objectName))
            return false;

        return DutzGiantBossNames.IsPrincessZara(objectName)
            || (DutzGiantBossNames.IsCawetan(objectName) && !DutzCollectibleProgress.IsLevel07);
    }

    void Awake()
    {
        if (!IsStationaryGiant(gameObject.name))
        {
            Destroy(this);
            return;
        }

        ApplyStationary();
    }

    void OnEnable()
    {
        if (!IsStationaryGiant(gameObject.name))
            return;

        ApplyStationary();
    }

    public void ApplyStationary()
    {
        if (!IsStationaryGiant(gameObject.name))
            return;
        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
        locked = true;

        DisableMovementComponents();
        FreezeAnimator();

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = lockedPosition;
            rb.rotation = lockedRotation;
        }

        DutzShopGiantTouch.EnsureOnGiant(gameObject);
    }

    void DisableMovementComponents()
    {
        var npcPhysics = GetComponent<SimpleCitizensNpcPhysics>();
        if (npcPhysics != null)
        {
            npcPhysics.SetWalkingEnabled(false);
            npcPhysics.ClearChaseTarget();
            npcPhysics.enabled = false;
        }

        var giantHunter = GetComponent<SimpleCitizensGiantHippieHunter>();
        if (giantHunter != null)
            giantHunter.enabled = false;

        var hippieHunter = GetComponent<SimpleCitizensHippieHunter>();
        if (hippieHunter != null)
            hippieHunter.enabled = false;

        var biter = GetComponent<SimpleCitizensHippieBiter>();
        if (biter != null)
            biter.enabled = false;
    }

    void FreezeAnimator()
    {
        var animator = GetComponent<Animator>();
        if (animator == null)
            return;

        animator.applyRootMotion = false;
        animator.speed = 0f;

        if (animator.runtimeAnimatorController != null)
        {
            animator.Play(0, 0, 0f);
            animator.Update(0f);
        }
    }

    void LateUpdate()
    {
        if (!locked)
            return;

        transform.SetPositionAndRotation(lockedPosition, lockedRotation);

        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;

        rb.position = lockedPosition;
        rb.rotation = lockedRotation;
    }
}

/// <summary>Body + touch trigger colliders for shop giants (Zara, Gong Bong, Cawetan).</summary>
public static class DutzShopGiantTouch
{
    public const string TouchVolumeName = "ShopGiantTouch";

    static readonly Vector3 TouchCenter = new(0f, 1.15f, 0.05f);
    static readonly Vector3 TouchSize = new(1.35f, 2.35f, 1.45f);

    public static bool IsShopGiant(string objectName)
    {
        if (DutzCollectibleProgress.IsLevel07 && DutzGiantBossNames.IsGongBong(objectName))
            return false;

        return DutzGiantBossNames.IsPrincessZara(objectName)
            || (DutzGiantBossNames.IsCawetan(objectName) && !DutzCollectibleProgress.IsLevel07);
    }

    public static void EnsureOnAllShopGiants()
    {
        EnsureOnGiant(DutzGiantBossNames.FindPrincessZara());
        if (!DutzCollectibleProgress.IsLevel07)
            EnsureOnGiant(DutzGiantBossNames.FindCawetan());
    }

    public static void EnsureOnGiant(GameObject giant)
    {
        if (giant == null || !IsShopGiant(giant.name))
            return;

        DutzHippieBiteCollider.EnsureSmallHippieColliders(giant);

        var touchTransform = giant.transform.Find(TouchVolumeName);
        if (touchTransform == null)
        {
            var touchGo = new GameObject(TouchVolumeName);
            touchTransform = touchGo.transform;
            touchTransform.SetParent(giant.transform, false);
        }

        var touch = touchTransform.GetComponent<BoxCollider>();
        if (touch == null)
            touch = touchTransform.gameObject.AddComponent<BoxCollider>();

        touch.isTrigger = true;
        touch.center = TouchCenter;
        touch.size = TouchSize;
        Physics.SyncTransforms();
    }
}
