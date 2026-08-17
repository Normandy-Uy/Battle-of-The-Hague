package com.dutz.battleofthehague;

import android.os.Bundle;
import androidx.core.view.WindowCompat;
import com.unity3d.player.UnityPlayerActivity;

/**
 * Edge-to-edge for Android 15+ targets. UnityPlayerActivity is not a ComponentActivity,
 * so use WindowCompat (works with Unity's Activity base class).
 */
public class DutzUnityPlayerActivity extends UnityPlayerActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        WindowCompat.setDecorFitsSystemWindows(getWindow(), false);
        super.onCreate(savedInstanceState);
    }
}
