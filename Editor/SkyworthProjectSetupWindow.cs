using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public sealed class SkyworthProjectSetupWindow : EditorWindow
{
    private const string SessionShownKey = "SkyworthVRPlugin.ProjectSetupWindow.Shown";
    private const string DontShowKey = "SkyworthVRPlugin.ProjectSetupWindow.DontShow";

    static SkyworthProjectSetupWindow()
    {
        EditorApplication.delayCall += ShowOnImportIfNeeded;
    }

    [MenuItem("Skyworth VR Plugin/Project Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkyworthProjectSetupWindow>(true, "Skyworth VR Plugin Setup");
        window.minSize = new Vector2(460f, 360f);
        window.Show();
    }

    private static void ShowOnImportIfNeeded()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        if (SessionState.GetBool(SessionShownKey, false) || EditorPrefs.GetBool(DontShowKey, false))
        {
            return;
        }

        SessionState.SetBool(SessionShownKey, true);

        if (!SkyworthProjectSettings.IsRecommended)
        {
            ShowWindow();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Skyworth VR Plugin Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Apply these settings before building for SkyworthVR-S801. The button changes project settings only when you click it.",
            MessageType.Info);

        DrawStatus("Build Target: Android", SkyworthProjectSettings.IsAndroidBuildTarget);
        DrawStatus("Active Input Handling: Both", SkyworthProjectSettings.IsInputHandlingRecommended);
        DrawStatus("Default Orientation: Portrait", SkyworthProjectSettings.IsPortraitOrientation);
        DrawStatus("Android Min SDK: 25", SkyworthProjectSettings.IsMinSdkRecommended);
        DrawStatus("Android Target SDK: Automatic", SkyworthProjectSettings.IsTargetSdkRecommended);
        DrawStatus("Scripting Backend: Mono", SkyworthProjectSettings.IsScriptingBackendRecommended);
        DrawStatus("Target Architecture: ARMv7", SkyworthProjectSettings.IsArchitectureRecommended);
        DrawStatus("Graphics API: OpenGLES3", SkyworthProjectSettings.IsGraphicsApiRecommended);
        DrawStatus("Multithreaded Rendering: Off", SkyworthProjectSettings.IsMobileMtRenderingRecommended);
        DrawStatus("Graphics Jobs: Off", SkyworthProjectSettings.IsGraphicsJobsRecommended);
        DrawStatus("Layer: SkyworthLeftEye", SkyworthProjectSettings.HasLeftEyeLayer);
        DrawStatus("Layer: SkyworthRightEye", SkyworthProjectSettings.HasRightEyeLayer);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(SkyworthProjectSettings.IsRecommended))
        {
            if (GUILayout.Button("Apply Recommended Settings", GUILayout.Height(34f)))
            {
                SkyworthProjectSettings.ApplyRecommended();
            }
        }

        if (SkyworthProjectSettings.IsRecommended)
        {
            EditorGUILayout.HelpBox("Project settings are ready for Skyworth VR builds.", MessageType.Info);
        }

        EditorGUILayout.Space();
        var dontShow = EditorPrefs.GetBool(DontShowKey, false);
        var nextDontShow = EditorGUILayout.ToggleLeft("Do not show this window automatically", dontShow);
        if (nextDontShow != dontShow)
        {
            EditorPrefs.SetBool(DontShowKey, nextDontShow);
        }
    }

    private static void DrawStatus(string label, bool ok)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(ok ? "OK" : "FIX", GUILayout.Width(32f));
            EditorGUILayout.LabelField(label);
        }
    }
}

internal static class SkyworthProjectSettings
{
    private const string LeftEyeLayerName = "SkyworthLeftEye";
    private const string RightEyeLayerName = "SkyworthRightEye";

    public static bool IsAndroidBuildTarget =>
        EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

    public static bool IsInputHandlingRecommended =>
        GetActiveInputHandler() == 2;

    public static bool IsPortraitOrientation =>
        PlayerSettings.defaultInterfaceOrientation == UIOrientation.Portrait;

    public static bool IsMinSdkRecommended =>
        PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel25;

    public static bool IsTargetSdkRecommended =>
        PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto;

    public static bool IsScriptingBackendRecommended =>
        PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) == ScriptingImplementation.Mono2x;

    public static bool IsArchitectureRecommended =>
        PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARMv7;

    public static bool IsGraphicsApiRecommended =>
        !PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
        PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.OpenGLES3 });

    public static bool IsMobileMtRenderingRecommended =>
        GetMobileMtRendering() == false;

    public static bool IsGraphicsJobsRecommended =>
        GetGraphicsJobsForPlatform() != true;

    public static bool HasLeftEyeLayer =>
        HasLayer(LeftEyeLayerName);

    public static bool HasRightEyeLayer =>
        HasLayer(RightEyeLayerName);

    public static bool IsRecommended =>
        IsAndroidBuildTarget &&
        IsInputHandlingRecommended &&
        IsPortraitOrientation &&
        IsMinSdkRecommended &&
        IsTargetSdkRecommended &&
        IsScriptingBackendRecommended &&
        IsArchitectureRecommended &&
        IsGraphicsApiRecommended &&
        IsMobileMtRenderingRecommended &&
        IsGraphicsJobsRecommended &&
        HasLeftEyeLayer &&
        HasRightEyeLayer;

    public static void ApplyRecommended()
    {
        if (!IsAndroidBuildTarget)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        SetActiveInputHandler(2);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        SetGraphicsJobsForPlatform(false);
        SetMobileMtRendering(false);
        EnsureLayer(LeftEyeLayerName);
        EnsureLayer(RightEyeLayerName);

        AssetDatabase.SaveAssets();
        Debug.Log("SKYWORTH_SETUP Applied recommended project settings.");
    }

    private static bool? GetMobileMtRendering()
    {
        var method = typeof(PlayerSettings).GetMethod(
            "GetMobileMTRendering",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(BuildTargetGroup) },
            null);

        if (method == null)
        {
            return null;
        }

        return (bool)method.Invoke(null, new object[] { BuildTargetGroup.Android });
    }

    private static int? GetActiveInputHandler()
    {
        var playerSettings = LoadPlayerSettingsObject();
        if (playerSettings == null)
        {
            return null;
        }

        var serialized = new SerializedObject(playerSettings);
        var property = serialized.FindProperty("activeInputHandler");
        return property?.intValue;
    }

    private static void SetActiveInputHandler(int value)
    {
        var playerSettings = LoadPlayerSettingsObject();
        if (playerSettings == null)
        {
            Debug.LogWarning("SKYWORTH_SETUP ProjectSettings/ProjectSettings.asset was not found.");
            return;
        }

        var serialized = new SerializedObject(playerSettings);
        var property = serialized.FindProperty("activeInputHandler");
        if (property == null)
        {
            Debug.LogWarning("SKYWORTH_SETUP activeInputHandler property was not found.");
            return;
        }

        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UnityEngine.Object LoadPlayerSettingsObject()
    {
        return AssetDatabase
            .LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")
            .FirstOrDefault(asset => asset != null && asset.GetType().Name == "PlayerSettings");
    }

    private static bool HasLayer(string layerName)
    {
        return LayerMask.NameToLayer(layerName) >= 0;
    }

    private static void EnsureLayer(string layerName)
    {
        if (HasLayer(layerName))
        {
            return;
        }

        var tagManager = LoadTagManagerObject();
        if (tagManager == null)
        {
            Debug.LogWarning("SKYWORTH_SETUP ProjectSettings/TagManager.asset was not found.");
            return;
        }

        var serialized = new SerializedObject(tagManager);
        var layers = serialized.FindProperty("layers");
        if (layers == null || !layers.isArray)
        {
            Debug.LogWarning("SKYWORTH_SETUP TagManager layers property was not found.");
            return;
        }

        for (var i = 8; i < layers.arraySize; i++)
        {
            var layer = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(layer.stringValue))
            {
                continue;
            }

            layer.stringValue = layerName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("SKYWORTH_SETUP Added layer " + layerName + " at slot " + i + ".");
            return;
        }

        Debug.LogWarning("SKYWORTH_SETUP Could not add layer " + layerName + ": no free user layer slots.");
    }

    private static UnityEngine.Object LoadTagManagerObject()
    {
        return AssetDatabase
            .LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")
            .FirstOrDefault(asset => asset != null && asset.GetType().Name == "TagManager");
    }

    private static bool? GetGraphicsJobsForPlatform()
    {
        var method = typeof(PlayerSettings).GetMethod(
            "GetGraphicsJobsForPlatform",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(BuildTarget) },
            null);

        if (method == null)
        {
            return null;
        }

        return (bool)method.Invoke(null, new object[] { BuildTarget.Android });
    }

    private static void SetGraphicsJobsForPlatform(bool enabled)
    {
        var method = typeof(PlayerSettings).GetMethod(
            "SetGraphicsJobsForPlatform",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(BuildTarget), typeof(bool) },
            null);

        if (method == null)
        {
            Debug.LogWarning("SKYWORTH_SETUP PlayerSettings.SetGraphicsJobsForPlatform was not found.");
            return;
        }

        method.Invoke(null, new object[] { BuildTarget.Android, enabled });
    }

    private static void SetMobileMtRendering(bool enabled)
    {
        var method = typeof(PlayerSettings).GetMethod(
            "SetMobileMTRendering",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(BuildTargetGroup), typeof(bool) },
            null);

        if (method == null)
        {
            Debug.LogWarning("SKYWORTH_SETUP PlayerSettings.SetMobileMTRendering was not found.");
            return;
        }

        method.Invoke(null, new object[] { BuildTargetGroup.Android, enabled });
    }
}
