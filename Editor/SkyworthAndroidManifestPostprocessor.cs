using System.IO;
using System.Xml;
using UnityEditor.Android;

public sealed class SkyworthAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string UnityActivityClass = "com.unity3d.player.UnityPlayerActivity";
    private const string SkyworthActivityClass = "com.ytreeeeeeeeeeee.skyworthvrplugin.SkyworthUnityActivity";
    private const string MainAction = "android.intent.action.MAIN";
    private const string LauncherCategory = "android.intent.category.LAUNCHER";

    public int callbackOrder => 10000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        UpdateManifest(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
    }

    private static void UpdateManifest(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var document = new XmlDocument { PreserveWhitespace = true };
        document.Load(manifestPath);

        var application = document.SelectSingleNode("/manifest/application") as XmlElement;
        if (application == null)
        {
            return;
        }

        var changed = RemoveUnityLauncherIntent(document);
        changed |= EnsureSkyworthLauncherActivity(document, application);

        if (changed)
        {
            document.Save(manifestPath);
        }
    }

    private static bool RemoveUnityLauncherIntent(XmlDocument document)
    {
        var changed = false;
        var activities = document.SelectNodes("/manifest/application/activity");
        if (activities == null)
        {
            return false;
        }

        foreach (XmlElement activity in activities)
        {
            if (activity.GetAttribute("name", AndroidNamespace) != UnityActivityClass)
            {
                continue;
            }

            var filters = activity.SelectNodes("intent-filter");
            if (filters == null)
            {
                continue;
            }

            foreach (XmlElement filter in filters)
            {
                if (!ContainsNamedChild(filter, "action", MainAction) ||
                    !ContainsNamedChild(filter, "category", LauncherCategory))
                {
                    continue;
                }

                activity.RemoveChild(filter);
                changed = true;
            }
        }

        return changed;
    }

    private static bool EnsureSkyworthLauncherActivity(XmlDocument document, XmlElement application)
    {
        var activity = FindActivity(document, SkyworthActivityClass);
        var changed = false;

        if (activity == null)
        {
            activity = document.CreateElement("activity");
            application.AppendChild(activity);
            SetAndroidAttribute(document, activity, "name", SkyworthActivityClass);
            SetAndroidAttribute(document, activity, "theme", "@android:style/Theme.Black.NoTitleBar.Fullscreen");
            SetAndroidAttribute(document, activity, "screenOrientation", "portrait");
            SetAndroidAttribute(document, activity, "launchMode", "singleTask");
            SetAndroidAttribute(document, activity, "configChanges", "keyboard|keyboardHidden|navigation|orientation|screenLayout|screenSize|uiMode|mcc|mnc|locale|touchscreen|smallestScreenSize|layoutDirection|fontScale|density");
            SetAndroidAttribute(document, activity, "resizeableActivity", "false");
            SetAndroidAttribute(document, activity, "exported", "true");
            SetAndroidAttribute(document, activity, "enableVrMode", "${applicationId}/com.ssnwt.sdk.VrListener");
            changed = true;
        }

        if (!HasLauncherIntent(activity))
        {
            var filter = document.CreateElement("intent-filter");

            var action = document.CreateElement("action");
            SetAndroidAttribute(document, action, "name", MainAction);
            filter.AppendChild(action);

            var category = document.CreateElement("category");
            SetAndroidAttribute(document, category, "name", LauncherCategory);
            filter.AppendChild(category);

            activity.AppendChild(filter);
            changed = true;
        }

        if (!HasUnityActivityMetadata(activity))
        {
            var metadata = document.CreateElement("meta-data");
            SetAndroidAttribute(document, metadata, "name", "unityplayer.UnityActivity");
            SetAndroidAttribute(document, metadata, "value", "true");
            activity.AppendChild(metadata);
            changed = true;
        }

        return changed;
    }

    private static XmlElement FindActivity(XmlDocument document, string activityName)
    {
        var activities = document.SelectNodes("/manifest/application/activity");
        if (activities == null)
        {
            return null;
        }

        foreach (XmlElement activity in activities)
        {
            if (activity.GetAttribute("name", AndroidNamespace) == activityName)
            {
                return activity;
            }
        }

        return null;
    }

    private static bool HasLauncherIntent(XmlElement activity)
    {
        var filters = activity.SelectNodes("intent-filter");
        if (filters == null)
        {
            return false;
        }

        foreach (XmlElement filter in filters)
        {
            if (ContainsNamedChild(filter, "action", MainAction) &&
                ContainsNamedChild(filter, "category", LauncherCategory))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnityActivityMetadata(XmlElement activity)
    {
        var metadataNodes = activity.SelectNodes("meta-data");
        if (metadataNodes == null)
        {
            return false;
        }

        foreach (XmlElement metadata in metadataNodes)
        {
            if (metadata.GetAttribute("name", AndroidNamespace) == "unityplayer.UnityActivity")
            {
                return true;
            }
        }

        return false;
    }

    private static void SetAndroidAttribute(XmlDocument document, XmlElement element, string name, string value)
    {
        var attribute = document.CreateAttribute("android", name, AndroidNamespace);
        attribute.Value = value;
        element.Attributes.Append(attribute);
    }

    private static bool ContainsNamedChild(XmlElement parent, string childName, string androidName)
    {
        var children = parent.GetElementsByTagName(childName);
        foreach (XmlElement child in children)
        {
            if (child.GetAttribute("name", AndroidNamespace) == androidName)
            {
                return true;
            }
        }

        return false;
    }
}
