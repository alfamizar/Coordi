
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.Locations
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