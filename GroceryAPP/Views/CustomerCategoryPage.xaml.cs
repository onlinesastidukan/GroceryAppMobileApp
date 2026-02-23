using GroceryApp.ViewModels;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class CustomerCategoryPage : ContentPage
{
    private readonly CustomerCategoryViewModel _viewModel;

    public CustomerCategoryPage(CustomerCategoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsyncSafe();
    }

    private async void OnCategorySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection?.FirstOrDefault() is Category category)
        {
            var productsPage = Application.Current.Handler.MauiContext.Services.GetService<CustomerProductPage>();
            await Navigation.PushAsync(productsPage);
            
            // Clear selection
            CategoriesCollectionView.SelectedItem = null;
        }
    }

    private async void OnCartClicked(object sender, EventArgs e)
    {
        var cartPage = Application.Current.Handler.MauiContext.Services.GetService<CartPage>();
        await Navigation.PushAsync(cartPage);
    }
}
