﻿using System;
using System.Globalization;
using atomex.Resources;
using atomex.ViewModel;
using Xamarin.Forms;

namespace atomex
{
    public partial class DelegatePage : ContentPage
    {
        private DelegateViewModel _delegateViewModel;

        public DelegatePage()
        {
            InitializeComponent();
        }

        public DelegatePage(DelegateViewModel delegateViewModel)
        {
            InitializeComponent();
            _delegateViewModel = delegateViewModel;
            BindingContext = delegateViewModel;
        }

        private void OnBakerPickerFocused(object sender, FocusEventArgs args)
        {
            BakerFrame.HasShadow = args.IsFocused;
        }

        private void OnFromAddressPickerFocused(object sender, FocusEventArgs args)
        {
            FromAddressFrame.HasShadow = args.IsFocused;
        }

        private void OnToAddressEntryFocused(object sender, FocusEventArgs args)
        {
            ToAddressFrame.HasShadow = args.IsFocused;
        }

        private void OnFeeEntryFocused(object sender, FocusEventArgs args)
        {
            FeeFrame.HasShadow = args.IsFocused;

            Device.StartTimer(TimeSpan.FromSeconds(0.1), () =>
            {
                if (args.IsFocused)
                    ScrollView.ScrollToAsync(0, ScrollView.Height / 2 - FeeFrame.Height / 2, true);
                else
                    ScrollView.ScrollToAsync(0, 0, true);
                return false;
            });

            if (!args.IsFocused)
            {
                decimal fee;
                try
                {
                    decimal.TryParse(Fee.Text?.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out fee);
                }
                catch (FormatException)
                {
                    fee = 0;
                }
                _delegateViewModel.Fee = fee;
                Fee.Text = _delegateViewModel.FeeString;
            }
        }

        private async void OnNextButtonClicked(object sender, EventArgs args)
        {
            try
            {
                BlockActions(true);
                var error = await _delegateViewModel.Validate();
                BlockActions(false);
                if (error != null)
                {
                    await DisplayAlert(AppResources.Error, error, AppResources.AcceptButton);
                    return;
                }
                else
                {
                    await Navigation.PushAsync(new DelegationConfirmationPage(_delegateViewModel));
                }
            }
            catch (Exception e)
            {
                BlockActions(false);
                await DisplayAlert(AppResources.Error, AppResources.DelegationValidationError, AppResources.AcceptButton);
            }
        }

        private void BlockActions(bool flag)
        {
            ValidatingLoader.IsVisible = ValidatingLoader.IsRunning = flag;
            NextButton.IsEnabled = !flag;
            if (flag)
            {
                NextButton.Text = "";
                Content.Opacity = 0.5;
            }
            else
            {
                NextButton.Text = AppResources.NextButton;
                Content.Opacity = 1;
            }
        }
    }
}
