using UnityEditor;

using UnityEditor.Animations;

using UnityEngine;



/// <summary>Creates PlayerPunch.anim and wires Punch_b into SimpleCitizens.controller.</summary>

public static class DutzPunchAnimationBuilder

{

    const string ClipPath = "Assets/Characters/NPCs/Animations/PlayerPunch.anim";

    const string ControllerPath = "Assets/SimpleCitizens/Models/SimpleCitizens.controller";

    const string PlayerPrefabPath = "Assets/Characters/NPCs/Prefabs/Dutz.prefab";

    const string PunchParameter = "Punch_b";

    const string PunchStateName = "Punch";



    public static void SetupFromMenu()

    {

        if (!BuildPunchAnimation(log: true))

        {

            EditorUtility.DisplayDialog("Player Punch", "Could not build punch animation. Check Console.", "OK");

            return;

        }



        EditorUtility.DisplayDialog("Player Punch", "Punch animation and animator trigger are ready.", "OK");

    }



    /// <summary>Batch: -executeMethod DutzPunchAnimationBuilder.BuildPunchAnimationBatch</summary>

    public static void BuildPunchAnimationBatch() => BuildPunchAnimation(log: true);



    public static bool BuildPunchAnimation(bool log)

    {

        var clip = CreateOrUpdateClip();

        if (clip == null)

            return false;



        if (!WireAnimatorController(clip))

            return false;



        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();



        if (log)

            Debug.Log("[Dutz] Player punch animation + Punch_b trigger configured.");



        return true;

    }



    static AnimationClip CreateOrUpdateClip()

    {

        var referenceRoot = LoadPlayerReferenceRoot();

        if (referenceRoot == null)

        {

            Debug.LogError("[Dutz] Could not load player prefab for punch bone paths: " + PlayerPrefabPath);

            return null;

        }



        var shoulderPath = FindBonePath(referenceRoot, "Shoulder_Right_jnt");

        var armPath = FindBonePath(referenceRoot, "Arm_Right_jnt", "UpperArm_Right_jnt");

        var forearmPath = FindBonePath(referenceRoot, "Forearm_Right_jnt", "LowerArm_Right_jnt");

        var handPath = FindBonePath(referenceRoot, "Hand_Right_jnt");

        var chestPath = FindBonePath(referenceRoot, "Chest_jnt");

        var spinePath = FindBonePath(referenceRoot, "Spine_jnt");



        if (string.IsNullOrEmpty(armPath) || string.IsNullOrEmpty(forearmPath))

        {

            Debug.LogError("[Dutz] Punch rig missing Arm_Right_jnt / Forearm_Right_jnt on player prefab.");

            return null;

        }



        System.IO.Directory.CreateDirectory("Assets/Characters/NPCs/Animations");



        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);

        if (clip == null)

        {

            clip = new AnimationClip { name = "PlayerPunch" };

            AssetDatabase.CreateAsset(clip, ClipPath);

        }



        clip.ClearCurves();

        clip.frameRate = 30f;

        clip.legacy = false;



        if (!string.IsNullOrEmpty(shoulderPath))

        {

            SetEulerKeyframes(clip, shoulderPath,

                (0f, new Vector3(0f, 0f, 0f)),

                (0.08f, new Vector3(0f, -6f, 24f)),

                (0.13f, new Vector3(8f, -12f, -6f)),

                (0.30f, new Vector3(0f, 0f, 0f)));

        }



        SetEulerKeyframes(clip, armPath,

            (0f, new Vector3(0f, 0f, 0f)),

            (0.08f, new Vector3(-90f, -8f, 20f)),

            (0.13f, new Vector3(-160f, -8f, 78f)),

            (0.30f, new Vector3(0f, 0f, 0f)));



        SetEulerKeyframes(clip, forearmPath,

            (0f, new Vector3(0f, 0f, 0f)),

            (0.09f, new Vector3(-70f, 0f, 0f)),

            (0.14f, new Vector3(-10f, 0f, 0f)),

            (0.30f, new Vector3(0f, 0f, 0f)));



        if (!string.IsNullOrEmpty(handPath))

        {

            SetEulerKeyframes(clip, handPath,

                (0f, new Vector3(0f, 0f, 0f)),

                (0.10f, new Vector3(8f, 0f, 0f)),

                (0.14f, new Vector3(18f, 0f, 0f)),

                (0.30f, new Vector3(0f, 0f, 0f)));

        }



        EditorUtility.SetDirty(clip);

        return clip;
    }



    static Transform LoadPlayerReferenceRoot()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        return prefab != null ? prefab.transform : null;
    }



    static string FindBonePath(Transform root, params string[] boneNames)

    {

        if (root == null || boneNames == null || boneNames.Length == 0)

            return null;



        foreach (var boneName in boneNames)

        {

            foreach (var child in root.GetComponentsInChildren<Transform>(true))

            {

                if (child.name != boneName)

                    continue;



                return AnimationUtility.CalculateTransformPath(child, root);

            }

        }



        return null;

    }



    static void SetEulerKeyframes(AnimationClip clip, string path, params (float time, Vector3 euler)[] keys)

    {

        var x = new Keyframe[keys.Length];

        var y = new Keyframe[keys.Length];

        var z = new Keyframe[keys.Length];



        for (var i = 0; i < keys.Length; i++)

        {

            x[i] = new Keyframe(keys[i].time, keys[i].euler.x);

            y[i] = new Keyframe(keys[i].time, keys[i].euler.y);

            z[i] = new Keyframe(keys[i].time, keys[i].euler.z);

        }



        clip.SetCurve(path, typeof(Transform), "localEulerAngles.x", new AnimationCurve(x));

        clip.SetCurve(path, typeof(Transform), "localEulerAngles.y", new AnimationCurve(y));

        clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", new AnimationCurve(z));

    }



    static bool WireAnimatorController(AnimationClip clip)

    {

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)

        {

            Debug.LogError("[Dutz] Missing animator controller: " + ControllerPath);

            return false;

        }



        if (!HasParameter(controller, PunchParameter))

            controller.AddParameter(PunchParameter, AnimatorControllerParameterType.Trigger);



        var layer = controller.layers[0].stateMachine;

        var punchState = FindState(layer, PunchStateName);

        if (punchState == null)

        {

            punchState = layer.AddState(PunchStateName, new Vector3(360f, 120f, 0f));

            var toIdle = punchState.AddTransition(FindState(layer, "Idle"));

            toIdle.hasExitTime = true;

            toIdle.exitTime = 0.92f;

            toIdle.duration = 0.08f;

        }



        punchState.motion = clip;

        punchState.speed = 1.35f;



        var hasAnyState = false;

        foreach (var transition in layer.anyStateTransitions)

        {

            if (transition.destinationState == punchState)

            {

                hasAnyState = true;

                break;

            }

        }



        if (!hasAnyState)

        {

            var anyTransition = layer.AddAnyStateTransition(punchState);

            anyTransition.AddCondition(AnimatorConditionMode.If, 0f, PunchParameter);

            anyTransition.duration = 0.02f;

            anyTransition.canTransitionToSelf = false;

            anyTransition.interruptionSource = TransitionInterruptionSource.None;

        }



        EditorUtility.SetDirty(controller);

        return true;

    }



    static AnimatorState FindState(AnimatorStateMachine machine, string stateName)

    {

        foreach (var child in machine.states)

        {

            if (child.state != null && child.state.name == stateName)

                return child.state;

        }



        return null;

    }



    static bool HasParameter(AnimatorController controller, string name)

    {

        foreach (var parameter in controller.parameters)

        {

            if (parameter.name == name)

                return true;

        }



        return false;

    }

}


