﻿using JustCompute.Presentation.ViewModels;

namespace JustCompute.Presentation.Pages
{
    public partial class AddLocationPage : BasePage
    {
        public AddLocationPage(AddLocationViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
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