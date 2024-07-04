using CommunityToolkit.Maui;
using JustCompute.Presentation.Popups.Pages;
using JustCompute.Presentation.Popups.ViewModels;

namespace JustCompute.DependencyInjectionExtensions;

public static class PopupsExtensions
{
    public static MauiAppBuilder ConfigurePopups(this MauiAppBuilder builder)
    {
        builder.Services.AddTransientPopup<SortOptionsPopup, SortOptionsPopupViewModel>();

        return builder;
    }
}