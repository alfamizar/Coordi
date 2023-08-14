using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class MoonPage : BasePage
    {
        public MoonPage(MoonViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        public override void OnAppWindowActivated()
        {
            base.OnAppWindowActivated();
        }

        public override void OnAppWindowResumed()
        {
            base.OnAppWindowResumed(); 
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}