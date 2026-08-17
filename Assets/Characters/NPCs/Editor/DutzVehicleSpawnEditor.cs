using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DutzVehicleSpawn))]
[CanEditMultipleObjects]
public class DutzVehicleSpawnEditor : Editor
{
    SerializedProperty spawnPosition;
    SerializedProperty spawnHeadingDegrees;
    SerializedProperty spawnPoseLocked;
    SerializedProperty moveSpeed;
    SerializedProperty snapToRoad;
    SerializedProperty fallYThreshold;
    SerializedProperty respawnEnabled;

    void OnEnable()
    {
        spawnPosition = serializedObject.FindProperty("spawnPosition");
        spawnHeadingDegrees = serializedObject.FindProperty("spawnHeadingDegrees");
        spawnPoseLocked = serializedObject.FindProperty("spawnPoseLocked");
        moveSpeed = serializedObject.FindProperty("moveSpeed");
        snapToRoad = serializedObject.FindProperty("snapToRoad");
        fallYThreshold = serializedObject.FindProperty("fallYThreshold");
        respawnEnabled = serializedObject.FindProperty("respawnEnabled");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Exit Play mode to edit spawn position and heading.",
                MessageType.Warning);
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.LabelField("Spawn Pose", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spawnPosition, new GUIContent("Spawn Position"));

            var highwayHeading = 90f;
            var hasHighwayHeading = DutzVehicleSpawn.TryGetHighwayHeadingDegrees(out highwayHeading);

            EditorGUILayout.HelpBox(
                hasHighwayHeading
                    ? $"On Level 00, ~{highwayHeading:0}° faces down the highway. Your 89.7° is basically correct.\n\n" +
                      "Edit the number below, or rotate the car in the Scene view and click Capture From Transform."
                    : "Heading = which way the vehicle faces (0–360°). Rotate in Scene view, then Capture From Transform.",
                MessageType.Info);

            if (spawnHeadingDegrees != null)
            {
                EditorGUI.BeginChangeCheck();
                var heading = EditorGUILayout.DelayedFloatField(
                    new GUIContent("Spawn Heading (degrees)", "Turn on the flat road. Press Enter after typing."),
                    spawnHeadingDegrees.floatValue);
                if (EditorGUI.EndChangeCheck())
                    spawnHeadingDegrees.floatValue = heading;

                EditorGUI.BeginChangeCheck();
                var slider = EditorGUILayout.Slider("Turn on road", spawnHeadingDegrees.floatValue, 0f, 360f);
                if (EditorGUI.EndChangeCheck())
                    spawnHeadingDegrees.floatValue = slider;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (hasHighwayHeading && GUILayout.Button($"Use Highway Heading ({highwayHeading:0}°)"))
                    spawnHeadingDegrees.floatValue = highwayHeading;

                if (DutzVehicleSpawn.TryGetAcrossRoadHeadingDegrees(out var acrossHeading)
                    && GUILayout.Button($"Use Across Road ({acrossHeading:0}°)"))
                {
                    spawnHeadingDegrees.floatValue = acrossHeading;
                }
            }

            EditorGUILayout.PropertyField(
                spawnPoseLocked,
                new GUIContent(
                    "Lock Batch Bake",
                    "When on, Tools/menu vehicle setup will not overwrite this pose. Does not block edits here."));
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Capture From Transform"))
                {
                    foreach (var t in targets)
                    {
                        if (t is not DutzVehicleSpawn spawn)
                            continue;

                        Undo.RecordObject(spawn, "Capture vehicle spawn pose");
                        spawn.CaptureSpawnPoseFromTransform(force: true);
                        EditorUtility.SetDirty(spawn);
                    }
                }

                if (GUILayout.Button("Apply To Transform"))
                {
                    foreach (var t in targets)
                    {
                        if (t is not DutzVehicleSpawn spawn)
                            continue;

                        Undo.RecordObject(spawn.transform, "Apply vehicle spawn pose");
                        spawn.ApplySpawnPose();
                        EditorUtility.SetDirty(spawn);
                    }
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moveSpeed);
        EditorGUILayout.PropertyField(snapToRoad);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fall Respawn", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fallYThreshold);
        EditorGUILayout.PropertyField(respawnEnabled);

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (var t in targets)
            {
                if (t is DutzVehicleSpawn spawn)
                    EditorUtility.SetDirty(spawn);
            }
        }
    }
}
