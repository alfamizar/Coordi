
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.Distance
{
    public partial class DistancePage : BasePage
    {
        public DistancePage(DistanceViewModel vm)
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