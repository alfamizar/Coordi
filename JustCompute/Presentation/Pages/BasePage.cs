﻿using JustCompute.Presentation.ViewModels.Base;

namespace JustCompute.Presentation.Pages
{
    public abstract class BasePage : ContentPage
    {
        public BaseViewModel ViewModel => BindingContext as BaseViewModel;

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel?.OnPageAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            ViewModel?.OnPageDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            // Check if the ViewModel has handled the back button press
            return ViewModel?.OnBackButtonPressed() ?? base.OnBackButtonPressed();
        }

        protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            ViewModel?.OnNavigatedFrom();
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            ViewModel?.OnNavigatedTo();
        }

        protected override void OnNavigatingFrom(NavigatingFromEventArgs args)
        {
            base.OnNavigatingFrom(args);
            ViewModel?.OnNavigatingFrom();
        }

        public virtual void OnAppWindowCreated()
        {
            // Custom logic
            ViewModel?.OnAppWindowCreated();
        }

        public virtual void OnAppWindowActivated()
        {
            // Custom logic
            ViewModel?.OnAppWindowActivated();
        }

        public virtual void OnAppWindowResumed()
        {
            // Custom logic
            ViewModel?.OnAppWindowResumed();
        }

        public virtual void OnAppWindowBackgrounding()
        {
            // Custom logic
            ViewModel?.OnAppWindowBackgrounding();
        }

        public virtual void OnAppWindowStopped()
        {
            // Custom logic
            ViewModel?.OnAppWindowStopped();
        }

        public virtual void OnAppWindowDestroying()
        {
            // Custom logic
            ViewModel?.OnAppWindowDestroying();
        }
    }
}
