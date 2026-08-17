using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Removes missing-script slots and rebinds scene-local MonoScript references to real assets.
/// </summary>
public static class DutzSceneMissingScriptRepair
{
    public static void RepairLevel02FromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Repair Missing Scripts", "Exit Play mode first.", "OK");
            return;
        }

        var okLevel0 = !File.Exists(DutzLevel02Setup.Level00ScenePath)
            || RepairScene(DutzLevel02Setup.Level00ScenePath, log: false);
        var okLevel1 = RepairScene(DutzLevel02Setup.Level01ScenePath, log: false);
        var okLevel2 = RepairScene(DutzShowcaseSceneRepair.Level02ScenePath, log: false);
        var okLevel3 = RepairScene(DutzLevel02Setup.Level03ScenePath, log: false);

        if (!okLevel0 || !okLevel1 || !okLevel2 || !okLevel3)
            EditorUtility.DisplayDialog("Repair Missing Scripts", "Repair failed. Check the Console.", "OK");
        else
            Debug.Log("[Dutz] Repaired scene scripts on Level 0, Level 1, Level 2, and Level 3 scenes.");
    }

    /// <summary>Batch: -executeMethod DutzSceneMissingScriptRepair.RepairAllLevelsBatch</summary>
    public static void RepairAllLevelsBatch()
    {
        RepairAllLevelsSilent(log: true);
    }

    public static void RepairAllLevelsSilent(bool log = true)
    {
        FixEmbeddedScriptRefsInYaml(DutzLevel02Setup.Level00ScenePath, log);
        FixEmbeddedScriptRefsInYaml(DutzLevel02Setup.Level01ScenePath, log);
        FixEmbeddedScriptRefsInYaml(DutzShowcaseSceneRepair.Level02ScenePath, log);
        FixEmbeddedScriptRefsInYaml(DutzLevel02Setup.Level03ScenePath, log);
        FixMultiClassIdentifiersInYaml(DutzLevel02Setup.Level00ScenePath, log);
        FixMultiClassIdentifiersInYaml(DutzLevel02Setup.Level01ScenePath, log);
        FixMultiClassIdentifiersInYaml(DutzShowcaseSceneRepair.Level02ScenePath, log);
        FixMultiClassIdentifiersInYaml(DutzLevel02Setup.Level03ScenePath, log);
        FixMisboundLevelObjectiveScriptsInYaml(DutzLevel02Setup.Level00ScenePath, log);
        FixMisboundLevelObjectiveScriptsInYaml(DutzLevel02Setup.Level01ScenePath, log);
        FixMisboundLevelObjectiveScriptsInYaml(DutzShowcaseSceneRepair.Level02ScenePath, log);
        FixMisboundLevelObjectiveScriptsInYaml(DutzLevel02Setup.Level03ScenePath, log);
        FixEndHouseMarkerScriptInYaml(DutzLevel02Setup.Level01ScenePath, log);
        FixEndHouseMarkerScriptInYaml(DutzShowcaseSceneRepair.Level02ScenePath, log);
        AssetDatabase.Refresh();

        if (File.Exists(DutzLevel02Setup.Level00ScenePath))
            RepairScene(DutzLevel02Setup.Level00ScenePath, log);
        RepairScene(DutzLevel02Setup.Level01ScenePath, log);
        RepairScene(DutzShowcaseSceneRepair.Level02ScenePath, log);
        RepairScene(DutzLevel02Setup.Level03ScenePath, log);
    }

    public static void FixEmbeddedScriptRefsInYamlFromMenu()
    {
        FixEmbeddedScriptRefsInYaml(DutzLevel02Setup.Level01ScenePath, log: true);
        FixEmbeddedScriptRefsInYaml(DutzShowcaseSceneRepair.Level02ScenePath, log: true);
        FixEmbeddedScriptRefsInYaml(DutzLevel02Setup.Level03ScenePath, log: true);
        AssetDatabase.Refresh();
        Debug.Log("[Dutz] YAML embedded-script fix complete for Levels 1–3.");
    }

    static void FixEmbeddedScriptRefsInYaml(string scenePath, bool log)
    {
        if (!System.IO.File.Exists(scenePath))
            return;

        const string guidRef = "m_Script: {fileID: 11500000, guid: d61e396f03121594ea823fb2a9456fe8, type: 3}";
        var text = System.IO.File.ReadAllText(scenePath);
        var matches = System.Text.RegularExpressions.Regex.Matches(
            text,
            @"m_Script: \{fileID: (?!11500000)\d+\}");
        if (matches.Count == 0)
        {
            if (log)
                Debug.Log($"[Dutz] YAML fix {scenePath}: no embedded script refs.");
            return;
        }

        var newText = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"m_Script: \{fileID: (?!11500000)\d+\}",
            guidRef);
        System.IO.File.WriteAllText(scenePath, newText);
        if (log)
            Debug.Log($"[Dutz] YAML fix {scenePath}: rebound {matches.Count} embedded script ref(s).");
    }

    static void FixMultiClassIdentifiersInYaml(string scenePath, bool log)
    {
        if (!System.IO.File.Exists(scenePath))
            return;

        var text = System.IO.File.ReadAllText(scenePath);
        var fixes = 0;
        var newText = ApplyMultiClassIdentifier(text, "meshLocalBounds:", "Assembly-CSharp::DutzEndHouseCollider", ref fixes);
        newText = ApplyMultiClassIdentifier(newText, "worldRoofMinY:", "Assembly-CSharp::DutzEndHouseCollider", ref fixes);
        newText = ApplyMultiClassIdentifier(newText, "spinSpeed:", "Assembly-CSharp::DutzGoldCoin", ref fixes);
        newText = ApplyMultiClassIdentifier(newText, "bobAmplitude:", "Assembly-CSharp::DutzForceFieldSuitPickup", ref fixes);

        if (fixes == 0)
        {
            if (log)
                Debug.Log($"[Dutz] YAML class-id fix {scenePath}: nothing to fix.");
            return;
        }

        System.IO.File.WriteAllText(scenePath, newText);
        if (log)
            Debug.Log($"[Dutz] YAML class-id fix {scenePath}: set {fixes} class identifier(s).");
    }

    static string ApplyMultiClassIdentifier(string text, string fieldLine, string classIdentifier, ref int fixes)
    {
        var pattern =
            "(m_Script: \\{fileID: 11500000, guid: d61e396f03121594ea823fb2a9456fe8, type: 3\\}\\s*\\r?\\n"
            + "  m_Name: \\s*\\r?\\n"
            + ")m_EditorClassIdentifier:\\s*(?:Assembly-CSharp::\\w+)?\\s*\\r?\\n"
            + $"(?=[\\s\\S]{{0,240}}?\\r?\\n  {System.Text.RegularExpressions.Regex.Escape(fieldLine)})";

        var localFixes = 0;
        var replaced = System.Text.RegularExpressions.Regex.Replace(
            text,
            pattern,
            match =>
            {
                localFixes++;
                return match.Groups[1].Value + "m_EditorClassIdentifier: " + classIdentifier + "\n";
            });
        fixes += localFixes;
        return replaced;
    }

    static void FixMisboundLevelObjectiveScriptsInYaml(string scenePath, bool log)
    {
        if (!System.IO.File.Exists(scenePath))
            return;

        const string wrongScriptRef = "m_Script: {fileID: 11500000, guid: d61e396f03121594ea823fb2a9456fe8, type: 3}";
        const string wrongFields =
            "  m_Name: \r\n"
            + "  m_EditorClassIdentifier: \r\n"
            + "  levelDurationSeconds: 240\r\n"
            + "  winScoreHoldSeconds: 1.4\r\n"
            + "  startMessage: Addicts Incoming!!\r\n"
            + "  startMessageDuration: 2\r\n"
            + "  timeoutMessage: Time is up! You lose.\r\n"
            + "  winMessage: Great job! You reached the goal!";

        const string coinScriptRef = "m_Script: {fileID: 11500000, guid: " + GoldCoinScriptGuid + ", type: 3}";
        const string coinFields =
            "  m_Name: \r\n"
            + "  m_EditorClassIdentifier: \r\n"
            + "  spinSpeed: 120\r\n"
            + "  spawnPose:\r\n"
            + "    position: {x: 0, y: 0, z: 0}\r\n"
            + "    eulerAngles: {x: 0, y: 0, z: 0}\r\n"
            + "    localScale: {x: 0, y: 0, z: 0}";

        const string flagPoleScriptRef = "m_Script: {fileID: 11500000, guid: " + FlagPoleGoalScriptGuid + ", type: 3}";
        const string flagPoleFields = "  m_Name: \r\n  m_EditorClassIdentifier: ";

        const string suitScriptRef = "m_Script: {fileID: 11500000, guid: " + ForceFieldSuitScriptGuid + ", type: 3}";
        const string suitFields =
            "  m_Name: \r\n"
            + "  m_EditorClassIdentifier: \r\n"
            + "  spinSpeed: 90\r\n"
            + "  bobAmplitude: 0.15\r\n"
            + "  bobFrequency: 1.4";

        var text = System.IO.File.ReadAllText(scenePath);
        if (!text.Contains(wrongScriptRef) || !text.Contains("levelDurationSeconds: 240"))
        {
            if (log)
                Debug.Log($"[Dutz] Misbound objective YAML fix {scenePath}: nothing to fix.");
            return;
        }

        var wrongBlock = wrongScriptRef + "\r\n" + wrongFields;
        var coinBlock = coinScriptRef + "\r\n" + coinFields;
        var flagPoleBlock = flagPoleScriptRef + "\r\n" + flagPoleFields;
        var suitBlock = suitScriptRef + "\r\n" + suitFields;

        var newText = text;
        var coinFixes = 0;
        var flagPoleFixes = 0;
        var suitFixes = 0;

        var flagPolePattern =
            "(--- !u!114 &\\d+\\s*\\r?\\n"
            + "MonoBehaviour:[\\s\\S]*?"
            + "m_GameObject: \\{fileID: 47961128\\}[\\s\\S]*?"
            + ")" + System.Text.RegularExpressions.Regex.Escape(wrongBlock);

        newText = System.Text.RegularExpressions.Regex.Replace(
            newText,
            flagPolePattern,
            match =>
            {
                flagPoleFixes++;
                return match.Groups[1].Value + flagPoleBlock;
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);

        var suitPattern =
            "(--- !u!114 &469746680\\s*\\r?\\n"
            + "MonoBehaviour:[\\s\\S]*?"
            + ")" + System.Text.RegularExpressions.Regex.Escape(wrongBlock);

        newText = System.Text.RegularExpressions.Regex.Replace(
            newText,
            suitPattern,
            match =>
            {
                suitFixes++;
                return match.Groups[1].Value + suitBlock;
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);

        newText = FixMisboundBlocksOnNamedObject(newText, "DutzForceFieldSuit", wrongBlock, suitBlock, ref suitFixes);

        while (newText.Contains(wrongBlock))
        {
            newText = newText.Replace(wrongBlock, coinBlock);
            coinFixes++;
        }

        if (newText == text)
        {
            if (log)
                Debug.Log($"[Dutz] Misbound objective YAML fix {scenePath}: pattern did not match.");
            return;
        }

        System.IO.File.WriteAllText(scenePath, newText);
        if (log)
        {
            Debug.Log(
                $"[Dutz] Misbound objective YAML fix {scenePath}: "
                + $"{coinFixes} coin(s), {flagPoleFixes} flagpole(s), {suitFixes} suit(s).");
        }
    }

    static string FixMisboundBlocksOnNamedObject(
        string text,
        string objectName,
        string wrongBlock,
        string correctBlock,
        ref int fixes)
    {
        var goPattern =
            "--- !u!1 &(\\d+)\\s*\\r?\\nGameObject:[\\s\\S]*?\\r?\\n  m_Name: "
            + System.Text.RegularExpressions.Regex.Escape(objectName);
        var goMatch = System.Text.RegularExpressions.Regex.Match(text, goPattern);
        if (!goMatch.Success)
            return text;

        var goId = goMatch.Groups[1].Value;
        var componentPattern =
            "(--- !u!114 &\\d+\\s*\\r?\\n"
            + "MonoBehaviour:[\\s\\S]*?"
            + "m_GameObject: \\{fileID: " + goId + "\\}[\\s\\S]*?"
            + ")" + System.Text.RegularExpressions.Regex.Escape(wrongBlock);

        var localFixes = 0;
        var replaced = System.Text.RegularExpressions.Regex.Replace(
            text,
            componentPattern,
            match =>
            {
                localFixes++;
                return match.Groups[1].Value + correctBlock;
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);
        fixes += localFixes;
        return replaced;
    }

    const string EndHouseScriptGuid = "8063ecddc60a88747a91412b1fdd1ea0";

    static void FixEndHouseMarkerScriptInYaml(string scenePath, bool log)
    {
        if (!System.IO.File.Exists(scenePath))
            return;

        const string oldScriptRef = "m_Script: {fileID: 11500000, guid: d61e396f03121594ea823fb2a9456fe8, type: 3}";
        const string newScriptRef = "m_Script: {fileID: 11500000, guid: " + EndHouseScriptGuid + ", type: 3}";

        var text = System.IO.File.ReadAllText(scenePath);
        if (!text.Contains("levelDurationSeconds: 240") || !text.Contains(oldScriptRef))
        {
            if (log)
                Debug.Log($"[Dutz] End-house YAML fix {scenePath}: nothing to fix.");
            return;
        }

        var pattern =
            "(--- !u!114 &\\d+\\s*\\r?\\n"
            + "MonoBehaviour:\\s*\\r?\\n"
            + "[\\s\\S]*?"
            + oldScriptRef.Replace("{", "\\{").Replace("}", "\\}")
            + "\\s*\\r?\\n"
            + "  m_Name:\\s*\\r?\\n"
            + "  m_EditorClassIdentifier:\\s*\\r?\\n)"
            + "  levelDurationSeconds: 240\\s*\\r?\\n"
            + "[\\s\\S]*?"
            + "(?=(--- !u!))";

        var newText = System.Text.RegularExpressions.Regex.Replace(
            text,
            pattern,
            match => match.Groups[1].Value + newScriptRef + "\n  m_Name: \n  m_EditorClassIdentifier: \n",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        if (newText == text)
        {
            if (log)
                Debug.LogWarning($"[Dutz] End-house YAML fix {scenePath}: pattern did not match.");
            return;
        }

        System.IO.File.WriteAllText(scenePath, newText);
        if (log)
            Debug.Log($"[Dutz] End-house YAML fix {scenePath}: rebound end-house marker script.");
    }

    /// <summary>Batch: -executeMethod DutzSceneMissingScriptRepair.RepairLevel02Batch</summary>
    public static void RepairLevel02Batch() => RepairScene(DutzShowcaseSceneRepair.Level02ScenePath, log: true);

    /// <summary>Batch: -executeMethod DutzSceneMissingScriptRepair.RepairLevel02TrackCoinScriptsBatch</summary>
    public static void RepairLevel02TrackCoinScriptsBatch()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != DutzShowcaseSceneRepair.Level02ScenePath)
            scene = EditorSceneManager.OpenScene(DutzShowcaseSceneRepair.Level02ScenePath, OpenSceneMode.Single);

        var repaired = RepairTrackCoinScripts();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Dutz] Level 2 track coin script repair saved ({repaired} coin(s) updated). Transforms were not modified.");
    }

    /// <summary>Batch: -executeMethod DutzSceneMissingScriptRepair.RestoreLevel02TrackCoinRotationsBatch</summary>
    public static void RestoreLevel02TrackCoinRotationsBatch()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != DutzShowcaseSceneRepair.Level02ScenePath)
            scene = EditorSceneManager.OpenScene(DutzShowcaseSceneRepair.Level02ScenePath, OpenSceneMode.Single);

        var restored = RestoreTrackCoinRotationsFromEulerHints();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Dutz] Restored rotation on {restored} Level 2 coin(s) from saved euler hints. Positions were not changed.");
    }

    public static int RestoreTrackCoinRotationsFromEulerHints()
    {
        const string coinsRootName = "DutzGoldCoins";
        var root = GameObject.Find(coinsRootName);
        if (root == null)
            return 0;

        var restored = 0;
        foreach (Transform child in root.transform)
        {
            if (!DutzGoldCoin.IsTrackCoinRoot(child.gameObject))
                continue;

            if (TryRestoreCoinRotationFromEulerHints(child.gameObject))
                restored++;
        }

        return restored;
    }

    static bool TryRestoreCoinRotationFromEulerHints(GameObject go)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go))
            return false;

        var mods = PrefabUtility.GetPropertyModifications(go);
        if (mods == null || mods.Length == 0)
            return false;

        var hasHint = false;
        var euler = Vector3.zero;
        foreach (var mod in mods)
        {
            if (mod == null || string.IsNullOrEmpty(mod.propertyPath) || mod.value == null)
                continue;

            if (mod.propertyPath == "m_LocalEulerAnglesHint.x")
            {
                euler.x = float.Parse(mod.value.ToString());
                hasHint = true;
            }
            else if (mod.propertyPath == "m_LocalEulerAnglesHint.y")
            {
                euler.y = float.Parse(mod.value.ToString());
                hasHint = true;
            }
            else if (mod.propertyPath == "m_LocalEulerAnglesHint.z")
            {
                euler.z = float.Parse(mod.value.ToString());
                hasHint = true;
            }
        }

        if (!hasHint)
            return false;

        var desired = Quaternion.Euler(euler);
        var changedRotation = Quaternion.Angle(go.transform.localRotation, desired) >= 0.1f;
        if (changedRotation)
        {
            Undo.RecordObject(go.transform, "Restore Coin Rotation");
            go.transform.localRotation = desired;
        }

        var coin = go.GetComponent<DutzGoldCoin>();
        if (coin != null)
        {
            DutzGoldCoin.SuppressSpawnPoseApply = true;
            try
            {
                coin.CaptureSpawnPoseFromTransform();
                EditorUtility.SetDirty(coin);
            }
            finally
            {
                DutzGoldCoin.SuppressSpawnPoseApply = false;
            }
        }

        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        return changedRotation;
    }

    static int RepairTrackCoinScripts()
    {
        const string coinsRootName = "DutzGoldCoins";
        var root = GameObject.Find(coinsRootName);
        if (root == null)
            return 0;

        var repaired = 0;
        foreach (Transform child in root.transform)
        {
            if (!DutzGoldCoin.IsTrackCoinRoot(child.gameObject))
                continue;

            if (RepairSingleTrackCoin(child.gameObject))
                repaired++;
        }

        return repaired;
    }

    static bool RepairSingleTrackCoin(GameObject go)
    {
        var hadMissing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0;
        if (hadMissing)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        var existingCoins = go.GetComponents<DutzGoldCoin>();
        for (var i = existingCoins.Length - 1; i >= 1; i--)
            Object.DestroyImmediate(existingCoins[i]);

        foreach (var wrongObjective in go.GetComponents<DutzLevelObjective>())
            Object.DestroyImmediate(wrongObjective);

        DutzGoldCoin.SuppressSpawnPoseApply = true;
        try
        {
            var keep = go.GetComponent<DutzGoldCoin>();
            if (keep == null)
                keep = Undo.AddComponent<DutzGoldCoin>(go);

            RebindDutzGoldCoinScript(keep);
            keep.CaptureSpawnPoseFromTransform();
            EditorUtility.SetDirty(keep);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            return hadMissing || existingCoins.Length != 1;
        }
        finally
        {
            DutzGoldCoin.SuppressSpawnPoseApply = false;
        }
    }

    const string DutzGoldCoinScriptPath = "Assets/Characters/NPCs/Scripts/DutzGoldCoin.cs";
    const string DutzFlagPoleGoalScriptPath = "Assets/Characters/NPCs/Scripts/DutzFlagPoleGoal.cs";
    const string DutzForceFieldSuitPickupScriptPath = "Assets/Characters/NPCs/Scripts/DutzForceFieldSuitPickup.cs";
    const string DutzLevelObjectiveScriptPath = "Assets/Characters/NPCs/Scripts/DutzLevelObjective.cs";

    const string GoldCoinScriptGuid = "f8e4a2b1c3d54e6f9a0b1c2d3e4f5678";
    const string FlagPoleGoalScriptGuid = "b2c3d4e5f6a7890123456789abcdef01";
    const string ForceFieldSuitScriptGuid = "c3d4e5f6a7b8901234567890abcdef02";
    const string LevelObjectiveScriptGuid = "d61e396f03121594ea823fb2a9456fe8";

    static void RebindDutzGoldCoinScript(DutzGoldCoin coin)
    {
        if (coin == null)
            return;

        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(DutzGoldCoinScriptPath);
        if (script == null)
        {
            Debug.LogError("[Dutz] Could not load MonoScript at " + DutzGoldCoinScriptPath);
            return;
        }

        var serialized = new SerializedObject(coin);
        serialized.FindProperty("m_Script").objectReferenceValue = script;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static bool RepairOpenLevelScene(Scene scene, bool log = false)
    {
        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
            return false;

        if (scene.path != DutzLevel02Setup.Level00ScenePath
            && scene.path != DutzLevel02Setup.Level01ScenePath
            && scene.path != DutzLevel02Setup.Level02ScenePath
            && scene.path != DutzShowcaseSceneRepair.Level02ScenePath
            && scene.path != DutzLevel02Setup.Level03ScenePath)
        {
            return false;
        }

        var removedMissing = RemoveMissingScriptsRecursive(scene);
        EnsureKnownSceneComponents();
        var repairedCoins = RepairTrackCoinScripts();
        var repairedMeshes = RepairBridgeMeshMaterials(scene);
        var repositionedGoals = RepositionLevel02GoalsInOpenScene(scene);

        if (removedMissing == 0 && repairedCoins == 0 && repairedMeshes == 0 && !repositionedGoals)
            return false;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Open scene script repair saved for {scene.path}: " +
                $"removed {removedMissing} missing script slot(s), " +
                $"repaired {repairedCoins} coin script(s), " +
                $"trimmed materials on {repairedMeshes} bridge renderer(s), " +
                $"repositioned goals={repositionedGoals}.");
        }

        return true;
    }

    static bool RepositionLevel02GoalsInOpenScene(Scene scene)
    {
        if (scene.path != DutzShowcaseSceneRepair.Level02ScenePath)
            return false;

        Physics.SyncTransforms();
        DutzEndHouseCollider.EnsureFromBoot();
        var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
        if (pole != null)
            Undo.DestroyObjectImmediate(pole);
        DutzForceFieldSuitPickup.EnsureOnSceneSuit();
        DutzRobinCarMuralPlacer.RemoveFromLevel02IfPresent(scene, log: false);
        DutzShowcaseSceneRepair.EnsureEndHouseColliderOnScene(
            DutzShowcaseSceneRepair.Level02ScenePath, log: false);

        return true;
    }

    public static bool RepairScene(string scenePath, bool log)
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != scenePath)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var removedMissing = RemoveMissingScriptsRecursive(scene);
        EnsureKnownSceneComponents();
        var reboundEmbedded = RebindEmbeddedScriptReferences(scene);
        var repairedCoins = RepairTrackCoinScripts();
        var repairedMeshes = RepairBridgeMeshMaterials(scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (log)
        {
            Debug.Log(
                $"[Dutz] Scene script repair complete for {scenePath}: " +
                $"removed {removedMissing} missing script slot(s), " +
                $"rebound {reboundEmbedded} embedded script ref(s), " +
                $"repaired {repairedCoins} coin script(s), " +
                $"trimmed materials on {repairedMeshes} bridge renderer(s).");
        }

        return true;
    }

    static int RebindEmbeddedScriptReferences(Scene scene)
    {
        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(DutzLevelObjectiveScriptPath);
        if (script == null || string.IsNullOrEmpty(scene.path))
            return 0;

        var rebound = 0;
        foreach (var root in scene.GetRootGameObjects())
            rebound += RebindEmbeddedScriptReferencesRecursive(root.transform, script, scene.path);
        return rebound;
    }

    static int RebindEmbeddedScriptReferencesRecursive(Transform root, MonoScript levelObjectiveScript, string scenePath)
    {
        var rebound = 0;
        var go = root.gameObject;
        foreach (var component in go.GetComponents<Component>())
        {
            if (component == null || component is not MonoBehaviour behaviour)
                continue;

            var serialized = new SerializedObject(behaviour);
            var scriptProp = serialized.FindProperty("m_Script");
            if (scriptProp == null)
                continue;

            var current = scriptProp.objectReferenceValue;
            if (current == levelObjectiveScript)
                continue;

            var assetPath = current != null ? AssetDatabase.GetAssetPath(current) : string.Empty;
            if (assetPath != scenePath)
                continue;

            scriptProp.objectReferenceValue = levelObjectiveScript;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (go != null)
                EditorUtility.SetDirty(go);
            rebound++;
        }

        for (var i = 0; i < root.childCount; i++)
            rebound += RebindEmbeddedScriptReferencesRecursive(root.GetChild(i), levelObjectiveScript, scenePath);

        return rebound;
    }

    static int RemoveMissingScriptsRecursive(Scene scene)
    {
        var removed = 0;
        foreach (var root in scene.GetRootGameObjects())
            removed += RemoveMissingScriptsRecursive(root.transform);
        return removed;
    }

    static int RemoveMissingScriptsRecursive(Transform root)
    {
        var removed = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root.gameObject);
        if (removed > 0)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root.gameObject);

        for (var i = 0; i < root.childCount; i++)
            removed += RemoveMissingScriptsRecursive(root.GetChild(i));

        return removed;
    }

    static void EnsureKnownSceneComponents()
    {
        RepairMultiScriptComponent<DutzForceFieldSuitPickup>(DutzForceFieldSuitPickup.PickupObjectName);
        RepairEndHouseMarker();
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == DutzMobileRuntime.Level01SceneName
            || sceneName == DutzMobileRuntime.Level02SceneName)
        {
            var pole = GameObject.Find(DutzFlagPoleGoal.FlagPoleName);
            if (pole != null)
                Undo.DestroyObjectImmediate(pole);

            if (sceneName == DutzMobileRuntime.Level02SceneName)
                DutzRobinCarMuralPlacer.RemoveFromLevel02IfPresent(SceneManager.GetActiveScene(), log: false);
        }
        else
        {
            RepairMultiScriptComponent<DutzFlagPoleGoal>(DutzFlagPoleGoal.FlagPoleName);
        }

        DutzForceFieldSuitPickup.EnsureOnSceneSuit();
        if (UsesSuitcasesInScene())
            DutzSuitcaseCounter.EnsureSuitcasesAreReady();
        else
            DutzGoldCoinCounter.EnsureCoinsAreReady();

        RepairHighwayWallSloganBoard();
        EnsureLevel02GiantBossFaces();
    }

    static void EnsureLevel02GiantBossFaces()
    {
        if (SceneManager.GetActiveScene().name != DutzMobileRuntime.Level02SceneName)
            return;

        DutzGiantHippieBossFaceBuilder.EnsureCawetanBossFaceOnOpenScene(log: false);
    }

    static bool UsesSuitcasesInScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.name == DutzMobileRuntime.Level00SceneName
            || scene.name == DutzMobileRuntime.Level01SceneName
            || scene.name == DutzMobileRuntime.Level03SceneName;
    }

    const string HighwayWallSloganBoardScriptPath =
        "Assets/Characters/NPCs/Scripts/DutzHighwayWallSloganBoard.cs";

    static void RepairHighwayWallSloganBoard()
    {
        const string boardObjectName = "Highway Wall Slogans";
        var slogans = GameObject.Find(boardObjectName);
        if (slogans == null)
            return;

        var boardScript = AssetDatabase.LoadAssetAtPath<MonoScript>(HighwayWallSloganBoardScriptPath);
        if (boardScript == null)
        {
            Debug.LogError("[Dutz] Could not load MonoScript at " + HighwayWallSloganBoardScriptPath);
            return;
        }

        var settingsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
            "Assets/Characters/NPCs/Scripts/DutzHighwayWallSloganSettings.cs");

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(slogans);

        DutzHighwayWallSloganBoard board = null;
        foreach (var component in slogans.GetComponents<Component>())
        {
            if (component == null || component is Transform)
                continue;

            if (component is DutzHighwayWallSloganBoard existingBoard)
            {
                board = existingBoard;
                continue;
            }

            if (component is not MonoBehaviour behaviour)
            {
                Undo.DestroyObjectImmediate(component);
                continue;
            }

            var serialized = new SerializedObject(behaviour);
            var scriptProp = serialized.FindProperty("m_Script");
            var wrongScript = scriptProp != null &&
                              (scriptProp.objectReferenceValue == settingsScript ||
                               scriptProp.objectReferenceValue == null ||
                               (scriptProp.objectReferenceValue != null &&
                                scriptProp.objectReferenceValue.name == "DutzHighwayWallSloganSettings"));

            if (wrongScript)
            {
                scriptProp.objectReferenceValue = boardScript;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                board = slogans.GetComponent<DutzHighwayWallSloganBoard>();
                continue;
            }

            Undo.DestroyObjectImmediate(behaviour);
        }

        if (board == null)
            board = Undo.AddComponent<DutzHighwayWallSloganBoard>(slogans);

        var boardSerialized = new SerializedObject(board);
        var boardScriptProp = boardSerialized.FindProperty("m_Script");
        if (boardScriptProp != null && boardScriptProp.objectReferenceValue != boardScript)
        {
            boardScriptProp.objectReferenceValue = boardScript;
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    const string DutzEndHouseColliderScriptPath =
        "Assets/Characters/NPCs/Scripts/DutzEndHouseCollider.cs";

    static void RepairEndHouseMarker()
    {
        var house = FindSceneObjectByName(DutzEndHouseCollider.HouseName);
        if (house == null)
            return;

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(house);

        var endHouseScript = AssetDatabase.LoadAssetAtPath<MonoScript>(DutzEndHouseColliderScriptPath);
        if (endHouseScript == null)
        {
            Debug.LogError("[Dutz] Missing MonoScript at " + DutzEndHouseColliderScriptPath);
            return;
        }

        var marker = house.GetComponent<DutzEndHouseCollider>();
        if (marker == null)
        {
            var wrongObjective = house.GetComponent<DutzLevelObjective>();
            if (wrongObjective != null)
            {
                var serialized = new SerializedObject(wrongObjective);
                serialized.FindProperty("m_Script").objectReferenceValue = endHouseScript;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(wrongObjective);
                marker = house.GetComponent<DutzEndHouseCollider>();
            }
        }

        if (marker == null)
            marker = Undo.AddComponent<DutzEndHouseCollider>(house);

        marker.RefreshRoofZoneFromHierarchy();
        DutzEndHouseCollider.EnsureMeshCollider(house);
        EditorUtility.SetDirty(marker);
    }

    static void RepairMultiScriptComponent<T>(string objectName) where T : Component
    {
        var go = FindSceneObjectByName(objectName);
        if (go == null)
            return;

        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        if (typeof(T) == typeof(DutzForceFieldSuitPickup))
        {
            var wrongCoin = go.GetComponent<DutzGoldCoin>();
            if (wrongCoin != null)
                Object.DestroyImmediate(wrongCoin);
        }

        var multiClassScriptPath = typeof(T) switch
        {
            not null when typeof(T) == typeof(DutzGoldCoin) => DutzGoldCoinScriptPath,
            not null when typeof(T) == typeof(DutzFlagPoleGoal) => DutzFlagPoleGoalScriptPath,
            not null when typeof(T) == typeof(DutzForceFieldSuitPickup) => DutzForceFieldSuitPickupScriptPath,
            _ => DutzLevelObjectiveScriptPath
        };
        var multiClassScript = AssetDatabase.LoadAssetAtPath<MonoScript>(multiClassScriptPath);
        var keepers = go.GetComponents<T>();
        for (var i = keepers.Length - 1; i >= 1; i--)
            Object.DestroyImmediate(keepers[i]);

        foreach (var behaviour in go.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null || behaviour is T)
                continue;

            if (typeof(T) == typeof(DutzForceFieldSuitPickup))
            {
                Object.DestroyImmediate(behaviour);
                continue;
            }

            if (multiClassScript != null)
            {
                var behaviourScript = MonoScript.FromMonoBehaviour(behaviour);
                if (behaviourScript == null
                    || AssetDatabase.GetAssetPath(behaviourScript) != multiClassScriptPath)
                {
                    continue;
                }
            }

            Object.DestroyImmediate(behaviour);
        }

        if (go.GetComponent<T>() == null)
            Undo.AddComponent<T>(go);
        else if (typeof(T) == typeof(DutzGoldCoin))
            RebindDutzGoldCoinScript(go.GetComponent<DutzGoldCoin>());
    }

    static GameObject FindSceneObjectByName(string objectName)
    {
        var found = GameObject.Find(objectName);
        if (found != null)
            return found;

        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child.gameObject;
            }
        }

        return null;
    }

    static int RepairBridgeMeshMaterials(Scene scene)
    {
        var repaired = 0;
        foreach (var root in scene.GetRootGameObjects())
            repaired += RepairBridgeMeshMaterialsRecursive(root.transform);
        return repaired;
    }

    static int RepairBridgeMeshMaterialsRecursive(Transform root)
    {
        var repaired = 0;
        var renderer = root.GetComponent<MeshRenderer>();
        if (renderer != null && DutzMeshMaterialRepair.RepairRenderer(renderer))
            repaired++;

        for (var i = 0; i < root.childCount; i++)
            repaired += RepairBridgeMeshMaterialsRecursive(root.GetChild(i));

        return repaired;
    }
}
