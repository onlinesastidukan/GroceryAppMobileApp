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

	private async void OnGoToDealerClicked(object sender, EventArgs e)
	{
		try
		{
			LoadingIndicator.IsRunning = true;
			LoadingOverlay.IsVisible = true;
			ErrorLabel.IsVisible = false;

			var dealerLogin = _serviceProvider.GetService<DealerLoginPage>();
			if (dealerLogin == null)
			{
				ErrorLabel.Text = "Unable to open dealer login.";
				ErrorLabel.IsVisible = true;
				return;
			}

			await Navigation.PushAsync(dealerLogin);
			Navigation.RemovePage(this);
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Navigation error: {ex.Message}";
			ErrorLabel.IsVisible = true;
		}
		finally
		{
			LoadingIndicator.IsRunning = false;
			LoadingOverlay.IsVisible = false;
		}
	}
}
