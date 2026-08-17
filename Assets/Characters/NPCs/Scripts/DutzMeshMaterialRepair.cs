using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trims extra material slots on meshes that have fewer submeshes (e.g. Highway_Bridge_1_mesh).
/// </summary>
public static class DutzMeshMaterialRepair
{
    const string BridgeMeshName = "Highway_Bridge_1_mesh";

    public static void EnsureBridgeMeshesRepaired()
    {
        if (!DutzMobileRuntime.IsDutzLevelScene(SceneManager.GetActiveScene().name))
            return;

        var repaired = 0;
        foreach (var renderer in Object.FindObjectsOfType<MeshRenderer>(true))
        {
            if (renderer == null)
                continue;

            if (RepairRenderer(renderer))
                repaired++;
        }

        if (repaired > 0)
            Debug.Log($"[Dutz] Trimmed extra bridge material slots on {repaired} renderer(s).");
    }

    public static bool RepairRenderer(Renderer renderer)
    {
        if (renderer == null)
            return false;

        var meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return false;

        if (!string.Equals(meshFilter.sharedMesh.name, BridgeMeshName, System.StringComparison.Ordinal))
            return false;

        var mesh = meshFilter.sharedMesh;
        var submeshCount = Mathf.Max(1, mesh.subMeshCount);
        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length <= submeshCount)
            return false;

        var trimmed = new Material[submeshCount];
        for (var i = 0; i < submeshCount; i++)
            trimmed[i] = materials[i] != null ? materials[i] : materials[materials.Length - 1];

        renderer.sharedMaterials = trimmed;
        return true;
    }
}
