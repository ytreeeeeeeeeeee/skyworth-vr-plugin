package com.ytreeeeeeeeeeee.skyworthvrplugin;

import android.app.Activity;
import android.content.Intent;
import android.util.Log;

public final class SkyworthSystem {
    private static final String TAG = "SkyworthSsnwt";

    private SkyworthSystem() {
    }

    public static boolean goHome(Activity activity) {
        if (activity == null) {
            return false;
        }

        try {
            Intent intent = new Intent(Intent.ACTION_MAIN);
            intent.addCategory(Intent.CATEGORY_HOME);
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            activity.startActivity(intent);
            return true;
        } catch (Throwable t) {
            Log.e(TAG, "Failed to start HOME intent", t);
            return false;
        }
    }
}
