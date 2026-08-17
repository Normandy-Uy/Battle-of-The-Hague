using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>MCP/editor diagnostics for Player1 punch + animator.</summary>
public static class DutzPlayerPunchDiagnostics
{
    public static void DiagnoseFromMenu()
    {
        var report = BuildReport(forceTestPunch: Application.isPlaying);
        Debug.Log(report);
    }

    /// <summary>Batch: -executeMethod DutzPlayerPunchDiagnostics.DiagnoseActiveSceneBatch</summary>
    public static void DiagnoseActiveSceneBatch() => DiagnoseFromMenu();

    public static string BuildReport(bool forceTestPunch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Dutz] Player punch diagnostics");

        var player = DutzPlayerController.Instance;
        if (player == null)
            player = Object.FindObjectOfType<DutzPlayerController>();

        if (player == null)
        {
            sb.AppendLine("FAIL: Player1 / DutzPlayerController not found.");
            return sb.ToString();
        }

        sb.AppendLine($"Player: {player.name} active={player.gameObject.activeInHierarchy}");
        sb.AppendLine($"ControlsLocked={player.ControlsLocked}");

        var punch = player.GetComponent<DutzPlayerPunch>();
        sb.AppendLine($"DutzPlayerPunch={(punch != null ? "YES" : "MISSING")} enabled={(punch != null && punch.enabled)}");

        var walk = player.GetComponent<DutzWalkAnimation>();
        sb.AppendLine($"DutzWalkAnimation={(walk != null ? "YES (should be removed on SimpleCitizens player)" : "no")}");

        var animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            sb.AppendLine("FAIL: Animator missing on player.");
            return sb.ToString();
        }

        sb.AppendLine($"Animator enabled={animator.enabled} humanoid={animator.isHuman} culling={animator.cullingMode}");
        sb.AppendLine($"Controller={(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");

        var hasPunchParam = false;
        foreach (var p in animator.parameters)
        {
            if (p.name == "Punch_b")
            {
                hasPunchParam = true;
                sb.AppendLine($"Punch_b parameter type={p.type}");
                break;
            }
        }

        sb.AppendLine(hasPunchParam ? "Punch_b parameter: OK" : "FAIL: Punch_b parameter missing");

        LogBone(sb, animator, "RightShoulder", HumanBodyBones.RightShoulder);
        LogBone(sb, animator, "RightUpperArm", HumanBodyBones.RightUpperArm);
        LogBone(sb, animator, "RightLowerArm", HumanBodyBones.RightLowerArm);
        LogBone(sb, animator, "RightHand", HumanBodyBones.RightHand);

        if (punch != null)
        {
            sb.AppendLine($"SUPERPUNCH_DAMAGE (Inspector)={punch.SuperPunchDamage}");
            sb.AppendLine($"HasSuperPunchActive={punch.HasSuperPunchActive}");
            sb.AppendLine($"GetCurrentPunchDamage()={punch.GetCurrentPunchDamage()}");
            punch.EnsureBonesCachedForDiagnostics();
            sb.AppendLine(punch.BuildRuntimeDiagnostics());
        }

        if (forceTestPunch && punch != null)
        {
            punch.DebugForcePunch();
            animator.Update(0f);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            sb.AppendLine($"After DebugForcePunch: state={GetStateName(animator)} shortHash={state.shortNameHash} normalizedTime={state.normalizedTime:F2}");
            sb.AppendLine($"IsPunchingVisual={punch.IsPunchingVisual}");
        }

        return sb.ToString();
    }

    static void LogBone(StringBuilder sb, Animator animator, string label, HumanBodyBones bone)
    {
        var t = animator.GetBoneTransform(bone);
        if (t == null)
        {
            sb.AppendLine($"{label}: MISSING");
            return;
        }

        sb.AppendLine($"{label}: {t.name} active={t.gameObject.activeInHierarchy} euler={t.localEulerAngles}");
    }

    static string GetStateName(Animator animator)
    {
        var info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.IsName("Punch"))
            return "Punch";
        if (info.IsName("Idle"))
            return "Idle";
        if (info.IsName("Walk"))
            return "Walk";
        if (info.IsName("Run"))
            return "Run";
        return $"hash={info.shortNameHash}";
    }
}
