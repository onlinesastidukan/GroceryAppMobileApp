using GroceryApp.ViewModels;
using GroceryApp.Models;
using GroceryApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryApp.Views;

public partial class CustomerCategoryPage : ContentPage
{
    private readonly CustomerCategoryViewModel _viewModel;
    private readonly AuthService _authService;
    private readonly CartService _cartService;
    private readonly IServiceProvider _serviceProvider;
    private bool _isMenuOpen;
    private bool _isNavigatingCategory;
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

    public CustomerCategoryPage(CustomerCategoryViewModel viewModel, AuthService authService, CartService cartService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
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

    private async void OnCategoryTapped(object sender, TappedEventArgs e)
    {
        if (_isNavigatingCategory)
        {
            return;
        }

        if (sender is not BindableObject bo || bo.BindingContext is not Category category)
        {
            return;
        }

        try
        {
            _isNavigatingCategory = true;
            CategoryLoadingOverlay.IsVisible = true;
            var productsPage = _serviceProvider.GetService<CustomerProductPage>();
            if (productsPage?.BindingContext is not CustomerProductViewModel productVm)
            {
                await DisplayAlert("Navigation Error", "Unable to open selected shop.", "OK");
                return;
            }

            productVm.CategoryId = category.CategoryId;
            productVm.CategoryName = category.Name;

            // Pre-fetch products while the loader is still visible on this page,
            // so the user transitions directly to a page that already has data.
            await productVm.InitializeAsyncSafe();

            await Navigation.PushAsync(productsPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to open shop: {ex.Message}", "OK");
        }
        finally
        {
            CategoryLoadingOverlay.IsVisible = false;
            _isNavigatingCategory = false;
        }
    }

    private async void OnMenuClicked(object sender, EventArgs e)
    {
        await OpenMenu();
    }

    private async void OnCloseMenuClicked(object sender, EventArgs e)
    {
        await CloseMenu();
    }

    private async void OnOverlayTapped(object sender, TappedEventArgs e)
    {
        await CloseMenu();
    }

    private async Task OpenMenu()
    {
        if (_isMenuOpen)
        {
            return;
        }

        _isMenuOpen = true;
        SideMenu.IsVisible = true;
        MenuOverlay.IsVisible = true;
        SideMenu.TranslationX = -300;
        MenuOverlay.Opacity = 0;

        await Task.WhenAll(
            SideMenu.TranslateTo(0, 0, 250, Easing.CubicOut),
            MenuOverlay.FadeTo(1, 250, Easing.CubicOut));
    }

    private async Task CloseMenu()
    {
        if (!_isMenuOpen)
        {
            return;
        }

        await Task.WhenAll(
            SideMenu.TranslateTo(-300, 0, 220, Easing.CubicIn),
            MenuOverlay.FadeTo(0, 220, Easing.CubicIn));

        SideMenu.IsVisible = false;
        MenuOverlay.IsVisible = false;
        _isMenuOpen = false;
    }

    private async void OnMenuCategoriesClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
    }

    private async void OnCartClicked(object sender, EventArgs e)
    {
        await NavigateToCartAsync();
    }

    private async Task NavigateToCartAsync()
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

    private async void OnMenuCartClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await NavigateToCartAsync();
    }

    private async void OnMenuPastOrdersClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        try
        {
            var orderHistoryPage = _serviceProvider.GetService<CustomerOrderHistoryPage>();
            if (orderHistoryPage == null)
            {
                await DisplayAlert("Navigation Error", "Unable to open order history.", "OK");
                return;
            }

            await Navigation.PushAsync(orderHistoryPage);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to open order history: {ex.Message}", "OK");
        }
    }

    private async void OnMenuLogoutClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await _authService.LogoutAsync(_serviceProvider.GetService<ApiService>());

        var loginPage = _serviceProvider.GetService<LoginPage>();
        Application.Current.MainPage = new NavigationPage(loginPage);
    }

    private void RefreshCartBadge()
    {
        CartItemCount = _cartService.TotalItems;
    }
}
