using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class SearchByCityPage : BasePage
    {
        public SearchByCityPage(SearchByCityViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            // this is a workaround to fix wrong collection view width after device rotation
            locationsCollectionView.ItemsLayout = new GridItemsLayout(ItemsLayoutOrientation.Vertical);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Disabled;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.Current.FlyoutBehavior = FlyoutBehavior.Flyout;
        }
    }
}