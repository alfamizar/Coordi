namespace Compute.Core.Navigation
{
    public interface INavigationService
    {
        Task NavigateToAsync<TViewModel>(object? parameter = null);
        Task NavigateBackAsync(object? result = null);
        void NavigateToDefaultShellItem();
        void QuitApp();
    }
}
