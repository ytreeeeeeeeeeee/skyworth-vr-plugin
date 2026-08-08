package com.ssnwt.sdk;

import android.app.Activity;
import android.content.ComponentName;
import android.provider.Settings;
import android.service.vr.VrListenerService;
import android.util.Log;

public final class VRAPI {
    private static final String TAG = "SkyworthSsnwt";
    private static Activity activity;

    private VRAPI() {
    }

    public static boolean initAtw(Activity targetActivity, int mode) {
        activity = targetActivity;
        enableVrSetting();

        if (!JniUtiles.isLoaded()) {
            return false;
        }

        try {
            int result = JniUtiles.nativeAtwInit(mode);
            Log.i(TAG, "nativeAtwInit(" + mode + ")=" + result);
            return result == 0;
        } catch (Throwable t) {
            Log.e(TAG, "nativeAtwInit failed", t);
            return false;
        }
    }

    public static void setVrMode(Activity targetActivity, boolean enabled) {
        ComponentName listener = new ComponentName(targetActivity.getPackageName(), "com.ssnwt.sdk.VrListener");
        try {
            boolean listenerEnabled = VrListenerService.isVrModePackageEnabled(targetActivity, listener);
            Log.i(TAG, "setVrMode " + enabled + ", listenerEnabled=" + listenerEnabled);
            targetActivity.setVrModeEnabled(enabled, listener);
        } catch (Throwable t) {
            Log.e(TAG, "setVrMode failed", t);
        }
    }

    public static boolean enableDevice() {
        if (!JniUtiles.isLoaded()) {
            return false;
        }

        try {
            int result = JniUtiles.nativeEnableDevice();
            Log.i(TAG, "nativeEnableDevice=" + result);
            return result == 0;
        } catch (Throwable t) {
            Log.e(TAG, "nativeEnableDevice failed", t);
            return false;
        }
    }

    public static void disableDevice() {
        if (!JniUtiles.isLoaded()) {
            return;
        }

        try {
            int result = JniUtiles.nativeDisableDevice();
            Log.i(TAG, "nativeDisableDevice=" + result);
        } catch (Throwable t) {
            Log.e(TAG, "nativeDisableDevice failed", t);
        }
    }

    private static void enableVrSetting() {
        if (activity == null) {
            return;
        }

        ComponentName listener = new ComponentName(activity.getPackageName(), "com.ssnwt.sdk.VrListener");
        String flattened = listener.flattenToString();
        try {
            String existing = Settings.Secure.getString(activity.getContentResolver(), "enabled_vr_listeners");
            if (existing == null || existing.length() == 0 || "null".equals(existing)) {
                Settings.Secure.putString(activity.getContentResolver(), "enabled_vr_listeners", flattened);
            } else if (!existing.contains(flattened) && !existing.contains(listener.flattenToShortString())) {
                Settings.Secure.putString(activity.getContentResolver(), "enabled_vr_listeners", existing + ":" + flattened);
            }
        } catch (Throwable t) {
            Log.e(TAG, "enableVrSetting failed", t);
        }
    }
}
