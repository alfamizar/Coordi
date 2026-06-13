using JustCompute.Shared.Controls;
using JustCompute.Shared.Helpers;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace JustCompute;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Application.Current?.RequestedThemeChanged += OnRequestedThemeChanged;
    }

    private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        SetStatusBarTheme();
    }

    private static void SetStatusBarTheme()
    {
        var resources = Application.Current?.Resources;
        if (resources == null) return;

        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        var color = isDark
            ? (Color)resources["DarkStatusBarColor"]
            : (Color)resources["LightStatusBarColor"];

#if ANDROID
        // StatusBar styling APIs require Android 23+; on older Android there is no equivalent API.
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            StatusBar.SetColor(color);
            StatusBar.SetStyle(StatusBarStyle.LightContent);
        }
#else
        // CA1416: the CommunityToolkit StatusBar APIs are annotated for iOS only, which makes the
        // analyzer flag MacCatalyst reachability. This app does not target MacCatalyst, and iOS 15
        // (the minimum deployment target) fully supports these APIs.
#pragma warning disable CA1416
        StatusBar.SetColor(color);
        StatusBar.SetStyle(StatusBarStyle.LightContent);
#pragma warning restore CA1416
#endif
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Deliberate UX: switching tabs snaps the newly-active tab back to its root page,
        // so a tab is always entered at root. This also sidesteps per-page status-bar /
        // toolbar colours not always restoring cleanly when popping back.
        // TODO (future): fix the underlying per-page status-bar restore so this reset can be
        // removed, and consider making it route-aware (opt-out for tabs that want to keep state).
        if (propertyName == nameof(CurrentItem))
        {
            ResetActiveTabToRoot();
        }
    }

    /// <summary>
    /// Pops the active Shell section back to its root page.
    /// </summary>
    /// <remarks>
    /// The old implementation iterated <c>Current.Items</c> but only ever mutated the active
    /// section's stack (RemovePage on other sections' pages silently no-ops), so this is
    /// behaviour-preserving — just scoped explicitly to the active section and made safe.
    ///
    /// Kept synchronous rather than deferred to the dispatcher: the app already ships this
    /// timing and it's flash-free, whereas deferring would let the entered tab render its old
    /// top page for a frame before snapping to root. The real hardening here is the modal guard
    /// (never yank the stack out from under a modal) plus exception safety — RemovePage can
    /// throw if it lands mid-transition, and that must never crash a tab switch. If a navigation
    /// race ever surfaces, the safe escalation is to wrap this body in Dispatcher.Dispatch(...).
    /// </remarks>
    private static void ResetActiveTabToRoot()
    {
        try
        {
            var navigation = Current?.Navigation;
            if (navigation is null)
            {
                return;
            }

            // Don't disturb the stack while a modal page is presented over it.
            if (navigation.ModalStack.Count > 0)
            {
                return;
            }

            // Snapshot before mutating. In Shell, NavigationStack[0] is the implicit root
            // (null), so removing everything above index 0 leaves the section at its root.
            var stack = navigation.NavigationStack.ToArray();
            for (int i = stack.Length - 1; i > 0; i--)
            {
                var page = stack[i];
                if (page is not null)
                {
                    navigation.RemovePage(page);
                }
            }
        }
        catch (Exception ex)
        {
            // A failed stack cleanup must never take down the tab switch that triggered it.
            System.Diagnostics.Debug.WriteLine($"ResetActiveTabToRoot failed: {ex}");
        }
    }

    public void OnAppWindowCreated()
    {
        SetStatusBarTheme();
        (CurrentPage as IAppLifecycleAware)?.OnAppWindowCreated();

        // DEBUG-only: deep-link + deterministic location seed for localized screenshots
        // (no-op in Release; no-op unless COORDI_SCREENSHOT_* inputs are provided).
        ScreenshotHarness.Apply();
    }

    public void OnAppWindowActivated()
    {
        (CurrentPage as IAppLifecycleAware)?.OnAppWindowActivated();
    }

    public void OnAppWindowResumed()
    {
        (CurrentPage as IAppLifecycleAware)?.OnAppWindowResumed();
    }

    public void OnAppWindowBackgrounding()
    {
        (CurrentPage as IAppLifecycleAware)?.OnAppWindowBackgrounding();
    }

    public void OnAppWindowStopped()
    {
        (CurrentPage as IAppLifecycleAware)?.OnAppWindowStopped();
    }

    public void OnAppWindowDestroying()
    {
        Application.Current?.RequestedThemeChanged -= OnRequestedThemeChanged;

        (CurrentPage as IAppLifecycleAware)?.OnAppWindowDestroying();
    }
}
