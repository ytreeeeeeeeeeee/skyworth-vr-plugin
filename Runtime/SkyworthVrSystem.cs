using System;
using UnityEngine;

public static class SkyworthVrSystem
{
    public static void ExitToLauncher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var system = new AndroidJavaClass("com.local.skyworth.testvr2021.SkyworthSystem"))
            {
                if (system.CallStatic<bool>("goHome", activity))
                {
                    return;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("SKYWORTH_SYSTEM ExitToLauncher failed: " + exception);
        }
#endif

        Application.Quit();
    }
}
