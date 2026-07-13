using GroceryApp.Services;
using GroceryApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryApp.Views;

public partial class LoginPage : ContentPage
{
	private readonly ApiService _apiService;
	private readonly AuthService _authService;
	private readonly CartService _cartService;
	private readonly LoginViewModel _viewModel;
	private readonly IServiceProvider _serviceProvider;

	public LoginPage(LoginViewModel viewModel, ApiService apiService, AuthService authService, CartService cartService, IServiceProvider serviceProvider)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_apiService = apiService;
		_authService = authService;
		_cartService = cartService;
		_serviceProvider = serviceProvider;
		BindingContext = _viewModel;
	}

	private async void OnLoginClicked(object sender, EventArgs e)
	{
		string userId = UserIdEntry.Text;
		string password = PasswordEntry.Text;

		if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
		{
			ErrorLabel.Text = "Please enter dealer mobile/user ID and password";
			ErrorLabel.IsVisible = true;
			return;
		}

		try
		{
			LoadingIndicator.IsRunning = true;
			LoadingOverlay.IsVisible = true;
			ErrorLabel.IsVisible = false;

			var success = await _authService.LoginAsync(userId, password, _apiService);

			if (success)
			{
				_apiService.SetAuthToken(_authService.CurrentUser.Token);

				if (_authService.IsAdmin)
				{
					var adminDashboard = _serviceProvider.GetService<AdminDashboardPage>();
					await Navigation.PushAsync(adminDashboard);
					Navigation.RemovePage(this);
				}
				else if (_authService.IsDealer)
				{
					var dealerProducts = _serviceProvider.GetService<AdminProductsPage>();
					await Navigation.PushAsync(dealerProducts);
					Navigation.RemovePage(this);
				}
				else
				{
					ErrorLabel.Text = "This account is not allowed for dealer login.";
					ErrorLabel.IsVisible = true;
				}
			}
			else
			{
				ErrorLabel.Text = string.IsNullOrWhiteSpace(_authService.LastErrorMessage)
					? "Invalid dealer credentials"
					: _authService.LastErrorMessage;
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
			LoadingOverlay.IsVisible = false;
		}
	}

	private async void OnGoToShopCustomerClicked(object sender, EventArgs e)
	{
		try
		{
			await _authService.LogoutAsync(_apiService);
			var customerCategory = _serviceProvider.GetService<CustomerCategoryPage>();
			if (customerCategory == null)
			{
				ErrorLabel.Text = "Unable to open customer shop.";
				ErrorLabel.IsVisible = true;
				return;
			}

			await Navigation.PushAsync(customerCategory);
			Navigation.RemovePage(this);
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Navigation error: {ex.Message}";
			ErrorLabel.IsVisible = true;
		}
	}

	private async void OnRegisterClicked(object sender, EventArgs e)
	{
		var registerPage = _serviceProvider.GetService<RegisterPage>();
		await Navigation.PushAsync(registerPage);
	}
}
