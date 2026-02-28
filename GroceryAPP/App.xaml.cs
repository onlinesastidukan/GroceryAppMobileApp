using GroceryApp.Views;

namespace GroceryApp;

public partial class App : Application
{
	public App(SplashPage splashPage)
	{
		InitializeComponent();
		MainPage = new NavigationPage(splashPage);
	}
}