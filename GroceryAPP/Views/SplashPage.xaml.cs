using GroceryApp.Views;

namespace GroceryApp.Views;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
        
        // Navigate to login page after a short delay
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var loginPage = Application.Current.Handler.MauiContext.Services.GetService<LoginPage>();
                await Navigation.PushAsync(loginPage);
                Navigation.RemovePage(this);
            });
            return false; // Don't repeat the timer
        });
    }
}
