using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keyboard polling for gameplay. Editor forwarding fills EditorKeysHeld when Unity blocks keyboard.
/// </summary>
public static class DutzGameplayInput
{
    static readonly HashSet<KeyCode> EditorKeysHeld = new();

    public static void SetEditorKeyHeld(KeyCode key, bool held)
    {
#if UNITY_EDITOR
        if (held)
            EditorKeysHeld.Add(key);
        else
            EditorKeysHeld.Remove(key);
#endif
    }

    public static void ClearEditorKeysHeld()
    {
#if UNITY_EDITOR
        EditorKeysHeld.Clear();
#endif
    }

    public static bool GetKey(KeyCode key)
    {
#if UNITY_EDITOR
        if (Application.isPlaying && EditorKeysHeld.Contains(key))
            return true;
#endif
        return Input.GetKey(key);
    }

    public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);

    public static Vector2 ReadMoveAxis()
    {
        var axis = new Vector2(
            ReadAxis("Horizontal", KeyCode.A, KeyCode.D, KeyCode.LeftArrow, KeyCode.RightArrow),
            ReadAxis("Vertical", KeyCode.S, KeyCode.W, KeyCode.DownArrow, KeyCode.UpArrow));

        if (axis.sqrMagnitude > 1f)
            axis.Normalize();

        return axis;
    }

    static float ReadAxis(string axisName, KeyCode negativeKey, KeyCode positiveKey, KeyCode negativeArrow, KeyCode positiveArrow)
    {
        var value = Input.GetAxisRaw(axisName);

        if (GetKey(positiveKey) || GetKey(positiveArrow))
            value = Mathf.Max(value, 1f);
        if (GetKey(negativeKey) || GetKey(negativeArrow))
            value = Mathf.Min(value, -1f);

        return value;
    }

    public static bool IsGameplayMoveKey(KeyCode key) =>
        key == KeyCode.W || key == KeyCode.A || key == KeyCode.S || key == KeyCode.D ||
        key == KeyCode.UpArrow || key == KeyCode.DownArrow ||
        key == KeyCode.LeftArrow || key == KeyCode.RightArrow;

    public static bool IsTrackedGameplayKey(KeyCode key) =>
        IsGameplayMoveKey(key) || key == KeyCode.Space || key == KeyCode.LeftShift || key == KeyCode.RightShift;
}
