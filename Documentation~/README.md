# Skyworth VR Bootstrap

Reusable bootstrap layer for Skyworth S801-like Android VR devices.

This package does not patch the official Skyworth SDK. It uses the official native
libraries as inputs:

- `libatw_api.so`
- `libavr_api.so`

Rendering stays on Unity's normal Android `SurfaceView`. The full Skyworth XR
display/ATW path is intentionally not enabled because it produced a black screen
on the tested `SkyworthVR-S801` device.

## What It Provides

- Android Activity configured for the Skyworth VR shell.
- VR listener service registration.
- Gradle/manifest postprocessor for Android builds.
- Side-by-side stereo fallback cameras.
- Head orientation from official Skyworth `awGetOrientation(double, float*)`.
- Fallback to Unity `Input.gyro` only if the official orientation bridge is not available.

## Required Project Settings

- Unity: tested with `2021.3.45f2`.
- Android target architecture: `ARMv7`.
- Graphics API: `OpenGLES3`.
- Disable multithreaded rendering / graphics jobs for Android.
- Do not enable the Skyworth XR Loader unless you are specifically debugging the
  official display subsystem.

## Important Behavior

For the official Skyworth orientation path, the current implementation keeps the
pose absolute. It does not recenter on startup.

See `SkyworthFallbackStereoRig.cs` near the `source == "android"` branch. There is
a TODO marking the place where product-style recenter-on-start can be restored.

## Importing Into Another Project

1. Add this package through Unity Package Manager.
   - Local path: `Packages/com.local.skyworth-vr-bootstrap`
   - Git URL after publication: use the repository URL for this package.
2. Add your own scene and normal `MainCamera`.
3. Build for Android.
4. Install on the Skyworth device.
5. Verify logcat contains:
   - `official orientation polling started=true`
   - `first official orientation ... magnitude=1`
   - `SKYWORTH_HEADTRACK source=android`

The Java Activity class currently lives at
`com.local.skyworth.testvr2021.SkyworthUnityActivity`. This package name does not
need to match your Android application id. The manifest postprocessor writes the
actual application id into `android:enableVrMode`.
