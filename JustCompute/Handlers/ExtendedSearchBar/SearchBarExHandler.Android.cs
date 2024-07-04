using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;

namespace JustCompute.Handlers.ExtendedSearchBar;

public partial class SearchBarExHandler : SearchBarHandler
{
    public void SetIconColor(AColor value)
    {
        var searchIcon = PlatformView.FindViewById(Resource.Id.search_mag_icon) as ImageView;
        searchIcon?.SetColorFilter(value, PorterDuff.Mode.SrcAtop);
    }

    public AColor GetTextColor() => VirtualView.TextColor.ToPlatform();

    public static void MapIconBackgroundColor(ISearchBarHandler handler)
    {
        if (handler is SearchBarExHandler customHandler)
        {
            var searchView = customHandler.PlatformView;
            searchView?.FindViewById(Resource.Id.search_mag_icon)?.SetBackgroundColor(Android.Graphics.Color.LightGray);
        }
    }

    public static void MapTextBackgroundColor(ISearchBarHandler handler)
    {
        if (handler is SearchBarExHandler customHandler)
        {
            var searchView = customHandler.PlatformView;
            var searchEditText = searchView.FindViewById(Resource.Id.search_src_text) as EditText;
            searchEditText?.SetBackgroundColor(Colors.LightGray.ToAndroid());
        }
    }

    public static void MapIconCircle(ISearchBarHandler handler)
    {
        if (handler is SearchBarExHandler customHandler)
        {
            var searchView = customHandler.PlatformView;
            var searchIcon = searchView.FindViewById(Resource.Id.search_mag_icon) as ImageView as ImageView;
            //searchIcon.SetBackgroundResource(Resource.Drawable.circle_border);
            searchIcon?.SetPadding(0, 10, 0, 10);
        }
    }
}