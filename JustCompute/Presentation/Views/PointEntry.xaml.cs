using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Location = Compute.Core.Domain.Entities.Models.Location;

namespace JustCompute.Presentation.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PointEntry : ContentView
    {
        public static readonly BindableProperty PointTitleProperty = BindableProperty.Create(
            nameof(PointTitle), typeof(string), typeof(PointEntry), default(string));

        public string PointTitle
        {
            get => (string)GetValue(PointTitleProperty);
            set => SetValue(PointTitleProperty, value);
        }

        public static readonly BindableProperty IsPointLabelVisibleProperty = BindableProperty.Create(
            nameof(IsPointLabelVisible), typeof(bool), typeof(PointEntry), true);

        public bool IsPointLabelVisible
        {
            get => (bool)GetValue(IsPointLabelVisibleProperty);
            set => SetValue(IsPointLabelVisibleProperty, value);
        }

        public static readonly BindableProperty IsPointCityCountryLabelVisibleProperty = BindableProperty.Create(
            nameof(IsPointCityCountryLabelVisible), typeof(bool), typeof(PointEntry), true);

        public bool IsPointCityCountryLabelVisible
        {
            get => (bool)GetValue(IsPointCityCountryLabelVisibleProperty);
            set => SetValue(IsPointCityCountryLabelVisibleProperty, value);
        }

        public static readonly BindableProperty ImageButtonIconSourceProperty = BindableProperty.Create(
            nameof(ImageButtonIconSource), typeof(ImageSource), typeof(PointEntry), default(ImageSource));

        public ImageSource ImageButtonIconSource
        {
            get => (ImageSource)GetValue(ImageButtonIconSourceProperty);
            set => SetValue(ImageButtonIconSourceProperty, value);
        }

        public static readonly BindableProperty StoppedTypingTimeThresholdProperty = BindableProperty.Create(
            nameof(StoppedTypingTimeThreshold), typeof(int), typeof(PointEntry));

        public int StoppedTypingTimeThreshold
        {
            get => (int)GetValue(StoppedTypingTimeThresholdProperty);
            set => SetValue(StoppedTypingTimeThresholdProperty, value);
        }

        public static readonly BindableProperty IsImageButtonVisibleProperty = BindableProperty.Create(
            nameof(IsImageButtonVisible), typeof(bool), typeof(PointEntry), true);

        public bool IsImageButtonVisible
        {
            get => (bool)GetValue(IsImageButtonVisibleProperty);
            set => SetValue(IsImageButtonVisibleProperty, value);
        }

        public static readonly BindableProperty ImageButtonClickedCommandProperty = BindableProperty.Create(
            nameof(ImageButtonClickedCommand), typeof(ICommand), typeof(PointEntry));

        public ICommand ImageButtonClickedCommand
        {
            get => (ICommand)GetValue(ImageButtonClickedCommandProperty);
            set => SetValue(ImageButtonClickedCommandProperty, value);
        }

        public static readonly BindableProperty UserStoppedTypingCommandProperty = BindableProperty.Create(
            nameof(UserStoppedTypingCommand), typeof(ICommand), typeof(PointEntry));

        public ICommand UserStoppedTypingCommand
        {
            get => (ICommand)GetValue(UserStoppedTypingCommandProperty);
            set => SetValue(UserStoppedTypingCommandProperty, value);
        }

        public static readonly BindableProperty LocationProperty =
            BindableProperty.Create(
                propertyName: nameof(Location),
                returnType: typeof(Location),
                declaringType: typeof(PointEntry));

        public Location Location
        {
            get => (Location)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        public PointEntry()
        {
            InitializeComponent();
        }
    }
}