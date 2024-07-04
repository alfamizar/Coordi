using Compute.Core.Navigation;
using JustCompute.Presentation.Pages;
using JustCompute.Presentation.ViewModels;

namespace JustCompute.Navigation
{
    internal class NavigationService : INavigationService
    {
        private readonly Dictionary<Type, Type> _viewModelToViewMapping = [];

        public NavigationService()
        {
            Register<LocationsViewModel, LocationsPage>();
            Register<InputLocationViewModel, InputLocationPage>();
            Register<SavedLocationsViewModel, SavedLocationsPage>();
            Register<SearchByCityViewModel, SearchByCityPage>();
        }

        private void Register<TViewModel, TPage>()
        {
            _viewModelToViewMapping[typeof(TViewModel)] = typeof(TPage);
            Routing.RegisterRoute(typeof(TPage).Name, typeof(TPage));
        }

        public async Task NavigateToAsync<TViewModel>(object? parameter = null)
        {
            if (_viewModelToViewMapping.TryGetValue(typeof(TViewModel), out var viewType))
            {
                await Shell.Current.GoToAsync(viewType.Name);
                var targetPage = Shell.Current.CurrentPage;
                var viewmodel = targetPage.BindingContext;
                if (parameter != null && viewmodel is IQueryParameter parameterReceiver)
                {
                    parameterReceiver.ApplyQueryParameter(parameter);
                }
            }
        }

        public async Task NavigateBackAsync(object? result = null)
        {
            await Shell.Current.GoToAsync("..");
            var targetPage = Shell.Current.CurrentPage;
            if (targetPage.BindingContext is IResultHandler resultHandler && result != null)
            {
                resultHandler.ApplyResult(result);
            }
        }

        public void QuitApp() => Application.Current?.Quit();

        public void NavigateToTheDefaultScreen()
        {
            Shell.Current.CurrentItem = Shell.Current.Items.First();
        }
    }
}
