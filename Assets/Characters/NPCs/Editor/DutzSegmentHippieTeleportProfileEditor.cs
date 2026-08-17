using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DutzSegmentHippieTeleportProfile))]
public class DutzSegmentHippieTeleportProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var profile = (DutzSegmentHippieTeleportProfile)target;

        EditorGUILayout.HelpBox(
            "Pool-wide overview. To edit one hippie: select DutzSegmentHippie_01 … _07 in the Hierarchy — " +
            "each has DutzSegmentHippieTeleportSlots with 6 labeled segment positions.",
            MessageType.Info);

        if (GUILayout.Button("Reset To Authored Defaults"))
        {
            Undo.RecordObject(profile, "Reset Hippie Teleport Defaults");
            profile.ApplyAuthoredDefaults();
            EditorUtility.SetDirty(profile);
        }

        EditorGUILayout.Space();

        serializedObject.Update();
        var profilesProp = serializedObject.FindProperty("hippieProfiles");

        if (profilesProp == null || !profilesProp.isArray)
        {
            EditorGUILayout.LabelField("No hippie profiles found.");
            return;
        }

        for (var h = 0; h < profilesProp.arraySize; h++)
        {
            var entryProp = profilesProp.GetArrayElementAtIndex(h);
            var nameProp = entryProp.FindPropertyRelative("hippieName");
            var segmentsProp = entryProp.FindPropertyRelative("segments");
            var hippieLabel = nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue)
                ? nameProp.stringValue
                : $"Hippie {h + 1}";

            EditorGUILayout.LabelField(hippieLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (segmentsProp != null && segmentsProp.isArray)
            {
                for (var s = 0; s < segmentsProp.arraySize && s < DutzSegmentHippieTeleportProfile.SegmentCount; s++)
                {
                    var segmentLabel = s < DutzSegmentHippieTeleportProfile.SegmentLabels.Length
                        ? $"Segment {s + 1} — {DutzSegmentHippieTeleportProfile.SegmentLabels[s]}"
                        : $"Segment {s + 1}";

                    var poseProp = segmentsProp.GetArrayElementAtIndex(s);
                    EditorGUILayout.PropertyField(poseProp, new GUIContent(segmentLabel), true);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
