namespace JustCompute.Shared.Controls
{
    public interface IAppLifecycleAware
    {
        void OnAppWindowCreated();
        void OnAppWindowActivated();
        void OnAppWindowResumed();
        void OnAppWindowBackgrounding();
        void OnAppWindowStopped();
        void OnAppWindowDestroying();
    }
}
