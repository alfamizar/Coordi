using JustCompute.Shared.ViewModels;

namespace JustCompute.Shared.Controls
{
    public abstract class BasePage : ContentPage, IAppLifecycleAware
    {
        /// <summary>
        /// The widest a screen's content is allowed to get. 720 is a large tablet held upright —
        /// comfortably more than the phone these layouts were drawn for, so nothing is cramped,
        /// and short enough that a card still reads as a card rather than a stripe.
        /// </summary>
        private const double ContentMaxWidth = 720;

        /// <summary>
        /// Below this height the window has no room to spare, so the cap is not applied. A phone
        /// held sideways is wider than the cap and shorter than anything: narrowing it there
        /// takes room from the screens that have least, and the sun chart loses on both axes at
        /// once. The cap is for windows with genuinely spare width — tablets and desktop.
        /// </summary>
        private const double CompactHeight = 480;

        public BaseViewModel? ViewModel => BindingContext as BaseViewModel;

        /// <summary>
        /// Lays the content out at a readable measure and centres it in whatever is left, so a
        /// tablet in landscape shows the app rather than the app stretched. Done here rather than
        /// per page, so a screen written later gets it without knowing it exists — and the app bar
        /// and flyout stay full width around it, which is what makes it read as a deliberate
        /// measure instead of a window that failed to resize.
        /// </summary>
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (Content is not View content || width <= 0 || height <= 0)
            {
                return;
            }

            bool cap = width > ContentMaxWidth && height >= CompactHeight;

            // PositiveInfinity, not -1: unlike WidthRequest, MaximumWidthRequest has no
            // "unset" sentinel — -1 is taken literally and collapses the page to nothing.
            content.MaximumWidthRequest = cap ? ContentMaxWidth : double.PositiveInfinity;
            content.HorizontalOptions = cap ? LayoutOptions.Center : LayoutOptions.Fill;
        }

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
                // Console rather than Debug: Debug.WriteLine is compiled out of Release, so a
                // page-lifecycle failure left no trace at all in a shipped build — which is how
                // a spinner turning forever went unexplained. This is the only place these
                // exceptions surface, so it must survive the Release build.
                Console.WriteLine($"[{source}] unhandled exception: {ex}");
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
