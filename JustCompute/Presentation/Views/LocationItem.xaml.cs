using System.Windows.Input;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LocationItem : ContentView
    {
        public static readonly BindableProperty IsButtonsVisibleProperty = BindableProperty.Create(
            nameof(IsButtonsVisible), typeof(bool), typeof(LocationItem), true);

        public bool IsButtonsVisible
        {
            get => (bool)GetValue(IsButtonsVisibleProperty);
            set => SetValue(IsButtonsVisibleProperty, value);
        }

        public static readonly BindableProperty IsLocationNameVisibleProperty = BindableProperty.Create(
            nameof(IsLocationNameVisible), typeof(bool), typeof(LocationItem), true);

        public bool IsLocationNameVisible
        {
            get => (bool)GetValue(IsLocationNameVisibleProperty);
            set => SetValue(IsLocationNameVisibleProperty, value);
        }

        public static readonly BindableProperty ItemClickedCommandProperty = BindableProperty.Create(
            nameof(ItemClickedCommand), typeof(ICommand), typeof(LocationItem));

        public ICommand ItemClickedCommand
        {
            get => (ICommand)GetValue(ItemClickedCommandProperty);
            set => SetValue(ItemClickedCommandProperty, value);
        }

        public static readonly BindableProperty EditButtonClickedCommandProperty = BindableProperty.Create(
            nameof(EditButtonClickedCommand), typeof(ICommand), typeof(LocationItem));

        public ICommand EditButtonClickedCommand
        {
            get => (ICommand)GetValue(EditButtonClickedCommandProperty);
            set => SetValue(EditButtonClickedCommandProperty, value);
        }

        public static readonly BindableProperty DeleteButtonClickedCommandProperty = BindableProperty.Create(
            nameof(DeleteButtonClickedCommand), typeof(ICommand), typeof(LocationItem));

        public ICommand DeleteButtonClickedCommand
        {
            get => (ICommand)GetValue(DeleteButtonClickedCommandProperty);
            set => SetValue(DeleteButtonClickedCommandProperty, value);
        }

        public static readonly BindableProperty LocationProperty =
            BindableProperty.Create(
                propertyName: nameof(Location),
                returnType: typeof(Location),
                declaringType: typeof(LocationItem));

        public Location Location
        {
            get => (Location)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public LocationItem()
        {
            InitializeComponent();
        }
    }
}