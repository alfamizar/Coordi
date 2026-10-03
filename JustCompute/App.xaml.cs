using Compute.Core.Domain.Services;
using JustCompute.Persistence.Repository;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Services.LocationService;
using JustCompute.Shared.Helpers;
using System.Reflection;
using Compute.Core.Utils;

namespace JustCompute;

public partial class App : Application
{
    private readonly ThemeHandler _themeHandler;
    private readonly IPermissionGateService _permissionGate;
    private readonly ILocationSelection _selection;

    public App(
        ThemeHandler themeHandler,
        IPermissionGateService permissionGate,
        DatabasePaths databasePaths,
        ILocationSelection selection,
        CityChangePrompt cityChangePrompt)
    {
        InitializeComponent();

        _themeHandler = themeHandler;
        _permissionGate = permissionGate;
        _selection = selection;

        // Listening before any screen loads: the first fresh fix of a launch can arrive a second
        // later, and an offer of a new town with nobody to ask would be lost.
        cityChangePrompt.Start();

        // Reinstalled on every new version, but never over a user's places: a catalogue from before
        // the split still holds them, and the installer parks it before writing a fresh one. The
        // old code here truncated it outright, before the migration had read anything out of it.
        CatalogueInstaller.Install(
            databasePaths,
            () => typeof(App).Assembly.GetManifestResourceStream(RepositoryConstants.PreinstalledDatabasePath),
            isNewVersion: VersionTracking.Default.IsFirstLaunchForCurrentVersion);

        // Off the UI thread on purpose: the first zone lookup pays a one-off ~23 ms to load
        // GeoTimeZone's dataset, and left to itself it lands on whichever screen first asks a
        // location for its time. Fire and forget — nothing waits on it, and any failure just
        // means the first real lookup pays the cost as it did before.
        Task.Run(TimeZoneUtils.Prewarm).Forget(nameof(TimeZoneUtils.Prewarm));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        AppShell appShell = new();

        Window window = new(appShell);

        window.Created += OnWindowCreated;
        window.Activated += OnWindowActivated;
        window.Resumed += OnWindowResumed;
        window.Backgrounding += OnWindowBackgrounding;
        window.Stopped += OnWindowStopped;
        window.Destroying += OnWindowDestroying;

        return window;
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        _themeHandler.SetTheme();

        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowCreated();
        }
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowActivated();
        }
    }

    private async void OnWindowResumed(object? sender, EventArgs e)
    {
        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowResumed();
        }

        try
        {
            await _permissionGate.RefreshLocationPermissionState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PermissionGate refresh failed: {ex}");
        }

        // After the permission check, which it relies on. Android keeps the process alive between
        // uses, so a return after a while away is a launch to the person holding the phone, and
        // the device is asked again where it is.
        _selection.RefreshOnReturnAsync().Forget(nameof(ILocationSelection.RefreshOnReturnAsync));
    }

    private void OnWindowBackgrounding(object? sender, BackgroundingEventArgs e)
    {
        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowBackgrounding();
        }
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowStopped();
        }
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        if (sender is Window { Page: AppShell appShell })
        {
            appShell.OnAppWindowDestroying();
        }

        if (sender is Window window)
        {
            window.Created -= OnWindowCreated;
            window.Activated -= OnWindowActivated;
            window.Resumed -= OnWindowResumed;
            window.Backgrounding -= OnWindowBackgrounding;
            window.Stopped -= OnWindowStopped;
            window.Destroying -= OnWindowDestroying;
        }
    }
}
