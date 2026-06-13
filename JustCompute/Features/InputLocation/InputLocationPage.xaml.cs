
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.InputLocation
{
    public partial class InputLocationPage : BasePage
    {
        public InputLocationPage(InputLocationViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}