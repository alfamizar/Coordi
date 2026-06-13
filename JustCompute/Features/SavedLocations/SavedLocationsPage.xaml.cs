
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.SavedLocations
{
    public partial class SavedLocationsPage : BasePage
    {
        public SavedLocationsPage(SavedLocationsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        public override void OnAppWindowResumed()
        {
            base.OnAppWindowResumed();
        }
    }
}