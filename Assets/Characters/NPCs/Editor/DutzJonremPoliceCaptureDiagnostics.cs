using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Simulates Jonrem police capture reach on Level 01 without manual play.</summary>
public static class DutzJonremPoliceCaptureDiagnostics
{
    const string LogPath = @"c:\Users\admin\Free Dutz 2025\New Unity Project\debug-11ec65.log";
    const string Level01Scene = "Assets/Scenes/Dutz_Level01.unity";

    /// <summary>Batch: -executeMethod DutzJonremPoliceCaptureDiagnostics.RunBatch</summary>
    public static void RunBatch() => Run(logToFile: true);

    public static void Run(bool logToFile)
    {
        if (!File.Exists(Level01Scene))
        {
            Debug.LogError("[Dutz] Level 01 scene not found for police capture diagnostics.");
            return;
        }

        var activeScene = SceneManager.GetActiveScene();
        var activePath = activeScene.path;
        EditorSceneManager.OpenScene(Level01Scene, OpenSceneMode.Single);

        var sb = new StringBuilder();
        sb.AppendLine("[Dutz] Jonrem police capture diagnostics");

        var police = DutzJonremPoliceBehavior.FindJonremPolice();
        var player = Object.FindObjectOfType<DutzPlayerController>();
        var cc = player != null ? player.GetComponent<CharacterController>() : null;

        if (police.Length == 0)
            sb.AppendLine("FAIL: no Jonrem police found.");
        if (cc == null)
            sb.AppendLine("FAIL: Player1 CharacterController not found.");

        foreach (var officer in police)
        {
            if (officer == null)
                continue;

            DutzJonremPoliceCapture.EnsureOnPolice(officer);
            var capture = officer.GetComponent<DutzJonremPoliceCapture>();
            sb.AppendLine($"Officer: {officer.name} pos={officer.transform.position} scale={officer.transform.lossyScale.x:F2}");

            foreach (var col in officer.GetComponents<BoxCollider>())
            {
                if (col == null || !col.isTrigger)
                    continue;

                var worldSize = Vector3.Scale(col.size, officer.transform.lossyScale);
                sb.AppendLine(
                    $"  trigger local={col.size} world={worldSize} center={col.center} reach={capture.CaptureReachMeters:F3}");
            }
        }

        if (cc != null)
        {
            var playerBounds = DutzHippieBiteCollider.GetPlayerBodyBounds(cc);
            sb.AppendLine($"Player bounds center={playerBounds.center} size={playerBounds.size}");

            foreach (var officer in police)
            {
                if (officer == null)
                    continue;

                var capture = officer.GetComponent<DutzJonremPoliceCapture>();
                if (capture == null)
                    continue;

                var basePos = officer.transform.position;
                var lateralOffsets = new[] { 0f, 1f, 2f, 3f, 4f, 5f, 6f };
                foreach (var lateral in lateralOffsets)
                {
                    foreach (var sign in new[] { -1f, 1f })
                    {
                        var sample = player.transform.position;
                        sample.x = basePos.x + lateral * sign;
                        sample.z = basePos.z;
                        sample.y = basePos.y;

                        var wouldCapture = SimulateCaptureAt(capture, cc, sample, out var gap);
                        sb.AppendLine(
                            $"  sample {officer.name} lateralX={lateral * sign:F1} gap={gap:F3} capture={wouldCapture}");
                        WriteAgentLog(
                            "SIM",
                            "DutzJonremPoliceCaptureDiagnostics.Run",
                            "Capture simulation sample",
                            officer.name,
                            sample,
                            gap,
                            wouldCapture,
                            capture);
                    }
                }
            }
        }

        var report = sb.ToString();
        Debug.Log(report);

        if (logToFile)
            File.WriteAllText(LogPath, report);

        if (!string.IsNullOrEmpty(activePath) && activePath != Level01Scene)
            EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
    }

    static bool SimulateCaptureAt(
        DutzJonremPoliceCapture capture,
        CharacterController cc,
        Vector3 playerPos,
        out float minGap)
    {
        minGap = float.MaxValue;
        var slop = capture.CaptureReachMeters;
        var touching = false;
        var playerBounds = BuildPlayerBoundsAt(cc, playerPos);
        var savedPos = cc.transform.position;

        cc.transform.position = playerPos;
        try
        {
            foreach (var col in capture.GetComponents<BoxCollider>())
            {
                if (col == null || !col.enabled || !col.isTrigger)
                    continue;

                var closestOnBody = col.ClosestPoint(playerBounds.center);
                var closestOnPlayer = playerBounds.ClosestPoint(closestOnBody);
                var gap = (closestOnBody - closestOnPlayer).magnitude;
                if (gap < minGap)
                    minGap = gap;

                if (DutzHippieBiteCollider.IsColliderContactingPlayerCapsule(col, cc, 0f, slop))
                    touching = true;
            }
        }
        finally
        {
            cc.transform.position = savedPos;
        }

        if (minGap == float.MaxValue)
            minGap = -1f;

        return touching;
    }

    static Bounds BuildPlayerBoundsAt(CharacterController cc, Vector3 playerPos)
    {
        var pad = DutzHippieBiteCollider.PlayerTouchBoundsPadding;
        var scale = Mathf.Max(0.01f, cc.transform.lossyScale.y);
        var center = playerPos + cc.center * scale;
        var size = new Vector3(
            (cc.radius + pad) * 2f * cc.transform.lossyScale.x,
            (cc.height + pad * 2f) * scale,
            (cc.radius + pad) * 2f * cc.transform.lossyScale.z);
        return new Bounds(center, size);
    }

    static void WriteAgentLog(
        string hypothesisId,
        string location,
        string message,
        string policeName,
        Vector3 playerPos,
        float gap,
        bool capture,
        DutzJonremPoliceCapture captureComponent)
    {
        try
        {
            var ts = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var line =
                "{\"sessionId\":\"11ec65\",\"hypothesisId\":\"" + hypothesisId +
                "\",\"location\":\"" + location +
                "\",\"message\":\"" + message +
                "\",\"data\":{\"policeName\":\"" + policeName +
                "\",\"playerPos\":{\"x\":" + playerPos.x.ToString("F2") +
                ",\"y\":" + playerPos.y.ToString("F2") +
                ",\"z\":" + playerPos.z.ToString("F2") +
                "},\"minGapMeters\":" + gap.ToString("F3") +
                ",\"capture\":" + (capture ? "true" : "false") +
                ",\"captureReachMeters\":" + captureComponent.CaptureReachMeters.ToString("F3") +
                "},\"timestamp\":" + ts + "}\n";
            File.AppendAllText(LogPath, line);
        }
        catch
        {
            // ignored
        }
    }
}
