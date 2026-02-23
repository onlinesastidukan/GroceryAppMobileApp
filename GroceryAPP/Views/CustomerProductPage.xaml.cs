using GroceryApp.ViewModels;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class CustomerProductPage : ContentPage
{
    private readonly CustomerProductViewModel _viewModel;

    public CustomerProductPage(CustomerProductViewModel viewModel)
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

    private async void OnCartClicked(object sender, EventArgs e)
    {
        var cartPage = Application.Current.Handler.MauiContext.Services.GetService<CartPage>();
        await Navigation.PushAsync(cartPage);
    }
}
