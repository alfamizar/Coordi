using JustCompute.Shared.ViewModels;

namespace JustCompute.Shared.Controls
{
    public abstract class BasePage : ContentPage, IAppLifecycleAware
    {
        public BaseViewModel? ViewModel => BindingContext as BaseViewModel;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (ViewModel is null) return;
            await SafeInvoke(ViewModel.OnPageAppearingAsync, nameof(OnAppearing));
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (ViewModel is null) return;
            await SafeInvoke(ViewModel.OnPageDisappearingAsync, nameof(OnDisappearing));
        }

        protected override bool OnBackButtonPressed()
        {
            return ViewModel?.OnBackButtonPressed() ?? base.OnBackButtonPressed();
        }

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            ViewModel?.OnNavigatedFrom();
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            if (ViewModel is null) return;
            await SafeInvoke(ViewModel.OnNavigatedToAsync, nameof(OnNavigatedTo));
        }

        protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
        {
            base.OnNavigatingFrom(args);
            ViewModel?.OnNavigatingFrom();
        }

        private static async Task SafeInvoke(Func<Task> work, string source)
        {
            try
            {
                await work().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{source}] unhandled exception: {ex}");
            }
        }

        public virtual void OnAppWindowCreated() => ViewModel?.OnAppWindowCreated();
        public virtual void OnAppWindowActivated() => ViewModel?.OnAppWindowActivated();
        public virtual void OnAppWindowResumed() => ViewModel?.OnAppWindowResumed();
        public virtual void OnAppWindowBackgrounding() => ViewModel?.OnAppWindowBackgrounding();
        public virtual void OnAppWindowStopped() => ViewModel?.OnAppWindowStopped();
        public virtual void OnAppWindowDestroying() => ViewModel?.OnAppWindowDestroying();
    }
}
