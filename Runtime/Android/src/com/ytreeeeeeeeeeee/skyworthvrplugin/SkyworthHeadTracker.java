package com.ytreeeeeeeeeeee.skyworthvrplugin;

import android.app.Activity;
import android.util.Log;

public final class SkyworthHeadTracker {
    private static final String TAG = "SkyworthHeadTracker";
    private static boolean librariesLoaded;
    private static boolean started;

    static {
        try {
            System.loadLibrary("atw_api");
            System.loadLibrary("skyworth_headtrack");
            librariesLoaded = true;
            Log.i(TAG, "official head tracker bridge libraries loaded");
        } catch (Throwable t) {
            librariesLoaded = false;
            Log.e(TAG, "failed to load official head tracker bridge", t);
        }
    }

    private SkyworthHeadTracker() {
    }

    public static synchronized boolean start(Activity activity) {
        if (!librariesLoaded) {
            return false;
        }

        if (started) {
            return true;
        }

        started = nativeStart();
        Log.i(TAG, "official orientation polling started=" + started);
        return started;
    }

    public static synchronized void stop() {
        if (!librariesLoaded || !started) {
            return;
        }

        nativeStop();
        started = false;
        Log.i(TAG, "official awStopHeadTracker");
    }

    public static synchronized void recenter() {
        if (librariesLoaded) {
            nativeRecenter();
        }
    }

    public static float[] getQuaternion() {
        if (!librariesLoaded || !started) {
            return new float[] { 0f, 0f, 0f, 1f, 0f };
        }

        return nativeGetQuaternion();
    }

    private static native boolean nativeStart();
    private static native void nativeStop();
    private static native void nativeRecenter();
    private static native float[] nativeGetQuaternion();
}
