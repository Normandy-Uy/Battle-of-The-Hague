using UnityEngine;

/// <summary>
/// Applies player gameplay components to a SimpleCitizens character mesh.
/// </summary>
public static class DutzSimpleCitizensSetup
{
    public const string DefaultSourceName = "SimpleCitizens_Emo_White";
    public const string ActiveOutfitName = "SC_Emo";
    public const float DefaultAvatarScale = 2f;

    const float BaseCharacterHeight = 1.85f;
    const float BaseCharacterRadius = 0.32f;
    const float BaseCharacterCenterY = 0.92f;
    const float BaseStepOffset = 0.3f;

    public static void EnableOutfitOnly(GameObject root, string outfitName = ActiveOutfitName)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.gameObject.name.StartsWith("SC_"))
                continue;

            renderer.gameObject.SetActive(renderer.gameObject.name == outfitName);
        }
    }

    public static Transform FindHeadBone(GameObject root)
    {
        var head = root.transform.Find("Head_jnt");
        if (head != null)
            return head;

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Head_jnt")
                return t;
        }

        return root.transform;
    }

    public static void ApplyAvatarScale(GameObject root, float scale = DefaultAvatarScale)
    {
        if (root == null || scale <= 0f)
            return;

        root.transform.localScale = Vector3.one * scale;

        var cc = root.GetComponent<CharacterController>();
        if (cc == null)
            return;

        cc.height = BaseCharacterHeight * scale;
        cc.radius = BaseCharacterRadius * scale;
        cc.center = new Vector3(0f, BaseCharacterCenterY * scale, 0f);
        cc.stepOffset = Mathf.Min(BaseStepOffset * scale, cc.height * 0.45f);
    }

    static void EnsureMovementAudio(GameObject root)
    {
        if (root.GetComponent<AudioSource>() == null)
            root.AddComponent<AudioSource>();

        var source = root.GetComponent<AudioSource>();
        if (source != null)
            source.playOnAwake = false;

        if (root.GetComponent<DutzMovementSounds>() == null)
            root.AddComponent<DutzMovementSounds>();
    }

    public static void ApplyPlayerComponents(GameObject root, string displayName = "Emo", float avatarScale = DefaultAvatarScale)
    {
        EnableOutfitOnly(root);

        foreach (var box in root.GetComponents<BoxCollider>())
            Object.Destroy(box);

        var animator = root.GetComponent<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;

        var cc = root.GetComponent<CharacterController>();
        if (cc == null)
            cc = root.AddComponent<CharacterController>();

        cc.slopeLimit = 45f;
        ApplyAvatarScale(root, avatarScale);

        var walk = root.GetComponent<DutzWalkAnimation>();
        if (walk != null)
        {
            if (Application.isPlaying)
                Object.Destroy(walk);
            else
                Object.DestroyImmediate(walk);
        }

        var idleBob = root.GetComponent<DutzIdleBob>();
        if (idleBob != null)
            idleBob.enabled = false;

        if (root.GetComponent<DutzNPC>() == null)
            root.AddComponent<DutzNPC>();
        if (root.GetComponent<DutzPlayerController>() == null)
            root.AddComponent<DutzPlayerController>();
        if (root.GetComponent<DutzFallRespawn>() == null)
            root.AddComponent<DutzFallRespawn>();
        if (root.GetComponent<DutzSimpleCitizensAnimator>() == null)
            root.AddComponent<DutzSimpleCitizensAnimator>();
        if (root.GetComponent<DutzSimpleCitizensSecondaryMotion>() == null)
            root.AddComponent<DutzSimpleCitizensSecondaryMotion>();

        EnsureMovementAudio(root);

        var npc = root.GetComponent<DutzNPC>();
        var head = FindHeadBone(root);
#if UNITY_EDITOR
        npc.BindReferences(head, null, displayName);
#else
        npc.ConfigureDisplayName(displayName);
#endif
    }
}
