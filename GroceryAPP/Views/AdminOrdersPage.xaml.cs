using GroceryApp.ViewModels;
using GroceryApp.Models;

namespace GroceryApp.Views;

public partial class AdminOrdersPage : ContentPage
{
    private bool _isNavigating;

    public AdminOrdersPage(AdminOrdersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel baseViewModel)
        {
            await baseViewModel.InitializeAsyncSafe();
        }
    }

    private async void OnOrderTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigating) return;
        if (sender is not BindableObject bo || bo.BindingContext is not Order order) return;

        try
        {
            _isNavigating = true;
            var detailPage = Application.Current?.Handler?.MauiContext?.Services.GetService<AdminOrderDetailPage>();
            if (detailPage?.BindingContext is not AdminOrderDetailViewModel detailVm)
            {
                await DisplayAlert("Navigation Error", "Unable to open order details.", "OK");
                return;
            }

            detailVm.OrderId = order.OrderId;
            detailVm.Order = order;
            await Navigation.PushAsync(detailPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Failed to open order details: {ex.Message}", "OK");
        }
        finally
        {
            _isNavigating = false;
        }
    }
}
