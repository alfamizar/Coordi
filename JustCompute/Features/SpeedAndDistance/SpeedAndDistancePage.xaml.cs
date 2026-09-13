
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.SpeedAndDistance
{
    public partial class SpeedAndDistancePage : BasePage
    {
        public SpeedAndDistancePage(SpeedAndDistanceViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}