using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;

namespace JustCompute.Handlers.IndicatedTabs;

/// <summary>
/// The Shell, with the tab bar iOS itself expects: the system's bar, the selected tab in the
/// accent colour and the others in the text colour.
///
/// The Shell's tab colours are chosen for Android's bar, which is painted in the header colour,
/// so they are white. Since iOS 26 the tab bar is glass and keeps none of that colour, and white
/// on it all but disappeared - the selected tab was the hardest one to read. The glass bar
/// already marks the selected tab with a platter of its own, so what is left to do here is give
/// both states colours that read on the system's background, light or dark: the palette's
/// accent, which every palette keeps at 4.5:1 on its page, and its text colour. Older iOS gets
/// the same, so the bar looks like one thing across versions.
/// </summary>
public class IndicatedTabsShellRenderer : ShellRenderer
{
    protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker() =>
        new SystemTabBarAppearanceTracker();

    private sealed class SystemTabBarAppearanceTracker : SafeShellTabBarAppearanceTracker
    {
        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);

            if (Theme("ThemeAccentStrong") is not { } accent || Theme("ThemeOnBackground") is not { } ink)
            {
                return;
            }

            var look = new UITabBarAppearance();
            look.ConfigureWithDefaultBackground();
            foreach (var layout in new[] { look.StackedLayoutAppearance, look.InlineLayoutAppearance, look.CompactInlineLayoutAppearance })
            {
                layout.Normal.IconColor = ink;
                layout.Normal.TitleTextAttributes = new UIStringAttributes { ForegroundColor = ink };
                layout.Selected.IconColor = accent;
                layout.Selected.TitleTextAttributes = new UIStringAttributes { ForegroundColor = accent };
            }

            var bar = controller.TabBar;
            bar.StandardAppearance = look;
            bar.ScrollEdgeAppearance = look;
            bar.TintColor = accent;
            bar.UnselectedItemTintColor = ink;
        }

        /// <summary>
        /// A palette colour as the page currently has it. Read here rather than bound, because
        /// UIKit takes plain colours; the Shell re-applies its appearance when the palette
        /// changes, since its tab bar background is one of the palette's colours.
        /// </summary>
        private static UIColor? Theme(string key) =>
            Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
                ? color.ToPlatform()
                : null;
    }
}
