using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using JustCompute.DependencyInjectionExtensions;
using JustCompute.Features.CoordinatesConverter;
using JustCompute.Features.Distance;
using JustCompute.Features.InputLocation;
using JustCompute.Features.Locations;
using JustCompute.Features.Moon;
using JustCompute.Features.MoonEclipses;
using JustCompute.Features.SavedLocations;
using JustCompute.Features.SearchByCity;
using JustCompute.Features.Settings;
using JustCompute.Features.SpeedAndDistance;
using JustCompute.Features.Sun;
using JustCompute.Features.SunEclipses;
using JustCompute.Features.TimeTravel;
using JustCompute.Shared.Helpers;

namespace JustCompute;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // DEBUG-only: force the UI language for localization testing (no-op in Release).
        DebugCulture.ApplyOverrideIfPresent();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureAutoMapper()
            .AddCoordinatesConverterFeature()
            .AddDistanceFeature()
            .AddInputLocationFeature()
            .AddLocationsFeature()
            .AddMoonFeature()
            .AddMoonEclipsesFeature()
            .AddSavedLocationsFeature()
            .AddSearchByCityFeature()
            .AddSettingsFeature()
            .AddSpeedAndDistanceFeature()
            .AddSunFeature()
            .AddSunEclipsesFeature()
            .AddTimeTravelFeature()
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
