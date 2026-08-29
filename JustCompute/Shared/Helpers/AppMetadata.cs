namespace JustCompute.Shared.Helpers;

/// <summary>
/// What the app calls itself and which build this is — surfaced in the flyout header so a user
/// reporting a problem can read the version off the screen rather than hunting for it in system
/// settings. Static so XAML can bind it with <c>{x:Static}</c>, which the flyout header needs:
/// its DataTemplate has no BindingContext of its own.
/// </summary>
public static class AppMetadata
{
    /// <summary>The display name, from the platform manifest rather than a duplicated constant.</summary>
    public static string Name => AppInfo.Current.Name;

    /// <summary>
    /// Display version with the build number beside it, e.g. <c>1.0.0 (2)</c>. Both matter: two
    /// builds of the same release differ only by the second number, which is exactly what a bug
    /// report needs to pin down.
    /// </summary>
    public static string Version => $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})";
}
