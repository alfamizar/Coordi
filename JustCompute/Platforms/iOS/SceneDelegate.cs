using Foundation;

namespace JustCompute.Platforms.iOS;

/// <summary>
/// Hosts the app's window in a UIScene. The iOS 27 SDK refuses to launch an app that still
/// relies on the app delegate's own window: it is killed within a second of starting, before
/// anything is drawn. MAUI does the work in <see cref="MauiUISceneDelegate"/>; this class only
/// exists so Info.plist has a name to point at.
/// </summary>
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
}
