using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class TimeTravelPage : BasePage
    {
        public TimeTravelPage(TimeTravelViewModel vm)
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