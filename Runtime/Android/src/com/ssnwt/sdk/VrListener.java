package com.ssnwt.sdk;

import android.content.ComponentName;
import android.service.vr.VrListenerService;

public final class VrListener extends VrListenerService {
    @Override
    public void onCurrentVrActivityChanged(ComponentName component) {
        super.onCurrentVrActivityChanged(component);
    }
}
