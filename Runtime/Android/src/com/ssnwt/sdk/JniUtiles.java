package com.ssnwt.sdk;

import android.util.Log;

public final class JniUtiles {
    private static final String TAG = "SkyworthSsnwt";
    private static boolean loaded;

    static {
        try {
            System.loadLibrary("avr_api");
            loaded = true;
        } catch (Throwable t) {
            loaded = false;
            Log.e(TAG, "Failed to load libavr_api", t);
        }
    }

    private JniUtiles() {
    }

    public static boolean isLoaded() {
        return loaded;
    }

    public static native int nativeAtwInit(int mode);
    public static native int nativeEnableDevice();
    public static native int nativeDisableDevice();
}
