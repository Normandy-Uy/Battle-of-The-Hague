using UnityEngine;

/// <summary>Layout and stylization for Hague photo murals on highway side walls.</summary>
[CreateAssetMenu(fileName = "DutzHighwayPhotoBillboardSettings", menuName = "Dutz/Highway Photo Billboards")]
public class DutzHighwayPhotoBillboardSettings : ScriptableObject
{
    [Header("Elevated Straights (3 tall panels per side, no gaps)")]
    public int panelsPerRoadSide = 3;
    [Tooltip("Fraction of segment wall height used for each tall panel.")]
    [Range(0.5f, 1f)] public float tallWallHeightCoverage = 0.98f;
    [Tooltip("Meters from the highway side wall surface to each mural (all segments).")]
    public float elevatedLateralOffset = 20f;
    [Tooltip("Small overlap so adjacent panels never show a seam.")]
    public float elevatedPanelOverlap = 0.2f;
    [Header("Legacy (unused by 3-panel layout)")]
    public float elevatedPanelWidth = 22f;
    public float elevatedHeightAboveDeck = 8f;

    [Header("Wall Murals (bridges / fallback)")]
    public float wallFaceInset = 0.08f;
    [Range(0.5f, 1f)] public float wallHeightCoverage = 0.98f;
    public float minPanelWidthMeters = 10f;
    public float panelOverlapMeters = 0.12f;

    [Header("Photo Stylization")]
    [Range(4, 24)] public int posterizeLevels = 10;
    [Range(0f, 1.5f)] public float saturationBoost = 0.25f;
    [Range(0.5f, 2f)] public float contrastBoost = 1.1f;
    public int maxTextureEdge = 1024;

    [Header("Level 00 EDSA Side Walls")]
    public int edsaMaxTextureEdge = 1536;
}
