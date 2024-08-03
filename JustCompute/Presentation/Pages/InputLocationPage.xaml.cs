using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
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