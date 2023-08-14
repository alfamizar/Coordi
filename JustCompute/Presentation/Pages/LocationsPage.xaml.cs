using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class LocationsPage : BasePage
    {
        public LocationsPage(LocationsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}