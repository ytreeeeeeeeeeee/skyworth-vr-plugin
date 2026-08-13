using System;
using UnityEngine;

public static class SkyworthVrRig
{
    private static Action<SkyworthEye, Camera> eyeCameraCreated;
    private static Action rigRebuilt;

    public static event Action<SkyworthEye, Camera> EyeCameraCreated
    {
        add
        {
            eyeCameraCreated += value;
            InvokeExistingEyeCameras(value);
        }
        remove
        {
            eyeCameraCreated -= value;
        }
    }

    public static event Action RigRebuilt
    {
        add
        {
            rigRebuilt += value;

            if (LeftEyeCamera != null && RightEyeCamera != null)
            {
                value?.Invoke();
            }
        }
        remove
        {
            rigRebuilt -= value;
        }
    }

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

        eyeCameraCreated?.Invoke(eye, camera);

        if (LeftEyeCamera != null && RightEyeCamera != null)
        {
            rigRebuilt?.Invoke();
        }
    }

    internal static void ClearEyeCameras()
    {
        LeftEyeCamera = null;
        RightEyeCamera = null;
    }

    private static void InvokeExistingEyeCameras(Action<SkyworthEye, Camera> callback)
    {
        if (callback == null)
        {
            return;
        }

        if (LeftEyeCamera != null)
        {
            callback.Invoke(SkyworthEye.Left, LeftEyeCamera);
        }

        if (RightEyeCamera != null)
        {
            callback.Invoke(SkyworthEye.Right, RightEyeCamera);
        }
    }
}
