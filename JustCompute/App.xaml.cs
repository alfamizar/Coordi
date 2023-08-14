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

    protected override Window CreateWindow(IActivationState activationState)
    {
        Window window = base.CreateWindow(activationState);

        window.Created += (s, e) =>
        {
            // Custom logic
            _themeHandler.SetTheme();
            ((AppShell)window.Page).OnAppWindowCreated();
        };

        window.Activated += (s, e) =>
        {
            // Custom logic
            ((AppShell)window.Page).OnAppWindowActivated();
        };

        window.Resumed += (s, e) =>
        {
            // Custom logic
            ((AppShell)window.Page).OnAppWindowResumed();
        };

        window.Backgrounding += (s, e) =>
        {
            // Custom logic
            ((AppShell)window.Page).OnAppWindowBackgrounding();
        };

        window.Stopped += (s, e) =>
        {
            // Custom logic
            ((AppShell)window.Page).OnAppWindowStopped();
        };

        window.Destroying += (s, e) =>
        {
            // Custom logic
            ((AppShell)window.Page).OnAppWindowDestroying();
        };

        return window;
    }

    private static void InstallDatabase()
    {
        var assembly = IntrospectionExtensions.GetTypeInfo(typeof(App)).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(RepositoryConstants.PreinstalledDatabasePath);
        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);

        File.WriteAllBytes(RepositoryConstants.DatabasePath, memoryStream.ToArray());
    }
}
