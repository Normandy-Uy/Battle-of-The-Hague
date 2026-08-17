using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places TextMesh Pro slogans on highway straight side walls in Dutz_Level02.
/// Edit slogans on the "Highway Wall Slogans" object in the scene Inspector.
/// Menu: Tools / Dutz / Place Highway Wall Slogans
/// </summary>
public static class DutzHighwayWallSloganPlacer
{
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string SettingsPath = "Assets/Characters/DutzHighwayWallSlogans.asset";
    const string BoardObjectName = "Highway Wall Slogans";
    const string SlogansRootName = "HighwayWallSlogans";
    const int MaxLabelsPerSide = 2;
    const float MaxWallFaceInset = 0.15f;
    static readonly string[] WallSloganSegmentNames = { "Highway Straight 2", "Highway Straight 6" };
    const string DefaultTmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Wall Slogans", "Exit Play mode first.", "OK");
            return;
        }

        if (!PlaceInShowcase(log: true))
        {
            EditorUtility.DisplayDialog(
                "Wall Slogans",
                "Could not place slogans.\n\n" +
                "1. Import TMP Essentials (Window → TextMeshPro → Import TMP Essential Resources)\n" +
                "2. Ensure Dutz_Level02 has Highway Straight 2 and Highway Straight 6 segments",
                "OK");
        }
    }

    /// <summary>Batch: -executeMethod DutzHighwayWallSloganPlacer.PlaceInShowcase</summary>
    public static void PlaceInShowcase() => PlaceInShowcase(log: false);

    public static void ApplyFromBoard(DutzHighwayWallSloganBoard board)
    {
        if (board == null || EditorApplication.isPlaying)
            return;

        if (board.slogans == null || board.slogans.Length == 0)
        {
            Debug.LogWarning("[Dutz] Add at least one slogan before applying.");
            return;
        }

        var updated = 0;
        foreach (var segment in FindAllHighwaySegments())
        {
            var root = FindSlogansRoot(segment);
            if (root == null)
                continue;

            updated += ApplyToSegmentLabels(segment, root, board);
        }

        if (updated == 0)
        {
            Debug.LogWarning("[Dutz] No wall labels found. Click Rebuild All Wall Labels first.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        Debug.Log($"[Dutz] Applied slogans to {updated} wall label(s).");
    }

    public static void RebuildFromBoard(DutzHighwayWallSloganBoard board)
    {
        if (board == null || EditorApplication.isPlaying)
            return;

        if (board.slogans == null || board.slogans.Length == 0)
        {
            Debug.LogWarning("[Dutz] Add at least one slogan before rebuilding.");
            return;
        }

        var font = LoadFont();
        if (font == null)
        {
            Debug.LogError("[Dutz] TMP font not found. Import TMP Essential Resources first.");
            return;
        }

        ClearAll();
        var segments = FindAllHighwaySegments();
        var totalLabels = 0;
        foreach (var segment in segments)
            totalLabels += PlaceOnSegment(segment, board, font);

        EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        Debug.Log($"[Dutz] Rebuilt {totalLabels} wall slogans on {segments.Count} segment(s).");
    }

    static bool PlaceInShowcase(bool log)
    {
        EnsureTmpEssentials();
        EnsureSettingsAsset();

        var font = LoadFont();
        if (font == null)
        {
            Debug.LogError("[Dutz] TMP font not found. Import TMP Essential Resources first.");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var board = EnsureSceneBoard();
        if (board.slogans == null || board.slogans.Length == 0)
        {
            Debug.LogError("[Dutz] Highway Wall Slogans board has no slogans.");
            return false;
        }

        ClearAll();

        var segments = FindAllHighwaySegments();
        if (segments.Count == 0)
        {
            Debug.LogWarning("[Dutz] No highway straight wall-slogan segments found in scene.");
            return false;
        }

        var totalLabels = 0;
        foreach (var segment in segments)
            totalLabels += PlaceOnSegment(segment, board, font);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
            Debug.Log($"[Dutz] Placed {totalLabels} wall slogans on {segments.Count} highway straight segment(s). " +
                      $"Edit slogans on '{BoardObjectName}' in the hierarchy. " +
                      "In Scene view, select 'HighwayWallSlogans' roots and press F to frame them.");

        return totalLabels > 0;
    }

    static DutzHighwayWallSloganBoard EnsureSceneBoard()
    {
        var board = Object.FindObjectOfType<DutzHighwayWallSloganBoard>();
        if (board != null)
            return board;

        var go = new GameObject(BoardObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Create Highway Wall Slogans Board");
        board = go.AddComponent<DutzHighwayWallSloganBoard>();

        var asset = AssetDatabase.LoadAssetAtPath<DutzHighwayWallSloganSettings>(SettingsPath);
        if (asset != null)
            board.CopyFrom(asset);

        return board;
    }

    static TMP_FontAsset LoadFont()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTmpFontPath);
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return font;
    }

    static int ApplyToSegmentLabels(GameObject segment, Transform root, DutzHighwayWallSloganBoard board)
    {
        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return 0;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        GetRoadAndWallAxes(bounds, out var roadAxis, out var wallAxis);
        var wallSpan = ProjectSpan(bounds, wallAxis);
        var wallMid = (wallSpan.min + wallSpan.max) * 0.5f;

        var labels = new List<TextMeshPro>();
        for (var i = 0; i < root.childCount; i++)
        {
            var tmp = root.GetChild(i).GetComponent<TextMeshPro>();
            if (tmp != null)
                labels.Add(tmp);
        }

        if (labels.Count == 0)
            return 0;

        labels.Sort((a, b) =>
        {
            var roadCmp = Vector3.Dot(a.transform.position, roadAxis)
                .CompareTo(Vector3.Dot(b.transform.position, roadAxis));
            if (roadCmp != 0)
                return roadCmp;

            var aRight = Vector3.Dot(a.transform.position, wallAxis) >= wallMid;
            var bRight = Vector3.Dot(b.transform.position, wallAxis) >= wallMid;
            return aRight.CompareTo(bRight);
        });

        var fontSize = board.scaleFontFromWallHeight
            ? bounds.size.y * board.fontSizePerWallHeight
            : board.fontSize;

        var slotsPerSide = Mathf.Max(1, labels.Count / 2);
        var updated = 0;
        var leftSlot = 0;
        var rightSlot = 0;
        for (var i = 0; i < labels.Count; i++)
        {
            var isRight = Vector3.Dot(labels[i].transform.position, wallAxis) >= wallMid;
            var slotIndex = isRight ? rightSlot++ : leftSlot++;
            var slogan = PickSloganForWallSlot(board, isRight, slotIndex, slotsPerSide);
            if (string.IsNullOrWhiteSpace(slogan))
                continue;

            var tmp = labels[i];
            Undo.RecordObject(tmp, "Apply Highway Wall Slogans");
            tmp.text = slogan.Trim().ToUpperInvariant();
            tmp.fontSize = fontSize;
            tmp.color = board.fontColor;
            tmp.characterSpacing = board.characterSpacing;
            tmp.outlineColor = board.outlineColor;
            tmp.outlineWidth = board.outlineWidth;
            updated++;
        }

        return updated;
    }

    static void EnsureTmpEssentials()
    {
        if (System.IO.File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            return;

        var packagePath = System.IO.Path.GetFullPath("Packages/com.unity.textmeshpro");
        var essentials = packagePath + "/Package Resources/TMP Essential Resources.unitypackage";
        if (!System.IO.File.Exists(essentials))
        {
            Debug.LogError("[Dutz] TMP package found but Essential Resources unitypackage is missing.");
            return;
        }

        AssetDatabase.ImportPackage(essentials, false);
        AssetDatabase.Refresh();
    }

    static void EnsureSettingsAsset()
    {
        if (AssetDatabase.LoadAssetAtPath<DutzHighwayWallSloganSettings>(SettingsPath) != null)
            return;

        var dir = System.IO.Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Characters"))
                AssetDatabase.CreateFolder("Assets", "Characters");
        }

        var asset = ScriptableObject.CreateInstance<DutzHighwayWallSloganSettings>();
        AssetDatabase.CreateAsset(asset, SettingsPath);
        AssetDatabase.SaveAssets();
    }

    static List<GameObject> FindAllHighwaySegments()
    {
        var list = new List<GameObject>();
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root != null)
                CollectHighwaySegments(root.transform, list);
        }

        return list;
    }

    static void CollectHighwaySegments(Transform node, List<GameObject> list)
    {
        foreach (var segmentName in WallSloganSegmentNames)
        {
            if (node.name == segmentName)
            {
                list.Add(node.gameObject);
                break;
            }
        }

        for (var i = 0; i < node.childCount; i++)
            CollectHighwaySegments(node.GetChild(i), list);
    }

    static int ClearAll()
    {
        var count = 0;
        foreach (var segment in FindAllHighwaySegments())
        {
            var existing = FindSlogansRoot(segment);
            if (existing == null)
                continue;

            Undo.DestroyObjectImmediate(existing.gameObject);
            count++;
        }

        return count;
    }

    static Transform FindSlogansRoot(GameObject segment)
    {
        var child = segment.transform.Find(SlogansRootName);
        if (child != null)
            return child;

        var sceneRoot = GameObject.Find($"{SlogansRootName} ({segment.name})");
        return sceneRoot != null ? sceneRoot.transform : null;
    }

    static int PlaceOnSegment(GameObject segment, DutzHighwayWallSloganBoard board, TMP_FontAsset font)
    {
        var collider = segment.GetComponent<MeshCollider>();
        var renderer = segment.GetComponent<Renderer>();
        if (collider == null && renderer == null)
            return 0;

        var bounds = collider != null ? collider.bounds : renderer.bounds;
        GetRoadAndWallAxes(bounds, out var roadAxis, out var wallAxis);
        var roadSpan = ProjectSpan(bounds, roadAxis);
        var wallSpan = ProjectSpan(bounds, wallAxis);
        var length = roadSpan.max - roadSpan.min;
        if (length < 10f)
            return 0;

        var labelCount = Mathf.Min(MaxLabelsPerSide, Mathf.Max(1, Mathf.FloorToInt(length / Mathf.Max(20f, board.spacingAlongWall))));
        var step = length / (labelCount + 1);
        var fontSize = board.scaleFontFromWallHeight
            ? Mathf.Max(board.fontSize, bounds.size.y * board.fontSizePerWallHeight)
            : board.fontSize;
        fontSize = Mathf.Max(fontSize, 32f);

        var root = new GameObject($"{SlogansRootName} ({segment.name})");
        Undo.RegisterCreatedObjectUndo(root, "Place Highway Wall Slogans");
        // Scene-root parent keeps TMP at uniform world scale (highway scale is 4×12×100).
        root.transform.SetParent(null);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        var placed = 0;
        for (var i = 0; i < labelCount; i++)
        {
            var t = roadSpan.min + step * (i + 1);
            var roadPoint = PointOnAxis(bounds.center, roadAxis, t);
            var leftSlogan = PickSloganForWallSlot(board, isRightWall: false, slotIndex: i, slotsPerSide: labelCount);
            if (!string.IsNullOrWhiteSpace(leftSlogan))
            {
                PlaceLabel(root.transform, leftSlogan, roadPoint, wallSpan.min, wallAxis, roadAxis, bounds, board, font, fontSize, collider, isLeftSide: true);
                placed++;
            }
        }

        for (var i = 0; i < labelCount; i++)
        {
            var t = roadSpan.min + step * (i + 1);
            var roadPoint = PointOnAxis(bounds.center, roadAxis, t);
            var rightSlogan = PickSloganForWallSlot(board, isRightWall: true, slotIndex: i, slotsPerSide: labelCount);
            if (!string.IsNullOrWhiteSpace(rightSlogan))
            {
                PlaceLabel(root.transform, rightSlogan, roadPoint, wallSpan.max, wallAxis, roadAxis, bounds, board, font, fontSize, collider, isLeftSide: false);
                placed++;
            }
        }

        return placed;
    }

    static List<string> GetNonEmptySlogans(DutzHighwayWallSloganBoard board)
    {
        var list = new List<string>();
        if (board.slogans == null)
            return list;

        foreach (var slogan in board.slogans)
        {
            if (!string.IsNullOrWhiteSpace(slogan))
                list.Add(slogan);
        }

        return list;
    }

    static string PickSloganForWallSlot(DutzHighwayWallSloganBoard board, bool isRightWall, int slotIndex, int slotsPerSide)
    {
        var slogans = GetNonEmptySlogans(board);
        if (slogans.Count == 0)
            return null;

        var sideOffset = isRightWall ? slotsPerSide : 0;
        return slogans[(sideOffset + slotIndex) % slogans.Count];
    }

    static void GetRoadAndWallAxes(Bounds bounds, out Vector3 roadAxis, out Vector3 wallAxis)
    {
        if (bounds.extents.x >= bounds.extents.z)
        {
            roadAxis = Vector3.right;
            wallAxis = Vector3.forward;
        }
        else
        {
            roadAxis = Vector3.forward;
            wallAxis = Vector3.right;
        }
    }

    static Vector3 PointOnAxis(Vector3 center, Vector3 axis, float axisValue)
    {
        var offset = axisValue - Vector3.Dot(center, axis);
        return center + axis * offset;
    }

    static void PlaceLabel(
        Transform parent,
        string text,
        Vector3 roadPoint,
        float wallAxisValue,
        Vector3 wallAxis,
        Vector3 roadAxis,
        Bounds bounds,
        DutzHighwayWallSloganBoard board,
        TMP_FontAsset font,
        float fontSize,
        MeshCollider collider,
        bool isLeftSide)
    {
        var centerOnWall = Vector3.Dot(bounds.center, wallAxis);
        var wallNormal = centerOnWall >= wallAxisValue ? wallAxis : -wallAxis;
        var desiredY = bounds.center.y + bounds.extents.y * board.heightOnWall + board.verticalOffset;
        var faceInset = Mathf.Min(board.wallFaceInset, MaxWallFaceInset);

        var origin = roadPoint;
        origin.y = desiredY;

        Vector3 wallPos;
        var faceDir = wallNormal;

        if (collider != null)
        {
            var maxDist = Mathf.Max(bounds.extents.x, bounds.extents.z) * 2f;
            if (collider.Raycast(new Ray(origin, -wallNormal), out var hit, maxDist))
            {
                faceDir = hit.normal;
                if (Vector3.Dot(faceDir, wallNormal) < 0f)
                    faceDir = -faceDir;
                wallPos = hit.point + faceDir * faceInset;
                wallPos.y = desiredY;
            }
            else
            {
                wallPos = PointOnAxis(roadPoint, wallAxis, wallAxisValue);
                wallPos.y = desiredY;
                wallPos += faceDir * faceInset;
            }
        }
        else
        {
            wallPos = PointOnAxis(roadPoint, wallAxis, wallAxisValue);
            wallPos.y = desiredY;
            wallPos += faceDir * faceInset;
        }

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = wallNormal;
        faceDir.Normalize();

        var finalRotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);

        var go = new GameObject(isLeftSide ? "Slogan_L" : "Slogan_R");
        Undo.RegisterCreatedObjectUndo(go, "Place Highway Wall Slogan");
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(wallPos, finalRotation);
        go.transform.localScale = Vector3.one;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        tmp.text = text.Trim().ToUpperInvariant();
        tmp.fontSize = fontSize;
        tmp.color = board.fontColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.characterSpacing = board.characterSpacing;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineColor = board.outlineColor;
        tmp.outlineWidth = board.outlineWidth;
        tmp.rectTransform.sizeDelta = new Vector2(180f, 36f);
        tmp.raycastTarget = false;
        tmp.richText = false;
        tmp.parseCtrlCharacters = false;
        tmp.enableCulling = false;
        tmp.isTextObjectScaleStatic = true;
        tmp.ForceMeshUpdate(true, true);
        // TMP RectTransform can reset pose after mesh build — lock world placement again.
        go.transform.SetPositionAndRotation(wallPos, finalRotation);
    }

    static (float min, float max) ProjectSpan(Bounds bounds, Vector3 axis)
    {
        var c = bounds.center;
        var e = bounds.extents;
        var corners = new[]
        {
            c + new Vector3( e.x,  e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z),
            c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z),
            c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z),
            c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x, -e.y, -e.z)
        };

        var min = float.PositiveInfinity;
        var max = float.NegativeInfinity;
        foreach (var corner in corners)
        {
            var d = Vector3.Dot(corner, axis);
            min = Mathf.Min(min, d);
            max = Mathf.Max(max, d);
        }

        return (min, max);
    }
}

[CustomEditor(typeof(DutzHighwayWallSloganBoard))]
public class DutzHighwayWallSloganBoardEditor : Editor
{
    ReorderableList _sloganList;

    void OnEnable()
    {
        var slogansProp = serializedObject.FindProperty("slogans");
        _sloganList = new ReorderableList(serializedObject, slogansProp, true, true, true, true);
        _sloganList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Wall Slogans");
        _sloganList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var prop = _sloganList.serializedProperty.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight * 2f;
            prop.stringValue = EditorGUI.TextArea(rect, prop.stringValue ?? string.Empty);
        };
        _sloganList.elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 2f + 6f;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Edit slogans below, then use Apply to update existing labels or Rebuild to reposition everything.",
            MessageType.Info);

        _sloganList.DoLayoutList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spacingAlongWall"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallFaceInset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("heightOnWall"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wallSideExtent"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleFontFromWallHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSizePerWallHeight"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fontColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineColor"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("outlineWidth"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("characterSpacing"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        var board = (DutzHighwayWallSloganBoard)target;

        if (GUILayout.Button("Apply To Wall Labels", GUILayout.Height(28)))
            DutzHighwayWallSloganPlacer.ApplyFromBoard(board);

        if (GUILayout.Button("Rebuild All Wall Labels", GUILayout.Height(28)))
            DutzHighwayWallSloganPlacer.RebuildFromBoard(board);
    }
}
