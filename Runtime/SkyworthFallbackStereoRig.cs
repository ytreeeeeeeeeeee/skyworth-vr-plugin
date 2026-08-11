using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SkyworthFallbackStereoRig : MonoBehaviour
{
    private const bool DiagnosticMonoRender = false;
#if UNITY_EDITOR
    private const float EditorMouseSensitivity = 2.5f;
    private const float EditorMouseMinPitch = -85f;
    private const float EditorMouseMaxPitch = 85f;
#endif
    private static SkyworthFallbackStereoRig instance;

    private Transform head;
    private Camera sourceCamera;
#if UNITY_EDITOR
    private bool editorMouseReady;
    private float editorMouseYaw;
    private float editorMousePitch;
#endif
    private Quaternion gyroReference;
    private bool gyroReady;
    private AndroidJavaClass headTrackerClass;
    private bool androidHeadTrackerReady;
    private float nextHeadTrackerLogTime;
    private int lastPoseUpdateFrame = -1;
    private float nextStatsLogTime;
    private int frameSamples;
    private float frameDeltaSum;
    private float frameDeltaMin = float.MaxValue;
    private float frameDeltaMax;
    private int framesOver20Ms;
    private int framesOver25Ms;
    private int framesOver33Ms;
    private bool previousPoseReady;
    private Quaternion previousPose;
    private int poseSamples;
    private float poseAngleSum;
    private float poseAngleMax;
    private int repeatedPoseSamples;
    private string lastPoseSource = "none";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        var go = new GameObject("Skyworth Fallback Stereo Rig");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkyworthFallbackStereoRig>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ConfigureFramePacing();

        if (IsUnityXrEnabled())
        {
            Debug.Log("SKYWORTH_FALLBACK Stereo disabled because XRSettings.enabled is true.");
            enabled = false;
            return;
        }

        RebuildRigForActiveScene();
        Input.gyro.enabled = true;
        androidHeadTrackerReady = StartAndroidHeadTracker();
        Debug.Log("SKYWORTH_FALLBACK fallback active: monoDiagnostic=" + DiagnosticMonoRender + " gyro=" + SystemInfo.supportsGyroscope + " androidHeadTracker=" + androidHeadTrackerReady);
        Debug.Log("SKYWORTH_DISPLAY resolution=" + Screen.width + "x" + Screen.height + " refreshRate=" + Screen.currentResolution.refreshRate);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!enabled)
        {
            return;
        }

        RebuildRigForActiveScene();
    }

    private void RebuildRigForActiveScene()
    {
        var source = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        if (source == null)
        {
            Debug.LogWarning("SKYWORTH_FALLBACK No camera found for stereo fallback.");
            return;
        }

        DestroyExistingHead();
        sourceCamera = source;
        head = new GameObject("Skyworth Fallback Head").transform;
        AttachHeadToSourceParent(source);
#if UNITY_EDITOR
        editorMouseReady = false;
#endif

        DisableAudioListener(source);

        if (DiagnosticMonoRender)
        {
            CreateEye(source, "Mono", 0f, new Rect(0f, 0f, 1f, 1f));
        }
        else
        {
            CreateEye(source, "Left", -SkyworthVrConstants.EyeSeparationMeters * 0.5f, new Rect(0f, 0f, 0.5f, 1f));
            CreateEye(source, "Right", SkyworthVrConstants.EyeSeparationMeters * 0.5f, new Rect(0.5f, 0f, 0.5f, 1f));
        }

        source.enabled = false;
        Debug.Log("SKYWORTH_FALLBACK rig rebuilt source=" + source.name + " parent=" + (head.parent != null ? head.parent.name : "<scene-root>"));
    }

    private void AttachHeadToSourceParent(Camera source)
    {
        var sourceTransform = source.transform;
        var sourceParent = sourceTransform.parent;
        head.SetParent(sourceParent, false);
        head.localPosition = sourceTransform.localPosition;
        head.localRotation = sourceTransform.localRotation;
        head.localScale = Vector3.one;
    }

    private void DestroyExistingHead()
    {
        if (head == null)
        {
            return;
        }

        Destroy(head.gameObject);
        head = null;
    }

    private static void ConfigureFramePacing()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 72;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.SetResolution(1360, 765, true, 72);
        Debug.Log("SKYWORTH_FRAME_PACING targetFrameRate=" + Application.targetFrameRate + " vSyncCount=" + QualitySettings.vSyncCount + " requestedResolution=1360x765@72");
    }

    private static bool IsUnityXrEnabled()
    {
        var xrSettingsType = System.Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule");
        var enabledProperty = xrSettingsType?.GetProperty("enabled", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (enabledProperty == null)
        {
            return false;
        }

        return (bool)enabledProperty.GetValue(null, null);
    }

    private void Update()
    {
        FollowSourceCameraBasePose();
        RecordFrameStats();
        MaybeLogStats();
    }

    private void FollowSourceCameraBasePose()
    {
        if (head == null || sourceCamera == null)
        {
            return;
        }

        var sourceTransform = sourceCamera.transform;
        if (head.parent != sourceTransform.parent)
        {
            head.SetParent(sourceTransform.parent, false);
        }

        head.localPosition = sourceTransform.localPosition;
    }

    private void UpdateHeadPose()
    {
        if (head == null)
        {
            return;
        }

        if (lastPoseUpdateFrame == Time.frameCount)
        {
            return;
        }

        lastPoseUpdateFrame = Time.frameCount;

#if UNITY_EDITOR
        if (TryGetEditorMouseRotation(out var editorMouseRotation))
        {
            ApplyHeadRotation(editorMouseRotation, "editor-mouse");
            return;
        }
#endif

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
        RecordPoseStats(attitude, source);

#if UNITY_EDITOR
        if (source == "editor-mouse")
        {
            head.localRotation = attitude;

            if (Time.unscaledTime >= nextHeadTrackerLogTime)
            {
                nextHeadTrackerLogTime = Time.unscaledTime + 3f;
                Debug.Log("SKYWORTH_HEADTRACK source=" + source + " rotation=" + head.localRotation.eulerAngles);
            }

            return;
        }
#endif

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

#if UNITY_EDITOR
    private bool TryGetEditorMouseRotation(out Quaternion rotation)
    {
        rotation = Quaternion.identity;

        if (head == null)
        {
            return false;
        }

        if (!editorMouseReady)
        {
            var euler = head.localRotation.eulerAngles;
            editorMouseYaw = NormalizeEditorAngle(euler.y);
            editorMousePitch = NormalizeEditorAngle(euler.x);
            editorMouseReady = true;
        }

        if (Input.GetMouseButton(1))
        {
            editorMouseYaw += Input.GetAxisRaw("Mouse X") * EditorMouseSensitivity;
            editorMousePitch -= Input.GetAxisRaw("Mouse Y") * EditorMouseSensitivity;
            editorMousePitch = Mathf.Clamp(editorMousePitch, EditorMouseMinPitch, EditorMouseMaxPitch);
        }

        rotation = Quaternion.Euler(editorMousePitch, editorMouseYaw, 0f);
        return true;
    }

    private static float NormalizeEditorAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
#endif

    private void RecordFrameStats()
    {
        var delta = Time.unscaledDeltaTime;
        if (delta <= 0f)
        {
            return;
        }

        frameSamples++;
        frameDeltaSum += delta;
        frameDeltaMin = Mathf.Min(frameDeltaMin, delta);
        frameDeltaMax = Mathf.Max(frameDeltaMax, delta);

        if (delta > 0.020f)
        {
            framesOver20Ms++;
        }

        if (delta > 0.025f)
        {
            framesOver25Ms++;
        }

        if (delta > 0.033333f)
        {
            framesOver33Ms++;
        }
    }

    private void RecordPoseStats(Quaternion attitude, string source)
    {
        lastPoseSource = source;
        poseSamples++;

        if (previousPoseReady)
        {
            var angle = Quaternion.Angle(previousPose, attitude);
            poseAngleSum += angle;
            poseAngleMax = Mathf.Max(poseAngleMax, angle);

            if (angle < 0.001f)
            {
                repeatedPoseSamples++;
            }
        }

        previousPose = attitude;
        previousPoseReady = true;
    }

    private void MaybeLogStats()
    {
        if (Time.unscaledTime < nextStatsLogTime)
        {
            return;
        }

        nextStatsLogTime = Time.unscaledTime + 1f;

        if (frameSamples > 0)
        {
            var averageMs = frameDeltaSum * 1000f / frameSamples;
            var minMs = frameDeltaMin * 1000f;
            var maxMs = frameDeltaMax * 1000f;
            var fps = frameSamples / Mathf.Max(frameDeltaSum, 0.0001f);
            Debug.Log(
                "SKYWORTH_FRAME_STATS fps=" + fps.ToString("F1") +
                " avgMs=" + averageMs.ToString("F2") +
                " minMs=" + minMs.ToString("F2") +
                " maxMs=" + maxMs.ToString("F2") +
                " over20=" + framesOver20Ms +
                " over25=" + framesOver25Ms +
                " over33=" + framesOver33Ms);
        }

        if (poseSamples > 0)
        {
            var averagePoseAngle = poseAngleSum / Mathf.Max(poseSamples - 1, 1);
            Debug.Log(
                "SKYWORTH_POSE_STATS source=" + lastPoseSource +
                " samples=" + poseSamples +
                " avgDeg=" + averagePoseAngle.ToString("F3") +
                " maxDeg=" + poseAngleMax.ToString("F3") +
                " repeats=" + repeatedPoseSamples);
        }

        ResetStats();
    }

    private void ResetStats()
    {
        frameSamples = 0;
        frameDeltaSum = 0f;
        frameDeltaMin = float.MaxValue;
        frameDeltaMax = 0f;
        framesOver20Ms = 0;
        framesOver25Ms = 0;
        framesOver33Ms = 0;
        poseSamples = 0;
        poseAngleSum = 0f;
        poseAngleMax = 0f;
        repeatedPoseSamples = 0;
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
        eye.gameObject.AddComponent<EyePoseUpdater>().Initialize(this);

        var listener = eye.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = eyeName == "Left" || eyeName == "Mono";
        }
    }

    private static void DisableAudioListener(Camera source)
    {
        var listener = source.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = false;
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
            headTrackerClass = new AndroidJavaClass("com.ytreeeeeeeeeeee.skyworthvrplugin.SkyworthHeadTracker");
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

    private sealed class EyePoseUpdater : MonoBehaviour
    {
        private SkyworthFallbackStereoRig rig;

        public void Initialize(SkyworthFallbackStereoRig owner)
        {
            rig = owner;
        }

        private void OnPreCull()
        {
            if (rig != null)
            {
                rig.UpdateHeadPose();
            }
        }
    }
}
