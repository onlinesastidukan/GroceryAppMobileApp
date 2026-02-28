using GroceryApp.ViewModels;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class CustomerOrderHistoryPage : ContentPage
{
    private readonly CustomerOrderHistoryViewModel _viewModel;
    private readonly ApiService _apiService;

    public CustomerOrderHistoryPage(CustomerOrderHistoryViewModel viewModel, ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOrdersFromApiAsync();
    }

    private async Task LoadOrdersFromApiAsync()
    {
        try
        {
            _viewModel.IsLoading = true;
            _viewModel.HasError = false;
            _viewModel.ErrorMessage = string.Empty;

            var response = await _apiService.GetOrdersAsync();
            if (response?.Success == true && response.Data != null)
            {
                _viewModel.Orders.Clear();
                foreach (var order in response.Data.OrderByDescending(x => x.OrderDate))
                {
                    _viewModel.Orders.Add(order);
                }
                _viewModel.NotifyOrdersChanged();
            }
            else
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = response?.Message ?? "Failed to load orders";
                _viewModel.NotifyOrdersChanged();
            }
        }
        catch (Exception ex)
        {
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = $"Error: {ex.Message}";
            _viewModel.NotifyOrdersChanged();
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async void OnOrderTapped(object sender, TappedEventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is Order order)
        {
            var detailPage = Application.Current?.Handler?.MauiContext?.Services?.GetService<CustomerOrderDetailPage>();
            if (detailPage?.BindingContext is CustomerOrderDetailViewModel detailVm)
            {
                detailVm.OrderId = order.OrderId;
                await Navigation.PushAsync(detailPage);
                return;
            }

            await DisplayAlert("Order", "Unable to open order details.", "OK");
        }
    }
}
