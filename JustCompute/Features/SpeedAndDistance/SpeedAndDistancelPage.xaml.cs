
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.SpeedAndDistance
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