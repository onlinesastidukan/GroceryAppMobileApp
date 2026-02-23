using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GroceryApp.Models;
using GroceryApp.Services;

namespace GroceryApp.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiService _apiService;
    private readonly AuthService _authService;

    [ObservableProperty]
    private string email = "";

    [ObservableProperty]
    private string password = "";

    public LoginViewModel(ApiService apiService, AuthService authService)
    {
        _apiService = apiService;
        _authService = authService;
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter email and password");
            return;
        }

        try
        {
            IsLoading = true;
            ClearError();

            var success = await _authService.LoginAsync(Email, Password, _apiService);

            if (success)
            {
                // Navigation is handled in LoginPage.xaml.cs, not here
                // This ViewModel is kept for reference but not used
                SetError("Login successful - UI will navigate");
            }
            else
            {
                SetError("Invalid credentials or login failed");
            }
        }
        catch (Exception ex)
        {
            SetError($"Login error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterAsync()
    {
        // Registration not implemented yet
        SetError("Registration not implemented");
    }

    protected override async Task InitializeAsync()
    {
        // Don't do anything on startup - let user log in
        await base.InitializeAsync();
    }
}
