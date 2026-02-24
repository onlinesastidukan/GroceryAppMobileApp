using GroceryApp.Services;
using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class LoginPage : ContentPage
{
	private readonly ApiService _apiService;
	private readonly AuthService _authService;
	private readonly CartService _cartService;
	private readonly LoginViewModel _viewModel;

	public LoginPage(LoginViewModel viewModel, ApiService apiService, AuthService authService, CartService cartService)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_apiService = apiService;
		_authService = authService;
		_cartService = cartService;
		BindingContext = _viewModel;
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		string userId = UserIdEntry.Text;
		string password = PasswordEntry.Text;

		if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
		{
			ErrorLabel.Text = "Please enter user ID and password";
			ErrorLabel.IsVisible = true;
			return;
		}

		try
		{
			LoadingIndicator.IsRunning = true;
			LoadingIndicator.IsVisible = true;
			ErrorLabel.IsVisible = false;
			
			var success = await _authService.LoginAsync(userId, password, _apiService);

			if (success)
			{
				// Set the auth token in API service
				_apiService.SetAuthToken(_authService.CurrentUser.Token);
				
				// Navigate based on role
				if (_authService.IsAdmin)
				{
					var adminDashboard = Application.Current.Handler.MauiContext.Services.GetService<AdminDashboardPage>();
					await Navigation.PushAsync(adminDashboard);
					Navigation.RemovePage(this);
				}
				else if (_authService.IsCustomer)
				{
					var customerCategory = Application.Current.Handler.MauiContext.Services.GetService<CustomerCategoryPage>();
					await Navigation.PushAsync(customerCategory);
					Navigation.RemovePage(this);
				}
			}
			else
			{
				ErrorLabel.Text = "Invalid user ID or password";
				ErrorLabel.IsVisible = true;
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LOGIN] Error: {ex.Message}");
			ErrorLabel.Text = $"Login error: {ex.Message}";
			ErrorLabel.IsVisible = true;
		}
		finally
		{
			LoadingIndicator.IsRunning = false;
			LoadingIndicator.IsVisible = false;
		}
	}

	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		var registerPage = Application.Current.Handler.MauiContext.Services.GetService<RegisterPage>();
		await Navigation.PushAsync(registerPage);
	}
}
