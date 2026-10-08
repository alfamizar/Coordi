using Android.Content.Res;
using Android.Graphics.Drawables;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;

namespace JustCompute.Handlers.IndicatedTabs;

/// <summary>
/// The Shell, with a tab bar that marks the selected tab with a shape instead of a shade.
///
/// The bar takes the header's colour, and on the light palettes that is a saturated blue or pink
/// where only white text reaches the 4.5:1 a label needs; a dimmer gray for the unselected tabs
/// read at 2.8:1 there. So the unselected tabs are the full label colour, and the selected one is
/// a rounded chip of that colour with its icon and label drawn on it in the bar's own colour,
/// in bold: inverted, rather than brighter. The colours are still the Shell's - TabBarTitleColor
/// for the chip, TabBarBackgroundColor for what is drawn on it, TabBarUnselectedColor for the
/// rest - so the palette decides them as before.
///
/// The chip is the item's background, not Material's active indicator: that indicator is laid
/// out for Material 3's 80 dp bar, and in this 56 dp one it pushed the icon down onto its label.
/// </summary>
public class IndicatedTabsShellRenderer : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem) =>
        new IndicatedTabsAppearanceTracker(this, shellItem);

    private sealed class IndicatedTabsAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
        : ShellBottomNavViewAppearanceTracker(shellContext, shellItem)
    {
        // The chip's inset from the edges of its tab, and its corner, in dp. None at the sides: the
        // selected label is drawn in the bar's colour, so any of it past the chip's edge would
        // vanish, and Material caps a tab at 168 dp, which "Sonnenfinsternisse" at a large text
        // size needs all of. A label never runs past its tab, so a chip the tab's width holds it.
        private const int ChipInsetHorizontalDp = 0;
        private const int ChipInsetVerticalDp = 4;
        private const int ChipCornerDp = 16;

        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);

            var bar = (appearance.EffectiveTabBarBackgroundColor ?? Colors.Black).ToPlatform();
            var ink = appearance.EffectiveTabBarTitleColor.ToPlatform();
            var unselected = appearance.EffectiveTabBarUnselectedColor.ToPlatform();
            var disabled = appearance.EffectiveTabBarDisabledColor.ToPlatform();

            // On the chip the icon and the label take the bar's colour; off it, the label colour.
            var onChip = new ColorStateList(
                [
                    [-global::Android.Resource.Attribute.StateEnabled],
                    [global::Android.Resource.Attribute.StateChecked],
                    [],
                ],
                [disabled.ToArgb(), bar.ToArgb(), unselected.ToArgb()]);
            bottomView.ItemIconTintList = onChip;
            bottomView.ItemTextColor = onChip;
            bottomView.SetItemTextAppearanceActiveBoldEnabled(true);

            var density = bottomView.Resources?.DisplayMetrics?.Density ?? 1f;
            bottomView.ItemBackground = ChipBackground(ink, density);
        }

        /// <summary>
        /// The chip behind the checked tab, and a ripple of the same shape on every tab, so a tap
        /// lights up the area the chip will fill.
        /// </summary>
        private static Drawable ChipBackground(AColor ink, float density)
        {
            // Inset as a layer, not with an InsetDrawable: that one reports its insets as padding,
            // and a view takes its background's padding as its own, which squeezed each tab's
            // icon down onto its label.
            Drawable Chip(AColor color)
            {
                var shape = new GradientDrawable();
                shape.SetColor(color);
                shape.SetCornerRadius(ChipCornerDp * density);
                var horizontal = (int)(ChipInsetHorizontalDp * density);
                var vertical = (int)(ChipInsetVerticalDp * density);
                var chip = new LayerDrawable([shape]);
                chip.SetLayerInset(0, horizontal, vertical, horizontal, vertical);
                return chip;
            }

            var states = new StateListDrawable();
            states.AddState([global::Android.Resource.Attribute.StateChecked], Chip(ink));
            states.AddState([], new ColorDrawable(AColor.Transparent));

            var ripple = ColorStateList.ValueOf(AColor.Argb(0x33, ink.R, ink.G, ink.B));
            return new RippleDrawable(ripple, states, Chip(ink));
        }
    }
}
