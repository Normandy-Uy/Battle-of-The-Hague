package com.dutz.game;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

public class DutzGalleryPickActivity extends Activity {
    private static final int PICK = 1;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("image/*");
        startActivityForResult(Intent.createChooser(intent, "Select Photo"), PICK);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        String path = "";
        if (requestCode == PICK && resultCode == RESULT_OK && data != null && data.getData() != null)
        {
            String copied = copyToCache(data.getData());
            if (copied != null)
                path = copied;
        }

        UnityPlayer.UnitySendMessage("DutzVictorySelfiePhotoPick", "OnAndroidPhotoPicked", path);
        finish();
    }

    String copyToCache(Uri uri) {
        InputStream in = null;
        OutputStream out = null;
        try {
            in = getContentResolver().openInputStream(uri);
            if (in == null)
                return null;

            File dest = new File(
                getCacheDir(),
                "victory_pick_" + System.currentTimeMillis() + ".jpg");
            out = new FileOutputStream(dest);

            byte[] buffer = new byte[8192];
            int read;
            while ((read = in.read(buffer)) != -1)
                out.write(buffer, 0, read);

            out.flush();
            return dest.getAbsolutePath();
        } catch (Exception ex) {
            return null;
        } finally {
            try {
                if (in != null)
                    in.close();
            } catch (Exception ignored) { }

            try {
                if (out != null)
                    out.close();
            } catch (Exception ignored) { }
        }
    }
}
