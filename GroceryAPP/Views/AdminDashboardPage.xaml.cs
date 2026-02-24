using GroceryApp.ViewModels;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _viewModel;
    private readonly AuthService _authService;
    private bool _isMenuOpen = false;

    public AdminDashboardPage(AdminDashboardViewModel viewModel, AuthService authService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _authService = authService;
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
        if (_isMenuOpen) return;

        _isMenuOpen = true;
        SideMenu.IsVisible = true;
        Overlay.IsVisible = true;

        // Animate menu sliding in from left
        await Task.WhenAll(
            SideMenu.TranslateTo(0, 0, 250, Easing.CubicOut),
            Overlay.FadeTo(0.5, 250)
        );
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
        if (!_isMenuOpen) return;

        // Animate menu sliding out to left
        await Task.WhenAll(
            SideMenu.TranslateTo(-300, 0, 250, Easing.CubicIn),
            Overlay.FadeTo(0, 250)
        );

        SideMenu.IsVisible = false;
        Overlay.IsVisible = false;
        _isMenuOpen = false;
    }

    // Menu Navigation Handlers
    private async void OnMenuOrdersClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        var ordersPage = Application.Current.Handler.MauiContext.Services.GetService<AdminOrdersPage>();
        await Navigation.PushAsync(ordersPage);
    }

    private async void OnMenuProductsClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        var productsPage = Application.Current.Handler.MauiContext.Services.GetService<AdminProductsPage>();
        await Navigation.PushAsync(productsPage);
    }

    private async void OnMenuCategoriesClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        var categoriesPage = Application.Current.Handler.MauiContext.Services.GetService<AdminCategoriesPage>();
        await Navigation.PushAsync(categoriesPage);
    }

    private async void OnMenuLogoutClicked(object sender, TappedEventArgs e)
    {
        await CloseMenu();
        
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (confirm)
        {
            await _authService.LogoutAsync();
            
            // Navigate back to login page and clear navigation stack
            var loginPage = Application.Current.Handler.MauiContext.Services.GetService<LoginPage>();
            Application.Current.MainPage = new NavigationPage(loginPage);
        }
    }
}
