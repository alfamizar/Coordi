using Foundation;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace JustCompute.Handlers.ExtendedSearchBar;

public partial class SearchBarExHandler : SearchBarHandler
{
    partial void SuppressPlatformCancelButton()
    {
        if (PlatformView is not UISearchBar uiSearchBar)
        {
            return;
        }

        uiSearchBar.ShowsCancelButton = false;

        if (uiSearchBar.ValueForKey(new NSString("cancelButton")) is UIButton cancelButton)
        {
            cancelButton.Hidden = true;
            cancelButton.Enabled = false;
        }
    }

    public void SetIconColor(UIColor value)
    {
        var textField = PlatformView.SearchTextField;
        var leftView = textField.LeftView ?? throw new Exception();
        leftView.TintColor = value;
    }

    private UIColor GetTextColor() => VirtualView.TextColor.ToPlatform();

    public void SetCancelButtonText(string newCancelButtonText)
    {
        PlatformView.SetValueForKey(NSObject.FromObject(newCancelButtonText)!, new NSString("cancelButtonText"));

        if (PlatformView is UISearchBar uiSearchBar)
        {
            if(VirtualView.CancelButtonColor is not null)
            {
                var cancelButtonAttributes = new UIStringAttributes
                {
                    ForegroundColor = VirtualView.CancelButtonColor.ToPlatform()
                };
                UIBarButtonItem.AppearanceWhenContainedIn(typeof(UISearchBar)).SetTitleTextAttributes(cancelButtonAttributes, UIControlState.Normal);
            }

            uiSearchBar.ShowsCancelButton = false;

            if (uiSearchBar.ValueForKey(new NSString("cancelButton")) is UIButton cancelButton)
            {
                cancelButton.Hidden = true;
                cancelButton.Enabled = false;
            }
        }
    }

    public static void MapTextBackgroundColor(ISearchBarHandler handler)
    {
        if (handler is SearchBarExHandler customHandler)
        {
            if(customHandler.PlatformView is UISearchBar uiSearchBar) 
            {
                uiSearchBar.BarTintColor = customHandler.VirtualView.CancelButtonColor.ToPlatform();
            }
        }
    }
}
