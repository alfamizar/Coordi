using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using JustCompute.DependencyInjectionExtensions;
using JustCompute.Features.Distance;
using JustCompute.Features.InputLocation;
using JustCompute.Features.Locations;
using JustCompute.Features.MoonEclipses;
using JustCompute.Features.Converter;
using JustCompute.Features.Optics;
using JustCompute.Features.Planets;
using JustCompute.Features.SkyChart;
using JustCompute.Features.SearchByCity;
using JustCompute.Features.Settings;
using JustCompute.Features.SpeedAndDistance;
using JustCompute.Features.SunEclipses;
using JustCompute.Features.Today;
using JustCompute.Shared.Helpers;

namespace JustCompute;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // DEBUG-only: force the UI language for localization testing (no-op in Release).
        DebugCulture.ApplyOverrideIfPresent();

        var builder = MauiApp.CreateBuilder();

        // View model lifetimes, so a new feature picks one on purpose rather than by copying:
        //   * a Shell root (a ShellContent in AppShell.xaml) is a singleton. Shell realises
        //     those pages once and keeps them, so a transient one would only look transient,
        //     and the Ruler's route and a running trip have to survive leaving the screen;
        //   * a page pushed on top of one, like the location editor, is transient, so every
        //     visit starts from nothing;
        //   * two documented exceptions: the eclipse pages are transient over a singleton view
        //     model (see SunEclipsesFeature), and city search is a pushed singleton so the
        //     results survive a trip to the editor and back (see its OnNavigatedToAsync).
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .AddDistanceFeature()
            .AddInputLocationFeature()
            .AddLocationsFeature()
            .AddMoonEclipsesFeature()
            .AddSearchByCityFeature()
            .AddSettingsFeature()
            .AddSpeedAndDistanceFeature()
            .AddOpticsFeature()
            .AddSkyChartFeature()
            .AddPlanetsFeature()
            .AddConverterFeature()
            .AddSunEclipsesFeature()
            .AddTodayFeature()
            .ConfigureServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
