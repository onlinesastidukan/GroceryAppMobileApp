using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using CommunityToolkit.Maui;
using GroceryApp.Services;
using GroceryApp.ViewModels;
using GroceryApp.Views;

namespace GroceryApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register Services
		builder.Services.AddSingleton<ApiService>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<CartService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<AdminDashboardViewModel>();
		builder.Services.AddTransient<AdminProductsViewModel>();
		builder.Services.AddTransient<AdminCategoriesViewModel>();
		builder.Services.AddTransient<AdminOrdersViewModel>();
		builder.Services.AddTransient<AdminOrderDetailViewModel>();
		builder.Services.AddTransient<CustomerCategoryViewModel>();
		builder.Services.AddTransient<CustomerProductViewModel>();
		builder.Services.AddTransient<CustomerOrderHistoryViewModel>();
		builder.Services.AddTransient<CartViewModel>();

		// Register Views
		builder.Services.AddSingleton<SplashPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<AdminDashboardPage>();
		builder.Services.AddTransient<AdminProductsPage>();
		builder.Services.AddTransient<AdminCategoriesPage>();
		builder.Services.AddTransient<AdminOrdersPage>();
		builder.Services.AddTransient<AdminOrderDetailPage>();
		builder.Services.AddTransient<CustomerProductPage>();
		builder.Services.AddTransient<CustomerCategoryPage>();
		builder.Services.AddTransient<CustomerOrderHistoryPage>();
		builder.Services.AddTransient<CartPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
