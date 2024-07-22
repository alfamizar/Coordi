using JustCompute.Persistance.Repository.Constants;
using JustCompute.Presentation.Helpers;
using System.Reflection;

namespace JustCompute;

public partial class App : Application
{
    private readonly ThemeHandler _themeHandler;

    public App(ThemeHandler themeHandler)
    {
        InitializeComponent();

        _themeHandler = themeHandler;
        MainPage = new AppShell();

        if (VersionTracking.Default.IsFirstLaunchEver)
        {
            InstallDatabase();
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = base.CreateWindow(activationState);

        window.Created += (s, e) =>
        {
            // Custom logic
            _themeHandler.SetTheme();
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowCreated();
        };

        window.Activated += (s, e) =>
        {
            // Custom logic
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowActivated(); 
        };

        window.Resumed += (s, e) =>
        {
            // Custom logic
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowResumed();
        };

        window.Backgrounding += (s, e) =>
        {
            // Custom logic
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowBackgrounding();
        };

        window.Stopped += (s, e) =>
        {
            // Custom logic
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowStopped();
        };

        window.Destroying += (s, e) =>
        {
            // Custom logic
            var appShell = window.Page as AppShell;
            appShell?.OnAppWindowDestroying();
        };

        return window;
    }

    private static void InstallDatabase()
    {
        var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(RepositoryConstants.PreinstalledDatabasePath);
        using MemoryStream memoryStream = new();
        stream?.CopyTo(memoryStream);

        File.WriteAllBytes(RepositoryConstants.DatabasePath, memoryStream.ToArray());
    }
}
