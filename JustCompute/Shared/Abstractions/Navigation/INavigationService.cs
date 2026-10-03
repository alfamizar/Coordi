namespace JustCompute.Shared.Abstractions.Navigation
{
    public interface INavigationService
    {
        Task NavigateToAsync<TViewModel>(object? parameter = null);

        /// <summary>
        /// Navigates to a Shell route (a flyout destination such as <c>locations</c>), which
        /// <see cref="NavigateToAsync{TViewModel}"/> cannot reach — that one only knows the
        /// modally-pushed pages registered with it.
        /// </summary>
        Task NavigateToShellRouteAsync(string route);
        Task NavigateBackAsync(object? result = null);

        /// <summary>
        /// Shows the default screen (Today). False when it is already showing, so a back press
        /// there can be handed to the system — which is what lets Back close the app.
        /// </summary>
        bool NavigateToDefaultShellItem();

        void QuitApp();
    }
}
