using GroceryApp.Services;
using GroceryApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryApp.Views;

public partial class DealerLoginPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    public DealerLoginPage(LoginViewModel viewModel, ApiService apiService, AuthService authService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _apiService = apiService;
        _authService = authService;
        _serviceProvider = serviceProvider;
        BindingContext = viewModel;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var userId = UserIdEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            ErrorLabel.Text = "Please enter shopkeeper mobile number and password";
            ErrorLabel.IsVisible = true;
            return;
        }

        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingOverlay.IsVisible = true;
            ErrorLabel.IsVisible = false;

            // Enforce dealer userId as mobile number.
            var success = await _authService.LoginAsync(userId, password, _apiService);
            if (success)
            {
                _apiService.SetAuthToken(_authService.CurrentUser.Token);

                // Update FCM token after successful login
                try
                {
                    var firebaseService = _serviceProvider.GetService<IFirebaseService>();
                    if (firebaseService != null)
                    {
                        var fcmToken = await firebaseService.GetTokenAsync();
                        if (!string.IsNullOrEmpty(fcmToken))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Mobile] Updating FCM token after login (length: {fcmToken.Length})");
                            await _apiService.UpdateFcmTokenAsync(fcmToken);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[Mobile] No FCM token available");
                        }
                    }
                }
                catch (Exception fcmEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[Mobile] Error updating FCM token: {fcmEx.Message}");
                    // Don't block login if FCM fails
                }

                if (_authService.IsAdmin)
                {
                    var adminDashboard = _serviceProvider.GetService<AdminDashboardPage>();
                    if (adminDashboard != null)
                    {
                        await Navigation.PushAsync(adminDashboard);
                        Navigation.RemovePage(this);
                        return;
                    }
                }

                if (_authService.IsDealer)
                {
                    var dealerProducts = _serviceProvider.GetService<AdminProductsPage>();
                    if (dealerProducts != null)
                    {
                        await Navigation.PushAsync(dealerProducts);
                        Navigation.RemovePage(this);
                        return;
                    }
                }

                ErrorLabel.Text = "This account is not allowed for shopkeeper login.";
                ErrorLabel.IsVisible = true;
            }
            else
            {
                ErrorLabel.Text = string.IsNullOrWhiteSpace(_authService.LastErrorMessage)
                    ? "Invalid shopkeeper credentials"
                    : _authService.LastErrorMessage;
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Login error: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingOverlay.IsVisible = false;
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var registerPage = _serviceProvider.GetService<RegisterPage>();
        if (registerPage != null)
        {
            await Navigation.PushAsync(registerPage);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        var loginPage = _serviceProvider.GetService<LoginPage>();
        if (loginPage != null)
        {
            await Navigation.PushAsync(loginPage);
            Navigation.RemovePage(this);
        }
    }
}
