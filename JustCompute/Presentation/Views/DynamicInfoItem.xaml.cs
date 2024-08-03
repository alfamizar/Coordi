namespace JustCompute.Presentation.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DynamicInfoItem : ContentView
    {
        public static readonly BindableProperty ItemTitleProperty = BindableProperty.Create(
            nameof(ItemTitle), typeof(string), typeof(DynamicInfoItem), default(string));

        public string ItemTitle
        {
            get => (string)GetValue(ItemTitleProperty);
            set => SetValue(ItemTitleProperty, value);
        }

        public static readonly BindableProperty ItemValueProperty = BindableProperty.Create(
            nameof(ItemValue), typeof(string), typeof(DynamicInfoItem), default(string));

        public string ItemValue
        {
            get => (string)GetValue(ItemValueProperty);
            set => SetValue(ItemValueProperty, value);
        }

        public DynamicInfoItem()
        {
            InitializeComponent();
        }
    }
}