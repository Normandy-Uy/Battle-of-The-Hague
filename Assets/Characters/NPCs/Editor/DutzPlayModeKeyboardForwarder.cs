#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Forwards arrow/WASD keys into the running game during Play Mode.
/// Unity 2022 blocks keyboard unless Game view is focused — QueueGameViewInputEvent fixes that.
/// </summary>
[InitializeOnLoad]
public static class DutzPlayModeKeyboardForwarder
{
    static bool globalHooked;

    static DutzPlayModeKeyboardForwarder()
    {
        SceneView.duringSceneGui += OnDuringSceneGui;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += TryHookGlobalEventHandler;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            DutzGameplayInput.ClearEditorKeysHeld();
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                    FocusGameView();
            };
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            DutzGameplayInput.ClearEditorKeysHeld();
        }
    }

    static void TryHookGlobalEventHandler()
    {
        if (globalHooked)
            return;

        var field = typeof(EditorApplication).GetField(
            "globalEventHandler",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        if (field == null)
            return;

        globalHooked = true;
        var previous = field.GetValue(null) as EditorApplication.CallbackFunction;
        field.SetValue(null, (EditorApplication.CallbackFunction)(() =>
        {
            ForwardCurrentEvent();
            previous?.Invoke();
        }));
    }

    static void OnDuringSceneGui(SceneView sceneView) => ForwardCurrentEvent();

    static void ForwardCurrentEvent()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        var e = Event.current;
        if (e == null)
            return;

        if (e.type != EventType.KeyDown && e.type != EventType.KeyUp)
            return;

        if (!DutzGameplayInput.IsTrackedGameplayKey(e.keyCode))
            return;

        DutzGameplayInput.SetEditorKeyHeld(e.keyCode, e.type == EventType.KeyDown);

        if (Application.isFocused && IsGameViewFocused())
            return;

        EditorGUIUtility.QueueGameViewInputEvent(e);

        if (EditorWindow.focusedWindow is SceneView)
            e.Use();
    }

    public static void FocusGameView()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType != null)
        {
            var window = EditorWindow.GetWindow(gameViewType, false, null, false);
            if (window != null)
            {
                window.Focus();
                return;
            }
        }

        if (!EditorApplication.ExecuteMenuItem("Window/General/Game"))
            EditorApplication.ExecuteMenuItem("Window/General/GameView");
    }

    static bool IsGameViewFocused()
    {
        var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (gameViewType == null)
            return false;

        var focused = EditorWindow.focusedWindow;
        return focused != null && focused.GetType() == gameViewType;
    }
}
#endif
