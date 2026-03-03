using GroceryApp.ViewModels;
using GroceryApp.Services;
using GroceryApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryApp.Views;

public partial class CustomerProductPage : ContentPage
{
    private readonly CustomerProductViewModel _viewModel;
    private readonly CartService _cartService;
    private readonly IServiceProvider _serviceProvider;
    private int _cartItemCount;

    public int CartItemCount
    {
        get => _cartItemCount;
        private set
        {
            if (_cartItemCount == value)
            {
                return;
            }

            _cartItemCount = value;
            OnPropertyChanged(nameof(CartItemCount));
            OnPropertyChanged(nameof(HasCartItems));
        }
    }

    public bool HasCartItems => CartItemCount > 0;

    public CustomerProductPage(CustomerProductViewModel viewModel, CartService cartService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _cartService = cartService;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsyncSafe();
        RefreshCartBadge();
    }

    private async void OnCartClicked(object sender, EventArgs e)
    {
        try
        {
            var cartPage = _serviceProvider.GetService<CartPage>();
            if (cartPage == null)
            {
                await DisplayAlert("Navigation Error", "Unable to open cart.", "OK");
                return;
            }

            await Navigation.PushAsync(cartPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to open cart: {ex.Message}", "OK");
        }
    }

    private async void OnAddToCartClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Product product)
        {
            await _viewModel.AddToCartAsync(product);
            RefreshCartBadge();
        }
    }

    private void RefreshCartBadge()
    {
        CartItemCount = _cartService.TotalItems;
    }

    private async void OnReadMoreClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Product product)
        {
            PopupProductNameLabel.Text = product.Name;
            PopupDescriptionLabel.Text = string.IsNullOrWhiteSpace(product.Description)
                ? "No description available."
                : product.Description;
            PopupPriceLabel.Text = $"₹{product.Price:F0}";
            PopupStockLabel.Text = product.StockStatus;

            DescriptionPopupOverlay.Opacity = 0;
            DescriptionPopupOverlay.IsVisible = true;
            await DescriptionPopupOverlay.FadeTo(1, 200, Easing.CubicOut);
        }
    }

    private async void OnCloseDescriptionPopup(object sender, EventArgs e)
    {
        await HideDescriptionPopup();
    }

    private async void OnPopupBackgroundTapped(object sender, TappedEventArgs e)
    {
        await HideDescriptionPopup();
    }

    private async Task HideDescriptionPopup()
    {
        await DescriptionPopupOverlay.FadeTo(0, 180, Easing.CubicIn);
        DescriptionPopupOverlay.IsVisible = false;
    }
}
