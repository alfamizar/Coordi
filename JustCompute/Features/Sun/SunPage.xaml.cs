
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.Sun
{
    public partial class SunPage : BasePage
    {
        public SunPage(SunViewModel vm)
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