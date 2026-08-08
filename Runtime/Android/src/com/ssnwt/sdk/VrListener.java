package com.ssnwt.sdk;

import android.content.ComponentName;
import android.service.vr.VrListenerService;
import android.util.Log;

public final class VrListener extends VrListenerService {
    private static final String TAG = "SkyworthSsnwt";

    @Override
    public void onCurrentVrActivityChanged(ComponentName component) {
        super.onCurrentVrActivityChanged(component);
        Log.i(TAG, "VrListener activity=" + component);
    }
}
