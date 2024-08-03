using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class SpeedAndDistancelPage : BasePage
    {
        public SpeedAndDistancelPage(SpeedAndDistanceViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}