using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Android;
using System.Xml;

public sealed class SkyworthGradleTargetSdkPostprocessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 11000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var gradleRoot = Path.GetFullPath(Path.Combine(path, ".."));
        ForceTargetSdk(Path.Combine(gradleRoot, "launcher", "build.gradle"));
        ForceTargetSdk(Path.Combine(gradleRoot, "unityLibrary", "build.gradle"));
        PatchUnityLibraryManifest(Path.Combine(gradleRoot, "unityLibrary", "src", "main", "AndroidManifest.xml"));
        PatchUnityLibraryManifest(Path.Combine(gradleRoot, "unityLibrary", "xrmanifest.androidlib", "AndroidManifest.xml"));
    }

    private static void ForceTargetSdk(string buildGradlePath)
    {
        if (!File.Exists(buildGradlePath))
        {
            return;
        }

        var text = File.ReadAllText(buildGradlePath);
        text = Regex.Replace(text, @"targetSdkVersion\s+\d+", "targetSdkVersion 25");
        File.WriteAllText(buildGradlePath, text);
    }

    private static void PatchUnityLibraryManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        PatchUnityLibraryManifestText(manifestPath);

        var document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.Load(manifestPath);

        var android = "http://schemas.android.com/apk/res/android";
        var applicationId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        EnsureUsesPermission(document, android, "android.permission.WAKE_LOCK");

        var application = (XmlElement)document.SelectSingleNode("/manifest/application");
        if (application == null)
        {
            return;
        }

        foreach (XmlElement activity in application.GetElementsByTagName("activity"))
        {
            var name = activity.GetAttribute("name", android);
            if (name == "com.unity3d.player.UnityPlayerActivity")
            {
                activity.SetAttribute("name", android, "com.local.skyworth.testvr2021.SkyworthUnityActivity");
                activity.SetAttribute("enableVrMode", android, applicationId + "/com.ssnwt.sdk.VrListener");
                activity.SetAttribute("screenOrientation", android, "portrait");
            }
        }

        EnsureVrListenerService(document, application, android, "com.ssnwt.sdk.VrListener");
        PatchSkyworthActivity(document, application, android, applicationId);
        document.Save(manifestPath);
    }

    private static void PatchUnityLibraryManifestText(string manifestPath)
    {
        var applicationId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        var text = File.ReadAllText(manifestPath);
        text = text.Replace(
            "android:name=\"com.unity3d.player.UnityPlayerActivity\"",
            "android:name=\"com.local.skyworth.testvr2021.SkyworthUnityActivity\"");
        text = text.Replace(
            "android:enableVrMode=\"com.local.skyworth.testvr2021/com.local.skyworth.testvr2021.SkyworthVrListener\"",
            "android:enableVrMode=\"com.local.skyworth.testvr2021/com.ssnwt.sdk.VrListener\"");
        text = Regex.Replace(
            text,
            "android:enableVrMode=\"[^\"]*/com\\.ssnwt\\.sdk\\.VrListener\"",
            "android:enableVrMode=\"" + applicationId + "/com.ssnwt.sdk.VrListener\"");
        text = text.Replace(
            "android:screenOrientation=\"landscape\"",
            "android:screenOrientation=\"portrait\"");

        if (!text.Contains("android:name=\"com.ssnwt.sdk.VrListener\""))
        {
            var service =
                "    <service android:name=\"com.ssnwt.sdk.VrListener\" android:permission=\"android.permission.BIND_VR_LISTENER_SERVICE\">\n" +
                "      <intent-filter>\n" +
                "        <action android:name=\"android.service.vr.VrListenerService\" />\n" +
                "      </intent-filter>\n" +
                "    </service>\n";
            text = text.Replace("</application>", service + "  </application>");
        }

        File.WriteAllText(manifestPath, text);
    }

    private static void EnsureUsesPermission(XmlDocument document, string android, string permissionName)
    {
        var manifest = (XmlElement)document.SelectSingleNode("/manifest");
        if (manifest == null)
        {
            return;
        }

        foreach (XmlElement permission in manifest.GetElementsByTagName("uses-permission"))
        {
            if (permission.GetAttribute("name", android) == permissionName)
            {
                return;
            }
        }

        var newPermission = document.CreateElement("uses-permission");
        newPermission.SetAttribute("name", android, permissionName);
        manifest.InsertBefore(newPermission, manifest.FirstChild);
    }

    private static void PatchSkyworthActivity(XmlDocument document, XmlElement application, string android, string applicationId)
    {
        foreach (XmlElement activity in application.GetElementsByTagName("activity"))
        {
            if (activity.GetAttribute("name", android) == "com.local.skyworth.testvr2021.SkyworthUnityActivity")
            {
                activity.SetAttribute("enableVrMode", android, applicationId + "/com.ssnwt.sdk.VrListener");
                activity.SetAttribute("screenOrientation", android, "portrait");
            }
        }
    }

    private static void EnsureVrListenerService(XmlDocument document, XmlElement application, string android, string className)
    {
        foreach (XmlElement service in application.GetElementsByTagName("service"))
        {
            if (service.GetAttribute("name", android) == className)
            {
                return;
            }
        }

        var newService = document.CreateElement("service");
        newService.SetAttribute("name", android, className);
        newService.SetAttribute("permission", android, "android.permission.BIND_VR_LISTENER_SERVICE");

        var intentFilter = document.CreateElement("intent-filter");
        var action = document.CreateElement("action");
        action.SetAttribute("name", android, "android.service.vr.VrListenerService");
        intentFilter.AppendChild(action);
        newService.AppendChild(intentFilter);
        application.AppendChild(newService);
    }

}
