using GroceryApp.ViewModels;
using GroceryApp.Services;
using GroceryApp.Models;

namespace GroceryApp.Views;

public partial class AdminProductsPage : ContentPage
{
    private readonly AdminProductsViewModel _viewModel;
    private readonly ApiService _apiService;

    public AdminProductsPage(AdminProductsViewModel viewModel, ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
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
        var addProductPage = new AdminAddProductPage(_apiService);
        await Navigation.PushAsync(addProductPage);
    }

    private async void OnEditProductClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Product product)
        {
            var editProductPage = new AdminEditProductPage(_apiService, product);
            await Navigation.PushAsync(editProductPage);
        }
    }
}
