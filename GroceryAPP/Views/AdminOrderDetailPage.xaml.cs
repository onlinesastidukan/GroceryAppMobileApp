using GroceryApp.ViewModels;

namespace GroceryApp.Views;

public partial class AdminOrderDetailPage : ContentPage
{
    private AdminOrderDetailViewModel? _vm;

    public AdminOrderDetailPage(AdminOrderDetailViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BaseViewModel baseViewModel)
        {
            await baseViewModel.InitializeAsyncSafe();
        }

        if (_vm != null)
        {
            _vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BaseViewModel.HasSuccess) && _vm?.HasSuccess == true)
            MainThread.BeginInvokeOnMainThread(() => _ = AnimateBannerIn(SuccessBanner, autoDismissMs: 3000, onDismiss: () => _vm?.DismissSuccess()));

        if (e.PropertyName == nameof(BaseViewModel.HasStatusInfo) && _vm?.HasStatusInfo == true)
            MainThread.BeginInvokeOnMainThread(() => _ = AnimateBannerIn(StatusInfoBanner, autoDismissMs: 2500, onDismiss: () => _vm?.DismissStatusInfo()));

        if (e.PropertyName == nameof(BaseViewModel.HasError) && _vm?.HasError == true)
            MainThread.BeginInvokeOnMainThread(() => _ = AnimateBannerIn(ErrorBanner));
    }

    private static async Task AnimateBannerIn(Frame banner, int autoDismissMs = 0, Action? onDismiss = null)
    {
        banner.TranslationY = -60;
        banner.Opacity = 0;
        banner.IsVisible = true;
        await Task.WhenAll(
            banner.TranslateTo(0, 0, 280, Easing.CubicOut),
            banner.FadeTo(1.0, 280));

        if (autoDismissMs > 0)
        {
            await Task.Delay(autoDismissMs);
            await Task.WhenAll(
                banner.TranslateTo(0, -60, 220, Easing.CubicIn),
                banner.FadeTo(0, 220));
            banner.IsVisible = false;
            onDismiss?.Invoke();
        }
    }
}
