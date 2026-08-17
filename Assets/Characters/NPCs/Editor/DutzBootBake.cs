using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Bakes boot prerequisites into Dutz level scenes so runtime boot is mostly validation.
/// </summary>
public static class DutzBootBake
{
    public static void BakeFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Bake Boot Prerequisites", "Exit Play mode first.", "OK");
            return;
        }

        var okLevel1 = BakeLevel1();
        var okLevel2 = BakeLevel2();

        if (!okLevel1 || !okLevel2)
        {
            EditorUtility.DisplayDialog(
                "Bake Boot Prerequisites",
                "One or more steps failed. Check the Console for details.",
                "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "Bake Boot Prerequisites",
            "Level 1 and Level 2 scenes were repaired, content was applied, and scenes were saved.",
            "OK");
    }

    /// <summary>Batch: -executeMethod DutzBootBake.BakeBatch</summary>
    public static void BakeBatch() => BakeFromMenu();

    static bool BakeLevel1()
    {
        if (!DutzSceneMissingScriptRepair.RepairScene(DutzLevel02Setup.Level01ScenePath, log: true))
            return false;

        if (!DutzLevel02Setup.ApplyLevel2GameParity(log: true))
            return false;

        var scene = EditorSceneManager.OpenScene(DutzLevel02Setup.Level01ScenePath, OpenSceneMode.Single);
        EnsureSegmentManagerInOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dutz] Baked boot prerequisites for Dutz_Level01.");
        return true;
    }

    static bool BakeLevel2()
    {
        if (!DutzShowcaseSceneRepair.Repair(redistributeCoins: false, log: true))
            return false;

        if (!DutzSceneMissingScriptRepair.RepairScene(DutzShowcaseSceneRepair.Level02ScenePath, log: true))
            return false;

        var scene = EditorSceneManager.OpenScene(DutzShowcaseSceneRepair.Level02ScenePath, OpenSceneMode.Single);

        if (SimpleCitizensHippieNpcSetup.ShowcaseNeedsSegmentHippiePoolApply())
            SimpleCitizensHippieNpcSetup.ApplySegmentHippiePoolToShowcase(log: true);
        else
            SimpleCitizensHippieNpcSetup.EnsureSegmentHippieTeleportProfile();

        if (GameObject.Find("GrandmaGiantDialog") == null)
            DutzGiantWorldDialogBuilder.SetupGrandmaDialog(saveScene: false);

        DutzGiantWorldDialogBuilder.EnsureCawetanDialogOnOpenScene();

        DutzGiantHippieBossFaceBuilder.EnsureCawetanBossFaceOnOpenScene(log: false);

        EnsureSegmentManagerInOpenScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dutz] Baked boot prerequisites for Dutz_Level02.");
        return true;
    }

    static void EnsureSegmentManagerInOpenScene()
    {
        if (GameObject.Find(DutzSegmentHippieIdentity.PoolRootName) == null)
            return;

        if (GameObject.Find(DutzSegmentHippieIdentity.ManagerObjectName) != null)
            return;

        var go = new GameObject(DutzSegmentHippieIdentity.ManagerObjectName);
        Undo.RegisterCreatedObjectUndo(go, "Ensure Segment Hippie Manager");
        go.AddComponent<DutzSegmentHippieManager>();
    }
}
