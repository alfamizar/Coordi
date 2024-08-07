using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using JustCompute.DependencyInjectionExtensions;

namespace JustCompute;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureAutoMapper()
            .ConfigureViewModels()
            .ConfigurePages()
            .ConfigureServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIconsRegular");
            })
            .ConfigureEssentials(essentials =>
            {
                essentials.UseVersionTracking();
            })
            .ConfigureMauiHandlers()
            .ConfigurePopups()
            .ConfigurePolly();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}