using UnityEngine;

/// <summary>Editable slogans and layout for highway straight wall text.</summary>
[CreateAssetMenu(fileName = "DutzHighwayWallSlogans", menuName = "Dutz/Highway Wall Slogans")]
public class DutzHighwayWallSloganSettings : ScriptableObject
{
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
    public float spacingAlongWall = 80f;
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
}
