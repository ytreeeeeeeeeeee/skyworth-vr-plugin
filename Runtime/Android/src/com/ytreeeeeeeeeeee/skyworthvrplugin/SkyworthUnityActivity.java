package com.ytreeeeeeeeeeee.skyworthvrplugin;

import android.os.Bundle;
import android.os.PowerManager;
import android.util.Log;
import android.app.Activity;
import android.content.Intent;
import android.content.res.Configuration;
import android.view.InputEvent;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.View;
import android.view.WindowManager;

import com.ssnwt.sdk.VRAPI;
import com.unity3d.player.UnityPlayer;

public final class SkyworthUnityActivity extends Activity {
    private static final String TAG = "SkyworthSsnwt";
    private UnityPlayer unityPlayer;
    private PowerManager.WakeLock wakeLock;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        UnityPlayer.currentActivity = this;
        keepDisplayAwake();
        VRAPI.setVrMode(this, true);
        VRAPI.enableDevice();
        unityPlayer = new UnityPlayer(this);
        setContentView(unityPlayer);
        unityPlayer.requestFocus();
    }

    @Override
    protected void onResume() {
        super.onResume();
        keepDisplayAwake();
        if (unityPlayer != null) {
            unityPlayer.onResume();
            unityPlayer.windowFocusChanged(true);
        }
        VRAPI.setVrMode(this, true);
        VRAPI.enableDevice();
    }

    @Override
    protected void onPause() {
        if (unityPlayer != null) {
            unityPlayer.onPause();
        }
        super.onPause();
    }

    @Override
    protected void onStart() {
        super.onStart();
        if (unityPlayer != null) {
            unityPlayer.onStart();
        }
    }

    @Override
    protected void onStop() {
        if (unityPlayer != null) {
            unityPlayer.onStop();
        }
        super.onStop();
    }

    @Override
    protected void onDestroy() {
        releaseWakeLock();
        VRAPI.disableDevice();
        VRAPI.setVrMode(this, false);
        if (unityPlayer != null) {
            unityPlayer.destroy();
        }
        super.onDestroy();
    }

    private void keepDisplayAwake() {
        getWindow().addFlags(
            WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON |
            WindowManager.LayoutParams.FLAG_TURN_SCREEN_ON |
            WindowManager.LayoutParams.FLAG_DISMISS_KEYGUARD |
            WindowManager.LayoutParams.FLAG_SHOW_WHEN_LOCKED);

        View decorView = getWindow().getDecorView();
        decorView.setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_FULLSCREEN |
            View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY |
            View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN |
            View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION |
            View.SYSTEM_UI_FLAG_LAYOUT_STABLE);

        try {
            if (wakeLock == null) {
                PowerManager powerManager = (PowerManager)getSystemService(POWER_SERVICE);
                wakeLock = powerManager.newWakeLock(
                    PowerManager.SCREEN_BRIGHT_WAKE_LOCK |
                    PowerManager.ACQUIRE_CAUSES_WAKEUP |
                    PowerManager.ON_AFTER_RELEASE,
                    "SkyworthVrPlugin:VrDisplay");
                wakeLock.setReferenceCounted(false);
            }
            if (!wakeLock.isHeld()) {
                wakeLock.acquire();
            }
        } catch (Throwable t) {
            Log.e(TAG, "WakeLock acquire failed", t);
        }
    }

    private void releaseWakeLock() {
        try {
            if (wakeLock != null && wakeLock.isHeld()) {
                wakeLock.release();
            }
        } catch (Throwable t) {
            Log.e(TAG, "WakeLock release failed", t);
        }
    }

    @Override
    protected void onNewIntent(Intent intent) {
        setIntent(intent);
        if (unityPlayer != null) {
            unityPlayer.newIntent(intent);
        }
        super.onNewIntent(intent);
    }

    @Override
    public void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        if (unityPlayer != null) {
            unityPlayer.configurationChanged(newConfig);
        }
    }

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (unityPlayer != null) {
            unityPlayer.windowFocusChanged(hasFocus);
        }
    }

    @Override
    public void onLowMemory() {
        super.onLowMemory();
        if (unityPlayer != null) {
            unityPlayer.lowMemory();
        }
    }

    @Override
    public boolean dispatchKeyEvent(KeyEvent event) {
        if (event.getAction() == KeyEvent.ACTION_MULTIPLE && unityPlayer != null) {
            return unityPlayer.injectEvent(event);
        }
        return super.dispatchKeyEvent(event);
    }

    @Override
    public boolean onKeyUp(int keyCode, KeyEvent event) {
        return unityPlayer != null && unityPlayer.injectEvent(event);
    }

    @Override
    public boolean onKeyDown(int keyCode, KeyEvent event) {
        return unityPlayer != null && unityPlayer.injectEvent(event);
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        return unityPlayer != null && unityPlayer.injectEvent(event);
    }

    @Override
    public boolean onGenericMotionEvent(MotionEvent event) {
        return unityPlayer != null && unityPlayer.injectEvent(event);
    }
}
