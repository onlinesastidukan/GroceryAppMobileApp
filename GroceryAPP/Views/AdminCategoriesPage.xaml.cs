using GroceryApp.ViewModels;
using GroceryApp.Services;
using GroceryApp.Models;

namespace GroceryApp.Views;

public partial class AdminCategoriesPage : ContentPage
{
    private readonly AdminCategoriesViewModel _viewModel;
    private readonly ApiService _apiService;

    public AdminCategoriesPage(AdminCategoriesViewModel viewModel, ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        BindingContext = _viewModel;
        System.Diagnostics.Debug.WriteLine("[ADMIN CATEGORIES PAGE] Constructor called");
        System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES PAGE] ViewModel is null: {_viewModel == null}");
        System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES PAGE] Initial Categories count: {_viewModel?.Categories?.Count ?? -1}");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        System.Diagnostics.Debug.WriteLine("[ADMIN CATEGORIES PAGE] OnAppearing called");
        System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES PAGE] ViewModel type: {_viewModel?.GetType().Name}");
        try
        {
            await _viewModel.InitializeAsyncSafe();
            System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES PAGE] InitializeAsyncSafe completed. Categories count: {_viewModel.Categories.Count}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ADMIN CATEGORIES PAGE] Error: {ex.Message}");
        }
    }

    private async void OnAddShopClicked(object sender, EventArgs e)
    {
        var addCategoryPage = new AdminAddCategoryPage(_apiService);
        await Navigation.PushAsync(addCategoryPage);
    }

    private async void OnEditShopClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Category category)
        {
            var editPage = new AdminEditCategoryPage(_apiService, category);
            await Navigation.PushAsync(editPage);
        }
    }
}
