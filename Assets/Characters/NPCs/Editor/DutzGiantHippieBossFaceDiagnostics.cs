using UnityEditor;
using UnityEngine;
public static class DutzGiantHippieBossFaceDiagnostics
{
    const string GiantName = "SimpleCitizens_Hippie_Giant";
    const string HippieMeshName = "SC_Hippie";

    public static void DiagnoseFromMenu()
    {
        var giant = GameObject.Find(GiantName);
        if (giant == null)
        {
            Debug.LogError("[Dutz] Giant not found in scene.");
            return;
        }

        var hippie = giant.transform.Find(HippieMeshName);
        if (hippie == null)
        {
            Debug.LogError("[Dutz] SC_Hippie not found under giant.");
            return;
        }

        var renderer = hippie.GetComponent<SkinnedMeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError("[Dutz] SC_Hippie has no SkinnedMeshRenderer.");
            return;
        }

        var mesh = renderer.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("[Dutz] SC_Hippie mesh is null.");
            return;
        }

        var headBoneIndex = FindBone(renderer, "Head_jnt");
        Transform headBoneTransform = null;
        if (headBoneIndex >= 0)
            headBoneTransform = renderer.bones[headBoneIndex];

        Debug.Log("[Dutz] === Giant Hippie Face Diagnostics ===");
        Debug.Log("[Dutz] SC_Hippie active=" + hippie.gameObject.activeSelf + " renderer.enabled=" + renderer.enabled);
        Debug.Log("[Dutz] Mesh: " + mesh.name + " submeshes=" + mesh.subMeshCount + " verts=" + mesh.vertexCount);

        var materials = renderer.sharedMaterials;
        for (var i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            var tex = mat != null ? mat.mainTexture : null;
            Debug.Log($"[Dutz] Material slot {i}: {(mat != null ? mat.name : "null")} tex={(tex != null ? tex.name : "null")}");
        }

        var bossFace = headBoneTransform != null ? headBoneTransform.Find("BossFace") : null;
        if (bossFace != null)
        {
            var bfRenderer = bossFace.GetComponent<MeshRenderer>();
            var bfMat = bfRenderer != null ? bfRenderer.sharedMaterial : null;
            var bfTex = bfMat != null ? bfMat.mainTexture : null;
            Debug.Log("[Dutz] BossFace billboard: active=" + bossFace.gameObject.activeSelf +
                      " parent=" + bossFace.parent.name +
                      " localPos=" + bossFace.localPosition +
                      " size=" + bossFace.localScale +
                      " mat=" + (bfMat != null ? bfMat.name : "null") +
                      " tex=" + (bfTex != null ? bfTex.name : "null") +
                      " visible=" + (bfRenderer != null && bfRenderer.isVisible));
        }
        else
        {
            Debug.LogWarning("[Dutz] BossFace billboard missing under Head_jnt — run Apply Billboard Face.");
        }

        var faceScript = giant.GetComponent<DutzGiantHippieBossFace>();
        Debug.Log("[Dutz] DutzGiantHippieBossFace: " + (faceScript != null ? "present" : "MISSING"));

        if (headBoneIndex < 0)
        {
            Debug.LogWarning("[Dutz] Head_jnt bone not found on renderer.");
            return;
        }

        Debug.Log("[Dutz] Caricature: Head_jnt 2x scale, arms/legs ~55-60% length.");
    }
    static int FindBone(SkinnedMeshRenderer renderer, string boneName)
    {
        var bones = renderer.bones;
        for (var i = 0; i < bones.Length; i++)
        {
            if (bones[i] != null && bones[i].name == boneName)
                return i;
        }

        return -1;
    }
}