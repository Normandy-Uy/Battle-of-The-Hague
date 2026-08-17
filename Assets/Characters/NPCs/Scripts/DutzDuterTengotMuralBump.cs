using UnityEngine;

/// <summary>Legacy alias for DUTERTENGOT murals — prefer <see cref="DutzMuralBumpMessage"/>.</summary>
public class DutzDuterTengotMuralBump : DutzMuralBumpMessage
{
    void Reset() => InitializeForAuthoring("MY GOD, I HATE DRUGS.");

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(BumpMessage))
            InitializeForAuthoring("MY GOD, I HATE DRUGS.");
    }
}
