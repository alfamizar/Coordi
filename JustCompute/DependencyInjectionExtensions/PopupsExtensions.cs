using CommunityToolkit.Maui;
using JustCompute.Shared.Popups;

namespace JustCompute.DependencyInjectionExtensions;

public static class PopupsExtensions
{
    public static MauiAppBuilder ConfigurePopups(this MauiAppBuilder builder)
    {
        builder.Services.AddTransientPopup<SortOptionsPopup, SortOptionsPopupViewModel>();

        return builder;
    }
}