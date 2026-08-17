using System;
using UnityEngine;

/// <summary>
/// Inspector-editable teleport poses for the 7 pooled small hippies across 6 highway segments.
/// Lives on DutzSegmentHippiePool.
/// </summary>
[DisallowMultipleComponent]
public class DutzSegmentHippieTeleportProfile : MonoBehaviour
{
    public const int SegmentCount = 6;
    public const int HippieCount = 7;

    public static readonly string[] SegmentLabels =
    {
        "Highway Bridge 1",
        "Highway Straight 2",
        "Highway Straight 3",
        "Highway Bridge 4",
        "Highway Bridge 5",
        "Highway Straight 6"
    };

    [Serializable]
    public struct TeleportPose
    {
        public Vector3 position;
        public Vector3 eulerAngles;

        public Quaternion Rotation => Quaternion.Euler(eulerAngles);
    }

    [Serializable]
    public class HippieTeleportEntry
    {
        public string hippieName;
        public TeleportPose[] segments = new TeleportPose[SegmentCount];
    }

    [SerializeField] HippieTeleportEntry[] hippieProfiles = new HippieTeleportEntry[HippieCount];

    public bool HasValidData =>
        hippieProfiles != null
        && hippieProfiles.Length >= HippieCount
        && hippieProfiles[0] != null
        && hippieProfiles[0].segments != null
        && hippieProfiles[0].segments.Length >= SegmentCount;

    public HippieTeleportEntry GetEntry(int hippieIndex)
    {
        if (!HasValidData || hippieIndex < 0 || hippieIndex >= hippieProfiles.Length)
            return null;

        return hippieProfiles[hippieIndex];
    }

    public TeleportPose GetPose(int hippieIndex, int segmentIndex)
    {
        if (!HasValidData
            || hippieIndex < 0
            || hippieIndex >= hippieProfiles.Length
            || segmentIndex < 0
            || segmentIndex >= SegmentCount)
        {
            return default;
        }

        var entry = hippieProfiles[hippieIndex];
        if (entry?.segments == null || segmentIndex >= entry.segments.Length)
            return default;

        return entry.segments[segmentIndex];
    }

    public void ApplyAuthoredDefaults()
    {
        EnsureProfileArray();

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
        var defaultEuler = new Vector3(0f, -90f, 0f);

        for (var h = 0; h < HippieCount; h++)
        {
            var entry = hippieProfiles[h];
            entry.hippieName = $"{DutzSegmentHippieIdentity.HippiePrefix}{h + 1:00}";
            if (entry.segments == null || entry.segments.Length != SegmentCount)
                entry.segments = new TeleportPose[SegmentCount];

            for (var s = 0; s < SegmentCount; s++)
            {
                entry.segments[s] = new TeleportPose
                {
                    position = new Vector3(anchors[s].x, anchors[s].y, zOffsets[h]),
                    eulerAngles = defaultEuler
                };
            }
        }
    }

    /// <summary>Hand-placed poses from Dutz_Level02 pool (varied X/Y/Z per hippie, not a straight row).</summary>
    public void ApplyIndividualAuthoredPositions()
    {
        EnsureProfileArray();

        var defaultEuler = new Vector3(0f, -90f, 0f);
        var positions = IndividualAuthoredSegmentPositions;

        for (var h = 0; h < HippieCount; h++)
        {
            var entry = hippieProfiles[h];
            entry.hippieName = $"{DutzSegmentHippieIdentity.HippiePrefix}{h + 1:00}";
            if (entry.segments == null || entry.segments.Length != SegmentCount)
                entry.segments = new TeleportPose[SegmentCount];

            for (var s = 0; s < SegmentCount; s++)
            {
                entry.segments[s] = new TeleportPose
                {
                    position = positions[h, s],
                    eulerAngles = defaultEuler
                };
            }
        }
    }

    /// <summary>Index [hippie 0..6, segment 0..5] — copied from Dutz_Level02 DutzSegmentHippieTeleportSlots.</summary>
    static readonly Vector3[,] IndividualAuthoredSegmentPositions =
    {
        {
            new Vector3(-890f, 9.2f, 5f),
            new Vector3(-580f, 6f, 5f),
            new Vector3(-310f, 2.8f, 5f),
            new Vector3(10f, 7.2f, 5f),
            new Vector3(390f, 6.6f, 5f),
            new Vector3(725f, 3.5f, 5f),
        },
        {
            new Vector3(-870f, 10f, 0f),
            new Vector3(-560f, 6f, 0f),
            new Vector3(-290f, 3.5f, 0f),
            new Vector3(0f, 7.1f, 0f),
            new Vector3(390f, 6.6f, 0f),
            new Vector3(725f, 3.5f, 0f),
        },
        {
            new Vector3(-850f, 10.5f, -5f),
            new Vector3(-540f, 5.5f, -5f),
            new Vector3(-270f, 4f, -5f),
            new Vector3(-10f, 7f, -5f),
            new Vector3(390f, 6.6f, -5f),
            new Vector3(725f, 3.5f, -5f),
        },
        {
            new Vector3(-830f, 11.35f, -10f),
            new Vector3(-520f, 5f, -10f),
            new Vector3(-250f, 4.5f, -10f),
            new Vector3(-20f, 6.9f, -10f),
            new Vector3(390f, 6.6f, -10f),
            new Vector3(725f, 3.5f, -10f),
        },
        {
            new Vector3(-810f, 12.43f, -15f),
            new Vector3(-500f, 4.5f, -15f),
            new Vector3(-230f, 5f, -15f),
            new Vector3(-30f, 6.8f, -15f),
            new Vector3(390f, 6.6f, -15f),
            new Vector3(725f, 3.5f, -15f),
        },
        {
            new Vector3(-780f, 13.5f, -20f),
            new Vector3(-480f, 4f, -20f),
            new Vector3(-210f, 5.5f, -20f),
            new Vector3(-40f, 6.7f, -20f),
            new Vector3(390f, 6.6f, -20f),
            new Vector3(725f, 3.5f, -20f),
        },
        {
            new Vector3(-760f, 14.3f, -25f),
            new Vector3(-460f, 3.5f, -25f),
            new Vector3(-190f, 6f, -25f),
            new Vector3(-50f, 6.7f, -25f),
            new Vector3(390f, 6.6f, -25f),
            new Vector3(725f, 3.5f, -25f),
        },
    };

    /// <summary>Sets each pose Y from the walkable road deck at its X/Z (keeps crocs on top of roads).</summary>
    public void SnapPoseHeightsToWalkableDeck()
    {
        EnsureProfileArray();

        for (var h = 0; h < HippieCount; h++)
        {
            var entry = hippieProfiles[h];
            for (var s = 0; s < SegmentCount; s++)
            {
                var pose = entry.segments[s];
                var hint = pose.position.y;
                var probe = new Vector3(pose.position.x, hint, pose.position.z);
                if (DutzRoadGround.TrySampleRoadDeckForPlacement(probe, hint, null, out var deckY))
                    pose.position = new Vector3(pose.position.x, deckY, pose.position.z);

                entry.segments[s] = pose;
            }
        }
    }

    public void CopyToHippieSlots(Transform poolRoot)
    {
        if (poolRoot == null || !HasValidData)
            return;

        for (var h = 0; h < HippieCount; h++)
        {
            var childName = $"{DutzSegmentHippieIdentity.HippiePrefix}{h + 1:00}";
            var child = poolRoot.Find(childName);
            if (child == null)
                continue;

            var slots = child.GetComponent<DutzSegmentHippieTeleportSlots>();
            if (slots == null)
                slots = child.gameObject.AddComponent<DutzSegmentHippieTeleportSlots>();

            slots.CopyFromProfileEntry(hippieProfiles[h]);
        }
    }

    public void PlaceHippiesAtSegmentOne(Transform poolRoot)
    {
        if (poolRoot == null || !HasValidData)
            return;

        for (var h = 0; h < HippieCount; h++)
        {
            var childName = $"{DutzSegmentHippieIdentity.HippiePrefix}{h + 1:00}";
            var child = poolRoot.Find(childName);
            if (child == null)
                continue;

            var pose = GetPose(h, 0);
            child.SetPositionAndRotation(pose.position, pose.Rotation);
        }
    }

    void EnsureProfileArray()
    {
        if (hippieProfiles == null || hippieProfiles.Length != HippieCount)
            hippieProfiles = new HippieTeleportEntry[HippieCount];

        for (var i = 0; i < HippieCount; i++)
        {
            if (hippieProfiles[i] == null)
                hippieProfiles[i] = new HippieTeleportEntry();

            if (hippieProfiles[i].segments == null || hippieProfiles[i].segments.Length != SegmentCount)
                hippieProfiles[i].segments = new TeleportPose[SegmentCount];
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureProfileArray();
        for (var i = 0; i < HippieCount; i++)
        {
            if (string.IsNullOrEmpty(hippieProfiles[i].hippieName))
                hippieProfiles[i].hippieName = $"{DutzSegmentHippieIdentity.HippiePrefix}{i + 1:00}";
        }
    }
#endif
}
