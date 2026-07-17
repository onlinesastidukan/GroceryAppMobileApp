using GroceryApp.ViewModels;
using GroceryApp.Services;
using GroceryApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryApp.Views;

public partial class AdminProductsPage : ContentPage
{
    private readonly AdminProductsViewModel _viewModel;
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    public AdminProductsPage(AdminProductsViewModel viewModel, ApiService apiService, AuthService authService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        _authService = authService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[ADMIN PRODUCTS PAGE] OnAppearing -> calling InitializeAsyncSafe");
        await _viewModel.InitializeAsyncSafe();
        System.Diagnostics.Debug.WriteLine($"[ADMIN PRODUCTS PAGE] InitializeAsyncSafe completed. Current UI Products.Count={_viewModel.Products?.Count ?? 0}");
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        _viewModel.DismissError();

        try
        {
            var addProductPage = new AdminAddProductPage(_apiService, _authService);
            await Navigation.PushAsync(addProductPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to open Add Product page. {ex.Message}", "OK");
        }
    }

    private async void OnEditProductClicked(object sender, EventArgs e)
    {
        _viewModel.DismissError();

        try
        {
            if (sender is Button button && button.CommandParameter is Product product)
            {
                var editProductPage = new AdminEditProductPage(_apiService, _authService, product);
                await Navigation.PushAsync(editProductPage);
            }
            else
            {
                await DisplayAlert("Edit Error", "Unable to determine which product to edit.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Edit Error", $"Unable to open Edit Product page. {ex.Message}", "OK");
        }
    }

    private async void OnRestockProductClicked(object sender, EventArgs e)
    {
        _viewModel.DismissError();

        try
        {
            if (sender is Button button && button.CommandParameter is Product product)
            {
                var editProductPage = new AdminEditProductPage(_apiService, _authService, product);
                await Navigation.PushAsync(editProductPage);
            }
            else
            {
                await DisplayAlert("Restock Error", "Unable to determine product for restock.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Restock Error", $"Unable to open stock editor. {ex.Message}", "OK");
        }
    }

    private void OnErrorBannerTapped(object sender, TappedEventArgs e)
    {
        _viewModel.DismissError();
    }
}
