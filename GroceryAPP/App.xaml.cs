using GroceryApp.Views;

namespace GroceryApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		MainPage = new NavigationPage(new SplashPage());
	}
}