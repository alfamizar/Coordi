using JustCompute.Shared.Controls;

namespace JustCompute.Features.Today
{
    public partial class TodayPage : BasePage
    {
        public TodayPage(TodayViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
