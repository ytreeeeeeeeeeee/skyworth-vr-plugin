using System;
using UnityEngine;

public static class SkyworthVrRig
{
    public static event Action<SkyworthEye, Camera> EyeCameraCreated;

    public static event Action RigRebuilt;

    public static Camera LeftEyeCamera { get; private set; }

    public static Camera RightEyeCamera { get; private set; }

    public static bool TryGetEyeCamera(SkyworthEye eye, out Camera camera)
    {
        camera = eye == SkyworthEye.Left ? LeftEyeCamera : RightEyeCamera;
        return camera != null;
    }

    internal static void SetEyeCamera(SkyworthEye eye, Camera camera)
    {
        if (eye == SkyworthEye.Left)
        {
            LeftEyeCamera = camera;
        }
        else
        {
            RightEyeCamera = camera;
        }

        EyeCameraCreated?.Invoke(eye, camera);

        if (LeftEyeCamera != null && RightEyeCamera != null)
        {
            RigRebuilt?.Invoke();
        }
    }

    internal static void ClearEyeCameras()
    {
        LeftEyeCamera = null;
        RightEyeCamera = null;
    }
}
