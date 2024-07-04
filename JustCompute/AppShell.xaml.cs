using JustCompute.Presentation.Pages;

namespace JustCompute;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();;
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
