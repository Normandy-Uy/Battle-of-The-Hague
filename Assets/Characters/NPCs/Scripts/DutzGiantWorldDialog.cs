using UnityEngine;

/// <summary>
/// World-space speech line near a giant — billboards toward the active camera.
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
[DefaultExecutionOrder(310)]
public class DutzGiantWorldDialog : MonoBehaviour
{
    const string GrandmaDialogObjectName = "GrandmaGiantDialog";
    const string CawetanDialogObjectName = "CawetanGiantDialog";

    [SerializeField] Transform anchor;
    [SerializeField] string dialogText = "PLEASE BRING HIM HOME.\nFREE DUTZ.";
    [SerializeField] Vector3 anchorOffset = new Vector3(0f, 22f, 8f);
    [SerializeField] int fontSize = 48;
    [SerializeField] float characterSize = 0.12f;
    [SerializeField] Color fontColor = new Color(1f, 0.95f, 0.72f, 1f);

    TextMesh label;

    public static void EnsureFromBoot()
    {
        EnsureGrandmaDialogFromBoot();
        EnsureCawetanDialogFromBoot();
    }

    static void EnsureGrandmaDialogFromBoot()
    {
        if (DutzCollectibleProgress.IsLevel00)
            return;

        if (GameObject.Find(GrandmaDialogObjectName) != null)
            return;

        var giant = DutzGiantBossNames.FindPrincessZara();
        if (giant == null)
            return;

        var dialog = CreateDialogObject(GrandmaDialogObjectName, giant.transform);
        dialog.ApplyDialog();
    }

    static void EnsureCawetanDialogFromBoot()
    {
        if (GameObject.Find(CawetanDialogObjectName) != null)
            return;

        var giant = DutzGiantBossNames.FindCawetan();
        if (giant == null)
            return;

        var dialog = CreateDialogObject(CawetanDialogObjectName, giant.transform);
        dialog.ApplyDialog();
    }

    void Reset() => ApplyDialog();

    void OnEnable() => ApplyDialog();

    void LateUpdate() => FaceCamera();

    public void ApplyDialog()
    {
        EnsureLabel();
        UpdateLabelText();
        SnapToAnchor();
        FaceCamera();
    }

    void EnsureLabel()
    {
        label = GetComponent<TextMesh>();
        if (label == null)
            label = gameObject.AddComponent<TextMesh>();

        var mobile = Application.isMobilePlatform;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = mobile ? Mathf.RoundToInt(fontSize * 1.55f) : fontSize;
        label.characterSize = mobile ? characterSize * 1.65f : characterSize;
        label.color = fontColor;
        label.fontStyle = FontStyle.Bold;
    }

    void UpdateLabelText()
    {
        if (label == null)
            return;

        label.text = string.IsNullOrWhiteSpace(dialogText) ? string.Empty : dialogText.Trim().ToUpperInvariant();
    }

    void SnapToAnchor()
    {
        if (anchor == null)
            return;

        transform.position = anchor.position +
                             anchor.right * anchorOffset.x +
                             Vector3.up * anchorOffset.y +
                             anchor.forward * anchorOffset.z;
        transform.localScale = Vector3.one;
    }

    void FaceCamera()
    {
        var cam = GetViewCamera();
        if (cam == null)
            return;

        var toCamera = cam.transform.position - transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
    }

    static Camera GetViewCamera()
    {
        if (Application.isPlaying)
        {
            var main = Camera.main;
            if (main != null)
                return main;

            return Object.FindObjectOfType<Camera>();
        }

#if UNITY_EDITOR
        if (UnityEditor.SceneView.lastActiveSceneView != null)
            return UnityEditor.SceneView.lastActiveSceneView.camera;
#endif

        return Camera.main;
    }

    public static DutzGiantWorldDialog CreateDialogObject(string objectName, Transform anchorTransform)
    {
        var existing = GameObject.Find(objectName);
        if (existing != null)
        {
            var existingDialog = existing.GetComponent<DutzGiantWorldDialog>();
            if (existingDialog != null)
            {
                existingDialog.anchor = anchorTransform;
                return existingDialog;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(existing);
            else
#endif
                Object.Destroy(existing);
        }

        var go = new GameObject(objectName);
        go.transform.SetParent(null);
        go.transform.localScale = Vector3.one;

        var dialog = go.AddComponent<DutzGiantWorldDialog>();
        dialog.anchor = anchorTransform;
        return dialog;
    }
}
