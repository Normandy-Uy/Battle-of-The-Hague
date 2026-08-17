using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Shows an editable bump quote when the player touches a highway mural.
/// </summary>
[DisallowMultipleComponent]
public class DutzMuralBumpMessage : MonoBehaviour
{
    const string Level00ScenePath = "Assets/Scenes/Dutz_Level00.unity";
    const string JailMuralRootName = "DutzJailMural";
    const string DisplayHostName = "DutzMuralBumpMessageDisplay";
    const float DefaultMessageDuration = 3f;
    const float DefaultBumpCooldown = 2f;
    const float TriggerDepthMeters = 1.5f;

    [SerializeField] string bumpMessage = string.Empty;
    [SerializeField] float messageDuration = DefaultMessageDuration;
    [SerializeField] float bumpCooldown = DefaultBumpCooldown;

    float cooldownUntil;
    int lastTriggerFrame = -1;

    public string BumpMessage => bumpMessage;

    public static void EnsureFromBoot()
    {
        if (!HasBumpMuralsInScene())
            return;

        DutzMuralBumpMessageDisplay.EnsureHost();

        StripMuralRootBumpComponents();

        if (DutzCollectibleProgress.IsLevel00)
            EnsureLevel00MuralsInScene(log: false);
        else
            EnsureTriggerCollidersForAllBumpMessages(log: false);
    }

    static bool HasBumpMuralsInScene()
    {
        if (DutzCollectibleProgress.IsLevel00)
            return true;

        return Object.FindObjectOfType<DutzMuralBumpMessage>(true) != null;
    }

    public static bool EnsureLevel00MuralsInScene(bool log)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return false;

        if (scene.name != DutzMobileRuntime.Level00SceneName && scene.path != Level00ScenePath)
            return false;

        var changed = StripMuralRootBumpComponents();

        foreach (var mural in CollectBumpMurals())
        {
#if UNITY_EDITOR
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(mural) > 0
                && GameObjectUtility.RemoveMonoBehavioursWithMissingScript(mural) > 0)
            {
                changed = true;
            }
#endif

            if (Apply(mural, GetDefaultMessage(mural.name)))
                changed = true;

            if (EnsureTriggerCollider(mural))
                changed = true;
        }

        if (log && changed)
            Debug.Log("[Dutz] Ensured bump messages on Level 00 mural(s).");

        return changed;
    }

    public static bool EnsureTriggerCollidersForAllBumpMessages(bool log)
    {
        var changed = false;
        foreach (var bump in Object.FindObjectsOfType<DutzMuralBumpMessage>(true))
        {
            if (bump == null)
                continue;

            if (EnsureTriggerCollider(bump.gameObject))
                changed = true;
        }

        if (log && changed)
            Debug.Log("[Dutz] Ensured trigger colliders on mural bump message(s).");

        return changed;
    }

    public static bool Apply(GameObject mural, string defaultMessage = null)
    {
        if (mural == null)
            return false;

        var changed = false;
        var bump = mural.GetComponent<DutzMuralBumpMessage>();
        if (bump == null)
        {
            bump = mural.AddComponent<DutzMuralBumpMessage>();
            bump.InitializeForAuthoring(defaultMessage);
            changed = true;
        }

        return changed;
    }

    public void InitializeForAuthoring(string message)
    {
        if (string.IsNullOrEmpty(bumpMessage) && !string.IsNullOrEmpty(message))
            bumpMessage = message;
    }

    static string GetDefaultMessage(string objectName)
    {
        if (objectName != null && objectName.StartsWith("DuterTengotMural_"))
            return "MY GOD, I HATE DRUGS.";

        return string.Empty;
    }

    static bool IsAutoBumpMuralName(string name) =>
        !string.IsNullOrEmpty(name)
        && (name.StartsWith("TimelineMural_")
            || name.StartsWith("DuterHagueMural_")
            || name.StartsWith("DuterTengotMural_")
            || name.StartsWith("DutzJailMural_"));

    /// <summary>Root group objects have no mesh — bump belongs on the panel child only.</summary>
    static bool StripMuralRootBumpComponents()
    {
        var changed = false;
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null || transform.name != JailMuralRootName)
                continue;

            if (transform.GetComponent<Renderer>() != null)
                continue;

            if (RemoveBumpTriggerComponents(transform.gameObject))
                changed = true;
        }

        return changed;
    }

    static bool RemoveBumpTriggerComponents(GameObject go)
    {
        if (go == null)
            return false;

        var changed = false;

        var bump = go.GetComponent<DutzMuralBumpMessage>();
        if (bump != null)
        {
            if (Application.isPlaying)
                Object.Destroy(bump);
            else
                Object.DestroyImmediate(bump);
            changed = true;
        }

        var box = go.GetComponent<BoxCollider>();
        if (box != null && box.isTrigger)
        {
            if (Application.isPlaying)
                Object.Destroy(box);
            else
                Object.DestroyImmediate(box);
            changed = true;
        }

        return changed;
    }

    static GameObject[] CollectBumpMurals()
    {
        var murals = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var transform in Object.FindObjectsOfType<Transform>(true))
        {
            if (transform == null)
                continue;

            if (IsAutoBumpMuralName(transform.name))
                murals.Add(transform.gameObject);
        }

        foreach (var bump in Object.FindObjectsOfType<DutzMuralBumpMessage>(true))
        {
            if (bump != null)
                murals.Add(bump.gameObject);
        }

        var list = new System.Collections.Generic.List<GameObject>(murals);
        return list.ToArray();
    }

    static bool EnsureTriggerCollider(GameObject mural)
    {
        if (mural == null)
            return false;

        var changed = false;

        var meshCollider = mural.GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            if (Application.isPlaying)
                Object.Destroy(meshCollider);
            else
                Object.DestroyImmediate(meshCollider);
            changed = true;
        }

        var box = mural.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = mural.AddComponent<BoxCollider>();
            changed = true;
        }

        if (box != null && !box.isTrigger)
        {
            box.isTrigger = true;
            changed = true;
        }

        var beforeCenter = box.center;
        var beforeSize = box.size;
        FitTriggerToRenderer(mural, box);
        if ((beforeCenter - box.center).sqrMagnitude > 0.0001f
            || (beforeSize - box.size).sqrMagnitude > 0.0001f)
            changed = true;

        return changed;
    }

    static void FitTriggerToRenderer(GameObject mural, BoxCollider box)
    {
        // Cut-out murals keep the mesh on a child Panel — root has no Renderer.
        var renderers = mural.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        var hasBounds = false;
        var min = Vector3.zero;
        var max = Vector3.zero;
        foreach (var renderer in renderers)
        {
            if (renderer == null)
                continue;

            var bounds = renderer.bounds;
            var center = bounds.center;
            var extents = bounds.extents;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var world = center + Vector3.Scale(extents, new Vector3(x, y, z));
                var local = mural.transform.InverseTransformPoint(world);
                if (!hasBounds)
                {
                    min = max = local;
                    hasBounds = true;
                }
                else
                {
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }
        }

        if (!hasBounds)
            return;

        var localCenter = (min + max) * 0.5f;
        var localSize = max - min;
        localSize.x = Mathf.Abs(localSize.x);
        localSize.y = Mathf.Abs(localSize.y);
        localSize.z = Mathf.Abs(localSize.z);

        var minAxis = 0;
        var minSize = localSize.x;
        if (localSize.y < minSize)
        {
            minAxis = 1;
            minSize = localSize.y;
        }

        if (localSize.z < minSize)
            minAxis = 2;

        if (minAxis == 0)
            localSize.x = Mathf.Max(localSize.x, TriggerDepthMeters);
        else if (minAxis == 1)
            localSize.y = Mathf.Max(localSize.y, TriggerDepthMeters);
        else
            localSize.z = Mathf.Max(localSize.z, TriggerDepthMeters);

        box.center = localCenter;
        box.size = localSize;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time < cooldownUntil || string.IsNullOrWhiteSpace(bumpMessage))
            return;

        if (!IsPlayerCollider(other))
            return;

        if (lastTriggerFrame == Time.frameCount)
            return;

        lastTriggerFrame = Time.frameCount;
        cooldownUntil = Time.time + bumpCooldown;
        DutzMuralBumpMessageDisplay.EnsureHost();
        DutzMuralBumpMessageDisplay.Show(bumpMessage, messageDuration);
    }

    void OnTriggerStay(Collider other) => OnTriggerEnter(other);

    static bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.GetComponentInParent<DutzPlayerController>() != null)
            return true;

        return other.CompareTag("Player");
    }
}

/// <summary>Single shared HUD draw for mural bump quotes.</summary>
static class DutzMuralBumpMessageDisplay
{
    const string DisplayHostName = "DutzMuralBumpMessageDisplay";

    static string activeMessage;
    static float messageTimeLeft;

    public static void EnsureHost()
    {
        if (Object.FindObjectOfType<DutzMuralBumpMessageDisplayHost>() != null)
            return;

        var host = new GameObject(DisplayHostName);
        host.AddComponent<DutzMuralBumpMessageDisplayHost>();
    }

    public static void Show(string message, float duration)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        activeMessage = CollapseExtraSpaces(message.Trim());
        messageTimeLeft = duration;
    }

    static string CollapseExtraSpaces(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        while (text.Contains("  "))
            text = text.Replace("  ", " ");

        return text;
    }

    public static void Tick()
    {
        if (messageTimeLeft > 0f)
            messageTimeLeft -= Time.deltaTime;
    }

    public static void Draw()
    {
        if (messageTimeLeft <= 0f || string.IsNullOrEmpty(activeMessage))
            return;

        if (DutzLevelObjective.IsStartMessageActive)
            return;

        DutzCartoonDialogGui.DrawMuralBumpBanner(
            activeMessage,
            DutzAnnouncementHud.DefaultFlashColor,
            DutzAnnouncementHud.StartMessageLine);
    }
}

[DisallowMultipleComponent]
public class DutzMuralBumpMessageDisplayHost : MonoBehaviour
{
    void Update() => DutzMuralBumpMessageDisplay.Tick();

    void OnGUI() => DutzMuralBumpMessageDisplay.Draw();
}
