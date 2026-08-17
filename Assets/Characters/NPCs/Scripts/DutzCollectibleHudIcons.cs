using UnityEngine;

/// <summary>HUD collectible icons — suitcase uses a flat sprite; coins still render from prefab.</summary>
public static class DutzCollectibleHudIcons
{
    const int IconLayer = 31;
    const int IconRenderSize = 256;

    const string GoldCoinResourcePath = "CollectibleHud/GoldCoin";
    const string SuitcaseIconResourcePath = "CollectibleHud/SuitcaseIcon";

#if UNITY_EDITOR
    const string GoldCoinPrefabPath =
        "Assets/LiquidFire Package 4 - BSH games/Devtoid - Gold Coins/3D Assets/Gold Coin - Single/Prefab/GoldCoin.prefab";
#endif

    static Texture2D coinIcon;
    static Texture2D suitcaseIcon;
    static bool coinIconAttempted;
    static bool suitcaseIconAttempted;

    public static Texture2D CoinIcon
    {
        get
        {
            if (!coinIconAttempted)
            {
                coinIconAttempted = true;
                coinIcon = BuildIcon(
                    LoadGoldCoinPrefab(),
                    FindSceneTemplate<DutzGoldCoin>());
            }

            return coinIcon;
        }
    }

    public static Texture2D SuitcaseIcon
    {
        get
        {
            if (!suitcaseIconAttempted)
            {
                suitcaseIconAttempted = true;
                suitcaseIcon = Resources.Load<Texture2D>(SuitcaseIconResourcePath);
                if (suitcaseIcon != null)
                {
                    suitcaseIcon.filterMode = FilterMode.Bilinear;
                    suitcaseIcon.wrapMode = TextureWrapMode.Clamp;
                }
            }

            return suitcaseIcon;
        }
    }

    static Texture2D BuildIcon(GameObject prefabTemplate, GameObject sceneTemplate) =>
        RenderCollectibleIcon(prefabTemplate) ?? RenderCollectibleIcon(sceneTemplate);

    static GameObject LoadGoldCoinPrefab()
    {
        var prefab = Resources.Load<GameObject>(GoldCoinResourcePath);
#if UNITY_EDITOR
        if (prefab == null)
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(GoldCoinPrefabPath);
#endif
        return prefab;
    }

    static GameObject FindSceneTemplate<T>() where T : MonoBehaviour
    {
        foreach (var item in Object.FindObjectsOfType<T>(true))
        {
            if (item != null)
                return item.gameObject;
        }

        return null;
    }

    static Texture2D RenderCollectibleIcon(GameObject template)
    {
        if (template == null)
            return null;

        GameObject root = null;
        GameObject camGo = null;
        GameObject lightGo = null;
        RenderTexture rt = null;

        try
        {
            root = Object.Instantiate(template);
            root.hideFlags = HideFlags.HideAndDontSave;
            root.transform.position = new Vector3(0f, -10000f, 0f);
            root.transform.rotation = Quaternion.identity;

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                    behaviour.enabled = false;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    Object.Destroy(collider);
            }

            SetLayerRecursive(root, IconLayer);
            root.transform.rotation = Quaternion.Euler(20f, 35f, 0f) * root.transform.rotation;

            var bounds = CalculateRendererBounds(root);
            if (bounds.size.sqrMagnitude <= 0.0001f)
                return null;

            camGo = new GameObject("DutzCollectibleHudIconCamera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = 1 << IconLayer;
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 1.15f;
            camGo.transform.position = bounds.center + Vector3.back * 2f;
            camGo.transform.LookAt(bounds.center);

            lightGo = new GameObject("DutzCollectibleHudIconLight");
            lightGo.hideFlags = HideFlags.HideAndDontSave;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(35f, -45f, 0f);

            rt = RenderTexture.GetTemporary(IconRenderSize, IconRenderSize, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var tex = new Texture2D(IconRenderSize, IconRenderSize, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0f, 0f, IconRenderSize, IconRenderSize), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            return tex;
        }
        finally
        {
            if (rt != null)
                RenderTexture.ReleaseTemporary(rt);

            if (root != null)
                Object.Destroy(root);

            if (camGo != null)
                Object.Destroy(camGo);

            if (lightGo != null)
                Object.Destroy(lightGo);
        }
    }

    static Bounds CalculateRendererBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
