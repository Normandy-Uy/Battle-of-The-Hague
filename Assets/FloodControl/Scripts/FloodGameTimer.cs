using UnityEngine;

/// <summary>
/// Four-minute Flood Control countdown. Timing begins after the intro enables gameplay.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloodGameTimer : MonoBehaviour
{
    const string DrowningTitle = "YOU DIED OF DROWNING.";

    [Header("References")]
    [SerializeField] GameManager gameManager;
    [SerializeField] FloodPlayerHealth playerHealth;

    [Header("Timer")]
    [SerializeField] float durationSeconds = 240f;
    [SerializeField] float remainingSeconds = 240f;

    bool expired;
    GUIStyle pauseButtonStyle;

    public float DurationSeconds => durationSeconds;
    public float RemainingSeconds => remainingSeconds;

    public void ResetForRespawn()
    {
        expired = false;
        remainingSeconds = Mathf.Max(1f, durationSeconds);
    }

    void Awake()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
        if (playerHealth == null)
            playerHealth = FindObjectOfType<FloodPlayerHealth>();

        remainingSeconds = Mathf.Max(1f, durationSeconds);
    }

    void Update()
    {
        if (!CanPause())
        {
            DutzGamePause.GuiHitRect = default;
            return;
        }

        DutzGamePause.PollTouchToggle();

        if (DutzGamePause.IsPaused)
            return;

        remainingSeconds = Mathf.Max(0f, remainingSeconds - Time.deltaTime);
        if (remainingSeconds > 0f)
            return;

        expired = true;
        playerHealth.KillWithDialog(DrowningTitle, string.Empty);
    }

    void OnGUI()
    {
        if (!CanPause())
            return;

        int totalSeconds = Mathf.CeilToInt(remainingSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = DutzCartoonDialogGui.ScaleFont(26, 36),
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = remainingSeconds <= 30f
            ? new Color(1f, 0.25f, 0.18f, 1f)
            : Color.white;

        float width = DutzCartoonDialogGui.Scale(280f, 380f);
        float height = DutzCartoonDialogGui.Scale(42f, 58f);
        Rect rect = new Rect(
            (Screen.width - width) * 0.5f,
            DutzCartoonDialogGui.Scale(12f, 20f),
            width,
            height);

        int previousDepth = GUI.depth;
        GUI.depth = -60;
        string timerText = $"TIME LEFT: {minutes:00}:{seconds:00}";
        GUI.Label(rect, timerText, style);
        DrawPauseButton(rect, style, timerText);
        GUI.depth = previousDepth;
    }

    bool CanPause()
    {
        if (expired || playerHealth == null || playerHealth.IsDead)
            return false;

        return gameManager == null || gameManager.IsGameplayEnabled;
    }

    void DrawPauseButton(Rect timerRect, GUIStyle timerStyle, string timerText)
    {
        float scale = Mathf.Max(1f, Screen.height / 720f);
        Vector2 textSize = timerStyle.CalcSize(new GUIContent(timerText));
        float buttonWidth = Mathf.Max(180f, 168f * scale);
        float buttonHeight = Mathf.Max(72f, 64f * scale);
        float gap = 10f * scale;
        float x = timerRect.center.x + textSize.x * 0.5f + gap;
        x = Mathf.Min(x, Screen.width - buttonWidth - 8f);
        float y = 2f * scale;
        Rect pauseRect = new Rect(x, y, buttonWidth, buttonHeight);
        DutzGamePause.GuiHitRect = pauseRect;

        if (pauseButtonStyle == null)
        {
            pauseButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(26f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            pauseButtonStyle.normal.textColor = Color.yellow;
        }

        string label = DutzGamePause.IsPaused ? "RESUME" : "PAUSE";
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.Label(
            new Rect(pauseRect.x + 2f, pauseRect.y + 2f, pauseRect.width, pauseRect.height),
            label,
            pauseButtonStyle);
        GUI.color = new Color(1f, 1f, 1f, 0.95f);
        GUI.Label(pauseRect, label, pauseButtonStyle);
        GUI.color = previous;
    }

    public void Configure(
        float duration,
        GameManager manager,
        FloodPlayerHealth health)
    {
        durationSeconds = Mathf.Max(1f, duration);
        remainingSeconds = durationSeconds;
        gameManager = manager;
        playerHealth = health;
        expired = false;
    }

    void OnValidate()
    {
        durationSeconds = Mathf.Max(1f, durationSeconds);
        remainingSeconds = Mathf.Clamp(remainingSeconds, 0f, durationSeconds);
    }
}
