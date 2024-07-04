using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using WColor = Windows.UI.Color;

namespace JustCompute.Handlers.ExtendedSearchBar;

public partial class SearchBarExHandler : SearchBarHandler
{
    public void SetIconColor(WColor value)
    {
        PlatformView.QueryIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(value);
    }

    private WColor GetTextColor() => VirtualView.TextColor.ToWindowsColor();
}