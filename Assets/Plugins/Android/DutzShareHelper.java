package com.dutz.game;

import android.app.Activity;
import android.content.ClipData;
import android.content.ContentResolver;
import android.content.ContentValues;
import android.content.Intent;
import android.media.MediaScannerConnection;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.provider.MediaStore;
import android.util.Log;
import android.widget.Toast;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class DutzShareHelper {
    static final String TAG = "DutzShareHelper";
    static final String ALBUM = "BattleOfTheHague";

    public static boolean shareImage(Activity activity, String filePath, String text) {
        if (activity == null || filePath == null)
            return false;

        File file = new File(filePath);
        if (!file.exists()) {
            Log.w(TAG, "shareImage missing file: " + filePath);
            toast(activity, "Share failed — image file missing.");
            return false;
        }

        String displayName = "DutzShare_"
            + new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date())
            + ".png";

        try {
            Uri uri = createMediaStoreUri(
                activity.getContentResolver(),
                file,
                displayName,
                Environment.DIRECTORY_PICTURES);
            if (uri == null) {
                Log.w(TAG, "shareImage MediaStore insert returned null");
                toast(activity, "Share failed — could not prepare image.");
                return false;
            }

            Intent send = new Intent(Intent.ACTION_SEND);
            send.setType("image/png");
            send.putExtra(Intent.EXTRA_STREAM, uri);
            send.putExtra(Intent.EXTRA_TEXT, text != null ? text : "");
            send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            send.setClipData(ClipData.newRawUri("image/png", uri));
            send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);

            Intent chooser = Intent.createChooser(send, "Share victory selfie");
            runOnUiThread(activity, () -> activity.startActivity(chooser));
            return true;
        } catch (Exception ex) {
            Log.e(TAG, "shareImage failed", ex);
            toast(activity, "Share failed — " + ex.getMessage());
            return false;
        }
    }

    public static boolean saveImageToGallery(Activity activity, String filePath) {
        if (activity == null || filePath == null)
            return false;

        File source = new File(filePath);
        if (!source.exists()) {
            Log.w(TAG, "saveImageToGallery missing file: " + filePath);
            toast(activity, "Download failed — image file missing.");
            return false;
        }

        String displayName = "DutzIsFree_"
            + new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date())
            + ".png";

        try {
            boolean saved;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                Uri uri = createMediaStoreUri(
                    activity.getContentResolver(),
                    source,
                    displayName,
                    Environment.DIRECTORY_DOWNLOADS);
                saved = uri != null;
            } else {
                saved = saveLegacy(activity, source, displayName);
            }

            if (saved) {
                toast(activity, "Saved to Download/" + ALBUM);
                return true;
            }

            toast(activity, "Download failed — storage permission or path blocked.");
            return false;
        } catch (Exception ex) {
            Log.e(TAG, "saveImageToGallery failed", ex);
            toast(activity, "Download failed — " + ex.getMessage());
            return false;
        }
    }

    static Uri createMediaStoreUri(
        ContentResolver resolver,
        File source,
        String displayName,
        String directory)
        throws IOException {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            ContentValues values = new ContentValues();
            values.put(MediaStore.MediaColumns.DISPLAY_NAME, displayName);
            values.put(MediaStore.MediaColumns.MIME_TYPE, "image/png");
            values.put(MediaStore.MediaColumns.RELATIVE_PATH, directory + "/" + ALBUM);
            values.put(MediaStore.MediaColumns.IS_PENDING, 1);

            Uri collection;
            if (Environment.DIRECTORY_DOWNLOADS.equals(directory)
                && Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                collection = MediaStore.Downloads.EXTERNAL_CONTENT_URI;
            } else {
                collection = MediaStore.Images.Media.getContentUri(MediaStore.VOLUME_EXTERNAL_PRIMARY);
            }

            Uri itemUri = resolver.insert(collection, values);
            if (itemUri == null)
                return null;

            OutputStream out = resolver.openOutputStream(itemUri);
            if (out == null)
                return null;

            copyFile(source, out);

            values.clear();
            values.put(MediaStore.MediaColumns.IS_PENDING, 0);
            resolver.update(itemUri, values, null, null);
            return itemUri;
        }

        ContentValues values = new ContentValues();
        values.put(MediaStore.Images.Media.DISPLAY_NAME, displayName);
        values.put(MediaStore.Images.Media.MIME_TYPE, "image/png");
        Uri itemUri = resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
        if (itemUri == null)
            return null;

        OutputStream out = resolver.openOutputStream(itemUri);
        if (out == null)
            return null;

        copyFile(source, out);
        return itemUri;
    }

    static boolean saveLegacy(Activity activity, File source, String displayName) throws IOException {
        File dir = new File(
            Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS),
            ALBUM);
        if (!dir.exists() && !dir.mkdirs())
            return false;

        File dest = new File(dir, displayName);
        copyFile(source, new FileOutputStream(dest));

        MediaScannerConnection.scanFile(
            activity,
            new String[] { dest.getAbsolutePath() },
            new String[] { "image/png" },
            null);
        return true;
    }

    static void copyFile(File source, OutputStream out) throws IOException {
        if (out == null)
            throw new IOException("No output stream");

        InputStream in = null;
        try {
            in = new FileInputStream(source);
            byte[] buffer = new byte[8192];
            int read;
            while ((read = in.read(buffer)) != -1)
                out.write(buffer, 0, read);
            out.flush();
        } finally {
            if (in != null)
                in.close();
            out.close();
        }
    }

    static void runOnUiThread(Activity activity, Runnable action) {
        if (activity == null || action == null)
            return;
        activity.runOnUiThread(action);
    }

    static void toast(Activity activity, String message) {
        if (activity == null || message == null)
            return;
        runOnUiThread(activity, () ->
            Toast.makeText(activity.getApplicationContext(), message, Toast.LENGTH_LONG).show());
    }
}
