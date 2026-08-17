using UnityEngine;

/// <summary>Keep mouse unlocked while modal IMGUI dialogs are showing (desktop).</summary>
public static class DutzDialogCursor
{
    public static void EnsureUnlockedForDialog()
    {
        if (Application.isMobilePlatform)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
