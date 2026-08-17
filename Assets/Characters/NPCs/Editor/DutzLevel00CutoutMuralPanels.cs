using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared Level 00 cut-out mural: one alpha panel facing oncoming traffic.
/// </summary>
public static class DutzLevel00CutoutMuralPanels
{
    public static GameObject CreatePanel(
        Transform parent,
        string rootName,
        Texture2D texture,
        Material materialTemplate,
        Vector3 boardCenter,
        Vector3 faceDir,
        float panelHeight,
        string undoName,
        string bumpMessage = null)
    {
        if (texture == null || materialTemplate == null || parent == null)
            return null;

        faceDir.y = 0f;
        if (faceDir.sqrMagnitude < 0.001f)
            faceDir = Vector3.forward;
        faceDir.Normalize();

        var aspect = texture.width / (float)Mathf.Max(1, texture.height);
        var panelWidth = panelHeight * aspect;
        var rotation = Quaternion.LookRotation(faceDir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
        var scale = new Vector3(panelWidth / 10f, 1f, panelHeight / 10f);

        var root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, undoName);
        root.transform.SetParent(parent, false);
        root.transform.SetPositionAndRotation(boardCenter, Quaternion.identity);
        root.transform.localScale = Vector3.one;

        var material = new Material(materialTemplate)
        {
            name = rootName + "_Cutout",
            mainTexture = texture,
            color = Color.white
        };

        var panel = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(panel, undoName);
        panel.name = "Panel";
        panel.transform.SetParent(root.transform, true);
        panel.transform.SetPositionAndRotation(boardCenter, rotation);
        panel.transform.localScale = scale;
        Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.GetComponent<MeshRenderer>().sharedMaterial = material;

        if (bumpMessage != null)
            DutzMuralBumpMessage.Apply(root, bumpMessage);
        else
            DutzMuralBumpMessage.Apply(root);

        return root;
    }

    /// <summary>Legacy name — creates a single cut-out panel.</summary>
    public static GameObject CreateDoublePanel(
        Transform parent,
        string rootName,
        Texture2D texture,
        Material materialTemplate,
        Vector3 boardCenter,
        Vector3 faceDir,
        float panelHeight,
        string undoName,
        string bumpMessage = null) =>
        CreatePanel(
            parent,
            rootName,
            texture,
            materialTemplate,
            boardCenter,
            faceDir,
            panelHeight,
            undoName,
            bumpMessage);
}
