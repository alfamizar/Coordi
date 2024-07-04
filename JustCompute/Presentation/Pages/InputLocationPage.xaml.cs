using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class InputLocationPage : BasePage
    {
        public InputLocationPage(InputLocationViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
    }
}