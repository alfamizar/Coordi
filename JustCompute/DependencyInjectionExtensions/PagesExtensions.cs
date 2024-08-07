﻿using JustCompute.Presentation.Pages;

namespace JustCompute.DependencyInjectionExtensions;

public static class PagesExtensions
{
    public static MauiAppBuilder ConfigurePages(this MauiAppBuilder builder)
    {
        // main tabs of the app
        builder.Services.AddSingleton<SunPage>();
        builder.Services.AddSingleton<MoonPage>();
        builder.Services.AddSingleton<SunEclipsesPage>();
        builder.Services.AddSingleton<CoordinatesConverterPage>();
        builder.Services.AddSingleton<MoonEclipsesPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<LocationsPage>();
        builder.Services.AddTransient<InputLocationPage>();
        builder.Services.AddSingleton<TimeTravelPage>();
        builder.Services.AddSingleton<SearchByCityPage>();
        builder.Services.AddTransient<SavedLocationsPage>();
        builder.Services.AddSingleton<DistancePage>();
        builder.Services.AddSingleton<SpeedAndDistancelPage>();

        return builder;
    }
}