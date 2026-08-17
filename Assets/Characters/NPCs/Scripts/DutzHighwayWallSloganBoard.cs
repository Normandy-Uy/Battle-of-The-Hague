using UnityEngine;

/// <summary>
/// Scene Inspector settings for highway wall slogans.
/// Select "Highway Wall Slogans" in the hierarchy to edit slogans and layout.
/// </summary>
[DisallowMultipleComponent]
public class DutzHighwayWallSloganBoard : MonoBehaviour
{
    [Header("Slogans")]
    [TextArea(1, 3)]
    public string[] slogans =
    {
        "BUILD THE WALL",
        "MAKE ROADS GREAT AGAIN",
        "LAW AND ORDER",
        "FREEDOM FOR ALL",
        "VOTE OR DIE",
        "ONE NATION UNDER DUTZ",
        "NO RETREAT NO SURRENDER",
        "THE HIGHWAY BELONGS TO THE PEOPLE",
        "STRONG BORDERS STRONG ROADS",
        "FORWARD TOGETHER",
        "TRUTH ON THE CONCRETE",
        "POWER TO THE DRIVER"
    };

    [Header("Layout")]
    public float spacingAlongWall = 55f;
    public float wallFaceInset = 0.1f;
    [Range(0.2f, 0.9f)] public float heightOnWall = 0.55f;
    public float verticalOffset = 2f;
    [Range(0.5f, 1f)] public float wallSideExtent = 0.92f;

    [Header("Typography")]
    public float fontSize = 36f;
    public bool scaleFontFromWallHeight = true;
    public float fontSizePerWallHeight = 1.15f;
    public Color fontColor = new Color(0.92f, 0.78f, 0.08f, 1f);
    public Color outlineColor = new Color(0.12f, 0.05f, 0.02f, 1f);
    public float outlineWidth = 0.22f;
    public float characterSpacing = 2f;

    public void CopyFrom(DutzHighwayWallSloganSettings asset)
    {
        if (asset == null)
            return;

        slogans = asset.slogans != null ? (string[])asset.slogans.Clone() : new string[0];
        spacingAlongWall = asset.spacingAlongWall;
        wallFaceInset = asset.wallFaceInset;
        heightOnWall = asset.heightOnWall;
        verticalOffset = asset.verticalOffset;
        wallSideExtent = asset.wallSideExtent;
        fontSize = asset.fontSize;
        scaleFontFromWallHeight = asset.scaleFontFromWallHeight;
        fontSizePerWallHeight = asset.fontSizePerWallHeight;
        fontColor = asset.fontColor;
        outlineColor = asset.outlineColor;
        outlineWidth = asset.outlineWidth;
        characterSpacing = asset.characterSpacing;
    }
}
