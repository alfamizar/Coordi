using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
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