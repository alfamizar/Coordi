using Compute.Core.Domain.Services;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Shared.Helpers;
using System.Reflection;
using Compute.Core.Utils;

namespace JustCompute;

public partial class App : Application
{
    private readonly ThemeHandler _themeHandler;
    private readonly IPermissionGateService _permissionGate;

    public App(ThemeHandler themeHandler, IPermissionGateService permissionGate)
    {
        InitializeComponent();

        _themeHandler = themeHandler;
        _permissionGate = permissionGate;

        // Reinstalled on every new version, not just the first launch ever. That is only safe
        // now the user's saved places live in their own file: this overwrites the shipped city
        // catalogue wholesale, which used to mean overwriting their locations along with it.
        if (VersionTracking.Default.IsFirstLaunchForCurrentVersion
            || !File.Exists(RepositoryConstants.CataloguePath))
        {
            InstallDatabase();
        }

        // Off the UI thread on purpose: the first zone lookup pays a one-off ~23 ms to load
        // GeoTimeZone's dataset, and left to itself it lands on whichever screen first asks a
        // location for its time. Fire and forget — nothing waits on it, and any failure just
        // means the first real lookup pays the cost as it did before.
        _ = Task.Run(TimeZoneUtils.Prewarm);
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

    private static void InstallDatabase()
    {
        var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(RepositoryConstants.PreinstalledDatabasePath);
        if (stream is null)
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(RepositoryConstants.CataloguePath)!);

        using FileStream fileStream = File.Create(RepositoryConstants.CataloguePath);
        stream.CopyTo(fileStream);
    }
}
