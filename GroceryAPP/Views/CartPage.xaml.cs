using GroceryApp.ViewModels;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class CartPage : ContentPage
{
    private readonly CartViewModel _viewModel;

    public CartPage(CartViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsyncSafe();
    }

    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        await _viewModel.PlaceOrderCommand.ExecuteAsync(null);
    }

    private async void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.OrderPlacedSuccessfully) && _viewModel.OrderPlacedSuccessfully)
        {
            var orderHistoryPage = Application.Current.Handler.MauiContext.Services.GetService<CustomerOrderHistoryPage>();
            await Navigation.PushAsync(orderHistoryPage);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
