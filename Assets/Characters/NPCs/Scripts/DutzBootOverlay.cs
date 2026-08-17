using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Boot loading / failure overlay for Dutz level scenes (all platforms).
/// </summary>
public class DutzBootOverlay : MonoBehaviour
{
    public enum OverlayState
    {
        Hidden,
        Loading,
        Failed
    }

    static DutzBootOverlay instance;

    OverlayState state = OverlayState.Hidden;
    string status = "Loading…";
    string errorMessage = string.Empty;

    public static OverlayState State => instance != null ? instance.state : OverlayState.Hidden;

    public static void EnsureVisible()
    {
        if (instance == null)
        {
            var go = new GameObject(nameof(DutzBootOverlay));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DutzBootOverlay>();
        }

        instance.state = OverlayState.Loading;
        instance.status = $"Loading {SceneManager.GetActiveScene().name}…";
        DutzAndroidBootLog.Write(instance.status);
    }

    public static void SetStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        DutzAndroidBootLog.Write(message);
        if (instance == null)
            return;

        instance.status = message;
        if (instance.state == OverlayState.Hidden)
            instance.state = OverlayState.Loading;
    }

    public static void ShowFailure(string message)
    {
        EnsureVisible();
        instance.state = OverlayState.Failed;
        instance.errorMessage = string.IsNullOrWhiteSpace(message) ? "Startup failed." : message.Trim();
        instance.status = "Startup failed";
        Debug.LogError("[Dutz] BOOT FAILED: " + instance.errorMessage);
    }

    public static void Hide()
    {
        if (instance == null)
            return;

        instance.state = OverlayState.Hidden;
        instance.errorMessage = string.Empty;
    }

    public static void DestroyInstance()
    {
        if (instance == null)
            return;

        Destroy(instance.gameObject);
        instance = null;
    }

    void OnGUI()
    {
        if (state == OverlayState.Hidden)
            return;

        if (state == OverlayState.Loading)
            DrawLoadingPanel();
        else if (state == OverlayState.Failed)
            DrawFailedPanel();
    }

    void DrawLoadingPanel()
    {
        const string loadingTitle = "DUTZ — STARTING LEVEL";
        var titleStyle = DutzCartoonDialogGui.TitleStyle();
        var bodyStyle = DutzCartoonDialogGui.BodyStyle();
        var hintStyle = DutzCartoonDialogGui.HintStyle();
        var panelWidth = DutzCartoonDialogGui.FitMessageBoxWidth(
            loadingTitle, titleStyle, DutzCartoonDialogGui.PanelWidth);
        if (!string.IsNullOrEmpty(status))
            panelWidth = Mathf.Max(panelWidth, DutzCartoonDialogGui.FitMessageBoxWidth(
                status, bodyStyle, DutzCartoonDialogGui.PanelWidth));
        if (Application.isMobilePlatform)
            panelWidth = Mathf.Max(panelWidth, DutzCartoonDialogGui.FitMessageBoxWidth(
                SystemInfo.graphicsDeviceName, hintStyle, DutzCartoonDialogGui.PanelWidth));

        var contentWidth = panelWidth - DutzCartoonDialogGui.ContentInset * 2f
            - DutzCartoonDialogGui.PanelPadding * 2f;

        var titleHeight = titleStyle.CalcHeight(new GUIContent(loadingTitle), contentWidth);
        var statusHeight = bodyStyle.CalcHeight(new GUIContent(status), contentWidth);
        var gpuHeight = Application.isMobilePlatform
            ? hintStyle.CalcHeight(new GUIContent(SystemInfo.graphicsDeviceName), contentWidth)
            : 0f;
        var height = DutzCartoonDialogGui.PanelPadding * 2f
            + titleHeight + 8f + statusHeight
            + (Application.isMobilePlatform ? 8f + gpuHeight : 0f)
            + DutzCartoonDialogGui.ContentInset * 2f
            + DutzCartoonDialogGui.Scale(8f, 16f);
        height = Mathf.Max(height, DutzCartoonDialogGui.Scale(140f, 240f));

        var frame = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            Screen.height - height - DutzCartoonDialogGui.Scale(24f, 40f),
            panelWidth,
            height);

        DutzCartoonDialogGui.DrawFrame(frame);

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label(loadingTitle, titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label(status, bodyStyle);
        if (Application.isMobilePlatform)
            GUILayout.Label($"{SystemInfo.graphicsDeviceName}", hintStyle);
        GUILayout.EndArea();
    }

    void DrawFailedPanel()
    {
        DutzCartoonDialogGui.DrawDimOverlay();

        var height = DutzCartoonDialogGui.BootFailureHeight;
        var frame = DutzCartoonDialogGui.CenteredPanel(height);
        DutzCartoonDialogGui.DrawFrame(frame);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle(new Color(0.9f, 0.2f, 0.18f));
        var messageStyle = DutzCartoonDialogGui.BodyStyle();

        GUILayout.BeginArea(DutzCartoonDialogGui.ContentRect(frame));
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.Label("COULD NOT START LEVEL", titleStyle);
        GUILayout.Space(DutzCartoonDialogGui.Scale(10f, 16f));
        GUILayout.Label(errorMessage, messageStyle, GUILayout.Height(DutzCartoonDialogGui.Scale(140f, 200f)));
        GUILayout.FlexibleSpace();
        if (DutzCartoonDialogGui.ActionButton("RETRY"))
            DutzGameBootstrap.RetrySceneLoad();
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);
        GUILayout.EndArea();
    }
}
