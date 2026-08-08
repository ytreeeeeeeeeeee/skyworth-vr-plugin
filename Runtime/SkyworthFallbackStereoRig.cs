using UnityEngine;
using UnityEngine.XR;

public sealed class SkyworthFallbackStereoRig : MonoBehaviour
{
    private const float EyeSeparationMeters = 0.064f;
    private Transform head;
    private Quaternion gyroReference;
    private bool gyroReady;
    private AndroidJavaClass headTrackerClass;
    private bool androidHeadTrackerReady;
    private float nextHeadTrackerLogTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var go = new GameObject("Skyworth Fallback Stereo Rig");
        DontDestroyOnLoad(go);
        go.AddComponent<SkyworthFallbackStereoRig>();
    }

    private void Start()
    {
        if (XRSettings.enabled)
        {
            Debug.Log("SKYWORTH_FALLBACK Stereo disabled because XRSettings.enabled is true.");
            enabled = false;
            return;
        }

        var source = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (source == null)
        {
            Debug.LogWarning("SKYWORTH_FALLBACK No camera found for stereo fallback.");
            enabled = false;
            return;
        }

        head = new GameObject("Skyworth Fallback Head").transform;
        head.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        DontDestroyOnLoad(head.gameObject);

        CreateEye(source, "Left", -EyeSeparationMeters * 0.5f, new Rect(0f, 0f, 0.5f, 1f));
        CreateEye(source, "Right", EyeSeparationMeters * 0.5f, new Rect(0.5f, 0f, 0.5f, 1f));

        source.enabled = false;
        Input.gyro.enabled = true;
        androidHeadTrackerReady = StartAndroidHeadTracker();
        Debug.Log("SKYWORTH_FALLBACK Stereo fallback active: two cameras, side-by-side, gyro=" + SystemInfo.supportsGyroscope + " androidHeadTracker=" + androidHeadTrackerReady);
    }

    private void Update()
    {
        if (head == null)
        {
            return;
        }

        if (androidHeadTrackerReady && TryGetAndroidHeadRotation(out var androidRotation))
        {
            ApplyHeadRotation(androidRotation, "android");
            return;
        }

        if (!SystemInfo.supportsGyroscope)
        {
            return;
        }

        ApplyHeadRotation(GyroToUnity(Input.gyro.attitude), "gyro");
    }

    private void ApplyHeadRotation(Quaternion attitude, string source)
    {
        if (source == "android")
        {
            // TODO: This intentionally keeps the official Skyworth pose absolute.
            // If product behavior should match common VR apps, restore recenter-on-start here.
            head.localRotation = attitude;

            if (Time.unscaledTime >= nextHeadTrackerLogTime)
            {
                nextHeadTrackerLogTime = Time.unscaledTime + 3f;
                Debug.Log("SKYWORTH_HEADTRACK source=" + source + " absoluteRotation=" + head.localRotation.eulerAngles);
            }

            return;
        }

        if (!gyroReady)
        {
            gyroReference = Quaternion.Inverse(attitude);
            gyroReady = true;
        }

        head.localRotation = gyroReference * attitude;

        if (Time.unscaledTime >= nextHeadTrackerLogTime)
        {
            nextHeadTrackerLogTime = Time.unscaledTime + 3f;
            Debug.Log("SKYWORTH_HEADTRACK source=" + source + " rotation=" + head.localRotation.eulerAngles);
        }
    }

    private void CreateEye(Camera source, string eyeName, float xOffset, Rect rect)
    {
        var eye = Instantiate(source, head);
        eye.name = "Skyworth Fallback " + eyeName + " Eye";
        eye.transform.localPosition = new Vector3(xOffset, 0f, 0f);
        eye.transform.localRotation = Quaternion.identity;
        eye.rect = rect;
        eye.stereoTargetEye = StereoTargetEyeMask.None;
        eye.enabled = true;

        var listener = eye.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = eyeName == "Left";
        }
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    private bool StartAndroidHeadTracker()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            headTrackerClass = new AndroidJavaClass("com.local.skyworth.testvr2021.SkyworthHeadTracker");
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                return headTrackerClass.CallStatic<bool>("start", activity);
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("SKYWORTH_HEADTRACK Android tracker unavailable: " + exception.Message);
        }
#endif
        return false;
    }

    private bool TryGetAndroidHeadRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var values = headTrackerClass.CallStatic<float[]>("getQuaternion");
            if (values == null || values.Length < 5 || values[4] < 0.5f)
            {
                return false;
            }

            rotation = new Quaternion(values[0], values[1], values[2], values[3]);
            return true;
        }
        catch (System.Exception exception)
        {
            androidHeadTrackerReady = false;
            Debug.LogWarning("SKYWORTH_HEADTRACK Android tracker failed: " + exception.Message);
        }
#endif
        return false;
    }
}
