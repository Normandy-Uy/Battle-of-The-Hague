using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tiny scene to verify the APK runs on Android before building the full showcase level.
/// </summary>
public static class DutzMobileSmokeSceneSetup
{
    const string SmokeScenePath = "Assets/Scenes/Dutz_MobileSmoke.unity";
    const string PingScenePath = "Assets/Scenes/Dutz_AndroidPing.unity";
    const string LoaderScenePath = "Assets/Scenes/Dutz_MobileLoader.unity";
    const string DutzPrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";

    public static void CreateSmokeScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(12f, 1f, 12f);

        var dutz = InstantiateDutz();
        PlaceDutzOnGround(dutz);
        SetupCamera(dutz != null ? dutz.transform : null);

        var hud = new GameObject("MobileSmokeHud");
        AddRuntimeScript(hud, "DutzMobileSmokeHud");

        Directory.CreateDirectory(Path.GetDirectoryName(SmokeScenePath)!);
        EditorSceneManager.SaveScene(scene, SmokeScenePath);
        Debug.Log($"[Dutz] Saved mobile smoke scene: {SmokeScenePath}");
    }

    public static void CreatePingScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "PingCube";
        cube.transform.position = new Vector3(0f, 0.5f, 2f);

        var cam = Camera.main;
        if (cam != null)
            cam.transform.position = new Vector3(0f, 1.2f, -3f);

        var ping = new GameObject("AndroidPing");
        AddRuntimeScript(ping, "DutzAndroidPing");

        Directory.CreateDirectory(Path.GetDirectoryName(PingScenePath)!);
        EditorSceneManager.SaveScene(scene, PingScenePath);
        Debug.Log($"[Dutz] Saved Android ping scene: {PingScenePath}");
    }

    public static void UsePingSceneInBuild()
    {
        if (!File.Exists(PingScenePath))
            CreatePingScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(PingScenePath, true)
        };

        Debug.Log("[Dutz] Build Settings now use Dutz_AndroidPing only. Build APK — you should see ALIVE on screen.");
    }

    public static void UseSmokeSceneInBuild()
    {
        if (!File.Exists(SmokeScenePath))
            CreateSmokeScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SmokeScenePath, true)
        };

        Debug.Log(
            "[Dutz] BUILD SCENE = Dutz_MobileSmoke (NOT ping).\n" +
            "Rebuild APK now. Phone must show SMOKE TEST banner + Dutz — NOT ALIVE.");
    }

    public static void UseTrainerScenesInBuild()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(DutzLevel02Setup.Level00ScenePath))
            scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level00ScenePath, true));

        if (File.Exists(DutzLevel02Setup.Level01ScenePath))
            scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level01ScenePath, true));

        if (File.Exists(DutzLevel02Setup.Level02ScenePath))
            scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level02ScenePath, true));

        if (scenes.Count == 0)
        {
            Debug.LogError("[Dutz] Trainer Build Settings — no Level 00/01/02 scenes found.");
            return;
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[Dutz] Build Settings (Trainer): "
            + string.Join(" → ", scenes.Select(s => Path.GetFileNameWithoutExtension(s.path))));
    }

    public static void UseCampaignScenesInBuild()
    {
        DutzLevel02Setup.RegisterInBuildSettings();
        Debug.Log("[Dutz] Build Settings (Campaign): Dutz_Level00 → L01 → L02 → L03 (when present).");
    }

    /// <summary>Sideload / test APK with Dutz_Level07 as the only build scene.</summary>
    public static void UseLevel07OnlyScenesInBuild()
    {
        if (!File.Exists(DutzLevel02Setup.Level07ScenePath))
        {
            Debug.LogError("[Dutz] Level07 Build Settings — missing " + DutzLevel02Setup.Level07ScenePath);
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(DutzLevel02Setup.Level07ScenePath, true)
        };

        Debug.Log("[Dutz] Build Settings (Level07 only): Dutz_Level07");
    }

    public static void UseShowcaseSceneInBuild(bool prependMobileLoader = false)
    {
        if (prependMobileLoader)
            RegisterShowcaseWithMobileLoader();
        else
            UseCampaignScenesInBuild();

        Debug.Log(prependMobileLoader
            ? "[Dutz] Build Settings: Dutz_MobileLoader → L00 → L01 → L02 → L03 (Android showcase)."
            : "[Dutz] Build Settings: Dutz_Level00, Dutz_Level01, Dutz_Level02, Dutz_Level03 (when present).");
    }

    public static void CreateLoaderScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var loader = new GameObject("MobileLevelLoader");
        AddRuntimeScript(loader, nameof(DutzMobileLevelLoader));

        var camGo = new GameObject("LoaderCamera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
        cam.depth = -10;

        Directory.CreateDirectory(Path.GetDirectoryName(LoaderScenePath)!);
        EditorSceneManager.SaveScene(scene, LoaderScenePath);
        Debug.Log($"[Dutz] Saved mobile loader scene: {LoaderScenePath}");
    }

    public static void RegisterShowcaseWithMobileLoader()
    {
        if (!File.Exists(LoaderScenePath))
            CreateLoaderScene();

        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(LoaderScenePath, true),
        };

        if (File.Exists(DutzLevel02Setup.Level00ScenePath))
            scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level00ScenePath, true));

        scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level01ScenePath, true));
        scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level02ScenePath, true));

        if (File.Exists(DutzLevel02Setup.Level03ScenePath))
            scenes.Add(new EditorBuildSettingsScene(DutzLevel02Setup.Level03ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static GameObject InstantiateDutz()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DutzPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[Dutz] Missing prefab: {DutzPrefabPath}");
            return null;
        }

        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    static void PlaceDutzOnGround(GameObject dutz)
    {
        if (dutz == null)
            return;

        var cc = dutz.GetComponent<CharacterController>();
        if (cc != null)
            cc.enabled = false;

        dutz.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        DutzNpcFeet.PlacePivotOnSurface(dutz, 0f);

        var player = dutz.GetComponent<DutzPlayerController>();
        if (player != null)
        {
            var so = new SerializedObject(player);
            so.FindProperty("spawnPosition").vector3Value = dutz.transform.position;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (cc != null)
            cc.enabled = true;
    }

    static void AddRuntimeScript(GameObject go, string typeName)
    {
        var type = System.AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .FirstOrDefault(candidate => candidate.Name == typeName);

        if (type == null || !typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogError($"[Dutz] Missing runtime script: {typeName}");
            return;
        }

        go.AddComponent(type);
    }

    static void SetupCamera(Transform target)
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<DutzCameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<DutzCameraFollow>();

        follow.ApplyRobloxDefaults();
        if (target != null)
            follow.BindTarget(target);
    }
}
