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
    private readonly IServiceProvider _serviceProvider;
    private bool _isMenuOpen = false;

    public AdminProductsPage(AdminProductsViewModel viewModel, ApiService apiService, AuthService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _apiService = apiService;
        _authService = authService;
        _serviceProvider = serviceProvider;
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

    // Hamburger Menu Methods
    private async void OnMenuClicked(object sender, EventArgs e)
    {
        await OpenMenu();
    }

    private async Task OpenMenu()
    {
        if (_isMenuOpen)
        {
            return;
        }

        try
        {
            _isMenuOpen = true;
            SideMenu.TranslationX = -300;
            SideMenu.IsVisible = true;
            Overlay.Opacity = 0;
            Overlay.InputTransparent = false;
            Overlay.IsVisible = true;

            await Task.WhenAll(
                SideMenu.TranslateTo(0, 0, 250, Easing.CubicOut),
                Overlay.FadeTo(1, 250, Easing.CubicOut)
            );
        }
        catch (Exception ex)
        {
            _isMenuOpen = false;
            SideMenu.IsVisible = false;
            Overlay.IsVisible = false;
            Overlay.InputTransparent = true;
            await DisplayAlert("Menu Error", $"Unable to open menu. {ex.Message}", "OK");
        }
    }

    private async void OnCloseMenuClicked(object sender, EventArgs e)
    {
        await CloseMenu();
    }

    private async void OnOverlayTapped(object sender, TappedEventArgs e)
    {
        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        if (!_isMenuOpen)
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                SideMenu.TranslateTo(-300, 0, 250, Easing.CubicIn),
                Overlay.FadeTo(0, 250, Easing.CubicIn)
            );
        }
        finally
        {
            SideMenu.IsVisible = false;
            Overlay.IsVisible = false;
            Overlay.InputTransparent = true;
            _isMenuOpen = false;
        }
    }

    // Menu Navigation Handlers
    private async void OnMenuDashboardClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        try
        {
            var dashboardPage = _serviceProvider.GetService<AdminDashboardPage>();
            if (dashboardPage != null)
            {
                await Navigation.PushAsync(dashboardPage);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to navigate to Dashboard. {ex.Message}", "OK");
        }
    }

    private async void OnMenuOrdersClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        try
        {
            var ordersPage = _serviceProvider.GetService<AdminOrdersPage>();
            if (ordersPage != null)
            {
                await Navigation.PushAsync(ordersPage);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Unable to navigate to Orders. {ex.Message}", "OK");
        }
    }

    private async void OnMenuProductsClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        // Already on products page, just close menu
    }

    private async void OnMenuLogoutClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await Task.Delay(300); // Wait for menu animation
        await OnLogoutAsync();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await OnLogoutAsync();
    }

    private async Task OnLogoutAsync()
    {
        var confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (!confirm)
        {
            return;
        }

        await _authService.LogoutAsync(_apiService);

        var loginPage = Application.Current?.Handler?.MauiContext?.Services?.GetService<LoginPage>();
        if (loginPage == null)
        {
            await DisplayAlert("Navigation Error", "Unable to load Login page.", "OK");
            return;
        }

        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
}
