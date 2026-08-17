using UnityEngine;

/// <summary>
/// Level 3 bonus highway giants (HONTAVIRUS, LENG LENG LUGAW) — always configured at boot; no menu required.
/// </summary>
public static class DutzLevel03BonusGiants
{
    static readonly string[] Names =
    {
        DutzGiantBossNames.Hontavirus,
        DutzGiantBossNames.LengLengLugaw,
    };

    public static void EnsureFromBoot()
    {
        if (!DutzCollectibleProgress.IsLevel03Gameplay)
            return;

        var trackRoot = GameObject.Find("DutzLevel03TrackGiants");
        if (trackRoot != null && !trackRoot.activeSelf)
            trackRoot.SetActive(true);

        var ensured = 0;
        foreach (var objectName in Names)
        {
            var giant = GameObject.Find(objectName);
            if (giant == null)
                continue;

            var hitPoints = giant.GetComponent<DutzNpcHitPoints>();
            if (hitPoints != null && hitPoints.IsDead)
                continue;

            if (!giant.activeSelf)
                giant.SetActive(true);

            var physics = giant.GetComponent<SimpleCitizensNpcPhysics>();
            if (physics == null)
                physics = giant.AddComponent<SimpleCitizensNpcPhysics>();

            SimpleCitizensGiantHippieHunter.EnsureOnNpc(physics);
            SimpleCitizensGiantHippieHunter.EnsureTrililingColliderOnNpc(physics);
            SimpleCitizensNpcRespawn.EnsureOnNpc(physics);
            DutzNpcHitPoints.EnsureOn(giant, DutzNpcHitPoints.TrackEtOlHitPoints);
            DutzGiantHeat.EnsureOn(giant);
            DutzGiantHeadTopCollider.EnsureOnGiant(giant);

            physics.Apply();
            physics.SnapFeetToRoad();

            var respawn = giant.GetComponent<SimpleCitizensNpcRespawn>();
            if (respawn != null)
                respawn.RecordSpawnPoint();

            ensured++;
        }

        if (ensured > 0)
        {
            Debug.Log(
                $"[Dutz] Level 3 bonus giants ensured at boot ({ensured}/{Names.Length}) — " +
                "HONTAVIRUS, LENG LENG LUGAW.");
        }
    }
}
