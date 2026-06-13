
using JustCompute.Shared.Controls;
using JustCompute.Shared.ViewModels;

namespace JustCompute.Features.SearchByCity
{
    public partial class SearchByCityPage : BasePage
    {
        public SearchByCityPage(SearchByCityViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm; 
        }
    }
}