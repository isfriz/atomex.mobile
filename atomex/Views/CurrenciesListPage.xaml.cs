﻿using Xamarin.Forms;
using atomex.ViewModel;
using Atomex;

namespace atomex
{
    public partial class CurrenciesListPage : ContentPage
    {
        private INavigationService _navigationService;

        public CurrenciesListPage()
        {
            InitializeComponent();
        }

        public CurrenciesListPage(IAtomexApp AtomexApp, CurrenciesViewModel CurrenciesViewModel, INavigationService navigationService)
        {
            InitializeComponent();

            _navigationService = navigationService;

            if (currenciesList != null)
            {
                //currenciesList.SeparatorVisibility = SeparatorVisibility.None;
                currenciesList.ItemsSource = CurrenciesViewModel.Currencies;
            }
        }

        async void OnListViewItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                await Navigation.PushAsync(new CurrencyPage(e.SelectedItem as CurrencyViewModel, _navigationService));
            }
        }
    }
}
