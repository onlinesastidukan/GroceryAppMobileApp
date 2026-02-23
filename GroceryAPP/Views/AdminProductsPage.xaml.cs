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
        await _viewModel.InitializeAsyncSafe();
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
            // TODO: Implement edit product page
            await DisplayAlert("Info", $"Edit product: {product.Name}", "OK");
        }
    }
}
