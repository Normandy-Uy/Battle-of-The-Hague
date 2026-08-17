using System.Collections.Generic;
using UnityEngine;

/// <summary>Level 3 giant heat aura — contact burns Player1 via DutzPlayerHitPoints.</summary>
[DisallowMultipleComponent]
public class DutzGiantHeat : MonoBehaviour
{
    public const float TrackBurnPerSecond = 10f;
    public const float EndBossBurnPerSecond = 2f;

    static readonly List<DutzGiantHeat> Registered = new List<DutzGiantHeat>(16);

    [SerializeField] float burnPerSecond = TrackBurnPerSecond;

    Collider[] contactColliders;

    public static IReadOnlyList<DutzGiantHeat> AllActive => Registered;

    public float BurnPerSecond => Mathf.Max(0f, burnPerSecond);

    /// <summary>Flat distance pre-check before expensive collider overlap tests.</summary>
    public static bool IsAnyNearPlayer(CharacterController playerCc, float maxDistance)
    {
        if (playerCc == null || Registered.Count == 0)
            return false;

        var playerPos = playerCc.transform.position;
        var maxDistSq = maxDistance * maxDistance;
        for (var i = 0; i < Registered.Count; i++)
        {
            var heat = Registered[i];
            if (heat == null || !heat.enabled)
                continue;

            var delta = heat.transform.position - playerPos;
            delta.y = 0f;
            if (delta.sqrMagnitude <= maxDistSq)
                return true;
        }

        return false;
    }

    public static float GetBurnPerSecondForGiant(string objectName) =>
        DutzGiantBossNames.IsLevel03EndBoss(objectName) ? EndBossBurnPerSecond : TrackBurnPerSecond;

    public void Configure(float burnRate) => burnPerSecond = Mathf.Max(0f, burnRate);

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        foreach (var hunter in Object.FindObjectsOfType<SimpleCitizensGiantHippieHunter>(true))
        {
            var name = hunter.gameObject.name;
            if (!DutzCollectibleProgress.IsLevel03Giant(name)
                && !DutzCollectibleProgress.IsLevel07CombatGiant(name))
                continue;

            EnsureOn(hunter.gameObject);
        }
    }

    public static DutzGiantHeat EnsureOn(GameObject target)
    {
        if (target == null)
            return null;

        var heat = target.GetComponent<DutzGiantHeat>();
        if (heat == null)
            heat = target.AddComponent<DutzGiantHeat>();

        heat.Configure(GetBurnPerSecondForGiant(target.name));
        heat.enabled = true;
        return heat;
    }

    void OnEnable()
    {
        if (!Registered.Contains(this))
            Registered.Add(this);
    }

    void OnDisable() => Registered.Remove(this);

    void Awake()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
        {
            enabled = false;
            return;
        }

        CacheColliders();
    }

    void CacheColliders()
    {
        contactColliders = GetComponentsInChildren<Collider>(true);
    }

    public bool IsTouchingPlayer(CharacterController playerCc)
    {
        if (!enabled || playerCc == null)
            return false;

        if (contactColliders == null || contactColliders.Length == 0)
            CacheColliders();

        if (contactColliders == null)
            return false;

        for (var i = 0; i < contactColliders.Length; i++)
        {
            var col = contactColliders[i];
            if (col == null || !col.enabled)
                continue;

            if (DutzHippieBiteCollider.IsColliderOverlappingPlayerBody(col, playerCc)
                || DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, playerCc))
                return true;
        }

        return false;
    }
}
