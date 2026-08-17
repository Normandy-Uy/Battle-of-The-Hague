using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Front-camera selfie capture overlay for victory profile setup.</summary>
[DefaultExecutionOrder(2500)]
public class DutzVictorySelfieCaptureHud : MonoBehaviour
{
    static DutzVictorySelfieCaptureHud instance;

    WebCamTexture webCam;
    Texture2D captured;
    bool active;
    bool starting;
    string statusMessage;

    public static bool IsActive => instance != null && instance.active;

    public static void BeginCapture(System.Action<Texture2D> onFinished)
    {
        EnsureInstance();
        instance.StartCapture(onFinished);
    }

    static void EnsureInstance()
    {
        if (instance != null)
            return;

        var go = new GameObject(nameof(DutzVictorySelfieCaptureHud));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<DutzVictorySelfieCaptureHud>();
    }

    System.Action<Texture2D> finishCallback;

    void StartCapture(System.Action<Texture2D> onFinished)
    {
        StopCamera();
        if (captured != null)
        {
            Destroy(captured);
            captured = null;
        }

        finishCallback = onFinished;
        active = true;
        starting = true;
        statusMessage = "Opening camera…";
        StartCoroutine(StartCameraRoutine());
    }

    IEnumerator StartCameraRoutine()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            var wait = 0f;
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)
                   && wait < 12f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            statusMessage = "Camera permission denied — use CHOOSE PHOTO instead.";
            starting = false;
            yield break;
        }
#endif

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            statusMessage = "No camera found — use CHOOSE PHOTO instead.";
            starting = false;
            yield break;
        }

        var deviceName = devices[0].name;
        for (var i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                deviceName = devices[i].name;
                break;
            }
        }

        webCam = new WebCamTexture(deviceName, 1280, 720, 30);
        webCam.Play();

        var timeout = 0f;
        while (webCam != null && webCam.width <= 16 && timeout < 8f)
        {
            timeout += Time.unscaledDeltaTime;
            yield return null;
        }

        starting = false;
        statusMessage = webCam != null && webCam.isPlaying ? string.Empty : "Camera unavailable.";
    }

    void Update()
    {
        if (!active)
            return;

        if (DutzGameplayInput.GetKeyDown(KeyCode.Escape))
            Cancel();
    }

    void OnGUI()
    {
        if (!active)
            return;

        var previousDepth = GUI.depth;
        GUI.depth = -4000;
        DutzCartoonDialogGui.DrawDimOverlay(0.75f);

        var frame = DutzCartoonDialogGui.ChoiceDialogFrame(Screen.height * 0.88f);
        DutzCartoonDialogGui.DrawFrame(frame);
        var content = DutzCartoonDialogGui.ContentRect(frame);

        GUILayout.BeginArea(content);
        GUILayout.Space(DutzCartoonDialogGui.PanelPadding);

        var titleStyle = DutzCartoonDialogGui.BannerTitleStyle();
        GUILayout.Label("ADD YOUR PHOTO", titleStyle);
        GUILayout.Space(8f);

        var previewHeight = Mathf.Max(220f, frame.height * 0.48f);
        var previewRect = GUILayoutUtility.GetRect(content.width - 24f, previewHeight);
        GUI.color = Color.black;
        GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        if (captured != null)
            GUI.DrawTexture(previewRect, captured, ScaleMode.ScaleToFit);
        else if (webCam != null && webCam.isPlaying)
            DrawWebCamPreview(previewRect);
        else if (!string.IsNullOrEmpty(statusMessage))
        {
            var hint = DutzCartoonDialogGui.HintStyle();
            GUI.Label(previewRect, statusMessage, hint);
        }

        GUILayout.Space(10f);

        if (captured == null && !starting && webCam != null && webCam.isPlaying)
        {
            if (DutzCartoonDialogGui.ActionButton("CAPTURE SELFIE"))
                CaptureCurrentFrame();
        }
        else if (captured != null)
        {
            if (DutzCartoonDialogGui.ActionButton("USE THIS PHOTO"))
                Finish(captured);
            GUILayout.Space(6f);
            if (DutzCartoonDialogGui.DismissButton("RETAKE"))
            {
                Destroy(captured);
                captured = null;
                StartCoroutine(StartCameraRoutine());
            }
        }

        GUILayout.Space(6f);
        if (DutzCartoonDialogGui.DismissButton("CANCEL"))
            Cancel();

         GUILayout.EndArea();
        GUI.depth = previousDepth;
    }

    void DrawWebCamPreview(Rect previewRect)
    {
        if (webCam == null || !webCam.isPlaying)
            return;

        var rotation = webCam.videoRotationAngle;
        var mirrored = webCam.videoVerticallyMirrored;
        var matrixBackup = GUI.matrix;
        var center = new Vector2(previewRect.x + previewRect.width * 0.5f, previewRect.y + previewRect.height * 0.5f);
        GUIUtility.RotateAroundPivot(rotation, center);
        if (mirrored)
            GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), center);

        GUI.DrawTexture(previewRect, webCam, ScaleMode.ScaleToFit);
        GUI.matrix = matrixBackup;
    }

    void CaptureCurrentFrame()
    {
        if (webCam == null || !webCam.isPlaying)
            return;

        var width = webCam.width;
        var height = webCam.height;
        var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(webCam, rt);

        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        captured = new Texture2D(width, height, TextureFormat.RGB24, false);
        captured.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        captured.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        captured = ApplyWebCamOrientation(captured, webCam.videoRotationAngle, webCam.videoVerticallyMirrored);
        StopCamera();
    }

    static Texture2D ApplyWebCamOrientation(Texture2D source, int rotationAngle, bool verticallyMirrored)
    {
        if (source == null)
            return null;

        var working = source;
        if (verticallyMirrored)
            working = FlipTexture(working, horizontal: true, vertical: false);

        if (rotationAngle == 90 || rotationAngle == 180 || rotationAngle == 270)
            working = RotateTexture(working, rotationAngle);

        if (working != source)
            Destroy(source);

        return working;
    }

    static Texture2D FlipTexture(Texture2D source, bool horizontal, bool vertical)
    {
        var width = source.width;
        var height = source.height;
        var pixels = source.GetPixels32();
        var flipped = new Color32[pixels.Length];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcX = horizontal ? width - 1 - x : x;
                var srcY = vertical ? height - 1 - y : y;
                flipped[y * width + x] = pixels[srcY * width + srcX];
            }
        }

        var result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.SetPixels32(flipped);
        result.Apply();
        return result;
    }

    static Texture2D RotateTexture(Texture2D source, int angle)
    {
        var width = source.width;
        var height = source.height;
        var pixels = source.GetPixels32();

        if (angle == 180)
            return FlipTexture(source, horizontal: true, vertical: true);

        var outWidth = angle == 90 || angle == 270 ? height : width;
        var outHeight = angle == 90 || angle == 270 ? width : height;
        var rotated = new Color32[outWidth * outHeight];

        if (angle == 90)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                rotated[x * outHeight + (outHeight - 1 - y)] = pixels[y * width + x];
        }
        else if (angle == 270)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                rotated[(outWidth - 1 - x) * outHeight + y] = pixels[y * width + x];
        }

        var result = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);
        result.SetPixels32(rotated);
        result.Apply();
        return result;
    }

    void Cancel() => Finish(null);

    void Finish(Texture2D photo)
    {
        active = false;
        StopCamera();
        var cb = finishCallback;
        finishCallback = null;
        cb?.Invoke(photo);
    }

    void StopCamera()
    {
        if (webCam == null)
            return;

        if (webCam.isPlaying)
            webCam.Stop();

        Destroy(webCam);
        webCam = null;
    }

    void OnDestroy()
    {
        StopCamera();
        if (captured != null)
            Destroy(captured);

        if (instance == this)
            instance = null;
    }

    public static bool TryPickPhotoFromDisk(out Texture2D texture)
    {
        texture = null;
#if UNITY_EDITOR
        var path = UnityEditor.EditorUtility.OpenFilePanel(
            "Victory selfie photo",
            Application.dataPath,
            "png,jpg,jpeg");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        var bytes = File.ReadAllBytes(path);
        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            texture = null;
            return false;
        }

        return true;
#else
        return false;
#endif
    }
}
