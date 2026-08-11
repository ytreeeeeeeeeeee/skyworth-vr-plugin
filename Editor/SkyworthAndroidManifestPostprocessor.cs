using System.IO;
using System.Xml;
using UnityEditor.Android;

public sealed class SkyworthAndroidManifestPostprocessor : IPostGenerateGradleAndroidProject
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string UnityActivityClass = "com.unity3d.player.UnityPlayerActivity";
    private const string MainAction = "android.intent.action.MAIN";
    private const string LauncherCategory = "android.intent.category.LAUNCHER";

    public int callbackOrder => 10000;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        RemoveUnityLauncherIntent(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
    }

    private static void RemoveUnityLauncherIntent(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            return;
        }

        var document = new XmlDocument { PreserveWhitespace = true };
        document.Load(manifestPath);

        var changed = false;
        var activities = document.SelectNodes("/manifest/application/activity");
        if (activities == null)
        {
            return;
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

        if (changed)
        {
            document.Save(manifestPath);
        }
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
