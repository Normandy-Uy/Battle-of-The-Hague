using UnityEngine;

/// <summary>Small protest placard among Level 00 crowd — billboards toward the camera.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(320)]
public class DutzLevel00RallyPlacard : MonoBehaviour
{
    const string BoardChildName = "Board";
    const string TextChildName = "Text";
    const float WrapWidthFactor = 0.88f;
    const float FitBoundsFactor = 0.90f;
    const float ShrinkStep = 0.92f;
    const float MinCharacterSizeFactor = 0.35f;
    const float LineHeightFactor = 0.55f;
    const float MaxLineHeightScale = 2.4f;
    const int MaxFitIterations = 24;

    [SerializeField] string placardText = string.Empty;
    [SerializeField] Transform holder;
    [SerializeField] bool followHolderAtRuntime = true;
    [SerializeField] float forwardOffsetMeters;
    [SerializeField] float lateralOffsetMeters;
    [SerializeField] float headClearanceMeters = 0.1f;
    [SerializeField] int layoutVersion;
    [SerializeField] float boardWidthMeters = 0.42f;
    [SerializeField] float boardHeightMeters = 0.28f;
    [SerializeField] float baseBoardHeightMeters = 0.28f;
    [SerializeField] int fontSize = 32;
    [SerializeField] float characterSize = 0.02f;

    TextMesh label;
    Transform boardTransform;
    MeshRenderer boardRenderer;
    MeshRenderer labelRenderer;

    public string PlacardText => placardText;
    public float BoardHeightMeters => boardHeightMeters;
    public int LayoutVersion => layoutVersion;

    public const int CurrentLayoutVersion = 4;

    public void Configure(
        Transform citizenHolder,
        string text,
        float widthMeters,
        float heightMeters,
        float charSize,
        float forwardOffset,
        float lateralOffset,
        float headClearance)
    {
        holder = citizenHolder;
        boardWidthMeters = widthMeters;
        baseBoardHeightMeters = heightMeters;
        boardHeightMeters = heightMeters;
        characterSize = charSize;
        forwardOffsetMeters = forwardOffset;
        lateralOffsetMeters = lateralOffset;
        headClearanceMeters = headClearance;
        layoutVersion = CurrentLayoutVersion;
        FitTextToBoard(text);
        ApplyVisual(refreshPlacement: true);
    }

    void OnEnable() => ApplyVisual(refreshPlacement: false);

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        if (followHolderAtRuntime && holder != null)
            SnapToHolder();

        FaceCamera();
    }

    void SnapToHolder()
    {
        if (holder == null)
            return;

        if (!TryGetRendererBounds(holder, out var bounds))
        {
            transform.position = holder.position + Vector3.up * 3.8f;
            return;
        }

        var headTopY = bounds.max.y + headClearanceMeters + boardHeightMeters * 0.5f;
        transform.position = new Vector3(bounds.center.x, headTopY, bounds.center.z)
                             + holder.forward * forwardOffsetMeters
                             + holder.right * lateralOffsetMeters;
    }

    static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        var renderers = root.GetComponentsInChildren<Renderer>();
        var found = false;

        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return found;
    }

    public void ApplyVisual(bool refreshPlacement = false)
    {
        EnsureBoard();
        EnsureLabel();
        UpdateBoardSize();
        ApplyLabelVisual();

        if (refreshPlacement)
            SnapToHolder();
    }

    /// <summary>Measure-based wrap + shrink so TextMesh stays inside the board.</summary>
    public void FitTextToBoard(string rawSlogan)
    {
        EnsureBoard();
        EnsureLabel();

        var raw = NormalizeSlogan(rawSlogan);
        if (string.IsNullOrEmpty(raw))
        {
            placardText = string.Empty;
            ApplyLabelVisual();
            return;
        }

        if (baseBoardHeightMeters < 0.01f)
            baseBoardHeightMeters = boardHeightMeters;

        var initialCharacterSize = Mathf.Max(characterSize, 0.001f);
        var minCharacterSize = initialCharacterSize * MinCharacterSizeFactor;
        characterSize = initialCharacterSize;

        for (var i = 0; i < MaxFitIterations; i++)
        {
            var wrapped = WrapByMeasuredWidth(raw, boardWidthMeters * WrapWidthFactor, characterSize);
            var lineCount = CountLines(wrapped);
            var heightScale = Mathf.Clamp(Mathf.Max(1f, lineCount * LineHeightFactor), 1f, MaxLineHeightScale);
            boardHeightMeters = baseBoardHeightMeters * heightScale;
            placardText = wrapped;

            UpdateBoardSize();
            ApplyLabelVisual();

            if (TextFitsBoard())
                break;

            var next = characterSize * ShrinkStep;
            if (next < minCharacterSize)
            {
                characterSize = minCharacterSize;
                wrapped = WrapByMeasuredWidth(raw, boardWidthMeters * WrapWidthFactor, characterSize);
                lineCount = CountLines(wrapped);
                heightScale = Mathf.Clamp(Mathf.Max(1f, lineCount * LineHeightFactor), 1f, MaxLineHeightScale);
                boardHeightMeters = baseBoardHeightMeters * heightScale;
                placardText = wrapped;
                UpdateBoardSize();
                ApplyLabelVisual();
                break;
            }

            characterSize = next;
        }
    }

    void EnsureBoard()
    {
        boardTransform = transform.Find(BoardChildName);
        if (boardTransform == null)
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            board.name = BoardChildName;
            board.transform.SetParent(transform, false);
            board.transform.localPosition = Vector3.zero;
            board.transform.localRotation = Quaternion.identity;

            var collider = board.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            boardTransform = board.transform;
            boardRenderer = board.GetComponent<MeshRenderer>();
        }

        if (boardRenderer == null)
            boardRenderer = boardTransform.GetComponent<MeshRenderer>();

        if (boardRenderer != null)
            boardRenderer.sharedMaterial = DutzLevel00RallyPlacardAssets.BoardMaterial;
    }

    void EnsureLabel()
    {
        var textTransform = transform.Find(TextChildName);
        if (textTransform == null)
        {
            var textGo = new GameObject(TextChildName);
            textGo.transform.SetParent(transform, false);
            textTransform = textGo.transform;
        }

        label = textTransform.GetComponent<TextMesh>();
        if (label == null)
            label = textTransform.gameObject.AddComponent<TextMesh>();

        if (label.font == null)
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = fontSize;
        label.color = new Color(0.12f, 0.08f, 0.08f, 1f);
        label.fontStyle = FontStyle.Bold;
        label.lineSpacing = 1f;
        textTransform.localPosition = new Vector3(0f, 0f, -0.004f);
        textTransform.localRotation = Quaternion.identity;
        textTransform.localScale = Vector3.one;
        labelRenderer = label.GetComponent<MeshRenderer>();
    }

    void UpdateBoardSize()
    {
        if (boardTransform == null)
            return;

        boardTransform.localScale = new Vector3(boardWidthMeters, boardHeightMeters, 1f);
    }

    void ApplyLabelVisual()
    {
        if (label == null)
            return;

        label.text = string.IsNullOrWhiteSpace(placardText) ? string.Empty : placardText;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.characterSize = characterSize;
        label.fontSize = fontSize;
        label.fontStyle = FontStyle.Bold;
    }

    bool TextFitsBoard()
    {
        if (label == null)
            return true;

        if (!TryGetTextLocalSize(out var textSize))
        {
            // Fall back to glyph estimate when mesh is not ready yet.
            var widest = 0f;
            foreach (var line in placardText.Split('\n'))
                widest = Mathf.Max(widest, MeasureLineWidth(line, characterSize));

            var lineCount = CountLines(placardText);
            var estimatedHeight = characterSize * fontSize * 0.08f * lineCount;
            return widest <= boardWidthMeters * FitBoundsFactor
                   && estimatedHeight <= boardHeightMeters * FitBoundsFactor;
        }

        return textSize.x <= boardWidthMeters * FitBoundsFactor
               && textSize.y <= boardHeightMeters * FitBoundsFactor;
    }

    bool TryGetTextLocalSize(out Vector2 size)
    {
        size = Vector2.zero;
        if (label == null)
            return false;

        if (labelRenderer == null)
            labelRenderer = label.GetComponent<MeshRenderer>();

        var filter = label.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            var b = filter.sharedMesh.bounds.size;
            size = new Vector2(b.x, b.y);
            return size.x > 0.0001f || size.y > 0.0001f;
        }

        if (labelRenderer != null)
        {
            // World AABB projected back by lossy scale (text scale is 1 under placard).
            var lossy = label.transform.lossyScale;
            var world = labelRenderer.bounds.size;
            size = new Vector2(
                lossy.x > 0.0001f ? world.x / lossy.x : world.x,
                lossy.y > 0.0001f ? world.y / lossy.y : world.y);
            return size.x > 0.0001f || size.y > 0.0001f;
        }

        return false;
    }

    string WrapByMeasuredWidth(string text, float maxWidthMeters, float charSize)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        EnsureFontCharacters(text);
        var words = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        var result = string.Empty;

        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word;
            if (MeasureLineWidth(candidate, charSize) > maxWidthMeters && !string.IsNullOrEmpty(line))
            {
                result = string.IsNullOrEmpty(result) ? line : result + "\n" + line;
                line = word;
            }
            else
            {
                line = candidate;
            }
        }

        if (!string.IsNullOrEmpty(line))
            result = string.IsNullOrEmpty(result) ? line : result + "\n" + line;

        return result;
    }

    float MeasureLineWidth(string line, float charSize)
    {
        if (string.IsNullOrEmpty(line))
            return 0f;

        var font = label != null && label.font != null
            ? label.font
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            return line.Length * charSize * 0.55f;

        var fontBase = Mathf.Max(1, font.fontSize);
        var width = 0f;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (font.GetCharacterInfo(c, out var info, fontSize, FontStyle.Bold))
                width += info.advance;
            else if (font.GetCharacterInfo(c, out info, fontSize, FontStyle.Normal))
                width += info.advance;
            else
                width += fontSize * 0.5f;
        }

        return width * (charSize / fontBase);
    }

    void EnsureFontCharacters(string text)
    {
        var font = label != null && label.font != null
            ? label.font
            : Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null || string.IsNullOrEmpty(text))
            return;

        font.RequestCharactersInTexture(text, fontSize, FontStyle.Bold);
        font.RequestCharactersInTexture(text, fontSize, FontStyle.Normal);
    }

    static string NormalizeSlogan(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return string.Join(" ", text.Replace('\n', ' ').Replace('\r', ' ').Split(
            new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 1;

        var lines = 1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
                lines++;
        }

        return lines;
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
        var main = Camera.main;
        if (main != null)
            return main;

        return Object.FindFirstObjectByType<Camera>();
    }
}

static class DutzLevel00RallyPlacardAssets
{
    static Material boardMaterial;

    public static Material BoardMaterial
    {
        get
        {
            if (boardMaterial != null)
                return boardMaterial;

            var shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            boardMaterial = new Material(shader) { color = new Color(0.98f, 0.95f, 0.86f, 1f) };
            boardMaterial.name = "Level00RallyPlacardBoard";
            return boardMaterial;
        }
    }
}
