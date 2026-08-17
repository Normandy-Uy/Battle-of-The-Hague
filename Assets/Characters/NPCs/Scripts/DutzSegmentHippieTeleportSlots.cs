using UnityEngine;

/// <summary>
/// Six segment teleport poses for one pooled small hippie — visible on each DutzSegmentHippie_XX object.
/// </summary>
[DisallowMultipleComponent]
public class DutzSegmentHippieTeleportSlots : MonoBehaviour
{
    [Header("Segment 1 — Highway Bridge 1")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment1_Bridge1;

    [Header("Segment 2 — Highway Straight 2")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment2_Straight2;

    [Header("Segment 3 — Highway Straight 3")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment3_Straight3;

    [Header("Segment 4 — Highway Bridge 4")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment4_Bridge4;

    [Header("Segment 5 — Highway Bridge 5")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment5_Bridge5;

    [Header("Segment 6 — Highway Straight 6")]
    [SerializeField] DutzSegmentHippieTeleportProfile.TeleportPose segment6_Straight6;

    public bool HasValidData => GetPose(0).position != Vector3.zero || GetPose(5).position != Vector3.zero;

    public DutzSegmentHippieTeleportProfile.TeleportPose GetPose(int segmentIndex)
    {
        return segmentIndex switch
        {
            0 => segment1_Bridge1,
            1 => segment2_Straight2,
            2 => segment3_Straight3,
            3 => segment4_Bridge4,
            4 => segment5_Bridge5,
            5 => segment6_Straight6,
            _ => default
        };
    }

    public void SetPose(int segmentIndex, DutzSegmentHippieTeleportProfile.TeleportPose pose)
    {
        switch (segmentIndex)
        {
            case 0: segment1_Bridge1 = pose; break;
            case 1: segment2_Straight2 = pose; break;
            case 2: segment3_Straight3 = pose; break;
            case 3: segment4_Bridge4 = pose; break;
            case 4: segment5_Bridge5 = pose; break;
            case 5: segment6_Straight6 = pose; break;
        }
    }

    public void ApplyAuthoredDefaults(int hippieIndex)
    {
        var anchors = new Vector2[]
        {
            new Vector2(-890f, 9.2f),
            new Vector2(-580f, 6f),
            new Vector2(-310f, 2.8f),
            new Vector2(10f, 7.2f),
            new Vector2(390f, 6.6f),
            new Vector2(725f, 3.5f)
        };

        var zOffsets = new[] { 5f, 0f, -5f, -10f, -15f, -20f, -25f };
        var z = hippieIndex >= 0 && hippieIndex < zOffsets.Length ? zOffsets[hippieIndex] : 0f;
        var euler = new Vector3(0f, -90f, 0f);

        for (var s = 0; s < DutzSegmentHippieTeleportProfile.SegmentCount; s++)
        {
            SetPose(s, new DutzSegmentHippieTeleportProfile.TeleportPose
            {
                position = new Vector3(anchors[s].x, anchors[s].y, z),
                eulerAngles = euler
            });
        }
    }

    public void CopyFromProfileEntry(DutzSegmentHippieTeleportProfile.HippieTeleportEntry entry)
    {
        if (entry?.segments == null)
            return;

        for (var s = 0; s < DutzSegmentHippieTeleportProfile.SegmentCount && s < entry.segments.Length; s++)
            SetPose(s, entry.segments[s]);
    }
}
