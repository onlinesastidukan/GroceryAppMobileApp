using GroceryApp.Views;
using GroceryApp.Services;

namespace GroceryApp.Views;

public partial class SplashPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly ApiService _apiService;

    public SplashPage(AuthService authService, ApiService apiService)
    {
        InitializeComponent();
        _authService = authService;
        _apiService = apiService;
        
        // Attempt to restore a previous secure session before navigating.
        Dispatcher.StartTimer(TimeSpan.FromSeconds(2), () =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Outer try-catch: prevents any unhandled exception from crashing
                // the async-void lambda (which would kill the Android process).
                try
                {
                    var services = Application.Current?.Handler?.MauiContext?.Services;
                    Page? nextPage = null;

                    try
                    {
                        var restored = await _authService.LoadUserFromLocalStorageAsync();
                        if (restored && _authService.CurrentUser?.Token is { Length: > 0 } token)
                        {
                            _apiService.SetAuthToken(token);
                            nextPage = _authService.IsAdmin
                                ? services?.GetService<AdminDashboardPage>()
                                : services?.GetService<CustomerCategoryPage>();
                        }
                        else
                        {
                            nextPage = services?.GetService<LoginPage>();
                        }
                    }
                    catch
                    {
                        nextPage = services?.GetService<LoginPage>();
                    }

                    if (nextPage == null)
                    {
                        System.Diagnostics.Debug.WriteLine("[SPLASH] nextPage is null — cannot navigate.");
                        return;
                    }

                    await Navigation.PushAsync(nextPage);
                    Navigation.RemovePage(this);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SPLASH] Fatal navigation error: {ex.Message}");
                }
            });
            return false; // Don't repeat the timer
        });
    }
}
