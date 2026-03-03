using GroceryApp.ViewModels;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _viewModel;
    private readonly AuthService _authService;
    private readonly ApiService _apiService;
    private bool _isMenuOpen = false;

    public AdminDashboardPage(AdminDashboardViewModel viewModel, AuthService authService, ApiService apiService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
        _apiService = apiService;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsyncSafe();
    }

    // Hamburger Menu Toggle
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
    private async void OnMenuOrdersClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await NavigateToPageAsync<AdminOrdersPage>("Orders");
    }

    private async void OnMenuProductsClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await NavigateToPageAsync<AdminProductsPage>("Products");
    }

    private async void OnMenuCategoriesClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        await NavigateToPageAsync<AdminCategoriesPage>("Categories");
    }

    private async void OnMenuLogoutClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await _authService.LogoutAsync(_apiService);
            
            // Navigate back to login page and clear navigation stack
            var loginPage = Application.Current?.Handler?.MauiContext?.Services?.GetService<LoginPage>();
            if (loginPage == null)
            {
                await DisplayAlert("Navigation Error", "Unable to load Login page.", "OK");
                return;
            }

            Application.Current.MainPage = new NavigationPage(loginPage);
        }
    }

    private async void OnOrdersCardTapped(object sender, TappedEventArgs e)
    {
        if (_viewModel.TotalOrders <= 0)
        {
            await DisplayAlert("No Orders", "There are no orders to manage.", "OK");
            return;
        }

        await NavigateToPageAsync<AdminOrdersPage>("Orders");
    }

    private async void OnProductsCardTapped(object sender, TappedEventArgs e)
    {
        if (_viewModel.TotalProducts <= 0)
        {
            await DisplayAlert("No Products", "There are no products to manage.", "OK");
            return;
        }

        await NavigateToPageAsync<AdminProductsPage>("Products");
    }

    private async void OnCategoriesCardTapped(object sender, TappedEventArgs e)
    {
        if (_viewModel.TotalCategories <= 0)
        {
            await DisplayAlert("No Categories", "There are no categories to manage.", "OK");
            return;
        }

        await NavigateToPageAsync<AdminCategoriesPage>("Categories");
    }

    private async void OnRevenueCardTapped(object sender, TappedEventArgs e)
    {
        await NavigateToPageAsync<AdminOrdersPage>("Orders");
    }

    private async Task NavigateToPageAsync<TPage>(string pageName) where TPage : Page
    {
        try
        {
            var page = Application.Current?.Handler?.MauiContext?.Services.GetService<TPage>();
            if (page == null)
            {
                await DisplayAlert("Navigation Error", $"Unable to open {pageName} page.", "OK");
                return;
            }

            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Navigation Error", $"Failed to open {pageName}: {ex.Message}", "OK");
        }
    }
}
