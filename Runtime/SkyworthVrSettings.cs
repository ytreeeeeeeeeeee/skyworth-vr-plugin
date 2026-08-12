using UnityEngine;

[CreateAssetMenu(fileName = ResourceName, menuName = "Skyworth VR/Settings")]
public sealed class SkyworthVrSettings : ScriptableObject
{
    public const string ResourceName = "SkyworthVrSettings";

    private const int DefaultScreenWidth = 1360;
    private const int DefaultScreenHeight = 765;
    private const int DefaultRefreshRate = 72;
    private const int DefaultTargetFrameRate = 72;

    [SerializeField]
    private int screenWidth = DefaultScreenWidth;

    [SerializeField]
    private int screenHeight = DefaultScreenHeight;

    [SerializeField]
    private int refreshRate = DefaultRefreshRate;

    [SerializeField]
    private int targetFrameRate = DefaultTargetFrameRate;

    public int ScreenWidth => Mathf.Max(1, screenWidth);

    public int ScreenHeight => Mathf.Max(1, screenHeight);

    public int RefreshRate => Mathf.Max(1, refreshRate);

    public int TargetFrameRate => Mathf.Max(1, targetFrameRate);

    public static SkyworthVrSettings Load()
    {
        var settings = Resources.Load<SkyworthVrSettings>(ResourceName);
        if (settings != null)
        {
            return settings;
        }

        var defaults = CreateInstance<SkyworthVrSettings>();
        defaults.hideFlags = HideFlags.HideAndDontSave;
        return defaults;
    }
}
