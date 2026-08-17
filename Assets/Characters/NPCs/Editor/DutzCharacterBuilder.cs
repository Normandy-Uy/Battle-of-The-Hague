using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sets up the player from SimpleCitizens_Emo_White (scene instance or prefab).
/// Batch: -executeMethod DutzCharacterBuilder.CreateDutz
/// </summary>
public static class DutzCharacterBuilder
{
    const string MaterialsFolder = "Assets/Characters/NPCs/Materials";
    const string PrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";
    const string ScenePath = "Assets/Scenes/Dutz_Level02.unity";
    const string SimpleCitizensPrefabPath = "Assets/SimpleCitizens/Prefabs/SimpleCitizens_Emo_White.prefab";

    // Classic Roblox default body colors (BrickColor-style)
    static readonly Color BrickYellow = new Color(0.961f, 0.804f, 0.588f);
    static readonly Color BrightBlue = new Color(0.051f, 0.412f, 0.675f);
    static readonly Color MediumGreen = new Color(0.294f, 0.592f, 0.294f);
    static readonly Color HairBlack = new Color(0.106f, 0.106f, 0.106f);
    static readonly Color FaceWhite = new Color(0.95f, 0.95f, 0.95f);

    /// <summary>Called from Unity batch mode: -executeMethod DutzCharacterBuilder.CreateDutz</summary>
    public static void CreateDutz()
    {
        EnsureFolders();

        if (!CreateDutzFromSimpleCitizens())
            Debug.LogError(
                "[Dutz] Could not create player. Add SimpleCitizens_Emo_White to the scene or install the SimpleCitizens prefab.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>Uses scene object SimpleCitizens_Emo_White, or the SimpleCitizens prefab, as the playable Dutz.</summary>
    public static bool CreateDutzFromSimpleCitizens()
    {
        GameObject root = GameObject.Find(DutzSimpleCitizensSetup.DefaultSourceName);
        var createdTemporary = false;

        if (root == null)
        {
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimpleCitizensPrefabPath);
            if (sourcePrefab == null)
                return false;

            root = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            createdTemporary = true;
        }

        var oldPlayer = DutzEditorHelpers.FindPrimaryDutzPlayer();
        var spawnPos = new Vector3(250f, 8f, -2.3f);
        var spawnRot = Quaternion.LookRotation(Vector3.right);

        if (oldPlayer != null && oldPlayer.gameObject != root)
        {
            spawnPos = oldPlayer.transform.position;
            spawnRot = oldPlayer.transform.rotation;
            Object.DestroyImmediate(oldPlayer.gameObject);
        }

        DutzSimpleCitizensSetup.ApplyPlayerComponents(root, "Emo");
        DutzSoundSetup.ApplyToGameObject(root);
        root.name = DutzPlayerController.PlayerObjectName;
        root.transform.SetPositionAndRotation(spawnPos, spawnRot);

        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        if (createdTemporary)
        {
            Object.DestroyImmediate(root);
            if (File.Exists(ScenePath))
                ReplaceDutzInShowcaseScene(savedPrefab);
            else
                CreateShowcaseScene(savedPrefab);
        }
        else if (File.Exists(ScenePath))
        {
            if (!EditorSceneManager.GetActiveScene().path.Equals(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        Debug.Log("[Dutz] Player is SimpleCitizens_Emo_White (saved to Dutz.prefab).");
        return true;
    }

    static GameObject BuildMopheadAvatar(Material yellow, Material blue, Material green, Material hair,
        Material white, Material black)
    {
        var root = new GameObject(DutzPlayerController.PlayerObjectName);
        root.transform.position = Vector3.zero;

        var npc = root.AddComponent<DutzNPC>();
        root.AddComponent<DutzIdleBob>();

        var controller = root.AddComponent<CharacterController>();
        controller.height = 2.2f;
        controller.radius = 0.38f;
        controller.center = new Vector3(0f, 1.1f, 0f);

        root.AddComponent<DutzPlayerController>();
        root.AddComponent<DutzWalkAnimation>();
        root.AddComponent<DutzFallRespawn>();
        DutzSoundSetup.ApplyToGameObject(root);

        // R6 torso (blue shirt)
        MakePrimitive(PrimitiveType.Cube, "Body", root.transform, blue,
            new Vector3(0f, 1.05f, 0f), new Vector3(1f, 1f, 0.5f), Vector3.zero);

        // R6 head (yellow)
        var head = MakePrimitive(PrimitiveType.Cube, "Head", root.transform, yellow,
            new Vector3(0f, 1.75f, 0f), new Vector3(1.15f, 1.15f, 1.15f), Vector3.zero);

        // Classic Roblox face (simple decals as cubes)
        MakePrimitive(PrimitiveType.Cube, "Eye_L", head.transform, white,
            new Vector3(-0.22f, 0.05f, 0.52f), new Vector3(0.18f, 0.18f, 0.06f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Eye_R", head.transform, white,
            new Vector3(0.22f, 0.05f, 0.52f), new Vector3(0.18f, 0.18f, 0.06f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Pupil_L", head.transform, black,
            new Vector3(-0.22f, 0.02f, 0.56f), new Vector3(0.08f, 0.1f, 0.04f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Pupil_R", head.transform, black,
            new Vector3(0.22f, 0.02f, 0.56f), new Vector3(0.08f, 0.1f, 0.04f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Mouth", head.transform, black,
            new Vector3(0f, -0.28f, 0.52f), new Vector3(0.42f, 0.06f, 0.05f), Vector3.zero);

        // Mophead hair — shaggy black blocks covering forehead and sides
        MakePrimitive(PrimitiveType.Cube, "Hair_Top", head.transform, hair,
            new Vector3(0f, 0.42f, 0.02f), new Vector3(1.22f, 0.55f, 1.22f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Hair_Front", head.transform, hair,
            new Vector3(0f, 0.12f, 0.38f), new Vector3(1.18f, 0.7f, 0.45f), new Vector3(8f, 0f, 0f));
        MakePrimitive(PrimitiveType.Cube, "Hair_Fringe_L", head.transform, hair,
            new Vector3(-0.48f, 0f, 0.28f), new Vector3(0.35f, 0.85f, 0.5f), new Vector3(0f, 0f, 18f));
        MakePrimitive(PrimitiveType.Cube, "Hair_Fringe_R", head.transform, hair,
            new Vector3(0.48f, 0f, 0.28f), new Vector3(0.35f, 0.85f, 0.5f), new Vector3(0f, 0f, -18f));
        MakePrimitive(PrimitiveType.Cube, "Hair_Side_L", head.transform, hair,
            new Vector3(-0.58f, 0.08f, 0f), new Vector3(0.22f, 0.75f, 1.05f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Hair_Side_R", head.transform, hair,
            new Vector3(0.58f, 0.08f, 0f), new Vector3(0.22f, 0.75f, 1.05f), Vector3.zero);

        // R6 arms (yellow)
        var armL = MakePrimitive(PrimitiveType.Cube, "Arm_L", root.transform, yellow,
            new Vector3(-0.72f, 1.05f, 0f), new Vector3(0.38f, 1.05f, 0.38f), new Vector3(0f, 0f, 12f));
        var armR = MakePrimitive(PrimitiveType.Cube, "Arm_R", root.transform, yellow,
            new Vector3(0.72f, 1.05f, 0f), new Vector3(0.38f, 1.05f, 0.38f), new Vector3(0f, 0f, -12f));
        MakePrimitive(PrimitiveType.Cube, "Hand_L", armL.transform, yellow,
            new Vector3(0f, -0.62f, 0f), new Vector3(0.4f, 0.35f, 0.4f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Hand_R", armR.transform, yellow,
            new Vector3(0f, -0.62f, 0f), new Vector3(0.4f, 0.35f, 0.4f), Vector3.zero);

        // R6 legs (green pants) + block feet
        MakePrimitive(PrimitiveType.Cube, "Leg_L", root.transform, green,
            new Vector3(-0.26f, 0.42f, 0f), new Vector3(0.42f, 0.85f, 0.42f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Leg_R", root.transform, green,
            new Vector3(0.26f, 0.42f, 0f), new Vector3(0.42f, 0.85f, 0.42f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Foot_L", root.transform, green,
            new Vector3(-0.26f, 0.02f, 0.08f), new Vector3(0.44f, 0.18f, 0.55f), Vector3.zero);
        MakePrimitive(PrimitiveType.Cube, "Foot_R", root.transform, green,
            new Vector3(0.26f, 0.02f, 0.08f), new Vector3(0.44f, 0.18f, 0.55f), Vector3.zero);

        npc.BindReferences(head.transform, null, "Mophead");
        return root;
    }

    static void ReplaceDutzInShowcaseScene(GameObject prefab)
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var pos = new Vector3(250f, 8f, -2.3f);
        var rot = Quaternion.LookRotation(Vector3.right);

        foreach (var existing in Object.FindObjectsOfType<DutzPlayerController>())
        {
            if (existing == null)
                continue;

            pos = existing.transform.position;
            rot = existing.transform.rotation;
            Object.DestroyImmediate(existing.gameObject);
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(pos, rot);

        var cam = Camera.main;
        if (cam != null)
        {
            var rts = cam.GetComponent<DutzRtsCamera>();
            if (rts != null)
                rts.enabled = false;

            if (cam.GetComponent<DutzCameraFollow>() == null)
                cam.gameObject.AddComponent<DutzCameraFollow>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Dutz] Replaced scene instance with Roblox mophead avatar.");
    }

    static void EnsureFolders()
    {
        Directory.CreateDirectory(Path.GetFullPath("Assets/Characters/NPCs/Prefabs"));
        Directory.CreateDirectory(Path.GetFullPath("Assets/Characters/NPCs/Materials"));
        Directory.CreateDirectory(Path.GetFullPath("Assets/Scenes"));
    }

    static Material EnsureColorMat(string name, Color color)
    {
        var path = $"{MaterialsFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    static GameObject MakePrimitive(PrimitiveType type, string objName, Transform parent, Material mat,
        Vector3 localPos, Vector3 localScale, Vector3 localEuler)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localEulerAngles = localEuler;

        var col = go.GetComponent<Collider>();
        if (col != null)
            Object.DestroyImmediate(col);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null && mat != null)
            renderer.sharedMaterial = mat;

        return go;
    }

    static void CreateShowcaseScene(GameObject dutzPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(26f, 1f, 2.5f);
        ground.transform.position = new Vector3(130f, 0f, 0f);
        var groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.35f, 0.55f, 0.32f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        var dutz = (GameObject)PrefabUtility.InstantiatePrefab(dutzPrefab);
        dutz.transform.rotation = Quaternion.LookRotation(Vector3.right);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.farClipPlane = 500f;
            var rts = cam.GetComponent<DutzRtsCamera>();
            if (rts != null)
                rts.enabled = false;

            if (cam.GetComponent<DutzCameraFollow>() == null)
                cam.gameObject.AddComponent<DutzCameraFollow>();
        }

        var light = Object.FindObjectOfType<Light>();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.intensity = 1.1f;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);

        var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorBuildSettings.scenes = scenes;
    }
}
