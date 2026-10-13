using JustCompute.Shared.Abstractions.Navigation;
using JustCompute.Features.InputLocation;
using JustCompute.Features.SearchByCity;

namespace JustCompute.Navigation
{
    internal class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _viewModelToViewMapping = [];

        public NavigationService()
        {
            Register<InputLocationViewModel, InputLocationPage>();
            Register<SearchByCityViewModel, SearchByCityPage>();
        }

        private void Register<TViewModel, TPage>()
        {
            _viewModelToViewMapping[typeof(TViewModel)] = typeof(TPage);
            Routing.RegisterRoute(typeof(TPage).Name, typeof(TPage));
        }

        /// <summary>
        /// Pushes the page registered for <typeparamref name="TViewModel"/>, with its parameter.
        ///
        /// The parameter travels with the navigation: Shell hands it to the new page's view model
        /// as it creates the page, before showing it (see BaseViewModel). It used to be applied
        /// once the push had finished, to whichever page was on top by then. A city picked while
        /// the search was still sliding in had already pushed the location editor, which was
        /// given the search's parameter, threw, and closed the app; and the search itself took
        /// that tap before it knew whether to return the city or add it.
        /// </summary>
        public async Task NavigateToAsync<TViewModel>(object? parameter = null)
        {
            if (!_viewModelToViewMapping.TryGetValue(typeof(TViewModel), out var viewType)) return;

            if (parameter is null)
            {
                if (typeof(IQueryParameter).IsAssignableFrom(typeof(TViewModel)))
                {
                    throw new Exception($"Parameter must be specified when navigate to {typeof(TViewModel)}.");
                }

                await Shell.Current.GoToAsync(viewType.Name);
                return;
            }

            // Single-use parameters: unlike a plain dictionary, Shell does not hand them to the
            // page again when the user comes back to it.
            await Shell.Current.GoToAsync(viewType.Name, new ShellNavigationQueryParameters
            {
                [IQueryParameter.Key] = parameter,
            });
        }

        public Task NavigateToShellRouteAsync(string route) => Shell.Current.GoToAsync($"//{route}");

        public async Task NavigateBackAsync(object? result = null)
        {
            await Shell.Current.GoToAsync("..");
            var targetPage = Shell.Current.CurrentPage;
            if (targetPage.BindingContext is IResultHandler resultHandler && result != null)
            {
                resultHandler.ApplyResult(result);
            }
        }

        public bool NavigateToDefaultShellItem()
        {
            var shell = Shell.Current;
            var home = shell?.Items.FirstOrDefault();
            if (shell is null || home is null || shell.CurrentItem == home) return false;

            shell.CurrentItem = home;
            return true;
        }

        public void QuitApp() => Application.Current?.Quit();
    }
}
