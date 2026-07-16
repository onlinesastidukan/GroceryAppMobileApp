using GroceryApp.ViewModels;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class CustomerOrderHistoryPage : ContentPage
{
    private readonly CustomerOrderHistoryViewModel _viewModel;
    private readonly ApiService _apiService;

    public CustomerOrderHistoryPage(
        CustomerOrderHistoryViewModel viewModel,
        ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private async Task LoadOrdersFromApiAsync(string mobileNumber)
    {
        try
        {
            _viewModel.IsLoading = true;
            _viewModel.HasError = false;
            _viewModel.ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(mobileNumber))
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "Mobile number is required to load past orders.";
                _viewModel.Orders.Clear();
                _viewModel.NotifyOrdersChanged();
                return;
            }

            var response = await _apiService.GetOrdersByMobileAsync(mobileNumber);
            if (response?.Success == true && response.Data != null)
            {
                var nonDeliveredOrders = response.Data
                    .Where(x => !string.Equals(x.Status, "Delivered", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.OrderDate)
                    .ToList();

                _viewModel.Orders.Clear();
                foreach (var order in nonDeliveredOrders)
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

    private async void OnCheckPastOrdersClicked(object sender, EventArgs e)
    {
        var mobileNumber = MobileEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(mobileNumber))
        {
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "Please enter mobile number.";
            return;
        }

        await LoadOrdersFromApiAsync(mobileNumber);
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
