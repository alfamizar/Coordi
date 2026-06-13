
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.MoonEclipses
{
    public partial class MoonEclipsesPage : BasePage
    {
        public MoonEclipsesPage(MoonEclipsesViewModel vm)
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
    }
}