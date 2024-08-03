using DotNext.Collections.Generic;
using JustCompute.Presentation.Pages;
using System.Runtime.CompilerServices;

namespace JustCompute;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(CurrentItem)) ResetNavigationStack();
    }

    private static void ResetNavigationStack()
    {
        Current?.Items?.ForEach(item =>
        {
            var stack = item?.Navigation?.NavigationStack?.ToArray();
            if (stack != null)
            {
                for (int i = stack.Length - 1; i > 0; i--)
                {
                    Current.Navigation.RemovePage(stack[i]);
                }
            }
        });
    }

    public void OnAppWindowCreated()
    {
        (CurrentPage as BasePage)?.OnAppWindowCreated();
    }

    public void OnAppWindowActivated()
    {
        (CurrentPage as BasePage)?.OnAppWindowActivated();
    }

    public void OnAppWindowResumed()
    {
        (CurrentPage as BasePage)?.OnAppWindowResumed();
    }

    public void OnAppWindowBackgrounding()
    {
        (CurrentPage as BasePage)?.OnAppWindowBackgrounding();
    }

    public void OnAppWindowStopped()
    {
        (CurrentPage as BasePage)?.OnAppWindowStopped();
    }
    public void OnAppWindowDestroying()
    {
        (CurrentPage as BasePage)?.OnAppWindowDestroying();
    }
}
