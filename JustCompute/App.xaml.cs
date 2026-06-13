using Compute.Core.Domain.Services;
using JustCompute.Persistence.Repository.Constants;
using JustCompute.Shared.Helpers;
using System.Reflection;

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

        if (VersionTracking.Default.IsFirstLaunchEver || !File.Exists(RepositoryConstants.DatabasePath))
        {
            InstallDatabase();
        }
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

        Directory.CreateDirectory(Path.GetDirectoryName(RepositoryConstants.DatabasePath)!);

        using FileStream fileStream = File.Create(RepositoryConstants.DatabasePath);
        stream.CopyTo(fileStream);
    }
}
